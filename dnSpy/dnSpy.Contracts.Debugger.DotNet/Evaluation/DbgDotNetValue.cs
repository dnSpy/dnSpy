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
		/// true if this is a null value
		/// </summary>
		public virtual bool IsNull => false;

		/// <summary>
		/// Gets the referenced value if it's a by-ref or a pointer
		/// </summary>
		/// <returns></returns>
		public virtual DbgDotNetValueResult LoadIndirect() =>
			DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);

		/// <summary>
		/// Writes to the referenced value (by-ref or pointer). The return value is null or an error message.
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="value">Value to store: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <returns></returns>
		public virtual string? StoreIndirect(DbgEvaluationInfo evalInfo, object? value) =>
			PredefinedEvaluationErrorMessages.InternalDebuggerError;

		/// <summary>
		/// Gets the number of elements of the array
		/// </summary>
		/// <param name="elementCount">Total number of elements in the array</param>
		/// <returns></returns>
		public virtual bool GetArrayCount(out uint elementCount) {
			elementCount = 0;
			return false;
		}

		/// <summary>
		/// Gets array information if it's an array or returns false
		/// </summary>
		/// <param name="elementCount">Total number of elements in the array</param>
		/// <param name="dimensionInfos">Dimension base indexes and lengths</param>
		/// <returns></returns>
		public virtual bool GetArrayInfo(out uint elementCount, [NotNullWhen(true)] out DbgDotNetArrayDimensionInfo[]? dimensionInfos) {
			elementCount = 0;
			dimensionInfos = null;
			return false;
		}

		/// <summary>
		/// Gets the address of the element at <paramref name="index"/> in the array or null if it's not supported.
		/// This method can be called even if it's a multi-dimensional array.
		/// </summary>
		/// <param name="index">Zero-based index of the element</param>
		/// <returns></returns>
		public virtual DbgDotNetValueResult? GetArrayElementAddressAt(uint index) => null;

		/// <summary>
		/// Gets the element at <paramref name="index"/> in the array. This method can be called even if it's
		/// a multi-dimensional array.
		/// </summary>
		/// <param name="index">Zero-based index of the element</param>
		/// <returns></returns>
		public virtual DbgDotNetValueResult GetArrayElementAt(uint index) =>
			DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);

		/// <summary>
		/// Stores a value at <paramref name="index"/> in the array. This method can be called even if it's
		/// a multi-dimensional array.
		/// The return value is null or an error message.
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="index">Zero-based index of the element</param>
		/// <param name="value">Value to store: A <see cref="DbgDotNetValue"/> or a primitive number or a string or arrays of primitive numbers / strings</param>
		/// <returns></returns>
		public virtual string? SetArrayElementAt(DbgEvaluationInfo evalInfo, uint index, object? value) =>
			PredefinedEvaluationErrorMessages.InternalDebuggerError;

		/// <summary>
		/// Boxes the value type, returns null on failure
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <returns></returns>
		public virtual DbgDotNetValueResult? Box(DbgEvaluationInfo evalInfo) => null;

		/// <summary>
		/// Gets the address of the value or null if there's no address available.
		/// The returned address gets invalid when the runtime continues.
		/// </summary>
		/// <param name="onlyDataAddress">If true and if it's a supported type (eg. a simple type such as integers,
		/// floating point values, strings or byte arrays) the returned object contains the address of the actual
		/// value, else the returned address and length covers the whole object including vtable, method table or other
		/// special data.</param>
		/// <returns></returns>
		public virtual DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress) => null;

		/// <summary>
		/// Gets the raw value
		/// </summary>
		/// <returns></returns>
		public virtual DbgDotNetRawValue GetRawValue() => new DbgDotNetRawValue(DbgSimpleValueType.Other);

		/// <summary>
		/// Returns the <see cref="IDbgDotNetRuntime"/> instance or null if it's unknown
		/// </summary>
		/// <returns></returns>
		public virtual IDbgDotNetRuntime? TryGetDotNetRuntime() => null;

		/// <summary>
		/// Called when its owner (<see cref="DbgEngineValue"/>) gets closed
		/// </summary>
		public virtual void Dispose() { }
	}

	/// <summary>
	/// Contains base index and length of an array dimension
	/// </summary>
	public readonly struct DbgDotNetArrayDimensionInfo {
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
	public readonly struct DbgDotNetRawValue {
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
		public object? RawValue { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="valueType">Type</param>
		public DbgDotNetRawValue(DbgSimpleValueType valueType) {
			Debug.Assert(valueType == DbgSimpleValueType.Other || valueType == DbgSimpleValueType.Void);
			ValueType = valueType;
			HasRawValue = false;
			RawValue = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="valueType">Type</param>
		/// <param name="rawValue">Value</param>
		public DbgDotNetRawValue(DbgSimpleValueType valueType, object? rawValue) {
			Debug.Assert(valueType != DbgSimpleValueType.Void);
			ValueType = valueType;
			HasRawValue = true;
			RawValue = rawValue;
		}
	}
}
