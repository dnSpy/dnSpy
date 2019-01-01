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

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Result of evaluating an expression
	/// </summary>
	public abstract class DbgValue : DbgObject {
		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Runtime.Process;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

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

		/// <summary>
		/// Closes this instance
		/// </summary>
		public abstract void Close();
	}

	/// <summary>
	/// Type of value
	/// </summary>
	public enum DbgSimpleValueType {
		/// <summary>
		/// Some other type
		/// </summary>
		Other,

		/// <summary>
		/// There's no value
		/// </summary>
		Void,

		/// <summary>
		/// Boolean, <see cref="DbgValue.RawValue"/> is a boxed <see cref="bool"/>
		/// </summary>
		Boolean,

		/// <summary>
		/// 1-byte char, <see cref="DbgValue.RawValue"/> is a boxed <see cref="byte"/>
		/// </summary>
		Char1,

		/// <summary>
		/// Char, <see cref="DbgValue.RawValue"/> is a boxed <see cref="char"/>
		/// </summary>
		CharUtf16,

		/// <summary>
		/// 8-bit signed int, <see cref="DbgValue.RawValue"/> is a boxed <see cref="sbyte"/>
		/// </summary>
		Int8,

		/// <summary>
		/// 16-bit signed int, <see cref="DbgValue.RawValue"/> is a boxed <see cref="short"/>
		/// </summary>
		Int16,

		/// <summary>
		/// 32-bit signed int, <see cref="DbgValue.RawValue"/> is a boxed <see cref="int"/>
		/// </summary>
		Int32,

		/// <summary>
		/// 64-bit signed int, <see cref="DbgValue.RawValue"/> is a boxed <see cref="long"/>
		/// </summary>
		Int64,

		/// <summary>
		/// 8-bit unsigned int, <see cref="DbgValue.RawValue"/> is a boxed <see cref="byte"/>
		/// </summary>
		UInt8,

		/// <summary>
		/// 16-bit unsigned int, <see cref="DbgValue.RawValue"/> is a boxed <see cref="ushort"/>
		/// </summary>
		UInt16,

		/// <summary>
		/// 32-bit unsigned int, <see cref="DbgValue.RawValue"/> is a boxed <see cref="uint"/>
		/// </summary>
		UInt32,

		/// <summary>
		/// 64-bit unsigned int, <see cref="DbgValue.RawValue"/> is a boxed <see cref="ulong"/>
		/// </summary>
		UInt64,

		/// <summary>
		/// 32-bit floating point number, <see cref="DbgValue.RawValue"/> is a boxed <see cref="float"/>
		/// </summary>
		Float32,

		/// <summary>
		/// 64-bit floating point number, <see cref="DbgValue.RawValue"/> is a boxed <see cref="double"/>
		/// </summary>
		Float64,

		/// <summary>
		/// Decimal, <see cref="DbgValue.RawValue"/> is a boxed <see cref="decimal"/>
		/// </summary>
		Decimal,

		/// <summary>
		/// 32-bit pointer, <see cref="DbgValue.RawValue"/> is a boxed <see cref="uint"/>
		/// </summary>
		Ptr32,

		/// <summary>
		/// 64-bit pointer, <see cref="DbgValue.RawValue"/> is a boxed <see cref="ulong"/>
		/// </summary>
		Ptr64,

		/// <summary>
		/// UTF-16 string, <see cref="DbgValue.RawValue"/> is a <see cref="string"/> or null
		/// </summary>
		StringUtf16,

		/// <summary>
		/// A <see cref="System.DateTime"/>, <see cref="DbgValue.RawValue"/> is a boxed <see cref="System.DateTime"/>
		/// </summary>
		DateTime,
	}

	/// <summary>
	/// Contains the address and length of a value
	/// </summary>
	public readonly struct DbgRawAddressValue {
		/// <summary>
		/// Gets the address
		/// </summary>
		public ulong Address { get; }

		/// <summary>
		/// Gets the length
		/// </summary>
		public ulong Length { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="address">Address</param>
		/// <param name="length">Length</param>
		public DbgRawAddressValue(ulong address, ulong length) {
			Address = address;
			Length = length;
		}
	}
}
