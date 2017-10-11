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
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DebuggerRuntimeImpl : DebuggerRuntime {
		public override int PointerSize { get; }

		internal IDbgDotNetRuntime Runtime => runtime;
		readonly IDbgDotNetRuntime runtime;
		readonly List<DbgDotNetValue> valuesToDispose;

		public DebuggerRuntimeImpl(IDbgDotNetRuntime runtime, int pointerSize) {
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			valuesToDispose = new List<DbgDotNetValue>();
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
			Debug.Assert(valuesToDispose.Count == 0);
		}

		public void Clear(DbgDotNetValue returnValue) {
			context = null;
			frame = null;
			cancellationToken = default;
			foreach (var v in valuesToDispose) {
				if (v != returnValue)
					v.Dispose();
			}
			valuesToDispose.Clear();
		}

		public DbgDotNetValue GetDotNetValue(ILValue value) {
			var dnValue = TryGetDotNetValue(value, canCreateValues: true);
			if (dnValue != null)
				return dnValue;
			throw new InvalidOperationException();//TODO:
		}

		DbgDotNetValue TryGetDotNetValue(ILValue value, bool canCreateValues) {
			if (value is IDebuggerRuntimeILValue rtValue)
				return rtValue.GetDotNetValue();
			if (canCreateValues) {
				if (value.IsNull)
					return new SyntheticNullValue(value.Type ?? frame.Module.AppDomain.GetReflectionAppDomain().System_Void);

				object newValue;
				var type = value.Type;
				switch (value.Kind) {
				case ILValueKind.Int32:
					int v32 = ((ConstantInt32ILValue)value).Value;
					switch (DmdType.GetTypeCode(type)) {
					case TypeCode.Boolean:	newValue = v32 != 0; break;
					case TypeCode.Char:		newValue = (char)v32; break;
					case TypeCode.SByte:	newValue = (sbyte)v32; break;
					case TypeCode.Byte:		newValue = (byte)v32; break;
					case TypeCode.Int16:	newValue = (short)v32; break;
					case TypeCode.UInt16:	newValue = (ushort)v32; break;
					case TypeCode.Int32:	newValue = v32; break;
					case TypeCode.UInt32:	newValue = (uint)v32; break;
					default:				newValue = null; break;
					}
					break;

				case ILValueKind.Int64:
					long v64 = ((ConstantInt64ILValue)value).Value;
					switch (DmdType.GetTypeCode(type)) {
					case TypeCode.Int64:	newValue = v64; break;
					case TypeCode.UInt64:	newValue = (ulong)v64; break;
					default:				newValue = null; break;
					}
					break;

				case ILValueKind.Float:
					double r8 = ((ConstantFloatILValue)value).Value;
					switch (DmdType.GetTypeCode(type)) {
					case TypeCode.Single:	newValue = (float)r8; break;
					case TypeCode.Double:	newValue = r8; break;
					default:				newValue = null; break;
					}
					break;

				case ILValueKind.NativeInt:
					if (value is ConstantNativeIntILValue ci) {
						if (type == type.AppDomain.System_IntPtr) {
							if (PointerSize == 4)
								newValue = new IntPtr(ci.Value32);
							else
								newValue = new IntPtr(ci.Value64);
						}
						else if (type == type.AppDomain.System_UIntPtr) {
							if (PointerSize == 4)
								newValue = new UIntPtr(ci.UnsignedValue32);
							else
								newValue = new UIntPtr(ci.UnsignedValue64);
						}
						else
							newValue = null;
					}
					else
						newValue = null;
					break;

				case ILValueKind.Type:
					if (value is ConstantStringILValueImpl sv)
						newValue = sv.Value;
					else
						newValue = null;
					break;

				default:
					newValue = null;
					break;
				}
				if (newValue != null)
					return RecordValue(runtime.CreateValue(context, frame, newValue, cancellationToken));
			}
			return null;
		}

		internal object GetDebuggerValue(ILValue value, DmdType targetType) {
			var dnValue = TryGetDotNetValue(value, canCreateValues: false);
			if (dnValue != null)
				return dnValue;

			if (value.IsNull)
				return null;

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
				if (value is ConstantStringILValueImpl sv)
					return sv.Value;
				break;
			}

			Debug.Fail($"Unknown value can't be converted to {targetType.FullName}: {value}");
			throw new InvalidOperationException();
		}

		ILValue CreateILValue(DbgDotNetValueResult result) {
			if (result.HasError)
				throw new InterpreterMessageException(result.ErrorMessage);
			if (result.ValueIsException)
				throw new InterpreterThrownExceptionException(result.Value);

			var dnValue = result.Value;
			if (dnValue == null)
				throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);

			return CreateILValue(dnValue);
		}

		DbgDotNetValue RecordValue(DbgDotNetValueResult result) {
			if (result.HasError)
				throw new InterpreterMessageException(result.ErrorMessage);
			if (result.ValueIsException)
				throw new InterpreterThrownExceptionException(result.Value);

			var dnValue = result.Value;
			if (dnValue == null)
				throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);

			return RecordValue(dnValue);
		}

		DbgDotNetValue RecordValue(DbgDotNetCreateValueResult result) {
			if (result.Error != null)
				throw new InterpreterMessageException(result.Error);
			if (result.Value == null)
				throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			return RecordValue(result.Value);
		}

		internal DbgDotNetValue RecordValue(DbgDotNetValue value) {
			try {
				cancellationToken.ThrowIfCancellationRequested();
				valuesToDispose.Add(value);
				return value;
			}
			catch {
				value.Dispose();
				throw;
			}
		}

		internal ILValue CreateILValue(DbgDotNetValue value) {
			try {
				valuesToDispose.Add(value);
				return CreateILValueCore(value);
			}
			catch {
				value.Dispose();
				throw;
			}
		}

		ILValue CreateILValueCore(DbgDotNetValue value) {
			if (value.Type.IsByRef)
				return new ByRefILValueImpl(this, value);
			if (value.IsNull)
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
				return new ConstantStringILValueImpl(this, value, (string)objValue);
			default:
				Debug.Fail($"Unknown type: {rawValue.ValueType}");
				throw new InvalidOperationException();
			}
		}

		public override ILValue LoadArgument(int index) => CreateILValue(runtime.GetParameterValue(context, frame, (uint)index, cancellationToken));
		internal DbgDotNetValue LoadArgument2(int index) => RecordValue(runtime.GetParameterValue(context, frame, (uint)index, cancellationToken));

		public override ILValue LoadLocal(int index) => CreateILValue(runtime.GetLocalValue(context, frame, (uint)index, cancellationToken));
		internal DbgDotNetValue LoadLocal2(int index) => RecordValue(runtime.GetLocalValue(context, frame, (uint)index, cancellationToken));

		public override ILValue LoadArgumentAddress(int index, DmdType type) => new ArgumentAddress(this, type, index);
		public override ILValue LoadLocalAddress(int index, DmdType type) => new LocalAddress(this, type, index);

		public override bool StoreArgument(int index, DmdType type, ILValue value) => StoreArgument2(index, type, GetDebuggerValue(value, type));

		internal bool StoreArgument2(int index, DmdType targetType, object value) {
			var error = runtime.SetParameterValue(context, frame, (uint)index, targetType, value, cancellationToken);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
		}

		public override bool StoreLocal(int index, DmdType type, ILValue value) => StoreLocal2(index, type, GetDebuggerValue(value, type));

		internal bool StoreLocal2(int index, DmdType targetType, object value) {
			var error = runtime.SetLocalValue(context, frame, (uint)index, targetType, value, cancellationToken);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
		}

		public override ILValue CreateSZArray(DmdType elementType, long length) {
			if (length < 0 || length > int.MaxValue)
				return null;
			var res = runtime.CreateSZArray(context, frame, elementType, (int)length, cancellationToken);
			return CreateILValue(res);
		}

		public override ILValue CreateRuntimeTypeHandle(DmdType type) {
			var appDomain = type.AppDomain;
			var methodGetType = appDomain.System_Type.GetMethod(nameof(Type.GetType), DmdSignatureCallingConvention.Default, 0, appDomain.System_Type, new[] { appDomain.System_String }, throwOnError: true);
			var typeValue = RecordValue(runtime.Call(context, frame, null, methodGetType, new[] { type.AssemblyQualifiedName }, cancellationToken));

			var runtimeTypeHandleType = appDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle);
			var getTypeHandleMethod = typeValue.Type.GetMethod("get_" + nameof(Type.TypeHandle), DmdSignatureCallingConvention.Default | DmdSignatureCallingConvention.HasThis, 0, runtimeTypeHandleType, Array.Empty<DmdType>(), throwOnError: true);
			var typeHandleValue = RecordValue(runtime.Call(context, frame, typeValue, getTypeHandleMethod, Array.Empty<object>(), cancellationToken));
			return CreateILValue(typeHandleValue);
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

		public override ILValue Box(ILValue value, DmdType type) {
			if (type.IsValueType) {
				var dnValue = TryGetDotNetValue(value, canCreateValues: true) ?? throw new InvalidOperationException();
				return new BoxedValueTypeILValue(this, value, dnValue, type);
			}
			return value;
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

		internal DbgDotNetValue LoadStaticField2(DmdFieldInfo field) {
			var res = runtime.LoadField(context, frame, null, field, cancellationToken);
			return RecordValue(res);
		}

		public override ILValue LoadStaticFieldAddress(DmdFieldInfo field) => new StaticFieldAddress(this, field);

		public override bool StoreStaticField(DmdFieldInfo field, ILValue value) => StoreStaticField(field, GetDebuggerValue(value, field.FieldType));
		internal bool StoreStaticField(DmdFieldInfo field, object value) {
			var error = runtime.StoreField(context, frame, null, field, value, cancellationToken);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
		}

		public override ILValue LoadString(DmdType type, string value) {
			var stringValue = RecordValue(runtime.CreateValue(context, frame, value, cancellationToken));
			return new ConstantStringILValueImpl(this, stringValue, value);
		}

		internal void SetArrayElementAt(DbgDotNetValue arrayValue, uint index, ILValue value) {
			var newValue = GetDebuggerValue(value, arrayValue.Type.GetElementType());
			SetArrayElementAt(arrayValue, index, newValue);
		}

		internal void SetArrayElementAt(DbgDotNetValue arrayValue, uint index, object value) {
			var error = arrayValue.SetArrayElementAt(context, frame, index, value, cancellationToken);
			if (error != null)
				throw new InterpreterMessageException(error);
		}

		public override int? CompareSigned(ILValue left, ILValue right) {
			return null;//TODO:
		}

		public override int? CompareUnsigned(ILValue left, ILValue right) {
			return null;//TODO:
		}

		public override bool? Equals(ILValue left, ILValue right) {
			if (left is AddressILValue laddr && right is AddressILValue raddr)
				return laddr.Equals(raddr);
			if (TryGetDotNetValue(left, canCreateValues: false) is DbgDotNetValue lv && TryGetDotNetValue(right, canCreateValues: false) is DbgDotNetValue rv) {
				var res = runtime.Equals(lv, rv);
				if (res != null)
					return res;
			}
			return null;
		}

		internal bool Equals(DbgDotNetValue a, DbgDotNetValue b) {
			if (a == b)
				return true;
			if (a.Type != b.Type)
				return false;

			var res = runtime.Equals(a, b);
			if (res != null)
				return res.Value;

			return false;
		}

		internal bool StoreInstanceField(DbgDotNetValue objValue, DmdFieldInfo field, ILValue value) =>
			StoreInstanceField(objValue, field, GetDebuggerValue(value, field.FieldType));

		internal bool StoreInstanceField(DbgDotNetValue objValue, DmdFieldInfo field, object value) {
			var error = runtime.StoreField(context, frame, objValue, field, value, cancellationToken);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
		}

		internal ILValue LoadInstanceField(DbgDotNetValue objValue, DmdFieldInfo field) =>
			CreateILValue(runtime.LoadField(context, frame, objValue, field, cancellationToken));

		internal DbgDotNetValue LoadInstanceField2(DbgDotNetValue objValue, DmdFieldInfo field) =>
			RecordValue(runtime.LoadField(context, frame, objValue, field, cancellationToken));

		internal ILValue LoadValueTypeFieldAddress(AddressILValue objValue, DmdFieldInfo field) {
			Debug.Assert(field.ReflectedType.IsValueType);
			return new ValueTypeFieldAddress(this, objValue, field);
		}

		internal ILValue LoadReferenceTypeFieldAddress(DbgDotNetValue objValue, DmdFieldInfo field) {
			Debug.Assert(field.ReflectedType.IsValueType);
			return new ReferenceTypeFieldAddress(this, objValue, field);
		}

		internal bool StoreIndirect(DbgDotNetValue byRefValue, object value) {
			Debug.Assert(byRefValue.Type.IsByRef);
			var error = byRefValue.StoreIndirect(context, frame, value, cancellationToken);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
		}

		internal object GetDefaultValue(DmdType type) {
			if (!type.IsValueType)
				return null;
			switch (DmdType.GetTypeCode(type)) {
			case TypeCode.Boolean:		return false;
			case TypeCode.Char:			return (char)0;
			case TypeCode.SByte:		return (sbyte)0;
			case TypeCode.Byte:			return (byte)0;
			case TypeCode.Int16:		return (short)0;
			case TypeCode.UInt16:		return (ushort)0;
			case TypeCode.Int32:		return 0;
			case TypeCode.UInt32:		return 0U;
			case TypeCode.Int64:		return 0L;
			case TypeCode.UInt64:		return 0UL;
			case TypeCode.Single:		return 0f;
			case TypeCode.Double:		return 0d;
			}
			return RecordValue(runtime.CreateInstanceNoConstructor(context, frame, type, cancellationToken));
		}

		internal bool CallInstance(DbgDotNetValue objValue, bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue returnValue) =>
			Call(objValue, isCallvirt, method, arguments, out returnValue);

		bool Call(DbgDotNetValue objValue, bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue returnValue) {
			if (method.SpecialMethodKind != DmdSpecialMethodKind.Metadata)
				throw new InvalidOperationException();
			var res = runtime.Call(context, frame, objValue, method, Convert(arguments, method.GetMethodSignature().GetParameterTypes()), cancellationToken);
			try {
				if (res.HasError)
					throw new InterpreterMessageException(res.ErrorMessage);
				if (res.ValueIsException) {
					var value = res.Value;
					res = default;
					throw new InterpreterThrownExceptionException(value);
				}
				if (method.GetMethodSignature().ReturnType == method.AppDomain.System_Void) {
					returnValue = null;
					res.Value?.Dispose();
				}
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

		internal int ToInt32(ILValue value) {
			if (value is ConstantInt32ILValue i32Value)
				return i32Value.Value;
			var dnValue = TryGetDotNetValue(value, canCreateValues: false);
			if (dnValue != null) {
				if (dnValue.Type != dnValue.Type.AppDomain.System_Int32)
					throw new InvalidOperationException();
				var rawValue = dnValue.GetRawValue();
				if (rawValue.ValueType == DbgSimpleValueType.Int32)
					return (int)rawValue.RawValue;
				throw new InvalidOperationException();
			}
			throw new InvalidOperationException();
		}
	}
}
