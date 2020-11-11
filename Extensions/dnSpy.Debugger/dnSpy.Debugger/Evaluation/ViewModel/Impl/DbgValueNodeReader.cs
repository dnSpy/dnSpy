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
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	abstract class DbgValueNodeReader {
		public abstract void SetEvaluationInfo(DbgEvaluationInfo? evalInfo);
		public abstract void SetValueNodeEvaluationOptions(DbgValueNodeEvaluationOptions options);
		public abstract DbgValueNode GetDebuggerNode(ChildDbgValueRawNode valueNode);
		public abstract DbgValueNode GetDebuggerNodeForReuse(DebuggerValueRawNode parent, uint startIndex);
		public abstract DbgValueNodeInfo Evaluate(string expression);
	}

	sealed class DbgValueNodeReaderImpl : DbgValueNodeReader {
		readonly Func<DbgEvaluationInfo, string, DbgValueNodeInfo> evaluate;
		DbgEvaluationInfo? evalInfo;
		DbgValueNodeEvaluationOptions dbgValueNodeEvaluationOptions;

		public DbgValueNodeReaderImpl(Func<DbgEvaluationInfo, string, DbgValueNodeInfo> evaluate) =>
			this.evaluate = evaluate ?? throw new ArgumentNullException(nameof(evaluate));

		public override void SetEvaluationInfo(DbgEvaluationInfo? evalInfo) => this.evalInfo = evalInfo;
		public override void SetValueNodeEvaluationOptions(DbgValueNodeEvaluationOptions options) => dbgValueNodeEvaluationOptions = options;

		public override DbgValueNode GetDebuggerNode(ChildDbgValueRawNode valueNode) {
			Debug2.Assert(evalInfo is not null);
			var parent = valueNode.Parent;
			uint startIndex = valueNode.DbgValueNodeChildIndex;
			const int count = 1;
			var newNodes = parent.DebuggerValueNode.GetChildren(evalInfo, startIndex, count, dbgValueNodeEvaluationOptions);
			Debug.Assert(count == 1);
			return newNodes[0];
		}

		public override DbgValueNode GetDebuggerNodeForReuse(DebuggerValueRawNode parent, uint startIndex) {
			Debug2.Assert(evalInfo is not null);
			const int count = 1;
			var newNodes = parent.DebuggerValueNode.GetChildren(evalInfo, startIndex, count, dbgValueNodeEvaluationOptions);
			Debug.Assert(count == 1);
			return newNodes[0];
		}

		public override DbgValueNodeInfo Evaluate(string expression) {
			Debug2.Assert(evalInfo is not null);
			return evaluate(evalInfo, expression);
		}
	}
}
