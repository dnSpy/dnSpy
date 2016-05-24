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
using System.Diagnostics;
using System.Windows.Threading;
using dnSpy.Contracts.Text;
using ICSharpCode.AvalonEdit.Document;

namespace dnSpy.Text {
	sealed class TextBuffer : ITextBuffer, IDisposable {
		public IContentType ContentType {
			get { return contentType; }
			set {
				dispatcher.VerifyAccess();
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (contentType != value) {
					var oldContentType = contentType;
					contentType = value;
					// ContentType is part of the snapshot, so make sure we create a new one
					CreateNewCurrentSnapshot();
					ContentTypeChanged?.Invoke(this, new ContentTypeChangedEventArgs(oldContentType, contentType));
				}
			}
		}
		IContentType contentType;

		void CreateNewCurrentSnapshot() => CurrentSnapshot = new TextSnapshot(Document.CreateSnapshot(), ContentType, this);
		ITextSnapshot ITextBuffer.CurrentSnapshot => CurrentSnapshot;
		public TextSnapshot CurrentSnapshot { get; private set; }

		public event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged;
		public event EventHandler<TextContentChangedEventArgs> Changed;

		public TextDocument Document {
			get { return document; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (document != value) {
					var oldCurrentSnapshot = CurrentSnapshot;
					var oldDocSnapshot = document?.CreateSnapshot();
					if (document != null)
						document.TextChanged -= TextDocument_TextChanged;
					document = value;
					CreateNewCurrentSnapshot();
					document.TextChanged += TextDocument_TextChanged;
					if (oldDocSnapshot != null) {
						Debug.Assert(oldCurrentSnapshot != null);
						Changed?.Invoke(this, new TextContentChangedEventArgs(oldCurrentSnapshot, CurrentSnapshot, new ITextChange[] { new TextChange(0, oldDocSnapshot, document.CreateSnapshot()) }));
					}
				}
			}
		}
		TextDocument document;

		public PropertyCollection Properties { get; }
		readonly Dispatcher dispatcher;

		public TextBuffer(Dispatcher dispatcher, IContentType contentType, string text) {
			if (dispatcher == null)
				throw new ArgumentNullException(nameof(dispatcher));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			Properties = new PropertyCollection();
			this.dispatcher = dispatcher;
			this.Document = new TextDocument(text);
			this.contentType = contentType;
		}

		void TextDocument_TextChanged(object sender, EventArgs e) {
			var beforeSnapshot = CurrentSnapshot;
			CreateNewCurrentSnapshot();
			var afterSnapshot = CurrentSnapshot;
			Changed?.Invoke(this, new TextContentChangedEventArgs(beforeSnapshot, afterSnapshot, afterSnapshot.GetTextChangesFrom(beforeSnapshot)));
		}

		public bool EditInProgress => textEditInProgress != null;
		public bool CheckEditAccess() => dispatcher.CheckAccess();
		TextEdit textEditInProgress;

		public ITextEdit CreateEdit() {
			dispatcher.VerifyAccess();
			if (EditInProgress)
				throw new InvalidOperationException("An edit operation is in progress");
			return textEditInProgress = new TextEdit(this);
		}

		internal void Cancel(TextEdit textEdit) {
			dispatcher.VerifyAccess();
			if (textEdit != textEditInProgress)
				throw new InvalidOperationException();
			textEditInProgress = null;
		}

		public ITextSnapshot Delete(Span deleteSpan) {
			using (var textEdit = CreateEdit()) {
				textEdit.Delete(deleteSpan);
				return textEdit.Apply();
			}
		}

		public ITextSnapshot Insert(int position, string text) {
			using (var textEdit = CreateEdit()) {
				textEdit.Insert(position, text);
				return textEdit.Apply();
			}
		}

		public ITextSnapshot Replace(Span replaceSpan, string replaceWith) {
			using (var textEdit = CreateEdit()) {
				textEdit.Replace(replaceSpan, replaceWith);
				return textEdit.Apply();
			}
		}

		internal void ApplyChanges(TextEdit textEdit, List<ITextChange> changes) {
			dispatcher.VerifyAccess();
			if (textEdit != textEditInProgress)
				throw new InvalidOperationException();
			textEditInProgress = null;

			// This could fail if the user has edited the document, but should normally
			// not happen since code calling CreateEdit() should finish synchronously
			// on the owner thread.
			if (textEdit.TextSnapshot.TextSource != CurrentSnapshot.TextSource)
				throw new InvalidOperationException();

			if (changes.Count != 0) {
				// We don't support overlapping changes. All offsets are relative to the original buffer
				changes.Sort((a, b) => b.OldPosition - a.OldPosition);
				for (int i = 1; i < changes.Count; i++) {
					if (changes[i - 1].OldSpan.OverlapsWith(changes[i].OldSpan))
						throw new InvalidOperationException("Two edit operations overlap");
				}

				using (Document.RunUpdate()) {
					// changes is sorted in reverse order by OldPosition
					foreach (var change in changes)
						Document.Replace(change.OldPosition, change.OldLength, change.NewText);
				}
			}
		}

		public void Dispose() {
			if (document != null)
				document.TextChanged -= TextDocument_TextChanged;
		}
	}
}
