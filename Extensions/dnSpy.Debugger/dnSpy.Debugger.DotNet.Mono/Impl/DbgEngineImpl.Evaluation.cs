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
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Mono.Impl.Evaluation;
using dnSpy.Debugger.DotNet.Mono.Properties;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	sealed partial class DbgEngineImpl {
		internal int? OffsetToStringData {
			get {
				debuggerThread.VerifyAccess();
				InitializeObjectConstantsCore_MonoDebug();
				return objectConstants?.OffsetToStringData;
			}
		}

		internal int? OffsetToArrayData {
			get {
				debuggerThread.VerifyAccess();
				InitializeObjectConstantsCore_MonoDebug();
				return objectConstants?.OffsetToArrayData;
			}
		}

		ObjectConstants? objectConstants;
		bool canInitializeObjectConstants;

		void InitializeObjectConstants_MonoDebug() {
			// Don't do a thing, it sometimes hangs somewhere in mono. It's less likely to
			// hang if the process has run for a few seconds.
			// The properties above will call the next method.
		}
		void InitializeObjectConstantsCore_MonoDebug() {
			debuggerThread.VerifyAccess();
			if (!canInitializeObjectConstants)
				return;
			if (!(objectConstants is null))
				return;
			if (objectFactory is null)
				return;

			foreach (var thread in vm!.GetThreads()) {
				if (thread.Name == FinalizerName)
					continue;
				var factory = new ObjectConstantsFactory(objectFactory.Process, thread);
				//TODO: Fails on mono when TypeLoad events are used, apparently we can't func-eval until much later and we don't get an error so it times out
				if (factory.TryCreate(out objectConstants))
					break;
			}
		}

		internal DbgDotNetValue CreateDotNetValue_MonoDebug(DmdAppDomain reflectionAppDomain, Value value, DmdType? realTypeOpt) {
			debuggerThread.VerifyAccess();
			if (value is null)
				return new SyntheticNullValue(realTypeOpt ?? reflectionAppDomain.System_Object);
			DmdType type;
			if (value is PrimitiveValue pv)
				type = MonoValueTypeCreator.CreateType(this, value, reflectionAppDomain.System_Object);
			else if (value is StructMirror sm)
				type = GetReflectionType(reflectionAppDomain, sm.Type, realTypeOpt);
			else {
				Debug.Assert(value is ObjectMirror);
				type = GetReflectionType(reflectionAppDomain, ((ObjectMirror)value).Type, realTypeOpt);
			}
			var valueLocation = new NoValueLocation(type, value);
			return CreateDotNetValue_MonoDebug(valueLocation);
		}

		internal DbgDotNetValue CreateDotNetValue_MonoDebug(ValueLocation valueLocation) {
			debuggerThread.VerifyAccess();
			var value = valueLocation.Load();
			if (value is null)
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

		internal void AddExecOnPause(Action action) {
			debuggerThread.VerifyAccess();
			if (IsPaused)
				action();
			else
				execOnPauseList.Add(action);
		}

		void RunExecOnPauseDelegates_MonoDebug() {
			debuggerThread.VerifyAccess();
			if (execOnPauseList.Count == 0)
				return;
			var list = execOnPauseList.ToArray();
			execOnPauseList.Clear();
			foreach (var action in list)
				action();
		}

		bool IsEvaluating => funcEvalFactory.IsEvaluating;
		internal int MethodInvokeCounter => funcEvalFactory.MethodInvokeCounter;

		sealed class EvalTimedOut { }

		internal DbgDotNetValueResult? CheckFuncEval(DbgEvaluationContext context) {
			debuggerThread.VerifyAccess();
			if (!IsPaused)
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CanFuncEvalOnlyWhenPaused);
			if (isUnhandledException)
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CantFuncEvalWhenUnhandledExceptionHasOccurred);
			if (context.ContinueContext.HasData<EvalTimedOut>())
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOutNowDisabled);
			if (IsEvaluating)
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CantFuncEval);
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

		FuncEval CreateFuncEval2(DbgEvaluationContext? contextOpt, ThreadMirror monoThread, CancellationToken cancellationToken) {
			if (!(contextOpt is null))
				return CreateFuncEval(contextOpt, monoThread, cancellationToken);
			return funcEvalFactory.CreateFuncEval(a => OnFuncEvalComplete(a), monoThread, DbgLanguage.DefaultFuncEvalTimeout, suspendOtherThreads: true, cancellationToken: cancellationToken);
		}

		Value? TryInvokeMethod(ThreadMirror thread, ObjectMirror obj, MethodMirror method, IList<Value> arguments, out bool timedOut) {
			debuggerThread.VerifyAccess();
			if (IsEvaluating) {
				timedOut = true;
				return null;
			}
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

		List<DbgThread> GetFuncEvalCallThreads(DmdAppDomain reflectionAppDomain) {
			debuggerThread.VerifyAccess();
			var allThreads = DbgRuntime.Threads;
			var threads = new List<DbgThread>(allThreads.Length);
			var appDomain = reflectionAppDomain.GetDebuggerAppDomain();
			foreach (var t in allThreads) {
				if (t.AppDomain == appDomain)
					threads.Add(t);
			}
			threads.Sort((a, b) => GetThreadOrder(a).CompareTo(GetThreadOrder(b)));
			return threads;
		}

		int GetThreadOrder(DbgThread a) {
			if (a == DbgRuntime.Process.DbgManager.CurrentThread.Current)
				return 0;
			if (a == DbgRuntime.Process.DbgManager.CurrentThread.Break)
				return 1;
			if (a.IsMain)
				return 2;
			if (a.Kind != PredefinedThreadKinds.Finalizer)
				return 3;
			return int.MaxValue;
		}

		internal DbgDotNetValueResult FuncEvalCall_AnyThread_MonoDebug(DmdMethodBase method, DbgDotNetValue? obj, object?[] arguments, DbgDotNetInvokeOptions invokeOptions, bool newObj) {
			debuggerThread.VerifyAccess();
			var cancellationToken = CancellationToken.None;
			foreach (var thread in GetFuncEvalCallThreads(method.AppDomain)) {
				var res = FuncEvalCallCore_MonoDebug(null, null, thread, method, obj, arguments, invokeOptions, newObj, cancellationToken);
				if (!res.HasError)
					return res;
			}
			return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
		}

		internal DbgDotNetValueResult FuncEvalCall_MonoDebug(DbgEvaluationInfo evalInfo, DmdMethodBase method, DbgDotNetValue? obj, object?[] arguments, DbgDotNetInvokeOptions invokeOptions, bool newObj) {
			debuggerThread.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(evalInfo.Context);
			if (!(tmp is null))
				return tmp.Value;
			return FuncEvalCallCore_MonoDebug(evalInfo.Context, evalInfo.Frame, evalInfo.Frame.Thread, method, obj, arguments, invokeOptions, newObj, evalInfo.CancellationToken);
		}

		internal MonoTypeLoader CreateMonoTypeLoader(DbgEvaluationInfo evalInfo) =>
			new MonoTypeLoaderImpl(this, evalInfo);

		internal MonoTypeLoader? TryCreateMonoTypeLoader(DbgEvaluationContext? contextOpt, DbgStackFrame? frameOpt, CancellationToken cancellationToken) {
			if (contextOpt is null || frameOpt is null)
				return null;
			return CreateMonoTypeLoader(new DbgEvaluationInfo(contextOpt, frameOpt, cancellationToken));
		}

		DbgDotNetValueResult FuncEvalCallCore_MonoDebug(DbgEvaluationContext? contextOpt, DbgStackFrame? frameOpt, DbgThread thread, DmdMethodBase method, DbgDotNetValue? obj, object?[] arguments, DbgDotNetInvokeOptions invokeOptions, bool newObj, CancellationToken cancellationToken) {
			// ReturnOutThis is only available since 2.35 so we'll special case the common case where a struct ctor
			// is called (CALL/CALLVIRT). We'll change it to a NEWOBJ and then copy the result to the input 'this' value.
			if (!newObj && obj is DbgDotNetValueImpl objImpl && method is DmdConstructorInfo ctor && ctor.ReflectedType!.IsValueType) {
				var res = FuncEvalCallCoreReal_MonoDebug(contextOpt, frameOpt, thread, method, null, arguments, invokeOptions, true, cancellationToken);
				if (res.IsNormalResult) {
					try {
						var error = objImpl.ValueLocation.Store(((DbgDotNetValueImpl)res.Value!).Value);
						if (!(error is null)) {
							res.Value?.Dispose();
							return DbgDotNetValueResult.CreateError(error);
						}
					}
					catch {
						res.Value?.Dispose();
						throw;
					}
				}
				return res;
			}
			else
				return FuncEvalCallCoreReal_MonoDebug(contextOpt, frameOpt, thread, method, obj, arguments, invokeOptions, newObj, cancellationToken);
		}

		DbgDotNetValueResult FuncEvalCallCoreReal_MonoDebug(DbgEvaluationContext? contextOpt, DbgStackFrame? frameOpt, DbgThread thread, DmdMethodBase method, DbgDotNetValue? obj, object?[] arguments, DbgDotNetInvokeOptions invokeOptions, bool newObj, CancellationToken cancellationToken) {
			debuggerThread.VerifyAccess();
			Debug.Assert(!newObj || method.IsConstructor);

			Debug.Assert(method.SpecialMethodKind == DmdSpecialMethodKind.Metadata, "Methods not defined in metadata should be emulated by other code (i.e., the caller)");
			if (method.SpecialMethodKind != DmdSpecialMethodKind.Metadata)
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);

			if (!vm!.Version.AtLeast(2, 24) && method is DmdMethodInfo && method.IsConstructedGenericMethod)
				return DbgDotNetValueResult.CreateError(dnSpy_Debugger_DotNet_Mono_Resources.Error_RuntimeDoesNotSupportCallingGenericMethods);

			var funcEvalOptions = FuncEvalOptions.None;
			if ((invokeOptions & DbgDotNetInvokeOptions.NonVirtual) == 0 && !method.IsStatic && (method.IsVirtual || method.IsAbstract))
				funcEvalOptions |= FuncEvalOptions.Virtual;
			MethodMirror func;
			DmdMethodBase calledMethod;
			if ((funcEvalOptions & FuncEvalOptions.Virtual) == 0 || vm.Version.AtLeast(2, 37))
				func = MethodCache.GetMethod(calledMethod = method, TryCreateMonoTypeLoader(contextOpt, frameOpt, cancellationToken));
			else {
				func = MethodCache.GetMethod(calledMethod = FindOverloadedMethod(obj?.Type, method), TryCreateMonoTypeLoader(contextOpt, frameOpt, cancellationToken));
				funcEvalOptions &= ~FuncEvalOptions.Virtual;
			}
			if (!vm.Version.AtLeast(2, 15) && calledMethod.DeclaringType!.ContainsGenericParameters)
				return DbgDotNetValueResult.CreateError(dnSpy_Debugger_DotNet_Mono_Resources.Error_CannotAccessMemberRuntimeLimitations);

			var monoThread = GetThread(thread);
			try {
				using (var funcEval = CreateFuncEval2(contextOpt, monoThread, cancellationToken)) {
					var converter = new EvalArgumentConverter(this, funcEval, monoThread.Domain, method.AppDomain);

					var paramTypes = GetAllMethodParameterTypes(method.GetMethodSignature());
					if (paramTypes.Count != arguments.Length)
						throw new InvalidOperationException();

					int argsCount = arguments.Length;
					var args = argsCount == 0 ? Array.Empty<Value>() : new Value[argsCount];
					DmdType origType;
					Value? hiddenThisValue;
					Value? createdResultValue = null;
					if (!method.IsStatic && !newObj) {
						var declType = method.DeclaringType!;
						if (method is DmdMethodInfo m)
							declType = m.GetBaseDefinition().DeclaringType!;
						var val = converter.Convert(obj, declType, out origType);
						if (!(val.ErrorMessage is null))
							return DbgDotNetValueResult.CreateError(val.ErrorMessage);
						// Don't box it if it's a value type and it implements the method, eg. 1.ToString() fails without this check
						if (origType.IsValueType && method.DeclaringType == origType) {
							if (val.Value is ObjectMirror)
								hiddenThisValue = ValueUtils.Unbox((ObjectMirror)val.Value, origType);
							else
								hiddenThisValue = val.Value!;
						}
						else
							hiddenThisValue = BoxIfNeeded(monoThread.Domain, val.Value!, declType, origType);
						if (val.Value == hiddenThisValue && val.Value is StructMirror && vm.Version.AtLeast(2, 35))
							funcEvalOptions |= FuncEvalOptions.ReturnOutThis;
					}
					else if (newObj && method.ReflectedType!.IsValueType) {
						if (!(contextOpt is null) && !(frameOpt is null)) {
							//TODO: The Mono fork Unity uses doesn't support this, it returns nothing
							var evalInfo = new DbgEvaluationInfo(contextOpt, frameOpt, cancellationToken);
							hiddenThisValue = CreateValueType(evalInfo, method.ReflectedType, 0);
							createdResultValue = hiddenThisValue;
							newObj = false;
							if (hiddenThisValue is StructMirror && vm.Version.AtLeast(2, 35))
								funcEvalOptions |= FuncEvalOptions.ReturnOutThis;
						}
						else
							hiddenThisValue = null;
					}
					else
						hiddenThisValue = null;
					for (int i = 0; i < arguments.Length; i++) {
						var paramType = paramTypes[i];
						var val = converter.Convert(arguments[i], paramType, out origType);
						if (!(val.ErrorMessage is null))
							return DbgDotNetValueResult.CreateError(val.ErrorMessage);
						var valType = origType ?? MonoValueTypeCreator.CreateType(this, val.Value!, paramType);
						args[i] = BoxIfNeeded(monoThread.Domain, val.Value!, paramType, valType);
					}

					var res = newObj ?
						funcEval.CreateInstance(func, args, funcEvalOptions) :
						funcEval.CallMethod(func, hiddenThisValue, args, funcEvalOptions);
					if (res is null)
						return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
					if ((funcEvalOptions & FuncEvalOptions.ReturnOutThis) != 0 && res.OutThis is StructMirror outStructMirror) {
						var error = (obj as DbgDotNetValueImpl)?.ValueLocation.Store(outStructMirror);
						if (!(error is null))
							return DbgDotNetValueResult.CreateError(error);
					}
					var returnType = (method as DmdMethodInfo)?.ReturnType ?? method.ReflectedType!;
					var returnValue = res.Exception ?? res.Result ?? createdResultValue ?? new PrimitiveValue(vm, ElementType.Object, null);
					var valueLocation = new NoValueLocation(returnType, returnValue);
					if (!(res.Exception is null))
						return DbgDotNetValueResult.CreateException(CreateDotNetValue_MonoDebug(valueLocation));
					return DbgDotNetValueResult.Create(CreateDotNetValue_MonoDebug(valueLocation));
				}
			}
			catch (VMNotSuspendedException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CantFuncEvaluateWhenThreadIsAtUnsafePoint);
			}
			catch (TimeoutException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
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

		internal DbgDotNetValueResult Box_MonoDebug(DbgEvaluationInfo evalInfo, Value value, DmdType type) {
			debuggerThread.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			var tmp = CheckFuncEval(evalInfo.Context);
			if (!(tmp is null))
				return tmp.Value;

			var monoThread = GetThread(evalInfo.Frame.Thread);
			try {
				using (var funcEval = CreateFuncEval(evalInfo.Context, monoThread, evalInfo.CancellationToken)) {
					value = ValueUtils.MakePrimitiveValueIfPossible(value, type);
					var boxedValue = BoxIfNeeded(monoThread.Domain, value, type.AppDomain.System_Object, type);
					if (boxedValue is null)
						return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
					return DbgDotNetValueResult.Create(CreateDotNetValue_MonoDebug(type.AppDomain, boxedValue, type));
				}
			}
			catch (VMNotSuspendedException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CantFuncEvaluateWhenThreadIsAtUnsafePoint);
			}
			catch (TimeoutException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		internal DbgDotNetValueResult CreateValue_MonoDebug(DbgEvaluationInfo evalInfo, object? value) {
			debuggerThread.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			if (value is DbgDotNetValueImpl)
				return DbgDotNetValueResult.Create((DbgDotNetValueImpl)value);
			var tmp = CheckFuncEval(evalInfo.Context);
			if (!(tmp is null))
				return tmp.Value;

			var monoThread = GetThread(evalInfo.Frame.Thread);
			try {
				var reflectionAppDomain = evalInfo.Frame.AppDomain?.GetReflectionAppDomain() ?? throw new InvalidOperationException();
				using (var funcEval = CreateFuncEval(evalInfo.Context, monoThread, evalInfo.CancellationToken)) {
					var converter = new EvalArgumentConverter(this, funcEval, monoThread.Domain, reflectionAppDomain);
					var evalRes = converter.Convert(value, reflectionAppDomain.System_Object, out var newValueType);
					if (!(evalRes.ErrorMessage is null))
						return DbgDotNetValueResult.CreateError(evalRes.ErrorMessage);

					var resultValue = CreateDotNetValue_MonoDebug(reflectionAppDomain, evalRes.Value!, newValueType);
					return DbgDotNetValueResult.Create(resultValue);
				}
			}
			catch (VMNotSuspendedException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.CantFuncEvaluateWhenThreadIsAtUnsafePoint);
			}
			catch (TimeoutException) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		internal DbgCreateMonoValueResult CreateMonoValue_MonoDebug(DbgEvaluationInfo evalInfo, object? value, DmdType targetType) {
			debuggerThread.VerifyAccess();
			evalInfo.CancellationToken.ThrowIfCancellationRequested();
			if (value is DbgDotNetValueImpl)
				return new DbgCreateMonoValueResult(((DbgDotNetValueImpl)value).Value);
			var tmp = CheckFuncEval(evalInfo.Context);
			if (!(tmp is null))
				return new DbgCreateMonoValueResult(tmp.Value.ErrorMessage ?? throw new InvalidOperationException());

			var monoThread = GetThread(evalInfo.Frame.Thread);
			try {
				var reflectionAppDomain = evalInfo.Frame.AppDomain?.GetReflectionAppDomain() ?? throw new InvalidOperationException();
				using (var funcEval = CreateFuncEval(evalInfo.Context, monoThread, evalInfo.CancellationToken)) {
					var converter = new EvalArgumentConverter(this, funcEval, monoThread.Domain, reflectionAppDomain);
					var evalRes = converter.Convert(value, targetType, out var newValueType);
					if (!(evalRes.ErrorMessage is null))
						return new DbgCreateMonoValueResult(evalRes.ErrorMessage);
					var newValue = evalRes.Value!;
					if (targetType.IsEnum && !(newValue is EnumMirror))
						newValue = MonoVirtualMachine.CreateEnumMirror(MonoDebugTypeCreator.GetType(this, targetType, CreateMonoTypeLoader(evalInfo)), (PrimitiveValue)newValue);
					newValue = BoxIfNeeded(monoThread.Domain, newValue, targetType, newValueType);
					return new DbgCreateMonoValueResult(newValue);
				}
			}
			catch (VMNotSuspendedException) {
				return new DbgCreateMonoValueResult(PredefinedEvaluationErrorMessages.CantFuncEvaluateWhenThreadIsAtUnsafePoint);
			}
			catch (TimeoutException) {
				return new DbgCreateMonoValueResult(PredefinedEvaluationErrorMessages.FuncEvalTimedOut);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgCreateMonoValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		static DmdMethodBase FindOverloadedMethod(DmdType? objType, DmdMethodBase method) {
			if (objType is null)
				return method;
			if (!method.IsVirtual && !method.IsAbstract)
				return method;
			if (method.DeclaringType == objType)
				return method;
			var comparer = new DmdSigComparer(DmdMemberInfoEqualityComparer.DefaultMember.Options & ~DmdSigComparerOptions.CompareDeclaringType);
			foreach (var m in objType.Methods) {
				if (!m.IsVirtual)
					continue;
				if (comparer.Equals(m, method))
					return m;
			}
			if (method.DeclaringType!.IsInterface) {
				var sig = method.GetMethodSignature();
				foreach (var m in objType.Methods) {
					if (!m.IsVirtual)
						continue;

					//TODO: Use MethodImpl table. For now assume it's an explicitly implemented iface method if it's private and name doesn't match original name
					if (!m.IsPrivate || m.Name == method.Name)
						continue;

					if (comparer.Equals(m.GetMethodSignature(), sig))
						return m;
				}
			}
			return method;
		}

		TypeMirror GetType(DbgEvaluationInfo evalInfo, DmdType type) =>
			MonoDebugTypeCreator.GetType(this, type, CreateMonoTypeLoader(evalInfo));

		internal Value CreateValueType(DbgEvaluationInfo evalInfo, DmdType type, int recursionCounter) {
			if (recursionCounter > 100)
				throw new InvalidOperationException();
			if (!type.IsValueType)
				throw new InvalidOperationException();
			var monoType = GetType(evalInfo, type);
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
				fieldValues.Add(CreateDefaultValue(evalInfo, field, 0));
			}
			if (type.IsEnum)
				return monoType.VirtualMachine.CreateEnumMirror(monoType, (PrimitiveValue)fieldValues[0]);
			return monoType.VirtualMachine.CreateStructMirror(monoType, fieldValues.ToArray());
		}

		Value CreateDefaultValue(DbgEvaluationInfo evalInfo, DmdFieldInfo field, int recursionCounter) {
			var type = field.FieldType;
			if (!type.IsValueType)
				return new PrimitiveValue(vm, ElementType.Object, null);
			if (type.IsPointer || type.IsFunctionPointer)
				return new PrimitiveValue(vm, ElementType.Ptr, 0L);
			if (!type.IsEnum) {
				switch (DmdType.GetTypeCode(type)) {
				case TypeCode.Boolean:		return new PrimitiveValue(vm, ElementType.Boolean, false);
				case TypeCode.Char:			return new PrimitiveValue(vm, ElementType.Char, '\0');
				case TypeCode.SByte:		return new PrimitiveValue(vm, ElementType.I1, (sbyte)0);
				case TypeCode.Byte:			return new PrimitiveValue(vm, ElementType.U1, (byte)0);
				case TypeCode.Int16:		return new PrimitiveValue(vm, ElementType.I2, (short)0);
				case TypeCode.UInt16:		return new PrimitiveValue(vm, ElementType.U2, (ushort)0);
				case TypeCode.Int32:		return new PrimitiveValue(vm, ElementType.I4, 0);
				case TypeCode.UInt32:		return new PrimitiveValue(vm, ElementType.U4, 0U);
				case TypeCode.Int64:		return new PrimitiveValue(vm, ElementType.I8, 0L);
				case TypeCode.UInt64:		return new PrimitiveValue(vm, ElementType.U8, 0UL);
				case TypeCode.Single:		return new PrimitiveValue(vm, ElementType.R4, 0f);
				case TypeCode.Double:		return new PrimitiveValue(vm, ElementType.R8, 0d);
				}
			}
			return CreateValueType(evalInfo, type, recursionCounter + 1);
		}
	}

	readonly struct DbgCreateMonoValueResult {
		public Value? Value { get; }
		public string? ErrorMessage { get; }
		public DbgCreateMonoValueResult(Value value) {
			Value = value ?? throw new ArgumentNullException(nameof(value));
			ErrorMessage = null;
		}
		public DbgCreateMonoValueResult(string errorMessage) {
			Value = null;
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
		}
	}
}
