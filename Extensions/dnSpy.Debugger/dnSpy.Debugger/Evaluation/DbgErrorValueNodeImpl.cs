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
using dnSpy.Contracts.Text;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgErrorValueNodeImpl : DbgBaseValueNodeImpl {
		public override DbgLanguage Language { get; }
		public override DbgRuntime Runtime { get; }
		public override string ErrorMessage => PredefinedEvaluationErrorMessagesHelper.GetErrorMessage(engineErrorValueNode.ErrorMessage);
		public override DbgValue Value => null;
		public override bool CanEvaluateExpression => true;
		public override string Expression => engineErrorValueNode.Expression;
		public override string ImageName => PredefinedDbgValueNodeImageNames.Error;
		public override bool IsReadOnly => true;
		public override bool CausesSideEffects => false;
		public override bool? HasChildren => false;
		public override ulong ChildCount => 0;

		readonly DbgEngineErrorValueNode engineErrorValueNode;

		public DbgErrorValueNodeImpl(DbgLanguage language, DbgRuntime runtime, DbgEngineErrorValueNode engineErrorValueNode) {
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			Language = language ?? throw new ArgumentNullException(nameof(language));
			this.engineErrorValueNode = engineErrorValueNode ?? throw new ArgumentNullException(nameof(engineErrorValueNode));
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
			if (index != 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (count != 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			return Array.Empty<DbgValueNode>();
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
			if (index != 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (count != 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			callback(Array.Empty<DbgValueNode>());
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
			options.ValueOutput?.Write(BoxedTextColor.Error, ErrorMessage);
			if (options.NameOutput != null)
				engineErrorValueNode.FormatName(context, options.NameOutput, cancellationToken);
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
			options.ValueOutput?.Write(BoxedTextColor.Error, ErrorMessage);
			if (options.NameOutput != null)
				engineErrorValueNode.FormatName(context, options.NameOutput, callback, cancellationToken);
			else
				callback();
		}

		public override DbgValueNodeAssignmentResult Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
		public override void Assign(DbgEvaluationContext context, string expression, DbgEvaluationOptions options, Action<DbgValueNodeAssignmentResult> callback, CancellationToken cancellationToken) => throw new NotSupportedException();
		protected override void CloseCore() => engineErrorValueNode.Close(Process.DbgManager.Dispatcher);
	}
}
