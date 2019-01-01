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
using System.Runtime.CompilerServices;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Disassembly;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Disassembly;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.CallStack;
using dnSpy.Debugger.DotNet.Mono.Impl.Evaluation.Hooks;
using dnSpy.Debugger.DotNet.Mono.Properties;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	sealed class DbgMonoDebugInternalRuntimeImpl : DbgMonoDebugInternalRuntime, IDbgDotNetRuntime, IMonoDebugRuntime {
		public override MonoDebugRuntimeKind Kind { get; }
		public override DmdRuntime ReflectionRuntime { get; }
		public override DbgRuntime Runtime { get; }
		public DbgDotNetDispatcher Dispatcher { get; }
		public DbgDotNetRuntimeFeatures Features { get; }

		IMonoDebugValueConverter IMonoDebugRuntime.ValueConverter => monoDebugValueConverter;

		readonly DbgEngineImpl engine;
		readonly Dictionary<DmdWellKnownType, ClassHook> classHooks;
		readonly IMonoDebugValueConverter monoDebugValueConverter;

		public DbgMonoDebugInternalRuntimeImpl(DbgEngineImpl engine, DbgRuntime runtime, DmdRuntime reflectionRuntime, MonoDebugRuntimeKind monoDebugRuntimeKind) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			ReflectionRuntime = reflectionRuntime ?? throw new ArgumentNullException(nameof(reflectionRuntime));
			Kind = monoDebugRuntimeKind;
			Dispatcher = new DbgDotNetDispatcherImpl(engine);
			Features = CalculateFeatures(engine.MonoVirtualMachine);
			reflectionRuntime.GetOrCreateData(() => runtime);

			monoDebugValueConverter = new MonoDebugValueConverterImpl(this);
			classHooks = new Dictionary<DmdWellKnownType, ClassHook>();
			foreach (var info in ClassHookProvider.Create(engine, this)) {
				Debug.Assert(info.Hook != null);
				Debug.Assert(!classHooks.ContainsKey(info.WellKnownType));
				classHooks.Add(info.WellKnownType, info.Hook);
			}
		}

		static DbgDotNetRuntimeFeatures CalculateFeatures(VirtualMachine vm) {
			var res = DbgDotNetRuntimeFeatures.ObjectIds | DbgDotNetRuntimeFeatures.NoDereferencePointers;
			if (!vm.Version.AtLeast(2, 24))
				res |= DbgDotNetRuntimeFeatures.NoGenericMethods;
			// We need FuncEvalOptions.ReturnOutThis support so func-eval of Task/ObjectIdForDebugger
			// prop updates the struct's task field
			if (!vm.Version.AtLeast(2, 35))
				res |= DbgDotNetRuntimeFeatures.NoAsyncStepObjectId;
			return res;
		}

		public ModuleId GetModuleId(DbgModule module) => engine.GetModuleId(module);

		public DbgDotNetRawModuleBytes GetRawModuleBytes(DbgModule module) {
			if (!module.IsDynamic)
				return DbgDotNetRawModuleBytes.None;
			if (Dispatcher.CheckAccess())
				return GetRawModuleBytesCore(module);
			return GetRawModuleBytesCore2(module);

			DbgDotNetRawModuleBytes GetRawModuleBytesCore2(DbgModule module2) {
				if (!Dispatcher.TryInvokeRethrow(() => GetRawModuleBytesCore(module2), out var result))
					result = DbgDotNetRawModuleBytes.None;
				return result;
			}
		}

		DbgDotNetRawModuleBytes GetRawModuleBytesCore(DbgModule module) {
			Dispatcher.VerifyAccess();
			if (!module.IsDynamic)
				return DbgDotNetRawModuleBytes.None;

			return DbgDotNetRawModuleBytes.None;//TODO:
		}

		public bool TryGetMethodToken(DbgModule module, int methodToken, out int metadataMethodToken, out int metadataLocalVarSigTok) {
			if (!module.IsDynamic) {
				metadataMethodToken = 0;
				metadataLocalVarSigTok = 0;
				return false;
			}

			if (Dispatcher.CheckAccess())
				return TryGetMethodTokenCore(module, methodToken, out metadataMethodToken, out metadataLocalVarSigTok);
			return TryGetMethodTokenCore2(module, methodToken, out metadataMethodToken, out metadataLocalVarSigTok);

			bool TryGetMethodTokenCore2(DbgModule module2, int methodToken2, out int metadataMethodToken2, out int metadataLocalVarSigTok2) {
				int tmpMetadataMethodToken = 0, tmpMetadataLocalVarSigTok = 0;
				if (!Dispatcher.TryInvokeRethrow(() => {
					var res = TryGetMethodTokenCore(module2, methodToken2, out var metadataMethodToken3, out var metadataLocalVarSigTok3);
					tmpMetadataMethodToken = metadataMethodToken3;
					tmpMetadataLocalVarSigTok = metadataLocalVarSigTok3;
					return res;
				}, out var result)) {
					metadataMethodToken2 = 0;
					metadataLocalVarSigTok2 = 0;
					return false;
				}
				metadataMethodToken2 = tmpMetadataMethodToken;
				metadataLocalVarSigTok2 = tmpMetadataLocalVarSigTok;
				return result;
			}
		}

		bool TryGetMethodTokenCore(DbgModule module, int methodToken, out int metadataMethodToken, out int metadataLocalVarSigTok) {
			Dispatcher.VerifyAccess();

			//TODO:
			metadataMethodToken = 0;
			metadataLocalVarSigTok = 0;
			return false;
		}

		sealed class GetFrameMethodState {
			public bool Initialized;
			public DmdMethodBase Method;
		}

		public DmdMethodBase GetFrameMethod(DbgEvaluationInfo evalInfo) {
			if (Dispatcher.CheckAccess())
				return GetFrameMethodCore(evalInfo);
			return GetFrameMethod2(evalInfo);

			DmdMethodBase GetFrameMethod2(DbgEvaluationInfo evalInfo2) {
				Dispatcher.TryInvokeRethrow(() => GetFrameMethodCore(evalInfo2), out var result);
				return result;
			}
		}

		DmdMethodBase GetFrameMethodCore(DbgEvaluationInfo evalInfo) {
			Dispatcher.VerifyAccess();
			var state = evalInfo.Frame.GetOrCreateData<GetFrameMethodState>();
			if (!state.Initialized) {
				evalInfo.CancellationToken.ThrowIfCancellationRequested();
				if (ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame)) {
					ilFrame.GetFrameMethodInfo(out var module, out var methodMetadataToken, out var genericTypeArguments, out var genericMethodArguments);
					// Don't throw if it fails to resolve. Callers must be able to handle null return values
					state.Method = TryGetMethod(module, methodMetadataToken, genericTypeArguments, genericMethodArguments);
				}
				state.Initialized = true;
			}
			return state.Method;
		}

		static DmdMethodBase TryGetMethod(DmdModule module, int methodMetadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments) {
			var method = module?.ResolveMethod(methodMetadataToken, (IList<DmdType>)null, null, DmdResolveOptions.None);
			if ((object)method != null) {
				if (genericTypeArguments.Count != 0) {
					var type = method.ReflectedType.MakeGenericType(genericTypeArguments);
					method = type.GetMethod(method.Module, method.MetadataToken, throwOnError: true);
				}
				if (genericMethodArguments.Count != 0)
					method = ((DmdMethodInfo)method).MakeGenericMethod(genericMethodArguments);
			}
			return method;
		}

		TypeMirror GetType(DbgEvaluationInfo evalInfo, DmdType type) =>
			MonoDebugTypeCreator.GetType(engine, type, engine.CreateMonoTypeLoader(evalInfo));

		public DbgDotNetValue LoadFieldAddress(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field) => null;

		public DbgDotNetValueResult LoadField(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field) {
			if (Dispatcher.CheckAccess())
				return LoadFieldCore(evalInfo, obj, field);
			return LoadField2(evalInfo, obj, field);

			DbgDotNetValueResult LoadField2(DbgEvaluationInfo evalInfo2, DbgDotNetValue obj2, DmdFieldInfo field2) {
				if (!Dispatcher.TryInvokeRethrow(() => LoadFieldCore(evalInfo2, obj2, field2), out var result))
					result = DbgDotNetValueResult.CreateError(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgDotNetValueResult LoadFieldCore(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field) {
			Dispatcher.VerifyAccess();
			try {
				var info = GetFieldValueLocationCore(evalInfo, obj, field);
				if (info.errorMessage != null)
					return DbgDotNetValueResult.CreateError(info.errorMessage);
				return DbgDotNetValueResult.Create(engine.CreateDotNetValue_MonoDebug(info.valueLocation));
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		(ValueLocation valueLocation, string errorMessage) GetFieldValueLocationCore(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field) {
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
				return (null, PredefinedEvaluationErrorMessages.InternalDebuggerError);

			var fieldDeclType = field.DeclaringType;
			bool canNotAccessField = !engine.MonoVirtualMachine.Version.AtLeast(2, 15) && fieldDeclType.ContainsGenericParameters;
			var monoFieldDeclType = GetType(evalInfo, fieldDeclType);
			if (obj == null) {
				if (!field.IsStatic)
					return (null, PredefinedEvaluationErrorMessages.InternalDebuggerError);

				if (field.IsLiteral) {
					var monoValue = MonoValueFactory.TryCreateSyntheticValue(engine, monoFieldDeclType.Assembly.Domain, field.FieldType, field.GetRawConstantValue()) ??
						new PrimitiveValue(monoFieldDeclType.VirtualMachine, ElementType.Object, null);
					return (new NoValueLocation(field.FieldType, monoValue), null);
				}
				else {
					if (canNotAccessField)
						return (null, dnSpy_Debugger_DotNet_Mono_Resources.Error_CannotAccessMemberRuntimeLimitations);
					var monoField = MemberMirrorUtils.GetMonoField(monoFieldDeclType, field);

					InitializeStaticConstructor(evalInfo, ilFrame, fieldDeclType, monoFieldDeclType);
					return (new StaticFieldValueLocation(field.FieldType, ilFrame.MonoFrame.Thread, monoField), null);
				}
			}
			else {
				if (field.IsStatic)
					return (null, PredefinedEvaluationErrorMessages.InternalDebuggerError);

				var objImp = obj as DbgDotNetValueImpl ?? throw new InvalidOperationException();
				var monoField = MemberMirrorUtils.GetMonoField(monoFieldDeclType, field);
				switch (objImp.Value) {
				case ObjectMirror om:
					if (canNotAccessField)
						return (null, dnSpy_Debugger_DotNet_Mono_Resources.Error_CannotAccessMemberRuntimeLimitations);
					return (new ReferenceTypeFieldValueLocation(field.FieldType, om, monoField), null);

				case StructMirror sm:
					return (new ValueTypeFieldValueLocation(field.FieldType, objImp.ValueLocation, sm, monoField), null);

				case PrimitiveValue pv:
					return (null, "NYI");//TODO:

				default:
					// Unreachable
					return (null, PredefinedEvaluationErrorMessages.InternalDebuggerError);
				}
			}
		}

		sealed class StaticConstructorInitializedState {
			public volatile int Initialized;
		}

		void InitializeStaticConstructor(DbgEvaluationInfo evalInfo, ILDbgEngineStackFrame ilFrame, DmdType type, TypeMirror monoType) {
			if (engine.CheckFuncEval(evalInfo.Context) != null)
				return;
			var state = type.GetOrCreateData<StaticConstructorInitializedState>();
			if (state.Initialized > 0 || Interlocked.Exchange(ref state.Initialized, 1) != 0)
				return;
			if (engine.MonoVirtualMachine.Version.AtLeast(2, 23) && monoType.IsInitialized)
				return;
			var cctor = type.TypeInitializer;
			if ((object)cctor != null) {
				var fields = type.DeclaredFields;
				for (int i = 0; i < fields.Count; i++) {
					var field = fields[i];
					if (!field.IsStatic || field.IsLiteral)
						continue;

					var monoField = MemberMirrorUtils.GetMonoField(monoType, fields, i);
					Value fieldValue;
					try {
						fieldValue = monoType.GetValue(monoField, ilFrame.MonoFrame.Thread);
					}
					catch {
						break;
					}
					if (fieldValue != null) {
						if (fieldValue is PrimitiveValue pv && pv.Value == null)
							continue;
						if (field.FieldType.IsValueType) {
							if (!IsZero(fieldValue, 0))
								return;
						}
						else {
							// It's a reference type and not null, so the field has been initialized
							return;
						}
					}
				}
			}

			DbgDotNetValue dnObjValue = null;
			try {
				var reflectionAppDomain = type.AppDomain;
				var monoObjValue = monoType.GetTypeObject();
				var objValueType = engine.GetReflectionType(reflectionAppDomain, monoObjValue.Type, null);
				var objValueLocation = new NoValueLocation(objValueType, monoObjValue);
				dnObjValue = engine.CreateDotNetValue_MonoDebug(objValueLocation);
				RuntimeHelpersRunClassConstructor(evalInfo, type, dnObjValue);
			}
			finally {
				dnObjValue?.Dispose();
			}
		}

		// Calls System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor():
		//		RuntimeHelpers.RunClassConstructor(obj.GetType().TypeHandle);
		bool RuntimeHelpersRunClassConstructor(DbgEvaluationInfo evalInfo, DmdType type, DbgDotNetValue objValue) {
			DbgDotNetValueResult typeHandleRes = default;
			DbgDotNetValueResult res = default;
			try {
				var reflectionAppDomain = type.AppDomain;
				var runtimeTypeHandleType = reflectionAppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle, isOptional: true);
				Debug.Assert((object)runtimeTypeHandleType != null);
				if ((object)runtimeTypeHandleType == null)
					return false;
				var getTypeHandleMethod = objValue.Type.GetMethod("get_" + nameof(Type.TypeHandle), DmdSignatureCallingConvention.Default | DmdSignatureCallingConvention.HasThis, 0, runtimeTypeHandleType, Array.Empty<DmdType>(), throwOnError: false);
				Debug.Assert((object)getTypeHandleMethod != null);
				if ((object)getTypeHandleMethod == null)
					return false;
				typeHandleRes = engine.FuncEvalCall_MonoDebug(evalInfo, getTypeHandleMethod, objValue, Array.Empty<object>(), DbgDotNetInvokeOptions.None, false);
				if (typeHandleRes.Value == null || typeHandleRes.ValueIsException)
					return false;
				var runtimeHelpersType = reflectionAppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_CompilerServices_RuntimeHelpers, isOptional: true);
				var runClassConstructorMethod = runtimeHelpersType?.GetMethod(nameof(RuntimeHelpers.RunClassConstructor), DmdSignatureCallingConvention.Default, 0, reflectionAppDomain.System_Void, new[] { runtimeTypeHandleType }, throwOnError: false);
				Debug.Assert((object)runClassConstructorMethod != null);
				if ((object)runClassConstructorMethod == null)
					return false;
				res = engine.FuncEvalCall_MonoDebug(evalInfo, runClassConstructorMethod, null, new[] { typeHandleRes.Value }, DbgDotNetInvokeOptions.None, false);
				return !res.HasError && !res.ValueIsException;
			}
			finally {
				typeHandleRes.Value?.Dispose();
				res.Value?.Dispose();
			}
		}

		bool IsZero(Value value, int recursionCounter) {
			if (recursionCounter > 100)
				return false;
			if (value is PrimitiveValue pv) {
				if (pv.Value == null)
					return true;
				switch (pv.Type) {
				case ElementType.Boolean:	return !(bool)pv.Value;
				case ElementType.Char:		return (char)pv.Value == '\0';
				case ElementType.I1:		return (sbyte)pv.Value == 0;
				case ElementType.U1:		return (byte)pv.Value == 0;
				case ElementType.I2:		return (short)pv.Value == 0;
				case ElementType.U2:		return (ushort)pv.Value == 0;
				case ElementType.I4:		return (int)pv.Value == 0;
				case ElementType.U4:		return (uint)pv.Value == 0;
				case ElementType.I8:		return (long)pv.Value == 0;
				case ElementType.U8:		return (ulong)pv.Value == 0;
				case ElementType.R4:		return (float)pv.Value == 0;
				case ElementType.R8:		return (double)pv.Value == 0;
				case ElementType.I:
				case ElementType.U:
				case ElementType.Ptr:		return (long)pv.Value == 0;
				case ElementType.Object:	return true;// It's a null value
				default:					throw new InvalidOperationException();
				}
			}
			if (value is StructMirror sm) {
				foreach (var f in sm.Fields) {
					if (!IsZero(f, recursionCounter + 1))
						return false;
				}
				return true;
			}
			Debug.Assert(value is ObjectMirror);
			// It's a non-null object reference
			return false;
		}

		public string StoreField(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field, object value) {
			if (Dispatcher.CheckAccess())
				return StoreFieldCore(evalInfo, obj, field, value);
			return StoreField2(evalInfo, obj, field, value);

			string StoreField2(DbgEvaluationInfo evalInfo2, DbgDotNetValue obj2, DmdFieldInfo field2, object value2) {
				if (!Dispatcher.TryInvokeRethrow(() => StoreFieldCore(evalInfo2, obj2, field2, value2), out var result))
					result = DispatcherConstants.ProcessExitedError;
				return result;
			}
		}

		string StoreFieldCore(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field, object value) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			try {
				var info = GetFieldValueLocationCore(evalInfo, obj, field);
				if (info.errorMessage != null)
					return info.errorMessage;
				var res = engine.CreateMonoValue_MonoDebug(evalInfo, value, field.FieldType);
				if (res.ErrorMessage != null)
					return res.ErrorMessage;
				return info.valueLocation.Store(res.Value);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return PredefinedEvaluationErrorMessages.InternalDebuggerError;
			}
		}

		public DbgDotNetValueResult Call(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, DbgDotNetInvokeOptions invokeOptions) {
			if (Dispatcher.CheckAccess())
				return CallCore(evalInfo, obj, method, arguments, invokeOptions);
			return Call2(evalInfo, obj, method, arguments, invokeOptions);

			DbgDotNetValueResult Call2(DbgEvaluationInfo evalInfo2, DbgDotNetValue obj2, DmdMethodBase method2, object[] arguments2, DbgDotNetInvokeOptions invokeOptions2) {
				if (!Dispatcher.TryInvokeRethrow(() => CallCore(evalInfo2, obj2, method2, arguments2, invokeOptions2), out var result))
					result = DbgDotNetValueResult.CreateError(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgDotNetValueResult CallCore(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, DbgDotNetInvokeOptions invokeOptions) {
			Dispatcher.VerifyAccess();
			try {
				var type = method.DeclaringType;
				if (type.IsConstructedGenericType)
					type = type.GetGenericTypeDefinition();
				var typeName = DmdTypeName.Create(type);
				if (DmdWellKnownTypeUtils.TryGetWellKnownType(typeName, out var wellKnownType)) {
					if (classHooks.TryGetValue(wellKnownType, out var hook) && type == type.AppDomain.GetWellKnownType(wellKnownType, isOptional: true)) {
						var res = hook.Call(obj, method, arguments);
						if (res != null)
							return DbgDotNetValueResult.Create(res);
					}
				}

				return engine.FuncEvalCall_MonoDebug(evalInfo, method, obj, arguments, invokeOptions, newObj: false);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		public DbgDotNetValueResult CreateInstance(DbgEvaluationInfo evalInfo, DmdConstructorInfo ctor, object[] arguments, DbgDotNetInvokeOptions invokeOptions) {
			if (Dispatcher.CheckAccess())
				return CreateInstanceCore(evalInfo, ctor, arguments, invokeOptions);
			return CreateInstance2(evalInfo, ctor, arguments, invokeOptions);

			DbgDotNetValueResult CreateInstance2(DbgEvaluationInfo evalInfo2, DmdConstructorInfo ctor2, object[] arguments2, DbgDotNetInvokeOptions invokeOptions2) {
				if (!Dispatcher.TryInvokeRethrow(() => CreateInstanceCore(evalInfo2, ctor2, arguments2, invokeOptions2), out var result))
					result = DbgDotNetValueResult.CreateError(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgDotNetValueResult CreateInstanceCore(DbgEvaluationInfo evalInfo, DmdConstructorInfo ctor, object[] arguments, DbgDotNetInvokeOptions invokeOptions) {
			Dispatcher.VerifyAccess();
			try {
				var type = ctor.DeclaringType;
				if (type.IsConstructedGenericType)
					type = type.GetGenericTypeDefinition();
				var typeName = DmdTypeName.Create(type);
				if (DmdWellKnownTypeUtils.TryGetWellKnownType(typeName, out var wellKnownType)) {
					if (classHooks.TryGetValue(wellKnownType, out var hook) && type == type.AppDomain.GetWellKnownType(wellKnownType, isOptional: true)) {
						var res = hook.CreateInstance(ctor, arguments);
						if (res != null)
							return DbgDotNetValueResult.Create(res);
					}
				}

				return engine.FuncEvalCall_MonoDebug(evalInfo, ctor, null, arguments, invokeOptions, newObj: true);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		public DbgDotNetValueResult CreateInstanceNoConstructor(DbgEvaluationInfo evalInfo, DmdType type) {
			if (Dispatcher.CheckAccess())
				return CreateInstanceNoConstructorCore(evalInfo, type);
			return CreateInstanceNoConstructor2(evalInfo, type);

			DbgDotNetValueResult CreateInstanceNoConstructor2(DbgEvaluationInfo evalInfo2, DmdType type2) {
				if (!Dispatcher.TryInvokeRethrow(() => CreateInstanceNoConstructorCore(evalInfo2, type2), out var result))
					result = DbgDotNetValueResult.CreateError(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgDotNetValueResult CreateInstanceNoConstructorCore(DbgEvaluationInfo evalInfo, DmdType type) {
			Dispatcher.VerifyAccess();
			try {
				if (type.IsValueType)
					return CreateValueTypeInstanceNoConstructorCore(evalInfo, type);
				return CreateReferenceTypeInstanceNoConstructorCore(evalInfo, type);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		DbgDotNetValueResult CreateValueTypeInstanceNoConstructorCore(DbgEvaluationInfo evalInfo, DmdType type) {
			var structMirror = engine.CreateValueType(evalInfo, type, 0);
			return DbgDotNetValueResult.Create(engine.CreateDotNetValue_MonoDebug(type.AppDomain, structMirror, type));
		}

		DbgDotNetValueResult CreateReferenceTypeInstanceNoConstructorCore(DbgEvaluationInfo evalInfo, DmdType type) {
			if (engine.MonoVirtualMachine.Version.AtLeast(2, 31)) {
				var monoType = GetType(evalInfo, type);
				var value = monoType.NewInstance();
				return DbgDotNetValueResult.Create(engine.CreateDotNetValue_MonoDebug(type.AppDomain, value, type));
			}
			else {
				var ctor = type.GetMethod(DmdConstructorInfo.ConstructorName, DmdSignatureCallingConvention.HasThis, 0, type.AppDomain.System_Void, Array.Empty<DmdType>(), throwOnError: false) as DmdConstructorInfo;
				if ((object)ctor == null)
					return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
				return CreateInstanceCore(evalInfo, ctor, Array.Empty<object>(), DbgDotNetInvokeOptions.None);
			}
		}

		public DbgDotNetValueResult CreateSZArray(DbgEvaluationInfo evalInfo, DmdType elementType, int length) {
			if (Dispatcher.CheckAccess())
				return CreateSZArrayCore(evalInfo, elementType, length);
			return CreateSZArray2(evalInfo, elementType, length);

			DbgDotNetValueResult CreateSZArray2(DbgEvaluationInfo evalInfo2, DmdType elementType2, int length2) {
				if (!Dispatcher.TryInvokeRethrow(() => CreateSZArrayCore(evalInfo2, elementType2, length2), out var result))
					result = DbgDotNetValueResult.CreateError(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgDotNetValueResult CreateSZArrayCore(DbgEvaluationInfo evalInfo, DmdType elementType, int length) {
			Dispatcher.VerifyAccess();
			var appDomain = elementType.AppDomain;
			DbgDotNetValue typeElementType = null;
			try {
				typeElementType = engine.CreateDotNetValue_MonoDebug(appDomain, GetType(evalInfo, elementType).GetTypeObject(), null);
				var methodCreateInstance = appDomain.System_Array.GetMethod(nameof(Array.CreateInstance), DmdSignatureCallingConvention.Default, 0, appDomain.System_Array, new[] { appDomain.System_Type, appDomain.System_Int32 }, throwOnError: true);
				return engine.FuncEvalCall_MonoDebug(evalInfo, methodCreateInstance, null, new object[] { typeElementType, length }, DbgDotNetInvokeOptions.None, false);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
			finally {
				typeElementType?.Dispose();
			}
		}

		public DbgDotNetValueResult CreateArray(DbgEvaluationInfo evalInfo, DmdType elementType, DbgDotNetArrayDimensionInfo[] dimensionInfos) {
			if (Dispatcher.CheckAccess())
				return CreateArrayCore(evalInfo, elementType, dimensionInfos);
			return CreateArray2(evalInfo, elementType, dimensionInfos);

			DbgDotNetValueResult CreateArray2(DbgEvaluationInfo evalInfo2, DmdType elementType2, DbgDotNetArrayDimensionInfo[] dimensionInfos2) {
				if (!Dispatcher.TryInvokeRethrow(() => CreateArrayCore(evalInfo2, elementType2, dimensionInfos2), out var result))
					result = DbgDotNetValueResult.CreateError(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgDotNetValueResult CreateArrayCore(DbgEvaluationInfo evalInfo, DmdType elementType, DbgDotNetArrayDimensionInfo[] dimensionInfos) {
			Dispatcher.VerifyAccess();
			var appDomain = elementType.AppDomain;
			DbgDotNetValue typeElementType = null;
			try {
				var lengths = new int[dimensionInfos.Length];
				var lowerBounds = new int[dimensionInfos.Length];
				for (int i = 0; i < dimensionInfos.Length; i++) {
					lengths[i] = (int)dimensionInfos[i].Length;
					lowerBounds[i] = dimensionInfos[i].BaseIndex;
				}

				typeElementType = engine.CreateDotNetValue_MonoDebug(appDomain, GetType(evalInfo, elementType).GetTypeObject(), null);
				var methodCreateInstance = appDomain.System_Array.GetMethod(nameof(Array.CreateInstance), DmdSignatureCallingConvention.Default, 0, appDomain.System_Array, new[] { appDomain.System_Type, appDomain.System_Int32.MakeArrayType(), appDomain.System_Int32.MakeArrayType() }, throwOnError: true);
				return engine.FuncEvalCall_MonoDebug(evalInfo, methodCreateInstance, null, new object[] { typeElementType, lengths, lowerBounds }, DbgDotNetInvokeOptions.None, false);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
			finally {
				typeElementType?.Dispose();
			}
		}

		public DbgDotNetAliasInfo[] GetAliases(DbgEvaluationInfo evalInfo) {
			if (Dispatcher.CheckAccess())
				return GetAliasesCore(evalInfo);
			return GetAliases2(evalInfo);

			DbgDotNetAliasInfo[] GetAliases2(DbgEvaluationInfo evalInfo2) {
				if (!Dispatcher.TryInvokeRethrow(() => GetAliasesCore(evalInfo2), out var result))
					result = Array.Empty<DbgDotNetAliasInfo>();
				return result;
			}
		}

		DbgDotNetAliasInfo[] GetAliasesCore(DbgEvaluationInfo evalInfo) {
			Dispatcher.VerifyAccess();

			DbgDotNetValue exception = null;
			DbgDotNetValue stowedException = null;
			var returnValues = Array.Empty<DbgDotNetReturnValueInfo>();
			try {
				exception = GetExceptionCore(evalInfo, DbgDotNetRuntimeConstants.ExceptionId);
				stowedException = GetStowedExceptionCore(evalInfo, DbgDotNetRuntimeConstants.StowedExceptionId);
				returnValues = GetReturnValuesCore(evalInfo);

				int count = (exception != null ? 1 : 0) + (stowedException != null ? 1 : 0) + returnValues.Length + (returnValues.Length != 0 ? 1 : 0);
				if (count == 0)
					return Array.Empty<DbgDotNetAliasInfo>();

				var res = new DbgDotNetAliasInfo[count];
				int w = 0;
				if (exception != null)
					res[w++] = new DbgDotNetAliasInfo(DbgDotNetAliasInfoKind.Exception, exception.Type, DbgDotNetRuntimeConstants.ExceptionId, Guid.Empty, null);
				if (stowedException != null)
					res[w++] = new DbgDotNetAliasInfo(DbgDotNetAliasInfoKind.StowedException, stowedException.Type, DbgDotNetRuntimeConstants.StowedExceptionId, Guid.Empty, null);
				if (returnValues.Length != 0) {
					res[w++] = new DbgDotNetAliasInfo(DbgDotNetAliasInfoKind.ReturnValue, returnValues[returnValues.Length - 1].Value.Type, DbgDotNetRuntimeConstants.LastReturnValueId, Guid.Empty, null);
					foreach (var returnValue in returnValues) {
						Debug.Assert(returnValue.Id != DbgDotNetRuntimeConstants.LastReturnValueId);
						res[w++] = new DbgDotNetAliasInfo(DbgDotNetAliasInfoKind.ReturnValue, returnValue.Value.Type, returnValue.Id, Guid.Empty, null);
					}
				}
				if (w != res.Length)
					throw new InvalidOperationException();
				return res;
			}
			finally {
				exception?.Dispose();
				stowedException?.Dispose();
				foreach (var rv in returnValues)
					rv.Value?.Dispose();
			}
		}

		public DbgDotNetExceptionInfo[] GetExceptions(DbgEvaluationInfo evalInfo) {
			if (Dispatcher.CheckAccess())
				return GetExceptionsCore(evalInfo);
			return GetExceptions2(evalInfo);

			DbgDotNetExceptionInfo[] GetExceptions2(DbgEvaluationInfo evalInfo2) {
				if (!Dispatcher.TryInvokeRethrow(() => GetExceptionsCore(evalInfo2), out var result))
					result = Array.Empty<DbgDotNetExceptionInfo>();
				return result;
			}
		}

		DbgDotNetExceptionInfo[] GetExceptionsCore(DbgEvaluationInfo evalInfo) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			DbgDotNetValue exception = null;
			DbgDotNetValue stowedException = null;
			try {
				exception = GetExceptionCore(evalInfo, DbgDotNetRuntimeConstants.ExceptionId);
				stowedException = GetStowedExceptionCore(evalInfo, DbgDotNetRuntimeConstants.StowedExceptionId);
				int count = (exception != null ? 1 : 0) + (stowedException != null ? 1 : 0);
				if (count == 0)
					return Array.Empty<DbgDotNetExceptionInfo>();
				var res = new DbgDotNetExceptionInfo[count];
				int w = 0;
				if (exception != null)
					res[w++] = new DbgDotNetExceptionInfo(exception, DbgDotNetRuntimeConstants.ExceptionId, DbgDotNetExceptionInfoFlags.None);
				if (stowedException != null)
					res[w++] = new DbgDotNetExceptionInfo(stowedException, DbgDotNetRuntimeConstants.StowedExceptionId, DbgDotNetExceptionInfoFlags.StowedException);
				if (w != res.Length)
					throw new InvalidOperationException();
				return res;
			}
			catch {
				exception?.Dispose();
				stowedException?.Dispose();
				throw;
			}
		}

		public DbgDotNetReturnValueInfo[] GetReturnValues(DbgEvaluationInfo evalInfo) {
			if (Dispatcher.CheckAccess())
				return GetReturnValuesCore(evalInfo);
			return GetReturnValues2(evalInfo);

			DbgDotNetReturnValueInfo[] GetReturnValues2(DbgEvaluationInfo evalInfo2) {
				if (!Dispatcher.TryInvokeRethrow(() => GetReturnValuesCore(evalInfo2), out var result))
					result = Array.Empty<DbgDotNetReturnValueInfo>();
				return result;
			}
		}

		DbgDotNetReturnValueInfo[] GetReturnValuesCore(DbgEvaluationInfo evalInfo) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			// Not supported by mono
			return Array.Empty<DbgDotNetReturnValueInfo>();
		}

		public DbgDotNetValue GetException(DbgEvaluationInfo evalInfo, uint id) {
			if (Dispatcher.CheckAccess())
				return GetExceptionCore(evalInfo, id);
			return GetException2(evalInfo, id);

			DbgDotNetValue GetException2(DbgEvaluationInfo evalInfo2, uint id2) {
				Dispatcher.TryInvokeRethrow(() => GetExceptionCore(evalInfo2, id2), out var result);
				return result;
			}
		}

		DbgDotNetValue GetExceptionCore(DbgEvaluationInfo evalInfo, uint id) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			if (id != DbgDotNetRuntimeConstants.ExceptionId)
				return null;
			return engine.TryGetExceptionValue();
		}

		public DbgDotNetValue GetStowedException(DbgEvaluationInfo evalInfo, uint id) {
			if (Dispatcher.CheckAccess())
				return GetStowedExceptionCore(evalInfo, id);
			return GetStowedException2(evalInfo, id);

			DbgDotNetValue GetStowedException2(DbgEvaluationInfo evalInfo2, uint id2) {
				Dispatcher.TryInvokeRethrow(() => GetStowedExceptionCore(evalInfo2, id2), out var result);
				return result;
			}
		}

		DbgDotNetValue GetStowedExceptionCore(DbgEvaluationInfo evalInfo, uint id) {
			Dispatcher.VerifyAccess();
			return null;
		}

		public DbgDotNetValue GetReturnValue(DbgEvaluationInfo evalInfo, uint id) {
			if (Dispatcher.CheckAccess())
				return GetReturnValueCore(evalInfo, id);
			return GetReturnValue2(evalInfo, id);

			DbgDotNetValue GetReturnValue2(DbgEvaluationInfo evalInfo2, uint id2) {
				Dispatcher.TryInvokeRethrow(() => GetReturnValueCore(evalInfo2, id2), out var result);
				return result;
			}
		}

		DbgDotNetValue GetReturnValueCore(DbgEvaluationInfo evalInfo, uint id) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			// Not supported by mono
			return null;
		}

		public DbgDotNetValueResult GetLocalValue(DbgEvaluationInfo evalInfo, uint index) {
			if (Dispatcher.CheckAccess())
				return GetLocalValueCore(evalInfo, index);
			return GetLocalValue2(evalInfo, index);

			DbgDotNetValueResult GetLocalValue2(DbgEvaluationInfo evalInfo2, uint index2) {
				if (!Dispatcher.TryInvokeRethrow(() => GetLocalValueCore(evalInfo2, index2), out var result))
					result = DbgDotNetValueResult.CreateError(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgDotNetValueResult GetLocalValueCore(DbgEvaluationInfo evalInfo, uint index) {
			Dispatcher.VerifyAccess();
			try {
				var info = GetLocalValueLocationCore(evalInfo, index);
				if (info.errorMessage != null)
					return DbgDotNetValueResult.CreateError(info.errorMessage);
				return DbgDotNetValueResult.Create(engine.CreateDotNetValue_MonoDebug(info.valueLocation));
			}
			catch (InvalidStackFrameException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
			}
			catch (AbsentInformationException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		(ValueLocation valueLocation, string errorMessage) GetLocalValueLocationCore(DbgEvaluationInfo evalInfo, uint index) {
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
				throw new InvalidOperationException();
			var monoFrame = ilFrame.MonoFrame;
			var locals = monoFrame.Method.GetLocals();
			if ((uint)index >= (uint)locals.Length)
				return (null, PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
			var local = locals[(int)index];
			var reflectionAppDomain = ilFrame.GetReflectionModule().AppDomain;

			var method = GetFrameMethodCore(evalInfo);
			var localVars = GetCachedMethodBody(method)?.LocalVariables;
			var localType = localVars != null && (uint)index < (uint)localVars.Count ? localVars[(int)index].LocalType : null;
			if ((object)localType != null && localType.IsByRef)
				localType = localType.GetElementType();
			var type = engine.GetReflectionType(reflectionAppDomain, local.Type, localType);
			type = AddByRefIfNeeded(type, localVars, index);

			return (new LocalValueLocation(type, ilFrame, (int)index), null);
		}

		sealed class CachedMethodBodyState {
			public readonly DmdMethodBody Body;
			public CachedMethodBodyState(DmdMethodBody body) => Body = body;
		}
		static DmdMethodBody GetCachedMethodBody(DmdMethodBase method) {
			if ((object)method == null)
				return null;
			if (method.TryGetData(out CachedMethodBodyState state))
				return state.Body;
			return CreateCachedMethodBody(method);

			DmdMethodBody CreateCachedMethodBody(DmdMethodBase method2) =>
				method2.GetOrCreateData(() => new CachedMethodBodyState(method2.GetMethodBody())).Body;
		}

		static DmdType AddByRefIfNeeded(DmdType type, ReadOnlyCollection<DmdLocalVariableInfo> locals, uint index) {
			if (type.IsByRef)
				return type;
			if (locals == null || index >= (uint)locals.Count)
				return type;
			if (locals[(int)index].LocalType.IsByRef)
				return type.MakeByRefType();
			return type;
		}

		static DmdType AddByRefIfNeeded(DmdType type, ReadOnlyCollection<DmdType> types, uint index) {
			if (type.IsByRef)
				return type;
			if (types == null || index >= (uint)types.Count)
				return type;
			if (types[(int)index].IsByRef)
				return type.MakeByRefType();
			return type;
		}

		public DbgDotNetValueResult GetParameterValue(DbgEvaluationInfo evalInfo, uint index) {
			if (Dispatcher.CheckAccess())
				return GetParameterValueCore(evalInfo, index);
			return GetParameterValue2(evalInfo, index);

			DbgDotNetValueResult GetParameterValue2(DbgEvaluationInfo evalInfo2, uint index2) {
				if (!Dispatcher.TryInvokeRethrow(() => GetParameterValueCore(evalInfo2, index2), out var result))
					result = DbgDotNetValueResult.CreateError(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgDotNetValueResult GetParameterValueCore(DbgEvaluationInfo evalInfo, uint index) {
			Dispatcher.VerifyAccess();
			try {
				var info = GetParameterValueLocationCore(evalInfo, index);
				if (info.errorMessage != null)
					return DbgDotNetValueResult.CreateError(info.errorMessage);
				return DbgDotNetValueResult.Create(engine.CreateDotNetValue_MonoDebug(info.valueLocation));
			}
			catch (InvalidStackFrameException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
			}
			catch (AbsentInformationException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		(ValueLocation valueLocation, string errorMessage) GetParameterValueLocationCore(DbgEvaluationInfo evalInfo, uint index) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
				throw new InvalidOperationException();
			DmdType type;
			var monoFrame = ilFrame.MonoFrame;
			var reflectionAppDomain = ilFrame.GetReflectionModule().AppDomain;
			if (!monoFrame.Method.IsStatic) {
				if (index == 0) {
					type = engine.GetReflectionType(reflectionAppDomain, monoFrame.Method.DeclaringType, null);
					if (type.IsValueType)
						type = type.MakeByRefType();
					return (new ThisValueLocation(type, ilFrame), null);
				}
				index--;
			}
			var parameters = monoFrame.Method.GetParameters();
			if ((uint)index >= (uint)parameters.Length)
				return (null, PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
			var parameter = parameters[(int)index];

			var method = GetFrameMethodCore(evalInfo);
			var paramTypes = method?.GetMethodSignature().GetParameterTypes();
			var paramType = paramTypes != null && (uint)index < (uint)paramTypes.Count ? paramTypes[(int)index] : null;
			if ((object)paramType != null && paramType.IsByRef)
				paramType = paramType.GetElementType();
			type = engine.GetReflectionType(reflectionAppDomain, parameter.ParameterType, paramType);
			type = AddByRefIfNeeded(type, paramTypes, index);

			return (new ArgumentValueLocation(type, ilFrame, (int)index), null);
		}

		public string SetLocalValue(DbgEvaluationInfo evalInfo, uint index, DmdType targetType, object value) {
			if (Dispatcher.CheckAccess())
				return SetLocalValueCore(evalInfo, index, targetType, value);
			return SetLocalValue2(evalInfo, index, targetType, value);

			string SetLocalValue2(DbgEvaluationInfo evalInfo2, uint index2, DmdType targetType2, object value2) {
				if (!Dispatcher.TryInvokeRethrow(() => SetLocalValueCore(evalInfo2, index2, targetType2, value2), out var result))
					result = DispatcherConstants.ProcessExitedError;
				return result;
			}
		}

		string SetLocalValueCore(DbgEvaluationInfo evalInfo, uint index, DmdType targetType, object value) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			try {
				var info = GetLocalValueLocationCore(evalInfo, index);
				if (info.errorMessage != null)
					return info.errorMessage;
				var res = engine.CreateMonoValue_MonoDebug(evalInfo, value, targetType);
				if (res.ErrorMessage != null)
					return res.ErrorMessage;
				return info.valueLocation.Store(res.Value);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return PredefinedEvaluationErrorMessages.InternalDebuggerError;
			}
		}

		public string SetParameterValue(DbgEvaluationInfo evalInfo, uint index, DmdType targetType, object value) {
			if (Dispatcher.CheckAccess())
				return SetParameterValueCore(evalInfo, index, targetType, value);
			return SetParameterValue2(evalInfo, index, targetType, value);

			string SetParameterValue2(DbgEvaluationInfo evalInfo2, uint index2, DmdType targetType2, object value2) {
				if (!Dispatcher.TryInvokeRethrow(() => SetParameterValueCore(evalInfo2, index2, targetType2, value2), out var result))
					result = DispatcherConstants.ProcessExitedError;
				return result;
			}
		}

		string SetParameterValueCore(DbgEvaluationInfo evalInfo, uint index, DmdType targetType, object value) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			try {
				var info = GetParameterValueLocationCore(evalInfo, index);
				if (info.errorMessage != null)
					return info.errorMessage;
				var res = engine.CreateMonoValue_MonoDebug(evalInfo, value, targetType);
				if (res.ErrorMessage != null)
					return res.ErrorMessage;
				return info.valueLocation.Store(res.Value);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return PredefinedEvaluationErrorMessages.InternalDebuggerError;
			}
		}

		public DbgDotNetValue GetLocalValueAddress(DbgEvaluationInfo evalInfo, uint index, DmdType targetType) => null;
		public DbgDotNetValue GetParameterValueAddress(DbgEvaluationInfo evalInfo, uint index, DmdType targetType) => null;

		public DbgDotNetValueResult CreateValue(DbgEvaluationInfo evalInfo, object value) {
			if (Dispatcher.CheckAccess())
				return CreateValueCore(evalInfo, value);
			return CreateValue2(evalInfo, value);

			DbgDotNetValueResult CreateValue2(DbgEvaluationInfo evalInfo2, object value2) {
				if (!Dispatcher.TryInvokeRethrow(() => CreateValueCore(evalInfo2, value2), out var result))
					result = DbgDotNetValueResult.CreateError(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgDotNetValueResult CreateValueCore(DbgEvaluationInfo evalInfo, object value) {
			Dispatcher.VerifyAccess();
			try {
				return engine.CreateValue_MonoDebug(evalInfo, value);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		public DbgDotNetValueResult Box(DbgEvaluationInfo evalInfo, object value) {
			if (Dispatcher.CheckAccess())
				return BoxCore(evalInfo, value);
			return Box2(evalInfo, value);

			DbgDotNetValueResult Box2(DbgEvaluationInfo evalInfo2, object value2) {
				if (!Dispatcher.TryInvokeRethrow(() => BoxCore(evalInfo2, value2), out var result))
					result = DbgDotNetValueResult.CreateError(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgDotNetValueResult BoxCore(DbgEvaluationInfo evalInfo, object value) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			DbgDotNetValueResult res = default;
			try {
				res = CreateValueCore(evalInfo, value);
				if (res.ErrorMessage != null)
					return res;
				var boxedValue = res.Value.Box(evalInfo);
				if (boxedValue != null)
					return boxedValue.Value;
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
			finally {
				res.Value?.Dispose();
			}
		}

		public bool CanCreateObjectId(DbgDotNetValue value) =>
			(value as DbgDotNetValueImpl)?.Value is ObjectMirror;

		public DbgDotNetObjectId CreateObjectId(DbgDotNetValue value, uint id) {
			var valueImpl = value as DbgDotNetValueImpl;
			if (valueImpl == null)
				return null;
			if (Dispatcher.CheckAccess())
				return CreateObjectIdCore(valueImpl, id);
			return CreateObjectId2(valueImpl, id);

			DbgDotNetObjectId CreateObjectId2(DbgDotNetValueImpl value2, uint id2) {
				Dispatcher.TryInvokeRethrow(() => CreateObjectIdCore(value2, id2), out var result);
				return result;
			}
		}

		DbgDotNetObjectId CreateObjectIdCore(DbgDotNetValueImpl value, uint id) {
			Dispatcher.VerifyAccess();
			try {
				if (!engine.IsPaused)
					return null;

				var valueObjectMirror = value.Value as ObjectMirror;
				if (valueObjectMirror == null)
					return null;

				var appDomain = value.Type.AppDomain;
				var gcHandleType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_GCHandle, isOptional: true);
				Debug.Assert((object)gcHandleType != null);
				if ((object)gcHandleType == null)
					return null;

				var allocMethod = gcHandleType.GetMethod(nameof(System.Runtime.InteropServices.GCHandle.Alloc),
					DmdSignatureCallingConvention.Default, 0, gcHandleType, new[] { appDomain.System_Object }, throwOnError: false);
				Debug.Assert((object)allocMethod != null);
				if ((object)allocMethod == null)
					return null;

				var res = engine.FuncEvalCall_AnyThread_MonoDebug(allocMethod, null, new[] { value }, DbgDotNetInvokeOptions.None, newObj: false);
				Debug.Assert(res.IsNormalResult);
				if (!res.IsNormalResult) {
					res.Value?.Dispose();
					return null;
				}

				return new DbgDotNetObjectIdImpl(this, id, res.Value, valueObjectMirror, appDomain);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return null;
			}
		}

		internal void FreeObjectId(DbgDotNetObjectIdImpl objectId) {
			if (Dispatcher.CheckAccess())
				FreeObjectIdCore(objectId);
			else
				FreeObjectId2(objectId);

			void FreeObjectId2(DbgDotNetObjectIdImpl objectId2) {
				if (!Dispatcher.TryBeginInvoke(() => FreeObjectIdCore(objectId2))) {
					// process has exited
				}
			}
		}

		void FreeObjectIdCore(DbgDotNetObjectIdImpl objectId) {
			Dispatcher.VerifyAccess();
			engine.AddExecOnPause(() => FreeObjectId_Paused(objectId));
		}

		void FreeObjectId_Paused(DbgDotNetObjectIdImpl objectId) {
			Dispatcher.VerifyAccess();
			if (engine.DbgRuntime.IsClosed)
				return;
			Debug.Assert(engine.IsPaused);

			var appDomain = objectId.ReflectionAppDomain;
			var gcHandleType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_GCHandle, isOptional: true);
			Debug.Assert((object)gcHandleType != null);
			if ((object)gcHandleType == null)
				return;

			var freeMethod = gcHandleType.GetMethod(nameof(System.Runtime.InteropServices.GCHandle.Free),
				DmdSignatureCallingConvention.HasThis, 0, appDomain.System_Void, Array.Empty<DmdType>(), throwOnError: false);
			Debug.Assert((object)freeMethod != null);
			if ((object)freeMethod == null)
				return;

			var res = engine.FuncEvalCall_AnyThread_MonoDebug(freeMethod, objectId.GCHandleValue, Array.Empty<object>(), DbgDotNetInvokeOptions.None, newObj: false);
			Debug.Assert(res.IsNormalResult);
			res.Value?.Dispose();
		}

		public bool Equals(DbgDotNetObjectId objectId, DbgDotNetValue value) {
			var objectIdImpl = objectId as DbgDotNetObjectIdImpl;
			var valueImpl = value as DbgDotNetValueImpl;
			if (objectIdImpl == null || valueImpl == null)
				return false;
			if (Dispatcher.CheckAccess())
				return EqualsCore(objectIdImpl, valueImpl);
			return Equals2(objectIdImpl, valueImpl);

			bool Equals2(DbgDotNetObjectIdImpl objectId2, DbgDotNetValueImpl value2) {
				Dispatcher.TryInvokeRethrow(() => EqualsCore(objectId2, value2), out var result);
				return result;
			}
		}

		readonly struct EquatableValue {
			public readonly ulong Address;
			readonly ObjectMirror value;

			public EquatableValue(ObjectMirror value) {
				if (value == null)
					Address = 0;
				else {
					try {
						Address = (ulong)value.Address;
					}
					catch (ObjectCollectedException) {
						Address = 0;
					}
				}
				this.value = value;
			}

			public bool Equals2(in EquatableValue other) => Address != 0 && Address == other.Address;
			public bool? Equals3(in EquatableValue other) => Address == 0 && other.Address == 0 ? (bool?)null : Address == other.Address;
			// Value must be stable, so we can't use Address (obj could get moved by the GC). It's used by dictionaries.
			public new int GetHashCode() => Address == 0 ? 0 : value?.Type.GetHashCode() ?? 0;
		}

		bool EqualsCore(DbgDotNetObjectIdImpl objectId, DbgDotNetValueImpl value) {
			Dispatcher.VerifyAccess();

			var objectIdValue = objectId.Value;
			var valueObject = value.Value as ObjectMirror;
			if (valueObject == null)
				return false;
			if (objectIdValue == valueObject)
				return true;
			var v1 = GetEquatableValue(objectIdValue);
			var v2 = GetEquatableValue(valueObject);
			return v1.Equals2(v2);
		}

		static EquatableValue GetEquatableValue(ObjectMirror value) => new EquatableValue(value);

		public int GetHashCode(DbgDotNetObjectId objectId) {
			var objectIdImpl = objectId as DbgDotNetObjectIdImpl;
			if (objectIdImpl == null)
				return 0;
			if (Dispatcher.CheckAccess())
				return GetHashCodeCore(objectIdImpl);
			return GetHashCode2(objectIdImpl);

			int GetHashCode2(DbgDotNetObjectIdImpl objectId2) {
				Dispatcher.TryInvokeRethrow(() => GetHashCodeCore(objectId2), out var result);
				return result;
			}
		}

		int GetHashCodeCore(DbgDotNetObjectIdImpl objectId) {
			Dispatcher.VerifyAccess();
			return GetEquatableValue(objectId.Value).GetHashCode();
		}

		public int GetHashCode(DbgDotNetValue value) {
			var valueImpl = value as DbgDotNetValueImpl;
			if (valueImpl == null)
				return 0;
			if (Dispatcher.CheckAccess())
				return GetHashCodeCore(valueImpl);
			return GetHashCode2(valueImpl);

			int GetHashCode2(DbgDotNetValueImpl value2) {
				Dispatcher.TryInvokeRethrow(() => GetHashCodeCore(value2), out var result);
				return result;
			}
		}

		int GetHashCodeCore(DbgDotNetValueImpl value) {
			Dispatcher.VerifyAccess();
			return GetEquatableValue(value.Value as ObjectMirror).GetHashCode();
		}

		public DbgDotNetValue GetValue(DbgEvaluationInfo evalInfo, DbgDotNetObjectId objectId) {
			var objectIdImpl = objectId as DbgDotNetObjectIdImpl;
			if (objectIdImpl == null)
				throw new ArgumentException();
			if (Dispatcher.CheckAccess())
				return GetValueCore(evalInfo, objectIdImpl);
			return GetValue2(evalInfo, objectIdImpl);

			DbgDotNetValue GetValue2(DbgEvaluationInfo evalInfo2, DbgDotNetObjectIdImpl objectId2) {
				Dispatcher.TryInvokeRethrow(() => GetValueCore(evalInfo2, objectId2), out var result);
				return result;
			}
		}

		DbgDotNetValue GetValueCore(DbgEvaluationInfo evalInfo, DbgDotNetObjectIdImpl objectId) {
			Dispatcher.VerifyAccess();
			return engine.CreateDotNetValue_MonoDebug(objectId.ReflectionAppDomain, objectId.Value, null);
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

			bool? Equals2(DbgDotNetValueImpl a2, DbgDotNetValueImpl b2) {
				if (!Dispatcher.TryInvokeRethrow(() => EqualsCore(a2, b2), out var result))
					result = false;
				return result;
			}
		}

		bool? EqualsCore(DbgDotNetValueImpl a, DbgDotNetValueImpl b) {
			Dispatcher.VerifyAccess();
			return GetEquatableValue(a.Value as ObjectMirror).Equals3(GetEquatableValue(b.Value as ObjectMirror));
		}

		public bool TryGetNativeCode(DbgStackFrame frame, out DbgDotNetNativeCode nativeCode) {
			nativeCode = default;
			return false;
		}

		public bool TryGetNativeCode(DmdMethodBase method, out DbgDotNetNativeCode nativeCode) {
			nativeCode = default;
			return false;
		}

		public bool TryGetSymbol(ulong address, out SymbolResolverResult result) {
			result = default;
			return false;
		}

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
