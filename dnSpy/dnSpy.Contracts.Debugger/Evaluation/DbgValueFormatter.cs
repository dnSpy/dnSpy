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
using System.Globalization;
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Debugger.Evaluation {
	/// <summary>
	/// Formats values and their types
	/// </summary>
	public abstract class DbgValueFormatter {
		/// <summary>
		/// Gets the language
		/// </summary>
		public abstract DbgLanguage Language { get; }

		/// <summary>
		/// Formats the value. The thread is blocked until the value has been formatted
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="frame">Stack frame</param>
		/// <param name="output">Output</param>
		/// <param name="value">Value to format</param>
		/// <param name="options">Options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Format(DbgEvaluationContext context, DbgStackFrame frame, ITextColorWriter output, DbgValue value, DbgValueFormatterOptions options, CultureInfo cultureInfo, CancellationToken cancellationToken = default);

		/// <summary>
		/// Formats the value's type. The thread is blocked until the type has been formatted
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="output">Output</param>
		/// <param name="value">Value to format</param>
		/// <param name="options">Options</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void FormatType(DbgEvaluationContext context, ITextColorWriter output, DbgValue value, DbgValueFormatterTypeOptions options, CultureInfo cultureInfo, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// Value formatter options
	/// </summary>
	[Flags]
	public enum DbgValueFormatterOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,

		/// <summary>
		/// Set if it should be formatted for display purposes, clear if it should be formatted so it can be edited
		/// </summary>
		Display						= 0x00000001,

		/// <summary>
		/// Set if integers are shown in decimal, clear if integers are shown in hexadecimal
		/// </summary>
		Decimal						= 0x00000002,

		/// <summary>
		/// Set to allow function evaluations (calling methods in the debugged process)
		/// </summary>
		FuncEval					= 0x00000004,

		/// <summary>
		/// Set to allow calling methods to get a string representation of the value. <see cref="FuncEval"/> must also be set.
		/// If it's a simple type (eg. an integer), it's formatted without calling any methods in the debugged process and
		/// this flag is ignored.
		/// </summary>
		ToString					= 0x00000008,

		/// <summary>
		/// Use digit separators. This flag is ignored if <see cref="Display"/> is not set and the language doesn't support digit separators
		/// </summary>
		DigitSeparators				= 0x00000010,

		/// <summary>
		/// Don't show string quotes, just the raw string value
		/// </summary>
		NoStringQuotes				= 0x00000020,

		/// <summary>
		/// Show namespaces. Only used if <see cref="Display"/> is set
		/// </summary>
		Namespaces					= 0x20000000,

		/// <summary>
		/// Show intrinsic type keywords (eg. int instead of Int32)
		/// </summary>
		IntrinsicTypeKeywords		= 0x40000000,

		/// <summary>
		/// Show tokens. Only used if <see cref="Display"/> is set
		/// </summary>
		Tokens						= int.MinValue,
	}

	/// <summary>
	/// Type formatter options
	/// </summary>
	[Flags]
	public enum DbgValueFormatterTypeOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,

		/// <summary>
		/// Show namespaces
		/// </summary>
		Namespaces					= 0x20000000,

		/// <summary>
		/// Show intrinsic type keywords (eg. int instead of Int32)
		/// </summary>
		IntrinsicTypeKeywords		= 0x40000000,

		/// <summary>
		/// Show tokens
		/// </summary>
		Tokens						= int.MinValue,
	}
}
