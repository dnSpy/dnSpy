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
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Interpreter {
	/// <summary>
	/// Class implemented by the debugger. It provides access to the debugged process' locals,
	/// arguments, allows calling methods etc.
	/// </summary>
	public abstract class DebuggerRuntime {
		/// <summary>
		/// Gets the size of a pointer in bytes
		/// </summary>
		public abstract int PointerSize { get; }

		/// <summary>
		/// Called before executing the method
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="body">Method body</param>
		public abstract void Initialize(DmdMethodBase method, DmdMethodBody body);

		/// <summary>
		/// Gets an argument value or returns null on failure
		/// </summary>
		/// <param name="index">Argument index</param>
		/// <returns></returns>
		public abstract ILValue? LoadArgument(int index);

		/// <summary>
		/// Gets a local value or returns null on failure
		/// </summary>
		/// <param name="index">Local index</param>
		/// <returns></returns>
		public abstract ILValue? LoadLocal(int index);

		/// <summary>
		/// Gets the address of an argument or returns null on failure
		/// </summary>
		/// <param name="index">Argument index</param>
		/// <param name="type">Type of the argument</param>
		/// <returns></returns>
		public abstract ILValue? LoadArgumentAddress(int index, DmdType type);

		/// <summary>
		/// Gets the address of a local or returns null on failure
		/// </summary>
		/// <param name="index">Local index</param>
		/// <param name="type">Type of the local</param>
		/// <returns></returns>
		public abstract ILValue? LoadLocalAddress(int index, DmdType type);

		/// <summary>
		/// Writes to an argument or returns false on failure
		/// </summary>
		/// <param name="index">Argument index</param>
		/// <param name="type">Type of the argument</param>
		/// <param name="value">New value</param>
		public abstract bool StoreArgument(int index, DmdType type, ILValue value);

		/// <summary>
		/// Writes to a local or returns false on failure
		/// </summary>
		/// <param name="index">Local index</param>
		/// <param name="type">Type of the local</param>
		/// <param name="value">New value</param>
		public abstract bool StoreLocal(int index, DmdType type, ILValue value);

		/// <summary>
		/// Creates an SZ array or returns null on failure
		/// </summary>
		/// <param name="elementType">Element type</param>
		/// <param name="length">Number of elements</param>
		/// <returns></returns>
		public abstract ILValue? CreateSZArray(DmdType elementType, long length);

		/// <summary>
		/// Creates a <see cref="RuntimeTypeHandle"/> value or returns null on failure
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public abstract ILValue? CreateRuntimeTypeHandle(DmdType type);

		/// <summary>
		/// Creates a <see cref="RuntimeFieldHandle"/> value or returns null on failure
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		public abstract ILValue? CreateRuntimeFieldHandle(DmdFieldInfo field);

		/// <summary>
		/// Creates a <see cref="RuntimeMethodHandle"/> value or returns null on failure
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		public abstract ILValue? CreateRuntimeMethodHandle(DmdMethodBase method);

		/// <summary>
		/// Creates a type without calling its constructor or returns null on failure. All fields are initialized to 0 or null depending on field type
		/// </summary>
		/// <param name="type">Type to create</param>
		/// <returns></returns>
		public abstract ILValue? CreateTypeNoConstructor(DmdType type);

		/// <summary>
		/// Boxes a value or returns null on failure
		/// </summary>
		/// <param name="value">Value to box</param>
		/// <param name="type">Target type</param>
		/// <returns></returns>
		public abstract ILValue? Box(ILValue value, DmdType type);

		/// <summary>
		/// Calls a static method or returns false on failure
		/// </summary>
		/// <param name="method">Method to call</param>
		/// <param name="arguments">Method arguments</param>
		/// <param name="returnValue">Return value. It's ignored if the method returns <see cref="void"/></param>
		/// <returns></returns>
		public abstract bool CallStatic(DmdMethodBase method, ILValue[] arguments, out ILValue? returnValue);

		/// <summary>
		/// Creates a new instance and calls its constructor or returns null on failure. The constructor could be a CLR-generated array constructor
		/// </summary>
		/// <param name="ctor">Constructor</param>
		/// <param name="arguments">Constructor arguments</param>
		/// <returns></returns>
		public abstract ILValue? CreateInstance(DmdConstructorInfo ctor, ILValue[] arguments);

		/// <summary>
		/// Calls a static method or returns false on failure
		/// </summary>
		/// <param name="methodAddress">Method address</param>
		/// <param name="methodSig">Method signature</param>
		/// <param name="arguments">Method arguments</param>
		/// <param name="returnValue">Return value. It's ignored if the method returns <see cref="void"/></param>
		/// <returns></returns>
		public abstract bool CallStaticIndirect(DmdMethodSignature methodSig, ILValue methodAddress, ILValue[] arguments, out ILValue? returnValue);

		/// <summary>
		/// Returns the value of a static field or returns null on failure
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		public abstract ILValue? LoadStaticField(DmdFieldInfo field);

		/// <summary>
		/// Returns the address of a static field or returns null on failure
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		public abstract ILValue? LoadStaticFieldAddress(DmdFieldInfo field);

		/// <summary>
		/// Stores a value in a static field or returns false on failure
		/// </summary>
		/// <param name="field">Field</param>
		/// <param name="value">Value to store in the field</param>
		public abstract bool StoreStaticField(DmdFieldInfo field, ILValue value);

		/// <summary>
		/// Returns a new string value
		/// </summary>
		/// <param name="type">String type</param>
		/// <param name="value">String value. This is never null.</param>
		/// <returns></returns>
		public abstract ILValue LoadString(DmdType type, string value);

		/// <summary>
		/// Compares <paramref name="left"/> and <paramref name="right"/>, returning less than 0, 0 or greater than 0.
		/// This method is called if one of the inputs is a non-constant native int or by-ref.
		/// </summary>
		/// <param name="left">Left operand</param>
		/// <param name="right">Right operand</param>
		/// <returns></returns>
		public abstract int? CompareSigned(ILValue left, ILValue right);

		/// <summary>
		/// Compares <paramref name="left"/> and <paramref name="right"/>, returning less than 0, 0 or greater than 0.
		/// This method is called if one of the inputs is a non-constant native int or by-ref.
		/// </summary>
		/// <param name="left">Left operand</param>
		/// <param name="right">Right operand</param>
		/// <returns></returns>
		public abstract int? CompareUnsigned(ILValue left, ILValue right);

		/// <summary>
		/// Checks if <paramref name="left"/> equals <paramref name="right"/> or returns null on failure
		/// </summary>
		/// <param name="left">Left operand</param>
		/// <param name="right">Right operand</param>
		/// <returns></returns>
		public abstract bool? Equals(ILValue left, ILValue right);

		/// <summary>
		/// Gets the size of a value type
		/// </summary>
		/// <param name="type">Value type</param>
		/// <returns></returns>
		public abstract int GetSizeOfValueType(DmdType type);
	}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public enum LoadValueType {
		I,
		I1,
		I2,
		I4,
		I8,
		R4,
		R8,
		Ref,
		U1,
		U2,
		U4,
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
