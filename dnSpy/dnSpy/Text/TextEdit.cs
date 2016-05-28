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
using System.Collections.Generic;
using dnSpy.Contracts.Text;

namespace dnSpy.Text {
	sealed class TextEdit : ITextEdit {
		public bool Canceled { get; private set; }
		ITextSnapshot ITextBufferEdit.Snapshot => TextSnapshot;
		public TextSnapshot TextSnapshot { get; }

		readonly TextBuffer textBuffer;
		readonly List<ITextChange> changes;
		readonly int? reiteratedVersionNumber;
		readonly object editTag;

		public TextEdit(TextBuffer textBuffer, int? reiteratedVersionNumber, object editTag) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			this.textBuffer = textBuffer;
			TextSnapshot = textBuffer.CurrentSnapshot;
			this.changes = new List<ITextChange>();
			this.reiteratedVersionNumber = reiteratedVersionNumber;
			this.editTag = editTag;
		}

		bool hasApplied;
		public ITextSnapshot Apply() {
			if (Canceled || hasApplied)
				throw new InvalidOperationException();
			hasApplied = true;
			textBuffer.ApplyChanges(this, changes, reiteratedVersionNumber, editTag);
			return textBuffer.CurrentSnapshot;
		}

		public void Cancel() => textBuffer.Cancel(this);
		public bool Delete(int startPosition, int charsToDelete) => Delete(new Span(startPosition, charsToDelete));
		public bool Delete(Span deleteSpan) => Replace(deleteSpan, string.Empty);
		public bool Insert(int position, char[] characterBuffer, int startIndex, int length) => Insert(position, new string(characterBuffer, startIndex, length));
		public bool Insert(int position, string text) => Replace(new Span(position, 0), text);
		public bool Replace(int startPosition, int charsToReplace, string replaceWith) => Replace(new Span(startPosition, charsToReplace), replaceWith);
		public bool Replace(Span replaceSpan, string replaceWith) {
			if (Canceled || hasApplied)
				throw new InvalidOperationException();
			if (replaceSpan.End > TextSnapshot.Length)
				throw new ArgumentOutOfRangeException(nameof(replaceSpan));
			if (replaceSpan.Length == 0 && replaceWith.Length == 0)
				return true;
			changes.Add(new TextChange(replaceSpan.Start, TextSnapshot.GetText(replaceSpan), replaceWith));
			return true;
		}

		public void Dispose() {
			if (!Canceled && !hasApplied)
				Cancel();
		}
	}
}
