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
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.Evaluation {
	sealed class NullDbgEngineLanguage : DbgEngineLanguage {
		public static readonly DbgEngineLanguage Instance = new NullDbgEngineLanguage();

		public override string Name => PredefinedDbgLanguageNames.None;
		public override string DisplayName => "<no name>";
		public override DbgEngineExpressionEvaluator ExpressionEvaluator { get; }
		public override DbgEngineValueFormatter ValueFormatter { get; }
		public override DbgEngineObjectIdFormatter ObjectIdFormatter { get; }
		public override DbgEngineValueNodeProvider LocalsProvider { get; }
		public override DbgEngineValueNodeProvider AutosProvider { get; }
		public override DbgEngineValueNodeProvider ExceptionsProvider { get; }
		public override DbgEngineValueNodeProvider ReturnValuesProvider { get; }
		public override DbgEngineValueNodeFactory ValueNodeFactory { get; }

		NullDbgEngineLanguage() {
			ExpressionEvaluator = new NullDbgEngineExpressionEvaluator();
			ValueFormatter = new NullDbgEngineValueFormatter();
			ObjectIdFormatter = new NullDbgEngineObjectIdFormatter();
			LocalsProvider = new NullDbgEngineValueNodeProvider();
			AutosProvider = new NullDbgEngineValueNodeProvider();
			ExceptionsProvider = new NullDbgEngineValueNodeProvider();
			ReturnValuesProvider = new NullDbgEngineValueNodeProvider();
			ValueNodeFactory = new NullDbgEngineValueNodeFactory();
		}

		public override void InitializeContext(DbgEvaluationContext context, DbgCodeLocation location) { }
	}

	sealed class NullDbgEngineExpressionEvaluator : DbgEngineExpressionEvaluator {
		// No need to localize it, an EE should always be available
		public const string ERROR = "No expression evaluator is available for this runtime";
		public override DbgEngineEvaluationResult Evaluate(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) => new DbgEngineEvaluationResult(ERROR);
		public override void Evaluate(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, Action<DbgEngineEvaluationResult> callback, CancellationToken cancellationToken) => callback(new DbgEngineEvaluationResult(ERROR));
		public override DbgEngineEEAssignmentResult Assign(DbgEvaluationContext context, string expression, string valueExpression, DbgEvaluationOptions options, CancellationToken cancellationToken) => new DbgEngineEEAssignmentResult(ERROR);
		public override void Assign(DbgEvaluationContext context, string expression, string valueExpression, DbgEvaluationOptions options, Action<DbgEngineEEAssignmentResult> callback, CancellationToken cancellationToken) => callback(new DbgEngineEEAssignmentResult(ERROR));
	}

	sealed class NullDbgEngineValueFormatter : DbgEngineValueFormatter {
		public override void Format(DbgEvaluationContext context, ITextColorWriter output, DbgEngineValue value, DbgValueFormatterOptions options, CancellationToken cancellationToken) { }
		public override void Format(DbgEvaluationContext context, ITextColorWriter output, DbgEngineValue value, DbgValueFormatterOptions options, Action callback, CancellationToken cancellationToken) => callback();
		public override void FormatType(DbgEvaluationContext context, ITextColorWriter output, DbgEngineValue value, DbgValueFormatterTypeOptions options, CancellationToken cancellationToken) { }
		public override void FormatType(DbgEvaluationContext context, ITextColorWriter output, DbgEngineValue value, DbgValueFormatterTypeOptions options, Action callback, CancellationToken cancellationToken) => callback();
	}

	sealed class NullDbgEngineObjectIdFormatter : DbgEngineObjectIdFormatter {
		public override void FormatName(DbgEvaluationContext context, ITextColorWriter output, DbgEngineObjectId objectId) { }
	}

	sealed class NullDbgEngineValueNodeProvider : DbgEngineValueNodeProvider {
		public override DbgBaseEngineValueNode[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) => Array.Empty<DbgBaseEngineValueNode>();
		public override void GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, Action<DbgBaseEngineValueNode[]> callback, CancellationToken cancellationToken) => callback(Array.Empty<DbgBaseEngineValueNode>());
	}

	sealed class NullDbgEngineValueNodeFactory : DbgEngineValueNodeFactory {
		public override DbgBaseEngineValueNode Create(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) => new NullDbgEngineErrorValueNode(expression);
		public override void Create(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgBaseEngineValueNode> callback, CancellationToken cancellationToken) => callback(Create(context, frame, expression, options, cancellationToken));
		public override DbgBaseEngineValueNode[] Create(DbgEvaluationContext context, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) => objectIds.Select(a => new NullDbgEngineErrorValueNode()).ToArray();
		public override void Create(DbgEvaluationContext context, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options, Action<DbgBaseEngineValueNode[]> callback, CancellationToken cancellationToken) => callback(Create(context, objectIds, options, cancellationToken));
	}

	sealed class NullDbgEngineErrorValueNode : DbgEngineErrorValueNode {
		public override string ErrorMessage => NullDbgEngineExpressionEvaluator.ERROR;
		public override string Expression { get; }
		public NullDbgEngineErrorValueNode(string expression = null) => Expression = expression ?? string.Empty;
		public override void FormatName(DbgEvaluationContext context, ITextColorWriter output, CancellationToken cancellationToken) { }
		public override void FormatName(DbgEvaluationContext context, ITextColorWriter output, Action callback, CancellationToken cancellationToken) => callback();
		protected override void CloseCore() { }
	}
}
