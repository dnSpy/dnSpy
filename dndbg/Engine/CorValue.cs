/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Linq;
using dndbg.Engine.COM.CorDebug;
using dndbg.Engine.COM.MetaData;
using dnlib.DotNet;

namespace dndbg.Engine {
	public sealed class CorValue : COMObject<ICorDebugValue>, IEquatable<CorValue> {
		/// <summary>
		/// true if it's a <see cref="ICorDebugGenericValue"/>
		/// </summary>
		public bool IsGeneric {
			get { return obj is ICorDebugGenericValue; }
		}

		/// <summary>
		/// true if it's a <see cref="ICorDebugReferenceValue"/>
		/// </summary>
		public bool IsReference {
			get { return obj is ICorDebugReferenceValue; }
		}

		/// <summary>
		/// true if it's a <see cref="ICorDebugHandleValue"/>
		/// </summary>
		public bool IsHandle {
			get { return obj is ICorDebugHandleValue; }
		}

		/// <summary>
		/// true if it's a <see cref="ICorDebugHeapValue"/>, <see cref="ICorDebugArrayValue"/>,
		/// <see cref="ICorDebugBoxValue"/> or <see cref="ICorDebugStringValue"/>
		/// </summary>
		public bool IsHeap {
			get { return obj is ICorDebugHeapValue; }
		}

		/// <summary>
		/// true if it's a <see cref="ICorDebugArrayValue"/>
		/// </summary>
		public bool IsArray {
			get { return obj is ICorDebugArrayValue; }
		}

		/// <summary>
		/// true if it's a <see cref="ICorDebugBoxValue"/>
		/// </summary>
		public bool IsBox {
			get { return obj is ICorDebugBoxValue; }
		}

		/// <summary>
		/// true if it's a <see cref="ICorDebugStringValue"/>
		/// </summary>
		public bool IsString {
			get { return obj is ICorDebugStringValue; }
		}

		/// <summary>
		/// true if it's a <see cref="ICorDebugObjectValue"/>
		/// </summary>
		public bool IsObject {
			get { return obj is ICorDebugObjectValue; }
		}

		/// <summary>
		/// true if it's a <see cref="ICorDebugContext"/>
		/// </summary>
		public bool IsContext {
			get { return obj is ICorDebugContext; }
		}

		/// <summary>
		/// true if it's a <see cref="ICorDebugComObjectValue"/>, ie., an RCW (Runtime Callable Wrapper)
		/// </summary>
		public bool IsComObject {
			get { return obj is ICorDebugComObjectValue; }
		}

		/// <summary>
		/// Gets the element type of this value
		/// </summary>
		public CorElementType Type {
			get { return elemType; }
		}
		readonly CorElementType elemType;

		/// <summary>
		/// Gets the size of the value
		/// </summary>
		public ulong Size {
			get { return size; }
		}
		readonly ulong size;

		/// <summary>
		/// Gets the address of the value or 0 if it's not available, eg. it could be in a register
		/// </summary>
		public ulong Address {
			get { return address; }
		}
		ulong address;

		/// <summary>
		/// Gets the class or null if it's not a <see cref="ICorDebugObjectValue"/> or
		/// <see cref="ICorDebugContext"/>
		/// </summary>
		public CorClass Class {
			get {
				var o = obj as ICorDebugObjectValue;
				if (o == null)
					return null;
				ICorDebugClass cls;
				int hr = o.GetClass(out cls);
				return hr < 0 || cls == null ? null : new CorClass(cls);
			}
		}

		/// <summary>
		/// Gets the type or null
		/// </summary>
		public CorType ExactType {
			get {
				var v2 = obj as ICorDebugValue2;
				if (v2 == null)
					return null;
				ICorDebugType type;
				int hr = v2.GetExactType(out type);
				return hr < 0 || type == null ? null : new CorType(type);
			}
		}

		/// <summary>
		/// true if this is a <see cref="ICorDebugReferenceValue"/> and the reference is null
		/// </summary>
		public bool IsNull {
			get {
				var r = obj as ICorDebugReferenceValue;
				if (r == null)
					return false;
				int isn;
				int hr = r.IsNull(out isn);
				return hr >= 0 && isn != 0;
			}
		}

		/// <summary>
		/// Gets/sets the address to which <see cref="ICorDebugReferenceValue"/> points
		/// </summary>
		public ulong ReferenceAddress {
			get {
				var r = obj as ICorDebugReferenceValue;
				if (r == null)
					return 0;
				ulong addr;
				int hr = r.GetValue(out addr);
				return hr < 0 ? 0 : addr;
			}
			set {
				var r = obj as ICorDebugReferenceValue;
				if (r == null)
					return;
				int hr = r.SetValue(value);
			}
		}

		/// <summary>
		/// Gets the handle type if it's a <see cref="ICorDebugHandleValue"/>
		/// </summary>
		public CorDebugHandleType HandleType {
			get {
				var h = obj as ICorDebugHandleValue;
				if (h == null)
					return 0;
				CorDebugHandleType type;
				int hr = h.GetHandleType(out type);
				return hr < 0 ? 0 : type;
			}
		}

