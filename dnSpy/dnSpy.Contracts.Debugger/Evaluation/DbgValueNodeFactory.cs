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
using dnSpy.Contracts.Debugger.CallStack;

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Creates <see cref="DbgValueNode"/>s
	/// </summary>
	public abstract class DbgValueNodeFactory {
		/// <summary>
		/// Gets the language
		/// </summary>
		public abstract DbgLanguage Language { get; }

		/// <summary>
		/// Creates <see cref="DbgValueNode"/>s. It blocks the current thread.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="expression">Expression</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public DbgCreateValueNodeResult Create(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken = default) =>
			Create(context, frame, new[] { new DbgExpressionEvaluationInfo(expression, options) }, cancellationToken)[0];

		/// <summary>
		/// Creates a <see cref="DbgValueNode"/>. The returned <see cref="DbgValueNode"/> is automatically closed when its runtime continues.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="expression">Expression</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the evaluation is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public void Create(DbgEvaluationContext context, DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgCreateValueNodeResult> callback, CancellationToken cancellationToken = default) =>
			Create(context, frame, new[] { new DbgExpressionEvaluationInfo(expression, options) }, result => callback(result[0]), cancellationToken);

		/// <summary>
		/// Creates a <see cref="DbgValueNode"/>. It blocks the current thread until the evaluation is complete.
		/// The returned <see cref="DbgValueNode"/> is automatically closed when its runtime continues.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="expressions">Expressions to evaluate</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgCreateValueNodeResult[] Create(DbgEvaluationContext context, DbgStackFrame frame, DbgExpressionEvaluationInfo[] expressions, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates a <see cref="DbgValueNode"/>. The returned <see cref="DbgValueNode"/> is automatically closed when its runtime continues.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="expressions">Expressions to evaluate</param>
		/// <param name="callback">Called when the evaluation is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Create(DbgEvaluationContext context, DbgStackFrame frame, DbgExpressionEvaluationInfo[] expressions, Action<DbgCreateValueNodeResult[]> callback, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates <see cref="DbgValueNode"/>s. It blocks the current thread.
		/// The returned <see cref="DbgValueNode"/>s are automatically closed when their runtime continues.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="objectIds">Object ids</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgValueNode[] Create(DbgEvaluationContext context, DbgStackFrame frame, DbgObjectId[] objectIds, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken = default);

		/// <summary>
		/// Creates <see cref="DbgValueNode"/>s. The returned <see cref="DbgValueNode"/>s are automatically closed when their runtime continues.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="objectIds">Object ids</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Create(DbgEvaluationContext context, DbgStackFrame frame, DbgObjectId[] objectIds, DbgValueNodeEvaluationOptions options, Action<DbgValueNode[]> callback, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// Contains the created <see cref="DbgValueNode"/> or an error message
	/// </summary>
	public struct DbgCreateValueNodeResult {
		/// <summary>
		/// Gets the created node or null if there was an error
		/// </summary>
		public DbgValueNode ValueNode { get; }

		/// <summary>
		/// true if the expression wasn't evaluated because it causes side effects (<see cref="DbgEvaluationOptions.NoSideEffects"/> was used)
		/// </summary>
		public bool CausesSideEffects { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="node">New value node</param>
		/// <param name="causesSideEffects">true if the expression wasn't evaluated because it causes side effects (<see cref="DbgEvaluationOptions.NoSideEffects"/> was used)</param>
		public DbgCreateValueNodeResult(DbgValueNode node, bool causesSideEffects) {
			ValueNode = node ?? throw new ArgumentNullException(nameof(node));
			CausesSideEffects = causesSideEffects;
		}
	}
}
