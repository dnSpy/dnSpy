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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Files.Tabs.DocViewer {
	interface IDocumentViewerServiceImpl : IDocumentViewerService {
		void RaiseAddedEvent(IDocumentViewer documentViewer);
		void RaiseRemovedEvent(IDocumentViewer documentViewer);
		void RaiseNewContentEvent(IDocumentViewer documentViewer, DnSpyTextOutputResult outputResult, IContentType contentType);
	}

	[Export(typeof(IDocumentViewerService))]
	[Export(typeof(IDocumentViewerServiceImpl))]
	sealed class DocumentViewerService : IDocumentViewerServiceImpl {
		readonly Lazy<IDocumentViewerListener, IDocumentViewerListenerMetadata>[] documentViewerListeners;

		public event EventHandler<DocumentViewerAddedEventArgs> Added;
		public event EventHandler<DocumentViewerRemovedEventArgs> Removed;
		public event EventHandler<DocumentViewerNewContentEventArgs> NewContent;

		[ImportingConstructor]
		DocumentViewerService([ImportMany] IEnumerable<Lazy<IDocumentViewerListener, IDocumentViewerListenerMetadata>> documentViewerListeners) {
			this.documentViewerListeners = documentViewerListeners.OrderBy(a => a.Metadata.Order).ToArray();
		}

		void NotifyListeners(DocumentViewerEventArgs e) {
			foreach (var lazy in documentViewerListeners)
				lazy.Value.OnEvent(e);
		}

		public void RaiseAddedEvent(IDocumentViewer documentViewer) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			var e = new DocumentViewerAddedEventArgs(documentViewer);
			NotifyListeners(e);
			Added?.Invoke(this, e);
		}

		public void RaiseRemovedEvent(IDocumentViewer documentViewer) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			var e = new DocumentViewerRemovedEventArgs(documentViewer);
			NotifyListeners(e);
			Removed?.Invoke(this, e);
		}

		public void RaiseNewContentEvent(IDocumentViewer documentViewer, DnSpyTextOutputResult outputResult, IContentType contentType) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			if (outputResult == null)
				throw new ArgumentNullException(nameof(outputResult));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			var e = new DocumentViewerNewContentEventArgs(documentViewer, outputResult, contentType);
			NotifyListeners(e);
			NewContent?.Invoke(this, e);
		}
	}
}
