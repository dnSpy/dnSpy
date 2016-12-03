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
using dnSpy.Text.AvalonEdit;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text {
	sealed class TextChange : ITextChange2 {
		readonly int oldOffset;
		readonly int newOffset;
		readonly ITextSource oldText;
		readonly ITextSource newText;

		public int LineCountDelta {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public bool IsOpaque => false;//TODO:

		public int Delta => newText.TextLength - oldText.TextLength;
		public int NewEnd => newOffset + newText.TextLength;
		public int NewLength => newText.TextLength;
		public int NewPosition => newOffset;
		public Span NewSpan => new Span(newOffset, newText.TextLength);
		public string NewText => newText.Text;

		public int OldEnd => oldOffset + oldText.TextLength;
		public int OldLength => oldText.TextLength;
		public int OldPosition => oldOffset;
		public Span OldSpan => new Span(oldOffset, oldText.TextLength);
		public string OldText => oldText.Text;

		public TextChange(int offset, ITextSource oldText, ITextSource newText) {
			if (oldText == null)
				throw new ArgumentNullException(nameof(oldText));
			if (newText == null)
				throw new ArgumentNullException(nameof(newText));
			oldOffset = offset;
			newOffset = offset;
			this.oldText = oldText;
			this.newText = newText;
		}

		public TextChange(int offset, string oldText, string newText) {
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (oldText == null)
				throw new ArgumentNullException(nameof(oldText));
			if (newText == null)
				throw new ArgumentNullException(nameof(newText));
			oldOffset = offset;
			newOffset = offset;
			this.oldText = new StringTextSource(oldText);
			this.newText = new StringTextSource(newText);
		}

		public TextChange(int oldOffset, string oldText, int newOffset, string newText) {
			if (oldOffset < 0)
				throw new ArgumentOutOfRangeException(nameof(oldOffset));
			if (oldText == null)
				throw new ArgumentNullException(nameof(oldText));
			if (newOffset < 0)
				throw new ArgumentOutOfRangeException(nameof(newOffset));
			if (newText == null)
				throw new ArgumentNullException(nameof(newText));
			this.oldOffset = oldOffset;
			this.newOffset = newOffset;
			this.oldText = new StringTextSource(oldText);
			this.newText = new StringTextSource(newText);
		}

		public override string ToString() => $"old={OldSpan}:'{OldText}' new={NewSpan}:'{NewText}'";
	}
}
