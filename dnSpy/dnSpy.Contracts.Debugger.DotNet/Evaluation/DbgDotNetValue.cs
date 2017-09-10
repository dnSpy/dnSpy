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
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation {
	/// <summary>
	/// Result of evaluating an expression. All values are automatically closed when the runtime continues
	/// but they implement <see cref="IDisposable"/> and should be disposed of earlier if possible.
	/// </summary>
	public abstract class DbgDotNetValue : IDisposable {
		/// <summary>
		/// Gets the type of the value
		/// </summary>
		public abstract DmdType Type { get; }

		/// <summary>
		/// true if this value references another value
		/// </summary>
		public abstract bool IsReference { get; }

		/// <summary>
		/// true if this is a null reference. It's only valid if <see cref="IsReference"/> is true
		/// </summary>
		public abstract bool IsNullReference { get; }

		/// <summary>
		/// Gets the address of the reference or null if it's unknown or if it's not a reference (<see cref="IsReference"/>)
		/// </summary>
		/// <returns></returns>
		public abstract ulong? GetReferenceAddress();

		/// <summary>
		/// Gets the referenced value if it's a reference (<see cref="IsReference"/>) or null if it's not a reference or if it's a null reference.
		/// </summary>
		/// <returns></returns>
		public abstract DbgDotNetValue Dereference();

		/// <summary>
		/// true if this is a boxed value type
		/// </summary>
		public abstract bool IsBox { get; }

		/// <summary>
		/// Gets the unboxed value if it's a boxed value (<see cref="IsBox"/>) or null
		/// </summary>
		/// <returns></returns>
		public abstract DbgDotNetValue Unbox();

		/// <summary>
		/// true if this is an array value
		/// </summary>
		public abstract bool IsArray { get; }

		/// <summary>
		/// Gets the number of elements of the array (<see cref="IsArray"/>)
		/// </summary>
		/// <param name="elementCount">Total number of elements in the array</param>
		/// <returns></returns>
		public abstract bool GetArrayCount(out uint elementCount);

		/// <summary>
		/// Gets array information if it's an array (<see cref="IsArray"/>) or returns false
		/// </summary>
		/// <param name="elementCount">Total number of elements in the array</param>
		/// <param name="dimensionInfos">Dimension base indexes and lengths</param>
		/// <returns></returns>
		public abstract bool GetArrayInfo(out uint elementCount, out DbgDotNetArrayDimensionInfo[] dimensionInfos);

		/// <summary>
		/// Gets the element at <paramref name="index"/> in the array. This method can be called even if it's
		/// a multi-dimensional array.
		/// </summary>
		/// <param name="index">Index of the element</param>
		/// <returns></returns>
		public abstract DbgDotNetValue GetArrayElementAt(uint index);

		/// <summary>
		/// Gets the address of the value or null if there's no address available.
		/// The returned address gets invalid when the runtime continues.
		/// </summary>
		/// <param name="onlyDataAddress">If true and if it's a supported type (eg. a simple type such as integers,
		/// floating point values, strings or byte arrays) the returned object contains the address of the actual
		/// value, else the returned address and length covers the whole object including vtable, method table or other
		/// special data.</param>
		/// <returns></returns>
		public abstract DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress);

		/// <summary>
		/// Gets the raw value
		/// </summary>
		/// <returns></returns>
		public abstract DbgDotNetRawValue GetRawValue();

		/// <summary>
		/// Called when its owner (<see cref="DbgEngineValue"/>) gets closed
		/// </summary>
		public abstract void Dispose();
	}

	/// <summary>
	/// Contains base index and length of an array dimension
	/// </summary>
	public struct DbgDotNetArrayDimensionInfo {
		/// <summary>
		/// Base index
		/// </summary>
		public int BaseIndex { get; }

		/// <summary>
		/// Number of elements in this dimension
		/// </summary>
		public uint Length { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="baseIndex">Base index</param>
		/// <param name="length">Number of elements in this dimension</param>
		public DbgDotNetArrayDimensionInfo(int baseIndex, uint length) {
			BaseIndex = baseIndex;
			Length = length;
		}
	}

	/// <summary>
	/// Raw value
	/// </summary>
	public struct DbgDotNetRawValue {
		/// <summary>
		/// Type of the value
		/// </summary>
		public DbgSimpleValueType ValueType { get; }

		/// <summary>
		/// true if <see cref="RawValue"/> is valid
		/// </summary>
		public bool HasRawValue { get; }

		/// <summary>
		/// The value. It's only valid if <see cref="HasRawValue"/> is true. A null value is a valid value.
		/// If it's an enum value, it's stored as the enum's underlying type (eg. <see cref="int"/>)
		/// </summary>
		public object RawValue { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="valueType">Type</param>
		public DbgDotNetRawValue(DbgSimpleValueType valueType) {
			Debug.Assert(valueType == DbgSimpleValueType.OtherReferenceType || valueType == DbgSimpleValueType.OtherValueType || valueType == DbgSimpleValueType.Void);
			ValueType = valueType;
			HasRawValue = false;
			RawValue = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="valueType">Type</param>
		/// <param name="rawValue">Value</param>
		public DbgDotNetRawValue(DbgSimpleValueType valueType, object rawValue) {
			Debug.Assert(valueType != DbgSimpleValueType.Void);
			ValueType = valueType;
			HasRawValue = true;
			RawValue = rawValue;
		}
	}
}
