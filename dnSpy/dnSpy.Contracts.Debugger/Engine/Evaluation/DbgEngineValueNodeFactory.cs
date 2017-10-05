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
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Creates <see cref="DbgEngineValueNode"/>s
	/// </summary>
	public abstract class DbgEngineValueNodeFactory {
		/// <summary>
		/// Creates a <see cref="DbgEngineValueNode"/>. It blocks the current thread until the evaluation is complete.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="expressions">Expressions to evaluate</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgEngineValueNode[] Create(DbgEvaluationContext context, DbgStackFrame frame, DbgExpressionEvaluationInfo[] expressions, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a <see cref="DbgEngineValueNode"/>
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="expressions">Expressions to evaluate</param>
		/// <param name="callback">Called when the evaluation is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Create(DbgEvaluationContext context, DbgStackFrame frame, DbgExpressionEvaluationInfo[] expressions, Action<DbgEngineValueNode[]> callback, CancellationToken cancellationToken);

		/// <summary>
		/// Creates <see cref="DbgEngineValueNode"/>s. It blocks the current thread.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="objectIds">Object ids</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgEngineValueNode[] Create(DbgEvaluationContext context, DbgStackFrame frame, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken);

		/// <summary>
		/// Creates <see cref="DbgEngineValueNode"/>s
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame</param>
		/// <param name="objectIds">Object ids</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Create(DbgEvaluationContext context, DbgStackFrame frame, DbgEngineObjectId[] objectIds, DbgValueNodeEvaluationOptions options, Action<DbgEngineValueNode[]> callback, CancellationToken cancellationToken);
	}
}
