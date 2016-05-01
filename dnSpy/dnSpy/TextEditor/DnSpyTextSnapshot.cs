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

using System.IO;
using dnSpy.Contracts.TextEditor;
using ICSharpCode.AvalonEdit.Document;

namespace dnSpy.TextEditor {
	sealed class DnSpyTextSnapshot : ITextSnapshot {
		readonly ITextSource textSource;

		public char this[int position] => textSource.GetCharAt(position);
		public IContentType ContentType { get; }
		public int Length => textSource.TextLength;
		public ITextBuffer TextBuffer { get; }

		public DnSpyTextSnapshot(ITextSource textSource, IContentType contentType, ITextBuffer textBuffer) {
			this.textSource = textSource;
			ContentType = contentType;
			TextBuffer = textBuffer;
		}

		public string GetText() => textSource.Text;
		public string GetText(Span span) => textSource.GetText(span.Start, span.Length);
		public string GetText(int startIndex, int length) => textSource.GetText(startIndex, length);
		public void Write(TextWriter writer) => textSource.WriteTextTo(writer);
		public void Write(TextWriter writer, Span span) => textSource.WriteTextTo(writer, span.Start, span.Length);
	}
}
