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
using System.Diagnostics;
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
		internal DbgDotNetValue CreateDotNetValue_CorDebug(CorValue value, DmdAppDomain reflectionAppDomain, bool tryCreateStrongHandle, bool closeOnContinue = true) {
			debuggerThread.VerifyAccess();
			if (value is null)
				return new SyntheticValue(reflectionAppDomain.System_Void, new DbgDotNetRawValue(DbgSimpleValueType.Void));

			try {
				var type = new ReflectionTypeCreator(this, reflectionAppDomain).Create(value.ExactType);

				if (tryCreateStrongHandle && !value.IsNull && !value.IsHandle && value.IsReference && !type.IsPointer && !type.IsFunctionPointer && !type.IsByRef) {
					var derefValue = value.GetDereferencedValue(out int hr);
					var strongHandle = derefValue?.CreateHandle(CorDebugHandleType.HANDLE_STRONG);
					Debug2.Assert(derefValue is null || strongHandle is not null || type == type.AppDomain.System_TypedReference);
					if (strongHandle is not null)
						value = strongHandle;
				}

				var dnValue = new DbgDotNetValueImpl(this, new DbgCorValueHolder(this, value, type));
				if (closeOnContinue) {
					lock (lockObj)
						dotNetValuesToCloseOnContinue.Add(dnValue);
				}
				return dnValue;
			}
			catch {
				dnDebugger.DisposeHandle(value);
				throw;
			}
		}

		internal DbgDotNetValue CreateDotNetValue_CorDebug(DbgCorValueHolder value) {
			debuggerThread.VerifyAccess();
			var dnValue = new DbgDotNetValueImpl(this, value);
			lock (lockObj)
				dotNetValuesToCloseOnContinue.Add(dnValue);
			return dnValue;
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

		internal void DisposeHandle_CorDebug(CorValue? value) {
			Debug.Assert(debuggerThread.CheckAccess());
			dnDebugger.DisposeHandle(value);
		}

		DbgDotNetRawValue? ReadField_CorDebug(CorValue? obj, DbgAppDomain? appDomain, string fieldName1, string fieldName2) {
			if (obj is null || appDomain is null)
				return null;
			var reflectionAppDomain = appDomain.GetReflectionAppDomain();
			if (reflectionAppDomain is null)
				return null;
			DbgDotNetValueImpl? objImp = null;
			try {
				objImp = CreateDotNetValue_CorDebug(obj, reflectionAppDomain, tryCreateStrongHandle: false) as DbgDotNetValueImpl;
				if (objImp is null)
					return null;
				return ReadField_CorDebug(objImp, fieldName1, fieldName2);
			}
			finally {
				objImp?.Dispose();
			}
		}

		DbgDotNetRawValue? ReadField_CorDebug(DbgDotNetValueImpl obj, string fieldName1, string? fieldName2) {
			const DmdBindingFlags fieldFlags = DmdBindingFlags.Public | DmdBindingFlags.NonPublic | DmdBindingFlags.Instance;
			var field = obj.Type.GetField(fieldName1, fieldFlags);
			if (field is null && fieldName2 is not null)
				field = obj.Type.GetField(fieldName2, fieldFlags);
			Debug2.Assert(field is not null);
			if (field is null)
				return null;

			var dnAppDomain = ((DbgCorDebugInternalAppDomainImpl)obj.Type.AppDomain.GetDebuggerAppDomain().InternalAppDomain).DnAppDomain;
			var corFieldDeclType = GetType(dnAppDomain.CorAppDomain, field.DeclaringType!);
			var objValue = DbgCorDebugInternalRuntimeImpl.TryGetObjectOrPrimitiveValue(obj.TryGetCorValue(), out int hr);
			if (objValue is null)
				return null;
			if (objValue.IsObject) {
				// This isn't a generic read-field method, so we won't try to load any classes by calling cctors.

				var fieldValue = objValue.GetFieldValue(corFieldDeclType.Class, (uint)field.MetadataToken, out hr);
				if (fieldValue is null)
					return null;
				DbgDotNetValue? dnValue = null;
				try {
					dnValue = CreateDotNetValue_CorDebug(fieldValue, field.AppDomain, tryCreateStrongHandle: false);
					return dnValue.GetRawValue();
				}
				finally {
					dnValue?.Dispose();
				}
			}
			return null;
		}

		CorType GetType(CorAppDomain appDomain, DmdType type) => CorDebugTypeCreator.GetType(this, appDomain, type);

		sealed class EvalTimedOut { }

		internal DbgDotNetValueResult? CheckFuncEval(DbgEvaluationInfo evalInfo) {
			debuggerThread.VerifyAccess();
			if (dnDebugger.ProcessState != DebuggerProcessState.Paused)
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CanFuncEvalOnlyWhenPaused);
			if (isUnhandledException)
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CantFuncEvalWhenUnhandledExceptionHasOccurred);
			if (evalInfo.Context.ContinueContext.HasData<EvalTimedOut>())
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOutNowDisabled);
			if (dnDebugger.IsEvaluating)
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CantFuncEval);
			return null;
		}

		internal DbgDotNetValueResult FuncEvalCall_CorDebug(DbgEvaluationInfo evalInfo, CorAppDomain appDomain, DmdMethodBase method, DbgDotNetValue? obj, object?[] arguments, bool newObj) {
			debuggerThread.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(evalInfo);
			if (tmp is not null)
				return tmp.Value;
			Debug.Assert(!newObj || method.IsConstructor);

			Debug.Assert(method.SpecialMethodKind == DmdSpecialMethodKind.Metadata, "Methods not defined in metadata should be emulated by other code (i.e., the caller)");
			if (method.SpecialMethodKind != DmdSpecialMethodKind.Metadata)
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);

			var reflectionAppDomain = method.AppDomain;
			var methodDbgModule = method.Module.GetDebuggerModule() ?? throw new InvalidOperationException();
			if (!TryGetDnModule(methodDbgModule, out var methodModule))
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			var func = methodModule.CorModule.GetFunctionFromToken((uint)method.MetadataToken) ?? throw new InvalidOperationException();

			int hr;
			var dnThread = GetThread(evalInfo.Frame.Thread);
			var createdValues = new List<CorValue>();
			try {
				using (var dnEval = dnDebugger.CreateEval(evalInfo.CancellationToken, suspendOtherThreads: (evalInfo.Context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(evalInfo.Context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, evalInfo);

					var converter = new EvalArgumentConverter(this, dnEval, appDomain, reflectionAppDomain, createdValues);

					var genTypeArgs = method.DeclaringType!.GetGenericArguments();
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
					var declType = method.DeclaringType;
					if (hiddenThisArg) {
						if (method is DmdMethodInfo m)
							declType = m.GetBaseDefinition().DeclaringType!;
						var val = converter.Convert(obj, declType, out origType);
						if (val.ErrorMessage is not null)
							return DbgDotNetValueResult.CreateError(val.ErrorMessage);
						args[w++] = BoxIfNeeded(dnEval, appDomain, createdValues, val.CorValue!, declType, origType);
					}
					for (int i = 0; i < arguments.Length; i++) {
						var paramType = paramTypes[i];
						var val = converter.Convert(arguments[i], paramType, out origType);
						if (val.ErrorMessage is not null)
							return DbgDotNetValueResult.CreateError(val.ErrorMessage);
						var valType = origType ?? new ReflectionTypeCreator(this, method.AppDomain).Create(val.CorValue!.ExactType);
						args[w++] = BoxIfNeeded(dnEval, appDomain, createdValues, val.CorValue!, paramType, valType);
					}
					if (args.Length != w)
						throw new InvalidOperationException();

					// Derefence/unbox the values here now that they can't get neutered
					for (int i = 0; i < args.Length; i++) {
						DmdType argType;
						if (!hiddenThisArg)
							argType = paramTypes[i];
						else if (i == 0)
							argType = declType;
						else
							argType = paramTypes[i - 1];
						CorValue? arg = args[i];
						if (argType.IsValueType || argType.IsPointer || argType.IsFunctionPointer) {
							if (arg.IsReference) {
								if (arg.IsNull)
									throw new InvalidOperationException();
								arg = arg.GetDereferencedValue(out hr);
								if (arg is null)
									return DbgDotNetValueResult.CreateError(CordbgErrorHelper.GetErrorMessage(hr));
							}
							if (arg.IsBox) {
								arg = arg.GetBoxedValue(out hr);
								if (arg is null)
									return DbgDotNetValueResult.CreateError(CordbgErrorHelper.GetErrorMessage(hr));
							}
							args[i] = arg;
						}
					}

					var res = newObj ?
						dnEval.CallConstructor(func, typeArgs, args, out hr) :
						dnEval.Call(func, typeArgs, args, out hr);
					if (res is null)
						return DbgDotNetValueResult.CreateError(CordbgErrorHelper.GetErrorMessage(hr));
					if (res.Value.WasCustomNotification)
						return DbgDotNetValueResult.CreateError(CordbgErrorHelper.FuncEvalRequiresAllThreadsToRun);
					if (res.Value.WasCancelled)
						return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
					if (res.Value.WasException)
						return DbgDotNetValueResult.CreateException(CreateDotNetValue_CorDebug(res.Value.ResultOrException!, reflectionAppDomain, tryCreateStrongHandle: true));
					return DbgDotNetValueResult.Create(CreateDotNetValue_CorDebug(res.Value.ResultOrException!, reflectionAppDomain, tryCreateStrongHandle: true));
				}
			}
			catch (TimeoutException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
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
				if (corValueType?.HasClass == false)
					corValueType = GetType(appDomain, valueType);
				var boxedValue = dnEval.Box(corValue, corValueType) ?? throw new InvalidOperationException();
				if (!boxedValue.Equals(corValue))
					createdValues.Add(boxedValue);
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

		internal DbgDotNetValueResult Box_CorDebug(DbgEvaluationInfo evalInfo, CorAppDomain appDomain, CorValue value, DmdType type) {
			debuggerThread.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(evalInfo);
			if (tmp is not null)
				return tmp.Value;

			var dnThread = GetThread(evalInfo.Frame.Thread);
			var createdValues = new List<CorValue>();
			CorValue? boxedValue = null;
			try {
				using (var dnEval = dnDebugger.CreateEval(evalInfo.CancellationToken, suspendOtherThreads: (evalInfo.Context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(evalInfo.Context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, evalInfo);

					boxedValue = BoxIfNeeded(dnEval, appDomain, createdValues, value, type.AppDomain.System_Object, type);
					if (boxedValue is null)
						return DbgDotNetValueResult.CreateError(CordbgErrorHelper.GetErrorMessage(-1));
					return DbgDotNetValueResult.Create(CreateDotNetValue_CorDebug(boxedValue, type.AppDomain, tryCreateStrongHandle: true));
				}
			}
			catch (TimeoutException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
			finally {
				foreach (var v in createdValues) {
					if (!v.Equals(boxedValue))
						dnDebugger.DisposeHandle(v);
				}
			}
		}

		internal DbgDotNetValueResult FuncEvalCreateInstanceNoCtor_CorDebug(DbgEvaluationInfo evalInfo, CorAppDomain appDomain, DmdType typeToCreate) {
			debuggerThread.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(evalInfo);
			if (tmp is not null)
				return tmp.Value;

			var dnThread = GetThread(evalInfo.Frame.Thread);
			try {
				using (var dnEval = dnDebugger.CreateEval(evalInfo.CancellationToken, suspendOtherThreads: (evalInfo.Context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(evalInfo.Context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, evalInfo);

					var corType = GetType(appDomain, typeToCreate);
					var res = dnEval.CreateDontCallConstructor(corType, out int hr);
					if (res is null)
						return DbgDotNetValueResult.CreateError(CordbgErrorHelper.GetErrorMessage(hr));
					if (res.Value.WasCustomNotification)
						return DbgDotNetValueResult.CreateError(CordbgErrorHelper.FuncEvalRequiresAllThreadsToRun);
					if (res.Value.WasCancelled)
						return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
					if (res.Value.WasException)
						return DbgDotNetValueResult.CreateException(CreateDotNetValue_CorDebug(res.Value.ResultOrException!, typeToCreate.AppDomain, tryCreateStrongHandle: true));
					return DbgDotNetValueResult.Create(CreateDotNetValue_CorDebug(res.Value.ResultOrException!, typeToCreate.AppDomain, tryCreateStrongHandle: true));
				}
			}
			catch (TimeoutException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
		}

		void DnEval_EvalEvent(DnEval dnEval, DbgEvaluationInfo evalInfo) {
			if (dnEval.EvalTimedOut)
				evalInfo.Context.ContinueContext.GetOrCreateData<EvalTimedOut>();
			dnDebugger.SignalEvalComplete();
		}

		internal DbgDotNetValueResult CreateValue_CorDebug(DbgEvaluationInfo evalInfo, ILDbgEngineStackFrame ilFrame, object? value) {
			debuggerThread.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			if (value is DbgDotNetValueImpl)
				return DbgDotNetValueResult.Create((DbgDotNetValueImpl)value);
			var tmp = CheckFuncEval(evalInfo);
			if (tmp is not null)
				return tmp.Value;

			var dnThread = GetThread(evalInfo.Frame.Thread);
			var createdValues = new List<CorValue>();
			CorValue? createdCorValue = null;
			try {
				var appDomain = ilFrame.GetCorAppDomain();
				var reflectionAppDomain = ilFrame.GetReflectionModule().AppDomain;
				using (var dnEval = dnDebugger.CreateEval(evalInfo.CancellationToken, suspendOtherThreads: (evalInfo.Context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(evalInfo.Context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, evalInfo);

					var converter = new EvalArgumentConverter(this, dnEval, appDomain, reflectionAppDomain, createdValues);
					var evalRes = converter.Convert(value, reflectionAppDomain.System_Object, out var newValueType);
					if (evalRes.ErrorMessage is not null)
						return DbgDotNetValueResult.CreateError(evalRes.ErrorMessage);

					var resultValue = CreateDotNetValue_CorDebug(evalRes.CorValue!, reflectionAppDomain, tryCreateStrongHandle: true);
					createdCorValue = evalRes.CorValue;
					return DbgDotNetValueResult.Create(resultValue);
				}
			}
			catch (TimeoutException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
			finally {
				foreach (var v in createdValues) {
					if (!v.Equals(createdCorValue))
						dnDebugger.DisposeHandle(v);
				}
			}
		}

		internal string? SetLocalValue_CorDebug(DbgEvaluationInfo evalInfo, ILDbgEngineStackFrame ilFrame, uint index, DmdType targetType, object? value) {
			Func<CreateCorValueResult> createTargetValue = () => {
				var corValue = ilFrame.CorFrame.GetILLocal(index, out int hr);
				return new CreateCorValueResult(corValue, hr);
			};
			return StoreValue_CorDebug(evalInfo, ilFrame, createTargetValue, targetType, value);
		}

		internal string? SetParameterValue_CorDebug(DbgEvaluationInfo evalInfo, ILDbgEngineStackFrame ilFrame, uint index, DmdType targetType, object? value) {
			Func<CreateCorValueResult> createTargetValue = () => {
				var corValue = ilFrame.CorFrame.GetILArgument(index, out int hr);
				return new CreateCorValueResult(corValue, hr);
			};
			return StoreValue_CorDebug(evalInfo, ilFrame, createTargetValue, targetType, value);
		}

		internal string? StoreValue_CorDebug(DbgEvaluationInfo evalInfo, ILDbgEngineStackFrame ilFrame, Func<CreateCorValueResult> createTargetValue, DmdType targetType, object? sourceValue) {
			debuggerThread.VerifyAccess();

			if (RequiresNoFuncEvalToStoreValue(targetType, sourceValue))
				return StoreSimpleValue_CorDebug(evalInfo, ilFrame, createTargetValue, targetType, sourceValue);

			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(evalInfo);
			if (tmp is not null)
				return tmp.Value.ErrorMessage ?? throw new InvalidOperationException();

			var dnThread = GetThread(evalInfo.Frame.Thread);
			var createdValues = new List<CorValue>();
			CreateCorValueResult createResult = default;
			try {
				var appDomain = ilFrame.GetCorAppDomain();
				var reflectionAppDomain = targetType.AppDomain;
				using (var dnEval = dnDebugger.CreateEval(evalInfo.CancellationToken, suspendOtherThreads: (evalInfo.Context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(evalInfo.Context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, evalInfo);

					var converter = new EvalArgumentConverter(this, dnEval, appDomain, reflectionAppDomain, createdValues);

					var evalRes = converter.Convert(sourceValue, targetType, out var newValueType);
					if (evalRes.ErrorMessage is not null)
						return evalRes.ErrorMessage;

					var sourceCorValue = evalRes.CorValue!;
					var sourceType = new ReflectionTypeCreator(this, reflectionAppDomain).Create(sourceCorValue.ExactType);

					createResult = createTargetValue();
					if (createResult.Value is null)
						return CordbgErrorHelper.GetErrorMessage(createResult.HResult);
					return StoreValue_CorDebug(dnEval, createdValues, appDomain, dnThread, createResult.Value, targetType, sourceCorValue, sourceType);
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

		string? StoreValue_CorDebug(DnEval dnEval, List<CorValue> createdValues, CorAppDomain appDomain, DnThread dnThread, CorValue targetValue, DmdType targetType, CorValue sourceValue, DmdType sourceType) {
			if (targetType.IsByRef)
				return CordbgErrorHelper.InternalError;
			int hr;
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
					var sourceDerefVal = sourceValue.GetDereferencedValue(out hr);
					if (sourceDerefVal is null)
						return CordbgErrorHelper.GetErrorMessage(hr);
					if (!sourceDerefVal.IsBox)
						return CordbgErrorHelper.InternalError;
				}
				hr = targetValue.SetReferenceAddress(sourceValue.ReferenceAddress);
				if (hr != 0)
					return CordbgErrorHelper.GetErrorMessage(hr);
				return null;
			}
			else {
				if (!sourceType.IsValueType)
					return CordbgErrorHelper.InternalError;

				if (targetValue.IsReference) {
					if (!(targetValue.GetDereferencedValue(out hr) is CorValue derefValue))
						return CordbgErrorHelper.GetErrorMessage(hr);
					targetValue = derefValue;
				}
				if (targetValue.IsBox)
					return CordbgErrorHelper.InternalError;

				if (sourceValue.IsReference) {
					if (!(sourceValue.GetDereferencedValue(out hr) is CorValue derefValue))
						return CordbgErrorHelper.GetErrorMessage(hr);
					sourceValue = derefValue;
				}
				if (sourceValue.IsBox) {
					if (!(sourceValue.GetBoxedValue(out hr) is CorValue unboxedValue))
						return CordbgErrorHelper.GetErrorMessage(hr);
					sourceValue = unboxedValue;
				}

				if (!targetValue.IsGeneric || !sourceValue.IsGeneric)
					return CordbgErrorHelper.InternalError;
				if (targetValue.Size != sourceValue.Size)
					return CordbgErrorHelper.InternalError;
				hr = targetValue.WriteGenericValue(sourceValue.ReadGenericValue(), dnThread.Process.CorProcess);
				if (hr < 0)
					return CordbgErrorHelper.GetErrorMessage(hr);
				return null;
			}
		}

		string? StoreSimpleValue_CorDebug(DbgEvaluationInfo evalInfo, ILDbgEngineStackFrame ilFrame, Func<CreateCorValueResult> createTargetValue, DmdType targetType, object? sourceValue) {
			Debug.Assert(RequiresNoFuncEvalToStoreValue(targetType, sourceValue));
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			CreateCorValueResult createResult = default;
			try {
				var dnThread = GetThread(evalInfo.Frame.Thread);
				createResult = createTargetValue();
				if (createResult.Value is null)
					return CordbgErrorHelper.GetErrorMessage(createResult.HResult);
				return StoreSimpleValue_CorDebug(dnThread, createResult.Value, targetType, sourceValue);
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
			}
		}

		static bool IsNoFuncEvalValue(object? value) {
			if (value is null)
				return true;

			var type = value.GetType();
			var tc = Type.GetTypeCode(type);
			if (TypeCode.Boolean <= tc && tc <= TypeCode.Double)
				return true;
			if (type == typeof(IntPtr) || type == typeof(UIntPtr))
				return true;

			return false;
		}

		static bool RequiresNoFuncEvalToStoreValue(DmdType targetType, object? sourceValue) {
			// Boxing requires func-eval
			if (!(targetType.IsValueType || targetType.IsPointer || targetType.IsFunctionPointer) && sourceValue is not null)
				return false;

			// Only primitive value types are supported
			if (!IsNoFuncEvalValue(sourceValue))
				return false;

			return true;
		}

		string? StoreSimpleValue_CorDebug(DnThread dnThread, CorValue targetValue, DmdType targetType, object? sourceValue) {
			if (targetType.IsByRef)
				return CordbgErrorHelper.InternalError;
			int hr;
			if (targetType.IsPointer || targetType.IsFunctionPointer) {
				var sourceValueBytes = TryGetValueBytes(sourceValue);
				Debug2.Assert(sourceValueBytes is not null);
				if (sourceValueBytes is null || targetValue.Size != (uint)sourceValueBytes.Length)
					return CordbgErrorHelper.InternalError;
				ulong address = targetValue.Address;
				if (address == 0)
					return CordbgErrorHelper.InternalError;
				hr = dnThread.Process.CorProcess.WriteMemory(address, sourceValueBytes, 0, sourceValueBytes.Length, out var sizeWritten);
				if (hr < 0)
					return CordbgErrorHelper.GetErrorMessage(hr);
				return null;
			}
			else if (!targetType.IsValueType) {
				if (sourceValue is not null)
					return CordbgErrorHelper.InternalError;
				hr = targetValue.SetReferenceAddress(0);
				if (hr != 0)
					return CordbgErrorHelper.GetErrorMessage(hr);
				return null;
			}
			else {
				if (targetValue.IsReference) {
					if (!(targetValue.GetDereferencedValue(out hr) is CorValue derefValue))
						return CordbgErrorHelper.GetErrorMessage(hr);
					targetValue = derefValue;
				}
				if (targetValue.IsBox)
					return CordbgErrorHelper.InternalError;

				if (!targetValue.IsGeneric || sourceValue is null)
					return CordbgErrorHelper.InternalError;
				var sourceValueBytes = TryGetValueBytes(sourceValue);
				Debug2.Assert(sourceValueBytes is not null);
				if (sourceValueBytes is null || targetValue.Size != (uint)sourceValueBytes.Length)
					return CordbgErrorHelper.InternalError;
				hr = targetValue.WriteGenericValue(sourceValueBytes, dnThread.Process.CorProcess);
				if (hr < 0)
					return CordbgErrorHelper.GetErrorMessage(hr);
				return null;
			}
		}

		static byte[]? TryGetValueBytes(object? value) {
			if (value is null)
				return null;
			var type = value.GetType();
			switch (Type.GetTypeCode(type)) {
			case TypeCode.Boolean:		return new byte[1] { (byte)((bool)value ? 1 : 0) };
			case TypeCode.Char:			return BitConverter.GetBytes((char)value);
			case TypeCode.SByte:		return new byte[1] { (byte)(sbyte)value };
			case TypeCode.Byte:			return new byte[1] { (byte)value };
			case TypeCode.Int16:		return BitConverter.GetBytes((short)value);
			case TypeCode.UInt16:		return BitConverter.GetBytes((ushort)value);
			case TypeCode.Int32:		return BitConverter.GetBytes((int)value);
			case TypeCode.UInt32:		return BitConverter.GetBytes((uint)value);
			case TypeCode.Int64:		return BitConverter.GetBytes((long)value);
			case TypeCode.UInt64:		return BitConverter.GetBytes((ulong)value);
			case TypeCode.Single:		return BitConverter.GetBytes((float)value);
			case TypeCode.Double:		return BitConverter.GetBytes((double)value);
			}
			if (type == typeof(IntPtr)) {
				if (IntPtr.Size == 4)
					return BitConverter.GetBytes(((IntPtr)value).ToInt32());
				return BitConverter.GetBytes(((IntPtr)value).ToInt64());
			}
			if (type == typeof(UIntPtr)) {
				if (UIntPtr.Size == 4)
					return BitConverter.GetBytes(((UIntPtr)value).ToUInt32());
				return BitConverter.GetBytes(((UIntPtr)value).ToUInt64());
			}
			return null;
		}

		// This method calls ICorDebugEval2.NewParameterizedArray() which doesn't support creating SZ arrays
		// with any element type. See the caller of this method (CreateSZArrayCore) for more info.
		internal DbgDotNetValueResult CreateSZArray_CorDebug(DbgEvaluationInfo evalInfo, CorAppDomain appDomain, DmdType elementType, int length) {
			debuggerThread.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(evalInfo);
			if (tmp is not null)
				return tmp.Value;

			var dnThread = GetThread(evalInfo.Frame.Thread);
			try {
				using (var dnEval = dnDebugger.CreateEval(evalInfo.CancellationToken, suspendOtherThreads: (evalInfo.Context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0)) {
					dnEval.SetThread(dnThread);
					dnEval.SetTimeout(evalInfo.Context.FuncEvalTimeout);
					dnEval.EvalEvent += (s, e) => DnEval_EvalEvent(dnEval, evalInfo);

					var corType = GetType(appDomain, elementType);
					var res = dnEval.CreateSZArray(corType, length, out int hr);
					if (res is null)
						return DbgDotNetValueResult.CreateError(CordbgErrorHelper.GetErrorMessage(hr));
					Debug.Assert(!res.Value.WasException, "Shouldn't throw " + nameof(ArgumentOutOfRangeException));
					if (res.Value.WasCustomNotification)
						return DbgDotNetValueResult.CreateError(CordbgErrorHelper.FuncEvalRequiresAllThreadsToRun);
					if (res.Value.WasCancelled)
						return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
					if (res.Value.WasException)
						return DbgDotNetValueResult.CreateException(CreateDotNetValue_CorDebug(res.Value.ResultOrException!, elementType.AppDomain, tryCreateStrongHandle: true));
					return DbgDotNetValueResult.Create(CreateDotNetValue_CorDebug(res.Value.ResultOrException!, elementType.AppDomain, tryCreateStrongHandle: true));
				}
			}
			catch (TimeoutException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(CordbgErrorHelper.InternalError);
			}
		}

		internal DbgDotNetReturnValueInfo[] GetCurrentReturnValues() {
			debuggerThread.VerifyAccess();
			var currentReturnValues = this.currentReturnValues;
			if (currentReturnValues.Length == 0)
				return Array.Empty<DbgDotNetReturnValueInfo>();
			var res = new DbgDotNetReturnValueInfo[currentReturnValues.Length];
			try {
				for (int i = 0; i < res.Length; i++) {
					var info = currentReturnValues[i];
					var dnValue = ((DbgDotNetValueImpl)info.Value).CorValueHolder.AddRef();
					try {
						res[i] = new DbgDotNetReturnValueInfo(info.Id, info.Method, CreateDotNetValue_CorDebug(dnValue));
					}
					catch {
						dnValue.Release();
						throw;
					}
				}
				return res;
			}
			catch {
				foreach (var info in res)
					info.Value?.Dispose();
				throw;
			}
		}

		internal DbgDotNetValue? GetCurrentReturnValue(uint id) {
			debuggerThread.VerifyAccess();
			var currentReturnValues = this.currentReturnValues;
			if (id == DbgDotNetRuntimeConstants.LastReturnValueId)
				id = (uint)currentReturnValues.Length;
			int index = (int)id - 1;
			if ((uint)index >= (uint)currentReturnValues.Length)
				return null;
			var info = currentReturnValues[index];
			var dnValue = ((DbgDotNetValueImpl)info.Value).CorValueHolder.AddRef();
			try {
				return CreateDotNetValue_CorDebug(dnValue);
			}
			catch {
				dnValue.Release();
				throw;
			}
		}

		internal void SetReturnValues(DbgDotNetReturnValueInfo[] returnValues) {
			debuggerThread.VerifyAccess();
			for (int i = 0; i < returnValues.Length; i++) {
				if (returnValues[i].Id != (uint)i + 1)
					throw new ArgumentException();
			}
			foreach (var info in currentReturnValues)
				info.Value.Dispose();
			currentReturnValues = returnValues;
		}
		DbgDotNetReturnValueInfo[] currentReturnValues;
	}
}
