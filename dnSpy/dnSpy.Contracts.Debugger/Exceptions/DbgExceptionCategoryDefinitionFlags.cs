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

namespace dnSpy.Contracts.Debugger.Exceptions {
	/// <summary>
	/// Exception category flags
	/// </summary>
	[Flags]
	public enum DbgExceptionCategoryDefinitionFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Exceptions are integer codes instead of strings
		/// </summary>
		Code					= 0x00000001,

		/// <summary>
		/// Exception code should be displayed in decimal and not in hexadecimal
		/// </summary>
		DecimalCode				= 0x00000002,

		/// <summary>
		/// Exception code is an unsigned integer
		/// </summary>
		UnsignedCode			= 0x00000004,
	}
}
