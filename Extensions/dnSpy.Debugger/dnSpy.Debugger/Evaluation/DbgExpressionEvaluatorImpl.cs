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
using System.Diagnostics;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgExpressionEvaluatorImpl : DbgExpressionEvaluator {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeKindGuid;
		readonly DbgEngineExpressionEvaluator engineExpressionEvaluator;

		public DbgExpressionEvaluatorImpl(DbgLanguage language, Guid runtimeKindGuid, DbgEngineExpressionEvaluator engineExpressionEvaluator) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeKindGuid = runtimeKindGuid;
			this.engineExpressionEvaluator = engineExpressionEvaluator ?? throw new ArgumentNullException(nameof(engineExpressionEvaluator));
		}

		DbgEvaluationResult CreateResult(DbgRuntime runtime, DbgEngineEvaluationResult result) {
			if (!(result.Error is null))
				return new DbgEvaluationResult(PredefinedEvaluationErrorMessagesHelper.GetErrorMessage(result.Error), result.FormatSpecifiers, result.Flags);
			Debug2.Assert(!(result.Value is null));
			try {
				var value = new DbgValueImpl(runtime, result.Value);
				runtime.CloseOnContinue(value);
				return new DbgEvaluationResult(value, result.FormatSpecifiers, result.Flags);
			}
			catch {
				runtime.Process.DbgManager.Close(result.Value);
				throw;
			}
		}

		DbgEEAssignmentResult CreateResult(DbgEngineEEAssignmentResult result) => new DbgEEAssignmentResult(result.Flags, PredefinedEvaluationErrorMessagesHelper.GetErrorMessageOrNull(result.Error));

		public override object? CreateExpressionEvaluatorState() => engineExpressionEvaluator.CreateExpressionEvaluatorState();

		public override DbgEvaluationResult Evaluate(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options, object? state) {
			if (evalInfo is null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (expression is null)
				throw new ArgumentNullException(nameof(expression));
			Debug.Assert((evalInfo.Context.Options & DbgEvaluationContextOptions.NoMethodBody) == 0, "Missing method debug info");
			return CreateResult(evalInfo.Context.Runtime, engineExpressionEvaluator.Evaluate(evalInfo, expression, options, state));
		}

		public override DbgEEAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, string valueExpression, DbgEvaluationOptions options) {
			if (evalInfo is null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime.RuntimeKindGuid != runtimeKindGuid)
				throw new ArgumentException();
			if (expression is null)
				throw new ArgumentNullException(nameof(expression));
			if (valueExpression is null)
				throw new ArgumentNullException(nameof(valueExpression));
			var result = engineExpressionEvaluator.Assign(evalInfo, expression, valueExpression, options);
			return CreateResult(result);
		}
	}
}
