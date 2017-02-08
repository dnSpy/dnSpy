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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Operations {
	sealed class AnyTextStructureNavigator : ITextStructureNavigator {
		public IContentType ContentType { get; }

		readonly ITextBuffer textBuffer;

		public AnyTextStructureNavigator(ITextBuffer textBuffer, IContentType contentType) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			this.textBuffer = textBuffer;
			ContentType = contentType;
		}

		public TextExtent GetExtentOfWord(SnapshotPoint currentPosition) {
			if (currentPosition.Snapshot?.TextBuffer != textBuffer)
				throw new ArgumentException();
			WordParser.WordKind kind;
			var span = WordParser.GetWordSpan(currentPosition, out kind);
			return new TextExtent(span, kind != WordParser.WordKind.Whitespace);
		}

		public SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan) {
			if (activeSpan.Snapshot?.TextBuffer != textBuffer)
				throw new ArgumentException();
			throw new NotImplementedException();//TODO:
		}

		public SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan) {
			if (activeSpan.Snapshot?.TextBuffer != textBuffer)
				throw new ArgumentException();
			throw new NotImplementedException();//TODO:
		}

		public SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan) {
			if (activeSpan.Snapshot?.TextBuffer != textBuffer)
				throw new ArgumentException();
			throw new NotImplementedException();//TODO:
		}

		public SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan) {
			if (activeSpan.Snapshot?.TextBuffer != textBuffer)
				throw new ArgumentException();
			throw new NotImplementedException();//TODO:
		}
	}
}
