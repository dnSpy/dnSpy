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
using dnSpy.Contracts.Metadata;
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

		public ModuleId GetModuleId(DbgModule module) => engine.GetModuleId(module);

		sealed class GetFrameMethodState {
			public bool Initialized;
			public DmdMethodBase Method;
		}

		public DmdMethodBase GetFrameMethod(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetFrameMethodCore(context, frame, cancellationToken);
			return GetFrameMethod2(context, frame, cancellationToken);

			DmdMethodBase GetFrameMethod2(DbgEvaluationContext context2, DbgStackFrame frame2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetFrameMethodCore(context2, frame2, cancellationToken2));
		}

		DmdMethodBase GetFrameMethodCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			var state = frame.GetOrCreateData<GetFrameMethodState>();
			if (!state.Initialized) {
				cancellationToken.ThrowIfCancellationRequested();
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
			return LoadField2(context, frame, obj, field, cancellationToken);

			DbgDotNetValueResult LoadField2(DbgEvaluationContext context2, DbgStackFrame frame2, DbgDotNetValue obj2, DmdFieldInfo field2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => LoadFieldCore(context2, frame2, obj2, field2, cancellationToken2));
		}

		DbgDotNetValueResult LoadFieldCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
				var appDomain = ilFrame.GetCorAppDomain();

				int hr;
				CorType corFieldDeclType;
				CorValue fieldValue;
				var fieldDeclType = field.DeclaringType;
				if (obj == null) {
					if (!field.IsStatic)
						return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);

					if (field.IsLiteral)
						return CreateSyntheticValue(field.FieldType, field.GetRawConstantValue());
					else {
						corFieldDeclType = GetType(appDomain, fieldDeclType);

						InitializeStaticConstructor(context, frame, ilFrame, fieldDeclType, corFieldDeclType, cancellationToken);
						fieldValue = corFieldDeclType.GetStaticFieldValue((uint)field.MetadataToken, ilFrame.CorFrame, out hr);
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
						fieldValue = objValue.GetFieldValue(corFieldDeclType.Class, (uint)field.MetadataToken, out hr);
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
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			}
		}

		public string StoreField(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, object value, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return StoreFieldCore(context, frame, obj, field, value, cancellationToken);
			return StoreField2(context, frame, obj, field, value, cancellationToken);

			string StoreField2(DbgEvaluationContext context2, DbgStackFrame frame2, DbgDotNetValue obj2, DmdFieldInfo field2, object value2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => StoreFieldCore(context2, frame2, obj2, field2, value2, cancellationToken2));
		}

		string StoreFieldCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, object value, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					return CordbgErrorHelper.InternalError;
				var appDomain = ilFrame.GetCorAppDomain();

				CorType corFieldDeclType;
				var fieldDeclType = field.DeclaringType;
				if (obj == null) {
					if (!field.IsStatic)
						return CordbgErrorHelper.InternalError;

					if (field.IsLiteral)
						return CordbgErrorHelper.InternalError;
					else {
						corFieldDeclType = GetType(appDomain, fieldDeclType);

						InitializeStaticConstructor(context, frame, ilFrame, fieldDeclType, corFieldDeclType, cancellationToken);
						Func<CreateCorValueResult> createTargetValue = () => {
							var fieldValue = corFieldDeclType.GetStaticFieldValue((uint)field.MetadataToken, ilFrame.CorFrame, out var hr);
							return new CreateCorValueResult(fieldValue, hr);
						};
						return engine.StoreValue_CorDebug(context, frame.Thread, ilFrame, createTargetValue, field.FieldType, value, cancellationToken);
					}
				}
				else {
					if (field.IsStatic)
						return CordbgErrorHelper.InternalError;

					var objImp = obj as DbgDotNetValueImpl ?? throw new InvalidOperationException();
					corFieldDeclType = GetType(appDomain, fieldDeclType);
					var objValue = TryGetObjectOrPrimitiveValue(objImp.TryGetCorValue());
					if (objValue == null)
						return CordbgErrorHelper.InternalError;
					if (objValue.IsObject) {
						Func<CreateCorValueResult> createTargetValue = () => {
							// Re-read it since it could've gotten neutered
							var objValue2 = TryGetObjectOrPrimitiveValue(objImp.TryGetCorValue());
							Debug.Assert(objValue2?.IsObject == true);
							if (objValue2 == null)
								return new CreateCorValueResult(null, -1);
							var fieldValue = objValue2.GetFieldValue(corFieldDeclType.Class, (uint)field.MetadataToken, out var hr);
							return new CreateCorValueResult(fieldValue, hr);
						};
						return engine.StoreValue_CorDebug(context, frame.Thread, ilFrame, createTargetValue, field.FieldType, value, cancellationToken);
					}
					else {
						if (IsPrimitiveValueType(objValue.ElementType)) {
							//TODO:
						}
					}
				}

				return "NYI";//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return CordbgErrorHelper.InternalError;
			}
		}

		static DbgDotNetValueResult CreateSyntheticValue(DmdType type, object constant) {
			var dnValue = SyntheticValueFactory.TryCreateSyntheticValue(type, constant);
			if (dnValue != null)
				return new DbgDotNetValueResult(dnValue, valueIsException: false);
			return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
		}

		sealed class StaticConstructorInitializedState {
			public volatile int Initialized;
		}

		void InitializeStaticConstructor(DbgEvaluationContext context, DbgStackFrame frame, ILDbgEngineStackFrame ilFrame, DmdType type, CorType corType, CancellationToken cancellationToken) {
			if (engine.CheckFuncEval(context) != null)
				return;
			var state = type.GetOrCreateData<StaticConstructorInitializedState>();
			if (state.Initialized > 0 || Interlocked.Exchange(ref state.Initialized, 1) != 0)
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
		bool RuntimeHelpersRunClassConstructor(DbgEvaluationContext context, DbgStackFrame frame, ILDbgEngineStackFrame ilFrame, DmdType type, CorType corType, DbgDotNetValue objValue, CancellationToken cancellationToken) {
			var reflectionAppDomain = type.AppDomain;
			var getTypeMethod = objValue.Type.GetMethod(nameof(object.GetType), DmdSignatureCallingConvention.Default | DmdSignatureCallingConvention.HasThis, 0, reflectionAppDomain.System_Type, Array.Empty<DmdType>(), throwOnError: false);
			Debug.Assert((object)getTypeMethod != null);
			if ((object)getTypeMethod == null)
				return false;
			var corAppDomain = ilFrame.GetCorAppDomain();
			var getTypeRes = engine.FuncEvalCall_CorDebug(context, frame.Thread, corAppDomain, getTypeMethod, objValue, Array.Empty<object>(), false, cancellationToken);
			if (getTypeRes.Value == null || getTypeRes.ValueIsException)
				return false;
			var typeObj = getTypeRes.Value;
			var runtimeTypeHandleType = reflectionAppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle, isOptional: true);
			Debug.Assert((object)runtimeTypeHandleType != null);
			if ((object)runtimeTypeHandleType == null)
				return false;
			var getTypeHandleMethod = typeObj.Type.GetMethod("get_" + nameof(Type.TypeHandle), DmdSignatureCallingConvention.Default | DmdSignatureCallingConvention.HasThis, 0, runtimeTypeHandleType, Array.Empty<DmdType>(), throwOnError: false);
			Debug.Assert((object)getTypeHandleMethod != null);
			if ((object)getTypeHandleMethod == null)
				return false;
			var typeHandleRes = engine.FuncEvalCall_CorDebug(context, frame.Thread, corAppDomain, getTypeHandleMethod, typeObj, Array.Empty<object>(), false, cancellationToken);
			if (typeHandleRes.Value == null | typeHandleRes.ValueIsException)
				return false;
			var runtimeHelpersType = reflectionAppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_CompilerServices_RuntimeHelpers, isOptional: true);
			var runClassConstructorMethod = runtimeHelpersType?.GetMethod(nameof(RuntimeHelpers.RunClassConstructor), DmdSignatureCallingConvention.Default, 0, reflectionAppDomain.System_Void, new[] { runtimeTypeHandleType }, throwOnError: false);
			Debug.Assert((object)runClassConstructorMethod != null);
			if ((object)runClassConstructorMethod == null)
				return false;
			var res = engine.FuncEvalCall_CorDebug(context, frame.Thread, corAppDomain, runClassConstructorMethod, null, new[] { typeHandleRes.Value }, false, cancellationToken);
			res.Value?.Dispose();
			return !res.HasError && !res.ValueIsException;
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

		public DbgDotNetValueResult Call(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CallCore(context, frame, obj, method, arguments, cancellationToken);
			return Call2(context, frame, obj, method, arguments, cancellationToken);

			DbgDotNetValueResult Call2(DbgEvaluationContext context2, DbgStackFrame frame2, DbgDotNetValue obj2, DmdMethodBase method2, object[] arguments2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => CallCore(context2, frame2, obj2, method2, arguments2, cancellationToken2));
		}

		DbgDotNetValueResult CallCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
				return engine.FuncEvalCall_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), method, obj, arguments, newObj: false, cancellationToken: cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			}
		}

		public DbgDotNetValueResult CreateInstance(DbgEvaluationContext context, DbgStackFrame frame, DmdConstructorInfo ctor, object[] arguments, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CreateInstanceCore(context, frame, ctor, arguments, cancellationToken);
			return CreateInstance2(context, frame, ctor, arguments, cancellationToken);

			DbgDotNetValueResult CreateInstance2(DbgEvaluationContext context2, DbgStackFrame frame2, DmdConstructorInfo ctor2, object[] arguments2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => CreateInstanceCore(context2, frame2, ctor2, arguments2, cancellationToken2));
		}

		DbgDotNetValueResult CreateInstanceCore(DbgEvaluationContext context, DbgStackFrame frame, DmdConstructorInfo ctor, object[] arguments, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
				return engine.FuncEvalCall_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), ctor, null, arguments, newObj: true, cancellationToken: cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			}
		}

		public DbgDotNetValueResult CreateInstanceNoConstructor(DbgEvaluationContext context, DbgStackFrame frame, DmdType type, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CreateInstanceNoConstructorCore(context, frame, type, cancellationToken);
			return CreateInstanceNoConstructor2(context, frame, type, cancellationToken);

			DbgDotNetValueResult CreateInstanceNoConstructor2(DbgEvaluationContext context2, DbgStackFrame frame2, DmdType type2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => CreateInstanceNoConstructorCore(context2, frame2, type2, cancellationToken2));
		}

		DbgDotNetValueResult CreateInstanceNoConstructorCore(DbgEvaluationContext context, DbgStackFrame frame, DmdType type, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
				return engine.FuncEvalCreateInstanceNoCtor_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), type, cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			}
		}

		public DbgDotNetValueResult CreateSZArray(DbgEvaluationContext context, DbgStackFrame frame, DmdType elementType, int length, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CreateSZArrayCore(context, frame, elementType, length, cancellationToken);
			return CreateSZArray2(context, frame, elementType, length, cancellationToken);

			DbgDotNetValueResult CreateSZArray2(DbgEvaluationContext context2, DbgStackFrame frame2, DmdType elementType2, int length2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => CreateSZArrayCore(context2, frame2, elementType2, length2, cancellationToken2));
		}

		DbgDotNetValueResult CreateSZArrayCore(DbgEvaluationContext context, DbgStackFrame frame, DmdType elementType, int length, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			try {
				if (!CanCallNewParameterizedArray(elementType))
					return CreateSZArrayCore_Array_CreateInstance(context, frame, elementType, length, cancellationToken);
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
				return engine.CreateSZArray_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), elementType, length, cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			}
		}

		DbgDotNetValueResult CreateSZArrayCore_Array_CreateInstance(DbgEvaluationContext context, DbgStackFrame frame, DmdType elementType, int length, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			Debug.Assert(!CanCallNewParameterizedArray(elementType));
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);

			// Execute this code:
			//	var elementType = Type.GetType(elementType.AssemblyQualifiedName);
			//	return Array.CreateInstance(elementType, length);

			var appDomain = elementType.AppDomain;
			DbgDotNetValue typeElementType = null;
			try {
				var methodGetType = appDomain.System_Type.GetMethod(nameof(Type.GetType), DmdSignatureCallingConvention.Default, 0, appDomain.System_Type, new[] { appDomain.System_String }, throwOnError: true);
				var res = engine.FuncEvalCall_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), methodGetType, null, new[] { elementType.AssemblyQualifiedName }, false, cancellationToken);
				if (res.HasError || res.ValueIsException)
					return res;
				typeElementType = res.Value;
				if (res.Value.IsNull)
					return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);

				var methodCreateInstance = appDomain.System_Array.GetMethod(nameof(Array.CreateInstance), DmdSignatureCallingConvention.Default, 0, appDomain.System_Array, new[] { appDomain.System_Type, appDomain.System_Int32 }, throwOnError: true);
				return engine.FuncEvalCall_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), methodCreateInstance, null, new object[] { typeElementType, length }, false, cancellationToken);
			}
			finally {
				typeElementType?.Dispose();
			}
		}

		// ICorDebugEval2.NewParameterizedArray() can only create arrays of reference types or
		// of primitive value types, but not including IntPtr/UIntPtr, see coreclr code, funceval.cpp, case DB_IPCE_FET_NEW_ARRAY:
		//		// Gotta be a primitive, class, or System.Object.
		//		if (((et < ELEMENT_TYPE_BOOLEAN) || (et > ELEMENT_TYPE_R8)) &&
		//			!IsElementTypeSpecial(et)) // <-- Class,Object,Array,SZArray,String
		//		{
		//			COMPlusThrow(kArgumentOutOfRangeException, W("ArgumentOutOfRange_Enum"));
		static bool CanCallNewParameterizedArray(DmdType elementType) {
			switch (elementType.TypeSignatureKind) {
			case DmdTypeSignatureKind.SZArray:
			case DmdTypeSignatureKind.MDArray:
				return true;

			case DmdTypeSignatureKind.Pointer:
			case DmdTypeSignatureKind.ByRef:
			case DmdTypeSignatureKind.TypeGenericParameter:
			case DmdTypeSignatureKind.MethodGenericParameter:
			case DmdTypeSignatureKind.FunctionPointer:
				return false;

			case DmdTypeSignatureKind.Type:
			case DmdTypeSignatureKind.GenericInstance:
				if (!elementType.IsValueType)
					return true;
				if (elementType.IsEnum)
					return false;

				var tc = DmdType.GetTypeCode(elementType);
				if (TypeCode.Boolean <= tc && tc <= TypeCode.Double)
					return true;

				return false;

			default:
				throw new InvalidOperationException();
			}
		}

		public DbgDotNetValueResult CreateArray(DbgEvaluationContext context, DbgStackFrame frame, DmdType elementType, DbgDotNetArrayDimensionInfo[] dimensionInfos, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CreateArrayCore(context, frame, elementType, dimensionInfos, cancellationToken);
			return CreateArray2(context, frame, elementType, dimensionInfos, cancellationToken);

			DbgDotNetValueResult CreateArray2(DbgEvaluationContext context2, DbgStackFrame frame2, DmdType elementType2, DbgDotNetArrayDimensionInfo[] dimensionInfos2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => CreateArrayCore(context2, frame2, elementType2, dimensionInfos2, cancellationToken2));
		}

		DbgDotNetValueResult CreateArrayCore(DbgEvaluationContext context, DbgStackFrame frame, DmdType elementType, DbgDotNetArrayDimensionInfo[] dimensionInfos, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);

			// There's no ICorDebugEval method that can create multi-dimensional arrays so
			// we have to use Array.CreateInstance(Type, int[], int[]). This method has a
			// problem, whenever rank == 1 and lower bounds == 0, it always creates an
			// SZ array..., see coreclr code: ArrayNative::CreateInstance

			var appDomain = elementType.AppDomain;
			DbgDotNetValue typeElementType = null;
			try {
				var methodGetType = appDomain.System_Type.GetMethod(nameof(Type.GetType), DmdSignatureCallingConvention.Default, 0, appDomain.System_Type, new[] { appDomain.System_String }, throwOnError: true);
				var res = engine.FuncEvalCall_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), methodGetType, null, new[] { elementType.AssemblyQualifiedName }, false, cancellationToken);
				if (res.HasError || res.ValueIsException)
					return res;
				typeElementType = res.Value;
				if (res.Value.IsNull)
					return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);

				var lengths = new int[dimensionInfos.Length];
				var lowerBounds = new int[dimensionInfos.Length];
				for (int i = 0; i < dimensionInfos.Length; i++) {
					lengths[i] = (int)dimensionInfos[i].Length;
					lowerBounds[i] = dimensionInfos[i].BaseIndex;
				}

				var methodCreateInstance = appDomain.System_Array.GetMethod(nameof(Array.CreateInstance), DmdSignatureCallingConvention.Default, 0, appDomain.System_Array, new[] { appDomain.System_Type, appDomain.System_Int32.MakeArrayType(), appDomain.System_Int32.MakeArrayType() }, throwOnError: true);
				res = engine.FuncEvalCall_CorDebug(context, frame.Thread, ilFrame.GetCorAppDomain(), methodCreateInstance, null, new object[] { typeElementType, lengths, lowerBounds }, false, cancellationToken);
				if (res.HasError || res.ValueIsException)
					return res;

				// Verify that it's not an SZ array
				if (res.Value.Type.IsSZArray) {
					res.Value.Dispose();
					return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
				}

				return res;
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			}
			finally {
				typeElementType?.Dispose();
			}
		}

		public DbgDotNetAliasInfo[] GetAliases(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetAliasesCore(context, frame, cancellationToken);
			return GetAliases2(context, frame, cancellationToken);

			DbgDotNetAliasInfo[] GetAliases2(DbgEvaluationContext context2, DbgStackFrame frame2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetAliasesCore(context2, frame2, cancellationToken2));
		}

		DbgDotNetAliasInfo[] GetAliasesCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return Array.Empty<DbgDotNetAliasInfo>();//TODO:
		}

		public DbgDotNetExceptionInfo[] GetExceptions(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetExceptionsCore(context, frame, cancellationToken);
			return GetExceptions2(context, frame, cancellationToken);

			DbgDotNetExceptionInfo[] GetExceptions2(DbgEvaluationContext context2, DbgStackFrame frame2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetExceptionsCore(context2, frame2, cancellationToken2));
		}

		DbgDotNetExceptionInfo[] GetExceptionsCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
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
			return GetReturnValues2(context, frame, cancellationToken);

			DbgDotNetReturnValueInfo[] GetReturnValues2(DbgEvaluationContext context2, DbgStackFrame frame2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetReturnValuesCore(context2, frame2, cancellationToken2));
		}

		DbgDotNetReturnValueInfo[] GetReturnValuesCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
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
			return GetLocalValue2(context, frame, index, cancellationToken);

			DbgDotNetValueResult GetLocalValue2(DbgEvaluationContext context2, DbgStackFrame frame2, uint index2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetLocalValueCore(context2, frame2, index2, cancellationToken2));
		}

		DbgDotNetValueResult GetLocalValueCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					throw new InvalidOperationException();
				var value = ilFrame.CorFrame.GetILLocal(index, out int hr);
				if (value == null)
					return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
				return CreateValue(value, ilFrame);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			}
		}

		public DbgDotNetValueResult GetParameterValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetParameterValueCore(context, frame, index, cancellationToken);
			return GetParameterValue2(context, frame, index, cancellationToken);

			DbgDotNetValueResult GetParameterValue2(DbgEvaluationContext context2, DbgStackFrame frame2, uint index2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetParameterValueCore(context2, frame2, index2, cancellationToken2));
		}

		DbgDotNetValueResult GetParameterValueCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					throw new InvalidOperationException();
				var value = ilFrame.CorFrame.GetILArgument(index, out int hr);
				if (value == null)
					return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
				return CreateValue(value, ilFrame);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			}
		}

		public string SetLocalValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, object value, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return SetLocalValueCore(context, frame, index, targetType, value, cancellationToken);
			return SetLocalValue2(context, frame, index, targetType, value, cancellationToken);

			string SetLocalValue2(DbgEvaluationContext context2, DbgStackFrame frame2, uint index2, DmdType targetType2, object value2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => SetLocalValueCore(context2, frame2, index2, targetType2, value2, cancellationToken2));
		}

		string SetLocalValueCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, object value, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					throw new InvalidOperationException();
				return engine.SetLocalValue_CorDebug(context, frame.Thread, ilFrame, index, targetType, value, cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return CordbgErrorHelper.InternalError;
			}
		}

		public string SetParameterValue(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, object value, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return SetParameterValueCore(context, frame, index, targetType, value, cancellationToken);
			return SetParameterValue2(context, frame, index, targetType, value, cancellationToken);

			string SetParameterValue2(DbgEvaluationContext context2, DbgStackFrame frame2, uint index2, DmdType targetType2, object value2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => SetParameterValueCore(context2, frame2, index2, targetType2, value2, cancellationToken2));
		}

		string SetParameterValueCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, object value, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					throw new InvalidOperationException();
				return engine.SetParameterValue_CorDebug(context, frame.Thread, ilFrame, index, targetType, value, cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return CordbgErrorHelper.InternalError;
			}
		}

		public DbgDotNetCreateValueResult CreateValue(DbgEvaluationContext context, DbgStackFrame frame, object value, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CreateValueCore(context, frame, value, cancellationToken);
			return CreateValue2(context, frame, value, cancellationToken);

			DbgDotNetCreateValueResult CreateValue2(DbgEvaluationContext context2, DbgStackFrame frame2, object value2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => CreateValueCore(context2, frame2, value2, cancellationToken2));
		}

		DbgDotNetCreateValueResult CreateValueCore(DbgEvaluationContext context, DbgStackFrame frame, object value, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					throw new InvalidOperationException();
				return engine.CreateValue_CorDebug(context, frame.Thread, ilFrame, value, cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetCreateValueResult(CordbgErrorHelper.InternalError);
			}
		}

		public bool CanCreateObjectId(DbgDotNetValue value) {
			var valueImpl = value as DbgDotNetValueImpl;
			if (valueImpl == null)
				return false;
			if (Dispatcher.CheckAccess())
				return CanCreateObjectIdCore(valueImpl);
			return CanCreateObjectId2(valueImpl);

			bool CanCreateObjectId2(DbgDotNetValueImpl value2) =>
				Dispatcher.InvokeRethrow(() => CanCreateObjectIdCore(value2));
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
			return CreateObjectId2(valueImpl, id);

			DbgDotNetObjectId CreateObjectId2(DbgDotNetValueImpl value2, uint id2) =>
				Dispatcher.InvokeRethrow(() => CreateObjectIdCore(value2, id2));
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
					engine.DisposeHandle_CorDebug(strongHandle);
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
			return Equals2(objectIdImpl, valueImpl);

			bool Equals2(DbgDotNetObjectIdImpl objectId2, DbgDotNetValueImpl value2) =>
				Dispatcher.InvokeRethrow(() => EqualsCore(objectId2, value2));
		}

		struct EquatableValue {
			public readonly ulong Address;

			public EquatableValue(DmdType type, CorValue value) {
				if (value == null)
					Address = 0;
				else if (type.IsByRef)
					Address = value.ReferenceAddress;
				else {
					if (value.IsReference)
						value = value.DereferencedValue;
					Address = value?.Address ?? 0;
				}
			}

			public bool Equals2(EquatableValue other) => Address != 0 && Address == other.Address;
			public bool? Equals3(EquatableValue other) => Address == 0 && other.Address == 0 ? (bool?)null : Address == other.Address;
			public new int GetHashCode() => Address == 0 ? 0 : Address.GetHashCode();
		}

		bool EqualsCore(DbgDotNetObjectIdImpl objectId, DbgDotNetValueImpl value) {
			Dispatcher.VerifyAccess();

			var idHolder = objectId.Value;
			var vHolder = value.CorValueHolder;
			if (idHolder == vHolder)
				return true;
			var v1 = GetEquatableValue(idHolder.Type, idHolder.CorValue);
			var v2 = GetEquatableValue(vHolder.Type, vHolder.CorValue);
			return v1.Equals2(v2);
		}

		static EquatableValue GetEquatableValue(DmdType type, CorValue corValue) => new EquatableValue(type, corValue);

		public int GetHashCode(DbgDotNetObjectId objectId) {
			var objectIdImpl = objectId as DbgDotNetObjectIdImpl;
			if (objectIdImpl == null)
				return 0;
			if (Dispatcher.CheckAccess())
				return GetHashCodeCore(objectIdImpl);
			return GetHashCode2(objectIdImpl);

			int GetHashCode2(DbgDotNetObjectIdImpl objectId2) =>
				Dispatcher.InvokeRethrow(() => GetHashCodeCore(objectId2));
		}

		int GetHashCodeCore(DbgDotNetObjectIdImpl objectId) {
			Dispatcher.VerifyAccess();
			return GetEquatableValue(objectId.Value.Type, objectId.Value.CorValue).GetHashCode();
		}

		public int GetHashCode(DbgDotNetValue value) {
			var valueImpl = value as DbgDotNetValueImpl;
			if (valueImpl == null)
				return 0;
			if (Dispatcher.CheckAccess())
				return GetHashCodeCore(valueImpl);
			return GetHashCode2(valueImpl);

			int GetHashCode2(DbgDotNetValueImpl value2) =>
				Dispatcher.InvokeRethrow(() => GetHashCodeCore(value2));
		}

		int GetHashCodeCore(DbgDotNetValueImpl value) {
			Dispatcher.VerifyAccess();
			return GetEquatableValue(value.Type, value.TryGetCorValue()).GetHashCode();
		}

		public DbgDotNetValue GetValue(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetObjectId objectId, CancellationToken cancellationToken) {
			var objectIdImpl = objectId as DbgDotNetObjectIdImpl;
			if (objectIdImpl == null)
				throw new ArgumentException();
			if (Dispatcher.CheckAccess())
				return GetValueCore(context, objectIdImpl, cancellationToken);
			return GetValue2(context, objectIdImpl, cancellationToken);

			DbgDotNetValue GetValue2(DbgEvaluationContext context2, DbgDotNetObjectIdImpl objectId2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetValueCore(context2, objectId2, cancellationToken2));
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
			return Equals2(ai, bi);

			bool? Equals2(DbgDotNetValueImpl a2, DbgDotNetValueImpl b2) =>
				Dispatcher.InvokeRethrow(() => EqualsCore(a2, b2));
		}

		bool? EqualsCore(DbgDotNetValueImpl a, DbgDotNetValueImpl b) {
			Dispatcher.VerifyAccess();
			return GetEquatableValue(a.Type, a.TryGetCorValue()).Equals3(GetEquatableValue(b.Type, b.TryGetCorValue()));
		}

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
