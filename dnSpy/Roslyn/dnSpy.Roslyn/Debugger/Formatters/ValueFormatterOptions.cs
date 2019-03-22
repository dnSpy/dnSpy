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

namespace dnSpy.Roslyn.Debugger.Formatters {
	[Flags]
	enum ValueFormatterOptions {
		None						= 0,
		Edit						= 0x00000001,
		Decimal						= 0x00000002,
		FuncEval					= 0x00000004,
		ToString					= 0x00000008,
		Namespaces					= 0x00000010,
		IntrinsicTypeKeywords		= 0x00000020,
		Tokens						= 0x00000040,
		DigitSeparators				= 0x00000080,
		NoStringQuotes				= 0x00000100,
		NoDebuggerDisplay			= 0x00000200,
		FullString					= 0x00000400,
	}
}
