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
using ICSharpCode.AvalonEdit.Document;

namespace dnSpy.Text {
	sealed class TextChange : ITextChange {
		readonly int offset;
		readonly ITextSource oldText;
		readonly ITextSource newText;

		public int Delta => newText.TextLength - oldText.TextLength;
		public int NewEnd => offset + newText.TextLength;
		public int NewLength => newText.TextLength;
		public int NewPosition => offset;
		public Span NewSpan => new Span(offset, newText.TextLength);
		public string NewText => newText.Text;
		public int OldEnd => offset + oldText.TextLength;
		public int OldLength => oldText.TextLength;
		public int OldPosition => offset;
		public Span OldSpan => new Span(offset, oldText.TextLength);
		public string OldText => oldText.Text;

		public TextChange(int offset, ITextSource oldText, ITextSource newText) {
			if (oldText == null)
				throw new ArgumentNullException(nameof(oldText));
			if (newText == null)
				throw new ArgumentNullException(nameof(newText));
			this.offset = offset;
			this.oldText = oldText;
			this.newText = newText;
		}

		public TextChange(int offset, string oldText, string newText) {
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset), offset, $"{nameof(offset)} can't be less than 0.");
			if (oldText == null)
				throw new ArgumentNullException(nameof(oldText));
			if (newText == null)
				throw new ArgumentNullException(nameof(newText));
			this.offset = offset;
			this.oldText = new StringTextSource(oldText);
			this.newText = new StringTextSource(newText);
		}

		public override string ToString() => $"old={OldSpan}:'{OldText}' new={NewSpan}:'{NewText}'";
	}
}
