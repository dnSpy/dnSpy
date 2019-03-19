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
using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	sealed class CorValue : COMObject<ICorDebugValue>, IEquatable<CorValue> {
		public bool IsGeneric => obj is ICorDebugGenericValue;
		public bool IsReference => obj is ICorDebugReferenceValue;
		public bool IsHandle => obj is ICorDebugHandleValue;
		public bool IsHeap => obj is ICorDebugHeapValue;
		public bool IsHeap2 => obj is ICorDebugHeapValue2;
		public bool IsArray => obj is ICorDebugArrayValue;
		public bool IsBox => obj is ICorDebugBoxValue;
		public bool IsString => obj is ICorDebugStringValue;
		public bool IsObject => obj is ICorDebugObjectValue;
		public bool IsContext => obj is ICorDebugContext;
		public bool IsComObject => obj is ICorDebugComObjectValue;
		public bool IsExceptionObject => obj is ICorDebugExceptionObjectValue;

		public CorElementType ElementType => elemType;
		readonly CorElementType elemType;

		public ulong Size => size;
		readonly ulong size;

		public ulong Address => address;
		readonly ulong address;

		public CorClass Class {
			get {
				var o = obj as ICorDebugObjectValue;
				if (o == null)
					return null;
				int hr = o.GetClass(out var cls);
				return hr < 0 || cls == null ? null : new CorClass(cls);
			}
		}

		public CorType ExactType {
			get {
				var v2 = obj as ICorDebugValue2;
				if (v2 == null)
					return null;
				int hr = v2.GetExactType(out var type);
				return hr < 0 || type == null ? null : new CorType(type);
			}
		}

		public bool IsNull {
			get {
				var r = obj as ICorDebugReferenceValue;
				if (r == null)
					return false;
				int hr = r.IsNull(out int isn);
				return hr >= 0 && isn != 0;
			}
		}

		public ulong ReferenceAddress {
			get {
				var r = obj as ICorDebugReferenceValue;
				if (r == null)
					return 0;
				int hr = r.GetValue(out ulong addr);
				return hr < 0 ? 0 : addr;
			}
		}

		public int SetReferenceAddress(ulong value) {
			var r = obj as ICorDebugReferenceValue;
			if (r == null)
				return -1;
			return r.SetValue(value);
		}

		public CorDebugHandleType HandleType {
			get {
				var h = obj as ICorDebugHandleValue;
				if (h == null)
					return 0;
				int hr = h.GetHandleType(out var type);
				return hr < 0 ? 0 : type;
			}
		}

		public CorValue DereferencedValue => GetDereferencedValue(out _);
		public CorValue GetDereferencedValue(out int hr) {
			var r = obj as ICorDebugReferenceValue;
			if (r == null) {
				hr = -1;
				return null;
			}
			hr = r.Dereference(out var value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		public CorElementType ArrayElementType {
			get {
				var a = obj as ICorDebugArrayValue;
				if (a == null)
					return CorElementType.End;
				int hr = a.GetElementType(out var etype);
				return hr < 0 ? 0 : etype;
			}
		}

		public uint Rank {
			get {
				var a = obj as ICorDebugArrayValue;
				if (a == null)
					return 0;
				int hr = a.GetRank(out uint rank);
				return hr < 0 ? 0 : rank;
			}
		}

		public uint ArrayCount {
			get {
				var a = obj as ICorDebugArrayValue;
				if (a == null)
					return 0;
				int hr = a.GetCount(out uint count);
				return hr < 0 ? 0 : count;
			}
		}

		public unsafe uint[] Dimensions {
			get {
				var a = obj as ICorDebugArrayValue;
				if (a == null)
					return null;
				uint[] dims = new uint[Rank];
				fixed (uint* p = &dims[0]) {
					int hr = a.GetDimensions((uint)dims.Length, new IntPtr(p));
				}
				return dims;
			}
		}

		public bool HasBaseIndicies {
			get {
				var a = obj as ICorDebugArrayValue;
				if (a == null)
					return false;
				int hr = a.HasBaseIndicies(out int has);
				return hr >= 0 && has != 0;
			}
		}

		public unsafe uint[] BaseIndicies {
			get {
				var a = obj as ICorDebugArrayValue;
				if (a == null)
					return null;
				uint[] indicies = new uint[Rank];
				fixed (uint* p = &indicies[0]) {
					int hr = a.GetBaseIndicies((uint)indicies.Length, new IntPtr(p));
				}
				return indicies;
			}
		}

		public CorValue BoxedValue => GetBoxedValue(out _);
		public CorValue GetBoxedValue(out int hr) {
			var b = obj as ICorDebugBoxValue;
			if (b == null) {
				hr = -1;
				return null;
			}
			hr = b.GetObject(out var value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		public uint StringLength {
			get {
				var s = obj as ICorDebugStringValue;
				if (s == null)
					return 0;
				int hr = s.GetLength(out uint len);
				return hr < 0 ? 0 : len;
			}
		}

		public unsafe string String {
			get {
				var s = obj as ICorDebugStringValue;
				if (s == null)
					return null;
				uint len = StringLength;
				if (len == 0)
					return string.Empty;
				var chars = new char[len];
				int hr;
				fixed (char* p = &chars[0]) {
					hr = s.GetString((uint)chars.Length, out len, new IntPtr(p));
				}
				if (hr < 0)
					return null;
				return new string(chars);
			}
		}

		public bool IsValueClass {
			get {
				var o = obj as ICorDebugObjectValue;
				if (o == null)
					return false;
				int hr = o.IsValueClass(out int i);
				return hr >= 0 && i != 0;
			}
		}

		public bool IsNeutered {
			get {
				// If it's neutered, at least one of these (most likely GetType()) should fail.
				int hr = obj.GetType(out var type);
				if (hr == CordbgErrors.CORDBG_E_OBJECT_NEUTERED)
					return true;
				Debug.Assert(hr == 0);
				hr = obj.GetAddress(out ulong addr);
				if (hr == CordbgErrors.CORDBG_E_OBJECT_NEUTERED)
					return true;
				Debug.Assert(hr == 0);
				hr = obj.GetSize(out uint size);
				if (hr == CordbgErrors.CORDBG_E_OBJECT_NEUTERED)
					return true;
				Debug.Assert(hr == 0);

				return false;
			}
		}

		public CorValue(ICorDebugValue value)
			: base(value) {
			int hr = value.GetType(out elemType);
			if (hr < 0)
				elemType = CorElementType.End;

			bool initdSize = false;
			if (value is ICorDebugValue3 v3)
				initdSize = v3.GetSize64(out size) == 0;
			if (!initdSize) {
				hr = value.GetSize(out uint size32);
				if (hr < 0)
					size32 = 0;
				size = size32;
			}

			hr = value.GetAddress(out address);
			if (hr < 0)
				address = 0;
		}

		public bool DisposeHandle() {
			var h = obj as ICorDebugHandleValue;
			if (h == null)
				return false;
			int hr = h.Dispose();
			bool success = hr == 0 || hr == CordbgErrors.CORDBG_E_OBJECT_NEUTERED;
			Debug.Assert(success);
			return success;
		}

		public CorValue GetElementAtPosition(uint index, out int hr) {
			var a = obj as ICorDebugArrayValue;
			if (a == null) {
				hr = -1;
				return null;
			}
			hr = a.GetElementAtPosition(index, out var value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		public CorValue GetElementAtPosition(int index, out int hr) => GetElementAtPosition((uint)index, out hr);

		public CorValue GetFieldValue(CorClass cls, uint token) => GetFieldValue(cls, token, out int hr);
		public CorValue GetFieldValue(CorClass cls, uint token, out int hr) {
			var o = obj as ICorDebugObjectValue;
			if (o == null || cls == null) {
				hr = -1;
				return null;
			}
			hr = o.GetFieldValue(cls.RawObject, token, out var value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		public CorValue CreateHandle(CorDebugHandleType type) {
			var h2 = obj as ICorDebugHeapValue2;
			if (h2 == null)
				return null;
			int hr = h2.CreateHandle(type, out var value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		public unsafe int WriteGenericValue(byte[] data, CorProcess process = null) {
			if (data == null || (uint)data.Length != Size)
				return -1;
			var g = obj as ICorDebugGenericValue;
			if (g == null)
				return -1;
			int hr;
			fixed (byte* p = &data[0]) {
				// This sometimes fails with CORDBG_E_CLASS_NOT_LOADED (ImmutableArray<T>, debugging VS2017).
				// If it fails, use process.WriteMemory().
				hr = g.SetValue(new IntPtr(p));
				if (hr < 0 && process != null) {
					hr = process.WriteMemory(address, data, 0, data.Length, out var sizeWritten);
					if (sizeWritten != data.Length && hr >= 0)
						hr = -1;
				}
			}
			return hr;
		}

		public unsafe byte[] ReadGenericValue() {
			var g = obj as ICorDebugGenericValue;
			if (g == null)
				return null;
			var data = new byte[Size];
			int hr;
			fixed (byte* p = &data[0]) {
				hr = g.GetValue(new IntPtr(p));
			}
			return hr < 0 ? null : data;
		}

		public static bool operator ==(CorValue a, CorValue b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorValue a, CorValue b) => !(a == b);
		public bool Equals(CorValue other) => !ReferenceEquals(other, null) && RawObject == other.RawObject;
		public override bool Equals(object obj) => Equals(obj as CorValue);
		public override int GetHashCode() => RawObject.GetHashCode();
	}
}
