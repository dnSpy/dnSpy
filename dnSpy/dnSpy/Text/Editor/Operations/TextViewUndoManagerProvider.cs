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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Operations;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Editor.Operations {
	[Export(typeof(ITextViewUndoManagerProvider))]
	sealed class TextViewUndoManagerProvider : ITextViewUndoManagerProvider {
		static readonly object textViewUndoManagerKey = new object();

		readonly ITextBufferUndoManagerProvider textBufferUndoManagerProvider;

		[ImportingConstructor]
		TextViewUndoManagerProvider(ITextBufferUndoManagerProvider textBufferUndoManagerProvider) {
			this.textBufferUndoManagerProvider = textBufferUndoManagerProvider;
		}

		public ITextViewUndoManager GetTextViewUndoManager(IDsWpfTextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return textView.Properties.GetOrCreateSingletonProperty(textViewUndoManagerKey, () => new TextViewUndoManager(textView, this, textBufferUndoManagerProvider));
		}

		public bool TryGetTextViewUndoManager(IDsWpfTextView textView, out ITextViewUndoManager manager) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			return textView.Properties.TryGetProperty(textViewUndoManagerKey, out manager);
		}

		public void RemoveTextViewUndoManager(IDsWpfTextView textView) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			TextViewUndoManager manager;
			if (!textView.Properties.TryGetProperty(textViewUndoManagerKey, out manager))
				return;
			textView.Properties.RemoveProperty(textViewUndoManagerKey);
			manager.Dispose();
		}
	}
}
