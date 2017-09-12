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
	/// Provides <see cref="DbgEngineValueNode"/>s for the variables windows
	/// </summary>
	public abstract class DbgEngineValueNodeProvider {
		/// <summary>
		/// Gets all values
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgEngineValueNode[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all values
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, Action<DbgEngineValueNode[]> callback, CancellationToken cancellationToken);
	}

	/// <summary>
	/// Provides <see cref="DbgEngineValueNode"/>s for the locals windows
	/// </summary>
	public abstract class DbgEngineLocalsValueNodeProvider {
		/// <summary>
		/// Gets all values
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="options">Options</param>
		/// <param name="localsOptions">Locals value node provider options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgEngineValueNode[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, DbgLocalsValueNodeEvaluationOptions localsOptions, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all values
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="options">Options</param>
		/// <param name="localsOptions">Locals value node provider options</param>
		/// <param name="callback">Called when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, DbgLocalsValueNodeEvaluationOptions localsOptions, Action<DbgEngineValueNode[]> callback, CancellationToken cancellationToken);
	}
}
