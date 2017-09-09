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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineValueNodeImpl : DbgEngineValueNode {
		public override string ErrorMessage => dnValueNode.ErrorMessage;
		public override DbgEngineValue Value => value;
		public override string Expression => dnValueNode.Expression;
		public override string ImageName => dnValueNode.ImageName;
		public override bool IsReadOnly => dnValueNode.IsReadOnly;
		public override bool CausesSideEffects => dnValueNode.CausesSideEffects;
		public override bool? HasChildren => dnValueNode.HasChildren;
		public override ulong ChildCount => dnValueNode.ChildCount;

		readonly DbgDotNetFormatter formatter;
		readonly DbgDotNetValueNode dnValueNode;
		readonly DbgEngineValueImpl value;

		public DbgEngineValueNodeImpl(DbgDotNetFormatter formatter, DbgDotNetValueNode dnValueNode) {
			if (dnValueNode == null)
				throw new ArgumentNullException(nameof(dnValueNode));
			this.formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
			var dnValue = dnValueNode.Value;
			value = dnValue == null ? null : new DbgEngineValueImpl(dnValue);
			this.dnValueNode = dnValueNode;
		}

		public override DbgEngineValueNode[] GetChildren(DbgEvaluationContext context, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			return Array.Empty<DbgEngineValueNode>();//TODO:
		}

		public override void GetChildren(DbgEvaluationContext context, ulong index, int count, DbgValueNodeEvaluationOptions options, Action<DbgEngineValueNode[]> callback, CancellationToken cancellationToken) {
			callback(Array.Empty<DbgEngineValueNode>());//TODO:
		}

		public override void Format(DbgEvaluationContext context, IDbgValueNodeFormatParameters options, CancellationToken cancellationToken) =>
			context.Runtime.GetDotNetRuntime().Dispatcher.Invoke(() => FormatCore(context, options, cancellationToken));

		public override void Format(DbgEvaluationContext context, IDbgValueNodeFormatParameters options, Action callback, CancellationToken cancellationToken) {
			context.Runtime.GetDotNetRuntime().Dispatcher.BeginInvoke(() => {
				FormatCore(context, options, cancellationToken);
				callback();
			});
		}

		void FormatCore(DbgEvaluationContext context, IDbgValueNodeFormatParameters options, CancellationToken cancellationToken) {
			context.Runtime.GetDotNetRuntime().Dispatcher.VerifyAccess();
			if (options.NameOutput != null)
				dnValueNode.Name.WriteTo(options.NameOutput);
			if (options.ExpectedTypeOutput != null && dnValueNode.ExpectedType is DmdType expectedType)
				formatter.FormatType(context, options.ExpectedTypeOutput, expectedType, options.ExpectedTypeFormatterOptions);
			if (options.ActualTypeOutput != null && value?.DotNetValue.Type is DmdType actualType)
				formatter.FormatType(context, options.ActualTypeOutput, actualType, options.ActualTypeFormatterOptions);
			if (options.ValueOutput != null && value?.DotNetValue is DbgDotNetValue dnValue)
				formatter.FormatValue(context, options.ValueOutput, dnValue, options.ValueFormatterOptions, cancellationToken);
		}

		public override DbgEngineValueNodeAssignmentResult Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			return new DbgEngineValueNodeAssignmentResult("NYI");//TODO:
		}

		public override void Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, Action<DbgEngineValueNodeAssignmentResult> callback, CancellationToken cancellationToken) {
			callback(new DbgEngineValueNodeAssignmentResult("NYI"));//TODO:
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			dnValueNode.Close(dispatcher);
			Value?.Close(dispatcher);
		}
	}
}
