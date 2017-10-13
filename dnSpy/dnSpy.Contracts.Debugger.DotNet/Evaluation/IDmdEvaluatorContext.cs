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

using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation {
	/// <summary>
	/// Context that can be passed into reflection methods that need an evaluator context
	/// </summary>
	public interface IDmdEvaluatorContext {
		/// <summary>
		/// Evaluation context
		/// </summary>
		DbgEvaluationContext EvaluationContext { get; }

		/// <summary>
		/// Stack frame
		/// </summary>
		DbgStackFrame Frame { get; }

		/// <summary>
		/// Cancellation token
		/// </summary>
		CancellationToken CancellationToken { get; }
	}

	/// <summary>
	/// Context that can be passed into reflection methods that need an evaluator context
	/// </summary>
	public sealed class DmdEvaluatorContext : IDmdEvaluatorContext {
		/// <summary>
		/// Evaluation context
		/// </summary>
		public DbgEvaluationContext EvaluationContext { get; set; }

		/// <summary>
		/// Stack frame
		/// </summary>
		public DbgStackFrame Frame { get; set; }

		/// <summary>
		/// Cancellation token
		/// </summary>
		public CancellationToken CancellationToken { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public DmdEvaluatorContext() { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public DmdEvaluatorContext(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken = default) {
			EvaluationContext = context;
			Frame = frame;
			CancellationToken = cancellationToken;
		}
	}
}
