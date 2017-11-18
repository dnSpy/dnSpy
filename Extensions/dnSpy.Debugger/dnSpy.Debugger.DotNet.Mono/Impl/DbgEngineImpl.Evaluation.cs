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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.Impl.Evaluation;
using dnSpy.Debugger.DotNet.Mono.Properties;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl {
		internal DbgDotNetValue CreateDotNetValue_MonoDebug(ValueLocation valueLocation) {
			debuggerThread.VerifyAccess();
			var value = valueLocation.Load();
			if (value == null)
				return new SyntheticNullValue(valueLocation.Type);

			var dnValue = new DbgDotNetValueImpl(this, valueLocation, value);
			lock (lockObj)
				dotNetValuesToCloseOnContinue.Add(dnValue);
			return dnValue;
		}

		void CloseDotNetValues_MonoDebug() {
			debuggerThread.VerifyAccess();
			DbgDotNetValueImpl[] valuesToClose;
			lock (lockObj) {
				valuesToClose = dotNetValuesToCloseOnContinue.Count == 0 ? Array.Empty<DbgDotNetValueImpl>() : dotNetValuesToCloseOnContinue.ToArray();
				dotNetValuesToCloseOnContinue.Clear();
			}
			foreach (var value in valuesToClose)
				value.Dispose();
		}

		bool IsEvaluating => funcEvalFactory.IsEvaluating;
		internal int MethodInvokeCounter => funcEvalFactory.MethodInvokeCounter;

		internal TypeMirror GetType(DmdType type) => MonoDebugTypeCreator.GetType(this, type);

		sealed class EvalTimedOut { }

		internal DbgDotNetValueResult? CheckFuncEval(DbgEvaluationContext context) {
			debuggerThread.VerifyAccess();
			if (!IsPaused)
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CanFuncEvalOnlyWhenPaused);
			if (isUnhandledException)
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CantFuncEvalWhenUnhandledExceptionHasOccurred);
			if (context.ContinueContext.HasData<EvalTimedOut>())
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOutNowDisabled);
			if (IsEvaluating)
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.CantFuncEval);
			return null;
		}

		void OnFuncEvalComplete(FuncEval funcEval, DbgEvaluationContext context) {
			if (funcEval.EvalTimedOut)
				context.ContinueContext.GetOrCreateData<EvalTimedOut>();
			OnFuncEvalComplete(funcEval);
		}

		void OnFuncEvalComplete(FuncEval funcEval) {
		}

		FuncEval CreateFuncEval(DbgEvaluationContext context, ThreadMirror monoThread, CancellationToken cancellationToken) =>
			funcEvalFactory.CreateFuncEval(a => OnFuncEvalComplete(a, context), monoThread, context.FuncEvalTimeout, suspendOtherThreads: (context.Options & DbgEvaluationContextOptions.RunAllThreads) == 0, cancellationToken: cancellationToken);

		Value TryInvokeMethod(ThreadMirror thread, ObjectMirror obj, MethodMirror method, IList<Value> arguments, out bool timedOut) {
			debuggerThread.VerifyAccess();
			Debug.Assert(!IsEvaluating);
			var funcEvalTimeout = DbgLanguage.DefaultFuncEvalTimeout;
			const bool suspendOtherThreads = true;
			var cancellationToken = CancellationToken.None;
			try {
				timedOut = false;
				using (var funcEval = funcEvalFactory.CreateFuncEval(a => OnFuncEvalComplete(a), thread, funcEvalTimeout, suspendOtherThreads, cancellationToken))
					return funcEval.CallMethod(method, obj, arguments, FuncEvalOptions.None).Result;
			}
			catch (TimeoutException) {
				timedOut = true;
				return null;
			}
		}

		internal DbgDotNetValueResult FuncEvalCall_MonoDebug(DbgEvaluationContext context, DbgThread thread, DmdMethodBase method, DbgDotNetValue obj, object[] arguments, DbgDotNetInvokeOptions invokeOptions, bool newObj, CancellationToken cancellationToken) {
			debuggerThread.VerifyAccess();
			cancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(context);
			if (tmp != null)
				return tmp.Value;
			Debug.Assert(!newObj || method.IsConstructor);

			Debug.Assert(method.SpecialMethodKind == DmdSpecialMethodKind.Metadata, "Methods not defined in metadata should be emulated by other code (i.e., the caller)");
			if (method.SpecialMethodKind != DmdSpecialMethodKind.Metadata)
				return new DbgDotNetValueResult(ErrorHelper.InternalError);

			if (!vm.Version.AtLeast(2, 24) && method is DmdMethodInfo && method.IsConstructedGenericMethod)
				return new DbgDotNetValueResult(dnSpy_Debugger_DotNet_Mono_Resources.Error_RuntimeDoesNotSupportCreatingGenericMethods);
			var func = MethodCache.GetMethod(method);
			var monoThread = GetThread(thread);
			try {
				using (var funcEval = CreateFuncEval(context, monoThread, cancellationToken)) {
					var converter = new EvalArgumentConverter(this, monoThread.Domain, method.AppDomain);

					var paramTypes = GetAllMethodParameterTypes(method.GetMethodSignature());
					if (paramTypes.Count != arguments.Length)
						throw new InvalidOperationException();

					var funcEvalOptions = FuncEvalOptions.None;
					if ((invokeOptions & DbgDotNetInvokeOptions.NonVirtual) == 0)
						funcEvalOptions |= FuncEvalOptions.Virtual;
					int argsCount = arguments.Length;
					var args = argsCount == 0 ? Array.Empty<Value>() : new Value[argsCount];
					DmdType origType;
					Value hiddenThisValue;
					if (!method.IsStatic && !newObj) {
						var declType = method.DeclaringType;
						if (method is DmdMethodInfo m)
							declType = m.GetBaseDefinition().DeclaringType;
						var val = converter.Convert(obj, declType, out origType);
						if (val.ErrorMessage != null)
							return new DbgDotNetValueResult(val.ErrorMessage);
						hiddenThisValue = BoxIfNeeded(monoThread.Domain, val.Value, declType, origType);
						if (val.Value == hiddenThisValue && val.Value is StructMirror)
							funcEvalOptions |= FuncEvalOptions.ReturnOutThis;
					}
					else
						hiddenThisValue = null;
					for (int i = 0; i < arguments.Length; i++) {
						var paramType = paramTypes[i];
						var val = converter.Convert(arguments[i], paramType, out origType);
						if (val.ErrorMessage != null)
							return new DbgDotNetValueResult(val.ErrorMessage);
						var valType = origType ?? MonoValueTypeCreator.CreateType(this, val.Value, paramType);
						args[i] = BoxIfNeeded(monoThread.Domain, val.Value, paramType, valType);
					}

					var res = newObj ?
						funcEval.CreateInstance(func, args, funcEvalOptions) :
						funcEval.CallMethod(func, hiddenThisValue, args, funcEvalOptions);
					if (res == null)
						return new DbgDotNetValueResult(ErrorHelper.InternalError);
					var returnType = (method as DmdMethodInfo)?.ReturnType ?? method.ReflectedType;
					var returnValue = res.Exception ?? res.Result ?? new PrimitiveValue(vm, ElementType.Object, null);
					var valueLocation = new NoValueLocation(returnType, returnValue);
					return new DbgDotNetValueResult(CreateDotNetValue_MonoDebug(valueLocation), valueIsException: res.Exception != null);
				}
			}
			catch (TimeoutException) {
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgDotNetValueResult(ErrorHelper.InternalError);
			}
		}

		Value BoxIfNeeded(AppDomainMirror appDomain, Value value, DmdType targetType, DmdType valueType) {
			if (!targetType.IsValueType && valueType.IsValueType && (value is PrimitiveValue || value is StructMirror))
				return appDomain.CreateBoxedValue(value);
			return value;
		}

		static IList<DmdType> GetAllMethodParameterTypes(DmdMethodSignature sig) {
			if (sig.GetVarArgsParameterTypes().Count == 0)
				return sig.GetParameterTypes();
			var list = new List<DmdType>(sig.GetParameterTypes().Count + sig.GetVarArgsParameterTypes().Count);
			list.AddRange(sig.GetParameterTypes());
			list.AddRange(sig.GetVarArgsParameterTypes());
			return list;
		}
	}
}
