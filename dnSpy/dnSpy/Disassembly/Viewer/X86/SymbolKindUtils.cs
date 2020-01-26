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
		public static SymbolKind ToSymbolKind(FormatterTextKind kind) {
			switch (kind) {
			case FormatterTextKindExtensions.UnknownSymbol:return SymbolKind.Unknown;
			case FormatterTextKind.Data:		return SymbolKind.Data;
			case FormatterTextKind.Label:		return SymbolKind.Label;
			case FormatterTextKind.Function:	return SymbolKind.Function;
			default:							return SymbolKind.Unknown;
			}
		}

		public static FormatterTextKind ToFormatterOutputTextKind(SymbolKind kind) {
			switch (kind) {
			case SymbolKind.Unknown:	return FormatterTextKindExtensions.UnknownSymbol;
			case SymbolKind.Data:		return FormatterTextKind.Data;
			case SymbolKind.Label:		return FormatterTextKind.Label;
			case SymbolKind.Function:	return FormatterTextKind.Function;
			default:					return FormatterTextKind.Text;
			}
		}
	}
}
