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
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Editor.Operations {
	sealed class TextViewUndoManager : ITextViewUndoManager {
		public IDsWpfTextView TextView { get; }
		public ITextUndoHistory TextViewUndoHistory => textBufferUndoManager.TextBufferUndoHistory;

		readonly ITextViewUndoManagerProvider textViewUndoManagerProvider;
		readonly ITextBufferUndoManagerProvider textBufferUndoManagerProvider;
		readonly ITextBufferUndoManager textBufferUndoManager;
		readonly UndoRedoCommandTargetFilter undoRedoCommandTargetFilter;

		public TextViewUndoManager(IDsWpfTextView textView, ITextViewUndoManagerProvider textViewUndoManagerProvider, ITextBufferUndoManagerProvider textBufferUndoManagerProvider) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (textViewUndoManagerProvider == null)
				throw new ArgumentNullException(nameof(textViewUndoManagerProvider));
			if (textBufferUndoManagerProvider == null)
				throw new ArgumentNullException(nameof(textBufferUndoManagerProvider));
			TextView = textView;
			this.textViewUndoManagerProvider = textViewUndoManagerProvider;
			this.textBufferUndoManagerProvider = textBufferUndoManagerProvider;
			textBufferUndoManager = textBufferUndoManagerProvider.GetTextBufferUndoManager(TextView.TextBuffer);
			undoRedoCommandTargetFilter = new UndoRedoCommandTargetFilter(this);
			TextView.CommandTarget.AddFilter(undoRedoCommandTargetFilter, CommandTargetFilterOrder.UndoRedo);
			TextView.Closed += TextView_Closed;
		}

		void TextView_Closed(object sender, EventArgs e) => textViewUndoManagerProvider.RemoveTextViewUndoManager(TextView);

		public void ClearUndoHistory() {
			textBufferUndoManager.UnregisterUndoHistory();
			var dummy = textBufferUndoManager.TextBufferUndoHistory;
		}

		public void Dispose() {
			TextView.Closed -= TextView_Closed;
			textBufferUndoManagerProvider.RemoveTextBufferUndoManager(TextView.TextBuffer);
			TextView.CommandTarget.RemoveFilter(undoRedoCommandTargetFilter);
		}
	}
}
