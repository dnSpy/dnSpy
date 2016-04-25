/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using System.Linq;
using dndbg.DotNet;
using dnlib.DotNet;
using dnlib.IO;
using dnlib.PE;

namespace dndbg.Engine {
	sealed class CorModuleDefHelper : ICorModuleDefHelper {
		const ulong FAT_HEADER_SIZE = 3 * 4;
		readonly DnModule module;
		ImageSectionHeader[] sectionHeaders;

		public CorModuleDefHelper(DnModule module) {
			this.module = module;
			Debug.Assert(!module.IsDynamic || module.Address == 0);
		}

		public IAssembly CorLib {
			get {
				var corAsm = this.module.AppDomain.Assemblies.FirstOrDefault();
				Debug.Assert(corAsm != null);
				if (corAsm == null)
					return AssemblyRefUser.CreateMscorlibReferenceCLR20();
				var corMod = corAsm.Modules.FirstOrDefault();
				Debug.Assert(corMod != null);
				if (corMod == null)
					return AssemblyRefUser.CreateMscorlibReferenceCLR20();
				return corMod.GetOrCreateCorModuleDef().Assembly;
			}
		}

		public bool IsDynamic {
			get { return module.IsDynamic; }
		}

		public bool IsInMemory {
			get { return module.IsInMemory; }
		}

		public bool? IsCorLib {
			get { return this.module.Assembly.UniqueIdAppDomain == 0 && this.module.UniqueIdAppDomain == 0; }
		}

		public string Filename {
			get {
				if (this.module.IsInMemory)
					return null;
				return this.module.Name;
			}
		}

		public bool IsManifestModule {
			get { return this.module.CorModule.IsManifestModule; }
		}

		public IBinaryReader CreateBodyReader(uint bodyRva, uint mdToken) {
			// bodyRva can be 0 if it's a dynamic module. this.module.Address will also be 0.
			if (!this.module.IsDynamic && bodyRva == 0)
				return null;

			var func = this.module.CorModule.GetFunctionFromToken(mdToken);
			var ilCode = func == null ? null : func.ILCode;
			if (ilCode == null)
				return null;
			ulong addr = ilCode.Address;
			if (addr == 0)
				return null;

			Debug.Assert(addr >= FAT_HEADER_SIZE);
			if (addr < FAT_HEADER_SIZE)
				return null;

			if (this.module.IsDynamic) {
				// It's always a fat header, see COMDynamicWrite::SetMethodIL() (coreclr/src/vm/comdynamic.cpp)
				addr -= FAT_HEADER_SIZE;
				var reader = new ProcessBinaryReader(new CorProcessReader(this.module.Process), 0);
				Debug.Assert((reader.Position = (long)addr) == (long)addr);
				Debug.Assert((reader.ReadByte() & 7) == 3);
				Debug.Assert((reader.Position = (long)addr + 4) == (long)addr + 4);
				Debug.Assert(reader.ReadUInt32() == ilCode.Size);
				reader.Position = (long)addr;
				return reader;
			}
			else {
				uint codeSize = ilCode.Size;
				// The address to the code is returned but we want the header. Figure out whether
				// it's the 1-byte or fat header.
				var reader = new ProcessBinaryReader(new CorProcessReader(this.module.Process), 0);
				uint locVarSigTok = func.LocalVarSigToken;
				bool isBig = codeSize >= 0x40 || (locVarSigTok & 0x00FFFFFF) != 0;
				if (!isBig) {
					reader.Position = (long)addr - 1;
					byte b = reader.ReadByte();
					var type = b & 7;
					if ((type == 2 || type == 6) && (b >> 2) == codeSize) {
						// probably small header
						isBig = false;
					}
					else {
						reader.Position = (long)addr - (long)FAT_HEADER_SIZE + 4;
						uint headerCodeSize = reader.ReadUInt32();
						uint headerLocVarSigTok = reader.ReadUInt32();
						bool valid = headerCodeSize == codeSize &&
							(locVarSigTok & 0x00FFFFFF) == (headerLocVarSigTok & 0x00FFFFFF) &&
							((locVarSigTok & 0x00FFFFFF) == 0 || locVarSigTok == headerLocVarSigTok);
						Debug.Assert(valid);
						if (!valid)
							return null;
						isBig = true;
					}
				}

				reader.Position = (long)addr - (long)(isBig ? FAT_HEADER_SIZE : 1);
				return reader;
			}
		}

		public byte[] ReadFieldInitialValue(uint fieldRva, uint fdToken, int size) {
			if (this.module.IsDynamic) {
				//TODO: See COMModule::SetFieldRVAContent()
				//TODO: Perhaps you could create a type using the CorDebug eval API and read the value?
				return null;
			}

			return ReadFromRVA(fieldRva, size);
		}

		byte[] ReadFromRVA(uint rva, int size) {
			if (module.IsDynamic)
				return null;

			ulong addr = module.Address;
			Debug.Assert(addr != 0);
			if (addr == 0)
				return null;

			var offs = RVAToAddressOffset(rva);
			if (offs == null)
				return null;
			addr += offs.Value;

			var data = module.Process.CorProcess.ReadMemory(addr, size);
			Debug.Assert(data != null && data.Length == size);
			return data;
		}

		uint? RVAToAddressOffset(uint rva) {
			if (module.IsDynamic)
				return null;
			if (!module.IsInMemory)
				return rva;
			return RVAToFileOffset(rva);
		}

		uint? RVAToFileOffset(uint rva) {
			foreach (var sh in GetOrCreateSectionHeaders()) {
				if ((uint)sh.VirtualAddress <= rva && rva < (uint)sh.VirtualAddress + Math.Max(sh.SizeOfRawData, sh.VirtualSize))
					return rva - (uint)sh.VirtualAddress + sh.PointerToRawData;
			}

			return null;
		}

		ImageSectionHeader[] GetOrCreateSectionHeaders() {
			var h = sectionHeaders;
			if (h != null)
				return h;

			try {
				ulong addr = module.Address;
				if (addr == 0)
					return sectionHeaders = new ImageSectionHeader[0];
				var data = new byte[0x1000];
				int sizeRead;
				this.module.Process.CorProcess.ReadMemory(this.module.Address, data, 0, data.Length, out sizeRead);
				using (var peImage = new PEImage(data, !module.IsDynamic && module.IsInMemory ? ImageLayout.File : ImageLayout.Memory, true))
					return sectionHeaders = peImage.ImageSectionHeaders.ToArray();
			}
			catch {
				Debug.Fail("Couldn't read section headers");
			}
			return sectionHeaders = new ImageSectionHeader[0];
		}

		public IImageStream CreateResourceStream(uint offset) {
			if (module.IsDynamic) {
				//TODO: 
				return null;
			}

			//TODO: See ModuleDefMD.CreateResourceStream()
			return null;
		}
	}
}
