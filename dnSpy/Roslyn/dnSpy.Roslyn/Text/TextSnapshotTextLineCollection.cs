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

using Microsoft.CodeAnalysis.Text;

namespace dnSpy.Roslyn.Text {
	sealed class TextSnapshotTextLineCollection : TextLineCollection {
		readonly TextSnapshotSourceText sourceText;

		public override int Count => sourceText.TextSnapshot.LineCount;
		public override TextLine this[int index] {
			get {
				var line = sourceText.TextSnapshot.GetLineFromLineNumber(index);
				return TextLine.FromSpan(sourceText, TextSpan.FromBounds(line.Start, line.End));
			}
		}

		public TextSnapshotTextLineCollection(TextSnapshotSourceText sourceText) => this.sourceText = sourceText;

		public override int IndexOf(int position) => sourceText.TextSnapshot.GetLineNumberFromPosition(position);
		public override TextLine GetLineFromPosition(int position) => this[IndexOf(position)];
		public override LinePosition GetLinePosition(int position) {
			var textLine = sourceText.TextSnapshot.GetLineFromPosition(position);
			return new LinePosition(textLine.LineNumber, position - textLine.Start);
		}
	}
}
