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
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgExpressionEvaluatorImpl : DbgExpressionEvaluator {
		public override DbgLanguage Language { get; }

		readonly Guid runtimeGuid;
		readonly DbgEngineExpressionEvaluator engineExpressionEvaluator;

		public DbgExpressionEvaluatorImpl(DbgLanguage language, Guid runtimeGuid, DbgEngineExpressionEvaluator engineExpressionEvaluator) {
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.runtimeGuid = runtimeGuid;
			this.engineExpressionEvaluator = engineExpressionEvaluator ?? throw new ArgumentNullException(nameof(engineExpressionEvaluator));
		}

		DbgEvaluationResult CreateResult(DbgEngineEvaluationResult result) {
			if (result.Error != null)
				return new DbgEvaluationResult(result.Error);
			var runtime = result.Thread.Runtime;
			var value = new DbgValueImpl(runtime, result.Value);
			runtime.CloseOnContinue(value);
			return new DbgEvaluationResult(value, result.Flags);
		}

		public override DbgEvaluationResult Evaluate(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			return CreateResult(engineExpressionEvaluator.Evaluate(context, expression, options, cancellationToken));
		}

		public override void Evaluate(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, Action<DbgEvaluationResult> callback, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			engineExpressionEvaluator.Evaluate(context, expression, options, result => callback(CreateResult(result)), cancellationToken);
		}

		public override DbgEEAssignmentResult Assign(DbgEvaluationContext context, string expression, string valueExpression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			if (valueExpression == null)
				throw new ArgumentNullException(nameof(valueExpression));
			var result = engineExpressionEvaluator.Assign(context, expression, valueExpression, options, cancellationToken);
			return new DbgEEAssignmentResult(result.Error);
		}

		public override void Assign(DbgEvaluationContext context, string expression, string valueExpression, DbgEvaluationOptions options, Action<DbgEEAssignmentResult> callback, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime.Guid != runtimeGuid)
				throw new ArgumentException();
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			if (valueExpression == null)
				throw new ArgumentNullException(nameof(valueExpression));
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			engineExpressionEvaluator.Assign(context, expression, valueExpression, options, result => callback(new DbgEEAssignmentResult(result.Error)), cancellationToken);
		}
	}
}
