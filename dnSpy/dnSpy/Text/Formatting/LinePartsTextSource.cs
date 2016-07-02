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
using System.Globalization;
using System.Windows.Media.TextFormatting;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text.Formatting {
	sealed class LinePartsTextSource : TextSource {
		readonly LinePartsCollection linePartsCollection;
		public int Length => linePartsCollection.Length;
		readonly string text;
		int maxLengthLeft;

		public LinePartsTextSource(LinePartsCollection linePartsCollection) {
			if (linePartsCollection == null)
				throw new ArgumentNullException(nameof(linePartsCollection));
			this.linePartsCollection = linePartsCollection;
			this.text = linePartsCollection.Span.GetText();
		}

		public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit) =>
			new TextSpan<CultureSpecificCharacterBufferRange>(0, new CultureSpecificCharacterBufferRange(CultureInfo.CurrentUICulture, new CharacterBufferRange(string.Empty, 0, 0)));
		public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex) => textSourceCharacterIndex;

		public override TextRun GetTextRun(int textSourceCharacterIndex) {
			var linePart = linePartsCollection.GetLinePartFromColumn(textSourceCharacterIndex);
			if (linePart == null)
				return endOfLine;
			var part = linePart.Value;
			if (part.AdornmentElement != null)
				throw new NotImplementedException();//TODO:
			else {
				int offs = textSourceCharacterIndex - part.Column;
				int length = part.Span.Length - offs;
				if (length > maxLengthLeft)
					length = maxLengthLeft;
				if (length == 0)
					return endOfLine;
				maxLengthLeft -= length;
				return new TextCharacters(text, part.Span.Start + offs, length, part.TextRunProperties);
			}
		}
		static readonly TextEndOfLine endOfLine = new TextEndOfLine(1);
		public TextEndOfLine EndOfLine => endOfLine;

		public SnapshotPoint ConvertColumnToBufferPosition(int column) => linePartsCollection.ConvertColumnToBufferPosition(column);

		public int GetColumnOfFirstNonWhitespace() {
			int column = 0;
			foreach (var part in linePartsCollection.LineParts) {
				if (part.AdornmentElement != null)
					break;
				int len = part.Span.Length;
				int start = part.Span.Start;
				for (int i = 0; i < len; i++, start++) {
					if (!char.IsWhiteSpace(text[start]))
						return column;
					column++;
				}
			}
			return column;
		}

		public void SetMaxLineLength(int maxLineLength) => maxLengthLeft = maxLineLength;
	}
}
