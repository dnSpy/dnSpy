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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation.Engine;
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.Evaluation {
	sealed class NullDbgEngineLanguage : DbgEngineLanguage {
		public static readonly DbgEngineLanguage Instance = new NullDbgEngineLanguage();

		public override string Name => PredefinedDbgLanguageNames.None;
		public override string DisplayName => "<no name>";
		public override DbgEngineExpressionEvaluator ExpressionEvaluator { get; }
		public override DbgEngineValueFormatter ValueFormatter { get; }
		public override DbgEngineValueNodeProvider LocalsProvider { get; }
		public override DbgEngineValueNodeProvider AutosProvider { get; }
		public override DbgEngineValueNodeFactory ValueNodeFactory { get; }

		NullDbgEngineLanguage() {
			ExpressionEvaluator = new NullDbgEngineExpressionEvaluator();
			ValueFormatter = new NullDbgEngineValueFormatter();
			LocalsProvider = new NullDbgEngineValueNodeProvider();
			AutosProvider = new NullDbgEngineValueNodeProvider();
			ValueNodeFactory = new NullDbgEngineValueNodeFactory();
		}

		public override void InitializeContext(DbgEvaluationContext context, DbgRuntime runtime, DbgCodeLocation location) { }
	}

	sealed class NullDbgEngineExpressionEvaluator : DbgEngineExpressionEvaluator {
		// No need to localize it, an EE should always be available
		const string ERROR = "No expression evaluator is available for this runtime";

		public override DbgEngineEvaluationResult Evaluate(DbgEvaluationContext context, string expression, DbgEvaluationOptions options) =>
			new DbgEngineEvaluationResult(ERROR);

		public override void Evaluate(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, Action<DbgEngineEvaluationResult> callback) =>
			callback(new DbgEngineEvaluationResult(ERROR));

		public override DbgEngineEEAssignmentResult Assign(DbgEvaluationContext context, string expression, string valueExpression, DbgEvaluationOptions options) =>
			new DbgEngineEEAssignmentResult(ERROR);

		public override void Assign(DbgEvaluationContext context, string expression, string valueExpression, DbgEvaluationOptions options, Action<DbgEngineEEAssignmentResult> callback) =>
			callback(new DbgEngineEEAssignmentResult(ERROR));
	}

	sealed class NullDbgEngineValueFormatter : DbgEngineValueFormatter {
		public override void Format(DbgEvaluationContext context, ITextColorWriter output, DbgEngineValue value, DbgValueFormatterOptions options) { }
		public override void Format(DbgEvaluationContext context, ITextColorWriter output, DbgEngineValue value, DbgValueFormatterOptions options, Action callback) => callback();
		public override void FormatType(DbgEvaluationContext context, ITextColorWriter output, DbgEngineValue value, DbgValueFormatterTypeOptions options) { }
		public override void FormatType(DbgEvaluationContext context, ITextColorWriter output, DbgEngineValue value, DbgValueFormatterTypeOptions options, Action callback) => callback();
	}

	sealed class NullDbgEngineValueNodeProvider : DbgEngineValueNodeProvider {
		public override DbgEngineValueNode[] GetNodes(DbgStackFrame frame) => Array.Empty<DbgEngineValueNode>();
		public override void GetNodes(DbgStackFrame frame, Action<DbgEngineValueNode[]> callback) => callback(Array.Empty<DbgEngineValueNode>());
	}

	sealed class NullDbgEngineValueNodeFactory : DbgEngineValueNodeFactory {
		public override DbgEngineValueNode Create(DbgStackFrame frame, string expression, DbgEvaluationOptions options) => null;
		public override void Create(DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgEngineValueNode> callback) => callback(Create(frame, expression, options));
	}
}
