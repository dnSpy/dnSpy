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
	/// Provides <see cref="DbgValueNode"/>s for the locals and autos windows
	/// </summary>
	public abstract class DbgValueNodeProvider {
		/// <summary>
		/// Gets the language
		/// </summary>
		public abstract DbgLanguage Language { get; }

		/// <summary>
		/// Gets all values. It blocks the current thread until the method is complete.
		/// The returned <see cref="DbgValueNode"/>s are automatically closed when their runtime continues.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract DbgValueNode[] GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// Gets all values. The returned <see cref="DbgValueNode"/>s are automatically closed when their runtime continues.
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Frame, owned by caller</param>
		/// <param name="options">Options</param>
		/// <param name="callback">Called when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void GetNodes(DbgEvaluationContext context, DbgStackFrame frame, DbgValueNodeEvaluationOptions options, Action<DbgValueNode[]> callback, CancellationToken cancellationToken = default(CancellationToken));
	}
}
