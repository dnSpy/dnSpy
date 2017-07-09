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
		/// Gets an argument value or returns null on failure
		/// </summary>
		/// <param name="index">Argument index</param>
		/// <returns></returns>
		public abstract ILValue GetArgument(int index);

		/// <summary>
		/// Gets a local value or returns null on failure
		/// </summary>
		/// <param name="index">Local index</param>
		/// <returns></returns>
		public abstract ILValue GetLocal(int index);

		/// <summary>
		/// Gets the address of an argument or returns null on failure
		/// </summary>
		/// <param name="index">Argument index</param>
		/// <returns></returns>
		public abstract ILValue GetArgumentAddress(int index);

		/// <summary>
		/// Gets the address of a local or returns null on failure
		/// </summary>
		/// <param name="index">Local index</param>
		/// <returns></returns>
		public abstract ILValue GetLocalAddress(int index);

		/// <summary>
		/// Writes to an argument or returns false on failure
		/// </summary>
		/// <param name="index">Argument index</param>
		/// <param name="value">New value</param>
		public abstract bool SetArgument(int index, ILValue value);

		/// <summary>
		/// Writes to a local or returns false on failure
		/// </summary>
		/// <param name="index">Local index</param>
		/// <param name="value">New value</param>
		public abstract bool SetLocal(int index, ILValue value);

		/// <summary>
		/// Creates an SZ array or returns null on failure
		/// </summary>
		/// <param name="elementType">Element type</param>
		/// <param name="length">Number of elements</param>
		/// <returns></returns>
		public abstract ILValue CreateSZArray(DmdType elementType, long length);

		/// <summary>
		/// Gets the value of an element in an SZ array or returns null on failure
		/// </summary>
		/// <param name="arrayValue">Array</param>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public abstract ILValue GetSZArrayElement(ILValue arrayValue, long index);

		/// <summary>
		/// Gets the address of an element in an SZ array or returns null on failure
		/// </summary>
		/// <param name="arrayValue">Array</param>
		/// <param name="index">Index</param>
		/// <returns></returns>
		public abstract ILValue GetSZArrayElementAddress(ILValue arrayValue, long index);

		/// <summary>
		/// Sets the value of an element in an SZ array or returns false on failure
		/// </summary>
		/// <param name="arrayValue">Array</param>
		/// <param name="index">Index</param>
		/// <param name="elementValue">New value</param>
		/// <returns></returns>
		public abstract bool SetSZArrayElement(ILValue arrayValue, long index, ILValue elementValue);

		/// <summary>
		/// Returns the length of an SZ array. Returns false if the input is not an array
		/// </summary>
		/// <param name="value">The value, most likely an array value</param>
		/// <param name="length">Updated with the length of the array</param>
		/// <returns></returns>
		public abstract bool GetSZArrayLength(ILValue value, out long length);

		/// <summary>
		/// Creates a <see cref="RuntimeTypeHandle"/> value or returns null on failure
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public abstract ILValue CreateRuntimeTypeHandle(DmdType type);

		/// <summary>
		/// Creates a <see cref="RuntimeFieldHandle"/> value or returns null on failure
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		public abstract ILValue CreateRuntimeFieldHandle(DmdFieldInfo field);

		/// <summary>
		/// Creates a <see cref="RuntimeMethodHandle"/> value or returns null on failure
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		public abstract ILValue CreateRuntimeMethodHandle(DmdMethodBase method);

		/// <summary>
		/// Creates a type without calling its constructor or returns null on failure. All fields are initialized to 0 or null depending on field type
		/// </summary>
		/// <param name="type">Type to create</param>
		/// <returns></returns>
		public abstract ILValue CreateTypeNoConstructor(DmdType type);

		/// <summary>
		/// Calls a method/constructor or returns false on failure. The method could be a CLR-generated method, eg. an array Address() method, see <see cref="DmdSpecialMethodKind"/>.
		/// </summary>
		/// <param name="isVirtual">true if it's a virtual call, false if it's a normal call</param>
		/// <param name="method">Method to call</param>
		/// <param name="obj">'this' pointer or null if it's a static method</param>
		/// <param name="parameters">Method arguments</param>
		/// <param name="returnValue">Return value. It's ignored if the method returns <see cref="void"/></param>
		/// <returns></returns>
		public abstract bool Call(bool isVirtual, DmdMethodBase method, ILValue obj, ILValue[] parameters, out ILValue returnValue);

		/// <summary>
		/// Calls a method or returns false on failure
		/// </summary>
		/// <param name="methodAddress">Method address</param>
		/// <param name="methodSig">Method signature</param>
		/// <param name="obj">'this' pointer or null if it's a static method</param>
		/// <param name="parameters">Method arguments</param>
		/// <param name="returnValue">Return value. It's ignored if the method returns <see cref="void"/></param>
		/// <returns></returns>
		public abstract bool CallIndirect(DmdMethodSignature methodSig, ILValue methodAddress, ILValue obj, ILValue[] parameters, out ILValue returnValue);

		/// <summary>
		/// Returns the value of a field or returns null on failure
		/// </summary>
		/// <param name="field">Field</param>
		/// <param name="obj">'this' pointer or null if it's a static field</param>
		/// <returns></returns>
		public abstract ILValue GetField(DmdFieldInfo field, ILValue obj);

		/// <summary>
		/// Returns the address of a field or returns null on failure
		/// </summary>
		/// <param name="field">Field</param>
		/// <param name="obj">'this' pointer or null if it's a static field</param>
		/// <returns></returns>
		public abstract ILValue GetFieldAddress(DmdFieldInfo field, ILValue obj);

		/// <summary>
		/// Stores a value to a field or returns false on failure
		/// </summary>
		/// <param name="field">Field</param>
		/// <param name="obj">'this' pointer or null if it's a static field</param>
		/// <param name="value">Value to store in the field</param>
		public abstract bool SetField(DmdFieldInfo field, ILValue obj, ILValue value);

		/// <summary>
		/// Reads a (managed or unmanaged) pointer or returns null on failure
		/// </summary>
		/// <param name="pointerType">Pointer type</param>
		/// <param name="address">Address</param>
		/// <returns></returns>
		public abstract ILValue ReadPointer(PointerOpCodeType pointerType, ILValue address);

		/// <summary>
		/// Writes a (managed or unmanaged) pointer or returns false on failure
		/// </summary>
		/// <param name="pointerType">Pointer type</param>
		/// <param name="address">Address</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool WritePointer(PointerOpCodeType pointerType, ILValue address, ILValue value);

		/// <summary>
		/// Loads a type (ldobj instruction) or returns null on failure
		/// </summary>
		/// <param name="address">Address of type object</param>
		/// <param name="type">Type to load</param>
		/// <returns></returns>
		public abstract ILValue LoadTypeObject(ILValue address, DmdType type);

		/// <summary>
		/// Writes a type (stobj instruction) or returns false on failure
		/// </summary>
		/// <param name="address">Address of type object</param>
		/// <param name="type">Type to store</param>
		/// <param name="value">New value</param>
		/// <returns></returns>
		public abstract bool StoreTypeObject(ILValue address, DmdType type, ILValue value);

		/// <summary>
		/// Boxes a value type (inluding a nullable value type) or returns null on failure
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="type">Boxed type</param>
		/// <returns></returns>
		public abstract ILValue Box(ILValue value, DmdType type);

		/// <summary>
		/// Unboxes a boxed value type or returns null on failure
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="type">Unboxed type</param>
		/// <returns></returns>
		public abstract ILValue UnboxAny(ILValue value, DmdType type);

		/// <summary>
		/// Calculates <paramref name="left"/> + <paramref name="right"/>. This method is called if
		/// one of the inputs is a non-constant native int or by-ref.
		/// </summary>
		/// <param name="left">Left operand</param>
		/// <param name="right">Right operand</param>
		/// <returns></returns>
		public abstract ILValue BinaryAdd(ILValue left, ILValue right);

		/// <summary>
		/// Calculates <paramref name="left"/> - <paramref name="right"/>. This method is called if
		/// one of the inputs is a non-constant native int or by-ref.
		/// </summary>
		/// <param name="left">Left operand</param>
		/// <param name="right">Right operand</param>
		/// <returns></returns>
		public abstract ILValue BinarySub(ILValue left, ILValue right);
	}

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
	public enum PointerOpCodeType {
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
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
}
