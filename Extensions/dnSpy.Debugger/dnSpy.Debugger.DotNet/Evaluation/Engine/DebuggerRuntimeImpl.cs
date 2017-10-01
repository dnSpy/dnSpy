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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Debugger;
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
		readonly Dictionary<int, ILValue> createdLocals;
		readonly Dictionary<int, ILValue> createdArguments;

		public DebuggerRuntimeImpl(IDbgDotNetRuntime runtime, int pointerSize) {
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			createdLocals = new Dictionary<int, ILValue>();
			createdArguments = new Dictionary<int, ILValue>();
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
			Debug.Assert(createdArguments.Count == 0);
			Debug.Assert(createdLocals.Count == 0);
		}

		public void Clear() {
			context = null;
			frame = null;
			cancellationToken = default;
			createdArguments.Clear();
			createdLocals.Clear();
		}

		public DbgDotNetValue GetDotNetValue(ILValue value) {
			var dnValue = TryGetDotNetValue(value);
			if (dnValue != null)
				return dnValue;
			throw new InvalidOperationException();//TODO:
		}

		static IDebuggerRuntimeILValue TryGetDebuggerRuntimeILValue(ILValue value) {
			var rtValue = value as IDebuggerRuntimeILValue;
			if (rtValue != null)
				return rtValue;
			if (value is BoxedValueTypeILValue boxedValue) {
				rtValue = boxedValue.Value as IDebuggerRuntimeILValue;
				if (rtValue != null)
					return rtValue;
			}
			return null;
		}

		DbgDotNetValue TryGetDotNetValue(ILValue value) {
			var rtValue = TryGetDebuggerRuntimeILValue(value);
			if (rtValue != null)
				return rtValue.GetDotNetValue();
			if (value.IsNull)
				return new SyntheticNullValue(value.Type ?? frame.Module.AppDomain.GetReflectionAppDomain().System_Void);
			return null;
		}

		object GetDebuggerValue(ILValue value, DmdType targetType) {
			var dnValue = TryGetDotNetValue(value);
			if (dnValue != null)
				return dnValue;

			var targetTypeCode = DmdType.GetTypeCode(targetType);
			switch (value.Kind) {
			case ILValueKind.Int32:
				int v32 = ((ConstantInt32ILValue)value).Value;
				switch (targetTypeCode) {
				case TypeCode.Boolean:	return v32 != 0;
				case TypeCode.Char:		return (char)v32;
				case TypeCode.SByte:	return (sbyte)v32;
				case TypeCode.Byte:		return (byte)v32;
				case TypeCode.Int16:	return (short)v32;
				case TypeCode.UInt16:	return (ushort)v32;
				case TypeCode.Int32:	return v32;
				case TypeCode.UInt32:	return (uint)v32;
				}
				break;

			case ILValueKind.Int64:
				long v64 = ((ConstantInt64ILValue)value).Value;
				switch (targetTypeCode) {
				case TypeCode.Int64:	return v64;
				case TypeCode.UInt64:	return (ulong)v64;
				}
				break;

			case ILValueKind.Float:
				double r8 = ((ConstantFloatILValue)value).Value;
				switch (targetTypeCode) {
				case TypeCode.Single:	return (float)r8;
				case TypeCode.Double:	return r8;
				}
				break;

			case ILValueKind.NativeInt:
				if (value is ConstantNativeIntILValue ci) {
					if (targetType == targetType.AppDomain.System_IntPtr) {
						if (PointerSize == 4)
							return new IntPtr(ci.Value32);
						return new IntPtr(ci.Value64);
					}
					else if (targetType == targetType.AppDomain.System_UIntPtr) {
						if (PointerSize == 4)
							return new UIntPtr(ci.UnsignedValue32);
						return new UIntPtr(ci.UnsignedValue64);
					}
				}
				break;

			case ILValueKind.Type:
				if (value is ConstantStringILValue sv)
					return sv.Value;
				break;
			}

			Debug.Fail($"Unknown value can't be converted to {targetType.FullName}: {value}");
			throw new InvalidOperationException();
		}

		ILValue CreateILValue(DbgDotNetValueResult result) {
			if (result.HasError)
				throw new InterpreterMessageException(result.ErrorMessage);
			if (result.ValueIsException) {
				result.Value.Dispose();
				return null;
			}

			var dnValue = result.Value;
			if (dnValue == null)
				return null;
			return CreateILValue(dnValue);
		}

		internal ILValue CreateILValue(DbgDotNetValue value, bool dispose = true) {
			try {
				return CreateILValueCore(value);
			}
			catch {
				if (dispose)
					value.Dispose();
				throw;
			}
		}

		ILValue CreateILValueCore(DbgDotNetValue value) {
			if (value.Type.IsByRef)
				return new ByRefILValueImpl(this, value);
			if (value.IsNullReference)
				return new NullObjectRefILValueImpl(value);

			if (value.Type.IsArray)
				return new ArrayILValue(this, value);

			var rawValue = value.GetRawValue();
			var objValue = rawValue.RawValue;
			switch (rawValue.ValueType) {
			case DbgSimpleValueType.Other:
				if (rawValue.HasRawValue && objValue == null)
					return new NullObjectRefILValueImpl(value);
				return new TypeILValueImpl(this, value);
			case DbgSimpleValueType.Decimal:
			case DbgSimpleValueType.DateTime:
				return new TypeILValueImpl(this, value);
			case DbgSimpleValueType.Void:
				throw new InvalidOperationException();
			case DbgSimpleValueType.Boolean:
				return new ConstantInt32ILValueImpl(value, (bool)objValue ? 1 : 0);
			case DbgSimpleValueType.Char1:
				return new ConstantInt32ILValueImpl(value, (byte)objValue);
			case DbgSimpleValueType.CharUtf16:
				return new ConstantInt32ILValueImpl(value, (char)objValue);
			case DbgSimpleValueType.Int8:
				return new ConstantInt32ILValueImpl(value, (sbyte)objValue);
			case DbgSimpleValueType.Int16:
				return new ConstantInt32ILValueImpl(value, (short)objValue);
			case DbgSimpleValueType.Int32:
				return new ConstantInt32ILValueImpl(value, (int)objValue);
			case DbgSimpleValueType.Int64:
				return new ConstantInt64ILValueImpl(value, (long)objValue);
			case DbgSimpleValueType.UInt8:
				return new ConstantInt32ILValueImpl(value, (byte)objValue);
			case DbgSimpleValueType.UInt16:
				return new ConstantInt32ILValueImpl(value, (ushort)objValue);
			case DbgSimpleValueType.UInt32:
				return new ConstantInt32ILValueImpl(value, (int)(uint)objValue);
			case DbgSimpleValueType.UInt64:
				return new ConstantInt64ILValueImpl(value, (long)(ulong)objValue);
			case DbgSimpleValueType.Float32:
				return new ConstantFloatILValueImpl(value, (float)objValue);
			case DbgSimpleValueType.Float64:
				return new ConstantFloatILValueImpl(value, (double)objValue);
			case DbgSimpleValueType.Ptr32:
				if (PointerSize != 4)
					throw new InvalidOperationException();
				return ConstantNativeIntILValueImpl.Create32(value, (int)(uint)objValue);
			case DbgSimpleValueType.Ptr64:
				if (PointerSize != 8)
					throw new InvalidOperationException();
				return ConstantNativeIntILValueImpl.Create64(value, (long)(ulong)objValue);
			case DbgSimpleValueType.StringUtf16:
				return new ConstantStringILValueImpl(value, (string)objValue);
			default:
				Debug.Fail($"Unknown type: {rawValue.ValueType}");
				throw new InvalidOperationException();
			}
		}

		public override ILValue LoadArgument(int index) {
			if (createdArguments.TryGetValue(index, out var value))
				return value;
			value = CreateILValue(runtime.GetParameterValue(context, frame, (uint)index, cancellationToken));
			createdArguments.Add(index, value);
			return value;
		}

		public override ILValue LoadLocal(int index) {
			if (createdLocals.TryGetValue(index, out var value))
				return value;
			value = CreateILValue(runtime.GetLocalValue(context, frame, (uint)index, cancellationToken));
			createdLocals.Add(index, value);
			return value;
		}

		public override ILValue LoadArgumentAddress(int index) {
			return null;//TODO:
		}

		public override ILValue LoadLocalAddress(int index) {
			return null;//TODO:
		}

		public override bool StoreArgument(int index, ILValue value) {
			createdArguments[index] = value;
			return true;
		}

		public override bool StoreLocal(int index, ILValue value) {
			createdLocals[index] = value;
			return true;
		}

		public override ILValue CreateSZArray(DmdType elementType, long length) {
			if (length < 0 || length > int.MaxValue)
				return null;
			var res = runtime.CreateSZArray(context, frame, elementType, (int)length, cancellationToken);
			return CreateILValue(res);
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
			var res = runtime.CreateInstanceNoConstructor(context, frame, type, cancellationToken);
			return CreateILValue(res);
		}

		public override bool CallStatic(DmdMethodBase method, ILValue[] arguments, out ILValue returnValue) =>
			Call(null, false, method, arguments, out returnValue);

		public override ILValue CreateInstance(DmdConstructorInfo ctor, ILValue[] arguments) {
			DbgDotNetValueResult res;
			DbgDotNetArrayDimensionInfo[] dimensionInfos;
			switch (ctor.SpecialMethodKind) {
			case DmdSpecialMethodKind.Array_Constructor1:
				dimensionInfos = new DbgDotNetArrayDimensionInfo[arguments.Length];
				for (int i = 0; i < dimensionInfos.Length; i++)
					dimensionInfos[i] = new DbgDotNetArrayDimensionInfo(0, (uint)ReadInt32(arguments[i]));
				res = runtime.CreateArray(context, frame, ctor.ReflectedType.GetElementType(), dimensionInfos, cancellationToken);
				return CreateILValue(res);

			case DmdSpecialMethodKind.Array_Constructor2:
				dimensionInfos = new DbgDotNetArrayDimensionInfo[arguments.Length / 2];
				for (int i = 0; i < dimensionInfos.Length; i++)
					dimensionInfos[i] = new DbgDotNetArrayDimensionInfo(ReadInt32(arguments[i * 2]), (uint)ReadInt32(arguments[i * 2 + 1]));
				res = runtime.CreateArray(context, frame, ctor.ReflectedType.GetElementType(), dimensionInfos, cancellationToken);
				return CreateILValue(res);

			default:
				res = runtime.CreateInstance(context, frame, ctor, Convert(arguments, ctor.GetMethodSignature().GetParameterTypes()), cancellationToken);
				return CreateILValue(res);
			}
		}

		static int ReadInt32(ILValue value) {
			if (value is ConstantInt32ILValue ci32)
				return ci32.Value;
			throw new InvalidOperationException();
		}

		public override bool CallStaticIndirect(DmdMethodSignature methodSig, ILValue methodAddress, ILValue[] arguments, out ILValue returnValue) {
			returnValue = null;
			return false;//TODO:
		}

		public override ILValue LoadStaticField(DmdFieldInfo field) {
			var res = runtime.LoadField(context, frame, null, field, cancellationToken);
			return CreateILValue(res);
		}

		public override ILValue LoadStaticFieldAddress(DmdFieldInfo field) {
			return null;//TODO:
		}

		public override bool StoreStaticField(DmdFieldInfo field, ILValue value) {
			var error = runtime.StoreField(context, frame, null, field, GetDebuggerValue(value, field.FieldType), cancellationToken);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
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

		internal bool StoreInstanceField(DbgDotNetValue objValue, DmdFieldInfo field, ILValue value) {
			var error = runtime.StoreField(context, frame, objValue, field, GetDebuggerValue(value, field.FieldType), cancellationToken);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
		}

		internal ILValue LoadInstanceField(DbgDotNetValue objValue, DmdFieldInfo field) =>
			CreateILValue(runtime.LoadField(context, frame, objValue, field, cancellationToken));

		internal ILValue LoadInstanceFieldAddress(DbgDotNetValue objValue, DmdFieldInfo field) {
			return null;//TODO:
		}

		internal bool CallInstance(DbgDotNetValue objValue, bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue returnValue) =>
			Call(objValue, isCallvirt, method, arguments, out returnValue);

		bool Call(DbgDotNetValue objValue, bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue returnValue) {
			if (method.SpecialMethodKind != DmdSpecialMethodKind.Metadata)
				throw new InvalidOperationException();
			var res = runtime.Call(context, frame, objValue, method, Convert(arguments, method.GetMethodSignature().GetParameterTypes()), cancellationToken);
			try {
				if (method.GetMethodSignature().ReturnType == method.AppDomain.System_Void)
					returnValue = null;
				else
					returnValue = CreateILValue(res);
				return true;
			}
			catch {
				res.Value?.Dispose();
				throw;
			}
		}

		object[] Convert(ILValue[] values, ReadOnlyCollection<DmdType> targetTypes) {
			if (values.Length != targetTypes.Count)
				throw new InvalidOperationException();
			var res = values.Length == 0 ? Array.Empty<object>() : new object[values.Length];
			for (int i = 0; i < res.Length; i++)
				res[i] = GetDebuggerValue(values[i], targetTypes[i]);
			return res;
		}
	}
}
