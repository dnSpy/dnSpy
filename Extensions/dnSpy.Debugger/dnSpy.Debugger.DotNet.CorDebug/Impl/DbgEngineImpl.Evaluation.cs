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
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.CorDebug.CallStack;
using dnSpy.Debugger.DotNet.CorDebug.Impl.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	abstract partial class DbgEngineImpl {
		internal DbgDotNetValue CreateDotNetValue_CorDebug(CorValue value, DmdAppDomain reflectionAppDomain, bool tryCreateStrongHandle) {
			debuggerThread.VerifyAccess();
			if (value == null)
				return new SyntheticValue(reflectionAppDomain.System_Void, new DbgDotNetRawValue(DbgSimpleValueType.Void));

			try {
				var type = new ReflectionTypeCreator(this, reflectionAppDomain).Create(value.ExactType);

				if (tryCreateStrongHandle && !value.IsNull && !value.IsHandle && value.IsReference && !type.IsPointer && !type.IsFunctionPointer && !type.IsByRef) {
					var strongHandle = value.DereferencedValue?.CreateHandle(CorDebugHandleType.HANDLE_STRONG);
					Debug.Assert(strongHandle != null || type == type.AppDomain.System_TypedReference);
					if (strongHandle != null)
						value = strongHandle;
				}

				var dnValue = new DbgDotNetValueImpl(this, new DbgCorValueHolder(this, value, type));
				lock (lockObj)
					dotNetValuesToCloseOnContinue.Add(dnValue);
				return dnValue;
			}
			catch {
				dnDebugger.DisposeHandle(value);
				throw;
			}
		}

		internal void Close(DbgCorValueHolder value) {
			if (CheckCorDebugThread()) {
				value.Dispose_CorDebug();
				return;
			}

			bool start;
			lock (lockObj) {
				start = valuesToCloseNow.Count == 0;
				valuesToCloseNow.Add(value);
			}
			if (start)
				CorDebugThread(CloseValuesNow_CorDebug);
		}

		void CloseValuesNow_CorDebug() {
			debuggerThread.VerifyAccess();
			DbgCorValueHolder[] values;
			lock (lockObj) {
				values = valuesToCloseNow.ToArray();
				valuesToCloseNow.Clear();
			}
			foreach (var value in values)
				value.Dispose_CorDebug();
		}

		void CloseDotNetValues_CorDebug() {
			debuggerThread.VerifyAccess();
			DbgDotNetValueImpl[] valuesToClose;
			lock (lockObj) {
				valuesToClose = dotNetValuesToCloseOnContinue.Count == 0 ? Array.Empty<DbgDotNetValueImpl>() : dotNetValuesToCloseOnContinue.ToArray();
				dotNetValuesToCloseOnContinue.Clear();
			}
			foreach (var value in valuesToClose)
				value.Dispose();
		}

		internal void DisposeHandle_CorDebug(CorValue value) {
			Debug.Assert(debuggerThread.CheckAccess());
			dnDebugger.DisposeHandle(value);
		}

		internal CorType GetType(CorAppDomain appDomain, DmdType type) => CorDebugTypeCreator.GetType(this, appDomain, type);

		sealed class EvalTimedOut { }

		internal DbgDotNetValueResult? CheckFuncEval(DbgEvaluationContext context) {
			debuggerThread.VerifyAccess();
			if (dnDebugger.ProcessState != DebuggerProcessState.Paused)
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CanFuncEvalOnlyWhenPaused);
			if (isUnhandledException)
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CantFuncEvalWhenUnhandledExceptionHasOccurred);
			if (context.ContinueContext.HasData<EvalTimedOut>())
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOutNowDisabled);
			if (dnDebugger.IsEvaluating)
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CantFuncEval);
			return null;
		}

		internal DbgDotNetValueResult FuncEvalCall_CorDebug(DbgEvaluationContext context, DbgThread thread, CorAppDomain appDomain, DmdMethodBase method, DbgDotNetValue obj, object[] arguments, bool newObj, CancellationToken cancellationToken) {
			debuggerThread.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(context);
			if (tmp != null)
				return tmp.Value;
			Debug.Assert(!newObj || method.IsConstructor);

			Debug.Assert(method.SpecialMethodKind == DmdSpecialMethodKind.Metadata, "Methods not defined in metadata should be emulated by other code (i.e., the caller)");
			if (method.SpecialMethodKind != DmdSpecialMethodKind.Metadata)
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);

			var reflectionAppDomain = thread.AppDomain.GetReflectionAppDomain() ?? throw new InvalidOperationException();
			var methodDbgModule = method.Module.GetDebuggerModule() ?? throw new InvalidOperationException();
			if (!TryGetDnModule(methodDbgModule, out var methodModule))
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			var func = methodModule.CorModule.GetFunctionFromToken((uint)method.MetadataToken) ?? throw new InvalidOperationException();

			var dnThread = GetThread(thread);
			var createdValues = new List<CorValue>();
			try {
				using (var dnEval = dnDebugger.CreateEval(cancellationToken, suspendOtherThreads: (context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, context);

					var converter = new EvalArgumentConverter(this, dnEval, appDomain, reflectionAppDomain, createdValues);

					var genTypeArgs = method.DeclaringType.GetGenericArguments();
					var methTypeArgs = method.GetGenericArguments();
					var typeArgs = genTypeArgs.Count == 0 && methTypeArgs.Count == 0 ? Array.Empty<CorType>() : new CorType[genTypeArgs.Count + methTypeArgs.Count];
					int w = 0;
					for (int i = 0; i < genTypeArgs.Count; i++)
						typeArgs[w++] = GetType(appDomain, genTypeArgs[i]);
					for (int i = 0; i < methTypeArgs.Count; i++)
						typeArgs[w++] = GetType(appDomain, methTypeArgs[i]);
					if (typeArgs.Length != w)
						throw new InvalidOperationException();

					var paramTypes = GetAllMethodParameterTypes(method.GetMethodSignature());
					if (paramTypes.Count != arguments.Length)
						throw new InvalidOperationException();

					bool hiddenThisArg = !method.IsStatic && !newObj;
					int argsCount = arguments.Length + (hiddenThisArg ? 1 : 0);
					var args = argsCount == 0 ? Array.Empty<CorValue>() : new CorValue[argsCount];
					w = 0;
					DmdType origType;
					if (hiddenThisArg) {
						var val = converter.Convert(obj, method.DeclaringType, out origType);
						if (val.ErrorMessage != null)
							return new DbgDotNetValueResult(val.ErrorMessage);
						args[w++] = BoxIfNeeded(dnEval, appDomain, createdValues, val.CorValue, method.DeclaringType, origType);
					}
					for (int i = 0; i < arguments.Length; i++) {
						var paramType = paramTypes[i];
						var val = converter.Convert(arguments[i], paramType, out origType);
						if (val.ErrorMessage != null)
							return new DbgDotNetValueResult(val.ErrorMessage);
						var valType = origType ?? new ReflectionTypeCreator(this, method.AppDomain).Create(val.CorValue.ExactType);
						args[w++] = BoxIfNeeded(dnEval, appDomain, createdValues, val.CorValue, paramType, valType);
					}
					if (args.Length != w)
						throw new InvalidOperationException();

					// Derefence/unbox the values here now that they can't get neutered
					w = hiddenThisArg ? 1 : 0;
					for (int i = 0; i < arguments.Length; i++) {
						var paramType = paramTypes[i];
						var arg = args[w];
						if (paramType.IsValueType) {
							if (arg.IsReference) {
								if (arg.IsNull)
									throw new InvalidOperationException();
								arg = arg.DereferencedValue ?? throw new InvalidOperationException();
							}
							if (arg.IsBox)
								arg = arg.BoxedValue ?? throw new InvalidOperationException();
							args[w] = arg;
						}
						w++;
					}
					if (args.Length != w)
						throw new InvalidOperationException();

					var res = newObj ?
						dnEval.CallConstructor(func, typeArgs, args, out int hr) :
						dnEval.Call(func, typeArgs, args, out hr);
					if (res == null)
						return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
					if (res.Value.WasCustomNotification)
						return new DbgDotNetValueResult(CordbgErrorHelper.FuncEvalRequiresAllThreadsToRun);
					if (res.Value.WasCancelled)
						return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
					return new DbgDotNetValueResult(CreateDotNetValue_CorDebug(res.Value.ResultOrException, reflectionAppDomain, tryCreateStrongHandle: true), valueIsException: res.Value.WasException);
				}
			}
			catch (TimeoutException) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			}
			finally {
				foreach (var value in createdValues)
					dnDebugger.DisposeHandle(value);
			}
		}

		CorValue BoxIfNeeded(DnEval dnEval, CorAppDomain appDomain, List<CorValue> createdValues, CorValue corValue, DmdType targetType, DmdType valueType) {
			if (!targetType.IsValueType && valueType.IsValueType && corValue.IsGeneric && !corValue.IsHeap) {
				var etype = corValue.ElementType;
				var corValueType = corValue.ExactType;
				if (!corValueType.HasClass)
					corValueType = GetType(appDomain, valueType);
				var boxedValue = dnEval.Box(corValue, corValueType) ?? throw new InvalidOperationException();
				if (boxedValue != corValue)
					createdValues.Add(corValue);
				corValue = boxedValue;
			}
			return corValue;
		}

		static IList<DmdType> GetAllMethodParameterTypes(DmdMethodSignature sig) {
			if (sig.GetVarArgsParameterTypes().Count == 0)
				return sig.GetParameterTypes();
			var list = new List<DmdType>(sig.GetParameterTypes().Count + sig.GetVarArgsParameterTypes().Count);
			list.AddRange(sig.GetParameterTypes());
			list.AddRange(sig.GetVarArgsParameterTypes());
			return list;
		}

		internal DbgDotNetValueResult FuncEvalCreateInstanceNoCtor_CorDebug(DbgEvaluationContext context, DbgThread thread, CorAppDomain appDomain, DmdType typeToCreate, CancellationToken cancellationToken) {
			debuggerThread.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(context);
			if (tmp != null)
				return tmp.Value;

			var dnThread = GetThread(thread);
			try {
				using (var dnEval = dnDebugger.CreateEval(cancellationToken, suspendOtherThreads: (context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, context);

					var corType = GetType(appDomain, typeToCreate);
					var res = dnEval.CreateDontCallConstructor(corType, out int hr);
					if (res == null)
						return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
					if (res.Value.WasCustomNotification)
						return new DbgDotNetValueResult(CordbgErrorHelper.FuncEvalRequiresAllThreadsToRun);
					if (res.Value.WasCancelled)
						return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
					return new DbgDotNetValueResult(CreateDotNetValue_CorDebug(res.Value.ResultOrException, typeToCreate.AppDomain, tryCreateStrongHandle: true), valueIsException: res.Value.WasException);
				}
			}
			catch (TimeoutException) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			}
		}

		void DnEval_EvalEvent(DnEval dnEval, DbgEvaluationContext context) {
			if (dnEval.EvalTimedOut)
				context.ContinueContext.GetOrCreateData<EvalTimedOut>();
			dnDebugger.SignalEvalComplete();
		}

		internal string SetLocalValue_CorDebug(DbgEvaluationContext context, DbgThread thread, ILDbgEngineStackFrame ilFrame, uint index, DmdType targetType, object value, CancellationToken cancellationToken) =>
			SetLocalOrParameterValue_CorDebug(context, thread, ilFrame, index, targetType, value, isLocal: true, cancellationToken: cancellationToken);

		internal string SetParameterValue_CorDebug(DbgEvaluationContext context, DbgThread thread, ILDbgEngineStackFrame ilFrame, uint index, DmdType targetType, object value, CancellationToken cancellationToken) =>
			SetLocalOrParameterValue_CorDebug(context, thread, ilFrame, index, targetType, value, isLocal: false, cancellationToken: cancellationToken);

		string SetLocalOrParameterValue_CorDebug(DbgEvaluationContext context, DbgThread thread, ILDbgEngineStackFrame ilFrame, uint index, DmdType targetType, object value, bool isLocal, CancellationToken cancellationToken) {
			debuggerThread.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(context);
			if (tmp != null)
				return tmp.Value.ErrorMessage ?? throw new InvalidOperationException();

			var dnThread = GetThread(thread);
			var createdValues = new List<CorValue>();
			CorValue targetValue = null;
			try {
				var appDomain = ilFrame.GetCorAppDomain();
				var reflectionAppDomain = thread.AppDomain.GetReflectionAppDomain() ?? throw new InvalidOperationException();
				using (var dnEval = dnDebugger.CreateEval(cancellationToken, suspendOtherThreads: (context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, context);

					var converter = new EvalArgumentConverter(this, dnEval, appDomain, reflectionAppDomain, createdValues);

					var evalRes = converter.Convert(value, targetType, out var newValueType);
					if (evalRes.ErrorMessage != null)
						return evalRes.ErrorMessage;

					targetValue = isLocal ? ilFrame.CorFrame.GetILLocal(index, out int hr) : ilFrame.CorFrame.GetILArgument(index, out hr);
					if (targetValue == null)
						return CordbgErrorHelper.GetErrorMessage(hr);

					var sourceValue = evalRes.CorValue;
					var sourceType = new ReflectionTypeCreator(this, reflectionAppDomain).Create(sourceValue.ExactType);

					return StoreValue_CorDegbug(dnEval, createdValues, appDomain, dnThread, targetValue, targetType, sourceValue, sourceType);
				}
			}
			catch (TimeoutException) {
				return PredefinedEvaluationErrorMessages.FuncEvalTimedOut;
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return CordbgErrorHelper.InternalError;
			}
			finally {
				dnDebugger.DisposeHandle(targetValue);
				foreach (var v in createdValues)
					dnDebugger.DisposeHandle(v);
			}
		}

		internal DbgDotNetCreateValueResult CreateValue_CorDebug(DbgEvaluationContext context, DbgThread thread, ILDbgEngineStackFrame ilFrame, object value, CancellationToken cancellationToken) {
			debuggerThread.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(context);
			if (tmp != null)
				return new DbgDotNetCreateValueResult(tmp.Value.ErrorMessage ?? throw new InvalidOperationException());

			var dnThread = GetThread(thread);
			var createdValues = new List<CorValue>();
			CorValue createdCorValue = null;
			try {
				var appDomain = ilFrame.GetCorAppDomain();
				var reflectionAppDomain = thread.AppDomain.GetReflectionAppDomain() ?? throw new InvalidOperationException();
				using (var dnEval = dnDebugger.CreateEval(cancellationToken, suspendOtherThreads: (context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, context);

					var converter = new EvalArgumentConverter(this, dnEval, appDomain, reflectionAppDomain, createdValues);
					var evalRes = converter.Convert(value, reflectionAppDomain.System_Object, out var newValueType);
					if (evalRes.ErrorMessage != null)
						return new DbgDotNetCreateValueResult(evalRes.ErrorMessage);

					var resultValue = CreateDotNetValue_CorDebug(evalRes.CorValue, reflectionAppDomain, tryCreateStrongHandle: true);
					createdCorValue = evalRes.CorValue;
					return new DbgDotNetCreateValueResult(resultValue);
				}
			}
			catch (TimeoutException) {
				return new DbgDotNetCreateValueResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetCreateValueResult(CordbgErrorHelper.InternalError);
			}
			finally {
				foreach (var v in createdValues) {
					if (createdCorValue != v)
						dnDebugger.DisposeHandle(v);
				}
			}
		}

		internal string StoreValue_CorDebug(DbgEvaluationContext context, DbgThread thread, ILDbgEngineStackFrame ilFrame, Func<CreateCorValueResult> createTargetValue, DmdType targetType, object sourceValue, CancellationToken cancellationToken) {
			debuggerThread.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(context);
			if (tmp != null)
				return tmp.Value.ErrorMessage ?? throw new InvalidOperationException();

			var dnThread = GetThread(thread);
			var createdValues = new List<CorValue>();
			CreateCorValueResult createResult = default;
			try {
				var appDomain = ilFrame.GetCorAppDomain();
				var reflectionAppDomain = thread.AppDomain.GetReflectionAppDomain() ?? throw new InvalidOperationException();
				using (var dnEval = dnDebugger.CreateEval(cancellationToken, suspendOtherThreads: (context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, context);

					var converter = new EvalArgumentConverter(this, dnEval, appDomain, reflectionAppDomain, createdValues);

					var evalRes = converter.Convert(sourceValue, targetType, out var newValueType);
					if (evalRes.ErrorMessage != null)
						return evalRes.ErrorMessage;

					var sourceCorValue = evalRes.CorValue;
					var sourceType = new ReflectionTypeCreator(this, reflectionAppDomain).Create(sourceCorValue.ExactType);

					createResult = createTargetValue();
					if (createResult.Value == null)
						return CordbgErrorHelper.GetErrorMessage(createResult.HResult);
					return StoreValue_CorDegbug(dnEval, createdValues, appDomain, dnThread, createResult.Value, targetType, sourceCorValue, sourceType);
				}
			}
			catch (TimeoutException) {
				return PredefinedEvaluationErrorMessages.FuncEvalTimedOut;
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return CordbgErrorHelper.InternalError;
			}
			finally {
				if (createResult.CanDispose)
					dnDebugger.DisposeHandle(createResult.Value);
				foreach (var v in createdValues)
					dnDebugger.DisposeHandle(v);
			}
		}

		string StoreValue_CorDegbug(DnEval dnEval, List<CorValue> createdValues, CorAppDomain appDomain, DnThread dnThread, CorValue targetValue, DmdType targetType, CorValue sourceValue, DmdType sourceType) {
			if (targetType.IsByRef)
				return CordbgErrorHelper.InternalError;
			if (!targetType.IsValueType) {
				if (!targetValue.IsReference)
					return CordbgErrorHelper.InternalError;
				if (!sourceValue.IsReference) {
					var boxedSourceValue = BoxIfNeeded(dnEval, appDomain, createdValues, sourceValue, targetType, sourceType);
					if (!boxedSourceValue.IsReference)
						return CordbgErrorHelper.InternalError;
					sourceValue = boxedSourceValue;
				}
				if (!sourceValue.IsNull && sourceType.IsValueType) {
					var sourceDerefVal = sourceValue.DereferencedValue;
					if (sourceDerefVal == null)
						return CordbgErrorHelper.InternalError;
					if (!sourceDerefVal.IsBox)
						return CordbgErrorHelper.InternalError;
				}
				targetValue.ReferenceAddress = sourceValue.ReferenceAddress;
				return null;
			}
			else {
				if (!sourceType.IsValueType)
					return CordbgErrorHelper.InternalError;

				if (targetValue.IsReference) {
					targetValue = targetValue.DereferencedValue;
					if (targetValue == null)
						return CordbgErrorHelper.InternalError;
				}
				if (targetValue.IsBox)
					return CordbgErrorHelper.InternalError;

				if (sourceValue.IsReference) {
					sourceValue = sourceValue.DereferencedValue;
					if (sourceValue == null)
						return CordbgErrorHelper.InternalError;
				}
				if (sourceValue.IsBox) {
					sourceValue = sourceValue.BoxedValue;
					if (sourceValue == null)
						return CordbgErrorHelper.InternalError;
				}

				if (!targetValue.IsGeneric || !sourceValue.IsGeneric)
					return CordbgErrorHelper.InternalError;
				if (targetValue.Size != sourceValue.Size)
					return CordbgErrorHelper.InternalError;
				int hr = targetValue.WriteGenericValue(sourceValue.ReadGenericValue(), dnThread.Process.CorProcess);
				if (hr < 0)
					return CordbgErrorHelper.GetErrorMessage(hr);
				return null;
			}
		}

		// This method calls ICorDebugEval2.NewParameterizedArray() which doesn't support creating SZ arrays
		// with any element type. See the caller of this method (CreateSZArrayCore) for more info.
		internal DbgDotNetValueResult CreateSZArray_CorDebug(DbgEvaluationContext context, DbgThread thread, CorAppDomain appDomain, DmdType elementType, int length, CancellationToken cancellationToken) {
			debuggerThread.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(context);
			if (tmp != null)
				return tmp.Value;

			var dnThread = GetThread(thread);
			try {
				using (var dnEval = dnDebugger.CreateEval(cancellationToken, suspendOtherThreads: (context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, context);

					var corType = GetType(appDomain, elementType);
					var res = dnEval.CreateSZArray(corType, length, out int hr);
					if (res == null)
						return new DbgDotNetValueResult(CordbgErrorHelper.GetErrorMessage(hr));
					Debug.Assert(!res.Value.WasException, "Shouldn't throw " + nameof(ArgumentOutOfRangeException));
					if (res.Value.WasCustomNotification)
						return new DbgDotNetValueResult(CordbgErrorHelper.FuncEvalRequiresAllThreadsToRun);
					if (res.Value.WasCancelled)
						return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
					return new DbgDotNetValueResult(CreateDotNetValue_CorDebug(res.Value.ResultOrException, elementType.AppDomain, tryCreateStrongHandle: true), valueIsException: res.Value.WasException);
				}
			}
			catch (TimeoutException) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(CordbgErrorHelper.InternalError);
			}
		}
	}
}
