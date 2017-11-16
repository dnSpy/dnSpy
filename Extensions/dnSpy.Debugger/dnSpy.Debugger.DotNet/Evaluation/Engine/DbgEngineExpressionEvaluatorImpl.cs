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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Engine.Evaluation.Internal;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Properties;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineExpressionEvaluatorImpl : DbgEngineExpressionEvaluator, IDebuggerDisplayAttributeEvaluator {
		readonly DbgModuleReferenceProvider dbgModuleReferenceProvider;
		readonly DbgDotNetExpressionCompiler expressionCompiler;
		readonly DbgDotNetILInterpreter dnILInterpreter;
		readonly DbgObjectIdService objectIdService;
		readonly IPredefinedEvaluationErrorMessagesHelper predefinedEvaluationErrorMessagesHelper;

		public DbgEngineExpressionEvaluatorImpl(DbgModuleReferenceProvider dbgModuleReferenceProvider, DbgDotNetExpressionCompiler expressionCompiler, DbgDotNetILInterpreter dnILInterpreter, DbgObjectIdService objectIdService, IPredefinedEvaluationErrorMessagesHelper predefinedEvaluationErrorMessagesHelper) {
			this.dbgModuleReferenceProvider = dbgModuleReferenceProvider ?? throw new ArgumentNullException(nameof(dbgModuleReferenceProvider));
			this.expressionCompiler = expressionCompiler ?? throw new ArgumentNullException(nameof(expressionCompiler));
			this.dnILInterpreter = dnILInterpreter ?? throw new ArgumentNullException(nameof(dnILInterpreter));
			this.objectIdService = objectIdService ?? throw new ArgumentNullException(nameof(objectIdService));
			this.predefinedEvaluationErrorMessagesHelper = predefinedEvaluationErrorMessagesHelper ?? throw new ArgumentNullException(nameof(predefinedEvaluationErrorMessagesHelper));
		}

		public override DbgEngineEEAssignmentResult Assign(DbgEvaluationContext context, DbgStackFrame frame, string expression, string valueExpression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			var dispatcher = context.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return AssignCore(context, frame, expression, valueExpression, options, cancellationToken);
			return Assign(dispatcher, context, frame, expression, valueExpression, options, cancellationToken);

			DbgEngineEEAssignmentResult Assign(DbgDotNetDispatcher dispatcher2, DbgEvaluationContext context2, DbgStackFrame frame2, string expression2, string valueExpression2, DbgEvaluationOptions options2, CancellationToken cancellationToken2) =>
				dispatcher2.InvokeRethrow(() => AssignCore(context2, frame2, expression2, valueExpression2, options2, cancellationToken2));
		}

		DbgEngineEEAssignmentResult AssignCore(DbgEvaluationContext context, DbgStackFrame frame, string expression, string valueExpression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			var resultFlags = DbgEEAssignmentResultFlags.None;
			try {
				var refsResult = dbgModuleReferenceProvider.GetModuleReferences(context.Runtime, frame);
				if (refsResult.ErrorMessage != null)
					return new DbgEngineEEAssignmentResult(resultFlags, refsResult.ErrorMessage);

				var aliases = GetAliases(context, frame, cancellationToken);
				var compRes = expressionCompiler.CompileAssignment(context, frame, refsResult.ModuleReferences, aliases, expression, valueExpression, options, cancellationToken);
				cancellationToken.ThrowIfCancellationRequested();
				if (compRes.IsError)
					return new DbgEngineEEAssignmentResult(resultFlags | DbgEEAssignmentResultFlags.CompilerError, compRes.ErrorMessage);

				var state = dnILInterpreter.CreateState(compRes.Assembly);
				ref var exprInfo = ref compRes.CompiledExpressions[0];
				if (exprInfo.ErrorMessage != null)
					return new DbgEngineEEAssignmentResult(resultFlags | DbgEEAssignmentResultFlags.CompilerError, exprInfo.ErrorMessage);
				resultFlags |= DbgEEAssignmentResultFlags.ExecutedCode;
				var res = dnILInterpreter.Execute(context, frame, state, exprInfo.TypeName, exprInfo.MethodName, options, out _, cancellationToken);
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

		public override DbgEngineEvaluationResult Evaluate(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, object state, CancellationToken cancellationToken) {
			var dispatcher = context.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return EvaluateCore(context, frame, expression, options, state, cancellationToken);
			return Evaluate(dispatcher, context, frame, expression, options, state, cancellationToken);

			DbgEngineEvaluationResult Evaluate(DbgDotNetDispatcher dispatcher2, DbgEvaluationContext context2, DbgStackFrame frame2, string expression2, DbgEvaluationOptions options2, object state2, CancellationToken cancellationToken2) =>
				dispatcher2.InvokeRethrow(() => EvaluateCore(context2, frame2, expression2, options2, state2, cancellationToken2));
		}

		DbgEngineEvaluationResult EvaluateCore(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, object state, CancellationToken cancellationToken) {
			var res = EvaluateImpl(context, frame, expression, options | DbgEvaluationOptions.NoName, state, cancellationToken);
			if (res.Error != null)
				return new DbgEngineEvaluationResult(res.Error, res.Flags);
			try {
				return new DbgEngineEvaluationResult(new DbgEngineValueImpl(res.Value), res.Flags);
			}
			catch {
				res.Value.Dispose();
				throw;
			}
		}

		sealed class EvaluateImplExpressionState {
			public struct Key : IEquatable<Key> {
				readonly DbgEngineExpressionEvaluatorImpl ee;
				readonly int decompilerOptionsVersion;
				readonly object memberModule;
				readonly int memberToken;
				readonly int memberVersion;
				readonly DbgModuleReference[] moduleReferences;
				readonly MethodDebugScope scope;
				readonly DbgDotNetAlias[] aliases;
				readonly DbgEvaluationOptions options;
				readonly string expression;

				public Key(DbgEngineExpressionEvaluatorImpl ee, int decompilerOptionsVersion, object memberModule, int memberToken, int memberVersion, DbgModuleReference[] moduleReferences, MethodDebugScope scope, DbgDotNetAlias[] aliases, DbgEvaluationOptions options, string expression) {
					this.ee = ee;
					this.decompilerOptionsVersion = decompilerOptionsVersion;
					this.memberModule = memberModule;
					this.memberToken = memberToken;
					this.memberVersion = memberVersion;
					this.moduleReferences = moduleReferences;
					this.scope = scope;
					this.aliases = aliases;
					this.options = options;
					this.expression = expression;
				}

				public bool Equals(Key other) =>
					scope == other.scope &&
					moduleReferences == other.moduleReferences &&
					ee == other.ee &&
					decompilerOptionsVersion == other.decompilerOptionsVersion &&
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

		EvaluateImplResult? GetMethodInterpreterState(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, object stateObj, CancellationToken cancellationToken, out EvaluateImplExpressionState evalExprState) {
			var languageDebugInfo = context.TryGetLanguageDebugInfo();
			if (languageDebugInfo == null) {
				evalExprState = null;
				return new EvaluateImplResult(dnSpy_Debugger_DotNet_Resources.CantEvaluateWhenCurrentFrameIsNative, CreateName(expression), null, 0, PredefinedDbgValueNodeImageNames.Error, null);
			}
			var methodDebugInfo = languageDebugInfo.MethodDebugInfo;
			var module = frame.Module ?? throw new InvalidOperationException();
			var aliases = GetAliases(context, frame, cancellationToken);
			return GetInterpreterStateCommon(context, frame, null, methodDebugInfo.DecompilerOptionsVersion, module, methodDebugInfo.Method.MDToken.ToInt32(), languageDebugInfo.MethodVersion, MethodDebugScopeUtils.GetScope(methodDebugInfo.Scope, languageDebugInfo.ILOffset), aliases, options, expression, stateObj, null, out evalExprState, cancellationToken);
		}

		EvaluateImplResult? GetTypeInterpreterState(DbgEvaluationContext context, DbgStackFrame frame, DmdType type, string expression, DbgEvaluationOptions options, object stateObj, CancellationToken cancellationToken, out EvaluateImplExpressionState evalExprState) {
			if (type.TypeSignatureKind != DmdTypeSignatureKind.Type) {
				evalExprState = null;
				return new EvaluateImplResult(dnSpy_Debugger_DotNet_Resources.CantEvaluateWhenCurrentFrameIsNative, CreateName(expression), null, 0, PredefinedDbgValueNodeImageNames.Error, null);
			}

			// This is for evaluating DebuggerDisplayAttribute expressions only, so don't use any aliases
			var aliases = Array.Empty<DbgDotNetAlias>();

			return GetInterpreterStateCommon(context, frame, type.Module, 0, type.Module, type.MetadataToken, 0, null, aliases, options, expression, stateObj, type, out evalExprState, cancellationToken);
		}

		EvaluateImplResult? GetInterpreterStateCommon(DbgEvaluationContext context, DbgStackFrame frame, DmdModule reflectionModuleOrNull, int decompilerOptionsVersion, object memberModule, int memberToken, int memberVersion, MethodDebugScope scope, DbgDotNetAlias[] aliases, DbgEvaluationOptions options, string expression, object stateObj, DmdType type, out EvaluateImplExpressionState evalExprState, CancellationToken cancellationToken) {
			evalExprState = null;
			EvaluateImplExpressionState evalState;
			if (stateObj != null) {
				evalState = stateObj as EvaluateImplExpressionState;
				Debug.Assert(evalState != null);
				if (evalState == null)
					throw new ArgumentException("Invalid expression evaluator state. It must be null or created by " + nameof(DbgExpressionEvaluator) + "." + nameof(DbgExpressionEvaluator.CreateExpressionEvaluatorState) + "()");
			}
			else
				evalState = context.GetOrCreateData<EvaluateImplExpressionState>();

			var refsResult = reflectionModuleOrNull != null ?
				dbgModuleReferenceProvider.GetModuleReferences(context.Runtime, reflectionModuleOrNull) :
				dbgModuleReferenceProvider.GetModuleReferences(context.Runtime, frame);
			if (refsResult.ErrorMessage != null)
				return new EvaluateImplResult(refsResult.ErrorMessage, CreateName(expression), null, 0, PredefinedDbgValueNodeImageNames.Error, null);

			var keyOptions = options & ~(DbgEvaluationOptions.NoSideEffects | DbgEvaluationOptions.NoFuncEval);
			var key = new EvaluateImplExpressionState.Key(this, decompilerOptionsVersion, memberModule, memberToken, memberVersion, refsResult.ModuleReferences, scope, aliases, keyOptions, expression);
			if (!evalState.CachedKey.Equals(key)) {
				evalState.CompilationResult = (object)type != null ?
					expressionCompiler.CompileTypeExpression(context, frame, type, refsResult.ModuleReferences, aliases, expression, keyOptions, cancellationToken) :
					expressionCompiler.CompileExpression(context, frame, refsResult.ModuleReferences, aliases, expression, keyOptions, cancellationToken);
				evalState.CachedKey = key;
				evalState.EvaluateImplResult = GetEvaluateImplResult(ref evalState.CompilationResult, expression);
				if (evalState.EvaluateImplResult == null)
					evalState.ILInterpreterState = dnILInterpreter.CreateState(evalState.CompilationResult.Assembly);
				else
					evalState.ILInterpreterState = null;
			}

			evalExprState = evalState;
			return evalState.EvaluateImplResult;
		}

		static EvaluateImplResult? GetEvaluateImplResult(ref DbgDotNetCompilationResult compRes, string expression) {
			if (compRes.IsError)
				return new EvaluateImplResult(compRes.ErrorMessage, CreateName(expression), null, 0, PredefinedDbgValueNodeImageNames.Error, null);
			Debug.Assert(compRes.CompiledExpressions.Length == 1);
			if (compRes.CompiledExpressions.Length != 1)
				return new EvaluateImplResult(PredefinedEvaluationErrorMessages.InternalDebuggerError, CreateName(expression), null, 0, PredefinedDbgValueNodeImageNames.Error, null);
			var exprInfo = compRes.CompiledExpressions[0];
			if (exprInfo.ErrorMessage != null)
				return new EvaluateImplResult(exprInfo.ErrorMessage, exprInfo.Name, null, exprInfo.Flags & ~DbgEvaluationResultFlags.SideEffects, exprInfo.ImageName, null);

			return null;
		}

		internal EvaluateImplResult EvaluateImpl(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, object stateObj, CancellationToken cancellationToken) {
			try {
				var errorRes = GetMethodInterpreterState(context, frame, expression, options, stateObj, cancellationToken, out var state);
				if (errorRes != null)
					return errorRes.Value;

				Debug.Assert(state.CompilationResult.CompiledExpressions.Length == 1);
				ref var exprInfo = ref state.CompilationResult.CompiledExpressions[0];

				if ((options & DbgEvaluationOptions.NoSideEffects) != 0 && (exprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0)
					return new EvaluateImplResult(PredefinedEvaluationErrorMessages.ExpressionCausesSideEffects, exprInfo.Name, null, exprInfo.Flags, exprInfo.ImageName, null);

				var res = dnILInterpreter.Execute(context, frame, state.ILInterpreterState, exprInfo.TypeName, exprInfo.MethodName, options, out var expectedType, cancellationToken);
				if (res.HasError)
					return new EvaluateImplResult(res.ErrorMessage, exprInfo.Name, null, exprInfo.Flags & ~DbgEvaluationResultFlags.SideEffects, exprInfo.ImageName, expectedType);
				if (res.ValueIsException)
					return new EvaluateImplResult(null, exprInfo.Name, res.Value, (exprInfo.Flags & ~DbgEvaluationResultFlags.SideEffects) | DbgEvaluationResultFlags.ThrownException, PredefinedDbgValueNodeImageNames.Error, expectedType);
				return new EvaluateImplResult(null, exprInfo.Name, res.Value, exprInfo.Flags, exprInfo.ImageName, expectedType);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new EvaluateImplResult(PredefinedEvaluationErrorMessages.InternalDebuggerError, DbgDotNetEngineValueNodeFactoryExtensions.errorName, null, DbgEvaluationResultFlags.None, PredefinedDbgValueNodeImageNames.Error, null);
			}
		}

		static DbgDotNetText CreateName(string expression) => new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Error, expression));

		DbgDotNetEvalResult IDebuggerDisplayAttributeEvaluator.Evaluate(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, string expression, DbgEvaluationOptions options, object state, CancellationToken cancellationToken) {
			var dispatcher = context.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return EvaluateCore(context, frame, obj, expression, options, state, cancellationToken);
			return Evaluate2(dispatcher, context, frame, obj, expression, options, state, cancellationToken);

			DbgDotNetEvalResult Evaluate2(DbgDotNetDispatcher dispatcher2, DbgEvaluationContext context2, DbgStackFrame frame2, DbgDotNetValue obj2, string expression2, DbgEvaluationOptions options2, object state2, CancellationToken cancellationToken2) =>
				dispatcher2.InvokeRethrow(() => EvaluateCore(context2, frame2, obj2, expression2, options2, state2, cancellationToken2));
		}

		DbgDotNetEvalResult EvaluateCore(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, string expression, DbgEvaluationOptions options, object state, CancellationToken cancellationToken) {
			var res = EvaluateImpl(context, frame, obj, expression, options, state, cancellationToken);
			if (res.Error != null)
				return new DbgDotNetEvalResult(predefinedEvaluationErrorMessagesHelper.GetErrorMessage(res.Error), res.Flags);
			return new DbgDotNetEvalResult(res.Value, res.Flags);
		}

		EvaluateImplResult EvaluateImpl(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValue obj, string expression, DbgEvaluationOptions options, object stateObj, CancellationToken cancellationToken) {
			try {
				var type = obj.Type;
				if (type.IsGenericType)
					type = type.GetGenericTypeDefinition();
				var errorRes = GetTypeInterpreterState(context, frame, type, expression, options | DbgEvaluationOptions.NoName, stateObj, cancellationToken, out var state);
				if (errorRes != null)
					return errorRes.Value;

				var genericTypeArguments = obj.Type.GetGenericArguments();
				var genericMethodArguments = Array.Empty<DmdType>();

				Debug.Assert(state.CompilationResult.CompiledExpressions.Length == 1);
				ref var exprInfo = ref state.CompilationResult.CompiledExpressions[0];

				if ((options & DbgEvaluationOptions.NoSideEffects) != 0 && (exprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0)
					return new EvaluateImplResult(PredefinedEvaluationErrorMessages.ExpressionCausesSideEffects, exprInfo.Name, null, exprInfo.Flags, exprInfo.ImageName, null);

				var argumentsProvider = new TypeArgumentsProvider(obj);
				var res = dnILInterpreter.Execute(context, frame, genericTypeArguments, genericMethodArguments, argumentsProvider, null, state.ILInterpreterState, exprInfo.TypeName, exprInfo.MethodName, options, out var expectedType, cancellationToken);
				if (res.HasError)
					return new EvaluateImplResult(res.ErrorMessage, exprInfo.Name, null, exprInfo.Flags & ~DbgEvaluationResultFlags.SideEffects, exprInfo.ImageName, expectedType);
				if (res.ValueIsException)
					return new EvaluateImplResult(null, exprInfo.Name, res.Value, exprInfo.Flags & ~DbgEvaluationResultFlags.SideEffects, PredefinedDbgValueNodeImageNames.Error, expectedType);
				return new EvaluateImplResult(null, exprInfo.Name, res.Value, exprInfo.Flags, exprInfo.ImageName, expectedType);
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new EvaluateImplResult(PredefinedEvaluationErrorMessages.InternalDebuggerError, DbgDotNetEngineValueNodeFactoryExtensions.errorName, null, DbgEvaluationResultFlags.None, PredefinedDbgValueNodeImageNames.Error, null);
			}
		}

		sealed class TypeArgumentsProvider : VariablesProvider {
			readonly DbgDotNetValue argument;

			public TypeArgumentsProvider(DbgDotNetValue argument) => this.argument = argument;

			public override void Initialize(DbgEvaluationContext context, DbgStackFrame frame, DmdMethodBase method, DmdMethodBody body, CancellationToken cancellationToken) { }

			public override DbgDotNetValue GetValueAddress(int index, DmdType targetType) => null;

			public override DbgDotNetValueResult GetVariable(int index) {
				if (index == 0)
					return new DbgDotNetValueResult(argument, valueIsException: false);
				return new DbgDotNetValueResult(PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}

			public override string SetVariable(int index, DmdType targetType, object value) => PredefinedEvaluationErrorMessages.InternalDebuggerError;
			public override bool CanDispose(DbgDotNetValue value) => value != argument;
			public override void Clear() { }
		}

		DbgDotNetAlias[] GetAliases(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			var runtime = context.Runtime.GetDotNetRuntime();
			var objectIds = objectIdService.GetObjectIds(context.Runtime);
			var aliases = runtime.GetAliases(context, frame, cancellationToken);

			if (objectIds.Length == 0 && aliases.Length == 0)
				return Array.Empty<DbgDotNetAlias>();

			var res = new DbgDotNetAlias[objectIds.Length + aliases.Length];

			var sb = ObjectCache.AllocStringBuilder();
			var output = new StringBuilderTextColorOutput(sb);
			int w = 0;
			foreach (var alias in aliases) {
				output.Reset();
				DbgDotNetAliasKind dnAliasKind;
				string aliasName;
				switch (alias.Kind) {
				case DbgDotNetAliasInfoKind.Exception:
					dnAliasKind = DbgDotNetAliasKind.Exception;
					context.Language.Formatter.FormatExceptionName(context, output, alias.Id);
					aliasName = sb.ToString();
					break;
				case DbgDotNetAliasInfoKind.StowedException:
					dnAliasKind = DbgDotNetAliasKind.StowedException;
					context.Language.Formatter.FormatStowedExceptionName(context, output, alias.Id);
					aliasName = sb.ToString();
					break;
				case DbgDotNetAliasInfoKind.ReturnValue:
					dnAliasKind = DbgDotNetAliasKind.ReturnValue;
					context.Language.Formatter.FormatReturnValueName(context, output, alias.Id);
					aliasName = sb.ToString();
					break;
				default:
					throw new InvalidOperationException();
				}
				res[w++] = new DbgDotNetAlias(dnAliasKind, alias.Type.AssemblyQualifiedName, aliasName, alias.CustomTypeInfoId, alias.CustomTypeInfo);
			}
			foreach (var objectId in objectIds) {
				output.Reset();
				var value = objectId.GetValue(context, frame, cancellationToken);
				var dnValue = (DbgDotNetValue)value.InternalValue;
				context.Language.Formatter.FormatObjectIdName(context, output, objectId.Id);
				res[w++] = new DbgDotNetAlias(DbgDotNetAliasKind.ObjectId, dnValue.Type.AssemblyQualifiedName, sb.ToString(), Guid.Empty, null);
				value.Close();
			}
			if (w != res.Length)
				throw new InvalidOperationException();
			ObjectCache.Free(ref sb);
			return res;
		}
	}

	struct EvaluateImplResult {
		public string Error;
		public DbgDotNetText Name;
		public DbgDotNetValue Value;
		public DbgEvaluationResultFlags Flags;
		public string ImageName;
		public DmdType Type;

		public EvaluateImplResult(string error, DbgDotNetText name, DbgDotNetValue value, DbgEvaluationResultFlags flags, string imageName, DmdType type) {
			Error = error;
			Name = name;
			Value = value;
			Flags = flags;
			ImageName = imageName;
			Type = type;
		}
	}
}
