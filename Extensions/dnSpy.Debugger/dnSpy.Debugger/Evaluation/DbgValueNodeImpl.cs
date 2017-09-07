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
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgValueNodeImpl : DbgValueNode {
		public override DbgLanguage Language { get; }
		public override DbgRuntime Runtime { get; }
		public override string ErrorMessage => PredefinedEvaluationErrorMessagesHelper.GetErrorMessage(engineValueNode.ErrorMessage);
		public override DbgValue Value => value;
		public override bool CanEvaluateExpression => value != null;
		public override string Expression => engineValueNode.Expression;
		public override string ImageName => engineValueNode.ImageName;
		public override bool IsReadOnly => engineValueNode.IsReadOnly;
		public override bool CausesSideEffects => engineValueNode.CausesSideEffects;
		public override bool? HasChildren => engineValueNode.HasChildren;
		public override ulong ChildCount => engineValueNode.ChildCount;

		readonly DbgEngineValueNode engineValueNode;
		DbgValueImpl value;

		public DbgValueNodeImpl(DbgLanguage language, DbgRuntime runtime, DbgEngineValueNode engineValueNode) {
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.engineValueNode = engineValueNode ?? throw new ArgumentNullException(nameof(engineValueNode));
			var engineValue = engineValueNode.Value;
			if (engineValue != null)
				value = new DbgValueImpl(runtime, engineValue);
			else if (!engineValueNode.IsReadOnly)
				throw new InvalidOperationException();
		}

		public override DbgValueNode[] GetChildren(DbgEvaluationContext context, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime != Runtime)
				throw new ArgumentException();
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			var engineNodes = engineValueNode.GetChildren(context, index, count, options, cancellationToken);
			return DbgValueNodeUtils.ToValueNodeArray(Language, Runtime, engineNodes);
		}

		public override void GetChildren(DbgEvaluationContext context, ulong index, int count, DbgValueNodeEvaluationOptions options, Action<DbgValueNode[]> callback, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime != Runtime)
				throw new ArgumentException();
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			engineValueNode.GetChildren(context, index, count, options, engineNodes => callback(DbgValueNodeUtils.ToValueNodeArray(Language, Runtime, engineNodes)), cancellationToken);
		}

		public override void Format(DbgEvaluationContext context, IDbgValueNodeFormatParameters options, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime != Runtime)
				throw new ArgumentException();
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			engineValueNode.Format(context, options, cancellationToken);
		}

		public override void Format(DbgEvaluationContext context, IDbgValueNodeFormatParameters options, Action callback, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime != Runtime)
				throw new ArgumentException();
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			engineValueNode.Format(context, options, callback, cancellationToken);
		}

		DbgValueNodeAssignmentResult CreateResult(DbgEngineValueNodeAssignmentResult result) {
			if (result.Error != null) {
				if (engineValueNode.Value != value.EngineValue)
					throw new InvalidOperationException();
				return new DbgValueNodeAssignmentResult(PredefinedEvaluationErrorMessagesHelper.GetErrorMessage(result.Error));
			}
			if (result.Value != engineValueNode.Value)
				throw new InvalidOperationException();
			lock (engineValueNode) {
				var oldValue = value;
				value = new DbgValueImpl(Runtime, result.Value);
				Process.DbgManager.Close(oldValue);
			}
			return new DbgValueNodeAssignmentResult(error: null);
		}

		public override DbgValueNodeAssignmentResult Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime != Runtime)
				throw new ArgumentException();
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			if (IsReadOnly)
				throw new InvalidOperationException();
			if (engineValueNode.ErrorMessage != null)
				throw new NotSupportedException();
			return CreateResult(engineValueNode.Assign(context, expression, options, cancellationToken));
		}

		public override void Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, Action<DbgValueNodeAssignmentResult> callback, CancellationToken cancellationToken) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (!(context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (context.Language != Language)
				throw new ArgumentException();
			if (context.Runtime != Runtime)
				throw new ArgumentException();
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			if (IsReadOnly)
				throw new InvalidOperationException();
			if (engineValueNode.ErrorMessage != null)
				throw new NotSupportedException();
			engineValueNode.Assign(context, expression, options, res => callback(CreateResult(res)), cancellationToken);
		}

		protected override void CloseCore() {
			value?.Close(Process.DbgManager.Dispatcher);
			engineValueNode.Close(Process.DbgManager.Dispatcher);
		}
	}
}
