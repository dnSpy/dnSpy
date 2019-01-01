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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.CallStack;

namespace dnSpy.Debugger.Evaluation {
	sealed class DbgValueNodeImpl : DbgValueNode {
		public override DbgLanguage Language { get; }
		public override DbgRuntime Runtime { get; }
		public override string ErrorMessage => PredefinedEvaluationErrorMessagesHelper.GetErrorMessage(engineValueNode.ErrorMessage);
		public override DbgValue Value => value;
		public override bool CanEvaluateExpression => true;
		public override string Expression => engineValueNode.Expression;
		public override string ImageName => engineValueNode.ImageName;
		public override bool IsReadOnly => engineValueNode.IsReadOnly;
		public override bool CausesSideEffects => engineValueNode.CausesSideEffects;
		public override bool? HasChildren => engineValueNode.HasChildren;

		readonly DbgEngineValueNode engineValueNode;
		readonly DbgValueImpl value;

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

		public override ulong GetChildCount(DbgEvaluationInfo evalInfo) {
			if (evalInfo == null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime != Runtime)
				throw new ArgumentException();
			return engineValueNode.GetChildCount(evalInfo);
		}

		public override DbgValueNode[] GetChildren(DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options) {
			if (evalInfo == null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime != Runtime)
				throw new ArgumentException();
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			var engineNodes = engineValueNode.GetChildren(evalInfo, index, count, options);
			return DbgValueNodeUtils.ToValueNodeArray(Language, Runtime, engineNodes);
		}

		public override void Format(DbgEvaluationInfo evalInfo, IDbgValueNodeFormatParameters options, CultureInfo cultureInfo) {
			if (evalInfo == null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime != Runtime)
				throw new ArgumentException();
			if (!(evalInfo.Frame is DbgStackFrameImpl))
				throw new ArgumentException();
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			engineValueNode.Format(evalInfo, options, cultureInfo);
		}

		DbgValueNodeAssignmentResult CreateResult(DbgEngineValueNodeAssignmentResult result) {
			if (result.Error != null) {
				if (engineValueNode.Value != value?.EngineValue)
					throw new InvalidOperationException();
				return new DbgValueNodeAssignmentResult(result.Flags, PredefinedEvaluationErrorMessagesHelper.GetErrorMessage(result.Error));
			}
			return new DbgValueNodeAssignmentResult(result.Flags, result.Error);
		}

		public override DbgValueNodeAssignmentResult Assign(DbgEvaluationInfo evalInfo, string expression, DbgEvaluationOptions options) {
			if (evalInfo == null)
				throw new ArgumentNullException(nameof(evalInfo));
			if (!(evalInfo.Context is DbgEvaluationContextImpl))
				throw new ArgumentException();
			if (evalInfo.Context.Language != Language)
				throw new ArgumentException();
			if (evalInfo.Context.Runtime != Runtime)
				throw new ArgumentException();
			if (!(evalInfo.Frame is DbgStackFrameImpl))
				throw new ArgumentException();
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));
			if (IsReadOnly)
				throw new InvalidOperationException();
			if (engineValueNode.ErrorMessage != null)
				throw new NotSupportedException();
			return CreateResult(engineValueNode.Assign(evalInfo, expression, options));
		}

		protected override void CloseCore(DbgDispatcher dispatcher) {
			value?.Close(dispatcher);
			engineValueNode.Close(dispatcher);
		}
	}
}
