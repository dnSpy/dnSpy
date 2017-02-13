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
using System.Collections.Generic;
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Hex.Formatting {
	sealed class HexLinePartsCollection {
		public string Text { get; }
		public List<HexLinePart> LineParts { get; }
		public int Length { get; }
		public VST.Span Span { get; }

		public HexLinePartsCollection(List<HexLinePart> lineParts, VST.Span lineSpan, string text) {
			Text = text ?? throw new ArgumentNullException(nameof(text));
			Span = lineSpan;
			LineParts = lineParts ?? throw new ArgumentNullException(nameof(lineParts));
			if (lineParts.Count == 0)
				Length = 0;
			else {
				var last = lineParts[lineParts.Count - 1];
				Length = last.Column + last.ColumnLength;
			}
		}

		public HexLinePart? GetLinePartFromColumn(int column) {
			int index = 0;
			return GetLinePartFromColumn(column, ref index);
		}

		public HexLinePart? GetLinePartFromColumn(int column, ref int linePartsIndex) {
			if (LineParts.Count == 0)
				return null;
			for (int i = 0; i < LineParts.Count; i++) {
				var part = LineParts[linePartsIndex];
				if (part.Column <= column && column < part.Column + part.ColumnLength)
					return part;
				linePartsIndex++;
				if (linePartsIndex >= LineParts.Count)
					linePartsIndex = 0;
			}
			return null;
		}

		public HexLinePart? GetLinePartFromLinePosition(int linePosition) {
			if (LineParts.Count == 0)
				return null;
			int lineIndex = linePosition - Span.Start;
			for (int i = 0; i < LineParts.Count; i++) {
				var part = LineParts[i];
				if (part.BelongsTo(lineIndex))
					return part;
			}
			return null;
		}

		public int ConvertLinePositionToColumn(int linePosition) {
			var linePart = GetLinePartFromLinePosition(linePosition);
			if (linePart == null && linePosition == Span.End && LineParts.Count != 0)
				linePart = LineParts[LineParts.Count - 1];
			if (linePart == null)
				return 0;
			if (linePart.Value.AdornmentElement != null)
				return linePart.Value.Column;
			return linePart.Value.Column + ((linePosition - Span.Start) - linePart.Value.Span.Start);
		}

		public int ConvertColumnToLinePosition(int column) {
			var linePart = GetLinePartFromColumn(column);
			if (linePart == null && column == Length && LineParts.Count != 0)
				linePart = LineParts[LineParts.Count - 1];
			return Span.Start + (linePart == null ? 0 : linePart.Value.Span.Start + (column - linePart.Value.Column));
		}

		public int? ConvertColumnToLinePosition(int column, bool includeHiddenPositions) {
			if (includeHiddenPositions)
				return ConvertColumnToLinePosition(column);

			var linePart = GetLinePartFromColumn(column);
			if (linePart == null && column == Length && LineParts.Count != 0)
				linePart = LineParts[LineParts.Count - 1];
			if (linePart == null)
				return null;
			if (linePart.Value.AdornmentElement != null)
				return null;
			return Span.Start + linePart.Value.Span.Start + (column - linePart.Value.Column);
		}
	}
}
