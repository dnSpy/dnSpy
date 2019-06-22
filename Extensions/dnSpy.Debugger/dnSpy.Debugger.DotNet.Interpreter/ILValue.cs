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
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Interpreter {
	/// <summary>
	/// IL stack value kind
	/// </summary>
	public enum ILValueKind {
		/// <summary>
		/// 32-bit integer. 1-byte and 2-byte integers are sign/zero extended to 32 bits. Booleans and chars are zero extended.
		/// </summary>
		Int32,

		/// <summary>
		/// 64-bit integer
		/// </summary>
		Int64,

		/// <summary>
		/// 64-bit float (32-bit floats are extended to 64-bit floats)
		/// </summary>
		Float,

		/// <summary>
		/// Unmanaged pointer or native int
		/// </summary>
		NativeInt,

		/// <summary>
		/// Managed pointer
		/// </summary>
		ByRef,

		/// <summary>
		/// Any other reference type or value type
		/// </summary>
		Type,
	}

	/// <summary>
	/// A value that can be stored on the IL stack
	/// </summary>
	public abstract class ILValue {
		/// <summary>
		/// Gets the stack value kind
		/// </summary>
		public abstract ILValueKind Kind { get; }

		/// <summary>
		/// true if this is a null value
		/// </summary>
		public virtual bool IsNull => false;

		/// <summary>
		/// Makes a copy of this instance so the new instance can be pushed onto the stack. The default implementation
		/// returns itself. Only mutable value types need to override this method.
		/// </summary>
		/// <returns></returns>
		public virtual ILValue Clone() => this;

		/// <summary>
		/// Gets the type of the value or null if it's unknown, eg. it's a null reference
		/// </summary>
		public abstract DmdType? Type { get; }

		/// <summary>
		/// Loads an instance field. Returns null if it's not supported.
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		public virtual ILValue? LoadField(DmdFieldInfo field) => null;

		/// <summary>
		/// Stores a value in an instance field. Returns false if it's not supported.
		/// </summary>
		/// <param name="field">Field</param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public virtual bool StoreField(DmdFieldInfo field, ILValue value) => false;

		/// <summary>
		/// Returns the address of an instance field. Returns null if it's not supported.
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		public virtual ILValue? LoadFieldAddress(DmdFieldInfo field) => null;

		/// <summary>
		/// Calls an instance method. The method could be a CLR-generated method, eg. an array Address() method, see <see cref="DmdSpecialMethodKind"/>.
		/// Returns false if it's not supported.
		/// </summary>
		/// <param name="isCallvirt">true if this is a virtual call, false if it's a non-virtual call</param>
		/// <param name="method">Method</param>
		/// <param name="arguments">Arguments. The hidden 'this' value isn't included, it's this instance.</param>
		/// <param name="returnValue">Updated with the return value. Can be null if the return type is <see cref="void"/></param>
		/// <returns></returns>
		public virtual bool Call(bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue? returnValue) {
			returnValue = null;
			return false;
		}

		/// <summary>
		/// Calls an instance method or returns false on failure
		/// </summary>
		/// <param name="methodAddress">Method address</param>
		/// <param name="methodSig">Method signature</param>
		/// <param name="arguments">Method arguments</param>
		/// <param name="returnValue">Return value. It's ignored if the method returns <see cref="void"/></param>
		/// <returns></returns>
		public virtual bool CallIndirect(DmdMethodSignature methodSig, ILValue methodAddress, ILValue[] arguments, out ILValue? returnValue) {
			returnValue = null;
			return false;
		}

		/// <summary>
		/// Boxes this instance. Returns null if it's not supported.
		/// </summary>
		/// <param name="type">Target type</param>
		/// <returns></returns>
		public virtual ILValue? Box(DmdType type) => null;

		/// <summary>
		/// Unboxes this instance. Returns null if it's not supported.
		/// </summary>
		/// <param name="type">Target type</param>
		/// <returns></returns>
		public virtual ILValue? UnboxAny(DmdType type) => null;

		/// <summary>
		/// Unboxes this instance. Returns null if it's not supported.
		/// </summary>
		/// <param name="type">Target type</param>
		/// <returns></returns>
		public virtual ILValue? Unbox(DmdType type) => null;

		/// <summary>
		/// Loads an SZ array element. Returns null if it's not supported.
		/// </summary>
		/// <param name="loadValueType">Type of value to load</param>
		/// <param name="index">Array index</param>
		/// <param name="elementType">Optional element type (eg. it's the ldelem instruction)</param>
		/// <returns></returns>
		public virtual ILValue? LoadSZArrayElement(LoadValueType loadValueType, long index, DmdType elementType) => null;

		/// <summary>
		/// Writes an SZ array element. Returns false if it's not supported.
		/// </summary>
		/// <param name="loadValueType">Type of value to store</param>
		/// <param name="index">Index</param>
		/// <param name="value">Value</param>
		/// <param name="elementType">Optional element type (eg. it's the stelem instruction)</param>
		/// <returns></returns>
		public virtual bool StoreSZArrayElement(LoadValueType loadValueType, long index, ILValue value, DmdType elementType) => false;

		/// <summary>
		/// Loads the address of an SZ array element. Returns null if it's not supported.
		/// </summary>
		/// <param name="index">Index</param>
		/// <param name="elementType">Element type</param>
		/// <returns></returns>
		public virtual ILValue? LoadSZArrayElementAddress(long index, DmdType elementType) => null;

		/// <summary>
		/// Gets the length of an SZ array. Returns false if it's not supported.
		/// </summary>
		/// <param name="length">Updated with the length of the array</param>
		/// <returns></returns>
		public virtual bool GetSZArrayLength(out long length) {
			length = 0;
			return false;
		}

		/// <summary>
		/// Loads a value. Returns null if it's not supported.
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="loadValueType">Type of value to load</param>
		/// <returns></returns>
		public virtual ILValue? LoadIndirect(DmdType type, LoadValueType loadValueType) => null;

		/// <summary>
		/// Stores a value. Returns false if it's not supported.
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="loadValueType">Type of value to store</param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public virtual bool StoreIndirect(DmdType type, LoadValueType loadValueType, ILValue value) => false;

		/// <summary>
		/// Clears the memory. Returns false if it's not supported.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public virtual bool InitializeObject(DmdType type) => false;

		/// <summary>
		/// Copies <paramref name="source"/> to this value. Returns false if it's not supported.
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="source">Source value</param>
		/// <returns></returns>
		public virtual bool CopyObject(DmdType type, ILValue source) => false;

		/// <summary>
		/// Initializes memory. Returns false if it's not supported.
		/// </summary>
		/// <param name="value">Value to write</param>
		/// <param name="size">Size of data</param>
		/// <returns></returns>
		public virtual bool InitializeMemory(byte value, long size) => false;

		/// <summary>
		/// Copies memory to this value. Returns false if it's not supported.
		/// </summary>
		/// <param name="source">Source value</param>
		/// <param name="size">Size in bytes</param>
		/// <returns></returns>
		public virtual bool CopyMemory(ILValue source, long size) => false;

		/// <summary>
		/// Adds a constant to a copy of this value and returns the result. Returns null if it's not supported.
		/// </summary>
		/// <param name="kind">Opcode kind</param>
		/// <param name="value">Value to add</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public virtual ILValue? Add(AddOpCodeKind kind, long value, int pointerSize) => null;

		/// <summary>
		/// Subtracts a constant from a copy of this value and returns the result. Returns null if it's not supported.
		/// </summary>
		/// <param name="kind">Opcode kind</param>
		/// <param name="value">Value to subtract</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public virtual ILValue? Sub(SubOpCodeKind kind, long value, int pointerSize) => null;

		/// <summary>
		/// Subtracts <paramref name="value"/> from a copy of this value and returns the result. Returns null if it's not supported.
		/// </summary>
		/// <param name="kind">Opcode kind</param>
		/// <param name="value">Value to subtract</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public virtual ILValue? Sub(SubOpCodeKind kind, ILValue value, int pointerSize) => null;

		/// <summary>
		/// Converts this value to a new value. Returns null if it's not supported.
		/// </summary>
		/// <param name="kind">Opcode kind</param>
		/// <returns></returns>
		public virtual ILValue? Conv(ConvOpCodeKind kind) => null;
	}

	/// <summary>
	/// Add opcode kind
	/// </summary>
	public enum AddOpCodeKind {
		/// <summary>
		/// Normal addition
		/// </summary>
		Add,

		/// <summary>
		/// Signed addition with an overflow check
		/// </summary>
		Add_Ovf,

		/// <summary>
		/// Unsigned addition with an overflow check
		/// </summary>
		Add_Ovf_Un,
	}

	/// <summary>
	/// Sub opcode kind
	/// </summary>
	public enum SubOpCodeKind {
		/// <summary>
		/// Normal subtraction
		/// </summary>
		Sub,

		/// <summary>
		/// Signed subtraction with an overflow check
		/// </summary>
		Sub_Ovf,

		/// <summary>
		/// Unsigned subtraction with an overflow check
		/// </summary>
		Sub_Ovf_Un,
	}

	/// <summary>
	/// Convert opcode kind
	/// </summary>
	public enum ConvOpCodeKind {
		/// <summary>
		/// Convert to a <see cref="IntPtr"/>
		/// </summary>
		Conv_I,

		/// <summary>
		/// Convert to a <see cref="IntPtr"/>, signed, overflow check
		/// </summary>
		Conv_Ovf_I,

		/// <summary>
		/// Convert to a <see cref="IntPtr"/>, unsigned, overflow check
		/// </summary>
		Conv_Ovf_I_Un,

		/// <summary>
		/// Convert to a <see cref="UIntPtr"/>
		/// </summary>
		Conv_U,

		/// <summary>
		/// Convert to a <see cref="UIntPtr"/>, signed, overflow check
		/// </summary>
		Conv_Ovf_U,

		/// <summary>
		/// Convert to a <see cref="UIntPtr"/>, unsigned, overflow check
		/// </summary>
		Conv_Ovf_U_Un,
	}

	/// <summary>
	/// 32-bit integer. 1-byte, 2-byte and 4-byte integer values, booleans, and chars use this class.
	/// Smaller values are sign or zero extended.
	/// </summary>
	public class ConstantInt32ILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.Int32"/>
		/// </summary>
		public sealed override ILValueKind Kind => ILValueKind.Int32;

		/// <summary>
		/// Gets the value
		/// </summary>
		public int Value { get; }

		/// <summary>
		/// Gets the value as a <see cref="uint"/>
		/// </summary>
		public uint UnsignedValue => (uint)Value;

		internal ulong UnsignedValue64 => (ulong)(long)Value;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <param name="value">Value</param>
		public ConstantInt32ILValue(DmdAppDomain appDomain, int value) {
			if (appDomain is null)
				throw new ArgumentNullException(nameof(appDomain));
			Type = appDomain.System_Int32;
			Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type, eg. <see cref="int"/></param>
		/// <param name="value">Value</param>
		public ConstantInt32ILValue(DmdType type, int value) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Value = value;
		}

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		public override DmdType? Type { get; }

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "0x" + Value.ToString("X8");
	}

	/// <summary>
	/// 64-bit integer
	/// </summary>
	public class ConstantInt64ILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.Int64"/>
		/// </summary>
		public sealed override ILValueKind Kind => ILValueKind.Int64;

		/// <summary>
		/// Gets the value
		/// </summary>
		public long Value { get; }

		/// <summary>
		/// Gets the value as a <see cref="ulong"/>
		/// </summary>
		public ulong UnsignedValue => (ulong)Value;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <param name="value">Value</param>
		public ConstantInt64ILValue(DmdAppDomain appDomain, long value) {
			if (appDomain is null)
				throw new ArgumentNullException(nameof(appDomain));
			Type = appDomain.System_Int64;
			Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type, eg. <see cref="long"/></param>
		/// <param name="value">Value</param>
		public ConstantInt64ILValue(DmdType type, long value) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Value = value;
		}

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		public override DmdType? Type { get; }

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "0x" + Value.ToString("X16");
	}

	/// <summary>
	/// 64-bit floating point value (32-bit floating point numbers are extended to 64 bits)
	/// </summary>
	public class ConstantFloatILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.Float"/>
		/// </summary>
		public sealed override ILValueKind Kind => ILValueKind.Float;

		/// <summary>
		/// Gets the value
		/// </summary>
		public double Value { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <param name="value">Value</param>
		public ConstantFloatILValue(DmdAppDomain appDomain, double value) {
			if (appDomain is null)
				throw new ArgumentNullException(nameof(appDomain));
			Type = appDomain.System_Double;
			Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type, eg. <see cref="double"/></param>
		/// <param name="value">Value</param>
		public ConstantFloatILValue(DmdType type, double value) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Value = value;
		}

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		public override DmdType? Type { get; }

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => Value.ToString();
	}

	/// <summary>
	/// native integer or unmanaged pointer
	/// </summary>
	public abstract class NativeIntILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.NativeInt"/>
		/// </summary>
		public sealed override ILValueKind Kind => ILValueKind.NativeInt;
	}

	/// <summary>
	/// native integer or unmanaged pointer
	/// </summary>
	public class ConstantNativeIntILValue : NativeIntILValue {
		readonly long value;

		/// <summary>
		/// Gets the value as a <see cref="int"/>
		/// </summary>
		public int Value32 => (int)value;

		/// <summary>
		/// Gets the value as a <see cref="long"/>
		/// </summary>
		public long Value64 => value;

		/// <summary>
		/// Gets the value as a <see cref="uint"/>
		/// </summary>
		public uint UnsignedValue32 => (uint)value;

		/// <summary>
		/// Gets the value as a <see cref="ulong"/>
		/// </summary>
		public ulong UnsignedValue64 => (ulong)value;

		/// <summary>
		/// Creates a 32-bit native int
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public static ConstantNativeIntILValue Create32(DmdAppDomain appDomain, int value) => new ConstantNativeIntILValue(appDomain, value);

		/// <summary>
		/// Creates a 64-bit native int
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public static ConstantNativeIntILValue Create64(DmdAppDomain appDomain, long value) => new ConstantNativeIntILValue(appDomain, value);

		/// <summary>
		/// Creates a 32-bit native int
		/// </summary>
		/// <param name="type">Type, eg. <see cref="IntPtr"/></param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public static ConstantNativeIntILValue Create32(DmdType type, int value) => new ConstantNativeIntILValue(type, value);

		/// <summary>
		/// Creates a 64-bit native int
		/// </summary>
		/// <param name="type">Type, eg. <see cref="IntPtr"/></param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public static ConstantNativeIntILValue Create64(DmdType type, long value) => new ConstantNativeIntILValue(type, value);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <param name="value">Value</param>
		protected ConstantNativeIntILValue(DmdAppDomain appDomain, int value) {
			if (appDomain is null)
				throw new ArgumentNullException(nameof(appDomain));
			Type = appDomain.System_IntPtr;
			this.value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <param name="value">Value</param>
		protected ConstantNativeIntILValue(DmdAppDomain appDomain, long value) {
			if (appDomain is null)
				throw new ArgumentNullException(nameof(appDomain));
			Type = appDomain.System_IntPtr;
			this.value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type, eg. <see cref="IntPtr"/></param>
		/// <param name="value">Value</param>
		protected ConstantNativeIntILValue(DmdType type, int value) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			this.value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type, eg. <see cref="IntPtr"/></param>
		/// <param name="value">Value</param>
		protected ConstantNativeIntILValue(DmdType type, long value) {
			Type = type ?? throw new ArgumentNullException(nameof(type));
			this.value = value;
		}

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		public override DmdType? Type { get; }

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "0x" + value.ToString("X");
	}

	/// <summary>
	/// Function pointer, created by the ldftn/ldvirtftn instructions
	/// </summary>
	public sealed class FunctionPointerILValue : NativeIntILValue {
		/// <summary>
		/// true if it was created by a ldvirtftn instruction, false it was created by a ldftn instruction
		/// </summary>
		public bool IsVirtual => !(VirtualThisObject is null);

		/// <summary>
		/// Gets the this value if and only if this was created by a ldvirtftn instruction, otherwise it's null
		/// </summary>
		public ILValue? VirtualThisObject { get; }

		/// <summary>
		/// Gets the method
		/// </summary>
		public DmdMethodBase Method { get; }

		/// <summary>
		/// Constructor (used by ldftn instruction)
		/// </summary>
		/// <param name="method">Method</param>
		public FunctionPointerILValue(DmdMethodBase method) {
			Method = method ?? throw new ArgumentNullException(nameof(method));
			Type = method.AppDomain.System_Void.MakePointerType();
		}

		/// <summary>
		/// Constructor (used by ldvirtftn instruction)
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="thisValue">This object</param>
		public FunctionPointerILValue(DmdMethodBase method, ILValue thisValue) {
			Method = method;
			Type = method.AppDomain.System_Void.MakePointerType();
			VirtualThisObject = thisValue;
		}

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		public override DmdType? Type { get; }
	}

	/// <summary>
	/// Pointer to a block of memory. Used by eg. localloc
	/// </summary>
	public sealed class NativeMemoryILValue : NativeIntILValue {
		readonly byte[] data;
		long offset;

		int Offset32 => (int)offset;
		long Offset64 => offset;
		uint UnsignedOffset32 => (uint)offset;
		ulong UnsignedOffset64 => (ulong)offset;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <param name="size">Size of memory</param>
		public NativeMemoryILValue(DmdAppDomain appDomain, int size) {
			if (appDomain is null)
				throw new ArgumentNullException(nameof(appDomain));
			Type = appDomain.System_Void.MakePointerType();
			data = new byte[size];
		}

		NativeMemoryILValue(byte[] data, long offset) {
			this.data = data;
			this.offset = offset;
		}

		/// <summary>
		/// Adds a constant to a copy of this value and returns the result. Returns null if it's not supported.
		/// </summary>
		/// <param name="kind">Opcode kind</param>
		/// <param name="value">Value to add</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public override ILValue? Add(AddOpCodeKind kind, long value, int pointerSize) {
			if (value == 0)
				return this;

			switch (kind) {
			case AddOpCodeKind.Add:
				if (pointerSize == 4)
					return new NativeMemoryILValue(data, Offset32 + (int)value);
				return new NativeMemoryILValue(data, Offset64 + value);

			case AddOpCodeKind.Add_Ovf:
				if (pointerSize == 4) {
					int value2 = (int)value;
					return new NativeMemoryILValue(data, checked(Offset32 + value2));
				}
				return new NativeMemoryILValue(data, checked(Offset64 + value));

			case AddOpCodeKind.Add_Ovf_Un:
				if (pointerSize == 4) {
					uint value2 = (uint)value;
					return new NativeMemoryILValue(data, (int)checked(UnsignedOffset32 + value2));
				}
				else {
					ulong value2 = (ulong)value;
					return new NativeMemoryILValue(data, (long)checked(UnsignedOffset64 + value2));
				}

			default:
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Subtracts a constant from a copy of this value and returns the result. Returns null if it's not supported.
		/// </summary>
		/// <param name="kind">Opcode kind</param>
		/// <param name="value">Value to subtract</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public override ILValue? Sub(SubOpCodeKind kind, long value, int pointerSize) {
			if (value == 0)
				return this;

			switch (kind) {
			case SubOpCodeKind.Sub:
				if (pointerSize == 4)
					return new NativeMemoryILValue(data, Offset32 - (int)value);
				return new NativeMemoryILValue(data, Offset64 - value);

			case SubOpCodeKind.Sub_Ovf:
				if (pointerSize == 4) {
					int value2 = (int)value;
					return new NativeMemoryILValue(data, checked(Offset32 - value2));
				}
				return new NativeMemoryILValue(data, checked(Offset64 - value));

			case SubOpCodeKind.Sub_Ovf_Un:
				if (pointerSize == 4) {
					uint value2 = (uint)value;
					return new NativeMemoryILValue(data, (int)checked(UnsignedOffset32 - value2));
				}
				else {
					ulong value2 = (ulong)value;
					return new NativeMemoryILValue(data, (long)checked(UnsignedOffset64 - value2));
				}

			default:
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Loads a value. Returns null if it's not supported.
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="loadValueType">Type of value to load</param>
		/// <returns></returns>
		public override ILValue? LoadIndirect(DmdType type, LoadValueType loadValueType) {
			int pointerSize = type.AppDomain.Runtime.PointerSize;
			switch (loadValueType) {
			case LoadValueType.I:
				if (pointerSize == 4) {
					if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
						return null;
					return ConstantNativeIntILValue.Create32(type.AppDomain, BitConverter.ToInt32(data, (int)offset));
				}
				else {
					Debug.Assert(pointerSize == 8);
					if (offset + 8 - 1 < offset || (ulong)offset + 8 - 1 >= (ulong)data.Length)
						return null;
					return ConstantNativeIntILValue.Create64(type.AppDomain, BitConverter.ToInt64(data, (int)offset));
				}

			case LoadValueType.I1:
				if ((ulong)offset >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue(type.AppDomain.System_SByte, (sbyte)data[(int)offset]);

			case LoadValueType.I2:
				if (offset + 2 - 1 < offset || (ulong)offset + 2 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue(type.AppDomain.System_Int16, BitConverter.ToInt16(data, (int)offset));

			case LoadValueType.I4:
				if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue(type.AppDomain.System_Int32, BitConverter.ToInt32(data, (int)offset));

			case LoadValueType.I8:
				if (offset + 8 - 1 < offset || (ulong)offset + 8 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantInt64ILValue(type.AppDomain.System_Int64, BitConverter.ToInt64(data, (int)offset));

			case LoadValueType.R4:
				if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantFloatILValue(type.AppDomain.System_Single, BitConverter.ToSingle(data, (int)offset));

			case LoadValueType.R8:
				if (offset + 8 - 1 < offset || (ulong)offset + 8 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantFloatILValue(type.AppDomain.System_Double, BitConverter.ToDouble(data, (int)offset));

			case LoadValueType.Ref:
				return null;

			case LoadValueType.U1:
				if ((ulong)offset >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue(type.AppDomain.System_Byte, data[(int)offset]);

			case LoadValueType.U2:
				if (offset + 2 - 1 < offset || (ulong)offset + 2 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue(type.AppDomain.System_UInt16, BitConverter.ToUInt16(data, (int)offset));

			case LoadValueType.U4:
				if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue(type.AppDomain.System_UInt32, BitConverter.ToInt32(data, (int)offset));

			default:
				return null;
			}
		}

		/// <summary>
		/// Stores a value. Returns false if it's not supported.
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="loadValueType">Type of value to store</param>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public override bool StoreIndirect(DmdType type, LoadValueType loadValueType, ILValue value) {
			int pointerSize = type.AppDomain.Runtime.PointerSize;
			long v;
			double d;
			switch (loadValueType) {
			case LoadValueType.I:
				if (pointerSize == 4) {
					if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
						return false;
					if (!GetValue(value, pointerSize, out v))
						return false;
					WriteInt32(data, (int)offset, v);
					return true;
				}
				else {
					Debug.Assert(pointerSize == 8);
					if (offset + 8 - 1 < offset || (ulong)offset + 8 - 1 >= (ulong)data.Length)
						return false;
					if (!GetValue(value, pointerSize, out v))
						return false;
					WriteInt64(data, (int)offset, v);
					return true;
				}

			case LoadValueType.I1:
			case LoadValueType.U1:
				if ((ulong)offset >= (ulong)data.Length)
					return false;
				if (!GetValue(value, pointerSize, out v))
					return false;
				WriteInt8(data, (int)offset, v);
				return true;

			case LoadValueType.I2:
			case LoadValueType.U2:
				if (offset + 2 - 1 < offset || (ulong)offset + 2 - 1 >= (ulong)data.Length)
					return false;
				if (!GetValue(value, pointerSize, out v))
					return false;
				WriteInt16(data, (int)offset, v);
				return true;

			case LoadValueType.I4:
			case LoadValueType.U4:
				if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
					return false;
				if (!GetValue(value, pointerSize, out v))
					return false;
				WriteInt32(data, (int)offset, v);
				return true;

			case LoadValueType.I8:
				if (offset + 8 - 1 < offset || (ulong)offset + 8 - 1 >= (ulong)data.Length)
					return false;
				if (!GetValue(value, pointerSize, out v))
					return false;
				WriteInt64(data, (int)offset, v);
				return true;

			case LoadValueType.R4:
				if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
					return false;
				if (!GetValue(value, out d))
					return false;
				WriteSingle(data, (int)offset, (float)d);
				return true;

			case LoadValueType.R8:
				if (offset + 8 - 1 < offset || (ulong)offset + 8 - 1 >= (ulong)data.Length)
					return false;
				if (!GetValue(value, out d))
					return false;
				WriteDouble(data, (int)offset, d);
				return true;

			case LoadValueType.Ref:
				return false;

			default:
				return false;
			}
		}

		static bool GetValue(ILValue value, int pointerSize, out long result) {
			if (value is ConstantInt32ILValue c32) {
				result = c32.Value;
				return true;
			}
			else if (value is ConstantInt64ILValue c64) {
				result = c64.Value;
				return true;
			}
			else if (value is ConstantNativeIntILValue cni) {
				result = pointerSize == 4 ? cni.Value32 : cni.Value64;
				return true;
			}

			result = 0;
			return false;
		}

		static bool GetValue(ILValue value, out double result) {
			if (value is ConstantFloatILValue f) {
				result = f.Value;
				return true;
			}

			result = 0;
			return false;
		}

		static void WriteInt8(byte[] data, int offset, long value) {
			data[offset] = (byte)value;
		}

		static void WriteInt16(byte[] data, int offset, long value) {
			data[offset++] = (byte)value;
			data[offset] = (byte)(value >> 8);
		}

		static void WriteInt32(byte[] data, int offset, long value) {
			data[offset++] = (byte)value;
			data[offset++] = (byte)(value >> 8);
			data[offset++] = (byte)(value >> 16);
			data[offset] = (byte)(value >> 24);
		}

		static void WriteInt64(byte[] data, int offset, long value) {
			data[offset++] = (byte)value;
			data[offset++] = (byte)(value >> 8);
			data[offset++] = (byte)(value >> 16);
			data[offset++] = (byte)(value >> 24);
			data[offset++] = (byte)(value >> 32);
			data[offset++] = (byte)(value >> 40);
			data[offset++] = (byte)(value >> 48);
			data[offset] = (byte)(value >> 56);
		}

		static void WriteSingle(byte[] data, int offset, float value) {
			var b = BitConverter.GetBytes((float)value);
			for (int i = 0; i < b.Length; i++)
				data[offset + i] = b[i];
		}

		static void WriteDouble(byte[] data, int offset, double value) {
			var b = BitConverter.GetBytes(value);
			for (int i = 0; i < b.Length; i++)
				data[offset + i] = b[i];
		}

		/// <summary>
		/// Initializes memory or returns false if it's not supported
		/// </summary>
		/// <param name="value">Value to write</param>
		/// <param name="size">Size of data</param>
		/// <returns></returns>
		public override bool InitializeMemory(byte value, long size) {
			if (offset + size < offset || (ulong)(offset + size) > (ulong)data.Length)
				return false;
			int size2 = (int)size;
			int o = (int)offset;
			var d = data;
			for (int i = 0; i < size2; i++)
				d[i + o] = value;
			return true;
		}

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		public override DmdType? Type { get; }
	}

	/// <summary>
	/// Managed pointer
	/// </summary>
	public abstract class ByRefILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.ByRef"/>
		/// </summary>
		public sealed override ILValueKind Kind => ILValueKind.ByRef;
	}

	/// <summary>
	/// A reference type or a value type
	/// </summary>
	public abstract class TypeILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.Type"/>
		/// </summary>
		public sealed override ILValueKind Kind => ILValueKind.Type;
	}

	/// <summary>
	/// A null reference
	/// </summary>
	public class NullObjectRefILValue : TypeILValue {
		/// <summary>
		/// Returns true since it's a null value
		/// </summary>
		public sealed override bool IsNull => true;

		/// <summary>
		/// Constructor
		/// </summary>
		public NullObjectRefILValue() { }

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		public override DmdType? Type => null;
	}
}
