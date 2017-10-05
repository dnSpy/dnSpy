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
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

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
			var runtime = context.Runtime.GetDotNetRuntime();
			if (runtime.Dispatcher.CheckAccess())
				return AssignCore(context, frame, expression, valueExpression, options, cancellationToken);
			return runtime.Dispatcher.Invoke(() => AssignCore(context, frame, expression, valueExpression, options, cancellationToken));
		}

		public override void Assign(DbgEvaluationContext context, DbgStackFrame frame, string expression, string valueExpression, DbgEvaluationOptions options, Action<DbgEngineEEAssignmentResult> callback, CancellationToken cancellationToken) =>
			context.Runtime.GetDotNetRuntime().Dispatcher.BeginInvoke(() => callback(AssignCore(context, frame, expression, valueExpression, options, cancellationToken)));

		DbgEngineEEAssignmentResult AssignCore(DbgEvaluationContext context, DbgStackFrame frame, string expression, string valueExpression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			var resultFlags = DbgEEAssignmentResultFlags.None;
			try {
				var references = dbgModuleReferenceProvider.GetModuleReferences(context.Runtime, frame);
				if (references.Length == 0)
					return new DbgEngineEEAssignmentResult(resultFlags, PredefinedEvaluationErrorMessages.InternalDebuggerError);

				var aliases = Array.Empty<DbgDotNetAlias>();//TODO: Add all aliases
				var compRes = expressionCompiler.CompileAssignment(context, frame, references, aliases, expression, valueExpression, options, cancellationToken);
				cancellationToken.ThrowIfCancellationRequested();
				if (compRes.IsError)
					return new DbgEngineEEAssignmentResult(resultFlags | DbgEEAssignmentResultFlags.CompilerError, compRes.ErrorMessage);

				var state = dnILInterpreter.CreateState(context, compRes.Assembly);
				ref var exprInfo = ref compRes.CompiledExpressions[0];
				if (exprInfo.ErrorMessage != null)
					return new DbgEngineEEAssignmentResult(resultFlags | DbgEEAssignmentResultFlags.CompilerError, exprInfo.ErrorMessage);
				resultFlags |= DbgEEAssignmentResultFlags.ExecutedCode;
				var res = dnILInterpreter.Execute(context, frame, state, exprInfo.TypeName, exprInfo.MethodName, ToValueNodeEvaluationOptions(options), out _, cancellationToken);
				if (res.HasError)
					return new DbgEngineEEAssignmentResult(resultFlags, res.ErrorMessage);
				if (res.ValueIsException) {
					res.Value?.Dispose();
					return new DbgEngineEEAssignmentResult(resultFlags, PredefinedEvaluationErrorMessages.InternalDebuggerError);
				}

				res.Value?.Dispose();
				return new DbgEngineEEAssignmentResult();
			}
			catch (Exception ex) when (ExceptionUtils.IsInternalDebuggerError(ex)) {
				return new DbgEngineEEAssignmentResult(resultFlags, PredefinedEvaluationErrorMessages.InternalDebuggerError);
			}
		}

		static DbgValueNodeEvaluationOptions ToValueNodeEvaluationOptions(DbgEvaluationOptions options) {
			var res = DbgValueNodeEvaluationOptions.None;
			if ((options & DbgEvaluationOptions.NoFuncEval) != 0)
				res |= DbgValueNodeEvaluationOptions.NoFuncEval;
			if ((options & DbgEvaluationOptions.RawView) != 0)
				res |= DbgValueNodeEvaluationOptions.RawView;
			if ((options & DbgEvaluationOptions.HideCompilerGeneratedMembers) != 0)
				res |= DbgValueNodeEvaluationOptions.HideCompilerGeneratedMembers;
			if ((options & DbgEvaluationOptions.RespectHideMemberAttributes) != 0)
				res |= DbgValueNodeEvaluationOptions.RespectHideMemberAttributes;
			if ((options & DbgEvaluationOptions.PublicMembers) != 0)
				res |= DbgValueNodeEvaluationOptions.PublicMembers;
			if ((options & DbgEvaluationOptions.NoHideRoots) != 0)
				res |= DbgValueNodeEvaluationOptions.NoHideRoots;
			return res;
		}

		public override DbgEngineEvaluationResult Evaluate(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			throw new NotImplementedException();//TODO:
		}

		public override void Evaluate(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgEngineEvaluationResult> callback, CancellationToken cancellationToken) {
			throw new NotImplementedException();//TODO:
		}
	}
}
