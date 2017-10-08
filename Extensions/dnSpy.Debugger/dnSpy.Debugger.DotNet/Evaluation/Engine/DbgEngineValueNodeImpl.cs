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
using System.Globalization;
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class DbgEngineValueNodeImpl : DbgEngineValueNode {
		public override string ErrorMessage => dnValueNode.ErrorMessage;
		public override DbgEngineValue Value => value;
		public override string Expression => dnValueNode.Expression;
		public override string ImageName => dnValueNode.ImageName;
		public override bool IsReadOnly => value == null || dnValueNode.IsReadOnly;
		public override bool CausesSideEffects => dnValueNode.CausesSideEffects;
		public override bool? HasChildren => dnValueNode.HasChildren;

		readonly DbgDotNetEngineValueNodeFactoryImpl owner;
		readonly DbgDotNetValueNode dnValueNode;
		readonly DbgEngineValueImpl value;

		public DbgEngineValueNodeImpl(DbgDotNetEngineValueNodeFactoryImpl owner, DbgDotNetValueNode dnValueNode) {
			if (dnValueNode == null)
				throw new ArgumentNullException(nameof(dnValueNode));
			this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
			var dnValue = dnValueNode.Value;
			value = dnValue == null ? null : new DbgEngineValueImpl(dnValue);
			this.dnValueNode = dnValueNode;
		}

		public override ulong GetChildCount(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			var dispatcher = context.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return GetChildCountCore(context, frame, cancellationToken);
			return dispatcher.Invoke(() => GetChildCountCore(context, frame, cancellationToken));
		}

		ulong GetChildCountCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) =>
			dnValueNode.GetChildCount(context, frame, cancellationToken);

		public override DbgEngineValueNode[] GetChildren(DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			var dispatcher = context.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return GetChildrenCore(context, frame, index, count, options, cancellationToken);
			return dispatcher.Invoke(() => GetChildrenCore(context, frame, index, count, options, cancellationToken));
		}

		DbgEngineValueNode[] GetChildrenCore(DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			DbgEngineValueNode[] res = null;
			DbgDotNetValueNode[] dnNodes = null;
			try {
				dnNodes = dnValueNode.GetChildren(context, frame, index, count, options, cancellationToken);
				res = new DbgEngineValueNode[dnNodes.Length];
				for (int i = 0; i < res.Length; i++) {
					cancellationToken.ThrowIfCancellationRequested();
					res[i] = owner.Create(dnNodes[i]);
				}
			}
			catch {
				if (res != null)
					context.Runtime.Process.DbgManager.Close(res.Where(a => a != null));
				if (dnNodes != null)
					context.Runtime.Process.DbgManager.Close(dnNodes);
				throw;
			}
			return res;
		}

		public override void Format(DbgEvaluationContext context, DbgStackFrame frame, IDbgValueNodeFormatParameters options, CultureInfo cultureInfo, CancellationToken cancellationToken) {
			var dispatcher = context.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				FormatCore(context, frame, options, cultureInfo, cancellationToken);
			else
				dispatcher.Invoke(() => FormatCore(context, frame, options, cultureInfo, cancellationToken));
		}

		void FormatCore(DbgEvaluationContext context, DbgStackFrame frame, IDbgValueNodeFormatParameters options, CultureInfo cultureInfo, CancellationToken cancellationToken) {
			context.Runtime.GetDotNetRuntime().Dispatcher.VerifyAccess();
			if (options.NameOutput != null)
				dnValueNode.Name.WriteTo(options.NameOutput);
			var formatter = owner.Formatter;
			var dnValue = value?.DotNetValue;
			if (options.ExpectedTypeOutput != null) {
				if (dnValueNode.FormatExpectedType(context, frame, options.ExpectedTypeOutput, cultureInfo, cancellationToken)) {
					// Nothing
				}
				else if (dnValueNode.ExpectedType is DmdType expectedType)
					formatter.FormatType(context, options.ExpectedTypeOutput, expectedType, null, options.ExpectedTypeFormatterOptions, cultureInfo);
				cancellationToken.ThrowIfCancellationRequested();
			}
			if (options.ActualTypeOutput != null) {
				if (dnValueNode.FormatActualType(context, frame, options.ActualTypeOutput, cultureInfo, cancellationToken)) {
					// Nothing
				}
				else if (dnValueNode.ActualType is DmdType actualType)
					formatter.FormatType(context, options.ActualTypeOutput, actualType, dnValue, options.ActualTypeFormatterOptions, cultureInfo);
				cancellationToken.ThrowIfCancellationRequested();
			}
			if (options.ValueOutput != null) {
				if (dnValueNode.FormatValue(context, frame, options.ValueOutput, cultureInfo, cancellationToken)) {
					// Nothing
				}
				else if (dnValue != null)
					formatter.FormatValue(context, options.ValueOutput, frame, dnValue, options.ValueFormatterOptions, cultureInfo, cancellationToken);
				else if (ErrorMessage is string errorMessage)
					options.ValueOutput.Write(BoxedTextColor.Error, owner.ErrorMessagesHelper.GetErrorMessage(errorMessage));
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		public override DbgEngineValueNodeAssignmentResult Assign(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			var dispatcher = context.Runtime.GetDotNetRuntime().Dispatcher;
			if (dispatcher.CheckAccess())
				return AssignCore(context, frame, expression, options, cancellationToken);
			return dispatcher.Invoke(() => AssignCore(context, frame, expression, options, cancellationToken));
		}

		DbgEngineValueNodeAssignmentResult AssignCore(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			var ee = context.Language.ExpressionEvaluator;
			var res = ee.Assign(context, frame, Expression, expression, options, cancellationToken);
			return new DbgEngineValueNodeAssignmentResult(res.Flags, res.Error);
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			dnValueNode.Close(dispatcher);
			Value?.Close(dispatcher);
		}
	}
}
