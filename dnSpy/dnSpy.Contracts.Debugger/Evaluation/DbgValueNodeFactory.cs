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
		/// Creates a <see cref="DbgValueNode"/>. It blocks the current thread until the evaluation is complete.
		/// </summary>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="expression">Expression</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgCreateValueNodeResult Create(DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// Creates a <see cref="DbgValueNode"/>
		/// </summary>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="expression">Expression</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the evaluation is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Create(DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgCreateValueNodeResult> callback, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// Creates <see cref="DbgValueNode"/>s. It blocks the current thread.
		/// </summary>
		/// <param name="objectIds">Object ids</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgCreateObjectIdValueNodeResult[] Create(DbgObjectId[] objectIds, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// Creates <see cref="DbgValueNode"/>s
		/// </summary>
		/// <param name="objectIds">Object ids</param>
		/// <param name="callback">Called when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Create(DbgObjectId[] objectIds, Action<DbgCreateObjectIdValueNodeResult[]> callback, CancellationToken cancellationToken = default(CancellationToken));
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
		/// true if there was an error (see <see cref="Error"/>)
		/// </summary>
		public bool HasError => Error != null;

		/// <summary>
		/// Error message or null if none
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// true if the expression wasn't evaluated because it causes side effects (<see cref="DbgEvaluationOptions.NoSideEffects"/> was used)
		/// </summary>
		public bool CausesSideEffects { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="node">New value node</param>
		public DbgCreateValueNodeResult(DbgValueNode node) {
			ValueNode = node ?? throw new ArgumentNullException(nameof(node));
			Error = null;
			CausesSideEffects = false;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="error">Error message</param>
		/// <param name="causesSideEffects">true if the expression wasn't evaluated because it causes side effects (<see cref="DbgEvaluationOptions.NoSideEffects"/> was used)</param>
		public DbgCreateValueNodeResult(string error, bool causesSideEffects) {
			ValueNode = null;
			Error = error ?? throw new ArgumentNullException(nameof(error));
			CausesSideEffects = causesSideEffects;
		}
	}

	/// <summary>
	/// Contains the created <see cref="DbgValueNode"/> or an error message
	/// </summary>
	public struct DbgCreateObjectIdValueNodeResult {
		/// <summary>
		/// Gets the created node or null if there was an error
		/// </summary>
		public DbgValueNode ValueNode { get; }

		/// <summary>
		/// true if there was an error (see <see cref="Error"/>)
		/// </summary>
		public bool HasError => Error != null;

		/// <summary>
		/// Error message or null if none
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// Object id expression
		/// </summary>
		public string Expression { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="node">New value node</param>
		public DbgCreateObjectIdValueNodeResult(DbgValueNode node) {
			ValueNode = node ?? throw new ArgumentNullException(nameof(node));
			Expression = node.Expression;
			Error = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="expression">Object id expression</param>
		/// <param name="error">Error message</param>
		public DbgCreateObjectIdValueNodeResult(string expression, string error) {
			ValueNode = null;
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
			Error = error ?? throw new ArgumentNullException(nameof(error));
		}
	}
}
