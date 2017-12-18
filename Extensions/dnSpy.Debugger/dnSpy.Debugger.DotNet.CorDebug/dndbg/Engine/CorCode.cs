/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorCode : COMObject<ICorDebugCode>, IEquatable<CorCode> {
		public bool IsIL { get; }
		public bool SupportsReturnValues => obj is ICorDebugCode3;

		public uint Size => size;
		readonly uint size;

		public ulong Address => address;
		readonly ulong address;

		public uint VersionNumber {
			get {
				int hr = obj.GetVersionNumber(out uint ver);
				return hr < 0 ? 0 : ver;
			}
		}

		public CorFunction Function {
			get {
				int hr = obj.GetFunction(out var func);
				return hr < 0 || func == null ? null : new CorFunction(func);
			}
		}

		public CorDebugJITCompilerFlags CompilerFlags {
			get {
				var c2 = obj as ICorDebugCode2;
				if (c2 == null)
					return 0;
				int hr = c2.GetCompilerFlags(out var flags);
				return hr < 0 ? 0 : flags;
			}
		}

		public CorCode(ICorDebugCode code)
			: base(code) {
			int hr = code.IsIL(out int i);
			IsIL = hr >= 0 && i != 0;

			hr = code.GetSize(out size);
			if (hr < 0)
				size = 0;

			hr = code.GetAddress(out address);
			if (hr < 0)
				address = 0;
		}

		public CorFunctionBreakpoint CreateBreakpoint(uint offset) {
			int hr = obj.CreateBreakpoint(offset, out var fnbp);
			return hr < 0 || fnbp == null ? null : new CorFunctionBreakpoint(fnbp);
		}

		public unsafe CodeChunkInfo[] GetCodeChunks() {
			var c2 = obj as ICorDebugCode2;
			if (c2 == null)
				return Array.Empty<CodeChunkInfo>();
			int hr = c2.GetCodeChunks(0, out uint cnumChunks, IntPtr.Zero);
			if (hr < 0)
				return Array.Empty<CodeChunkInfo>();
			var infos = new CodeChunkInfo[cnumChunks];
			if (cnumChunks != 0) {
				fixed (void* p = &infos[0])
					hr = c2.GetCodeChunks(cnumChunks, out cnumChunks, new IntPtr(p));
				if (hr < 0)
					return Array.Empty<CodeChunkInfo>();
			}
			return infos;
		}

		public unsafe uint[] GetReturnValueLiveOffset(uint ilOffset) {
			var c3 = obj as ICorDebugCode3;
			if (c3 == null)
				return Array.Empty<uint>();
			int hr = c3.GetReturnValueLiveOffset(ilOffset, 0, out uint totalSize, null);
			// E_UNEXPECTED if it returns void
			const int E_UNEXPECTED = unchecked((int)0x8000FFFF);
			// E_FAIL if nothing is found
			const int E_FAIL = unchecked((int)0x80004005);
			Debug.Assert(hr == 0 || hr == CordbgErrors.CORDBG_E_INVALID_OPCODE || hr == CordbgErrors.CORDBG_E_UNSUPPORTED || hr == E_UNEXPECTED || hr == E_FAIL);
			if (hr < 0)
				return Array.Empty<uint>();
			if (totalSize == 0)
				return Array.Empty<uint>();
			var res = new uint[totalSize];
			hr = c3.GetReturnValueLiveOffset(ilOffset, (uint)res.Length, out uint fetched, res);
			if (hr < 0)
				return Array.Empty<uint>();
			if (fetched != (uint)res.Length)
				Array.Resize(ref res, (int)fetched);
			return res;
		}

		public static bool operator ==(CorCode a, CorCode b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorCode a, CorCode b) => !(a == b);
		public bool Equals(CorCode other) => !ReferenceEquals(other, null) && RawObject == other.RawObject;
		public override bool Equals(object obj) => Equals(obj as CorCode);
		public override int GetHashCode() => RawObject.GetHashCode();
	}
}
