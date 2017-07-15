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
		/// Object reference (any type that's not a value type)
		/// </summary>
		ObjectRef,

		/// <summary>
		/// Value type, but not a primitive value type
		/// </summary>
		ValueType,
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
		public bool IsNull => this == NullObjectRefILValue.Instance;

		/// <summary>
		/// Makes a copy of this instance so the new instance can be pushed onto the stack. The default implementation
		/// returns itself. Only mutable value types need to override this method.
		/// </summary>
		/// <returns></returns>
		public virtual ILValue Clone() => this;

		/// <summary>
		/// Gets the type of the value or null if it's <see cref="NullObjectRefILValue"/> or if it's a primitive type
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <returns></returns>
		public virtual DmdType GetType(DmdAppDomain appDomain) => null;

		/// <summary>
		/// Adds <paramref name="value"/> to this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to add to this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public virtual ILValue PointerAdd(long value, int pointerSize) => null;

		/// <summary>
		/// Adds <paramref name="value"/> to this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to add to this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public virtual ILValue PointerAddOvf(long value, int pointerSize) => null;

		/// <summary>
		/// Adds <paramref name="value"/> to this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to add to this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public virtual ILValue PointerAddOvfUn(long value, int pointerSize) => null;

		/// <summary>
		/// Subtracts <paramref name="value"/> from this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to subtract from this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public virtual ILValue PointerSub(long value, int pointerSize) => null;

		/// <summary>
		/// Subtracts <paramref name="value"/> from this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to subtract from this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public virtual ILValue PointerSubOvf(long value, int pointerSize) => null;

		/// <summary>
		/// Subtracts <paramref name="value"/> from this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to subtract from this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public virtual ILValue PointerSubOvfUn(long value, int pointerSize) => null;

		/// <summary>
		/// Reads a (managed or unmanaged) pointer or returns null if it's not supported
		/// </summary>
		/// <param name="pointerType">Pointer type</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public virtual ILValue ReadPointer(PointerOpCodeType pointerType, int pointerSize) => null;

		/// <summary>
		/// Writes a (managed or unmanaged) pointer or returns false if it's not supported
		/// </summary>
		/// <param name="pointerType">Pointer type</param>
		/// <param name="value">New value</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public virtual bool WritePointer(PointerOpCodeType pointerType, ILValue value, int pointerSize) => false;

		/// <summary>
		/// Initializes memory or returns false if it's not supported
		/// </summary>
		/// <param name="value">Value to write</param>
		/// <param name="size">Size of data</param>
		/// <returns></returns>
		public virtual bool InitializeMemory(byte value, long size) => false;
	}

	/// <summary>
	/// 32-bit integer. 1-byte, 2-byte and 4-byte integer values, booleans, and chars use this class.
	/// Smaller values are sign or zero extended.
	/// </summary>
	public sealed class ConstantInt32ILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.Int32"/>
		/// </summary>
		public override ILValueKind Kind => ILValueKind.Int32;

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
		/// <param name="value">Value</param>
		public ConstantInt32ILValue(int value) => Value = value;

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "0x" + Value.ToString("X8");
	}

	/// <summary>
	/// 64-bit integer
	/// </summary>
	public sealed class ConstantInt64ILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.Int64"/>
		/// </summary>
		public override ILValueKind Kind => ILValueKind.Int64;

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
		/// <param name="value">Value</param>
		public ConstantInt64ILValue(long value) => Value = value;

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "0x" + Value.ToString("X16");
	}

	/// <summary>
	/// 64-bit floating point value (32-bit floating point numbers are extended to 64 bits)
	/// </summary>
	public sealed class ConstantFloatILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.Float"/>
		/// </summary>
		public override ILValueKind Kind => ILValueKind.Float;

		/// <summary>
		/// Gets the value
		/// </summary>
		public double Value { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public ConstantFloatILValue(double value) => Value = value;

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
		public override ILValueKind Kind => ILValueKind.NativeInt;
	}

	/// <summary>
	/// native integer or unmanaged pointer
	/// </summary>
	public sealed class ConstantNativeIntILValue : NativeIntILValue {
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
		/// <param name="value">Value</param>
		/// <returns></returns>
		public static ConstantNativeIntILValue Create32(int value) => new ConstantNativeIntILValue(value);

		/// <summary>
		/// Creates a 64-bit native int
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public static ConstantNativeIntILValue Create64(long value) => new ConstantNativeIntILValue(value);

		ConstantNativeIntILValue(int value) => this.value = value;
		ConstantNativeIntILValue(long value) => this.value = value;

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
		public bool IsVirtual => VirtualThisObject != null;

		/// <summary>
		/// Gets the this value if and only if this was created by a ldvirtftn instruction, otherwise it's null
		/// </summary>
		public ILValue VirtualThisObject { get; }

		/// <summary>
		/// Gets the method
		/// </summary>
		public DmdMethodBase Method { get; }

		/// <summary>
		/// Constructor (used by ldftn instruction)
		/// </summary>
		/// <param name="method">Method</param>
		public FunctionPointerILValue(DmdMethodBase method) => Method = method;

		/// <summary>
		/// Constructor (used by ldvirtftn instruction)
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="thisValue">This object</param>
		public FunctionPointerILValue(DmdMethodBase method, ILValue thisValue) {
			Method = method;
			VirtualThisObject = thisValue;
		}
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
		/// <param name="size">Size of memory</param>
		public NativeMemoryILValue(int size) => data = new byte[size];

		NativeMemoryILValue(byte[] data, long offset) {
			this.data = data;
			this.offset = offset;
		}

		/// <summary>
		/// Adds <paramref name="value"/> to this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to add to this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public override ILValue PointerAdd(long value, int pointerSize) {
			if (value == 0)
				return this;
			if (pointerSize == 4)
				return new NativeMemoryILValue(data, Offset32 + (int)value);
			return new NativeMemoryILValue(data, Offset64 + value);
		}

		/// <summary>
		/// Adds <paramref name="value"/> to this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to add to this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public override ILValue PointerAddOvf(long value, int pointerSize) {
			if (value == 0)
				return this;
			if (pointerSize == 4) {
				int value2 = (int)value;
				return new NativeMemoryILValue(data, checked(Offset32 + value2));
			}
			return new NativeMemoryILValue(data, checked(Offset64 + value));
		}

		/// <summary>
		/// Adds <paramref name="value"/> to this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to add to this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public override ILValue PointerAddOvfUn(long value, int pointerSize) {
			if (value == 0)
				return this;
			if (pointerSize == 4) {
				uint value2 = (uint)value;
				return new NativeMemoryILValue(data, (int)checked(UnsignedOffset32 + value2));
			}
			else {
				ulong value2 = (ulong)value;
				return new NativeMemoryILValue(data, (long)checked(UnsignedOffset64 + value2));
			}
		}

		/// <summary>
		/// Subtracts <paramref name="value"/> from this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to subtract from this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public override ILValue PointerSub(long value, int pointerSize) {
			if (value == 0)
				return this;
			if (pointerSize == 4)
				return new NativeMemoryILValue(data, Offset32 - (int)value);
			return new NativeMemoryILValue(data, Offset64 - value);
		}

		/// <summary>
		/// Subtracts <paramref name="value"/> from this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to subtract from this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public override ILValue PointerSubOvf(long value, int pointerSize) {
			if (value == 0)
				return this;
			if (pointerSize == 4) {
				int value2 = (int)value;
				return new NativeMemoryILValue(data, checked(Offset32 - value2));
			}
			return new NativeMemoryILValue(data, checked(Offset64 - value));
		}

		/// <summary>
		/// Subtracts <paramref name="value"/> from this pointer or by-ref and returns a new value.
		/// Returns null if it's not supported.
		/// </summary>
		/// <param name="value">Value to subtract from this instance</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public override ILValue PointerSubOvfUn(long value, int pointerSize) {
			if (value == 0)
				return this;
			if (pointerSize == 4) {
				uint value2 = (uint)value;
				return new NativeMemoryILValue(data, (int)checked(UnsignedOffset32 - value2));
			}
			else {
				ulong value2 = (ulong)value;
				return new NativeMemoryILValue(data, (long)checked(UnsignedOffset64 - value2));
			}
		}

		/// <summary>
		/// Reads a (managed or unmanaged) pointer or returns null if it's not supported
		/// </summary>
		/// <param name="pointerType">Pointer type</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public override ILValue ReadPointer(PointerOpCodeType pointerType, int pointerSize) {
			switch (pointerType) {
			case PointerOpCodeType.I:
				if (pointerSize == 4) {
					if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
						return null;
					return ConstantNativeIntILValue.Create32(BitConverter.ToInt32(data, (int)offset));
				}
				else {
					Debug.Assert(pointerSize == 8);
					if (offset + 8 - 1 < offset || (ulong)offset + 8 - 1 >= (ulong)data.Length)
						return null;
					return ConstantNativeIntILValue.Create64(BitConverter.ToInt64(data, (int)offset));
				}

			case PointerOpCodeType.I1:
				if ((ulong)offset >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue((sbyte)data[(int)offset]);

			case PointerOpCodeType.I2:
				if (offset + 2 - 1 < offset || (ulong)offset + 2 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue(BitConverter.ToInt16(data, (int)offset));

			case PointerOpCodeType.I4:
				if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue(BitConverter.ToInt32(data, (int)offset));

			case PointerOpCodeType.I8:
				if (offset + 8 - 1 < offset || (ulong)offset + 8 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantInt64ILValue(BitConverter.ToInt64(data, (int)offset));

			case PointerOpCodeType.R4:
				if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantFloatILValue(BitConverter.ToSingle(data, (int)offset));

			case PointerOpCodeType.R8:
				if (offset + 8 - 1 < offset || (ulong)offset + 8 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantFloatILValue(BitConverter.ToDouble(data, (int)offset));

			case PointerOpCodeType.Ref:
				return null;

			case PointerOpCodeType.U1:
				if ((ulong)offset >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue(data[(int)offset]);

			case PointerOpCodeType.U2:
				if (offset + 2 - 1 < offset || (ulong)offset + 2 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue(BitConverter.ToUInt16(data, (int)offset));

			case PointerOpCodeType.U4:
				if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
					return null;
				return new ConstantInt32ILValue(BitConverter.ToInt32(data, (int)offset));

			default:
				return null;
			}
		}

		/// <summary>
		/// Writes a (managed or unmanaged) pointer or returns null if it's not supported
		/// </summary>
		/// <param name="pointerType">Pointer type</param>
		/// <param name="value">New value</param>
		/// <param name="pointerSize">Size of a pointer in bytes</param>
		/// <returns></returns>
		public override bool WritePointer(PointerOpCodeType pointerType, ILValue value, int pointerSize) {
			long v;
			double d;
			switch (pointerType) {
			case PointerOpCodeType.I:
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

			case PointerOpCodeType.I1:
			case PointerOpCodeType.U1:
				if ((ulong)offset >= (ulong)data.Length)
					return false;
				if (!GetValue(value, pointerSize, out v))
					return false;
				WriteInt8(data, (int)offset, v);
				return true;

			case PointerOpCodeType.I2:
			case PointerOpCodeType.U2:
				if (offset + 2 - 1 < offset || (ulong)offset + 2 - 1 >= (ulong)data.Length)
					return false;
				if (!GetValue(value, pointerSize, out v))
					return false;
				WriteInt16(data, (int)offset, v);
				return true;

			case PointerOpCodeType.I4:
			case PointerOpCodeType.U4:
				if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
					return false;
				if (!GetValue(value, pointerSize, out v))
					return false;
				WriteInt32(data, (int)offset, v);
				return true;

			case PointerOpCodeType.I8:
				if (offset + 8 - 1 < offset || (ulong)offset + 8 - 1 >= (ulong)data.Length)
					return false;
				if (!GetValue(value, pointerSize, out v))
					return false;
				WriteInt64(data, (int)offset, v);
				return true;

			case PointerOpCodeType.R4:
				if (offset + 4 - 1 < offset || (ulong)offset + 4 - 1 >= (ulong)data.Length)
					return false;
				if (!GetValue(value, out d))
					return false;
				WriteSingle(data, (int)offset, (float)d);
				return true;

			case PointerOpCodeType.R8:
				if (offset + 8 - 1 < offset || (ulong)offset + 8 - 1 >= (ulong)data.Length)
					return false;
				if (!GetValue(value, out d))
					return false;
				WriteDouble(data, (int)offset, d);
				return true;

			case PointerOpCodeType.Ref:
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
	}

	/// <summary>
	/// Managed pointer
	/// </summary>
	public abstract class ByRefILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.ByRef"/>
		/// </summary>
		public override ILValueKind Kind => ILValueKind.ByRef;
	}

	/// <summary>
	/// Object reference (non-value type)
	/// </summary>
	public abstract class ObjectRefILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.ObjectRef"/>
		/// </summary>
		public override ILValueKind Kind => ILValueKind.ObjectRef;

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <returns></returns>
		public abstract override DmdType GetType(DmdAppDomain appDomain);
	}

	/// <summary>
	/// A string value
	/// </summary>
	public sealed class ConstantStringILValue : ObjectRefILValue {
		/// <summary>
		/// Gets the value
		/// </summary>
		public string Value { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">String value, but not null</param>
		public ConstantStringILValue(string value) => Value = value;

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <returns></returns>
		public override DmdType GetType(DmdAppDomain appDomain) => appDomain.System_String;

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() => "\"" + Value + "\"";
	}

	/// <summary>
	/// A boxed value type
	/// </summary>
	public sealed class BoxedValueTypeILValue : ObjectRefILValue {
		/// <summary>
		/// Gets the value
		/// </summary>
		public ILValue Value { get; }

		readonly DmdType type;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public BoxedValueTypeILValue(ValueTypeILValue value) => Value = (ValueTypeILValue)value.Clone();

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		/// <param name="type">Boxed type</param>
		public BoxedValueTypeILValue(ILValue value, DmdType type) {
			Value = value.Clone();
			this.type = type ?? throw new ArgumentNullException(nameof(type));
		}

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <returns></returns>
		public override DmdType GetType(DmdAppDomain appDomain) => type ?? Value.GetType(appDomain);
	}

	/// <summary>
	/// A null reference
	/// </summary>
	public sealed class NullObjectRefILValue : ObjectRefILValue {
		/// <summary>
		/// Gets the single instance
		/// </summary>
		public static readonly NullObjectRefILValue Instance = new NullObjectRefILValue();
		NullObjectRefILValue() { }

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <returns></returns>
		public override DmdType GetType(DmdAppDomain appDomain) => null;
	}

	/// <summary>
	/// Value type, but not a primitive value type such as <see cref="int"/> or <see cref="double"/>
	/// </summary>
	public abstract class ValueTypeILValue : ILValue {
		/// <summary>
		/// Always returns <see cref="ILValueKind.ValueType"/>
		/// </summary>
		public override ILValueKind Kind => ILValueKind.ValueType;

		/// <summary>
		/// Makes a copy of this instance so the new instance can be pushed onto the stack.
		/// </summary>
		/// <returns></returns>
		public abstract override ILValue Clone();

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <returns></returns>
		public abstract override DmdType GetType(DmdAppDomain appDomain);
	}
}
