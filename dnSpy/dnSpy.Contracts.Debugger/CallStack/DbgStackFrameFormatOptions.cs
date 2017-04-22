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

namespace dnSpy.Contracts.Debugger.CallStack {
	/// <summary>
	/// Flags used when formatting a <see cref="DbgStackFrame"/>
	/// </summary>
	[Flags]
	public enum DbgStackFrameFormatOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None							= 0,

		/// <summary>
		/// Show the return type
		/// </summary>
		ShowReturnType					= 0x00000001,

		/// <summary>
		/// Show parameter types
		/// </summary>
		ShowParameterTypes				= 0x00000002,

		/// <summary>
		/// Show parameter names
		/// </summary>
		ShowParameterNames				= 0x00000004,

		/// <summary>
		/// Show parameter values (parameters will be evaluated and formatted). See also <see cref="UseDecimal"/>
		/// </summary>
		ShowParameterValues				= 0x00000008,

		/// <summary>
		/// Show the offset of the IP relative to the start of the function (always in hexadecimal)
		/// </summary>
		ShowFunctionOffset				= 0x00000010,

		/// <summary>
		/// Show module names
		/// </summary>
		ShowModuleName					= 0x00000020,

		/// <summary>
		/// Show declaring types
		/// </summary>
		ShowDeclaringTypes				= 0x00000040,

		/// <summary>
		/// Show namespaces
		/// </summary>
		ShowNamespaces					= 0x00000080,

		/// <summary>
		/// Show intrinsic type keywords (eg. int instead of Int32)
		/// </summary>
		ShowIntrinsicTypeKeywords		= 0x00000100,

		/// <summary>
		/// Show tokens (always in hexadecimal)
		/// </summary>
		ShowTokens						= 0x00000200,

		/// <summary>
		/// Use decimal instead of hexadecimal when formatting parameter values. Offsets, addresses, tokens are always in hexadecimal.
		/// </summary>
		UseDecimal						= 0x00000400,
	}
}
