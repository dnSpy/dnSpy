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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	sealed class DbgCorDebugInternalRuntimeImpl : DbgCorDebugInternalRuntime, IDbgDotNetRuntime {
		public override DbgRuntime Runtime { get; }
		public override DmdRuntime ReflectionRuntime { get; }
		public override CorDebugRuntimeVersion Version { get; }
		public override string ClrFilename { get; }
		public override string RuntimeDirectory { get; }
		public DbgDotNetDispatcher Dispatcher { get; }
		public bool SupportsObjectIds => true;

		readonly DbgEngineImpl engine;

		public DbgCorDebugInternalRuntimeImpl(DbgEngineImpl engine, DbgRuntime runtime, DmdRuntime reflectionRuntime, CorDebugRuntimeKind kind, string version, string clrPath, string runtimeDir) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			ReflectionRuntime = reflectionRuntime ?? throw new ArgumentNullException(nameof(reflectionRuntime));
			Version = new CorDebugRuntimeVersion(kind, version ?? throw new ArgumentNullException(nameof(version)));
			ClrFilename = clrPath ?? throw new ArgumentNullException(nameof(clrPath));
			RuntimeDirectory = runtimeDir ?? throw new ArgumentNullException(nameof(runtimeDir));
			Dispatcher = new DbgDotNetDispatcherImpl(engine);
			reflectionRuntime.GetOrCreateData(() => runtime);
		}

		sealed class GetFrameMethodState {
			public bool Initialized;
			public DmdMethodBase Method;
		}

		public DmdMethodBase GetFrameMethod(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetFrameMethodCore(context, frame, cancellationToken);
			return Dispatcher.Invoke(() => GetFrameMethodCore(context, frame, cancellationToken));
		}

		DmdMethodBase GetFrameMethodCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			var state = frame.GetOrCreateData<GetFrameMethodState>();
			if (!state.Initialized) {
				if (ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame)) {
					ilFrame.GetFrameMethodInfo(out var module, out var methodMetadataToken, out var genericTypeArguments, out var genericMethodArguments);
					var method = module.ResolveMethod(methodMetadataToken, (IList<DmdType>)null, null, DmdResolveOptions.ThrowOnError);
					if (genericTypeArguments.Count != 0) {
						var type = method.ReflectedType.MakeGenericType(genericTypeArguments);
						method = type.GetMethod(method.Module, method.MetadataToken, throwOnError: true);
					}
					if (genericMethodArguments.Count != 0)
						method = ((DmdMethodInfo)method).MakeGenericMethod(genericMethodArguments);
					state.Method = method;
				}
				state.Initialized = true;
			}
			return state.Method;
		}

		CorType GetType(CorAppDomain appDomain, DmdType type) => CorDebugTypeCreator.GetType(engine, appDomain, type);

		static CorValue TryGetObjectOrPrimitiveValue(CorValue value) {
			if (value == null)
				return null;
			if (value.IsReference) {
				if (value.IsNull)
					throw new InvalidOperationException();
				value = value.DereferencedValue;
				if (value == null)
					return null;
			}
			if (value.IsBox)
				value = value.BoxedValue;
			return value;
		}

		public DbgDotNetValueResult LoadField(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return LoadFieldCore(context, frame, obj, field, cancellationToken);
			return Dispatcher.Invoke(() => LoadFieldCore(context, frame, obj, field, cancellationToken));
		}

		DbgDotNetValueResult LoadFieldCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, CancellationToken cancellationToken) {
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			var appDomain = ilFrame.GetCorAppDomain();

			int hr;
			CorType corFieldDeclType;
			var fieldDeclType = field.DeclaringType;
			if (obj == null) {
				if (!field.IsStatic)
					return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);

				if (field.IsLiteral)
					return CreateSyntheticValue(field.FieldType, field.GetRawConstantValue());
				else {
					corFieldDeclType = GetType(appDomain, fieldDeclType);

					InitializeStaticConstructor(context, frame, ilFrame, fieldDeclType, corFieldDeclType, cancellationToken);
					var fieldValue = corFieldDeclType.GetStaticFieldValue((uint)field.MetadataToken, ilFrame.CorFrame, out hr);
					if (fieldValue == null) {
						if (hr == CordbgErrors.CORDBG_E_CLASS_NOT_LOADED || hr == CordbgErrors.CORDBG_E_STATIC_VAR_NOT_AVAILABLE) {
							//TODO: Create a synthetic value init'd to the default value (0s or null ref)
						}
					}
					if (fieldValue == null)
						return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
					return new DbgDotNetValueResult(engine.CreateDotNetValue_CorDebug(fieldValue, field.AppDomain, tryCreateStrongHandle: true), valueIsException: false);
				}
			}
			else {
				if (field.IsStatic)
					return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);

				var objImp = obj as DbgDotNetValueImpl ?? throw new InvalidOperationException();
				corFieldDeclType = GetType(appDomain, fieldDeclType);
				var objValue = TryGetObjectOrPrimitiveValue(objImp.TryGetCorValue());
				if (objValue == null)
					return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
				if (objValue.IsObject) {
					var fieldValue = objValue.GetFieldValue(corFieldDeclType.Class, (uint)field.MetadataToken, out hr);
					if (fieldValue == null)
						return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
					return new DbgDotNetValueResult(engine.CreateDotNetValue_CorDebug(fieldValue, field.AppDomain, tryCreateStrongHandle: true), valueIsException: false);
				}
				else {
					if (IsPrimitiveValueType(objValue.ElementType)) {
						//TODO:
					}
				}
			}

			return new DbgDotNetValueResult("NYI");//TODO:
		}

		static DbgDotNetValueResult CreateSyntheticValue(DmdType type, object constant) {
			var dnValue = SyntheticValueFactory.TryCreateSyntheticValue(type, constant);
			if (dnValue != null)
				return new DbgDotNetValueResult(dnValue, valueIsException: false);
			return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		sealed class StaticConstructorInitializedState {
			public volatile int Initialized;
		}

		void InitializeStaticConstructor(DbgEvaluationContext context, DbgStackFrame frame, ILDbgEngineStackFrame ilFrame, DmdType type, CorType corType, CancellationToken cancellationToken) {
			if (engine.CheckFuncEval(context) != null)
				return;
			var state = type.GetOrCreateData<StaticConstructorInitializedState>();
			if (state.Initialized > 0 || Interlocked.Increment(ref state.Initialized) != 1)
				return; 
			var cctor = type.TypeInitializer;
			if ((object)cctor != null) {
				foreach (var field in type.DeclaredFields) {
					if (!field.IsStatic || field.IsLiteral)
						continue;

					var fieldValue = corType.GetStaticFieldValue((uint)field.MetadataToken, ilFrame.CorFrame, out int hr);
					if (hr == CordbgErrors.CORDBG_E_CLASS_NOT_LOADED || hr == CordbgErrors.CORDBG_E_STATIC_VAR_NOT_AVAILABLE)
						break;
					if (fieldValue != null) {
						if (fieldValue.IsNull)
							continue;
						if (field.FieldType.IsValueType) {
							var objValue = fieldValue.DereferencedValue?.BoxedValue;
							var data = objValue?.ReadGenericValue();
							if (data != null && !IsZero(data))
								return;
						}
						else {
							// It's a reference type and not null, so the field has been initialized
							return;
						}
					}
				}

				if (HasNativeCode(cctor))
					return;
			}

			DbgDotNetValueResult res = default;
			try {
				res = engine.FuncEvalCreateInstanceNoCtor_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), type, cancellationToken);
				if (res.Value == null || res.ValueIsException)
					return;
				RuntimeHelpersRunClassConstructor(context, frame, ilFrame, type, corType, res.Value, cancellationToken);
			}
			finally {
				res.Value?.Dispose();
			}
		}

		bool HasNativeCode(DmdMethodBase method) {
			var reflectionAppDomain = method.AppDomain;
			var methodDbgModule = method.Module.GetDebuggerModule() ?? throw new InvalidOperationException();
			if (!engine.TryGetDnModule(methodDbgModule, out var methodModule))
				return false;
			var func = methodModule.CorModule.GetFunctionFromToken((uint)method.MetadataToken) ?? throw new InvalidOperationException();
			return func.NativeCode != null;
		}

		// Calls System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor():
		//		RuntimeHelpers.RunClassConstructor(obj.GetType().TypeHandle);
		void RuntimeHelpersRunClassConstructor(DbgEvaluationContext context, DbgStackFrame frame, ILDbgEngineStackFrame ilFrame, DmdType type, CorType corType, DbgDotNetValue objValue, CancellationToken cancellationToken) {
			var reflectionAppDomain = type.AppDomain;
			var getTypeMethod = objValue.Type.GetMethod(nameof(object.GetType), DmdSignatureCallingConvention.Default | DmdSignatureCallingConvention.HasThis, 0, reflectionAppDomain.System_Type, Array.Empty<DmdType>(), throwOnError: false);
			Debug.Assert((object)getTypeMethod != null);
			if ((object)getTypeMethod == null)
				return;
			var corAppDomain = ilFrame.GetCorAppDomain();
			var getTypeRes = engine.FuncEvalCall_CorDebug(context, frame.Thread, corAppDomain, getTypeMethod, objValue, Array.Empty<object>(), false, cancellationToken);
			if (getTypeRes.Value == null || getTypeRes.ValueIsException)
				return;
			var typeObj = getTypeRes.Value;
			var runtimeTypeHandleType = reflectionAppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle, isOptional: true);
			Debug.Assert((object)runtimeTypeHandleType != null);
			if ((object)runtimeTypeHandleType == null)
				return;
			var getTypeHandleMethod = typeObj.Type.GetMethod("get_" + nameof(Type.TypeHandle), DmdSignatureCallingConvention.Default | DmdSignatureCallingConvention.HasThis, 0, runtimeTypeHandleType, Array.Empty<DmdType>(), throwOnError: false);
			Debug.Assert((object)getTypeHandleMethod != null);
			if ((object)getTypeHandleMethod == null)
				return;
			var typeHandleRes = engine.FuncEvalCall_CorDebug(context, frame.Thread, corAppDomain, getTypeHandleMethod, typeObj, Array.Empty<object>(), false, cancellationToken);
			if (typeHandleRes.Value == null | typeHandleRes.ValueIsException)
				return;
			var runtimeHelpersType = reflectionAppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_CompilerServices_RuntimeHelpers, isOptional: true);
			var runClassConstructorMethod = runtimeHelpersType?.GetMethod(nameof(RuntimeHelpers.RunClassConstructor), DmdSignatureCallingConvention.Default, 0, reflectionAppDomain.System_Void, new[] { runtimeTypeHandleType }, throwOnError: false);
			Debug.Assert((object)runClassConstructorMethod != null);
			if ((object)runClassConstructorMethod == null)
				return;
			engine.FuncEvalCall_CorDebug(context, frame.Thread, corAppDomain, runClassConstructorMethod, null, new[] { typeHandleRes.Value }, false, cancellationToken);
		}

		static bool IsZero(byte[] a) {
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != 0)
					return false;
			}
			return true;
		}

		static bool IsPrimitiveValueType(CorElementType etype) {
			switch (etype) {
			case CorElementType.Boolean:
			case CorElementType.Char:
			case CorElementType.I1:
			case CorElementType.U1:
			case CorElementType.I2:
			case CorElementType.U2:
			case CorElementType.I4:
			case CorElementType.U4:
			case CorElementType.I8:
			case CorElementType.U8:
			case CorElementType.R4:
			case CorElementType.R8:
			case CorElementType.I:
			case CorElementType.U:
				return true;

			default:
				return false;
			}
		}

		public string StoreField(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, object value, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return StoreFieldCore(context, frame, obj, field, value, cancellationToken);
			return Dispatcher.Invoke(() => StoreFieldCore(context, frame, obj, field, value, cancellationToken));
		}

		string StoreFieldCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, object value, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return "NYI";//TODO:
		}

		public DbgDotNetValueResult Call(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CallCore(context, frame, obj, method, arguments, cancellationToken);
			return Dispatcher.Invoke(() => CallCore(context, frame, obj, method, arguments, cancellationToken));
		}

		DbgDotNetValueResult CallCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			return engine.FuncEvalCall_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), method, obj, arguments, newObj: false, cancellationToken: cancellationToken);
		}

		public DbgDotNetValueResult CreateInstance(DbgEvaluationContext context, DbgStackFrame frame, DmdConstructorInfo ctor, object[] arguments, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CreateInstanceCore(context, frame, ctor, arguments, cancellationToken);
			return Dispatcher.Invoke(() => CreateInstanceCore(context, frame, ctor, arguments, cancellationToken));
		}

		DbgDotNetValueResult CreateInstanceCore(DbgEvaluationContext context, DbgStackFrame frame, DmdConstructorInfo ctor, object[] arguments, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			return engine.FuncEvalCall_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), ctor, null, arguments, newObj: true, cancellationToken: cancellationToken);
		}

		public DbgDotNetValueResult CreateInstanceNoConstructor(DbgEvaluationContext context, DbgStackFrame frame, DmdType type, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CreateInstanceNoConstructorCore(context, frame, type, cancellationToken);
			return Dispatcher.Invoke(() => CreateInstanceNoConstructorCore(context, frame, type, cancellationToken));
		}

		DbgDotNetValueResult CreateInstanceNoConstructorCore(DbgEvaluationContext context, DbgStackFrame frame, DmdType type, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			return engine.FuncEvalCreateInstanceNoCtor_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), type, cancellationToken);
		}

		public DbgDotNetValueResult CreateSZArray(DbgEvaluationContext context, DbgStackFrame frame, DmdType elementType, int length, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CreateSZArrayCore(context, frame, elementType, length, cancellationToken);
			return Dispatcher.Invoke(() => CreateSZArrayCore(context, frame, elementType, length, cancellationToken));
		}

		DbgDotNetValueResult CreateSZArrayCore(DbgEvaluationContext context, DbgStackFrame frame, DmdType elementType, int length, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);//TODO:
		}

		public DbgDotNetValueResult CreateArray(DbgEvaluationContext context, DbgStackFrame frame, DmdType elementType, DbgDotNetArrayDimensionInfo[] dimensionInfos, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CreateArrayCore(context, frame, elementType, dimensionInfos, cancellationToken);
			return Dispatcher.Invoke(() => CreateArrayCore(context, frame, elementType, dimensionInfos, cancellationToken));
		}

		DbgDotNetValueResult CreateArrayCore(DbgEvaluationContext context, DbgStackFrame frame, DmdType elementType, DbgDotNetArrayDimensionInfo[] dimensionInfos, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);//TODO:
		}

		public DbgDotNetAliasInfo[] GetAliases(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetAliasesCore(context, frame, cancellationToken);
			return Dispatcher.Invoke(() => GetAliasesCore(context, frame, cancellationToken));
		}

		DbgDotNetAliasInfo[] GetAliasesCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return Array.Empty<DbgDotNetAliasInfo>();//TODO:
		}

		public DbgDotNetExceptionInfo[] GetExceptions(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetExceptionsCore(context, frame, cancellationToken);
			return Dispatcher.Invoke(() => GetExceptionsCore(context, frame, cancellationToken));
		}

		DbgDotNetExceptionInfo[] GetExceptionsCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			var dnThread = engine.GetThread(frame.Thread);
			var corValue = dnThread.CorThread.CurrentException;
			if (corValue == null)
				return Array.Empty<DbgDotNetExceptionInfo>();
			var reflectionAppDomain = frame.Thread.AppDomain.GetReflectionAppDomain() ?? throw new InvalidOperationException();
			var value = engine.CreateDotNetValue_CorDebug(corValue, reflectionAppDomain, tryCreateStrongHandle: true);
			const uint exceptionId = 1;
			return new[] { new DbgDotNetExceptionInfo(value, exceptionId, DbgDotNetExceptionInfoFlags.None) };
		}

		public DbgDotNetReturnValueInfo[] GetReturnValues(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetReturnValuesCore(context, frame, cancellationToken);
			return Dispatcher.Invoke(() => GetReturnValuesCore(context, frame, cancellationToken));
		}

		DbgDotNetReturnValueInfo[] GetReturnValuesCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return Array.Empty<DbgDotNetReturnValueInfo>();//TODO:
		}

		DbgDotNetValueResult CreateValue(CorValue value, ILDbgEngineStackFrame ilFrame) {
			var reflectionAppDomain = ilFrame.GetReflectionModule().AppDomain;
			var dnValue = engine.CreateDotNetValue_CorDebug(value, reflectionAppDomain, tryCreateStrongHandle: true);
			return new DbgDotNetValueResult(dnValue, valueIsException: false);
		}

		public DbgDotNetValueResult GetLocalValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetLocalValueCore(context, frame, index, cancellationToken);
			return Dispatcher.Invoke(() => GetLocalValueCore(context, frame, index, cancellationToken));
		}

		DbgDotNetValueResult GetLocalValueCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				throw new InvalidOperationException();
			var value = ilFrame.CorFrame.GetILLocal(index, out int hr);
			if (value == null)
				return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
			return CreateValue(value, ilFrame);
		}

		public DbgDotNetValueResult GetParameterValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetParameterValueCore(context, frame, index, cancellationToken);
			return Dispatcher.Invoke(() => GetParameterValueCore(context, frame, index, cancellationToken));
		}

		DbgDotNetValueResult GetParameterValueCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				throw new InvalidOperationException();
			var value = ilFrame.CorFrame.GetILArgument(index, out int hr);
			if (value == null)
				return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
			return CreateValue(value, ilFrame);
		}

		public bool CanCreateObjectId(DbgDotNetValue value) {
			var valueImpl = value as DbgDotNetValueImpl;
			if (valueImpl == null)
				return false;
			if (Dispatcher.CheckAccess())
				return CanCreateObjectIdCore(valueImpl);
			return Dispatcher.Invoke(() => CanCreateObjectIdCore(valueImpl));
		}

		bool CanCreateObjectIdCore(DbgDotNetValueImpl value) {
			Dispatcher.VerifyAccess();

			// Keep this in sync with CreateObjectIdCore()
			var corValue = value.TryGetCorValue();
			if (corValue == null)
				return false;
			if (!corValue.IsHandle) {
				if (corValue.IsReference) {
					if (corValue.IsNull)
						return false;
					corValue = corValue.DereferencedValue;
					if (corValue == null)
						return false;
				}
				if (!corValue.IsHeap2)
					return false;
			}

			return true;
		}

		public DbgDotNetObjectId CreateObjectId(DbgDotNetValue value, uint id) {
			var valueImpl = value as DbgDotNetValueImpl;
			if (valueImpl == null)
				return null;
			if (Dispatcher.CheckAccess())
				return CreateObjectIdCore(valueImpl, id);
			return Dispatcher.Invoke(() => CreateObjectIdCore(valueImpl, id));
		}

		DbgDotNetObjectId CreateObjectIdCore(DbgDotNetValueImpl value, uint id) {
			Dispatcher.VerifyAccess();

			// Keep this in sync with CanCreateObjectIdCore()
			var corValue = value.TryGetCorValue();
			if (corValue == null)
				return null;
			if (corValue.IsHandle) {
				var valueHolder = value.CorValueHolder.AddRef();
				try {
					return new DbgDotNetObjectIdImpl(valueHolder, id);
				}
				catch {
					valueHolder.Release();
					throw;
				}
			}
			else {
				if (corValue.IsReference) {
					if (corValue.IsNull)
						return null;
					corValue = corValue.DereferencedValue;
					if (corValue == null)
						return null;
				}
				var strongHandle = corValue.CreateHandle(CorDebugHandleType.HANDLE_STRONG);
				if (strongHandle == null)
					return null;
				try {
					return new DbgDotNetObjectIdImpl(new DbgCorValueHolder(engine, strongHandle, value.Type), id);
				}
				catch {
					strongHandle.DisposeHandle();
					throw;
				}
			}
		}

		public bool Equals(DbgDotNetObjectId objectId, DbgDotNetValue value) {
			var objectIdImpl = objectId as DbgDotNetObjectIdImpl;
			var valueImpl = value as DbgDotNetValueImpl;
			if (objectIdImpl == null || valueImpl == null)
				return false;
			if (Dispatcher.CheckAccess())
				return EqualsCore(objectIdImpl, valueImpl);
			return Dispatcher.Invoke(() => EqualsCore(objectIdImpl, valueImpl));
		}

		bool EqualsCore(DbgDotNetObjectIdImpl objectId, DbgDotNetValueImpl value) {
			Dispatcher.VerifyAccess();

			var idHolder = objectId.Value;
			var vHolder = value.CorValueHolder;
			if (idHolder == vHolder)
				return true;
			var v1 = GetValue(idHolder.CorValue);
			var v2 = GetValue(vHolder.CorValue);
			if (v1 == null || v2 == null)
				return false;
			if (v1.Address != v2.Address)
				return false;
			if (v1.Address == 0)
				return false;

			return true;
		}

		static CorValue GetValue(CorValue corValue) {
			if (corValue == null)
				return null;
			if (corValue.IsReference) {
				if (corValue.IsNull)
					return null;
				corValue = corValue.DereferencedValue;
			}
			return corValue;
		}

		public int GetHashCode(DbgDotNetObjectId objectId) {
			var objectIdImpl = objectId as DbgDotNetObjectIdImpl;
			if (objectIdImpl == null)
				return 0;
			if (Dispatcher.CheckAccess())
				return GetHashCodeCore(objectIdImpl);
			return Dispatcher.Invoke(() => GetHashCodeCore(objectIdImpl));
		}

		int GetHashCodeCore(DbgDotNetObjectIdImpl objectId) {
			Dispatcher.VerifyAccess();
			var v = GetValue(objectId.Value.CorValue);
			return GetHashCode(v);
		}

		static int GetHashCode(CorValue v) {
			if (v == null)
				return 0;
			var addr = v.Address;
			return addr == 0 ? 0 : addr.GetHashCode();
		}

		public int GetHashCode(DbgDotNetValue value) {
			var valueImpl = value as DbgDotNetValueImpl;
			if (valueImpl == null)
				return 0;
			if (Dispatcher.CheckAccess())
				return GetHashCodeCore(valueImpl);
			return Dispatcher.Invoke(() => GetHashCodeCore(valueImpl));
		}

		int GetHashCodeCore(DbgDotNetValueImpl value) {
			Dispatcher.VerifyAccess();
			var v = GetValue(value.TryGetCorValue());
			return GetHashCode(v);
		}

		public DbgDotNetValue GetValue(DbgEvaluationContext context, DbgDotNetObjectId objectId, CancellationToken cancellationToken) {
			var objectIdImpl = objectId as DbgDotNetObjectIdImpl;
			if (objectIdImpl == null)
				throw new ArgumentException();
			if (Dispatcher.CheckAccess())
				return GetValueCore(context, objectIdImpl, cancellationToken);
			return Dispatcher.Invoke(() => GetValueCore(context, objectIdImpl, cancellationToken));
		}

		DbgDotNetValue GetValueCore(DbgEvaluationContext context, DbgDotNetObjectIdImpl objectId, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			var dnValue = objectId.Value.AddRef();
			try {
				return new DbgDotNetValueImpl(engine, dnValue);
			}
			catch {
				dnValue.Release();
				throw;
			}
		}

		public bool? Equals(DbgDotNetValue a, DbgDotNetValue b) {
			if (a == b)
				return true;
			if (a.Type != b.Type)
				return false;
			var ai = a as DbgDotNetValueImpl;
			var bi = b as DbgDotNetValueImpl;
			if (ai == null || bi == null) {
				// If they're equal, they're both null
				return ai == bi ? (bool?)null : false;
			}
			if (Dispatcher.CheckAccess())
				return EqualsCore(ai, bi);
			return Dispatcher.Invoke(() => EqualsCore(ai, bi));
		}

		bool? EqualsCore(DbgDotNetValueImpl a, DbgDotNetValueImpl b) {
			Dispatcher.VerifyAccess();
			var addra = GetValue(a.TryGetCorValue())?.Address ?? 0;
			var addrb = GetValue(b.TryGetCorValue())?.Address ?? 0;
			if (addra != 0 && addrb != 0)
				return addra == addrb;
			if (addra == 0 && addrb == 0)
				return null;
			return false;
		}

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
