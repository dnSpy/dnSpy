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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Metadata;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.CallStack;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	sealed class DbgMonoDebugInternalRuntimeImpl : DbgMonoDebugInternalRuntime, IDbgDotNetRuntime {
		public override MonoDebugRuntimeKind Kind { get; }
		public override DmdRuntime ReflectionRuntime { get; }
		public override DbgRuntime Runtime { get; }
		public DbgDotNetDispatcher Dispatcher { get; }
		public bool SupportsObjectIds => false;//TODO:

		readonly DbgEngineImpl engine;

		public DbgMonoDebugInternalRuntimeImpl(DbgEngineImpl engine, DbgRuntime runtime, DmdRuntime reflectionRuntime, MonoDebugRuntimeKind monoDebugRuntimeKind) {
			this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			ReflectionRuntime = reflectionRuntime ?? throw new ArgumentNullException(nameof(reflectionRuntime));
			Kind = monoDebugRuntimeKind;
			Dispatcher = new DbgDotNetDispatcherImpl(engine);
			reflectionRuntime.GetOrCreateData(() => runtime);
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
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					return new DbgDotNetValueResult(ErrorHelper.InternalError);

				TypeMirror monoFieldDeclType;
				var fieldDeclType = field.DeclaringType;
				if (obj == null) {
					if (!field.IsStatic)
						return new DbgDotNetValueResult(ErrorHelper.InternalError);

					if (field.IsLiteral)
						return CreateSyntheticValue(field.FieldType, field.GetRawConstantValue());
					else {
						monoFieldDeclType = GetType(fieldDeclType);
						var monoField = MemberMirrorUtils.GetMonoField(monoFieldDeclType, field);

						InitializeStaticConstructor(context, frame, ilFrame, fieldDeclType, monoFieldDeclType, cancellationToken);
						var valueLocation = new StaticFieldValueLocation(field.FieldType, ilFrame.MonoFrame.Thread, monoField);
						return new DbgDotNetValueResult(engine.CreateDotNetValue_MonoDebug(valueLocation), valueIsException: false);
					}
				}
				else {
					if (field.IsStatic)
						return new DbgDotNetValueResult(ErrorHelper.InternalError);

					var objImp = obj as DbgDotNetValueImpl ?? throw new InvalidOperationException();
					monoFieldDeclType = GetType(fieldDeclType);
					var monoField = MemberMirrorUtils.GetMonoField(monoFieldDeclType, field);
					ValueLocation valueLocation;
					switch (objImp.Value) {
					case ObjectMirror om:
						valueLocation = new ReferenceTypeFieldValueLocation(field.FieldType, om, monoField);
						break;

					case StructMirror sm:
						valueLocation = new ValueTypeFieldValueLocation(field.FieldType, objImp.ValueLocation, sm, monoField);
						break;

					case PrimitiveValue pv:
						return new DbgDotNetValueResult("NYI");//TODO:

					default:
						// Unreachable
						return new DbgDotNetValueResult(ErrorHelper.InternalError);
					}
					return new DbgDotNetValueResult(engine.CreateDotNetValue_MonoDebug(valueLocation), valueIsException: false);
				}
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(ErrorHelper.InternalError);
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
			if (typeHandleRes.Value == null | typeHandleRes.ValueIsException)
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
				foreach (var v in sm.Fields) {
					if (!IsZero(v, recursionCounter + 1))
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
				return "NYI";//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return ErrorHelper.InternalError;
			}
		}

		static DbgDotNetValueResult CreateSyntheticValue(DmdType type, object constant) {
			var dnValue = SyntheticValueFactory.TryCreateSyntheticValue(type.AppDomain, constant);
			if (dnValue != null)
				return new DbgDotNetValueResult(dnValue, valueIsException: false);
			return new DbgDotNetValueResult(ErrorHelper.InternalError);
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
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					return new DbgDotNetValueResult(ErrorHelper.InternalError);
				return engine.FuncEvalCall_MonoDebug(context, frame.Thread, method, obj, arguments, invokeOptions, newObj: false, cancellationToken: cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(ErrorHelper.InternalError);
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
				if (!ILDbgEngineStackFrame.TryGetEngineStackFrame(frame, out var ilFrame))
					return new DbgDotNetValueResult(ErrorHelper.InternalError);
				return engine.FuncEvalCall_MonoDebug(context, frame.Thread, ctor, null, arguments, invokeOptions, newObj: true, cancellationToken: cancellationToken);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(ErrorHelper.InternalError);
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
				return new DbgDotNetValueResult("NYI");//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(ErrorHelper.InternalError);
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
				return new DbgDotNetValueResult("NYI");//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(ErrorHelper.InternalError);
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
			try {
				return new DbgDotNetValueResult("NYI");//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(ErrorHelper.InternalError);
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
			return Array.Empty<DbgDotNetReturnValueInfo>();//TODO:
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
			return null;//TODO: If id==DbgDotNetRuntimeConstants.LastReturnValueId, return the last return value
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
				var monoFrame = ilFrame.MonoFrame;
				var locals = monoFrame.Method.GetLocals();
				if ((uint)index >= (uint)locals.Length)
					return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
				var local = locals[(int)index];
				var reflectionAppDomain = ilFrame.GetReflectionModule().AppDomain;
				var type = ToReflectionType(local.Type, reflectionAppDomain);
				var valueLocation = new LocalValueLocation(type, ilFrame, (int)index);
				var dnValue = engine.CreateDotNetValue_MonoDebug(valueLocation);
				return new DbgDotNetValueResult(dnValue, valueIsException: false);
			}
			catch (AbsentInformationException) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(ErrorHelper.InternalError);
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
				DmdType type;
				var monoFrame = ilFrame.MonoFrame;
				var reflectionAppDomain = ilFrame.GetReflectionModule().AppDomain;
				if (!monoFrame.Method.IsStatic) {
					if (index == 0) {
						type = ToReflectionType(monoFrame.Method.DeclaringType, reflectionAppDomain);
						return new DbgDotNetValueResult(engine.CreateDotNetValue_MonoDebug(new ThisValueLocation(type, ilFrame)), valueIsException: false);
					}
					index--;
				}
				var parameters = monoFrame.Method.GetParameters();
				if ((uint)index >= (uint)parameters.Length)
					return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
				var parameter = parameters[(int)index];
				type = ToReflectionType(parameter.ParameterType, reflectionAppDomain);
				var valueLocation = new ArgumentValueLocation(type, ilFrame, (int)index);
				return new DbgDotNetValueResult(engine.CreateDotNetValue_MonoDebug(valueLocation), valueIsException: false);
			}
			catch (AbsentInformationException) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CannotReadLocalOrArgumentMaybeOptimizedAway);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(ErrorHelper.InternalError);
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
				return "NYI";//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return ErrorHelper.InternalError;
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
				return "NYI";//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return ErrorHelper.InternalError;
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
				return new DbgDotNetCreateValueResult("NYI");//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetCreateValueResult(ErrorHelper.InternalError);
			}
		}

		public bool CanCreateObjectId(DbgDotNetValue value) {
			return false;//TODO:
		}

		public DbgDotNetObjectId CreateObjectId(DbgDotNetValue value, uint id) {
			throw new NotImplementedException();//TODO:
		}

		public bool Equals(DbgDotNetObjectId objectId, DbgDotNetValue value) {
			throw new NotImplementedException();//TODO:
		}

		public int GetHashCode(DbgDotNetObjectId objectId) {
			throw new NotImplementedException();//TODO:
		}

		public int GetHashCode(DbgDotNetValue value) {
			throw new NotImplementedException();//TODO:
		}

		public DbgDotNetValue GetValue(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetObjectId objectId, CancellationToken cancellationToken) {
			throw new NotImplementedException();//TODO:
		}

		public bool? Equals(DbgDotNetValue a, DbgDotNetValue b) {
			throw new NotImplementedException();//TODO:
		}

		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
