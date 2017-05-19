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

namespace dnSpy.Contracts.Debugger.Evaluation.Engine {
	/// <summary>
	/// Creates <see cref="DbgEngineValueNode"/>s
	/// </summary>
	public abstract class DbgEngineValueNodeFactory {
		/// <summary>
		/// Creates a <see cref="DbgEngineValueNode"/>. It blocks the current thread until the evaluation is complete.
		/// </summary>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="expression">Expression</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgCreateEngineValueNodeResult Create(DbgStackFrame frame, string expression, DbgEvaluationOptions options, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a <see cref="DbgEngineValueNode"/>
		/// </summary>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="expression">Expression</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the evaluation is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Create(DbgStackFrame frame, string expression, DbgEvaluationOptions options, Action<DbgCreateEngineValueNodeResult> callback, CancellationToken cancellationToken);
	}

	/// <summary>
	/// Common errors
	/// </summary>
	public static class PredefinedDbgCreateEngineValueNodeResultErrors {
		const string PREFIX = "<dnSpy>";

		/// <summary>
		/// <see cref="DbgEvaluationOptions.NoSideEffects"/> is set but expression causes side effects
		/// </summary>
		public const string ExpressionCausesSideEffects = PREFIX + nameof(ExpressionCausesSideEffects);
	}

	/// <summary>
	/// Contains the created <see cref="DbgEngineValueNode"/> or an error message
	/// </summary>
	public struct DbgCreateEngineValueNodeResult {
		/// <summary>
		/// Gets the created node or null if there was an error
		/// </summary>
		public DbgEngineValueNode EngineValueNode { get; }

		/// <summary>
		/// Error message or null if none
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="node">New value node</param>
		public DbgCreateEngineValueNodeResult(DbgEngineValueNode node) {
			EngineValueNode = node ?? throw new ArgumentNullException(nameof(node));
			Error = null;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="error">Error message, see also <see cref="PredefinedDbgCreateEngineValueNodeResultErrors"/></param>
		public DbgCreateEngineValueNodeResult(string error) {
			EngineValueNode = null;
			Error = error ?? throw new ArgumentNullException(nameof(error));
		}
	}
}
