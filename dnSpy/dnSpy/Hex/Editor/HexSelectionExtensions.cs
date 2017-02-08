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

using System.Text;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor {
	static class HexSelectionExtensions {
		public static string GetText(this HexSelection selection) {
			var bufferLines = selection.HexView.BufferLines;
			var span = selection.StreamSelectionSpan;

			var sb = new StringBuilder();
			var pos = span.Start;
			for (;;) {
				var line = bufferLines.GetLineFromPosition(pos);
				sb.AppendLine(line.GetText(span));

				pos = line.BufferEnd;
				if (pos >= span.End)
					break;
			}

			return sb.ToString();
		}
	}
}
