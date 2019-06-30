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

using dnSpy.Contracts.Disassembly;
using dnSpy.Disassembly.X86;
using Iced.Intel;

namespace dnSpy.Disassembly.Viewer.X86 {
	static class SymbolKindUtils {
		public static SymbolKind ToSymbolKind(FormatterOutputTextKind kind) {
			switch (kind) {
			case FormatterOutputTextKindExtensions.UnknownSymbol:return SymbolKind.Unknown;
			case FormatterOutputTextKind.Data:		return SymbolKind.Data;
			case FormatterOutputTextKind.Label:		return SymbolKind.Label;
			case FormatterOutputTextKind.Function:	return SymbolKind.Function;
			default:								return SymbolKind.Unknown;
			}
		}

		public static FormatterOutputTextKind ToFormatterOutputTextKind(SymbolKind kind) {
			switch (kind) {
			case SymbolKind.Unknown:	return FormatterOutputTextKindExtensions.UnknownSymbol;
			case SymbolKind.Data:		return FormatterOutputTextKind.Data;
			case SymbolKind.Label:		return FormatterOutputTextKind.Label;
			case SymbolKind.Function:	return FormatterOutputTextKind.Function;
			default:					return FormatterOutputTextKind.Text;
			}
		}
	}
}
