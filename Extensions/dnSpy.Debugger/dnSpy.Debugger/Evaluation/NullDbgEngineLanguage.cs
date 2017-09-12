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
using dnSpy.Contracts.Debugger;
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
		public override DbgEngineFormatter Formatter { get; }
		public override DbgEngineLocalsValueNodeProvider LocalsProvider { get; }
		public override DbgEngineValueNodeProvider AutosProvider { get; }
		public override DbgEngineValueNodeProvider ExceptionsProvider { get; }
		public override DbgEngineValueNodeProvider ReturnValuesProvider { get; }
		public override DbgEngineValueNodeFactory ValueNodeFactory { get; }

		NullDbgEngineLanguage() {
			ExpressionEvaluator = new NullDbgEngineExpressionEvaluator();
			ValueFormatter = new NullDbgEngineValueFormatter();
			Formatter = new NullDbgEngineEngineFormatter();
			LocalsProvider = new NullDbgEngineLocalsValueNodeProvider();
			AutosProvider = new NullDbgEngineValueNodeProvider();
			ExceptionsProvider = new NullDbgEngineValueNodeProvider();
			ReturnValuesProvider = new NullDbgEngineValueNodeProvider();
			ValueNodeFactory = new NullDbgEngineValueNodeFactory();
		}

		public override void InitializeContext(DbgEvaluationContext context, DbgCodeLocation location, CancellationToken cancellationToken) { }
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

	sealed class NullDbgEngineEngineFormatter : DbgEngineFormatter {
		public override void FormatExceptionName(DbgEvaluationContext context, ITextColorWriter output, uint id) { }
		public override void FormatStowedExceptionName(DbgEvaluationContext context, ITextColorWriter output, uint id) { }
		public override void FormatReturnValueName(DbgEvaluationContext context, ITextColorWriter output, uint id, bool isDefault) { }
		public override void FormatObjectIdName(DbgEvaluationContext context, ITextColorWriter output, uint id) { }
	}

	sealed class NullDbgEngineLocalsValueNodeProvider : DbgEngineLocalsValueNodeProvider {
		public override DbgEngineLocalsValueNodeInfo[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, DbgLocalsValueNodeEvaluationOptions localsOptions, CancellationToken cancellationToken) => Array.Empty<DbgEngineLocalsValueNodeInfo>();
		public override void GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, DbgLocalsValueNodeEvaluationOptions localsOptions, Action<DbgEngineLocalsValueNodeInfo[]> callback, CancellationToken cancellationToken) => callback(Array.Empty<DbgEngineLocalsValueNodeInfo>());
	}

	sealed class NullDbgEngineValueNodeProvider : DbgEngineValueNodeProvider {
		public override DbgEngineValueNode[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) => Array.Empty<DbgEngineValueNode>();
		public override void GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, Action<DbgEngineValueNode[]> callback, CancellationToken cancellationToken) => callback(Array.Empty<DbgEngineValueNode>());
	}

	sealed class NullDbgEngineValueNodeFactory : DbgEngineValueNodeFactory {
		public override DbgEngineValueNode Create(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) => new NullDbgEngineErrorValueNode(expression);
		public override void Create(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgEngineValueNode> callback, CancellationToken cancellationToken) => callback(Create(context, frame, expression, options, cancellationToken));
		public override DbgEngineValueNode[] Create(DbgEvaluationContext context, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) => objectIds.Select(a => new NullDbgEngineErrorValueNode()).ToArray();
		public override void Create(DbgEvaluationContext context, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options, Action<DbgEngineValueNode[]> callback, CancellationToken cancellationToken) => callback(Create(context, objectIds, options, cancellationToken));
	}

	sealed class NullDbgEngineErrorValueNode : DbgEngineValueNode {
		public override string ErrorMessage => NullDbgEngineExpressionEvaluator.ERROR;
		public override DbgEngineValue Value => null;
		public override string Expression { get; }
		public override string ImageName => PredefinedDbgValueNodeImageNames.Error;
		public override bool IsReadOnly => true;
		public override bool CausesSideEffects => false;
		public override bool? HasChildren => false;
		public override ulong ChildCount => 0;
		public NullDbgEngineErrorValueNode(string expression = null) => Expression = expression ?? string.Empty;
		public override DbgEngineValueNode[] GetChildren(DbgEvaluationContext context, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) => Array.Empty<DbgEngineValueNode>();
		public override void GetChildren(DbgEvaluationContext context, ulong index, int count, DbgValueNodeEvaluationOptions options, Action<DbgEngineValueNode[]> callback, CancellationToken cancellationToken) => callback(GetChildren(context, index, count, options, cancellationToken));
		public override void Format(DbgEvaluationContext context, IDbgValueNodeFormatParameters options, CancellationToken cancellationToken) { }
		public override void Format(DbgEvaluationContext context, IDbgValueNodeFormatParameters options, Action callback, CancellationToken cancellationToken) { }
		public override DbgEngineValueNodeAssignmentResult Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) => new DbgEngineValueNodeAssignmentResult(NullDbgEngineExpressionEvaluator.ERROR);
		public override void Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, Action<DbgEngineValueNodeAssignmentResult> callback, CancellationToken cancellationToken) => callback(Assign(context, expression, options, cancellationToken));
		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
