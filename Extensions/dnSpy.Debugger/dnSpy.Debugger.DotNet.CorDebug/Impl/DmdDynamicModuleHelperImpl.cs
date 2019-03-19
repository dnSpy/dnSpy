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
using dndbg.DotNet;
using dndbg.Engine;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	sealed class DmdDynamicModuleHelperImpl : DmdDynamicModuleHelper {
		public override event EventHandler<DmdTypeLoadedEventArgs> TypeLoaded;

		readonly DbgEngineImpl engine;
		const ulong FAT_HEADER_SIZE = 3 * 4;

		public DmdDynamicModuleHelperImpl(DbgEngineImpl engine) => this.engine = engine ?? throw new ArgumentNullException(nameof(engine));

		internal void RaiseTypeLoaded(DmdTypeLoadedEventArgs e) => TypeLoaded?.Invoke(this, e);

		sealed class DmdDataStreamImpl : DmdDataStream {
			readonly ProcessBinaryReader reader;
			public DmdDataStreamImpl(ProcessBinaryReader reader) => this.reader = reader;
			public override long Position {
				get => reader.Position;
				set => reader.Position = (uint)value;
			}
			public override long Length => reader.Length;
			public override byte ReadByte() => reader.ReadByte();
			public override ushort ReadUInt16() => reader.ReadUInt16();
			public override uint ReadUInt32() => reader.ReadUInt32();
			public override ulong ReadUInt64() => reader.ReadUInt64();
			public override float ReadSingle() => reader.ReadSingle();
			public override double ReadDouble() => reader.ReadDouble();
			public override byte[] ReadBytes(int length) => reader.ReadBytes(length);
			public override void Dispose() { }
		}

		public override DmdDataStream TryGetMethodBody(DmdModule module, int metadataToken, uint rva) {
			engine.VerifyCorDebugThread();

			var dbgModule = module.GetDebuggerModule();
			if (dbgModule == null || !engine.TryGetDnModule(dbgModule, out var dnModule))
				throw new InvalidOperationException();

			// rva can be 0 if it's a dynamic module. module.Address will also be 0.
			if (!module.IsDynamic && rva == 0)
				return null;

			var func = dnModule.CorModule.GetFunctionFromToken((uint)metadataToken);
			var ilCode = func?.ILCode;
			if (ilCode == null)
				return null;
			ulong addr = ilCode.Address;
			if (addr == 0)
				return null;

			Debug.Assert(addr >= FAT_HEADER_SIZE);
			if (addr < FAT_HEADER_SIZE)
				return null;

			ProcessBinaryReader reader;
			if (module.IsDynamic) {
				// It's always a fat header, see COMDynamicWrite::SetMethodIL() (coreclr/src/vm/comdynamic.cpp)
				addr -= FAT_HEADER_SIZE;
				reader = new ProcessBinaryReader(new CorProcessReader(dnModule.Process), 0);
				Debug.Assert((reader.Position = (long)addr) == (long)addr);
				Debug.Assert((reader.ReadByte() & 7) == 3);
				Debug.Assert((reader.Position = (long)addr + 4) == (long)addr + 4);
				Debug.Assert(reader.ReadUInt32() == ilCode.Size);

				reader.Position = (long)addr;
			}
			else {
				uint codeSize = ilCode.Size;
				// The address to the code is returned but we want the header. Figure out whether
				// it's the 1-byte or fat header.
				reader = new ProcessBinaryReader(new CorProcessReader(dnModule.Process), 0);
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

				reader.Position = (long)addr - (isBig ? (int)FAT_HEADER_SIZE : 1);
			}
			return new DmdDataStreamImpl(reader);
		}
	}
}
