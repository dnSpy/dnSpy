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

using System.Globalization;
using System.Windows.Media.TextFormatting;

namespace dnSpy.Contracts.HexEditor {
	sealed class HexLineTextSource : TextSource {
		readonly HexLine hexLine;
		int index;
		static readonly TextEndOfLine endOfLine = new TextEndOfLine(1);

		public HexLineTextSource(HexLine hexLine) {
			this.hexLine = hexLine;
		}

		public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit) =>
			new TextSpan<CultureSpecificCharacterBufferRange>(0, new CultureSpecificCharacterBufferRange(CultureInfo.CurrentUICulture, new CharacterBufferRange(string.Empty, 0, 0)));
		public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex) => textSourceCharacterIndex;

		public override TextRun GetTextRun(int textSourceCharacterIndex) {
			var part = GetHexLinePart(textSourceCharacterIndex);
			if (part == null)
				return endOfLine;

			return new TextCharacters(hexLine.Text, part.Offset, part.Length, part.TextRunProperties);
		}

		HexLinePart GetHexLinePart(int offset) {
			var part = hexLine.LineParts[index];
			if (part.Offset <= offset && offset < part.Offset + part.Length)
				return part;
			for (int i = 1; i < hexLine.LineParts.Length; i++) {
				index = (index + 1) % hexLine.LineParts.Length;
				part = hexLine.LineParts[index];
				if (part.Offset <= offset && offset < part.Offset + part.Length)
					return part;
			}
			return null;
		}
	}
}
