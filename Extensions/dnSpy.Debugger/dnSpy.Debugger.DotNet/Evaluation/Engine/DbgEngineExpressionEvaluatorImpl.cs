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
using System.Diagnostics;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Debugger.DotNet.Properties;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineExpressionEvaluatorImpl : DbgEngineExpressionEvaluator {
		readonly DbgModuleReferenceProvider dbgModuleReferenceProvider;
		readonly DbgDotNetExpressionCompiler expressionCompiler;
		readonly DbgDotNetILInterpreter dnILInterpreter;

		public DbgEngineExpressionEvaluatorImpl(DbgModuleReferenceProvider dbgModuleReferenceProvider, DbgDotNetExpressionCompiler expressionCompiler, DbgDotNetILInterpreter dnILInterpreter) {
			this.dbgModuleReferenceProvider = dbgModuleReferenceProvider ?? throw new ArgumentNullException(nameof(dbgModuleReferenceProvider));
			this.expressionCompiler = expressionCompiler ?? throw new ArgumentNullException(nameof(expressionCompiler));
			this.dnILInterpreter = dnILInterpreter ?? throw new ArgumentNullException(nameof(dnILInterpreter));
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

				var state = dnILInterpreter.CreateState(context, compRes.Assembly);
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

		public override DbgEngineEvaluationResult Evaluate(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			var dispatcher = context.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return EvaluateCore(context, frame, expression, options, cancellationToken);
			return Evaluate(dispatcher, context, frame, expression, options, cancellationToken);

			DbgEngineEvaluationResult Evaluate(DbgDotNetDispatcher dispatcher2, DbgEvaluationContext context2, DbgStackFrame frame2, string expression2, DbgEvaluationOptions options2, CancellationToken cancellationToken2) =>
				dispatcher2.InvokeRethrow(() => EvaluateCore(context2, frame2, expression2, options2, cancellationToken2));
		}

		DbgEngineEvaluationResult EvaluateCore(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			var res = EvaluateImpl(context, frame, expression, options, cancellationToken);
			if (res.Error != null)
				return new DbgEngineEvaluationResult(res.Error, res.Flags);
			try {
				return new DbgEngineEvaluationResult(frame.Thread, new DbgEngineValueImpl(res.Value), res.Flags);
			}
			catch {
				res.Value.Dispose();
				throw;
			}
		}

		sealed class EvaluateImplExpressionState {
			public readonly DbgDotNetILInterpreterState ILInterpreterState;
			public /*readonly*/ DbgDotNetCompiledExpressionResult CompiledExpressionResult;
			public EvaluateImplExpressionState(DbgDotNetILInterpreterState ilInterpreterState, DbgDotNetCompiledExpressionResult compiledExpressionResult) {
				ILInterpreterState = ilInterpreterState;
				CompiledExpressionResult = compiledExpressionResult;
			}
		}

		EvaluateImplResult? GetInterpreterState(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken, out EvaluateImplExpressionState state) {
			state = null;
			var refsResult = dbgModuleReferenceProvider.GetModuleReferences(context.Runtime, frame);
			if (refsResult.ErrorMessage != null)
				return new EvaluateImplResult(refsResult.ErrorMessage, CreateName(expression), null, 0, PredefinedDbgValueNodeImageNames.Error, null);

			var aliases = GetAliases(context, frame, cancellationToken);
			var compRes = expressionCompiler.CompileExpression(context, frame, refsResult.ModuleReferences, aliases, expression, options, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			if (compRes.IsError)
				return new EvaluateImplResult(compRes.ErrorMessage, CreateName(expression), null, 0, PredefinedDbgValueNodeImageNames.Error, null);
			Debug.Assert(compRes.CompiledExpressions.Length == 1);
			if (compRes.CompiledExpressions.Length != 1)
				return new EvaluateImplResult(PredefinedEvaluationErrorMessages.InternalDebuggerError, CreateName(expression), null, 0, PredefinedDbgValueNodeImageNames.Error, null);
			var exprInfo = compRes.CompiledExpressions[0];
			if (exprInfo.ErrorMessage != null)
				return new EvaluateImplResult(exprInfo.ErrorMessage, exprInfo.Name, null, exprInfo.Flags & ~DbgEvaluationResultFlags.SideEffects, exprInfo.ImageName, null);

			if ((options & DbgEvaluationOptions.NoSideEffects) != 0 && (exprInfo.Flags & DbgEvaluationResultFlags.SideEffects) != 0)
				return new EvaluateImplResult(PredefinedEvaluationErrorMessages.ExpressionCausesSideEffects, exprInfo.Name, null, exprInfo.Flags, exprInfo.ImageName, null);

			var ilState = dnILInterpreter.CreateState(context, compRes.Assembly);
			state = new EvaluateImplExpressionState(ilState, exprInfo);
			return null;
		}

		internal EvaluateImplResult EvaluateImpl(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			try {
				var errorRes = GetInterpreterState(context, frame, expression, options, cancellationToken, out var state);
				if (errorRes != null)
					return errorRes.Value;

				ref var exprInfo = ref state.CompiledExpressionResult;
				var res = dnILInterpreter.Execute(context, frame, state.ILInterpreterState, exprInfo.TypeName, exprInfo.MethodName, options, out var expectedType, cancellationToken);
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

		static DbgDotNetText CreateName(string expression) => new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Error, expression));

		DbgDotNetAlias[] GetAliases(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			return Array.Empty<DbgDotNetAlias>();//TODO:
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
