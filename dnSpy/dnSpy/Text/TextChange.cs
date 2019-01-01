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
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text {
	sealed class TextChange : ITextChange3 {
		readonly int oldOffset;
		readonly int newOffset;
		readonly string oldText;
		readonly string newText;

		public int LineCountDelta => throw new NotImplementedException();//TODO:

		public bool IsOpaque => oldText.Length > 0 && newText.Length > 0;

		public int Delta => newText.Length - oldText.Length;
		public int NewEnd => newOffset + newText.Length;
		public int NewLength => newText.Length;
		public int NewPosition => newOffset;
		public Span NewSpan => new Span(newOffset, newText.Length);
		public string NewText => newText;

		public int OldEnd => oldOffset + oldText.Length;
		public int OldLength => oldText.Length;
		public int OldPosition => oldOffset;
		public Span OldSpan => new Span(oldOffset, oldText.Length);
		public string OldText => oldText;

		public TextChange(int offset, string oldText, string newText) {
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			oldOffset = offset;
			newOffset = offset;
			this.oldText = oldText ?? throw new ArgumentNullException(nameof(oldText));
			this.newText = newText ?? throw new ArgumentNullException(nameof(newText));
		}

		public TextChange(int oldOffset, string oldText, int newOffset, string newText) {
			if (oldOffset < 0)
				throw new ArgumentOutOfRangeException(nameof(oldOffset));
			if (newOffset < 0)
				throw new ArgumentOutOfRangeException(nameof(newOffset));
			this.oldOffset = oldOffset;
			this.newOffset = newOffset;
			this.oldText = oldText ?? throw new ArgumentNullException(nameof(oldText));
			this.newText = newText ?? throw new ArgumentNullException(nameof(newText));
		}

		public string GetNewText(Span span) {
			if ((uint)span.End > newText.Length)
				throw new ArgumentOutOfRangeException(nameof(span));
			return newText.Substring(span.Start, span.Length);
		}

		public char GetNewTextAt(int position) {
			if ((uint)position >= newText.Length)
				throw new ArgumentOutOfRangeException(nameof(position));
			return newText[position];
		}

		public string GetOldText(Span span) {
			if ((uint)span.End > oldText.Length)
				throw new ArgumentOutOfRangeException(nameof(span));
			return oldText.Substring(span.Start, span.Length);
		}

		public char GetOldTextAt(int position) {
			if ((uint)position >= oldText.Length)
				throw new ArgumentOutOfRangeException(nameof(position));
			return oldText[position];
		}

		public override string ToString() => $"old={OldSpan}:'{OldText}' new={NewSpan}:'{NewText}'";
	}
}
