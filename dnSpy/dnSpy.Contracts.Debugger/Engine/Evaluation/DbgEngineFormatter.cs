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

using System.Globalization;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Formats names, types, values, stack frames
	/// </summary>
	public abstract class DbgEngineFormatter {
		/// <summary>
		/// Formats an exception name
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="output">Output</param>
		/// <param name="id">Exception id</param>
		public abstract void FormatExceptionName(DbgEvaluationContext context, IDbgTextWriter output, uint id);

		/// <summary>
		/// Formats a stowed exception name
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="output">Output</param>
		/// <param name="id">Stowed exception id</param>
		public abstract void FormatStowedExceptionName(DbgEvaluationContext context, IDbgTextWriter output, uint id);

		/// <summary>
		/// Formats a return value name
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="output">Output</param>
		/// <param name="id">Return value id</param>
		public abstract void FormatReturnValueName(DbgEvaluationContext context, IDbgTextWriter output, uint id);

		/// <summary>
		/// Formats an object ID name
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="output">Output</param>
		/// <param name="id">Object id</param>
		public abstract void FormatObjectIdName(DbgEvaluationContext context, IDbgTextWriter output, uint id);

		/// <summary>
		/// Formats a stack frame
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="output">Output</param>
		/// <param name="options">Stack frame options</param>
		/// <param name="valueOptions">Value option</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		public abstract void FormatFrame(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgStackFrameFormatterOptions options, DbgValueFormatterOptions valueOptions, CultureInfo cultureInfo);

		/// <summary>
		/// Formats a value
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="output">Output</param>
		/// <param name="value">Value to format</param>
		/// <param name="options">Options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		public abstract void FormatValue(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgEngineValue value, DbgValueFormatterOptions options, CultureInfo cultureInfo);

		/// <summary>
		/// Formats a value's type
		/// </summary>
		/// <param name="evalInfo">Evaluation info</param>
		/// <param name="output">Output</param>
		/// <param name="value">Value to format</param>
		/// <param name="options">Options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		public abstract void FormatType(DbgEvaluationInfo evalInfo, IDbgTextWriter output, DbgEngineValue value, DbgValueFormatterTypeOptions options, CultureInfo cultureInfo);
	}
}
