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
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineValueNodeImpl : DbgEngineValueNode {
		public override string ErrorMessage => dnValueNode.ErrorMessage;
		public override DbgEngineValue Value { get; }
		public override string Expression => dnValueNode.Expression;
		public override string ImageName => dnValueNode.ImageName;
		public override bool IsReadOnly => dnValueNode.IsReadOnly;
		public override bool CausesSideEffects => dnValueNode.CausesSideEffects;
		public override bool? HasChildren => dnValueNode.HasChildren;
		public override ulong ChildCount => dnValueNode.ChildCount;

		readonly DbgDotNetValueNode dnValueNode;

		public DbgEngineValueNodeImpl(DbgDotNetValueNode dnValueNode) {
			if (dnValueNode == null)
				throw new ArgumentNullException(nameof(dnValueNode));
			var dnValue = dnValueNode.Value;
			Value = dnValue == null ? null : new DbgEngineValueImpl(dnValue);
			this.dnValueNode = dnValueNode;
		}

		public override DbgEngineValueNode[] GetChildren(DbgEvaluationContext context, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			return Array.Empty<DbgEngineValueNode>();//TODO:
		}

		public override void GetChildren(DbgEvaluationContext context, ulong index, int count, DbgValueNodeEvaluationOptions options, Action<DbgEngineValueNode[]> callback, CancellationToken cancellationToken) {
			callback(Array.Empty<DbgEngineValueNode>());//TODO:
		}

		public override void Format(DbgEvaluationContext context, IDbgValueNodeFormatParameters options, CancellationToken cancellationToken) {
			//TODO:
		}

		public override void Format(DbgEvaluationContext context, IDbgValueNodeFormatParameters options, Action callback, CancellationToken cancellationToken) {
			callback();//TODO:
		}

		public override DbgEngineValueNodeAssignmentResult Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			return new DbgEngineValueNodeAssignmentResult("NYI");//TODO:
		}

		public override void Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, Action<DbgEngineValueNodeAssignmentResult> callback, CancellationToken cancellationToken) {
			callback(new DbgEngineValueNodeAssignmentResult("NYI"));//TODO:
		}

		protected override void CloseCore() {
			//TODO: Close Value (if not null), dnValueNode
		}
	}
}
