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
	/// Formats values and their types
	/// </summary>
	public abstract class DbgEngineValueFormatter {
		/// <summary>
		/// Formats the value. The thread is blocked until the value has been formatted
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="output">Output</param>
		/// <param name="value">Value to format</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Format(DbgEvaluationContext context, ITextColorWriter output, DbgEngineValue value, DbgValueFormatterOptions options, CancellationToken cancellationToken);

		/// <summary>
		/// Formats the value's type. The thread is blocked until the type has been formatted
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="output">Output</param>
		/// <param name="value">Value to format</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void FormatType(DbgEvaluationContext context, ITextColorWriter output, DbgEngineValue value, DbgValueFormatterTypeOptions options, CancellationToken cancellationToken);
	}
}
