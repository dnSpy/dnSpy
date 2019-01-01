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
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Contains the classes needed to func-eval
	/// </summary>
	public sealed class DbgEvaluationInfo {
		/// <summary>
		/// Gets the evaluation context
		/// </summary>
		public DbgEvaluationContext Context { get; }

		/// <summary>
		/// Gets the stack frame
		/// </summary>
		public DbgStackFrame Frame { get; }

		/// <summary>
		/// Gets the cancellation token
		/// </summary>
		public CancellationToken CancellationToken { get; }

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public DbgRuntime Runtime => Context.Runtime;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public DbgEvaluationInfo(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken = default) {
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Frame = frame ?? throw new ArgumentNullException(nameof(frame));
			CancellationToken = cancellationToken;
			if (context.Runtime != frame.Runtime)
				throw new ArgumentException();
		}

		/// <summary>
		/// Closes <see cref="Frame"/> and <see cref="Context"/>
		/// </summary>
		public void Close() {
			Context.Close();
			Frame.Close();
		}
	}
}