		/// <summary>
		/// Gets the dereferenced value to which <see cref="ICorDebugReferenceValue"/> points or null
		/// </summary>
		public CorValue DereferencedValue {
			get {
				var r = obj as ICorDebugReferenceValue;
				if (r == null)
					return null;
				ICorDebugValue value;
				int hr = r.Dereference(out value);
				return hr < 0 || value == null ? null : new CorValue(value);
			}
		}

		/// <summary>
		/// Gets the type of the array's elements or <see cref="CorElementType.End"/> if it's not an array
		/// </summary>
		public CorElementType ArrayElementType {
			get {
				var a = obj as ICorDebugArrayValue;
				if (a == null)
					return CorElementType.End;
				CorElementType etype;
				int hr = a.GetElementType(out etype);
				return hr < 0 ? 0 : etype;
			}
		}

		/// <summary>
		/// Gets the rank of the array or 0 if it's not an array
		/// </summary>
		public uint Rank {
			get {
				var a = obj as ICorDebugArrayValue;
				if (a == null)
					return 0;
				uint rank;
				int hr = a.GetRank(out rank);
				return hr < 0 ? 0 : rank;
			}
		}

		/// <summary>
		/// Gets the number of elements in the array or 0 if it's not an array
		/// </summary>
		public uint ArrayCount {
			get {
				var a = obj as ICorDebugArrayValue;
				if (a == null)
					return 0;
				uint count;
				int hr = a.GetCount(out count);
				return hr < 0 ? 0 : count;
			}
		}

		/// <summary>
		/// Gets the dimensions or null if it's not an array
		/// </summary>
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

		/// <summary>
		/// true if the array has base indices
		/// </summary>
		public bool HasBaseIndicies {
			get {
				var a = obj as ICorDebugArrayValue;
				if (a == null)
					return false;
				int has;
				int hr = a.HasBaseIndicies(out has);
				return hr >= 0 && has != 0;
			}
		}

		/// <summary>
		/// Gets all base indicies or null if it's not an array
		/// </summary>
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

		/// <summary>
		/// Gets the boxed object value or null if none. It's a <see cref="ICorDebugObjectValue"/>
		/// </summary>
		public CorValue BoxedValue {
			get {
				var b = obj as ICorDebugBoxValue;
				if (b == null)
					return null;
				ICorDebugObjectValue value;
				int hr = b.GetObject(out value);
				return hr < 0 || value == null ? null : new CorValue(value);
			}
		}

		/// <summary>
		/// Gets the length of the string in characters or 0 if it's not a <see cref="ICorDebugStringValue"/>
		/// </summary>
		public uint StringLength {
			get {
				var s = obj as ICorDebugStringValue;
				if (s == null)
					return 0;
				uint len;
				int hr = s.GetLength(out len);
				return hr < 0 ? 0 : len;
			}
		}

		/// <summary>
		/// Gets the string or null if it's not a <see cref="ICorDebugStringValue"/>
		/// </summary>
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

		/// <summary>
		/// true if this is a <see cref="ICorDebugObjectValue"/> and it's a value type
		/// </summary>
		public bool IsValueClass {
			get {
				var o = obj as ICorDebugObjectValue;
				if (o == null)
					return false;
				int i;
				int hr = o.IsValueClass(out i);
				return hr >= 0 && i != 0;
			}
		}

		/// <summary>
		/// Gets an ordered list of threads that are queued on the event that is associated with a
		/// monitor lock
		/// </summary>
		public IEnumerable<CorThread> MonitorEventWaitList {
			get {
				var h3 = obj as ICorDebugHeapValue3;
				if (h3 == null)
					yield break;
				ICorDebugThreadEnum threadEnum;
				int hr = h3.GetMonitorEventWaitList(out threadEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					ICorDebugThread thread = null;
					uint count;
					hr = threadEnum.Next(1, out thread, out count);
					if (hr != 0 || thread == null)
						break;
					yield return new CorThread(thread);
				}
			}
		}

		/// <summary>
		/// Gets the value. Only values of simple types are currently returned: integers, floating points,
		/// decimal, string and null.
		/// </summary>
		public CorValueResult Value {
			get { return CorValueReader.ReadSimpleTypeValue(this); }
		}

		public CorValue(ICorDebugValue value)
			: base(value) {
			int hr = value.GetType(out this.elemType);
			if (hr < 0)
				this.elemType = CorElementType.End;

			bool initdSize = false;
			var v3 = value as ICorDebugValue3;
			if (v3 != null)
				initdSize = v3.GetSize64(out this.size) == 0;
			if (!initdSize) {
				uint size32;
				hr = value.GetSize(out size32);
				if (hr < 0)
					size32 = 0;
				this.size = size32;
			}

			hr = value.GetAddress(out this.address);
			if (hr < 0)
				this.address = 0;
		}

		/// <summary>
		/// Disposes the handle if it's a <see cref="ICorDebugHandleValue"/>
		/// </summary>
		/// <returns></returns>
		public bool DisposeHandle() {
			var h = obj as ICorDebugHandleValue;
			if (h == null)
				return false;
			int hr = h.Dispose();
			return hr >= 0;
		}

