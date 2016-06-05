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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor.Operations;

namespace dnSpy.Text.Editor.Operations {
	/// <summary>
	/// Default navigator. A word is just one character. This one shouldn't be used at all,
	/// it's just the default one created by <see cref="TextStructureNavigatorSelectorService"/>
	/// if nothing else is found.
	/// </summary>
	sealed class TextStructureNavigator : ITextStructureNavigator {
		public IContentType ContentType { get; }

		readonly ITextBuffer textBuffer;

		public TextStructureNavigator(ITextBuffer textBuffer, IContentType contentType) {
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
			if (currentPosition.Position >= currentPosition.Snapshot.Length)
				return new TextExtent(new SnapshotSpan(currentPosition, currentPosition), true);
			return new TextExtent(new SnapshotSpan(currentPosition, currentPosition + 1), true);
		}

		public SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan) {
			if (activeSpan.Snapshot?.TextBuffer != textBuffer)
				throw new ArgumentException();
			return new SnapshotSpan(activeSpan.Snapshot, 0, activeSpan.Snapshot.Length);
		}

		public SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan) {
			if (activeSpan.Snapshot?.TextBuffer != textBuffer)
				throw new ArgumentException();
			if (activeSpan.IsEmpty || activeSpan.Length != 1)
				return GetSpanOfEnclosing(activeSpan);
			return new SnapshotSpan(activeSpan.Snapshot, 0, activeSpan.Snapshot.Length == 0 ? 0 : 1);
		}

		public SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan) {
			if (activeSpan.Snapshot?.TextBuffer != textBuffer)
				throw new ArgumentException();
			if (activeSpan.IsEmpty || activeSpan.Length != 1)
				return GetSpanOfEnclosing(activeSpan);
			if (activeSpan.Start.Position + 1 >= activeSpan.Snapshot.Length)
				return GetSpanOfEnclosing(activeSpan);
			return new SnapshotSpan(activeSpan.Start + 1, activeSpan.Start + 2);
		}

		public SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan) {
			if (activeSpan.Snapshot?.TextBuffer != textBuffer)
				throw new ArgumentException();
			if (activeSpan.IsEmpty || activeSpan.Length != 1)
				return GetSpanOfEnclosing(activeSpan);
			if (activeSpan.Start.Position == 0)
				return GetSpanOfEnclosing(activeSpan);
			return new SnapshotSpan(activeSpan.Start - 1, activeSpan.Start);
		}
	}
}
