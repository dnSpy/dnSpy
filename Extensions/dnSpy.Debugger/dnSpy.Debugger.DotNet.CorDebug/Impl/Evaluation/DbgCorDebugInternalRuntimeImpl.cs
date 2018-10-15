/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Contracts.Debugger.DotNet.Disassembly;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Disassembly;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation.Hooks;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation {
	sealed class DbgCorDebugInternalRuntimeImpl : DbgCorDebugInternalRuntime, IDbgDotNetRuntime, ICorDebugRuntime {
		public override DbgRuntime Runtime { get; }
		public override DmdRuntime ReflectionRuntime { get; }
		public override CorDebugRuntimeVersion Version { get; }
		public override string ClrFilename { get; }
		public override string RuntimeDirectory { get; }
		public DbgDotNetDispatcher Dispatcher { get; }
		public DbgDotNetRuntimeFeatures Features => DbgDotNetRuntimeFeatures.ObjectIds | DbgDotNetRuntimeFeatures.NativeMethodBodies;

		ICorDebugValueConverter ICorDebugRuntime.ValueConverter => corDebugValueConverter;

		readonly DbgEngineImpl engine;
		readonly Dictionary<DmdWellKnownType, ClassHook> classHooks;
		readonly ICorDebugValueConverter corDebugValueConverter;

		public DbgCorDebugInternalRuntimeImpl(DbgEngineImpl engine, DbgRuntime runtime, DmdRuntime reflectionRuntime, CorDebugRuntimeKind kind, string version, string clrPath, string runtimeDir) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			ReflectionRuntime = reflectionRuntime ?? throw new ArgumentNullException(nameof(reflectionRuntime));
			Version = new CorDebugRuntimeVersion(kind, version ?? throw new ArgumentNullException(nameof(version)));
			ClrFilename = clrPath ?? throw new ArgumentNullException(nameof(clrPath));
			RuntimeDirectory = runtimeDir ?? throw new ArgumentNullException(nameof(runtimeDir));
			Dispatcher = new DbgDotNetDispatcherImpl(engine);
			reflectionRuntime.GetOrCreateData(() => runtime);

			corDebugValueConverter = new CorDebugValueConverterImpl(this);
			classHooks = new Dictionary<DmdWellKnownType, ClassHook>();
			foreach (var info in ClassHookProvider.Create(this)) {
				Debug.Assert(info.Hook != null);
				Debug.Assert(!classHooks.ContainsKey(info.WellKnownType));
				classHooks.Add(info.WellKnownType, info.Hook);
			}
		}

		public ModuleId GetModuleId(DbgModule module) => engine.GetModuleId(module);

		public DbgDotNetRawModuleBytes GetRawModuleBytes(DbgModule module) {
			if (!module.IsDynamic)
				return DbgDotNetRawModuleBytes.None;
			if (Dispatcher.CheckAccess())
				return GetRawModuleBytesCore(module);
			return GetRawModuleBytesCore2(module);

			DbgDotNetRawModuleBytes GetRawModuleBytesCore2(DbgModule module2) =>
				Dispatcher.InvokeRethrow(() => GetRawModuleBytesCore(module2));
		}

		sealed class DynamicModuleMetadataState {
			public byte[] RawBytes;
			public ModuleDefMD Module;
			public int LoadClassVersion;
			public DbgDotNetRawModuleBytes ToDbgDotNetRawModuleBytes() {
				if (RawBytes != null)
					return new DbgDotNetRawModuleBytes(RawBytes, isFileLayout: true);
				return DbgDotNetRawModuleBytes.None;
			}
		}

		DbgDotNetRawModuleBytes GetRawModuleBytesCore(DbgModule module) {
			Dispatcher.VerifyAccess();
			if (!module.IsDynamic)
				return DbgDotNetRawModuleBytes.None;

			if (!engine.TryGetDnModuleAndVersion(module, out var dnModule, out int loadClassVersion))
				return DbgDotNetRawModuleBytes.None;

			var state = module.GetOrCreateData<DynamicModuleMetadataState>();
			if (state.RawBytes != null && state.LoadClassVersion == loadClassVersion)
				return state.ToDbgDotNetRawModuleBytes();

			var md = dnModule.GetOrCreateCorModuleDef();
			try {
				md.DisableMDAPICalls = false;
				md.LoadEverything(null);
			}
			finally {
				md.DisableMDAPICalls = true;
			}

			var resultStream = new MemoryStream();
			var options = new ModuleWriterOptions(md);
			options.MetadataOptions.Flags = MetadataFlags.PreserveRids;
			md.Write(resultStream, options);

			state.Module = null;
			state.RawBytes = resultStream.ToArray();
			state.LoadClassVersion = loadClassVersion;

			engine.RaiseModulesRefreshed(module);

			return state.ToDbgDotNetRawModuleBytes();
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
				var res2 = Dispatcher.InvokeRethrow(() => {
					var res = TryGetMethodTokenCore(module2, methodToken2, out var metadataMethodToken3, out var metadataLocalVarSigTok3);
					tmpMetadataMethodToken = metadataMethodToken3;
					tmpMetadataLocalVarSigTok = metadataLocalVarSigTok3;
					return res;
				});
				metadataMethodToken2 = tmpMetadataMethodToken;
				metadataLocalVarSigTok2 = tmpMetadataLocalVarSigTok;
				return res2;
			}
		}

		bool TryGetMethodTokenCore(DbgModule module, int methodToken, out int metadataMethodToken, out int metadataLocalVarSigTok) {
			Dispatcher.VerifyAccess();
			DynamicModuleMetadataState state = null;
			if (module.IsDynamic && !module.TryGetData<DynamicModuleMetadataState>(out state)) {
				GetRawModuleBytesCore(module);
				bool b = module.TryGetData<DynamicModuleMetadataState>(out state);
				Debug.Assert(b);
			}
			if (state != null) {
				if (state.Module == null)
					state.Module = ModuleDefMD.Load(state.RawBytes);
				var method = state.Module.ResolveToken(methodToken) as MethodDef;
				if (method != null) {
					metadataMethodToken = method.MDToken.ToInt32();
					metadataLocalVarSigTok = (int)(method.Body?.LocalVarSigTok ?? 0);
					return true;
				}
			}

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

			DmdMethodBase GetFrameMethod2(DbgEvaluationInfo evalInfo2) =>
				Dispatcher.InvokeRethrow(() => GetFrameMethodCore(evalInfo2));
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

		CorType GetType(CorAppDomain appDomain, DmdType type) => CorDebugTypeCreator.GetType(engine, appDomain, type);

		internal static CorValue TryGetObjectOrPrimitiveValue(CorValue value, out int hr) {
			hr = -1;
			if (value == null)
				return null;
			if (value.IsReference) {
				if (value.IsNull)
					throw new InvalidOperationException();
				value = value.GetDereferencedValue(out hr);
				if (value == null)
					return null;
			}
			if (value.IsBox) {
				value = value.GetBoxedValue(out hr);
				if (value == null)
					return null;
			}
			hr = 0;
			return value;
		}

		public DbgDotNetValue LoadFieldAddress(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field) => null;

		public DbgDotNetValueResult LoadField(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field) {
			if (Dispatcher.CheckAccess())
				return LoadFieldCore(evalInfo, obj, field);
			return LoadField2(evalInfo, obj, field);

			DbgDotNetValueResult LoadField2(DbgEvaluationInfo evalInfo2, DbgDotNetValue obj2, DmdFieldInfo field2) =>
				Dispatcher.InvokeRethrow(() => LoadFieldCore(evalInfo2, obj2, field2));
		}

		DbgDotNetValueResult LoadFieldCore(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
					return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
				var appDomain = ilFrame.GetCorAppDomain();

				int hr;
				CorType corFieldDeclType;
				CorValue fieldValue;
				var fieldDeclType = field.DeclaringType;
				if (obj == null) {
					if (!field.IsStatic)
						return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);

					if (field.IsLiteral)
						return CreateSyntheticValue(field.FieldType, field.GetRawConstantValue());
					else {
						corFieldDeclType = GetType(appDomain, fieldDeclType);

						InitializeStaticConstructor(evalInfo, ilFrame, fieldDeclType, corFieldDeclType);
						fieldValue = corFieldDeclType.GetStaticFieldValue((uint)field.MetadataToken, ilFrame.CorFrame, out hr);
						if (fieldValue == null) {
							if (hr == CordbgErrors.CORDBG_E_CLASS_NOT_LOADED || hr == CordbgErrors.CORDBG_E_STATIC_VAR_NOT_AVAILABLE) {
								//TODO: Create a synthetic value init'd to the default value (0s or null ref)
							}
						}
						if (fieldValue == null)
							return DbgDotNetValueResult.CreateError(CordbgErrorHelper.GetErrorMessage(hr));
						return DbgDotNetValueResult.Create(engine.CreateDotNetValue_CorDebug(fieldValue, field.AppDomain, tryCreateStrongHandle: true));
					}
				}
				else {
					if (field.IsStatic)
						return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);

					var objImp = obj as DbgDotNetValueImpl ?? throw new InvalidOperationException();
					corFieldDeclType = GetType(appDomain, fieldDeclType);
					var objValue = TryGetObjectOrPrimitiveValue(objImp.TryGetCorValue(), out hr);
					if (objValue == null)
						return DbgDotNetValueResult.CreateError(CordbgErrorHelper.GetErrorMessage(hr));
					if (objValue.IsObject) {
						fieldValue = objValue.GetFieldValue(corFieldDeclType.Class, (uint)field.MetadataToken, out hr);
						if (fieldValue == null)
							return DbgDotNetValueResult.CreateError(CordbgErrorHelper.GetErrorMessage(hr));
						return DbgDotNetValueResult.Create(engine.CreateDotNetValue_CorDebug(fieldValue, field.AppDomain, tryCreateStrongHandle: true));
					}
					else {
						if (IsPrimitiveValueType(objValue.ElementType)) {
							//TODO:
						}
					}
				}

				return DbgDotNetValueResult.CreateError("NYI");//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
		}

		public string StoreField(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field, object value) {
			if (Dispatcher.CheckAccess())
				return StoreFieldCore(evalInfo, obj, field, value);
			return StoreField2(evalInfo, obj, field, value);

			string StoreField2(DbgEvaluationInfo evalInfo2, DbgDotNetValue obj2, DmdFieldInfo field2, object value2) =>
				Dispatcher.InvokeRethrow(() => StoreFieldCore(evalInfo2, obj2, field2, value2));
		}

		string StoreFieldCore(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdFieldInfo field, object value) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
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

						InitializeStaticConstructor(evalInfo, ilFrame, fieldDeclType, corFieldDeclType);
						Func<CreateCorValueResult> createTargetValue = () => {
							var fieldValue = corFieldDeclType.GetStaticFieldValue((uint)field.MetadataToken, ilFrame.CorFrame, out var hr);
							return new CreateCorValueResult(fieldValue, hr);
						};
						return engine.StoreValue_CorDebug(evalInfo, ilFrame, createTargetValue, field.FieldType, value);
					}
				}
				else {
					if (field.IsStatic)
						return CordbgErrorHelper.InternalError;

					var objImp = obj as DbgDotNetValueImpl ?? throw new InvalidOperationException();
					corFieldDeclType = GetType(appDomain, fieldDeclType);
					var objValue = TryGetObjectOrPrimitiveValue(objImp.TryGetCorValue(), out int hr);
					if (objValue == null)
						return CordbgErrorHelper.GetErrorMessage(hr);
					if (objValue.IsObject) {
						Func<CreateCorValueResult> createTargetValue = () => {
							// Re-read it since it could've gotten neutered
							var objValue2 = TryGetObjectOrPrimitiveValue(objImp.TryGetCorValue(), out int hr2);
							Debug.Assert(objValue2?.IsObject == true);
							if (objValue2 == null)
								return new CreateCorValueResult(null, hr2);
							var fieldValue = objValue2.GetFieldValue(corFieldDeclType.Class, (uint)field.MetadataToken, out hr2);
							return new CreateCorValueResult(fieldValue, hr2);
						};
						return engine.StoreValue_CorDebug(evalInfo, ilFrame, createTargetValue, field.FieldType, value);
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
				return DbgDotNetValueResult.Create(dnValue);
			return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
		}

		sealed class StaticConstructorInitializedState {
			public volatile int Initialized;
		}

		void InitializeStaticConstructor(DbgEvaluationInfo evalInfo, ILDbgEngineStackFrame ilFrame, DmdType type, CorType corType) {
			if (engine.CheckFuncEval(evalInfo) != null)
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
						try {
							if (fieldValue.IsNull)
								continue;
							if (field.FieldType.IsValueType) {
								var objValue = fieldValue.GetDereferencedValue(out hr)?.GetBoxedValue(out hr);
								var data = objValue?.ReadGenericValue();
								if (data != null && !IsZero(data))
									return;
							}
							else {
								// It's a reference type and not null, so the field has been initialized
								return;
							}
						}
						finally {
							engine.DisposeHandle_CorDebug(fieldValue);
						}
					}
				}

				if (HasNativeCode(cctor))
					return;
			}

			DbgDotNetValueResult res = default;
			try {
				res = engine.FuncEvalCreateInstanceNoCtor_CorDebug(evalInfo, ilFrame.GetCorAppDomain(), type);
				if (res.Value == null || res.ValueIsException)
					return;
				RuntimeHelpersRunClassConstructor(evalInfo, ilFrame, type, res.Value);
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
		bool RuntimeHelpersRunClassConstructor(DbgEvaluationInfo evalInfo, ILDbgEngineStackFrame ilFrame, DmdType type, DbgDotNetValue objValue) {
			DbgDotNetValueResult getTypeRes = default;
			DbgDotNetValueResult typeHandleRes = default;
			DbgDotNetValueResult res = default;
			try {
				var reflectionAppDomain = type.AppDomain;
				var getTypeMethod = objValue.Type.GetMethod(nameof(object.GetType), DmdSignatureCallingConvention.Default | DmdSignatureCallingConvention.HasThis, 0, reflectionAppDomain.System_Type, Array.Empty<DmdType>(), throwOnError: false);
				Debug.Assert((object)getTypeMethod != null);
				if ((object)getTypeMethod == null)
					return false;
				var corAppDomain = ilFrame.GetCorAppDomain();
				getTypeRes = engine.FuncEvalCall_CorDebug(evalInfo, corAppDomain, getTypeMethod, objValue, Array.Empty<object>(), false);
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
				typeHandleRes = engine.FuncEvalCall_CorDebug(evalInfo, corAppDomain, getTypeHandleMethod, typeObj, Array.Empty<object>(), false);
				if (typeHandleRes.Value == null || typeHandleRes.ValueIsException)
					return false;
				var runtimeHelpersType = reflectionAppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_CompilerServices_RuntimeHelpers, isOptional: true);
				var runClassConstructorMethod = runtimeHelpersType?.GetMethod(nameof(RuntimeHelpers.RunClassConstructor), DmdSignatureCallingConvention.Default, 0, reflectionAppDomain.System_Void, new[] { runtimeTypeHandleType }, throwOnError: false);
				Debug.Assert((object)runClassConstructorMethod != null);
				if ((object)runClassConstructorMethod == null)
					return false;
				res = engine.FuncEvalCall_CorDebug(evalInfo, corAppDomain, runClassConstructorMethod, null, new[] { typeHandleRes.Value }, false);
				return !res.HasError && !res.ValueIsException;
			}
			finally {
				getTypeRes.Value?.Dispose();
				typeHandleRes.Value?.Dispose();
				res.Value?.Dispose();
			}
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

		public DbgDotNetValueResult Call(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, DbgDotNetInvokeOptions invokeOptions) {
			if (Dispatcher.CheckAccess())
				return CallCore(evalInfo, obj, method, arguments, invokeOptions);
			return Call2(evalInfo, obj, method, arguments, invokeOptions);

			DbgDotNetValueResult Call2(DbgEvaluationInfo evalInfo2, DbgDotNetValue obj2, DmdMethodBase method2, object[] arguments2, DbgDotNetInvokeOptions invokeOptions2) =>
				Dispatcher.InvokeRethrow(() => CallCore(evalInfo2, obj2, method2, arguments2, invokeOptions2));
		}

		DbgDotNetValueResult CallCore(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, DbgDotNetInvokeOptions invokeOptions) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
					return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);

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

				return engine.FuncEvalCall_CorDebug(evalInfo, ilFrame.GetCorAppDomain(), method, obj, arguments, newObj: false);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
		}

		public DbgDotNetValueResult CreateInstance(DbgEvaluationInfo evalInfo, DmdConstructorInfo ctor, object[] arguments, DbgDotNetInvokeOptions invokeOptions) {
			if (Dispatcher.CheckAccess())
				return CreateInstanceCore(evalInfo, ctor, arguments, invokeOptions);
			return CreateInstance2(evalInfo, ctor, arguments, invokeOptions);

			DbgDotNetValueResult CreateInstance2(DbgEvaluationInfo evalInfo2, DmdConstructorInfo ctor2, object[] arguments2, DbgDotNetInvokeOptions invokeOptions2) =>
				Dispatcher.InvokeRethrow(() => CreateInstanceCore(evalInfo2, ctor2, arguments2, invokeOptions2));
		}

		DbgDotNetValueResult CreateInstanceCore(DbgEvaluationInfo evalInfo, DmdConstructorInfo ctor, object[] arguments, DbgDotNetInvokeOptions invokeOptions) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
					return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);

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

				return engine.FuncEvalCall_CorDebug(evalInfo, ilFrame.GetCorAppDomain(), ctor, null, arguments, newObj: true);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
		}

		public DbgDotNetValueResult CreateInstanceNoConstructor(DbgEvaluationInfo evalInfo, DmdType type) {
			if (Dispatcher.CheckAccess())
				return CreateInstanceNoConstructorCore(evalInfo, type);
			return CreateInstanceNoConstructor2(evalInfo, type);

			DbgDotNetValueResult CreateInstanceNoConstructor2(DbgEvaluationInfo evalInfo2, DmdType type2) =>
				Dispatcher.InvokeRethrow(() => CreateInstanceNoConstructorCore(evalInfo2, type2));
		}

		DbgDotNetValueResult CreateInstanceNoConstructorCore(DbgEvaluationInfo evalInfo, DmdType type) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
					return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
				return engine.FuncEvalCreateInstanceNoCtor_CorDebug(evalInfo, ilFrame.GetCorAppDomain(), type);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
		}

		public DbgDotNetValueResult CreateSZArray(DbgEvaluationInfo evalInfo, DmdType elementType, int length) {
			if (Dispatcher.CheckAccess())
				return CreateSZArrayCore(evalInfo, elementType, length);
			return CreateSZArray2(evalInfo, elementType, length);

			DbgDotNetValueResult CreateSZArray2(DbgEvaluationInfo evalInfo2, DmdType elementType2, int length2) =>
				Dispatcher.InvokeRethrow(() => CreateSZArrayCore(evalInfo2, elementType2, length2));
		}

		DbgDotNetValueResult CreateSZArrayCore(DbgEvaluationInfo evalInfo, DmdType elementType, int length) {
			Dispatcher.VerifyAccess();
			try {
				if (!CanCallNewParameterizedArray(elementType))
					return CreateSZArrayCore_Array_CreateInstance(evalInfo, elementType, length);
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
					return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
				return engine.CreateSZArray_CorDebug(evalInfo, ilFrame.GetCorAppDomain(), elementType, length);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
		}

		DbgDotNetValueResult CreateSZArrayCore_Array_CreateInstance(DbgEvaluationInfo evalInfo, DmdType elementType, int length) {
			Dispatcher.VerifyAccess();
			Debug.Assert(!CanCallNewParameterizedArray(elementType));
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);

			// Execute this code:
			//	var elementType = Type.GetType(elementType.AssemblyQualifiedName);
			//	return Array.CreateInstance(elementType, length);

			var appDomain = elementType.AppDomain;
			DbgDotNetValue typeElementType = null;
			try {
				var methodGetType = appDomain.System_Type.GetMethod(nameof(Type.GetType), DmdSignatureCallingConvention.Default, 0, appDomain.System_Type, new[] { appDomain.System_String }, throwOnError: true);
				var res = engine.FuncEvalCall_CorDebug(evalInfo, ilFrame.GetCorAppDomain(), methodGetType, null, new[] { elementType.AssemblyQualifiedName }, false);
				if (res.HasError || res.ValueIsException)
					return res;
				typeElementType = res.Value;
				if (res.Value.IsNull)
					return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);

				var methodCreateInstance = appDomain.System_Array.GetMethod(nameof(Array.CreateInstance), DmdSignatureCallingConvention.Default, 0, appDomain.System_Array, new[] { appDomain.System_Type, appDomain.System_Int32 }, throwOnError: true);
				return engine.FuncEvalCall_CorDebug(evalInfo, ilFrame.GetCorAppDomain(), methodCreateInstance, null, new object[] { typeElementType, length }, false);
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

		public DbgDotNetValueResult CreateArray(DbgEvaluationInfo evalInfo, DmdType elementType, DbgDotNetArrayDimensionInfo[] dimensionInfos) {
			if (Dispatcher.CheckAccess())
				return CreateArrayCore(evalInfo, elementType, dimensionInfos);
			return CreateArray2(evalInfo, elementType, dimensionInfos);

			DbgDotNetValueResult CreateArray2(DbgEvaluationInfo evalInfo2, DmdType elementType2, DbgDotNetArrayDimensionInfo[] dimensionInfos2) =>
				Dispatcher.InvokeRethrow(() => CreateArrayCore(evalInfo2, elementType2, dimensionInfos2));
		}

		DbgDotNetValueResult CreateArrayCore(DbgEvaluationInfo evalInfo, DmdType elementType, DbgDotNetArrayDimensionInfo[] dimensionInfos) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);

			// There's no ICorDebugEval method that can create multi-dimensional arrays so
			// we have to use Array.CreateInstance(Type, int[], int[]).

			var appDomain = elementType.AppDomain;
			DbgDotNetValue typeElementType = null;
			try {
				var methodGetType = appDomain.System_Type.GetMethod(nameof(Type.GetType), DmdSignatureCallingConvention.Default, 0, appDomain.System_Type, new[] { appDomain.System_String }, throwOnError: true);
				var res = engine.FuncEvalCall_CorDebug(evalInfo, ilFrame.GetCorAppDomain(), methodGetType, null, new[] { elementType.AssemblyQualifiedName }, false);
				if (res.HasError || res.ValueIsException)
					return res;
				typeElementType = res.Value;
				if (res.Value.IsNull)
					return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);

				var lengths = new int[dimensionInfos.Length];
				var lowerBounds = new int[dimensionInfos.Length];
				for (int i = 0; i < dimensionInfos.Length; i++) {
					lengths[i] = (int)dimensionInfos[i].Length;
					lowerBounds[i] = dimensionInfos[i].BaseIndex;
				}

				var methodCreateInstance = appDomain.System_Array.GetMethod(nameof(Array.CreateInstance), DmdSignatureCallingConvention.Default, 0, appDomain.System_Array, new[] { appDomain.System_Type, appDomain.System_Int32.MakeArrayType(), appDomain.System_Int32.MakeArrayType() }, throwOnError: true);
				res = engine.FuncEvalCall_CorDebug(evalInfo, ilFrame.GetCorAppDomain(), methodCreateInstance, null, new object[] { typeElementType, lengths, lowerBounds }, false);
				if (res.HasError || res.ValueIsException)
					return res;

				return res;
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
			finally {
				typeElementType?.Dispose();
			}
		}

		public DbgDotNetAliasInfo[] GetAliases(DbgEvaluationInfo evalInfo) {
			if (Dispatcher.CheckAccess())
				return GetAliasesCore(evalInfo);
			return GetAliases2(evalInfo);

			DbgDotNetAliasInfo[] GetAliases2(DbgEvaluationInfo evalInfo2) =>
				Dispatcher.InvokeRethrow(() => GetAliasesCore(evalInfo2));
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

			DbgDotNetExceptionInfo[] GetExceptions2(DbgEvaluationInfo evalInfo2) =>
				Dispatcher.InvokeRethrow(() => GetExceptionsCore(evalInfo2));
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

			DbgDotNetReturnValueInfo[] GetReturnValues2(DbgEvaluationInfo evalInfo2) =>
				Dispatcher.InvokeRethrow(() => GetReturnValuesCore(evalInfo2));
		}

		DbgDotNetReturnValueInfo[] GetReturnValuesCore(DbgEvaluationInfo evalInfo) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			return engine.GetCurrentReturnValues();
		}

		public DbgDotNetValue GetException(DbgEvaluationInfo evalInfo, uint id) {
			if (Dispatcher.CheckAccess())
				return GetExceptionCore(evalInfo, id);
			return GetException2(evalInfo, id);

			DbgDotNetValue GetException2(DbgEvaluationInfo evalInfo2, uint id2) =>
				Dispatcher.InvokeRethrow(() => GetExceptionCore(evalInfo2, id2));
		}

		DbgDotNetValue GetExceptionCore(DbgEvaluationInfo evalInfo, uint id) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			if (id != DbgDotNetRuntimeConstants.ExceptionId)
				return null;
			var corException = TryGetException(evalInfo.Frame);
			if (corException == null)
				return null;
			var reflectionAppDomain = evalInfo.Frame.AppDomain.GetReflectionAppDomain() ?? throw new InvalidOperationException();
			return engine.CreateDotNetValue_CorDebug(corException, reflectionAppDomain, tryCreateStrongHandle: true);
		}

		public DbgDotNetValue GetStowedException(DbgEvaluationInfo evalInfo, uint id) {
			if (Dispatcher.CheckAccess())
				return GetStowedExceptionCore(evalInfo, id);
			return GetStowedException2(evalInfo, id);

			DbgDotNetValue GetStowedException2(DbgEvaluationInfo evalInfo2, uint id2) =>
				Dispatcher.InvokeRethrow(() => GetStowedExceptionCore(evalInfo2, id2));
		}

		DbgDotNetValue GetStowedExceptionCore(DbgEvaluationInfo evalInfo, uint id) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			if (id != DbgDotNetRuntimeConstants.StowedExceptionId)
				return null;
			var corStowedException = TryGetStowedException(evalInfo.Frame);
			if (corStowedException == null)
				return null;
			var reflectionAppDomain = evalInfo.Frame.AppDomain.GetReflectionAppDomain() ?? throw new InvalidOperationException();
			return engine.CreateDotNetValue_CorDebug(corStowedException, reflectionAppDomain, tryCreateStrongHandle: true);
		}

		CorValue TryGetException(DbgStackFrame frame) {
			Dispatcher.VerifyAccess();
			var dnThread = engine.GetThread(frame.Thread);
			return dnThread.CorThread.CurrentException;
		}

		CorValue TryGetStowedException(DbgStackFrame frame) {
			Dispatcher.VerifyAccess();
			return null;//TODO:
		}

		public DbgDotNetValue GetReturnValue(DbgEvaluationInfo evalInfo, uint id) {
			if (Dispatcher.CheckAccess())
				return GetReturnValueCore(evalInfo, id);
			return GetReturnValue2(evalInfo, id);

			DbgDotNetValue GetReturnValue2(DbgEvaluationInfo evalInfo2, uint id2) =>
				Dispatcher.InvokeRethrow(() => GetReturnValueCore(evalInfo2, id2));
		}

		DbgDotNetValue GetReturnValueCore(DbgEvaluationInfo evalInfo, uint id) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			return engine.GetCurrentReturnValue(id);
		}

		DbgDotNetValueResult CreateValue(CorValue value, ILDbgEngineStackFrame ilFrame) {
			var reflectionAppDomain = ilFrame.GetReflectionModule().AppDomain;
			var dnValue = engine.CreateDotNetValue_CorDebug(value, reflectionAppDomain, tryCreateStrongHandle: true);
			return DbgDotNetValueResult.Create(dnValue);
		}

		public DbgDotNetValueResult GetLocalValue(DbgEvaluationInfo evalInfo, uint index) {
			if (Dispatcher.CheckAccess())
				return GetLocalValueCore(evalInfo, index);
			return GetLocalValue2(evalInfo, index);

			DbgDotNetValueResult GetLocalValue2(DbgEvaluationInfo evalInfo2, uint index2) =>
				Dispatcher.InvokeRethrow(() => GetLocalValueCore(evalInfo2, index2));
		}

		DbgDotNetValueResult GetLocalValueCore(DbgEvaluationInfo evalInfo, uint index) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
					throw new InvalidOperationException();
				var value = ilFrame.CorFrame.GetILLocal(index, out int hr);
				if (value == null)
					return DbgDotNetValueResult.CreateError(CordbgErrorHelper.GetErrorMessage(hr));
				return CreateValue(value, ilFrame);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
		}

		public DbgDotNetValueResult GetParameterValue(DbgEvaluationInfo evalInfo, uint index) {
			if (Dispatcher.CheckAccess())
				return GetParameterValueCore(evalInfo, index);
			return GetParameterValue2(evalInfo, index);

			DbgDotNetValueResult GetParameterValue2(DbgEvaluationInfo evalInfo2, uint index2) =>
				Dispatcher.InvokeRethrow(() => GetParameterValueCore(evalInfo2, index2));
		}

		DbgDotNetValueResult GetParameterValueCore(DbgEvaluationInfo evalInfo, uint index) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
					throw new InvalidOperationException();
				var value = ilFrame.CorFrame.GetILArgument(index, out int hr);
				if (value == null)
					return DbgDotNetValueResult.CreateError(CordbgErrorHelper.GetErrorMessage(hr));
				return CreateValue(value, ilFrame);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
		}

		public string SetLocalValue(DbgEvaluationInfo evalInfo, uint index, DmdType targetType, object value) {
			if (Dispatcher.CheckAccess())
				return SetLocalValueCore(evalInfo, index, targetType, value);
			return SetLocalValue2(evalInfo, index, targetType, value);

			string SetLocalValue2(DbgEvaluationInfo evalInfo2, uint index2, DmdType targetType2, object value2) =>
				Dispatcher.InvokeRethrow(() => SetLocalValueCore(evalInfo2, index2, targetType2, value2));
		}

		string SetLocalValueCore(DbgEvaluationInfo evalInfo, uint index, DmdType targetType, object value) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
					throw new InvalidOperationException();
				return engine.SetLocalValue_CorDebug(evalInfo, ilFrame, index, targetType, value);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return CordbgErrorHelper.InternalError;
			}
		}

		public string SetParameterValue(DbgEvaluationInfo evalInfo, uint index, DmdType targetType, object value) {
			if (Dispatcher.CheckAccess())
				return SetParameterValueCore(evalInfo, index, targetType, value);
			return SetParameterValue2(evalInfo, index, targetType, value);

			string SetParameterValue2(DbgEvaluationInfo evalInfo2, uint index2, DmdType targetType2, object value2) =>
				Dispatcher.InvokeRethrow(() => SetParameterValueCore(evalInfo2, index2, targetType2, value2));
		}

		string SetParameterValueCore(DbgEvaluationInfo evalInfo, uint index, DmdType targetType, object value) {
			Dispatcher.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
					throw new InvalidOperationException();
				return engine.SetParameterValue_CorDebug(evalInfo, ilFrame, index, targetType, value);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return CordbgErrorHelper.InternalError;
			}
		}

		public DbgDotNetValue GetLocalValueAddress(DbgEvaluationInfo evalInfo, uint index, DmdType targetType) => null;
		public DbgDotNetValue GetParameterValueAddress(DbgEvaluationInfo evalInfo, uint index, DmdType targetType) => null;

		public DbgDotNetValueResult CreateValue(DbgEvaluationInfo evalInfo, object value) {
			if (Dispatcher.CheckAccess())
				return CreateValueCore(evalInfo, value);
			return CreateValue2(evalInfo, value);

			DbgDotNetValueResult CreateValue2(DbgEvaluationInfo evalInfo2, object value2) =>
				Dispatcher.InvokeRethrow(() => CreateValueCore(evalInfo2, value2));
		}

		DbgDotNetValueResult CreateValueCore(DbgEvaluationInfo evalInfo, object value) {
			Dispatcher.VerifyAccess();
			try {
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(evalInfo.Frame, out var ilFrame))
					throw new InvalidOperationException();
				return engine.CreateValue_CorDebug(evalInfo, ilFrame, value);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
		}

		public DbgDotNetValueResult Box(DbgEvaluationInfo evalInfo, object value) {
			if (Dispatcher.CheckAccess())
				return BoxCore(evalInfo, value);
			return Box2(evalInfo, value);

			DbgDotNetValueResult Box2(DbgEvaluationInfo evalInfo2, object value2) =>
				Dispatcher.InvokeRethrow(() => BoxCore(evalInfo2, value2));
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
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
			finally {
				res.Value?.Dispose();
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
			if (corValue.IsNull)
				return false;
			if (!corValue.IsHandle) {
				if (corValue.IsReference) {
					if (corValue.IsNull)
						return false;
					corValue = corValue.GetDereferencedValue(out int hr);
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
			if (corValue.IsNull)
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
					corValue = corValue.GetDereferencedValue(out int hr);
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

		readonly struct EquatableValue {
			public readonly ulong Address;
			readonly DmdType type;

			public EquatableValue(DmdType type, CorValue value) {
				if (value == null)
					Address = 0;
				else if (type.IsByRef)
					Address = value.ReferenceAddress;
				else {
					if (value.IsReference)
						value = value.GetDereferencedValue(out int hr);
					Address = value?.Address ?? 0;
				}
				this.type = type;
			}

			public bool Equals2(in EquatableValue other) => Address != 0 && Address == other.Address;
			public bool? Equals3(in EquatableValue other) => Address == 0 && other.Address == 0 ? (bool?)null : Address == other.Address;
			// Value must be stable, so we can't use Address (obj could get moved by the GC). It's used by dictionaries.
			public new int GetHashCode() => Address == 0 ? 0 : type.AssemblyQualifiedName.GetHashCode();
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

		public DbgDotNetValue GetValue(DbgEvaluationInfo evalInfo, DbgDotNetObjectId objectId) {
			var objectIdImpl = objectId as DbgDotNetObjectIdImpl;
			if (objectIdImpl == null)
				throw new ArgumentException();
			if (Dispatcher.CheckAccess())
				return GetValueCore(evalInfo, objectIdImpl);
			return GetValue2(evalInfo, objectIdImpl);

			DbgDotNetValue GetValue2(DbgEvaluationInfo evalInfo2, DbgDotNetObjectIdImpl objectId2) =>
				Dispatcher.InvokeRethrow(() => GetValueCore(evalInfo2, objectId2));
		}

		DbgDotNetValue GetValueCore(DbgEvaluationInfo evalInfo, DbgDotNetObjectIdImpl objectId) {
			Dispatcher.VerifyAccess();
			var dnValue = objectId.Value.AddRef();
			try {
				return engine.CreateDotNetValue_CorDebug(dnValue);
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

		public bool TryGetNativeCode(DbgStackFrame frame, out DbgDotNetNativeCode nativeCode) {
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame)) {
				nativeCode = default;
				return false;
			}
			if (Dispatcher.CheckAccess())
				return TryGetNativeCodeCore(ilFrame, out nativeCode);
			return TryGetNativeCode2(ilFrame, out nativeCode);

			bool TryGetNativeCode2(ILDbgEngineStackFrame ilFrame2, out DbgDotNetNativeCode nativeCode2) {
				DbgDotNetNativeCode nativeCodeTmp = default;
				bool res = Dispatcher.InvokeRethrow(() => TryGetNativeCodeCore(ilFrame2, out nativeCodeTmp));
				nativeCode2 = nativeCodeTmp;
				return res;
			}
		}
		bool TryGetNativeCodeCore(ILDbgEngineStackFrame ilFrame, out DbgDotNetNativeCode nativeCode) {
			Dispatcher.VerifyAccess();
			if (!engine.IsPaused) {
				nativeCode = default;
				return false;
			}
			var code = ilFrame.CorFrame.Code;
			ilFrame.GetFrameMethodInfo(out var module, out var methodMetadataToken, out var genericTypeArguments, out var genericMethodArguments);
			var reflectionMethod = TryGetMethod(module, methodMetadataToken, genericTypeArguments, genericMethodArguments);
			return TryGetNativeCodeCore(code, reflectionMethod, out nativeCode);
		}

		public bool TryGetNativeCode(DmdMethodBase method, out DbgDotNetNativeCode nativeCode) {
			if (Dispatcher.CheckAccess())
				return TryGetNativeCodeCore(method, out nativeCode);
			return TryGetNativeCode2(method, out nativeCode);

			bool TryGetNativeCode2(DmdMethodBase method2, out DbgDotNetNativeCode nativeCode2) {
				DbgDotNetNativeCode nativeCodeTmp = default;
				bool res = Dispatcher.InvokeRethrow(() => TryGetNativeCodeCore(method2, out nativeCodeTmp));
				nativeCode2 = nativeCodeTmp;
				return res;
			}
		}
		bool TryGetNativeCodeCore(DmdMethodBase method, out DbgDotNetNativeCode nativeCode) {
			Dispatcher.VerifyAccess();
			nativeCode = default;
			if (!engine.IsPaused)
				return false;

			var dbgModule = method.Module.GetDebuggerModule();
			if (dbgModule == null)
				return false;
			if (!engine.TryGetDnModule(dbgModule, out var dnModule))
				return false;
			var func = dnModule.CorModule.GetFunctionFromToken((uint)method.MetadataToken);
			if (func == null)
				return false;
			var code = func.NativeCode;
			if (code == null)
				return false;
			return TryGetNativeCodeCore(code, method, out nativeCode);
		}

		bool TryGetNativeCodeCore(CorCode code, DmdMethodBase reflectionMethod, out DbgDotNetNativeCode nativeCode) {
			nativeCode = default;
			if (code == null)
				return false;

			var process = code.Function?.Module?.Process;
			if (process == null)
				return false;

			// The returned chunks are sorted
			var chunks = code.GetCodeChunks();
			if (chunks.Length == 0)
				return false;

			int totalLen = 0;
			foreach (var chunk in chunks)
				totalLen += (int)chunk.Length;
			var allCodeBytes = new byte[totalLen];
			int currentPos = 0;
			foreach (var chunk in chunks) {
				int hr = process.ReadMemory(chunk.StartAddr, allCodeBytes, currentPos, (int)chunk.Length, out int sizeRead);
				if (hr < 0 || sizeRead != (int)chunk.Length)
					return false;
				currentPos += (int)chunk.Length;
			}
			Debug.Assert(currentPos == totalLen);

			// We must get IL to native mappings before we get var homes, or the var
			// homes array will be empty.
			var map = code.GetILToNativeMapping();
			var varHomes = code.GetVariables();
			Array.Sort(varHomes, (a, b) => {
				int c = a.StartOffset.CompareTo(b.StartOffset);
				if (c != 0)
					return c;
				return a.Length.CompareTo(b.Length);
			});
			for (int i = 0, chunkIndex = 0, chunkOffset = 0; i < varHomes.Length; i++) {
				var startOffset = varHomes[i].StartOffset;
				while (chunkIndex < chunks.Length) {
					if (startOffset < (uint)chunkOffset + chunks[chunkIndex].Length)
						break;
					chunkOffset += (int)chunks[chunkIndex].Length;
					chunkIndex++;
				}
				Debug.Assert(chunkIndex < chunks.Length);
				if (chunkIndex >= chunks.Length) {
					varHomes = Array.Empty<VariableHome>();
					break;
				}
				varHomes[i].StartOffset += chunks[chunkIndex].StartAddr - (uint)chunkOffset;
			}
			Array.Sort(varHomes, (a, b) => {
				int c = a.SlotIndex.CompareTo(b.SlotIndex);
				if (c != 0)
					return c;
				c = a.ArgumentIndex.CompareTo(b.ArgumentIndex);
				if (c != 0)
					return c;
				c = a.StartOffset.CompareTo(b.StartOffset);
				if (c != 0)
					return c;
				return a.Length.CompareTo(b.Length);
			});

			Array.Sort(map, (a, b) => {
				int c = a.nativeStartOffset.CompareTo(b.nativeStartOffset);
				if (c != 0)
					return c;
				return a.nativeEndOffset.CompareTo(b.nativeEndOffset);
			});
			totalLen = 0;
			for (int i = 0; i < chunks.Length; i++) {
				chunks[i].StartAddr -= (uint)totalLen;
				totalLen += (int)chunks[i].Length;
			}
			var blocks = new DbgDotNetNativeCodeBlock[map.Length];
			ulong baseAddress = chunks[0].StartAddr;
			uint chunkByteOffset = 0;
			for (int i = 0, chunkIndex = 0; i < blocks.Length; i++) {
				var info = map[i];
				bool b = info.nativeEndOffset <= (uint)allCodeBytes.Length && info.nativeStartOffset <= info.nativeEndOffset && chunkIndex < chunks.Length;
				Debug.Assert(b);
				if (!b)
					return false;
				int codeLen = (int)(info.nativeEndOffset - info.nativeStartOffset);
				var rawCode = new ArraySegment<byte>(allCodeBytes, (int)info.nativeStartOffset, codeLen);
				ulong address = baseAddress + info.nativeStartOffset;
				if ((CorDebugIlToNativeMappingTypes)info.ilOffset == CorDebugIlToNativeMappingTypes.NO_MAPPING)
					blocks[i] = new DbgDotNetNativeCodeBlock(NativeCodeBlockKind.Unknown, address, rawCode, -1);
				else if ((CorDebugIlToNativeMappingTypes)info.ilOffset == CorDebugIlToNativeMappingTypes.PROLOG)
					blocks[i] = new DbgDotNetNativeCodeBlock(NativeCodeBlockKind.Prolog, address, rawCode, -1);
				else if ((CorDebugIlToNativeMappingTypes)info.ilOffset == CorDebugIlToNativeMappingTypes.EPILOG)
					blocks[i] = new DbgDotNetNativeCodeBlock(NativeCodeBlockKind.Epilog, address, rawCode, -1);
				else
					blocks[i] = new DbgDotNetNativeCodeBlock(NativeCodeBlockKind.Code, address, rawCode, (int)info.ilOffset);

				chunkByteOffset += (uint)codeLen;
				for (;;) {
					if (chunkIndex >= chunks.Length) {
						if (i + 1 == blocks.Length)
							break;
						Debug.Assert(false);
						return false;
					}
					if (chunkByteOffset < chunks[chunkIndex].Length)
						break;
					chunkByteOffset -= chunks[chunkIndex].Length;
					chunkIndex++;
					if (chunkIndex < chunks.Length)
						baseAddress = chunks[chunkIndex].StartAddr;
				}
			}

			var x86Variables = CreateVariables(varHomes) ?? Array.Empty<X86Variable>();
			X86NativeCodeInfo codeInfo = null;
			if (x86Variables.Length != 0)
				codeInfo = new X86NativeCodeInfo(x86Variables);

			NativeCodeOptimization optimization;
			switch (code.CompilerFlags) {
			case CorDebugJITCompilerFlags.CORDEBUG_JIT_DEFAULT:
				optimization = NativeCodeOptimization.Optimized;
				break;

			case CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION:
			case CorDebugJITCompilerFlags.CORDEBUG_JIT_ENABLE_ENC:
				optimization = NativeCodeOptimization.Unoptimized;
				break;

			default:
				Debug.Fail($"Unknown optimization: {code.CompilerFlags}");
				optimization = NativeCodeOptimization.Unknown;
				break;
			}

			NativeCodeKind codeKind;
			switch (Runtime.Process.Machine) {
			case DbgMachine.X64:
				codeKind = NativeCodeKind.X86_64;
				break;

			case DbgMachine.X86:
				codeKind = NativeCodeKind.X86_32;
				break;

			default:
				Debug.Fail($"Unknown machine: {Runtime.Process.Machine}");
				return false;
			}

			var methodName = reflectionMethod?.ToString();
			nativeCode = new DbgDotNetNativeCode(codeKind, optimization, blocks, codeInfo, methodName);
			return true;
		}

		X86Variable[] CreateVariables(VariableHome[] varHomes) {
			var x86Variables = varHomes.Length == 0 ? Array.Empty<X86Variable>() : new X86Variable[varHomes.Length];
			var machine = Runtime.Process.Machine;
			for (int i = 0; i < varHomes.Length; i++) {
				var varHome = varHomes[i];
				bool isLocal;
				int varIndex;
				if (varHome.SlotIndex >= 0) {
					isLocal = true;
					varIndex = varHome.SlotIndex;
				}
				else if (varHome.ArgumentIndex >= 0) {
					isLocal = false;
					varIndex = varHome.ArgumentIndex;
				}
				else
					return null;

				X86VariableLocationKind locationKind;
				X86Register register;
				int memoryOffset;
				switch (varHome.LocationType) {
				case VariableLocationType.VLT_REGISTER:
					locationKind = X86VariableLocationKind.Register;
					if (!TryGetRegister(machine, varHome.Register, out register))
						return null;
					memoryOffset = 0;
					break;

				case VariableLocationType.VLT_REGISTER_RELATIVE:
					locationKind = X86VariableLocationKind.Memory;
					if (!TryGetRegister(machine, varHome.Register, out register))
						return null;
					memoryOffset = varHome.Offset;
					break;

				case VariableLocationType.VLT_INVALID:
					// eg. local is a ulong stored on the stack and it's 32-bit code
					locationKind = X86VariableLocationKind.Other;
					register = X86Register.None;
					memoryOffset = 0;
					break;

				default:
					return null;
				}

				const string varName = null;
				x86Variables[i] = new X86Variable(varName, varIndex, isLocal, varHome.StartOffset, varHome.Length, locationKind, register, memoryOffset);
			}
			return x86Variables;
		}

		static bool TryGetRegister(DbgMachine machine, CorDebugRegister corReg, out X86Register register) {
			switch (machine) {
			case DbgMachine.X86:
				switch (corReg) {
				case CorDebugRegister.REGISTER_X86_EIP:
					register = X86Register.EIP;
					return true;
				case CorDebugRegister.REGISTER_X86_ESP:
					register = X86Register.ESP;
					return true;
				case CorDebugRegister.REGISTER_X86_EBP:
					register = X86Register.EBP;
					return true;
				case CorDebugRegister.REGISTER_X86_EAX:
					register = X86Register.EAX;
					return true;
				case CorDebugRegister.REGISTER_X86_ECX:
					register = X86Register.ECX;
					return true;
				case CorDebugRegister.REGISTER_X86_EDX:
					register = X86Register.EDX;
					return true;
				case CorDebugRegister.REGISTER_X86_EBX:
					register = X86Register.EBX;
					return true;
				case CorDebugRegister.REGISTER_X86_ESI:
					register = X86Register.ESI;
					return true;
				case CorDebugRegister.REGISTER_X86_EDI:
					register = X86Register.EDI;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_0:
					register = X86Register.ST0;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_1:
					register = X86Register.ST1;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_2:
					register = X86Register.ST2;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_3:
					register = X86Register.ST3;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_4:
					register = X86Register.ST4;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_5:
					register = X86Register.ST5;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_6:
					register = X86Register.ST6;
					return true;
				case CorDebugRegister.REGISTER_X86_FPSTACK_7:
					register = X86Register.ST7;
					return true;
				default:
					Debug.Fail($"Unknown register number {(int)corReg}");
					register = default;
					return false;
				}

			case DbgMachine.X64:
				switch (corReg) {
				case CorDebugRegister.REGISTER_AMD64_RIP:
					register = X86Register.RIP;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RSP:
					register = X86Register.RSP;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RBP:
					register = X86Register.RBP;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RAX:
					register = X86Register.RAX;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RCX:
					register = X86Register.RCX;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RDX:
					register = X86Register.RDX;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RBX:
					register = X86Register.RBX;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RSI:
					register = X86Register.RSI;
					return true;
				case CorDebugRegister.REGISTER_AMD64_RDI:
					register = X86Register.RDI;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R8:
					register = X86Register.R8;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R9:
					register = X86Register.R9;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R10:
					register = X86Register.R10;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R11:
					register = X86Register.R11;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R12:
					register = X86Register.R12;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R13:
					register = X86Register.R13;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R14:
					register = X86Register.R14;
					return true;
				case CorDebugRegister.REGISTER_AMD64_R15:
					register = X86Register.R15;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM0:
					register = X86Register.XMM0;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM1:
					register = X86Register.XMM1;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM2:
					register = X86Register.XMM2;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM3:
					register = X86Register.XMM3;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM4:
					register = X86Register.XMM4;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM5:
					register = X86Register.XMM5;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM6:
					register = X86Register.XMM6;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM7:
					register = X86Register.XMM7;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM8:
					register = X86Register.XMM8;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM9:
					register = X86Register.XMM9;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM10:
					register = X86Register.XMM10;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM11:
					register = X86Register.XMM11;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM12:
					register = X86Register.XMM12;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM13:
					register = X86Register.XMM13;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM14:
					register = X86Register.XMM14;
					return true;
				case CorDebugRegister.REGISTER_AMD64_XMM15:
					register = X86Register.XMM15;
					return true;
				default:
					Debug.Fail($"Unknown register number {(int)corReg}");
					register = default;
					return false;
				}

			default:
				Debug.Fail($"Unknown machine: {machine}");
				register = default;
				return false;
			}
		}

		public bool TryGetSymbol(ulong address, out SymbolResolverResult result) {
			if (Dispatcher.CheckAccess())
				return TryGetSymbolCore(address, out result);
			return TryGetSymbolCore2(address, out result);

			bool TryGetSymbolCore2(ulong address2, out SymbolResolverResult result2) {
				SymbolResolverResult resultTmp = default;
				bool res = Dispatcher.InvokeRethrow(() => TryGetSymbolCore(address2, out resultTmp));
				result2 = resultTmp;
				return res;
			}
		}
		bool TryGetSymbolCore(ulong address, out SymbolResolverResult result) {
			Dispatcher.VerifyAccess();
			if (!engine.IsPaused) {
				result = default;
				return false;
			}
			return engine.clrDac.TryGetSymbolCore(address, out result);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
