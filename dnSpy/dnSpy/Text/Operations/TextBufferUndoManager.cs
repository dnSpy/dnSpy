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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Operations {
	sealed class TextBufferUndoManager : ITextBufferUndoManager {
		public ITextBuffer TextBuffer { get; }

		public ITextUndoHistory TextBufferUndoHistory {
			get {
				// In case someone removed it to clear the undo/redo history (by calling UnregisterUndoHistory()),
				// always call RegisterHistory() (it will return a cached instance)
				return textBufferUndoHistory = textUndoHistoryRegistry.RegisterHistory(TextBuffer);
			}
		}
		ITextUndoHistory textBufferUndoHistory;

		static readonly object undoRedoEditTag = new object();
		readonly ITextUndoHistoryRegistry textUndoHistoryRegistry;
		readonly List<ChangeInfo> changes;

		public TextBufferUndoManager(ITextBuffer textBuffer, ITextUndoHistoryRegistry textUndoHistoryRegistry) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (textUndoHistoryRegistry == null)
				throw new ArgumentNullException(nameof(textUndoHistoryRegistry));
			changes = new List<ChangeInfo>();
			TextBuffer = textBuffer;
			this.textUndoHistoryRegistry = textUndoHistoryRegistry;
			textBufferUndoHistory = textUndoHistoryRegistry.RegisterHistory(TextBuffer);
			TextBuffer.Changed += TextBuffer_Changed;
			TextBuffer.PostChanged += TextBuffer_PostChanged;
		}

		void TextBuffer_Changed(object sender, TextContentChangedEventArgs e) {
			if (e.EditTag != undoRedoEditTag && e.Changes.Count > 0)
				changes.Add(new ChangeInfo(e.Changes, e.BeforeVersion.VersionNumber, e.AfterVersion.VersionNumber));
		}

		void TextBuffer_PostChanged(object sender, EventArgs e) {
			if (changes.Count > 0) {
				using (var transaction = TextBufferUndoHistory.CreateTransaction("Text Buffer Change")) {
					foreach (var info in changes)
						transaction.AddUndo(new TextUndoPrimitive(TextBuffer, info, undoRedoEditTag));
					transaction.Complete();
				}
				changes.Clear();
			}
		}

		public void UnregisterUndoHistory() =>
			textUndoHistoryRegistry.RemoveHistory(textBufferUndoHistory);

		public void Dispose() {
			UnregisterUndoHistory();
			TextBuffer.Changed -= TextBuffer_Changed;
			TextBuffer.PostChanged -= TextBuffer_PostChanged;
		}
	}
}
