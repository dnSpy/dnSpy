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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Properties;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	abstract class DebuggerRuntime2 : DebuggerRuntime {
		public abstract IDbgDotNetRuntime Runtime { get; }
		public abstract void Initialize(DbgEvaluationInfo evalInfo, DmdMethodBody realMethodBody, VariablesProvider argumentsProvider, VariablesProvider localsProvider, bool canFuncEval);
		public abstract void Clear(DbgDotNetValue returnValue);
		public abstract DbgDotNetValue GetDotNetValue(ILValue value, DmdType targetType = null);
	}

	sealed class DebuggerRuntimeImpl : DebuggerRuntime2, IDebuggerRuntime {
		public override int PointerSize { get; }

		public override IDbgDotNetRuntime Runtime => runtime;
		readonly DbgObjectIdService dbgObjectIdService;
		readonly IDbgDotNetRuntime runtime;
		readonly DotNetClassHook[] anyClassHooks;
		readonly Dictionary<DmdTypeName, DotNetClassHook> classHooks;
		readonly List<DbgDotNetValue> valuesToDispose;
		readonly InterpreterLocalsProvider interpreterLocalsProvider;

		public DebuggerRuntimeImpl(DbgObjectIdService dbgObjectIdService, IDbgDotNetRuntime runtime, int pointerSize, DotNetClassHookFactory[] dotNetClassHookFactories) {
			if (dotNetClassHookFactories == null)
				throw new ArgumentNullException(nameof(dotNetClassHookFactories));
			this.dbgObjectIdService = dbgObjectIdService ?? throw new ArgumentNullException(nameof(dbgObjectIdService));
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			valuesToDispose = new List<DbgDotNetValue>();
			interpreterLocalsProvider = new InterpreterLocalsProvider(this);
			PointerSize = pointerSize;

			var anyClassHooksList = new List<DotNetClassHook>();
			classHooks = new Dictionary<DmdTypeName, DotNetClassHook>(DmdTypeNameEqualityComparer.Instance);
			foreach (var factory in dotNetClassHookFactories) {
				foreach (var info in factory.Create(this)) {
					Debug.Assert(info.Hook != null);
					if (info.WellKnownType == null && info.TypeName == null)
						anyClassHooksList.Add(info.Hook);
					else {
						DmdTypeName typeName;
						if (info.WellKnownType != null)
							typeName = DmdWellKnownTypeUtils.GetTypeName(info.WellKnownType.Value);
						else {
							Debug.Assert(info.TypeName != null);
							typeName = info.TypeName.Value;
						}
						Debug.Assert(!classHooks.ContainsKey(typeName));
						classHooks[typeName] = info.Hook;
					}
				}
			}
			anyClassHooks = anyClassHooksList.ToArray();
		}

		VariablesProvider DefaultArgumentsProvider => defaultArgumentsProvider ?? (defaultArgumentsProvider = new DefaultArgumentsProviderImpl(runtime));
		VariablesProvider defaultArgumentsProvider;
		VariablesProvider DefaultLocalsProvider => defaultLocalsProvider ?? (defaultLocalsProvider = new DefaultLocalsProviderImpl(runtime));
		VariablesProvider defaultLocalsProvider;

		VariablesProvider argumentsProvider;
		DbgEvaluationInfo evalInfo;
		bool canFuncEval;

		public override void Initialize(DbgEvaluationInfo evalInfo, DmdMethodBody realMethodBody, VariablesProvider argumentsProvider, VariablesProvider localsProvider, bool canFuncEval) {
			Debug.Assert(this.evalInfo == null);
			if (this.evalInfo != null)
				throw new InvalidOperationException();
			this.evalInfo = evalInfo;
			this.canFuncEval = canFuncEval;
			this.argumentsProvider = argumentsProvider ?? DefaultArgumentsProvider;
			interpreterLocalsProvider.Initialize(realMethodBody, localsProvider ?? DefaultLocalsProvider);
			Debug.Assert(valuesToDispose.Count == 0);
		}

		public override void Initialize(DmdMethodBase method, DmdMethodBody body) {
			argumentsProvider.Initialize(evalInfo, method, body);
			interpreterLocalsProvider.Initialize(evalInfo, method, body);
		}

		public override void Clear(DbgDotNetValue returnValue) {
			evalInfo = null;
			canFuncEval = false;
			foreach (var v in valuesToDispose) {
				if (v != returnValue && argumentsProvider.CanDispose(v) && interpreterLocalsProvider.CanDispose(v))
					v.Dispose();
			}
			valuesToDispose.Clear();
			argumentsProvider.Clear();
			interpreterLocalsProvider.Clear();
			argumentsProvider = null;
		}

		public override DbgDotNetValue GetDotNetValue(ILValue value, DmdType targetType = null) {
			targetType = targetType ?? value.Type;
			var dnValue = TryGetDotNetValue(value, value.IsNull ? targetType : value.Type, canCreateValue: true);
			if (dnValue != null)
				return dnValue;
			throw new InvalidOperationException();//TODO:
		}

		DbgDotNetValue TryCreateSyntheticValue(DmdType type, object value) {
			var dnValue = SyntheticValueFactory.TryCreateSyntheticValue(type, value);
			if (dnValue != null)
				RecordValue(dnValue);
			return dnValue;
		}

		DbgDotNetValue TryGetDotNetValue(ILValue value, bool canCreateValue) => TryGetDotNetValue(value, value.Type, canCreateValue);

		DbgDotNetValue TryGetDotNetValue(ILValue value, DmdType valueType, bool canCreateValue) {
			if (value is IDebuggerRuntimeILValue rtValue)
				return rtValue.GetDotNetValue();
			if (canCreateValue) {
				if (value.IsNull)
					return new SyntheticNullValue(valueType ?? evalInfo.Frame.Module.AppDomain.GetReflectionAppDomain().System_Void);

				object newValue;
				var type = valueType;
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
						else if (type == type.AppDomain.System_UIntPtr || type.IsPointer || type.IsFunctionPointer) {
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
				if (newValue != null) {
					var dnValue = TryCreateSyntheticValue(type, newValue);
					if (dnValue != null)
						return dnValue;
					return RecordValue(runtime.CreateValue(evalInfo, newValue));
				}
			}
			return null;
		}

		internal object GetDebuggerValue(ILValue value, DmdType targetType) {
			var dnValue = TryGetDotNetValue(value, targetType, canCreateValue: false);
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
					if (targetType.IsPointer || targetType.IsFunctionPointer || targetType == targetType.AppDomain.System_IntPtr) {
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

		internal ILValue CreateILValue(DbgDotNetValueResult result) {
			if (result.HasError)
				throw new InterpreterMessageException(result.ErrorMessage);
			if (result.ValueIsException)
				throw new InterpreterThrownExceptionException(result.Value);

			var dnValue = result.Value;
			if (dnValue == null)
				throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);

			return CreateILValue(dnValue);
		}

		internal DbgDotNetValue RecordValue(DbgDotNetValueResult result) {
			if (result.HasError)
				throw new InterpreterMessageException(result.ErrorMessage);
			if (result.ValueIsException)
				throw new InterpreterThrownExceptionException(result.Value);

			var dnValue = result.Value;
			if (dnValue == null)
				throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);

			return RecordValue(dnValue);
		}

		internal DbgDotNetValue RecordValue(DbgDotNetValue value) {
			try {
				evalInfo.CancellationToken.ThrowIfCancellationRequested();
				Debug.Assert(value != null);
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
				Debug.Assert(value != null);
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
			if (value.Type.IsPointer)
				return new PointerILValue(this, value);
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

		DbgDotNetValueResult GetArgument(int index) => argumentsProvider.GetVariable(index);
		string SetArgument(int index, DmdType targetType, object value) => argumentsProvider.SetVariable(index, targetType, value);

		DbgDotNetValueResult GetLocal(int index) => interpreterLocalsProvider.GetVariable(index);
		string SetLocal(int index, DmdType targetType, object value) => interpreterLocalsProvider.SetVariable(index, targetType, value);

		public override ILValue LoadArgument(int index) => CreateILValue(GetArgument(index));
		internal DbgDotNetValue LoadArgument2(int index) => RecordValue(GetArgument(index));

		public override ILValue LoadLocal(int index) => CreateILValue(GetLocal(index));
		internal DbgDotNetValue LoadLocal2(int index) => RecordValue(GetLocal(index));

		public override ILValue LoadArgumentAddress(int index, DmdType type) {
			var addrValue = argumentsProvider.GetValueAddress(index, type);
			if (addrValue != null) {
				Debug.Assert(addrValue.Type.IsByRef);
				return new ByRefILValueImpl(this, RecordValue(addrValue));
			}
			return new ArgumentAddress(this, type, index);
		}

		public override ILValue LoadLocalAddress(int index, DmdType type) {
			var addrValue = interpreterLocalsProvider.GetValueAddress(index, type);
			if (addrValue != null) {
				Debug.Assert(addrValue.Type.IsByRef);
				return new ByRefILValueImpl(this, RecordValue(addrValue));
			}
			return new LocalAddress(this, type, index);
		}

		public override bool StoreArgument(int index, DmdType type, ILValue value) => StoreArgument2(index, type, GetDebuggerValue(value, type));

		internal bool StoreArgument2(int index, DmdType targetType, object value) {
			var error = SetArgument(index, targetType, value);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
		}

		public override bool StoreLocal(int index, DmdType type, ILValue value) => StoreLocal2(index, type, GetDebuggerValue(value, type));

		internal bool StoreLocal2(int index, DmdType targetType, object value) {
			var error = SetLocal(index, targetType, value);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
		}

		public override ILValue CreateSZArray(DmdType elementType, long length) {
			if (length < 0 || length > int.MaxValue)
				return null;
			var res = runtime.CreateSZArray(evalInfo, elementType, (int)length);
			return CreateILValue(res);
		}

		public override ILValue CreateRuntimeTypeHandle(DmdType type) => new RuntimeTypeHandleILValue(this, type);
		internal DbgDotNetValue CreateRuntimeTypeHandleCore(DmdType type) {
			if (!canFuncEval)
				throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.FuncEvalDisabled);
			var appDomain = type.AppDomain;
			var methodGetType = appDomain.System_Type.GetMethod(nameof(Type.GetType), DmdSignatureCallingConvention.Default, 0, appDomain.System_Type, new[] { appDomain.System_String }, throwOnError: true);
			var typeValue = RecordValue(runtime.Call(evalInfo, null, methodGetType, new[] { type.AssemblyQualifiedName }, DbgDotNetInvokeOptions.None));

			var runtimeTypeHandleType = appDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle);
			var getTypeHandleMethod = typeValue.Type.GetMethod("get_" + nameof(Type.TypeHandle), DmdSignatureCallingConvention.Default | DmdSignatureCallingConvention.HasThis, 0, runtimeTypeHandleType, Array.Empty<DmdType>(), throwOnError: true);
			return RecordValue(runtime.Call(evalInfo, typeValue, getTypeHandleMethod, Array.Empty<object>(), DbgDotNetInvokeOptions.None));
		}

		public override ILValue CreateRuntimeFieldHandle(DmdFieldInfo field) => new RuntimeFieldHandleILValue(this, field);
		internal DbgDotNetValue CreateRuntimeFieldHandleCore(DmdFieldInfo field) {
			throw new NotImplementedException();//TODO:
		}

		public override ILValue CreateRuntimeMethodHandle(DmdMethodBase method) => new RuntimeMethodHandleILValue(this, method);
		internal DbgDotNetValue CreateRuntimeMethodHandleCore(DmdMethodBase method) {
			throw new NotImplementedException();//TODO:
		}

		DbgDotNetValue TryCreateDefaultValue(DmdType type) {
			switch (DmdType.GetTypeCode(type)) {
			case TypeCode.Boolean:	return TryCreateSyntheticValue(type, false);
			case TypeCode.Char:		return TryCreateSyntheticValue(type, '\0');
			case TypeCode.SByte:	return TryCreateSyntheticValue(type, (sbyte)0);
			case TypeCode.Byte:		return TryCreateSyntheticValue(type, (byte)0);
			case TypeCode.Int16:	return TryCreateSyntheticValue(type, (short)0);
			case TypeCode.UInt16:	return TryCreateSyntheticValue(type, (ushort)0);
			case TypeCode.Int32:	return TryCreateSyntheticValue(type, 0);
			case TypeCode.UInt32:	return TryCreateSyntheticValue(type, 0U);
			case TypeCode.Int64:	return TryCreateSyntheticValue(type, 0L);
			case TypeCode.UInt64:	return TryCreateSyntheticValue(type, 0UL);
			case TypeCode.Single:	return TryCreateSyntheticValue(type, 0f);
			case TypeCode.Double:	return TryCreateSyntheticValue(type, 0d);
			}
			if (type == type.AppDomain.System_IntPtr || type.IsPointer || type.IsFunctionPointer)
				return TryCreateSyntheticValue(type, IntPtr.Zero);
			if (type == type.AppDomain.System_UIntPtr)
				return TryCreateSyntheticValue(type, UIntPtr.Zero);
			return null;
		}

		internal DbgDotNetValue GetDefaultValue(DmdType type) {
			if (!type.IsValueType)
				return new SyntheticNullValue(type);
			var dnValue = TryCreateDefaultValue(type);
			if (dnValue != null)
				return dnValue;
			if (!canFuncEval)
				throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.FuncEvalDisabled);
			return RecordValue(runtime.CreateInstanceNoConstructor(evalInfo, type));
		}

		public override ILValue CreateTypeNoConstructor(DmdType type) {
			var dnValue = TryCreateDefaultValue(type);
			if (dnValue != null)
				return CreateILValue(dnValue);
			if (!canFuncEval)
				throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.FuncEvalDisabled);
			return CreateILValue(runtime.CreateInstanceNoConstructor(evalInfo, type));
		}

		public override ILValue Box(ILValue value, DmdType type) {
			if (type.IsValueType) {
				var dnValue = TryGetDotNetValue(value, type, canCreateValue: true) ?? throw new InvalidOperationException();
				var boxedValue = dnValue.Box(evalInfo) ?? runtime.Box(evalInfo, dnValue);
				RecordValue(boxedValue);
				return new BoxedValueTypeILValue(this, value, boxedValue.Value, type);
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
				res = runtime.CreateArray(evalInfo, ctor.ReflectedType.GetElementType(), dimensionInfos);
				return CreateILValue(res);

			case DmdSpecialMethodKind.Array_Constructor2:
				dimensionInfos = new DbgDotNetArrayDimensionInfo[arguments.Length / 2];
				for (int i = 0; i < dimensionInfos.Length; i++)
					dimensionInfos[i] = new DbgDotNetArrayDimensionInfo(ReadInt32(arguments[i * 2]), (uint)ReadInt32(arguments[i * 2 + 1]));
				res = runtime.CreateArray(evalInfo, ctor.ReflectedType.GetElementType(), dimensionInfos);
				return CreateILValue(res);

			default:
				res = CreateInstanceCore(ctor, arguments);
				return CreateILValue(res);
			}
		}

		DbgDotNetValueResult CreateInstanceCore(DmdConstructorInfo ctor, ILValue[] arguments) {
			if (ctor.IsStatic)
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);

			const DotNetClassHookCallOptions options = DotNetClassHookCallOptions.None;
			foreach (var anyHook in anyClassHooks) {
				var res = anyHook.CreateInstance(options, ctor, arguments);
				if (res != null)
					return DbgDotNetValueResult.Create(res);
			}

			var type = ctor.DeclaringType;
			if (type.IsConstructedGenericType)
				type = type.GetGenericTypeDefinition();
			var typeName = DmdTypeName.Create(type);
			if (classHooks.TryGetValue(typeName, out var hook)) {
				if (DmdWellKnownTypeUtils.TryGetWellKnownType(typeName, out var wellKnownType)) {
					if (type != type.AppDomain.GetWellKnownType(wellKnownType, isOptional: true))
						hook = null;
				}
				if (hook != null) {
					var res = hook.CreateInstance(options, ctor, arguments);
					if (res != null)
						return DbgDotNetValueResult.Create(res);
				}
			}

			if (!canFuncEval)
				throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.FuncEvalDisabled);
			return runtime.CreateInstance(evalInfo, ctor, Convert(arguments, ctor.GetMethodSignature().GetParameterTypes()), DbgDotNetInvokeOptions.None);
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
			var res = runtime.LoadField(evalInfo, null, field);
			return CreateILValue(res);
		}

		internal DbgDotNetValue LoadStaticField2(DmdFieldInfo field) {
			var res = runtime.LoadField(evalInfo, null, field);
			return RecordValue(res);
		}

		public override ILValue LoadStaticFieldAddress(DmdFieldInfo field) {
			var addrValue = runtime.LoadFieldAddress(evalInfo, null, field);
			if (addrValue != null) {
				Debug.Assert(addrValue.Type.IsByRef);
				return new ByRefILValueImpl(this, RecordValue(addrValue));
			}
			return new StaticFieldAddress(this, field);
		}

		public override bool StoreStaticField(DmdFieldInfo field, ILValue value) => StoreStaticField(field, GetDebuggerValue(value, field.FieldType));
		internal bool StoreStaticField(DmdFieldInfo field, object value) {
			var error = runtime.StoreField(evalInfo, null, field, value);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
		}

		public override ILValue LoadString(DmdType type, string value) {
			var stringValue = TryCreateSyntheticValue(type, value);
			if (stringValue == null)
				stringValue = RecordValue(runtime.CreateValue(evalInfo, value));
			return new ConstantStringILValueImpl(this, stringValue, value);
		}

		internal void SetArrayElementAt(DbgDotNetValue arrayValue, uint index, ILValue value) {
			var newValue = GetDebuggerValue(value, arrayValue.Type.GetElementType());
			SetArrayElementAt(arrayValue, index, newValue);
		}

		internal void SetArrayElementAt(DbgDotNetValue arrayValue, uint index, object value) {
			var error = arrayValue.SetArrayElementAt(evalInfo, index, value);
			if (error != null)
				throw new InterpreterMessageException(error);
		}

		public override int? CompareSigned(ILValue left, ILValue right) => null;
		public override int? CompareUnsigned(ILValue left, ILValue right) => null;

		public override bool? Equals(ILValue left, ILValue right) {
			if (left is AddressILValue laddr && right is AddressILValue raddr)
				return laddr.Equals(raddr);
			if (TryGetDotNetValue(left, canCreateValue: false) is DbgDotNetValue lv && TryGetDotNetValue(right, canCreateValue: false) is DbgDotNetValue rv) {
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
			var error = runtime.StoreField(evalInfo, objValue, field, value);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
		}

		internal ILValue LoadInstanceField(DbgDotNetValue objValue, DmdFieldInfo field) =>
			CreateILValue(runtime.LoadField(evalInfo, objValue, field));

		internal DbgDotNetValue LoadInstanceField2(DbgDotNetValue objValue, DmdFieldInfo field) =>
			RecordValue(runtime.LoadField(evalInfo, objValue, field));

		internal ILValue LoadValueTypeFieldAddress(AddressILValue objValue, DmdFieldInfo field) {
			Debug.Assert(field.ReflectedType.IsValueType);
			var dnObjValue = TryGetDotNetValue(objValue, canCreateValue: false);
			if (dnObjValue != null) {
				var addrValue = runtime.LoadFieldAddress(evalInfo, dnObjValue, field);
				if (addrValue != null) {
					Debug.Assert(addrValue.Type.IsByRef);
					return new ByRefILValueImpl(this, RecordValue(addrValue));
				}
			}
			return new ValueTypeFieldAddress(this, objValue, field);
		}

		internal ILValue LoadReferenceTypeFieldAddress(DbgDotNetValue objValue, DmdFieldInfo field) {
			Debug.Assert(!field.ReflectedType.IsValueType);
			var addrValue = runtime.LoadFieldAddress(evalInfo, objValue, field);
			if (addrValue != null) {
				Debug.Assert(addrValue.Type.IsByRef);
				return new ByRefILValueImpl(this, RecordValue(addrValue));
			}
			return new ReferenceTypeFieldAddress(this, objValue, field);
		}

		internal bool StoreIndirect(DbgDotNetValue refValue, object value) {
			Debug.Assert(refValue.Type.IsByRef || refValue.Type.IsPointer);
			var error = refValue.StoreIndirect(evalInfo, value);
			if (error != null)
				throw new InterpreterMessageException(error);
			return true;
		}

		internal bool CallInstance(DbgDotNetValue objValue, bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue returnValue) =>
			Call(objValue, isCallvirt, method, arguments, out returnValue);

		bool Call(DbgDotNetValue objValue, bool isCallvirt, DmdMethodBase method, ILValue[] arguments, out ILValue returnValue) {
			if (method.SpecialMethodKind != DmdSpecialMethodKind.Metadata)
				throw new InvalidOperationException();
			var res = CallCore(objValue, isCallvirt, method, arguments);
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

		DbgDotNetValueResult CallCore(DbgDotNetValue obj, bool isCallvirt, DmdMethodBase method, ILValue[] arguments) {
			var options = isCallvirt ? DotNetClassHookCallOptions.IsCallvirt : DotNetClassHookCallOptions.None;
			foreach (var anyHook in anyClassHooks) {
				var res = anyHook.Call(options, obj, method, arguments);
				if (res != null)
					return DbgDotNetValueResult.Create(res);
			}

			var type = method.DeclaringType;
			if (type.IsConstructedGenericType)
				type = type.GetGenericTypeDefinition();
			var typeName = DmdTypeName.Create(type);
			if (classHooks.TryGetValue(typeName, out var hook)) {
				if (DmdWellKnownTypeUtils.TryGetWellKnownType(typeName, out var wellKnownType)) {
					if (type != type.AppDomain.GetWellKnownType(wellKnownType, isOptional: true))
						hook = null;
				}
				if (hook != null) {
					var res = hook.Call(options, obj, method, arguments);
					if (res != null)
						return DbgDotNetValueResult.Create(res);
				}
			}

			if (!canFuncEval)
				throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.FuncEvalDisabled);
			var invokeOptions = isCallvirt ? DbgDotNetInvokeOptions.None : DbgDotNetInvokeOptions.NonVirtual;
			return runtime.Call(evalInfo, obj, method, Convert(arguments, method.GetMethodSignature().GetParameterTypes()), invokeOptions);
		}

		object[] Convert(ILValue[] values, ReadOnlyCollection<DmdType> targetTypes) {
			if (values.Length != targetTypes.Count)
				throw new InvalidOperationException();
			var res = values.Length == 0 ? Array.Empty<object>() : new object[values.Length];
			for (int i = 0; i < res.Length; i++)
				res[i] = GetDebuggerValue(values[i], targetTypes[i]);
			return res;
		}

		public override int GetSizeOfValueType(DmdType type) {
			Debug.Assert(type.IsValueType);
			Debug.Assert(!type.IsPrimitive);
			throw new NotImplementedException();//TODO:
		}

		internal int ToInt32(ILValue value) {
			if (value is ConstantInt32ILValue i32Value)
				return i32Value.Value;
			var dnValue = TryGetDotNetValue(value, canCreateValue: false);
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

		DbgDotNetValue IDebuggerRuntime.CreateValue(object value, DmdType targetType) {
			var res = TryCreateSyntheticValue(targetType, value);
			if (res != null)
				return res;
			return RecordValue(runtime.CreateValue(evalInfo, value));
		}

		char IDebuggerRuntime.ToChar(ILValue value) {
			if (value.Kind == ILValueKind.Int32)
				return (char)((ConstantInt32ILValue)value).Value;
			throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		int IDebuggerRuntime.ToInt32(ILValue value) {
			if (value.Kind == ILValueKind.Int32)
				return ((ConstantInt32ILValue)value).Value;
			throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		ulong IDebuggerRuntime.ToUInt64(ILValue value) {
			if (value.Kind == ILValueKind.Int64)
				return ((ConstantInt64ILValue)value).UnsignedValue;
			throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		string IDebuggerRuntime.ToString(ILValue value) {
			if (value is ConstantStringILValueImpl stringValue)
				return stringValue.Value;
			throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		DmdType IDebuggerRuntime.ToType(ILValue value) {
			//TODO:
			throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		Guid IDebuggerRuntime.ToGuid(ILValue value) {
			//TODO:
			throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		byte[] IDebuggerRuntime.ToByteArray(ILValue value) {
			//TODO:
			throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		DbgDotNetValue IDebuggerRuntime.ToDotNetValue(ILValue value) {
			var dnValue = TryGetDotNetValue(value, canCreateValue: false);
			if (dnValue != null)
				return dnValue;
			throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		DbgDotNetValue IDebuggerRuntime.GetException() {
			var value = runtime.GetException(evalInfo, DbgDotNetRuntimeConstants.ExceptionId);
			if (value == null)
				throw new InterpreterMessageException(dnSpy_Debugger_DotNet_Resources.NoExceptionOnTheCurrentThread);
			return value;
		}

		DbgDotNetValue IDebuggerRuntime.GetStowedException() {
			var value = runtime.GetStowedException(evalInfo, DbgDotNetRuntimeConstants.StowedExceptionId);
			if (value == null)
				throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			return value;
		}

		DbgDotNetValue IDebuggerRuntime.GetReturnValue(int index) {
			var value = runtime.GetReturnValue(evalInfo, (uint)index);
			if (value == null)
				throw new InterpreterMessageException(dnSpy_Debugger_DotNet_Resources.ReturnValueNotAvailable);
			return value;
		}

		DbgDotNetValue IDebuggerRuntime.GetObjectByAlias(string name) {
			evalInfo.Context.TryGetData(out DbgDotNetExpressionCompiler expressionCompiler);
			Debug.Assert(expressionCompiler != null);
			if (expressionCompiler == null)
				throw new InvalidOperationException();

			if (!expressionCompiler.TryGetAliasInfo(name, out var aliasInfo))
				aliasInfo = new DbgDotNetParsedAlias(DbgDotNetAliasKind.Variable, 0);

			DbgDotNetValue value;
			switch (aliasInfo.Kind) {
			case DbgDotNetAliasKind.Exception:
			case DbgDotNetAliasKind.StowedException:
			case DbgDotNetAliasKind.ReturnValue:
				// These can't be returned by this method
				break;

			case DbgDotNetAliasKind.Variable:
				//TODO:
				break;

			case DbgDotNetAliasKind.ObjectId:
				var objectId = dbgObjectIdService.GetObjectId(evalInfo.Runtime, aliasInfo.Id);
				if (objectId != null) {
					var dbgValue = objectId.GetValue(evalInfo);
					value = (DbgDotNetValue)dbgValue.InternalValue;
					return RecordValue(value);
				}
				break;

			default:
				throw new InvalidOperationException();
			}

			throw new InterpreterMessageException(dnSpy_Debugger_DotNet_Resources.UnknownVariableOrObjectId);
		}

		DbgDotNetValue IDebuggerRuntime.GetObjectAtAddress(ulong address) {
			//TODO:
			throw new InterpreterMessageException(dnSpy_Debugger_DotNet_Resources.NoDotNetObjectFoundAtAddress);
		}

		void IDebuggerRuntime.CreateVariable(DmdType type, string name, Guid customTypeInfoPayloadTypeId, byte[] customTypeInfoPayload) {
			//TODO:
			throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		DbgDotNetValue IDebuggerRuntime.GetVariableAddress(DmdType type, string name) {
			//TODO:
			throw new InterpreterMessageException(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		internal DbgDotNetValue CreateValue(object value, DmdType targetType) =>
			((IDebuggerRuntime)this).CreateValue(value, targetType);
	}
}
