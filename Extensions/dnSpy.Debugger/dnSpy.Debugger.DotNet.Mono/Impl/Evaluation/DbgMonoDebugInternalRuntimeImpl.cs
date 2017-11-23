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
using System.Runtime.CompilerServices;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.CallStack;
using dnSpy.Debugger.DotNet.Mono.Impl.Evaluation.Hooks;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	sealed class DbgMonoDebugInternalRuntimeImpl : DbgMonoDebugInternalRuntime, IDbgDotNetRuntime, IMonoDebugRuntime {
		public override MonoDebugRuntimeKind Kind { get; }
		public override DmdRuntime ReflectionRuntime { get; }
		public override DbgRuntime Runtime { get; }
		public DbgDotNetDispatcher Dispatcher { get; }
		public bool SupportsObjectIds => true;

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
			reflectionRuntime.GetOrCreateData(() => runtime);

			monoDebugValueConverter = new MonoDebugValueConverterImpl(this);
			classHooks = new Dictionary<DmdWellKnownType, ClassHook>();
			foreach (var info in ClassHookProvider.Create(engine, this)) {
				Debug.Assert(info.Hook != null);
				Debug.Assert(!classHooks.ContainsKey(info.WellKnownType));
				classHooks.Add(info.WellKnownType, info.Hook);
			}
		}

		DmdType ToReflectionType(TypeMirror type, DmdAppDomain reflectionAppDomain) =>
			new ReflectionTypeCreator(engine, reflectionAppDomain).Create(type);

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

			//TODO:
			metadataMethodToken = 0;
			metadataLocalVarSigTok = 0;
			return false;
		}

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
					// Don't throw if it fails to resolve. Callers must be able to handle null return values
					var method = module.ResolveMethod(methodMetadataToken, (IList<DmdType>)null, null, DmdResolveOptions.None);
					if ((object)method != null) {
						if (genericTypeArguments.Count != 0) {
							var type = method.ReflectedType.MakeGenericType(genericTypeArguments);
							method = type.GetMethod(method.Module, method.MetadataToken, throwOnError: true);
						}
						if (genericMethodArguments.Count != 0)
							method = ((DmdMethodInfo)method).MakeGenericMethod(genericMethodArguments);
					}
					state.Method = method;
				}
				state.Initialized = true;
			}
			return state.Method;
		}

		TypeMirror GetType(DmdType type) => MonoDebugTypeCreator.GetType(engine, type);

		public DbgDotNetValue LoadFieldAddress(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, CancellationToken cancellationToken) => null;

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
				var info = GetFieldValueLocationCore(context, frame, obj, field, cancellationToken);
				if (info.errorMessage != null)
					return new DbgDotNetValueResult(info.errorMessage);
				return new DbgDotNetValueResult(engine.CreateDotNetValue_MonoDebug(info.valueLocation), valueIsException: false);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		(ValueLocation valueLocation, string errorMessage) GetFieldValueLocationCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, CancellationToken cancellationToken) {
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				return (null, PredefinedEvaluationErrorMessages.InternalDebuggerError);

			var fieldDeclType = field.DeclaringType;
			var monoFieldDeclType = GetType(fieldDeclType);
			if (obj == null) {
				if (!field.IsStatic)
					return (null, PredefinedEvaluationErrorMessages.InternalDebuggerError);

				if (field.IsLiteral) {
					var monoValue = MonoValueFactory.TryCreateSyntheticValue(engine, monoFieldDeclType.Assembly.Domain, field.FieldType, field.GetRawConstantValue()) ??
						new PrimitiveValue(monoFieldDeclType.VirtualMachine, ElementType.Object, null);
					return (new NoValueLocation(field.FieldType, monoValue), null);
				}
				else {
					var monoField = MemberMirrorUtils.GetMonoField(monoFieldDeclType, field);

					InitializeStaticConstructor(context, frame, ilFrame, fieldDeclType, monoFieldDeclType, cancellationToken);
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

		void InitializeStaticConstructor(DbgEvaluationContext context, DbgStackFrame frame, ILDbgEngineStackFrame ilFrame, DmdType type, TypeMirror monoType, CancellationToken cancellationToken) {
			if (engine.CheckFuncEval(context) != null)
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
				var objValueType = new ReflectionTypeCreator(engine, reflectionAppDomain).Create(monoObjValue.Type);
				var objValueLocation = new NoValueLocation(objValueType, monoObjValue);
				dnObjValue = engine.CreateDotNetValue_MonoDebug(objValueLocation);
				RuntimeHelpersRunClassConstructor(context, frame, type, dnObjValue, cancellationToken);
			}
			finally {
				dnObjValue?.Dispose();
			}
		}

		// Calls System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor():
		//		RuntimeHelpers.RunClassConstructor(obj.GetType().TypeHandle);
		bool RuntimeHelpersRunClassConstructor(DbgEvaluationContext context, DbgStackFrame frame, DmdType type, DbgDotNetValue objValue, CancellationToken cancellationToken) {
			var reflectionAppDomain = type.AppDomain;
			var runtimeTypeHandleType = reflectionAppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle, isOptional: true);
			Debug.Assert((object)runtimeTypeHandleType != null);
			if ((object)runtimeTypeHandleType == null)
				return false;
			var getTypeHandleMethod = objValue.Type.GetMethod("get_" + nameof(Type.TypeHandle), DmdSignatureCallingConvention.Default | DmdSignatureCallingConvention.HasThis, 0, runtimeTypeHandleType, Array.Empty<DmdType>(), throwOnError: false);
			Debug.Assert((object)getTypeHandleMethod != null);
			if ((object)getTypeHandleMethod == null)
				return false;
			var typeHandleRes = engine.FuncEvalCall_MonoDebug(context, frame.Thread, getTypeHandleMethod, objValue, Array.Empty<object>(), DbgDotNetInvokeOptions.None, false, cancellationToken);
			if (typeHandleRes.Value == null || typeHandleRes.ValueIsException)
				return false;
			var runtimeHelpersType = reflectionAppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_CompilerServices_RuntimeHelpers, isOptional: true);
			var runClassConstructorMethod = runtimeHelpersType?.GetMethod(nameof(RuntimeHelpers.RunClassConstructor), DmdSignatureCallingConvention.Default, 0, reflectionAppDomain.System_Void, new[] { runtimeTypeHandleType }, throwOnError: false);
			Debug.Assert((object)runClassConstructorMethod != null);
			if ((object)runClassConstructorMethod == null)
				return false;
			var res = engine.FuncEvalCall_MonoDebug(context, frame.Thread, runClassConstructorMethod, null, new[] { typeHandleRes.Value }, DbgDotNetInvokeOptions.None, false, cancellationToken);
			res.Value?.Dispose();
			return !res.HasError && !res.ValueIsException;
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
				var info = GetFieldValueLocationCore(context, frame, obj, field, cancellationToken);
				if (info.errorMessage != null)
					return info.errorMessage;
				var res = engine.CreateMonoValue_MonoDebug(context, frame.Thread, value, field.FieldType, cancellationToken);
				if (res.ErrorMessage != null)
					return res.ErrorMessage;
				info.valueLocation.Store(res.Value);
				return null;
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return PredefinedEvaluationErrorMessages.InternalDebuggerError;
			}
		}

		public DbgDotNetValueResult Call(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, DbgDotNetInvokeOptions invokeOptions, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CallCore(context, frame, obj, method, arguments, invokeOptions, cancellationToken);
			return Call2(context, frame, obj, method, arguments, invokeOptions, cancellationToken);

			DbgDotNetValueResult Call2(DbgEvaluationContext context2, DbgStackFrame frame2, DbgDotNetValue obj2, DmdMethodBase method2, object[] arguments2, DbgDotNetInvokeOptions invokeOptions2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => CallCore(context2, frame2, obj2, method2, arguments2, invokeOptions2, cancellationToken2));
		}

		DbgDotNetValueResult CallCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdMethodBase method, object[] arguments, DbgDotNetInvokeOptions invokeOptions, CancellationToken cancellationToken) {
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
							return new DbgDotNetValueResult(res, valueIsException: false);
					}
				}

				return engine.FuncEvalCall_MonoDebug(context, frame.Thread, method, obj, arguments, invokeOptions, newObj: false, cancellationToken: cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		public DbgDotNetValueResult CreateInstance(DbgEvaluationContext context, DbgStackFrame frame, DmdConstructorInfo ctor, object[] arguments, DbgDotNetInvokeOptions invokeOptions, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return CreateInstanceCore(context, frame, ctor, arguments, invokeOptions, cancellationToken);
			return CreateInstance2(context, frame, ctor, arguments, invokeOptions, cancellationToken);

			DbgDotNetValueResult CreateInstance2(DbgEvaluationContext context2, DbgStackFrame frame2, DmdConstructorInfo ctor2, object[] arguments2, DbgDotNetInvokeOptions invokeOptions2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => CreateInstanceCore(context2, frame2, ctor2, arguments2, invokeOptions2, cancellationToken2));
		}

		DbgDotNetValueResult CreateInstanceCore(DbgEvaluationContext context, DbgStackFrame frame, DmdConstructorInfo ctor, object[] arguments, DbgDotNetInvokeOptions invokeOptions, CancellationToken cancellationToken) {
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
							return new DbgDotNetValueResult(res, valueIsException: false);
					}
				}

				return engine.FuncEvalCall_MonoDebug(context, frame.Thread, ctor, null, arguments, invokeOptions, newObj: true, cancellationToken: cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
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
				if (type.IsValueType)
					return CreateValueTypeInstanceNoConstructorCore(context, frame, type, cancellationToken);
				return CreateReferenceTypeInstanceNoConstructorCore(context, frame, type, cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		DbgDotNetValueResult CreateValueTypeInstanceNoConstructorCore(DbgEvaluationContext context, DbgStackFrame frame, DmdType type, CancellationToken cancellationToken) {
			var structMirror = CreateValueType(type, 0);
			return new DbgDotNetValueResult(engine.CreateDotNetValue_MonoDebug(type.AppDomain, structMirror), valueIsException: false);
		}

		Value CreateValueType(DmdType type, int recursionCounter) {
			if (recursionCounter > 100)
				throw new InvalidOperationException();
			if (!type.IsValueType)
				throw new InvalidOperationException();
			var monoType = GetType(type);
			var fields = type.DeclaredFields;
			var monoFields = monoType.GetFields();
			if (fields.Count != monoFields.Length)
				throw new InvalidOperationException();
			var fieldValues = new List<Value>(monoFields.Length);
			for (int i = 0; i < monoFields.Length; i++) {
				Debug.Assert(fields[i].Name == monoFields[i].Name);
				var field = fields[i];
				if (field.IsStatic || field.IsLiteral)
					continue;
				fieldValues.Add(CreateDefaultValue(field, 0));
			}
			if (type.IsEnum)
				return monoType.VirtualMachine.CreateEnumMirror(monoType, (PrimitiveValue)fieldValues[0]);
			return monoType.VirtualMachine.CreateStructMirror(monoType, fieldValues.ToArray());
		}

		Value CreateDefaultValue(DmdFieldInfo field, int recursionCounter) {
			var type = field.FieldType;
			if (!type.IsValueType)
				return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.Object, null);
			if (type.IsPointer || type.IsFunctionPointer)
				return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.Ptr, 0L);
			if (!type.IsEnum) {
				switch (DmdType.GetTypeCode(type)) {
				case TypeCode.Boolean:		return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.Boolean, false);
				case TypeCode.Char:			return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.Char, '\0');
				case TypeCode.SByte:		return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.I1, (sbyte)0);
				case TypeCode.Byte:			return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.U1, (byte)0);
				case TypeCode.Int16:		return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.I2, (short)0);
				case TypeCode.UInt16:		return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.U2, (ushort)0);
				case TypeCode.Int32:		return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.I4, 0);
				case TypeCode.UInt32:		return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.U4, 0U);
				case TypeCode.Int64:		return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.I8, 0L);
				case TypeCode.UInt64:		return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.U8, 0UL);
				case TypeCode.Single:		return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.R4, 0f);
				case TypeCode.Double:		return new PrimitiveValue(engine.MonoVirtualMachine, ElementType.R8, 0d);
				}
			}
			return CreateValueType(type, recursionCounter + 1);
		}

		DbgDotNetValueResult CreateReferenceTypeInstanceNoConstructorCore(DbgEvaluationContext context, DbgStackFrame frame, DmdType type, CancellationToken cancellationToken) {
			if (engine.MonoVirtualMachine.Version.AtLeast(2, 31)) {
				var monoType = GetType(type);
				var value = monoType.NewInstance();
				return new DbgDotNetValueResult(engine.CreateDotNetValue_MonoDebug(type.AppDomain, value), valueIsException: false);
			}
			else {
				var ctor = type.GetMethod(DmdConstructorInfo.ConstructorName, DmdSignatureCallingConvention.HasThis, 0, type.AppDomain.System_Void, Array.Empty<DmdType>(), throwOnError: false) as DmdConstructorInfo;
				if ((object)ctor == null)
					return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
				return CreateInstanceCore(context, frame, ctor, Array.Empty<object>(), DbgDotNetInvokeOptions.None, cancellationToken);
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
			var appDomain = elementType.AppDomain;
			DbgDotNetValue typeElementType = null;
			try {
				typeElementType = engine.CreateDotNetValue_MonoDebug(appDomain, GetType(elementType).GetTypeObject());
				var methodCreateInstance = appDomain.System_Array.GetMethod(nameof(Array.CreateInstance), DmdSignatureCallingConvention.Default, 0, appDomain.System_Array, new[] { appDomain.System_Type, appDomain.System_Int32 }, throwOnError: true);
				return engine.FuncEvalCall_MonoDebug(context, frame.Thread, methodCreateInstance, null, new object[] { typeElementType, length }, DbgDotNetInvokeOptions.None, false, cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
			finally {
				typeElementType?.Dispose();
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
			var appDomain = elementType.AppDomain;
			DbgDotNetValue typeElementType = null;
			try {
				var lengths = new int[dimensionInfos.Length];
				var lowerBounds = new int[dimensionInfos.Length];
				for (int i = 0; i < dimensionInfos.Length; i++) {
					lengths[i] = (int)dimensionInfos[i].Length;
					lowerBounds[i] = dimensionInfos[i].BaseIndex;
				}

				typeElementType = engine.CreateDotNetValue_MonoDebug(appDomain, GetType(elementType).GetTypeObject());
				var methodCreateInstance = appDomain.System_Array.GetMethod(nameof(Array.CreateInstance), DmdSignatureCallingConvention.Default, 0, appDomain.System_Array, new[] { appDomain.System_Type, appDomain.System_Int32.MakeArrayType(), appDomain.System_Int32.MakeArrayType() }, throwOnError: true);
				return engine.FuncEvalCall_MonoDebug(context, frame.Thread, methodCreateInstance, null, new object[] { typeElementType, lengths, lowerBounds }, DbgDotNetInvokeOptions.None, false, cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
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

			DbgDotNetValue exception = null;
			DbgDotNetValue stowedException = null;
			var returnValues = Array.Empty<DbgDotNetReturnValueInfo>();
			try {
				exception = GetExceptionCore(context, frame, DbgDotNetRuntimeConstants.ExceptionId, cancellationToken);
				stowedException = GetStowedExceptionCore(context, frame, DbgDotNetRuntimeConstants.StowedExceptionId, cancellationToken);
				returnValues = GetReturnValuesCore(context, frame, cancellationToken);

				int count = (exception != null ? 1 : 0) + (stowedException != null ? 1 : 0) + returnValues.Length + (returnValues.Length != 0 ? 1 : 0);
				if (count == 0)
					return Array.Empty<DbgDotNetAliasInfo>();

				var res = new DbgDotNetAliasInfo[count];
				int w = 0;
				if (exception != null)
					res[w++] = new DbgDotNetAliasInfo(DbgDotNetAliasInfoKind.Exception, exception.Type, 0, Guid.Empty, null);
				if (stowedException != null)
					res[w++] = new DbgDotNetAliasInfo(DbgDotNetAliasInfoKind.StowedException, stowedException.Type, 0, Guid.Empty, null);
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
			DbgDotNetValue exception = null;
			DbgDotNetValue stowedException = null;
			try {
				exception = GetExceptionCore(context, frame, DbgDotNetRuntimeConstants.ExceptionId, cancellationToken);
				stowedException = GetStowedExceptionCore(context, frame, DbgDotNetRuntimeConstants.StowedExceptionId, cancellationToken);
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
			// Not supported by mono
			return Array.Empty<DbgDotNetReturnValueInfo>();
		}

		public DbgDotNetValue GetException(DbgEvaluationContext context, DbgStackFrame frame, uint id, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetExceptionCore(context, frame, id, cancellationToken);
			return GetException2(context, frame, id, cancellationToken);

			DbgDotNetValue GetException2(DbgEvaluationContext context2, DbgStackFrame frame2, uint id2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetExceptionCore(context2, frame2, id2, cancellationToken2));
		}

		DbgDotNetValue GetExceptionCore(DbgEvaluationContext context, DbgStackFrame frame, uint id, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			if (id != DbgDotNetRuntimeConstants.ExceptionId)
				return null;
			return engine.TryGetExceptionValue();
		}

		public DbgDotNetValue GetStowedException(DbgEvaluationContext context, DbgStackFrame frame, uint id, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetStowedExceptionCore(context, frame, id, cancellationToken);
			return GetStowedException2(context, frame, id, cancellationToken);

			DbgDotNetValue GetStowedException2(DbgEvaluationContext context2, DbgStackFrame frame2, uint id2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetStowedExceptionCore(context2, frame2, id2, cancellationToken2));
		}

		DbgDotNetValue GetStowedExceptionCore(DbgEvaluationContext context, DbgStackFrame frame, uint id, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return null;
		}

		public DbgDotNetValue GetReturnValue(DbgEvaluationContext context, DbgStackFrame frame, uint id, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetReturnValueCore(context, frame, id, cancellationToken);
			return GetReturnValue2(context, frame, id, cancellationToken);

			DbgDotNetValue GetReturnValue2(DbgEvaluationContext context2, DbgStackFrame frame2, uint id2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetReturnValueCore(context2, frame2, id2, cancellationToken2));
		}

		DbgDotNetValue GetReturnValueCore(DbgEvaluationContext context, DbgStackFrame frame, uint id, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			// Not supported by mono
			return null;
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
				var info = GetLocalValueLocationCore(context, frame, index, cancellationToken);
				if (info.errorMessage != null)
					return new DbgDotNetValueResult(info.errorMessage);
				return new DbgDotNetValueResult(engine.CreateDotNetValue_MonoDebug(info.valueLocation), valueIsException: false);
			}
			catch (AbsentInformationException) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		(ValueLocation valueLocation, string errorMessage) GetLocalValueLocationCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				throw new InvalidOperationException();
			var monoFrame = ilFrame.MonoFrame;
			var locals = monoFrame.Method.GetLocals();
			if ((uint)index >= (uint)locals.Length)
				return (null, PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
			var local = locals[(int)index];
			var reflectionAppDomain = ilFrame.GetReflectionModule().AppDomain;
			var type = ToReflectionType(local.Type, reflectionAppDomain);

			var method = GetFrameMethodCore(context, frame, cancellationToken);
			type = AddByRefIfNeeded(type, GetCachedMethodBody(method)?.LocalVariables, index);

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
				var info = GetParameterValueLocationCore(context, frame, index, cancellationToken);
				if (info.errorMessage != null)
					return new DbgDotNetValueResult(info.errorMessage);
				return new DbgDotNetValueResult(engine.CreateDotNetValue_MonoDebug(info.valueLocation), valueIsException: false);
			}
			catch (AbsentInformationException) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		(ValueLocation valueLocation, string errorMessage) GetParameterValueLocationCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
				throw new InvalidOperationException();
			DmdType type;
			var monoFrame = ilFrame.MonoFrame;
			var reflectionAppDomain = ilFrame.GetReflectionModule().AppDomain;
			if (!monoFrame.Method.IsStatic) {
				if (index == 0) {
					type = ToReflectionType(monoFrame.Method.DeclaringType, reflectionAppDomain);
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

			type = ToReflectionType(parameter.ParameterType, reflectionAppDomain);
			var method = GetFrameMethodCore(context, frame, cancellationToken);
			type = AddByRefIfNeeded(type, method?.GetMethodSignature().GetParameterTypes(), index);

			return (new ArgumentValueLocation(type, ilFrame, (int)index), null);
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
				var info = GetLocalValueLocationCore(context, frame, index, cancellationToken);
				if (info.errorMessage != null)
					return info.errorMessage;
				var res = engine.CreateMonoValue_MonoDebug(context, frame.Thread, value, targetType, cancellationToken);
				if (res.ErrorMessage != null)
					return res.ErrorMessage;
				info.valueLocation.Store(res.Value);
				return null;
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return PredefinedEvaluationErrorMessages.InternalDebuggerError;
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
				var info = GetParameterValueLocationCore(context, frame, index, cancellationToken);
				if (info.errorMessage != null)
					return info.errorMessage;
				var res = engine.CreateMonoValue_MonoDebug(context, frame.Thread, value, targetType, cancellationToken);
				if (res.ErrorMessage != null)
					return res.ErrorMessage;
				info.valueLocation.Store(res.Value);
				return null;
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return PredefinedEvaluationErrorMessages.InternalDebuggerError;
			}
		}

		public DbgDotNetValue GetLocalValueAddress(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, CancellationToken cancellationToken) => null;
		public DbgDotNetValue GetParameterValueAddress(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, CancellationToken cancellationToken) => null;

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
				return engine.CreateValue_MonoDebug(context, frame.Thread, value, cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetCreateValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		public DbgDotNetCreateValueResult Box(DbgEvaluationContext context, DbgStackFrame frame, object value, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return BoxCore(context, frame, value, cancellationToken);
			return Box2(context, frame, value, cancellationToken);

			DbgDotNetCreateValueResult Box2(DbgEvaluationContext context2, DbgStackFrame frame2, object value2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => BoxCore(context2, frame2, value2, cancellationToken2));
		}

		DbgDotNetCreateValueResult BoxCore(DbgEvaluationContext context, DbgStackFrame frame, object value, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			DbgDotNetCreateValueResult res = default;
			try {
				res = CreateValueCore(context, frame, value, cancellationToken);
				if (res.Error != null)
					return res;
				var boxedValue = res.Value.Box(context, frame, cancellationToken);
				if (boxedValue != null)
					return new DbgDotNetCreateValueResult(boxedValue);
				return new DbgDotNetCreateValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetCreateValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
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

			DbgDotNetObjectId CreateObjectId2(DbgDotNetValueImpl value2, uint id2) =>
				Dispatcher.InvokeRethrow(() => CreateObjectIdCore(value2, id2));
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

			void FreeObjectId2(DbgDotNetObjectIdImpl objectId2) =>
				Dispatcher.BeginInvoke(() => FreeObjectIdCore(objectId2));
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

			bool Equals2(DbgDotNetObjectIdImpl objectId2, DbgDotNetValueImpl value2) =>
				Dispatcher.InvokeRethrow(() => EqualsCore(objectId2, value2));
		}

		struct EquatableValue {
			public readonly ulong Address;

			public EquatableValue(ObjectMirror value) {
				if (value == null)
					Address = 0;
				else
					Address = (ulong)value.Address;
			}

			public bool Equals2(EquatableValue other) => Address != 0 && Address == other.Address;
			public bool? Equals3(EquatableValue other) => Address == 0 && other.Address == 0 ? (bool?)null : Address == other.Address;
			public new int GetHashCode() => Address == 0 ? 0 : Address.GetHashCode();
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

			int GetHashCode2(DbgDotNetObjectIdImpl objectId2) =>
				Dispatcher.InvokeRethrow(() => GetHashCodeCore(objectId2));
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

			int GetHashCode2(DbgDotNetValueImpl value2) =>
				Dispatcher.InvokeRethrow(() => GetHashCodeCore(value2));
		}

		int GetHashCodeCore(DbgDotNetValueImpl value) {
			Dispatcher.VerifyAccess();
			return GetEquatableValue(value.Value as ObjectMirror).GetHashCode();
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
			return engine.CreateDotNetValue_MonoDebug(objectId.ReflectionAppDomain, objectId.Value);
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
			return GetEquatableValue(a.Value as ObjectMirror).Equals3(GetEquatableValue(b.Value as ObjectMirror));
		}

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
