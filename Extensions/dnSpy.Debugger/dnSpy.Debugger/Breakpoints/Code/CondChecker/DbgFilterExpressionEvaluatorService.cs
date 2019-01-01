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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger.Breakpoints.Code.FilterExpressionEvaluator;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.Breakpoints.Code.CondChecker {
	abstract class DbgFilterExpressionEvaluatorService {
		public abstract bool HasExpressionEvaluator { get; }
		public abstract string IsValidExpression(string expr);
		public abstract DbgFilterExpressionEvaluatorResult Evaluate(string expr, DbgFilterEEVariableProvider variableProvider);
		public abstract void Write(IDbgTextWriter output, string expr);
	}

	[Export(typeof(DbgFilterExpressionEvaluatorService))]
	sealed class DbgFilterExpressionEvaluatorServiceImpl : DbgFilterExpressionEvaluatorService {
		readonly Lazy<DbgFilterExpressionEvaluator, IDbgFilterExpressionEvaluatorMetadata> dbgFilterExpressionEvaluator;

		public override bool HasExpressionEvaluator => dbgFilterExpressionEvaluator != null;

		[ImportingConstructor]
		DbgFilterExpressionEvaluatorServiceImpl([ImportMany] IEnumerable<Lazy<DbgFilterExpressionEvaluator, IDbgFilterExpressionEvaluatorMetadata>> dbgFilterExpressionEvaluators) =>
			dbgFilterExpressionEvaluator = dbgFilterExpressionEvaluators.OrderBy(a => a.Metadata.Order).FirstOrDefault();

		const string NoFEEError = "No filter expression evaluator available";

		public override string IsValidExpression(string expr) {
			if (expr == null)
				throw new ArgumentNullException(nameof(expr));
			if (dbgFilterExpressionEvaluator != null)
				return dbgFilterExpressionEvaluator.Value.IsValidExpression(expr);
			return NoFEEError;
		}

		public override DbgFilterExpressionEvaluatorResult Evaluate(string expr, DbgFilterEEVariableProvider variableProvider) {
			if (expr == null)
				throw new ArgumentNullException(nameof(expr));
			if (variableProvider == null)
				throw new ArgumentNullException(nameof(variableProvider));
			return dbgFilterExpressionEvaluator?.Value.Evaluate(expr, variableProvider) ?? new DbgFilterExpressionEvaluatorResult(NoFEEError);
		}

		public override void Write(IDbgTextWriter output, string expr) {
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (expr == null)
				throw new ArgumentNullException(nameof(expr));
			if (dbgFilterExpressionEvaluator != null)
				dbgFilterExpressionEvaluator.Value.Write(output, expr);
			else
				output.Write(DbgTextColor.Error, expr);
		}
	}
}
