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

namespace dnSpy.Contracts.Debugger.Evaluation {
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
		/// Set if it should be formatted so it can be edited
		/// </summary>
		Edit						= 0x00000001,

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
		/// Use digit separators. This flag is ignored if <see cref="Edit"/> is set and the language doesn't support digit separators
		/// </summary>
		DigitSeparators				= 0x00000010,

		/// <summary>
		/// Don't show string quotes, just the raw string value
		/// </summary>
		NoStringQuotes				= 0x00000020,

		/// <summary>
		/// Don't use debugger display attributes
		/// </summary>
		NoDebuggerDisplay			= 0x00000040,

		/// <summary>
		/// Show the full string value even if it's a very long string
		/// </summary>
		FullString					= 0x00000080,

		/// <summary>
		/// Show namespaces. Only used if <see cref="Edit"/> is clear
		/// </summary>
		Namespaces					= 0x20000000,

		/// <summary>
		/// Show intrinsic type keywords (eg. int instead of Int32)
		/// </summary>
		IntrinsicTypeKeywords		= 0x40000000,

		/// <summary>
		/// Show tokens. Only used if <see cref="Edit"/> is clear
		/// </summary>
		Tokens						= int.MinValue,
	}
}
