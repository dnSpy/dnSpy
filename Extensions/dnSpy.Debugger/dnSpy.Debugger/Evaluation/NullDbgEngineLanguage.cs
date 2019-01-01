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
using System.Globalization;
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.Evaluation {
	sealed class NullDbgEngineLanguage : DbgEngineLanguage {
		public static readonly DbgEngineLanguage Instance = new NullDbgEngineLanguage();

		public override string Name => PredefinedDbgLanguageNames.None;
		public override string DisplayName => "<no name>";
		public override DbgEngineExpressionEvaluator ExpressionEvaluator { get; }
		public override DbgEngineFormatter Formatter { get; }
		public override DbgEngineLocalsValueNodeProvider LocalsProvider { get; }
		public override DbgEngineValueNodeProvider AutosProvider { get; }
		public override DbgEngineValueNodeProvider ExceptionsProvider { get; }
		public override DbgEngineValueNodeProvider ReturnValuesProvider { get; }
		public override DbgEngineValueNodeProvider TypeVariablesProvider { get; }
		public override DbgEngineValueNodeFactory ValueNodeFactory { get; }

		NullDbgEngineLanguage() {
			ExpressionEvaluator = new NullDbgEngineExpressionEvaluator();
			Formatter = new NullDbgEngineEngineFormatter();
			LocalsProvider = new NullDbgEngineLocalsValueNodeProvider();
			AutosProvider = new NullDbgEngineValueNodeProvider();
			ExceptionsProvider = new NullDbgEngineValueNodeProvider();
			ReturnValuesProvider = new NullDbgEngineValueNodeProvider();
			TypeVariablesProvider = new NullDbgEngineValueNodeProvider();
			ValueNodeFactory = new NullDbgEngineValueNodeFactory();
		}

		public override void InitializeContext(DbgEvaluationContext context, DbgCodeLocation location, CancellationToken cancellationToken) { }
	}

	sealed class NullDbgEngineExpressionEvaluator : DbgEngineExpressionEvaluator {
		// No need to localize it, an EE should always be available
		public const string ERROR = "No expression evaluator is available for this runtime";
		public override object CreateExpressionEvaluatorState() => null;
		public override DbgEngineEvaluationResult Evaluate(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options, object state) => new DbgEngineEvaluationResult(ERROR);
		public override DbgEngineEEAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, string valueExpression, DbgEvaluationOptions options) => new DbgEngineEEAssignmentResult(DbgEEAssignmentResultFlags.CompilerError, ERROR);
	}

	sealed class NullDbgEngineEngineFormatter : DbgEngineFormatter {
		public override void FormatExceptionName(DbgEvaluationContext context, IDbgTextWriter output, uint id) { }
		public override void FormatStowedExceptionName(DbgEvaluationContext context, IDbgTextWriter output, uint id) { }
		public override void FormatReturnValueName(DbgEvaluationContext context, IDbgTextWriter output, uint id) { }
		public override void FormatObjectIdName(DbgEvaluationContext context, IDbgTextWriter output, uint id) { }
		public override void FormatFrame(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgStackFrameFormatterOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo) { }
		public override void FormatValue(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgEngineValue value, DbgValueFormatterOptions options, CultureInfo cultureInfo) { }
		public override void FormatType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgEngineValue value, DbgValueFormatterTypeOptions options, CultureInfo cultureInfo) { }
	}

	sealed class NullDbgEngineLocalsValueNodeProvider : DbgEngineLocalsValueNodeProvider {
		public override DbgEngineLocalsValueNodeInfo[] GetNodes(DbgEvaluationInfo evalInfo, DbgValueNodeEvaluationOptions options, DbgLocalsValueNodeEvaluationOptions localsOptions) => Array.Empty<DbgEngineLocalsValueNodeInfo>();
	}

	sealed class NullDbgEngineValueNodeProvider : DbgEngineValueNodeProvider {
		public override DbgEngineValueNode[] GetNodes(DbgEvaluationInfo evalInfo, DbgValueNodeEvaluationOptions options) => Array.Empty<DbgEngineValueNode>();
	}

	sealed class NullDbgEngineValueNodeFactory : DbgEngineValueNodeFactory {
		public override DbgEngineValueNode[] Create(DbgEvaluationInfo evalInfo, DbgExpressionEvaluationInfo[] expressions) => expressions.Select(a => new NullDbgEngineErrorValueNode(a.Expression)).ToArray();
		public override DbgEngineValueNode[] Create(DbgEvaluationInfo evalInfo, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options) => objectIds.Select(a => new NullDbgEngineErrorValueNode()).ToArray();
	}

	sealed class NullDbgEngineErrorValueNode : DbgEngineValueNode {
		public override string ErrorMessage => NullDbgEngineExpressionEvaluator.ERROR;
		public override DbgEngineValue Value => null;
		public override string Expression { get; }
		public override string ImageName => PredefinedDbgValueNodeImageNames.Error;
		public override bool IsReadOnly => true;
		public override bool CausesSideEffects => false;
		public override bool? HasChildren => false;
		public NullDbgEngineErrorValueNode(string expression = null) => Expression = expression ?? string.Empty;
		public override ulong GetChildCount(DbgEvaluationInfo evalInfo) => 0;
		public override DbgEngineValueNode[] GetChildren(DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options) => Array.Empty<DbgEngineValueNode>();
		public override void Format(DbgEvaluationInfo evalInfo, IDbgValueNodeFormatParameters options, CultureInfo cultureInfo) { }
		public override DbgEngineValueNodeAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options) => new DbgEngineValueNodeAssignmentResult(DbgEEAssignmentResultFlags.None, NullDbgEngineExpressionEvaluator.ERROR);
		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
