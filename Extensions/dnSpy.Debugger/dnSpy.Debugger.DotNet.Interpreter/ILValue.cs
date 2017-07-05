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
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public ConstantInt32ILValue(int value) => Value = value;
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
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public ConstantInt64ILValue(long value) => Value = value;
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
		/// <summary>
		/// Gets the value
		/// </summary>
		public long Value { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public ConstantNativeIntILValue(long value) => Value = value;
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
	}

	/// <summary>
	/// A boxed value type
	/// </summary>
	public sealed class BoxedValueTypeILValue : ObjectRefILValue {
		/// <summary>
		/// Gets the value
		/// </summary>
		public ValueTypeILValue Value { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public BoxedValueTypeILValue(ValueTypeILValue value) => Value = value;

		/// <summary>
		/// Gets the type of the value
		/// </summary>
		/// <param name="appDomain">AppDomain</param>
		/// <returns></returns>
		public override DmdType GetType(DmdAppDomain appDomain) => Value.GetType(appDomain);
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
