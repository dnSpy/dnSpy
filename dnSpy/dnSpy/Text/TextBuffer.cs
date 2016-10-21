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
using System.Threading;
using dnSpy.Text.AvalonEdit;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text {
	sealed class TextBuffer : ITextBuffer {
		public IContentType ContentType => contentType;
		IContentType contentType;
		TextVersion currentTextVersion;

		public void ChangeContentType(IContentType newContentType, object editTag) {
			VerifyAccess();
			if (newContentType == null)
				throw new ArgumentNullException(nameof(newContentType));
			if (contentType != newContentType) {
				var oldContentType = contentType;
				contentType = newContentType;
				// ContentType is part of the snapshot, so make sure we create a new one
				var beforeSnapshot = CurrentSnapshot;
				CreateNewCurrentSnapshot(Array.Empty<ITextChange>());
				var afterSnapshot = CurrentSnapshot;
				ContentTypeChanged?.Invoke(this, new ContentTypeChangedEventArgs(beforeSnapshot, afterSnapshot, oldContentType, contentType, editTag));
			}
		}

		void CreateNewCurrentSnapshot(IList<ITextChange> changes, int? reiteratedVersionNumber = null, ITextSource afterTextSource = null) {
			// It's null the first time it's called from the ctor
			if (changes != null)
				currentTextVersion = currentTextVersion.SetChanges(changes, reiteratedVersionNumber);
			CurrentSnapshot = new TextSnapshot(afterTextSource ?? Document.CreateSnapshot(), ContentType, this, currentTextVersion);
		}

		ITextSnapshot ITextBuffer.CurrentSnapshot => CurrentSnapshot;
		public TextSnapshot CurrentSnapshot { get; private set; }

		public event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged;
		public event EventHandler<TextContentChangedEventArgs> ChangedHighPriority;
		public event EventHandler<TextContentChangedEventArgs> Changed;
		public event EventHandler<TextContentChangedEventArgs> ChangedLowPriority;
		public event EventHandler<TextContentChangingEventArgs> Changing;
		public event EventHandler PostChanged;
#pragma warning disable 0067
		public event EventHandler<SnapshotSpanEventArgs> ReadOnlyRegionsChanged;//TODO: Use this event
#pragma warning restore 0067

		internal TextDocument Document {
			get { return document; }
			private set {
				if (document != null)
					throw new InvalidOperationException();
				document = value;
				CreateNewCurrentSnapshot(null);
			}
		}
		TextDocument document;

		public PropertyCollection Properties { get; }

		public TextBuffer(IContentType contentType, string text) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			Properties = new PropertyCollection();
			this.contentType = contentType;
			this.currentTextVersion = new TextVersion(this, text?.Length ?? 0, 0, 0);
			this.Document = new TextDocument(text);
			this.Document.SetOwnerThread(null);
		}

		public bool EditInProgress => textEditInProgress != null;
		public bool CheckEditAccess() => CheckAccess();
		TextEdit textEditInProgress;

		public ITextEdit CreateEdit() => CreateEdit(EditOptions.None,  null, null);
		public ITextEdit CreateEdit(EditOptions options, int? reiteratedVersionNumber, object editTag) {
			VerifyAccess();
			if (EditInProgress)
				throw new InvalidOperationException("An edit operation is in progress");
			return textEditInProgress = new TextEdit(this, options, reiteratedVersionNumber, editTag);
		}

		internal void Cancel(TextEdit textEdit) {
			VerifyAccess();
			if (textEdit != textEditInProgress)
				throw new InvalidOperationException();
			textEditInProgress = null;
			PostChanged?.Invoke(this, EventArgs.Empty);
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

		bool RaiseChangingGetIsCanceled(object editTag) {
			var c = Changing;
			if (c == null)
				return false;

			Action<TextContentChangingEventArgs> cancelAction = null;
			var args = new TextContentChangingEventArgs(CurrentSnapshot, editTag, cancelAction);
			foreach (EventHandler<TextContentChangingEventArgs> handler in c.GetInvocationList()) {
				handler(this, args);
				if (args.Canceled)
					break;
			}
			return args.Canceled;
		}

		internal void ApplyChanges(TextEdit textEdit, List<ITextChange> changes, EditOptions options, int? reiteratedVersionNumber, object editTag) {
			VerifyAccess();
			if (textEdit != textEditInProgress)
				throw new InvalidOperationException();
			textEditInProgress = null;

			if (RaiseChangingGetIsCanceled(editTag)) {
				PostChanged?.Invoke(this, EventArgs.Empty);
				return;
			}

			if (changes.Count != 0) {
				// We don't support overlapping changes. All offsets are relative to the original buffer
				changes.Sort((a, b) => b.OldPosition - a.OldPosition);
				for (int i = 1; i < changes.Count; i++) {
					if (changes[i - 1].OldSpan.OverlapsWith(changes[i].OldSpan))
						throw new InvalidOperationException("Two edit operations overlap");
				}

				var beforeSnapshot = CurrentSnapshot;
				Document.BeginUpdate();
				// changes is sorted in reverse order by OldPosition
				foreach (var change in changes)
					Document.Replace(change.OldPosition, change.OldLength, change.NewText);
				Document.EndUpdate();
				CreateNewCurrentSnapshot(changes, reiteratedVersionNumber, Document.CreateSnapshot());
				var afterSnapshot = CurrentSnapshot;

				TextContentChangedEventArgs args = null;
				//TODO: The event handlers are allowed to modify the buffer, but the new events must only be
				//		raised after all of these three events have been raised.
				ChangedHighPriority?.Invoke(this, args ?? (args = new TextContentChangedEventArgs(beforeSnapshot, afterSnapshot, options, editTag)));
				Changed?.Invoke(this, args ?? (args = new TextContentChangedEventArgs(beforeSnapshot, afterSnapshot, options, editTag)));
				ChangedLowPriority?.Invoke(this, args ?? (args = new TextContentChangedEventArgs(beforeSnapshot, afterSnapshot, options, editTag)));
			}
			PostChanged?.Invoke(this, EventArgs.Empty);
		}

		internal bool IsSafeToAccessDocumentFromSnapshot(TextSnapshot snapshot) {
			// If it's not the latest snapshot, Document could be different
			if (snapshot != CurrentSnapshot)
				return false;
			// It's only safe to access Document on the owner thread. If owner thread
			// is null, assume there's only one thread that accesses the text buffer.
			return CheckAccess();
		}

		public void TakeThreadOwnership() {
			if (ownerThread != null && ownerThread != Thread.CurrentThread)
				throw new InvalidOperationException();
			ownerThread = Thread.CurrentThread;
			Document.SetOwnerThread(ownerThread);
		}

		Thread ownerThread;
		bool CheckAccess() => ownerThread == null || ownerThread == Thread.CurrentThread;
		void VerifyAccess() {
			if (!CheckAccess())
				throw new InvalidOperationException();
		}

		public IReadOnlyRegionEdit CreateReadOnlyRegionEdit() {
			throw new NotImplementedException();//TODO:
		}

		public bool IsReadOnly(int position) {
			throw new NotImplementedException();//TODO:
		}

		public bool IsReadOnly(int position, bool isEdit) {
			throw new NotImplementedException();//TODO:
		}

		public bool IsReadOnly(Span span) {
			throw new NotImplementedException();//TODO:
		}

		public bool IsReadOnly(Span span, bool isEdit) {
			throw new NotImplementedException();//TODO:
		}

		public NormalizedSpanCollection GetReadOnlyExtents(Span span) {
			throw new NotImplementedException();//TODO:
		}
	}
}
