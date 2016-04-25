/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Type printer flags
	/// </summary>
	[Flags]
	public enum TypeFormatFlags {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

		// IMPORTANT: Must be identical to dndbg.Engine.TypePrinterFlags

		ShowModuleNames				= 0x00000001,
		ShowParameterTypes			= 0x00000002,
		ShowParameterNames			= 0x00000004,
		ShowParameterValues			= 0x00000008,
		ShowOwnerTypes				= 0x00000010,
		ShowReturnTypes				= 0x00000020,
		ShowNamespaces				= 0x00000040,
		ShowTypeKeywords			= 0x00000080,
		UseDecimal					= 0x00000100,
		ShowTokens					= 0x00000200,
		ShowIP						= 0x00000400,
		ShowArrayValueSizes			= 0x00000800,
		ShowFieldLiteralValues		= 0x00001000,
		ShowParameterLiteralValues	= 0x00002000,

		Default =
			ShowModuleNames |
			ShowParameterTypes |
			ShowParameterNames |
			ShowOwnerTypes |
			ShowNamespaces |
			ShowTypeKeywords |
			ShowArrayValueSizes |
			ShowFieldLiteralValues,
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
