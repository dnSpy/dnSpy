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
	/// Stack frame formatter options
	/// </summary>
	[Flags]
	public enum DbgStackFrameFormatterOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Show module name, eg. <c>module.dll!func</c>
		/// </summary>
		ModuleNames				= 0x00000001,

		/// <summary>
		/// Show parameter types
		/// </summary>
		ParameterTypes			= 0x00000002,

		/// <summary>
		/// Show parameter names
		/// </summary>
		ParameterNames			= 0x00000004,

		/// <summary>
		/// Show parameter values
		/// </summary>
		ParameterValues			= 0x00000008,

		/// <summary>
		/// Show declaring type
		/// </summary>
		DeclaringTypes			= 0x00000010,

		/// <summary>
		/// Show return type
		/// </summary>
		ReturnTypes				= 0x00000020,

		/// <summary>
		/// Show namespace
		/// </summary>
		Namespaces				= 0x00000040,

		/// <summary>
		/// Show intrinsic type keywords (eg. int instead of Int32)
		/// </summary>
		IntrinsicTypeKeywords	= 0x00000080,

		/// <summary>
		/// Set if integers are shown in decimal, clear if integers are shown in hexadecimal
		/// </summary>
		Decimal					= 0x00000100,

		/// <summary>
		/// Show tokens
		/// </summary>
		Tokens					= 0x00000200,

		/// <summary>
		/// Show instruction pointer
		/// </summary>
		IP						= 0x00000400,

		/// <summary>
		/// Use digit separators
		/// </summary>
		DigitSeparators			= 0x00000800,

		/// <summary>
		/// Show the full string value even if it's a very long string
		/// </summary>
		FullString				= 0x00001000,
	}
}