		/// <summary>
		/// Gets the value at a specified index in the array or null. The array is treated as a
		/// zero-based, single-dimensional array
		/// </summary>
		/// <param name="index">Index of element</param>
		/// <returns></returns>
		public CorValue GetElementAtPosition(uint index) {
			var a = obj as ICorDebugArrayValue;
			if (a == null)
				return null;
			ICorDebugValue value;
			int hr = a.GetElementAtPosition(index, out value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		/// <summary>
		/// Gets the value at the specified indices or null
		/// </summary>
		/// <param name="indices">Indices into the array</param>
		/// <returns></returns>
		public unsafe CorValue GetElement(uint[] indices) {
			Debug.Assert(indices != null && (uint)indices.Length == Rank);
			var a = obj as ICorDebugArrayValue;
			if (a == null)
				return null;
			int hr;
			ICorDebugValue value;
			fixed (uint* p = &indices[0]) {
				hr = a.GetElement((uint)indices.Length, new IntPtr(p), out value);
			}
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		/// <summary>
		/// Gets the value of a field or null if it's not a <see cref="ICorDebugObjectValue"/>
		/// </summary>
		/// <param name="cls">Class</param>
		/// <param name="token">Token of field in <paramref name="cls"/></param>
		/// <returns></returns>
		public CorValue GetFieldValue(CorClass cls, uint token) {
			var o = obj as ICorDebugObjectValue;
			if (o == null)
				return null;
			ICorDebugValue value;
			int hr = o.GetFieldValue(cls.RawObject, token, out value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		/// <summary>
		/// Gets the value of a field. Returns null if field wasn't found or there was another error
		/// </summary>
		/// <param name="name">Name of field</param>
		/// <returns></returns>
		public CorValue GetFieldValue(string name) {
			var self = this;
			if (self.IsReference) {
				self = self.DereferencedValue;
				if (self == null)
					return null;
			}

			var cls = self.Class;
			if (cls == null) {
				var type = self.ExactType;
				if (type != null)
					cls = type.Class;
				if (cls == null)
					return null;
			}
			var module = cls.Module;
			if (module == null)
				return null;
			var info = MetaDataUtils.GetFields(module.GetMetaDataInterface<IMetaDataImport>(), cls.Token).FirstOrDefault(i => i.Name == name);
			if (info.Name == null)
				return null;
			return self.GetFieldValue(cls, info.Token);
		}

		/// <summary>
		/// Creates a handle to this <see cref="ICorDebugHeapValue"/>. The returned value is a
		/// <see cref="ICorDebugHandleValue"/>.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public CorValue CreateHandle(CorDebugHandleType type) {
			var h2 = obj as ICorDebugHeapValue2;
			if (h2 == null)
				return null;
			ICorDebugHandleValue value;
			int hr = h2.CreateHandle(type, out value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		/// <summary>
		/// Returns the managed thread that owns the monitor lock on this object
		/// </summary>
		/// <param name="acquisitionCount">The number of times this thread would have to release the
		/// lock before it returns to being unowned</param>
		/// <returns></returns>
		public CorThread GetThreadOwningMonitorLock(out uint acquisitionCount) {
			acquisitionCount = 0;
			var h3 = obj as ICorDebugHeapValue3;
			if (h3 == null)
				return null;
			ICorDebugThread thread;
			int hr = h3.GetThreadOwningMonitorLock(out thread, out acquisitionCount);
			return hr < 0 || thread == null ? null : new CorThread(thread);
		}

		/// <summary>
		/// Writes a new value. Can be called if <see cref="IsGeneric"/> is true
		/// </summary>
		/// <param name="data">Data</param>
		/// <returns></returns>
		public unsafe int WriteGenericValue(byte[] data) {
			if (data == null || (uint)data.Length != Size)
				return -1;
			var g = obj as ICorDebugGenericValue;
			if (g == null)
				return -1;
			int hr;
			fixed (byte* p = &data[0]) {
				hr = g.SetValue(new IntPtr(p));
			}
			return hr;
		}

		/// <summary>
		/// Reads the data. Can be called if <see cref="IsGeneric"/> is true. Returns null if there
		/// was an error.
		/// </summary>
		/// <returns></returns>
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

		public static bool operator !=(CorValue a, CorValue b) {
			return !(a == b);
		}

		public bool Equals(CorValue other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorValue);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public T Write<T>(T output, TypePrinterFlags flags) where T : ITypeOutput {
			new TypePrinter(output, flags).Write(this);
			return output;
		}

		public T WriteType<T>(T output, TypeSig ts, List<CorType> typeArgs, List<CorType> methodArgs, TypePrinterFlags flags) where T : ITypeOutput {
			new TypePrinter(output, flags).Write(ts, typeArgs, methodArgs);
			return output;
		}

		public string ToString(TypePrinterFlags flags) {
			return Write(new StringBuilderTypeOutput(), flags).ToString();
		}

		public override string ToString() {
			return ToString(TypePrinterFlags.Default);
		}
	}
}
