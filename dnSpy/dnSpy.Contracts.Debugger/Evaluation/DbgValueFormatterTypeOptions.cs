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
	/// Type formatter options
	/// </summary>
	[Flags]
	public enum DbgValueFormatterTypeOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,

		/// <summary>
		/// Set if integers are shown in decimal, clear if integers are shown in hexadecimal
		/// </summary>
		Decimal						= 0x00000001,

		/// <summary>
		/// Use digit separators
		/// </summary>
		DigitSeparators				= 0x00000002,

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
