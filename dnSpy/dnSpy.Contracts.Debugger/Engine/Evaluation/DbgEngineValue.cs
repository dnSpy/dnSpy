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

using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Result of evaluating an expression
	/// </summary>
	public abstract class DbgEngineValue : DbgObject {
		/// <summary>
		/// Gets the value object created by the debug engine
		/// </summary>
		public abstract object InternalValue { get; }

		/// <summary>
		/// Type of the value
		/// </summary>
		public abstract DbgSimpleValueType ValueType { get; }

		/// <summary>
		/// true if <see cref="RawValue"/> is valid
		/// </summary>
		public abstract bool HasRawValue { get; }

		/// <summary>
		/// The value. It's only valid if <see cref="HasRawValue"/> is true. A null value is a valid value.
		/// If it's an enum value, it's stored as the enum's underlying type (eg. <see cref="int"/>)
		/// </summary>
		public abstract object RawValue { get; }

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
	}
}
