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
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	abstract class DbgValueNodeReader {
		public abstract void SetEvaluationContext(DbgEvaluationContext context);
		public abstract DbgValueNode GetDebuggerNode(ChildDbgValueRawNode valueNode);
		public abstract DbgValueNode GetDebuggerNodeForReuse(DebuggerValueRawNode parent, uint startIndex);
		public abstract DbgCreateValueNodeResult Evaluate(string expression);
	}

	sealed class DbgValueNodeReaderImpl : DbgValueNodeReader {
		readonly Func<DbgEvaluationContext, string, DbgCreateValueNodeResult> evaluate;
		DbgEvaluationContext dbgEvaluationContext;

		public DbgValueNodeReaderImpl(Func<DbgEvaluationContext, string, DbgCreateValueNodeResult> evaluate) =>
			this.evaluate = evaluate ?? throw new ArgumentNullException(nameof(evaluate));

		public override void SetEvaluationContext(DbgEvaluationContext context) => dbgEvaluationContext = context;

		public override DbgValueNode GetDebuggerNode(ChildDbgValueRawNode valueNode) {
			Debug.Assert(dbgEvaluationContext != null);
			var parent = valueNode.Parent;
			uint startIndex = valueNode.DbgValueNodeChildIndex;
			const int count = 1;
			var newNodes = parent.DebuggerValueNode.GetChildren(dbgEvaluationContext, startIndex, count);
			Debug.Assert(count == 1);
			return newNodes[0];
		}

		public override DbgValueNode GetDebuggerNodeForReuse(DebuggerValueRawNode parent, uint startIndex) {
			Debug.Assert(dbgEvaluationContext != null);
			const int count = 1;
			var newNodes = parent.DebuggerValueNode.GetChildren(dbgEvaluationContext, startIndex, count);
			Debug.Assert(count == 1);
			return newNodes[0];
		}

		public override DbgCreateValueNodeResult Evaluate(string expression) {
			Debug.Assert(dbgEvaluationContext != null);
			var res = evaluate(dbgEvaluationContext, expression);
			res.ValueNode?.Runtime.CloseOnContinue(res.ValueNode);
			return res;
		}
	}
}
