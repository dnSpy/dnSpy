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
using dnSpy.Contracts.TextEditor;
using ICSharpCode.AvalonEdit.Document;

namespace dnSpy.TextEditor {
	sealed class DnSpyTextBuffer : ITextBuffer, IDisposable {
		public IContentType ContentType {
			get { owner.Dispatcher.VerifyAccess(); return contentType; }
			set {
				owner.Dispatcher.VerifyAccess();
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				if (contentType != value) {
					var oldContentType = contentType;
					contentType = value;
					// ContentType is part of the snapshot, so make sure we create a new one
					currentSnapshot = null;
					RecreateColorizers();
					ContentTypeChanged?.Invoke(this, new ContentTypeChangedEventArgs(oldContentType, contentType));
				}
			}
		}
		IContentType contentType;

		public ITextSnapshot CurrentSnapshot {
			get {
				owner.Dispatcher.VerifyAccess();
				if (currentSnapshot == null)
					currentSnapshot = new DnSpyTextSnapshot(owner.TextArea.TextView.Document.CreateSnapshot(), ContentType, this);
				return currentSnapshot;
			}
		}
		ITextSnapshot currentSnapshot;

		public ITextSnapshotColorizer[] Colorizers { get; private set; }
		public event EventHandler<ContentTypeChangedEventArgs> ContentTypeChanged;

		readonly DnSpyTextEditor owner;
		readonly ITextSnapshotColorizerCreator textBufferColorizerCreator;
		ITextSnapshotColorizer defaultColorizer;

		public DnSpyTextBuffer(DnSpyTextEditor owner, ITextSnapshotColorizerCreator textBufferColorizerCreator, IContentType contentType) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			this.owner = owner;
			OnNewDocument(owner.TextArea.TextView.Document);
			this.owner.TextArea.TextView.DocumentChanged += TextView_DocumentChanged;
			this.textBufferColorizerCreator = textBufferColorizerCreator;
			this.contentType = contentType;
			this.defaultColorizer = null;
			this.Colorizers = Array.Empty<ITextSnapshotColorizer>();
			RecreateColorizers();
		}

		void NewDocument_UpdateFinished(object sender, EventArgs e) => currentSnapshot = null;
		void TextView_DocumentChanged(object sender, EventArgs e) => OnNewDocument(owner.TextArea.TextView.Document);
		void OnNewDocument(TextDocument newDocument) {
			currentSnapshot = null;
			if (currentDocument != null)
				currentDocument.UpdateFinished -= NewDocument_UpdateFinished;
			currentDocument = newDocument;
			if (currentDocument != null)
				currentDocument.UpdateFinished += NewDocument_UpdateFinished;
		}
		TextDocument currentDocument;

		public void SetDefaultColorizer(ITextSnapshotColorizer defaultColorizer) {
			Debug.Assert(this.defaultColorizer == null);
			if (this.defaultColorizer != null)
				throw new InvalidOperationException();
			this.defaultColorizer = defaultColorizer;
			RecreateColorizers();
		}

		public void RecreateColorizers() {
			ClearColorizers();
			var list = new List<ITextSnapshotColorizer>();
			if (defaultColorizer != null)
				list.Add(defaultColorizer);
			list.AddRange(textBufferColorizerCreator.Create(this));
			Colorizers = list.ToArray();
		}

		void ClearColorizers() {
			foreach (var c in Colorizers)
				(c as IDisposable)?.Dispose();
			Colorizers = Array.Empty<ITextSnapshotColorizer>();
		}

		public void Dispose() => ClearColorizers();
	}
}
