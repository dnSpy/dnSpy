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
	/// Result of evaluating an expression
	/// </summary>
	public abstract class DbgDotNetValue : IDisposable {
		/// <summary>
		/// Gets the type of the value
		/// </summary>
		public abstract DmdType Type { get; }

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
