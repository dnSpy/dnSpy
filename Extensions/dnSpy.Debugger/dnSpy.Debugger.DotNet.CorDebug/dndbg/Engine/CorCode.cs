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
using System.Collections.Generic;
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

		public unsafe ILToNativeMap[] GetILToNativeMapping() {
			int hr = obj.GetILToNativeMapping(0, out uint cMap, IntPtr.Zero);
			if (hr < 0)
				return Array.Empty<ILToNativeMap>();
			var infos = new ILToNativeMap[cMap];
			if (cMap != 0) {
				fixed (void* p = &infos[0])
					hr = obj.GetILToNativeMapping(cMap, out cMap, new IntPtr(p));
				if (hr < 0)
					return Array.Empty<ILToNativeMap>();
			}
			return infos;
		}

		public unsafe VariableHome[] GetVariables() {
			if (!(obj is ICorDebugCode4 c4))
				return Array.Empty<VariableHome>();

			var list = new List<VariableHome>();

			int hr = c4.EnumerateVariableHomes(out var varEnum);
			if (hr < 0)
				return Array.Empty<VariableHome>();
			var varHomeArray = new ICorDebugVariableHome[1];
			const int E_FAIL = unchecked((int)0x80004005);
			for (;;) {
				hr = varEnum.Next((uint)varHomeArray.Length, varHomeArray, out uint count);
				if (hr < 0 || count == 0)
					break;

				var varHome = varHomeArray[0];
				bool error = false;
				VariableHome varInfo = default;

				hr = varHome.GetSlotIndex(out varInfo.SlotIndex);
				if (hr == E_FAIL)
					varInfo.SlotIndex = -1;
				else if (hr < 0)
					error = true;

				hr = varHome.GetArgumentIndex(out varInfo.ArgumentIndex);
				if (hr == E_FAIL)
					varInfo.ArgumentIndex = -1;
				else if (hr < 0)
					error = true;

				hr = varHome.GetLiveRange(out uint startOffset, out uint endOffset);
				if (hr < 0 || startOffset > endOffset)
					error = true;
				varInfo.StartOffset = startOffset;
				varInfo.Length = endOffset - startOffset;

				hr = varHome.GetLocationType(out varInfo.LocationType);
				if (hr < 0)
					error = true;

				switch (varInfo.LocationType) {
				case VariableLocationType.VLT_REGISTER:
					hr = varHome.GetRegister(out varInfo.Register);
					if (hr < 0)
						error = true;
					varInfo.Offset = 0;
					break;

				case VariableLocationType.VLT_REGISTER_RELATIVE:
					hr = varHome.GetRegister(out varInfo.Register);
					if (hr < 0)
						error = true;
					hr = varHome.GetOffset(out varInfo.Offset);
					if (hr < 0)
						error = true;
					break;

				case VariableLocationType.VLT_INVALID:
					break;

				default:
					Debug.Fail($"Unknown location type: {varInfo.LocationType}");
					continue;
				}
				if (error)
					return Array.Empty<VariableHome>();

				list.Add(varInfo);
			}

			return list.Count == 0 ? Array.Empty<VariableHome>() : list.ToArray();
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
			Debug.Assert(hr == 0 || hr == CordbgErrors.CORDBG_E_INVALID_OPCODE || hr == CordbgErrors.CORDBG_E_UNSUPPORTED || hr == CordbgErrors.META_E_BAD_SIGNATURE || hr == E_UNEXPECTED || hr == E_FAIL);
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

	struct VariableHome {
		public int SlotIndex;
		public int ArgumentIndex;
		public ulong StartOffset;
		public uint Length;
		public VariableLocationType LocationType;
		public CorDebugRegister Register;
		public int Offset;
	}
}
