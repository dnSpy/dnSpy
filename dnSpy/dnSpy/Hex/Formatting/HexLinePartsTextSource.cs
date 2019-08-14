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
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Hex.Formatting;
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Hex.Formatting {
	sealed class HexLinePartsTextSource : TextSource {
		readonly HexLinePartsCollection linePartsCollection;
		public int Length => linePartsCollection.Length;
		readonly string text;
		int maxLengthLeft;
		int linePartIndex;

		public HexLinePartsTextSource(HexLinePartsCollection linePartsCollection) {
			this.linePartsCollection = linePartsCollection ?? throw new ArgumentNullException(nameof(linePartsCollection));
			text = linePartsCollection.Text;
		}

		public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit) =>
			new TextSpan<CultureSpecificCharacterBufferRange>(0, new CultureSpecificCharacterBufferRange(CultureInfo.CurrentUICulture, new CharacterBufferRange(string.Empty, 0, 0)));
		public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex) => textSourceCharacterIndex;

		public override TextRun GetTextRun(int textSourceCharacterIndex) {
			var linePart = linePartsCollection.GetLinePartFromColumn(textSourceCharacterIndex, ref linePartIndex);
			if (linePart is null)
				return endOfLine;
			var part = linePart.Value;
			if (!(part.AdornmentElement is null))
				return new AdornmentTextRun(part);
			else {
				int offs = textSourceCharacterIndex - part.Column;
				int length = part.ColumnLength - offs;
				Debug.Assert(length >= 0);
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

		sealed class AdornmentTextRun : TextEmbeddedObject {
			public override CharacterBufferReference CharacterBufferReference => new CharacterBufferReference(" ", 1);
			public override LineBreakCondition BreakBefore { get; }
			public override LineBreakCondition BreakAfter { get; }
			public override bool HasFixedSize { get; }
			public override int Length { get; }
			public override TextRunProperties? Properties { get; }
			readonly HexAdornmentElement adornmentElement;

			public AdornmentTextRun(HexLinePart linePart) {
				Debug2.Assert(!(linePart.AdornmentElement is null));
				adornmentElement = linePart.AdornmentElement;
				if (linePart.Span.Length != 0 || adornmentElement.Affinity == VST.PositionAffinity.Successor) {
					BreakBefore = LineBreakCondition.BreakPossible;
					BreakAfter = LineBreakCondition.BreakRestrained;
				}
				else {
					BreakBefore = LineBreakCondition.BreakRestrained;
					BreakAfter = LineBreakCondition.BreakPossible;
				}
				HasFixedSize = true;
				Length = linePart.ColumnLength;
				Properties = linePart.TextRunProperties;
			}

			public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways) {
				// IntraTextAdornment service does this in its own adornment layer
			}

			public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways) =>
				new Rect(0, 0, adornmentElement.Width, adornmentElement.TextHeight);
			public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth) =>
				new TextEmbeddedObjectMetrics(adornmentElement.Width, adornmentElement.TextHeight, adornmentElement.Baseline);
		}

		public int ConvertColumnToLinePosition(int column) => linePartsCollection.ConvertColumnToLinePosition(column);

		public int GetColumnOfFirstNonWhitespace() {
			int column = 0;
			foreach (var part in linePartsCollection.LineParts) {
				if (!(part.AdornmentElement is null))
					break;
				int len = part.Span.Length;
				int start = part.Span.Start;
				for (int i = 0; i < len; i++, start++) {
					if (!char.IsWhiteSpace(text[start]))
						return column + i;
				}
				column += part.ColumnLength;
			}
			return column;
		}

		public void SetMaxLineLength(int maxLineLength) => maxLengthLeft = maxLineLength;
	}
}
