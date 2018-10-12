/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using Iced.Intel;

namespace dnSpy.Disassembly.Viewer {
	static class SymbolKindUtils {
		public static SymbolKind ToSymbolKind(FormatterOutputTextKind kind) {
			switch (kind) {
			case FormatterOutputTextKindExtensions.UnknownSymbol:return SymbolKind.Unknown;
			case FormatterOutputTextKindExtensions.Data:		return SymbolKind.Data;
			case FormatterOutputTextKindExtensions.Label:		return SymbolKind.Label;
			case FormatterOutputTextKindExtensions.Function:	return SymbolKind.Function;
			default:											return SymbolKind.Unknown;
			}
		}

		public static FormatterOutputTextKind ToFormatterOutputTextKind(SymbolKind kind) {
			switch (kind) {
			case SymbolKind.Unknown:	return FormatterOutputTextKindExtensions.UnknownSymbol;
			case SymbolKind.Data:		return FormatterOutputTextKindExtensions.Data;
			case SymbolKind.Label:		return FormatterOutputTextKindExtensions.Label;
			case SymbolKind.Function:	return FormatterOutputTextKindExtensions.Function;
			default:					return FormatterOutputTextKind.Text;
			}
		}
	}
}
