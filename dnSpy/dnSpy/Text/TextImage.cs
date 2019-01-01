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
using dnSpy.Text.AvalonEdit;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text {
	sealed class TextImage : ITextImage {
		internal TextBuffer TextBuffer { get; }
		readonly ITextSource textSource;
		uint[] lineOffsets;

		public char this[int position] => textSource.GetCharAt(position);
		public ITextImageVersion Version { get; }
		public int Length => textSource.TextLength;

		public int LineCount {
			get {
				if (TextBuffer.IsSafeToAccessDocumentFromSnapshot(this))
					return TextBuffer.Document.LineCount;
				if (lineOffsets == null)
					lineOffsets = TextImageUtils.CreateLineOffsets(this);
				return lineOffsets.Length;
			}
		}

		public TextImage(TextBuffer textBuffer, ITextSource textSource, ITextImageVersion version) {
			TextBuffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
			this.textSource = textSource ?? throw new ArgumentNullException(nameof(textSource));
			Version = version ?? throw new ArgumentNullException(nameof(version));
		}

		public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) => textSource.CopyTo(sourceIndex, destination, destinationIndex, count);
		public string GetText(Span span) => textSource.GetText(span.Start, span.Length);
		public char[] ToCharArray(int startIndex, int length) => textSource.ToCharArray(startIndex, length);
		public void Write(TextWriter writer, Span span) => textSource.WriteTextTo(writer, span.Start, span.Length);
		public ITextImage GetSubText(Span span) => new SimpleTextImage(GetText(span));

		public TextImageLine GetLineFromLineNumber(int lineNumber) {
			if (TextBuffer.IsSafeToAccessDocumentFromSnapshot(this)) {
				var docLine = TextBuffer.Document.GetLineByNumber(lineNumber + 1);
				return new TextImageLine(this, lineNumber, new Span(docLine.Offset, docLine.Length), docLine.DelimiterLength);
			}
			if (lineOffsets == null)
				lineOffsets = TextImageUtils.CreateLineOffsets(this);
			TextImageUtils.GetLineInfo(lineOffsets, lineNumber, Length, out int start, out int end, out int lineBreakLength);
			return new TextImageLine(this, lineNumber, new Span(start, end - start), lineBreakLength);
		}

		public TextImageLine GetLineFromPosition(int position) {
			if (TextBuffer.IsSafeToAccessDocumentFromSnapshot(this)) {
				var docLine = TextBuffer.Document.GetLineByOffset(position);
				return new TextImageLine(this, docLine.LineNumber - 1, new Span(docLine.Offset, docLine.Length), docLine.DelimiterLength);
			}
			return GetLineFromLineNumber(GetLineNumberFromPosition(position));
		}

		public int GetLineNumberFromPosition(int position) {
			if ((uint)position > (uint)Length)
				throw new ArgumentOutOfRangeException(nameof(position));
			if (TextBuffer.IsSafeToAccessDocumentFromSnapshot(this))
				return TextBuffer.Document.GetLineByOffset(position).LineNumber - 1;
			if (lineOffsets == null)
				lineOffsets = TextImageUtils.CreateLineOffsets(this);
			return TextImageUtils.GetLineNumberFromPosition(lineOffsets, position, Length);
		}

		internal uint[] GetOrCreateLineOffsets() => lineOffsets ?? (lineOffsets = TextImageUtils.CreateLineOffsets(this));
	}
}
