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
using dnSpy.Contracts.Hex.Formatting;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Editor {
	static class HexViewLineExtensions {
		public static bool IsVisible(this HexViewLine line) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			return line.VisibilityState == VSTF.VisibilityState.FullyVisible || line.VisibilityState == VSTF.VisibilityState.PartiallyVisible;
		}

		public static bool IsFirstDocumentLine(this HexViewLine line) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			return line.BufferLine.LineNumber == 0;
		}

		public static bool IsLastDocumentLine(this HexViewLine line) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			return line.BufferLine.LineNumber + 1 == line.BufferLine.LineProvider.LineCount;
		}
	}
}
