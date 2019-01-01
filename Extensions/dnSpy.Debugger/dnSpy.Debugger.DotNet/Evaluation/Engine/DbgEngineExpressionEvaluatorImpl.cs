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
using System.Collections.ObjectModel;
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Code;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation.Internal;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Properties;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineExpressionEvaluatorImpl : DbgEngineExpressionEvaluator, IDebuggerDisplayAttributeEvaluator {
		readonly DbgModuleReferenceProvider dbgModuleReferenceProvider;
		readonly DbgDotNetExpressionCompiler expressionCompiler;
		readonly DbgDotNetILInterpreter dnILInterpreter;
		readonly DbgAliasProvider dbgAliasProvider;
		readonly IPredefinedEvaluationErrorMessagesHelper predefinedEvaluationErrorMessagesHelper;

		public DbgEngineExpressionEvaluatorImpl(DbgModuleReferenceProvider dbgModuleReferenceProvider, DbgDotNetExpressionCompiler expressionCompiler, DbgDotNetILInterpreter dnILInterpreter, DbgAliasProvider dbgAliasProvider, IPredefinedEvaluationErrorMessagesHelper predefinedEvaluationErrorMessagesHelper) {
			this.dbgModuleReferenceProvider = dbgModuleReferenceProvider ?? throw new ArgumentNullException(nameof(dbgModuleReferenceProvider));
			this.expressionCompiler = expressionCompiler ?? throw new ArgumentNullException(nameof(expressionCompiler));
			this.dnILInterpreter = dnILInterpreter ?? throw new ArgumentNullException(nameof(dnILInterpreter));
			this.dbgAliasProvider = dbgAliasProvider ?? throw new ArgumentNullException(nameof(dbgAliasProvider));
			this.predefinedEvaluationErrorMessagesHelper = predefinedEvaluationErrorMessagesHelper ?? throw new ArgumentNullException(nameof(predefinedEvaluationErrorMessagesHelper));
		}

		public override DbgEngineEEAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, string valueExpression, DbgEvaluationOptions options) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return AssignCore(evalInfo, expression, valueExpression, options);
			return Assign(dispatcher, evalInfo, expression, valueExpression, options);

			DbgEngineEEAssignmentResult Assign(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2, string expression2, string valueExpression2, DbgEvaluationOptions options2) {
				if (!dispatcher2.TryInvokeRethrow(() => AssignCore(evalInfo2, expression2, valueExpression2, options2), out var result))
					result = new DbgEngineEEAssignmentResult(DbgEEAssignmentResultFlags.None, DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgEngineEEAssignmentResult AssignCore(DbgEvaluationInfo evalInfo, string expression, string valueExpression, DbgEvaluationOptions options) {
			var resultFlags = DbgEEAssignmentResultFlags.None;
			try {
				var info = dbgAliasProvider.GetAliases(evalInfo);
				var refsResult = dbgModuleReferenceProvider.GetModuleReferences(evalInfo.Runtime, evalInfo.Frame, info.typeReferences);
				if (refsResult.ErrorMessage != null)
					return new DbgEngineEEAssignmentResult(resultFlags, refsResult.ErrorMessage);

				var compRes = expressionCompiler.CompileAssignment(evalInfo, refsResult.ModuleReferences, info.aliases, expression, valueExpression, options);
				evalInfo.CancellationToken.ThrowIfCancellationRequested();
				if (compRes.IsError)
					return new DbgEngineEEAssignmentResult(resultFlags | DbgEEAssignmentResultFlags.CompilerError, compRes.ErrorMessage);

				var state = dnILInterpreter.CreateState(compRes.Assembly);
				Debug.Assert(compRes.CompiledExpressions.Length == 1);
				ref var exprInfo = ref compRes.CompiledExpressions[0];
				if (exprInfo.ErrorMessage != null)
					return new DbgEngineEEAssignmentResult(resultFlags | DbgEEAssignmentResultFlags.CompilerError, exprInfo.ErrorMessage);
				resultFlags |= DbgEEAssignmentResultFlags.ExecutedCode;
				var res = dnILInterpreter.Execute(evalInfo, state, exprInfo.TypeName, exprInfo.MethodName, options, out _);
				if (res.HasError)
					return new DbgEngineEEAssignmentResult(resultFlags, res.ErrorMessage);
				if (res.ValueIsException) {
					res.Value.Dispose();
					var error = string.Format(dnSpy_Debugger_DotNet_Resources.Method_X_ThrewAnExceptionOfType_Y, expression, res.Value.Type.FullName);
					return new DbgEngineEEAssignmentResult(resultFlags, error);
				}

				res.Value?.Dispose();
				return new DbgEngineEEAssignmentResult();
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgEngineEEAssignmentResult(resultFlags, PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		public override object CreateExpressionEvaluatorState() => new EvaluateImplExpressionState();

		public override DbgEngineEvaluationResult Evaluate(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options, object state) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return EvaluateCore(evalInfo, expression, options, state);
			return Evaluate(dispatcher, evalInfo, expression, options, state);

			DbgEngineEvaluationResult Evaluate(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2, string expression2, DbgEvaluationOptions options2, object state2) {
				if (!dispatcher2.TryInvokeRethrow(() => EvaluateCore(evalInfo2, expression2, options2, state2), out var result))
					result = new DbgEngineEvaluationResult(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgEngineEvaluationResult EvaluateCore(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options, object state) {
			var res = EvaluateImpl(evalInfo, expression, options | DbgEvaluationOptions.NoName, state);
			if (res.Error != null)
				return new DbgEngineEvaluationResult(res.Error, res.Flags);
			try {
				return new DbgEngineEvaluationResult(new DbgEngineValueImpl(res.Value), res.FormatSpecifiers, res.Flags);
			}
			catch {
				res.Value.Dispose();
				throw;
			}
		}

		sealed class EvaluateImplExpressionState {
			public readonly struct Key {
				readonly DbgEngineExpressionEvaluatorImpl ee;
				readonly int debugInfoVersion;
				readonly object memberModule;
				readonly int memberToken;
				readonly int memberVersion;
				readonly DbgModuleReference[] moduleReferences;
				readonly DbgMethodDebugScope scope;
				readonly DbgDotNetAlias[] aliases;
				readonly DbgEvaluationOptions options;
				readonly string expression;

				public Key(DbgEngineExpressionEvaluatorImpl ee, int debugInfoVersion, object memberModule, int memberToken, int memberVersion, DbgModuleReference[] moduleReferences, DbgMethodDebugScope scope, DbgDotNetAlias[] aliases, DbgEvaluationOptions options, string expression) {
					this.ee = ee;
					this.debugInfoVersion = debugInfoVersion;
					this.memberModule = memberModule;
					this.memberToken = memberToken;
					this.memberVersion = memberVersion;
					this.moduleReferences = moduleReferences;
					this.scope = scope;
					this.aliases = aliases;
					this.options = options;
					this.expression = expression;
				}

				public bool Equals(in Key other) =>
					scope == other.scope &&
					moduleReferences == other.moduleReferences &&
					ee == other.ee &&
					debugInfoVersion == other.debugInfoVersion &&
					memberModule == other.memberModule &&
					memberToken == other.memberToken &&
					memberVersion == other.memberVersion &&
					Equals(aliases, other.aliases) &&
					options == other.options &&
					StringComparer.Ordinal.Equals(expression, other.expression);

				static bool Equals(DbgDotNetAlias[] a, DbgDotNetAlias[] b) {
					if (a.Length != b.Length)
						return false;
					for (int i = 0; i < a.Length; i++) {
						if (!Equals(a[i], b[i]))
							return false;
					}
					return true;
				}

				static bool Equals(DbgDotNetAlias a, DbgDotNetAlias b) =>
					a.Kind == b.Kind &&
					Equals(a.CustomTypeInfo, b.CustomTypeInfo) &&
					a.CustomTypeInfoId == b.CustomTypeInfoId &&
					StringComparer.Ordinal.Equals(a.Name, b.Name) &&
					StringComparer.Ordinal.Equals(a.Type, b.Type);

				static bool Equals(ReadOnlyCollection<byte> a, ReadOnlyCollection<byte> b) {
					if (a == b)
						return true;
					if (a == null || b == null)
						return false;
					if (a.Count != b.Count)
						return false;
					for (int i = 0; i < a.Count; i++) {
						if (a[i] != b[i])
							return false;
					}
					return true;
				}
			}

			public Key CachedKey;
			public DbgDotNetCompilationResult CompilationResult;
			public DbgDotNetILInterpreterState ILInterpreterState;
			public EvaluateImplResult? EvaluateImplResult;
		}

		EvaluateImplResult? GetMethodInterpreterState(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options, object stateObj, out EvaluateImplExpressionState evalExprState) {
			var languageDebugInfo = evalInfo.Context.TryGetLanguageDebugInfo();
			if (languageDebugInfo == null) {
				evalExprState = null;
				return new EvaluateImplResult(dnSpy_Debugger_DotNet_Resources.CantEvaluateWhenCurrentFrameIsNative, CreateName(expression), null, null, 0, PredefinedDbgValueNodeImageNames.Error, null);
			}
			var methodDebugInfo = languageDebugInfo.MethodDebugInfo;
			var module = evalInfo.Frame.Module ?? throw new InvalidOperationException();
			var info = dbgAliasProvider.GetAliases(evalInfo);
			return GetInterpreterStateCommon(evalInfo, null, methodDebugInfo.DebugInfoVersion, module, methodDebugInfo.Method.MDToken.ToInt32(), languageDebugInfo.MethodVersion, MethodDebugScopeUtils.GetScope(methodDebugInfo.Scope, languageDebugInfo.ILOffset), info.aliases, info.typeReferences, options, expression, stateObj, null, out evalExprState);
		}

		EvaluateImplResult? GetTypeInterpreterState(DbgEvaluationInfo evalInfo, DmdType type, string expression, DbgEvaluationOptions options, object stateObj, out EvaluateImplExpressionState evalExprState) {
			if (type.TypeSignatureKind != DmdTypeSignatureKind.Type) {
				evalExprState = null;
				return new EvaluateImplResult(dnSpy_Debugger_DotNet_Resources.CantEvaluateWhenCurrentFrameIsNative, CreateName(expression), null, null, 0, PredefinedDbgValueNodeImageNames.Error, null);
			}

			// This is for evaluating DebuggerDisplayAttribute expressions only, so don't use any aliases.
			// But pass in the same typeReferences so the module references array doesn't get recreated.
			var aliases = Array.Empty<DbgDotNetAlias>();
			var info = dbgAliasProvider.GetAliases(evalInfo);
			var typeReferences = info.typeReferences;

			return GetInterpreterStateCommon(evalInfo, type.Module, 0, type.Module, type.MetadataToken, 0, null, aliases, typeReferences, options, expression, stateObj, type, out evalExprState);
		}

		EvaluateImplResult? GetInterpreterStateCommon(DbgEvaluationInfo evalInfo, DmdModule reflectionModuleOrNull, int debugInfoVersion, object memberModule, int memberToken, int memberVersion, DbgMethodDebugScope scope, DbgDotNetAlias[] aliases, DmdType[] typeReferences, DbgEvaluationOptions options, string expression, object stateObj, DmdType type, out EvaluateImplExpressionState evalExprState) {
			evalExprState = null;
			EvaluateImplExpressionState evalState;
			if (stateObj != null) {
				evalState = stateObj as EvaluateImplExpressionState;
				Debug.Assert(evalState != null);
				if (evalState == null)
					throw new ArgumentException("Invalid expression evaluator state. It must be null or created by " + nameof(DbgExpressionEvaluator) + "." + nameof(DbgExpressionEvaluator.CreateExpressionEvaluatorState) + "()");
			}
			else
				evalState = evalInfo.Context.GetOrCreateData<EvaluateImplExpressionState>();

			var refsResult = reflectionModuleOrNull != null ?
				dbgModuleReferenceProvider.GetModuleReferences(evalInfo.Runtime, reflectionModuleOrNull, typeReferences) :
				dbgModuleReferenceProvider.GetModuleReferences(evalInfo.Runtime, evalInfo.Frame, typeReferences);
			if (refsResult.ErrorMessage != null)
				return new EvaluateImplResult(refsResult.ErrorMessage, CreateName(expression), null, null, 0, PredefinedDbgValueNodeImageNames.Error, null);

			var keyOptions = options & ~(DbgEvaluationOptions.NoSideEffects | DbgEvaluationOptions.NoFuncEval);
			var key = new EvaluateImplExpressionState.Key(this, debugInfoVersion, memberModule, memberToken, memberVersion, refsResult.ModuleReferences, scope, aliases, keyOptions, expression);
			if (!evalState.CachedKey.Equals(key)) {
				evalState.CompilationResult = (object)type != null ?
					expressionCompiler.CompileTypeExpression(evalInfo, type, refsResult.ModuleReferences, aliases, expression, keyOptions) :
					expressionCompiler.CompileExpression(evalInfo, refsResult.ModuleReferences, aliases, expression, keyOptions);
				evalState.CachedKey = key;
				evalState.EvaluateImplResult = GetEvaluateImplResult(evalState.CompilationResult, expression);
				if (evalState.EvaluateImplResult == null)
					evalState.ILInterpreterState = dnILInterpreter.CreateState(evalState.CompilationResult.Assembly);
				else
					evalState.ILInterpreterState = null;
			}

			evalExprState = evalState;
			return evalState.EvaluateImplResult;
		}

		static EvaluateImplResult? GetEvaluateImplResult(in DbgDotNetCompilationResult compRes, string expression) {
			if (compRes.IsError)
				return new EvaluateImplResult(compRes.ErrorMessage, CreateName(expression), null, null, 0, PredefinedDbgValueNodeImageNames.Error, null);
			Debug.Assert(compRes.CompiledExpressions.Length == 1);
			if (compRes.CompiledExpressions.Length != 1)
				return new EvaluateImplResult(PredefinedEvaluationErrorMessages.InternalDebuggerError, CreateName(expression), null, null, 0, PredefinedDbgValueNodeImageNames.Error, null);
			var exprInfo = compRes.CompiledExpressions[0];
			if (exprInfo.ErrorMessage != null)
				return new EvaluateImplResult(exprInfo.ErrorMessage, exprInfo.Name, null, exprInfo.FormatSpecifiers, exprInfo.Flags & ~DbgEvaluationResultFlags.SideEffects, exprInfo.ImageName, null);

			return null;
		}

		static bool HasAllowFuncEval(ReadOnlyCollection<string> formatSpecifiers) =>
			formatSpecifiers?.Contains(PredefinedFormatSpecifiers.AllowFuncEval) == true;

		internal EvaluateImplResult EvaluateImpl(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options, object stateObj) {
			try {
				var errorRes = GetMethodInterpreterState(evalInfo, expression, options, stateObj, out var state);
				if (errorRes != null)
					return errorRes.Value;

				Debug.Assert(state.CompilationResult.CompiledExpressions.Length == 1);
				ref var exprInfo = ref state.CompilationResult.CompiledExpressions[0];

				if ((options & DbgEvaluationOptions.NoSideEffects) != 0 && (exprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0 && !HasAllowFuncEval(exprInfo.FormatSpecifiers))
					return new EvaluateImplResult(PredefinedEvaluationErrorMessages.ExpressionCausesSideEffects, exprInfo.Name, null, exprInfo.FormatSpecifiers, exprInfo.Flags, exprInfo.ImageName, null);

				var res = dnILInterpreter.Execute(evalInfo, state.ILInterpreterState, exprInfo.TypeName, exprInfo.MethodName, options, out var expectedType);
				if (res.HasError)
					return new EvaluateImplResult(res.ErrorMessage, exprInfo.Name, null, exprInfo.FormatSpecifiers, exprInfo.Flags & ~DbgEvaluationResultFlags.SideEffects, exprInfo.ImageName, expectedType);
				if (res.ValueIsException)
					return new EvaluateImplResult(null, exprInfo.Name, res.Value, exprInfo.FormatSpecifiers, (exprInfo.Flags & ~DbgEvaluationResultFlags.SideEffects) | DbgEvaluationResultFlags.ThrownException, PredefinedDbgValueNodeImageNames.Error, expectedType);
				return new EvaluateImplResult(null, exprInfo.Name, res.Value, exprInfo.FormatSpecifiers, exprInfo.Flags, exprInfo.ImageName, expectedType);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new EvaluateImplResult(PredefinedEvaluationErrorMessages.InternalDebuggerError, DbgDotNetEngineValueNodeFactoryExtensions.errorName, null, null, DbgEvaluationResultFlags.None, PredefinedDbgValueNodeImageNames.Error, null);
			}
		}

		static DbgDotNetText CreateName(string expression) => new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.Error, expression));

		DbgDotNetEvalResult IDebuggerDisplayAttributeEvaluator.Evaluate(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, string expression, DbgEvaluationOptions options, object state) {
			var dispatcher = evalInfo.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return EvaluateCore(evalInfo, obj, expression, options, state);
			return Evaluate2(dispatcher, evalInfo, obj, expression, options, state);

			DbgDotNetEvalResult Evaluate2(DbgDotNetDispatcher dispatcher2, DbgEvaluationInfo evalInfo2, DbgDotNetValue obj2, string expression2, DbgEvaluationOptions options2, object state2) {
				if (!dispatcher2.TryInvokeRethrow(() => EvaluateCore(evalInfo2, obj2, expression2, options2, state2), out var result))
					result = new DbgDotNetEvalResult(DispatcherConstants.ProcessExitedError);
				return result;
			}
		}

		DbgDotNetEvalResult EvaluateCore(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, string expression, DbgEvaluationOptions options, object state) {
			var res = EvaluateImpl(evalInfo, obj, expression, options, state);
			if (res.Error != null)
				return new DbgDotNetEvalResult(predefinedEvaluationErrorMessagesHelper.GetErrorMessage(res.Error), res.FormatSpecifiers, res.Flags);
			return new DbgDotNetEvalResult(res.Value, res.FormatSpecifiers, res.Flags);
		}

		EvaluateImplResult EvaluateImpl(DbgEvaluationInfo evalInfo, DbgDotNetValue obj, string expression, DbgEvaluationOptions options, object stateObj) {
			try {
				var type = obj.Type;
				if (type.IsGenericType)
					type = type.GetGenericTypeDefinition();
				var errorRes = GetTypeInterpreterState(evalInfo, type, expression, options | DbgEvaluationOptions.NoName, stateObj, out var state);
				if (errorRes != null)
					return errorRes.Value;

				var genericTypeArguments = obj.Type.GetGenericArguments();
				var genericMethodArguments = Array.Empty<DmdType>();

				Debug.Assert(state.CompilationResult.CompiledExpressions.Length == 1);
				ref var exprInfo = ref state.CompilationResult.CompiledExpressions[0];

				if ((options & DbgEvaluationOptions.NoSideEffects) != 0 && (exprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0 && !HasAllowFuncEval(exprInfo.FormatSpecifiers))
					return new EvaluateImplResult(PredefinedEvaluationErrorMessages.ExpressionCausesSideEffects, exprInfo.Name, null, exprInfo.FormatSpecifiers, exprInfo.Flags, exprInfo.ImageName, null);

				var argumentsProvider = new TypeArgumentsProvider(obj);
				var localsProvider = DummyLocalsProvider.Instance;
				var res = dnILInterpreter.Execute(evalInfo, genericTypeArguments, genericMethodArguments, argumentsProvider, localsProvider, state.ILInterpreterState, exprInfo.TypeName, exprInfo.MethodName, options, out var expectedType);
				if (res.HasError)
					return new EvaluateImplResult(res.ErrorMessage, exprInfo.Name, null, exprInfo.FormatSpecifiers, exprInfo.Flags & ~DbgEvaluationResultFlags.SideEffects, exprInfo.ImageName, expectedType);
				if (res.ValueIsException)
					return new EvaluateImplResult(null, exprInfo.Name, res.Value, exprInfo.FormatSpecifiers, exprInfo.Flags & ~DbgEvaluationResultFlags.SideEffects, PredefinedDbgValueNodeImageNames.Error, expectedType);
				return new EvaluateImplResult(null, exprInfo.Name, res.Value, exprInfo.FormatSpecifiers, exprInfo.Flags, exprInfo.ImageName, expectedType);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new EvaluateImplResult(PredefinedEvaluationErrorMessages.InternalDebuggerError, DbgDotNetEngineValueNodeFactoryExtensions.errorName, null, null, DbgEvaluationResultFlags.None, PredefinedDbgValueNodeImageNames.Error, null);
			}
		}

		sealed class TypeArgumentsProvider : VariablesProvider {
			readonly DbgDotNetValue argument;

			public TypeArgumentsProvider(DbgDotNetValue argument) => this.argument = argument;

			public override void Initialize(DbgEvaluationInfo evalInfo, DmdMethodBase method, DmdMethodBody body) { }

			public override DbgDotNetValue GetValueAddress(int index, DmdType targetType) => null;

			public override DbgDotNetValueResult GetVariable(int index) {
				if (index == 0)
					return DbgDotNetValueResult.Create(argument);
				return DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}

			public override string SetVariable(int index, DmdType targetType, object value) => PredefinedEvaluationErrorMessages.InternalDebuggerError;
			public override bool CanDispose(DbgDotNetValue value) => value != argument;
			public override void Clear() { }
		}

		sealed class DummyLocalsProvider : VariablesProvider {
			public static readonly DummyLocalsProvider Instance = new DummyLocalsProvider();
			DummyLocalsProvider() { }
			public override void Initialize(DbgEvaluationInfo evalInfo, DmdMethodBase method, DmdMethodBody body) { }
			public override DbgDotNetValue GetValueAddress(int index, DmdType targetType) => null;
			public override DbgDotNetValueResult GetVariable(int index) => DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			public override string SetVariable(int index, DmdType targetType, object value) => PredefinedEvaluationErrorMessages.InternalDebuggerError;
			public override bool CanDispose(DbgDotNetValue value) => true;
			public override void Clear() { }
		}
	}

	readonly struct EvaluateImplResult {
		public readonly string Error;
		public readonly DbgDotNetText Name;
		public readonly DbgDotNetValue Value;
		public readonly ReadOnlyCollection<string> FormatSpecifiers;
		public readonly DbgEvaluationResultFlags Flags;
		public readonly string ImageName;
		public readonly DmdType Type;

		public EvaluateImplResult(string error, DbgDotNetText name, DbgDotNetValue value, ReadOnlyCollection<string> formatSpecifiers, DbgEvaluationResultFlags flags, string imageName, DmdType type) {
			Error = error;
			Name = name;
			Value = value;
			FormatSpecifiers = formatSpecifiers;
			Flags = flags;
			ImageName = imageName;
			Type = type;
		}
	}
}
