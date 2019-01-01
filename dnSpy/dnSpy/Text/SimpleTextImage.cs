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
using System.IO;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text {
	sealed class SimpleTextImage : ITextImage {
		readonly string text;
		uint[] lineOffsets;

		public char this[int position] => text[position];
		public ITextImageVersion Version => null;
		public int Length => text.Length;

		public int LineCount {
			get {
				if (lineOffsets == null)
					lineOffsets = TextImageUtils.CreateLineOffsets(this);
				return lineOffsets.Length;
			}
		}

		public SimpleTextImage(string text) => this.text = text ?? throw new ArgumentNullException(nameof(text));

		public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => text.CopyTo(sourceIndex, destination, destinationIndex, count);
		public string GetText(Span span) => text.Substring(span.Start, span.Length);
		public char[] ToCharArray(int startIndex, int length) => text.ToCharArray(startIndex, length);
		public void Write(TextWriter writer, Span span) => writer.Write(GetText(span));
		public ITextImage GetSubText(Span span) => new SimpleTextImage(GetText(span));

		public TextImageLine GetLineFromLineNumber(int lineNumber) {
			if (lineOffsets == null)
				lineOffsets = TextImageUtils.CreateLineOffsets(this);
			TextImageUtils.GetLineInfo(lineOffsets, lineNumber, Length, out int start, out int end, out int lineBreakLength);
			return new TextImageLine(this, lineNumber, new Span(start, end - start), lineBreakLength);
		}

		public TextImageLine GetLineFromPosition(int position) => GetLineFromLineNumber(GetLineNumberFromPosition(position));

		public int GetLineNumberFromPosition(int position) {
			if ((uint)position > (uint)Length)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (lineOffsets == null)
				lineOffsets = TextImageUtils.CreateLineOffsets(this);
			return TextImageUtils.GetLineNumberFromPosition(lineOffsets, position, Length);
		}
	}
}
