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
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Engine value node with an error message
	/// </summary>
	public abstract class DbgEngineErrorValueNode : DbgBaseEngineValueNode {
		/// <summary>
		/// Gets the error message. It can be a custom error message or an error message defined in <see cref="PredefinedEvaluationErrorMessages"/>
		/// </summary>
		public abstract string ErrorMessage { get; }

		/// <summary>
		/// Gets the expression
		/// </summary>
		public abstract string Expression { get; }

		/// <summary>
		/// Formats the name
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="output">Output</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void FormatName(DbgEvaluationContext context, ITextColorWriter output, CancellationToken cancellationToken);

		/// <summary>
		/// Formats the name
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="output">Output</param>
		/// <param name="callback">Called when the formatting is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void FormatName(DbgEvaluationContext context, ITextColorWriter output, Action callback, CancellationToken cancellationToken);
	}
}
