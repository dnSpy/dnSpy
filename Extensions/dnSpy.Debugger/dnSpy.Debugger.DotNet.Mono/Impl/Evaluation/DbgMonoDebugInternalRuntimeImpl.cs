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

		public DbgDotNetValue LoadFieldAddress(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return LoadFieldAddressCore(context, frame, obj, field, cancellationToken);
			return LoadFieldAddressCore2(context, frame, obj, field, cancellationToken);

			DbgDotNetValue LoadFieldAddressCore2(DbgEvaluationContext context2, DbgStackFrame frame2, DbgDotNetValue obj2, DmdFieldInfo field2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => LoadFieldAddressCore(context2, frame2, obj2, field2, cancellationToken2));
		}

		DbgDotNetValue LoadFieldAddressCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, DmdFieldInfo field, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			return null;//TODO:
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
				return new DbgDotNetValueResult("NYI");//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(ErrorHelper.InternalError);
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
				return "NYI";//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return ErrorHelper.InternalError;
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
				return new DbgDotNetValueResult("NYI");//TODO:
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(ErrorHelper.InternalError);
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
				return new DbgDotNetValueResult("NYI");//TODO:
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
			var corException = TryGetException(frame);
			if (corException == null)
				return null;
			return null;//TODO:
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

		object TryGetException(DbgStackFrame frame) {
			Dispatcher.VerifyAccess();
			return null;//TODO:
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

		DbgDotNetValueResult CreateValue(Value value, ILDbgEngineStackFrame ilFrame, TypeMirror slotTypeMirror) {
			var reflectionAppDomain = ilFrame.GetReflectionModule().AppDomain;
			var slotType = ToReflectionType(slotTypeMirror, reflectionAppDomain);
			if (value == null)
				return new DbgDotNetValueResult(new SyntheticNullValue(slotType), valueIsException: false);
			var dnValue = engine.CreateDotNetValue_MonoDebug(value, slotType);
			return new DbgDotNetValueResult(dnValue, valueIsException: false);
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
				var value = monoFrame.GetValue(local);
				return CreateValue(value, ilFrame, local.Type);
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
				return new DbgDotNetValueResult("NYI");//TODO:
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

		public DbgDotNetValue GetLocalValueAddress(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetLocalValueAddressCore(context, frame, index, targetType, cancellationToken);
			return GetLocalValueAddressCore2(context, frame, index, targetType, cancellationToken);

			DbgDotNetValue GetLocalValueAddressCore2(DbgEvaluationContext context2, DbgStackFrame frame2, uint index2, DmdType targetType2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetLocalValueAddressCore(context2, frame2, index2, targetType2, cancellationToken2));
		}

		DbgDotNetValue GetLocalValueAddressCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			return null;//TODO:
		}

		public DbgDotNetValue GetParameterValueAddress(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, CancellationToken cancellationToken) {
			if (Dispatcher.CheckAccess())
				return GetParameterValueAddressCore(context, frame, index, targetType, cancellationToken);
			return GetParameterValueAddressCore2(context, frame, index, targetType, cancellationToken);

			DbgDotNetValue GetParameterValueAddressCore2(DbgEvaluationContext context2, DbgStackFrame frame2, uint index2, DmdType targetType2, CancellationToken cancellationToken2) =>
				Dispatcher.InvokeRethrow(() => GetParameterValueAddressCore(context2, frame2, index2, targetType2, cancellationToken2));
		}

		DbgDotNetValue GetParameterValueAddressCore(DbgEvaluationContext context, DbgStackFrame frame, uint index, DmdType targetType, CancellationToken cancellationToken) {
			Dispatcher.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			return null;//TODO:
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
