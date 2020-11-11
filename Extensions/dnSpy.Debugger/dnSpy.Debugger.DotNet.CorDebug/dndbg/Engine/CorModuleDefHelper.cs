/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using dndbg.DotNet;
using dnlib.DotNet;
using dnlib.IO;
using dnlib.PE;

namespace dndbg.Engine {
	sealed class CorModuleDefHelper : ICorModuleDefHelper {
		const ulong FAT_HEADER_SIZE = 3 * 4;
		readonly DnModule module;
		ImageSectionHeader[]? sectionHeaders;

		public CorModuleDefHelper(DnModule module) {
			this.module = module;
			Debug.Assert(!module.IsDynamic || module.Address == 0);
		}

		public IAssembly CorLib {
			get {
				var corAsm = module.AppDomain.Assemblies.FirstOrDefault();
				Debug2.Assert(corAsm is not null);
				if (corAsm is null)
					return AssemblyRefUser.CreateMscorlibReferenceCLR20();
				var corMod = corAsm.Modules.FirstOrDefault();
				Debug2.Assert(corMod is not null);
				if (corMod is null)
					return AssemblyRefUser.CreateMscorlibReferenceCLR20();
				return corMod.GetOrCreateCorModuleDef().Assembly;
			}
		}

		public bool IsDynamic => module.IsDynamic;
		public bool IsInMemory => module.IsInMemory;
		public bool? IsCorLib => module.Assembly.UniqueIdAppDomain == 0 && module.UniqueIdAppDomain == 0;

		public string? Filename {
			get {
				if (module.IsInMemory)
					return null;
				return module.Name;
			}
		}

		public bool IsManifestModule => module.CorModule.IsManifestModule;

		public bool TryCreateBodyReader(uint bodyRva, uint mdToken, out DataReader reader) {
			reader = default;

			// bodyRva can be 0 if it's a dynamic module. this.module.Address will also be 0.
			if (!module.IsDynamic && bodyRva == 0)
				return false;

			var func = module.CorModule.GetFunctionFromToken(mdToken);
			var ilCode = func?.ILCode;
			if (ilCode is null)
				return false;
			Debug2.Assert(func is not null);
			ulong addr = ilCode.Address;
			if (addr == 0)
				return false;

			Debug.Assert(addr >= FAT_HEADER_SIZE);
			if (addr < FAT_HEADER_SIZE)
				return false;

			if (module.IsDynamic) {
				// It's always a fat header, see COMDynamicWrite::SetMethodIL() (coreclr/src/vm/comdynamic.cpp)
				addr -= FAT_HEADER_SIZE;
				var procReader = new ProcessBinaryReader(new CorProcessReader(module.Process), 0);
				Debug.Assert((procReader.Position = (long)addr) == (long)addr);
				Debug.Assert((procReader.ReadByte() & 7) == 3);
				Debug.Assert((procReader.Position = (long)addr + 4) == (long)addr + 4);
				Debug.Assert(procReader.ReadUInt32() == ilCode.Size);
				procReader.Position = (long)addr;
				reader = new DataReader(new ProcessDataStream(procReader), 0, uint.MaxValue);
				return true;
			}
			else {
				uint codeSize = ilCode.Size;
				// The address to the code is returned but we want the header. Figure out whether
				// it's the 1-byte or fat header.
				var procReader = new ProcessBinaryReader(new CorProcessReader(module.Process), 0);
				uint locVarSigTok = func.LocalVarSigToken;
				bool isBig = codeSize >= 0x40 || (locVarSigTok & 0x00FFFFFF) != 0;
				if (!isBig) {
					procReader.Position = (long)addr - 1;
					byte b = procReader.ReadByte();
					var type = b & 7;
					if ((type == 2 || type == 6) && (b >> 2) == codeSize) {
						// probably small header
						isBig = false;
					}
					else {
						procReader.Position = (long)addr - (long)FAT_HEADER_SIZE + 4;
						uint headerCodeSize = procReader.ReadUInt32();
						uint headerLocVarSigTok = procReader.ReadUInt32();
						bool valid = headerCodeSize == codeSize &&
							(locVarSigTok & 0x00FFFFFF) == (headerLocVarSigTok & 0x00FFFFFF) &&
							((locVarSigTok & 0x00FFFFFF) == 0 || locVarSigTok == headerLocVarSigTok);
						Debug.Assert(valid);
						if (!valid)
							return false;
						isBig = true;
					}
				}

				procReader.Position = (long)addr - (long)(isBig ? FAT_HEADER_SIZE : 1);
				reader = new DataReader(new ProcessDataStream(procReader), 0, uint.MaxValue);
				return true;
			}
		}

		public byte[]? ReadFieldInitialValue(uint fieldRva, uint fdToken, int size) {
			if (module.IsDynamic)
				return null;

			return ReadFromRVA(fieldRva, size);
		}

		byte[]? ReadFromRVA(uint rva, int size) {
			if (module.IsDynamic)
				return null;

			ulong addr = module.Address;
			Debug.Assert(addr != 0);
			if (addr == 0)
				return null;

			var offs = RVAToAddressOffset(rva);
			if (offs is null)
				return null;
			addr += offs.Value;

			var data = module.Process.CorProcess.ReadMemory(addr, size);
			Debug2.Assert(data is not null && data.Length == size);
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
			if (h is not null)
				return h;

			try {
				ulong addr = module.Address;
				if (addr == 0)
					return sectionHeaders = Array.Empty<ImageSectionHeader>();
				var data = new byte[0x1000];
				module.Process.CorProcess.ReadMemory(module.Address, data, 0, data.Length, out int sizeRead);
				using (var peImage = new PEImage(data, !module.IsDynamic && module.IsInMemory ? ImageLayout.File : ImageLayout.Memory, true))
					return sectionHeaders = peImage.ImageSectionHeaders.ToArray();
			}
			catch {
				Debug.Fail("Couldn't read section headers");
			}
			return sectionHeaders = Array.Empty<ImageSectionHeader>();
		}

		public bool TryCreateResourceStream(uint offset, [NotNullWhen(true)] out DataReaderFactory? dataReaderFactory, out uint resourceOffset, out uint resourceLength) {
			if (module.IsDynamic) {
				//TODO: 
				dataReaderFactory = null;
				resourceOffset = 0;
				resourceLength = 0;
				return false;
			}

			//TODO: See ModuleDefMD.CreateResourceStream()
			dataReaderFactory = null;
			resourceOffset = 0;
			resourceLength = 0;
			return false;
		}
	}
}
