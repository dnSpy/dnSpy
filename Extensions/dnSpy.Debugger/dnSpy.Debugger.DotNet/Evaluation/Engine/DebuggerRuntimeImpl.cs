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
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DebuggerRuntimeImpl : DebuggerRuntime {
		public override int PointerSize { get; }

		internal IDbgDotNetRuntime Runtime => runtime;
		readonly IDbgDotNetRuntime runtime;

		public DebuggerRuntimeImpl(IDbgDotNetRuntime runtime, int pointerSize) {
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			PointerSize = pointerSize;
		}

		DbgEvaluationContext context;
		DbgStackFrame frame;
		CancellationToken cancellationToken;

		public void Initialize(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Debug.Assert(this.context == null);
			if (this.context != null)
				throw new InvalidOperationException();
			this.context = context;
			this.frame = frame;
			this.cancellationToken = cancellationToken;
		}

		public void Clear() {
			context = null;
			frame = null;
			cancellationToken = default;
		}

		public DbgDotNetValue GetDotNetValue(ILValue value) {
			var rtValue = value as IDebuggerRuntimeILValue;
			if (rtValue == null) {
				if (value is BoxedValueTypeILValue boxedValue)
					rtValue = boxedValue.Value as IDebuggerRuntimeILValue;
				if (rtValue == null)
					throw new InvalidOperationException();//TODO:
			}
			return rtValue.GetDotNetValue();
		}

		ILValue CreateILValue(DbgDotNetValueResult result) {
			if (result.HasError)
				return null;//TODO: Return error message
			if (result.ValueIsException)
				return null;//TODO: Return error message

			var dnValue = result.Value;
			if (dnValue == null)
				return null;

			if (dnValue.Type.IsByRef)
				return new ByRefILValueImpl(dnValue);

			var rawValue = dnValue.GetRawValue();
			var objValue = rawValue.RawValue;
			switch (rawValue.ValueType) {
			case DbgSimpleValueType.Other:
				if (rawValue.HasRawValue && objValue == null)
					return new NullObjectRefILValueImpl(dnValue);
				return new TypeILValueImpl(dnValue);
			case DbgSimpleValueType.Decimal:
			case DbgSimpleValueType.DateTime:
				return new TypeILValueImpl(dnValue);
			case DbgSimpleValueType.Void:
				throw new InvalidOperationException();
			case DbgSimpleValueType.Boolean:
				return new ConstantInt32ILValueImpl(dnValue, (bool)objValue ? 1 : 0);
			case DbgSimpleValueType.Char1:
				return new ConstantInt32ILValueImpl(dnValue, (byte)objValue);
			case DbgSimpleValueType.CharUtf16:
				return new ConstantInt32ILValueImpl(dnValue, (char)objValue);
			case DbgSimpleValueType.Int8:
				return new ConstantInt32ILValueImpl(dnValue, (sbyte)objValue);
			case DbgSimpleValueType.Int16:
				return new ConstantInt32ILValueImpl(dnValue, (short)objValue);
			case DbgSimpleValueType.Int32:
				return new ConstantInt32ILValueImpl(dnValue, (int)objValue);
			case DbgSimpleValueType.Int64:
				return new ConstantInt64ILValueImpl(dnValue, (long)objValue);
			case DbgSimpleValueType.UInt8:
				return new ConstantInt32ILValueImpl(dnValue, (byte)objValue);
			case DbgSimpleValueType.UInt16:
				return new ConstantInt32ILValueImpl(dnValue, (ushort)objValue);
			case DbgSimpleValueType.UInt32:
				return new ConstantInt32ILValueImpl(dnValue, (int)(uint)objValue);
			case DbgSimpleValueType.UInt64:
				return new ConstantInt64ILValueImpl(dnValue, (long)(ulong)objValue);
			case DbgSimpleValueType.Float32:
				return new ConstantFloatILValueImpl(dnValue, (float)objValue);
			case DbgSimpleValueType.Float64:
				return new ConstantFloatILValueImpl(dnValue, (double)objValue);
			case DbgSimpleValueType.Ptr32:
				if (PointerSize != 4)
					throw new InvalidOperationException();
				return ConstantNativeIntILValueImpl.Create32(dnValue, (int)(uint)objValue);
			case DbgSimpleValueType.Ptr64:
				if (PointerSize != 8)
					throw new InvalidOperationException();
				return ConstantNativeIntILValueImpl.Create64(dnValue, (long)(ulong)objValue);
			case DbgSimpleValueType.StringUtf16:
				return new ConstantStringILValueImpl(dnValue, (string)objValue);
			default:
				Debug.Fail($"Unknown type: {rawValue.ValueType}");
				throw new InvalidOperationException();
			}
		}

		public override ILValue LoadArgument(int index) => CreateILValue(runtime.GetParameterValue(context, frame, (uint)index, cancellationToken));
		public override ILValue LoadLocal(int index) => CreateILValue(runtime.GetLocalValue(context, frame, (uint)index, cancellationToken));

		public override ILValue LoadArgumentAddress(int index) {
			return null;//TODO:
		}

		public override ILValue LoadLocalAddress(int index) {
			return null;//TODO:
		}

		public override bool StoreArgument(int index, ILValue value) {
			return false;//TODO:
		}

		public override bool StoreLocal(int index, ILValue value) {
			return false;//TODO:
		}

		public override ILValue CreateSZArray(DmdType elementType, long length) {
			return null;//TODO:
		}

		public override ILValue CreateRuntimeTypeHandle(DmdType type) {
			return null;//TODO:
		}

		public override ILValue CreateRuntimeFieldHandle(DmdFieldInfo field) {
			return null;//TODO:
		}

		public override ILValue CreateRuntimeMethodHandle(DmdMethodBase method) {
			return null;//TODO:
		}

		public override ILValue CreateTypeNoConstructor(DmdType type) {
			return null;//TODO:
		}

		public override bool CallStatic(DmdMethodBase method, ILValue[] arguments, out ILValue returnValue) {
			returnValue = null;
			return false;//TODO:
		}

		public override ILValue CreateInstance(DmdConstructorInfo ctor, ILValue[] arguments) {
			switch (ctor.SpecialMethodKind) {
			case DmdSpecialMethodKind.Array_Constructor1:
				return null;//TODO:

			case DmdSpecialMethodKind.Array_Constructor2:
				return null;//TODO:

			default:
				return null;//TODO:
			}
		}

		public override bool CallStaticIndirect(DmdMethodSignature methodSig, ILValue methodAddress, ILValue[] arguments, out ILValue returnValue) {
			returnValue = null;
			return false;//TODO:
		}

		public override ILValue LoadStaticField(DmdFieldInfo field) {
			return null;//TODO:
		}

		public override ILValue LoadStaticFieldAddress(DmdFieldInfo field) {
			return null;//TODO:
		}

		public override bool StoreStaticField(DmdFieldInfo field, ILValue value) {
			return false;//TODO:
		}

		public override int? CompareSigned(ILValue left, ILValue right) {
			return null;//TODO:
		}

		public override int? CompareUnsigned(ILValue left, ILValue right) {
			return null;//TODO:
		}

		public override bool? Equals(ILValue left, ILValue right) {
			return null;//TODO:
		}
	}
}
