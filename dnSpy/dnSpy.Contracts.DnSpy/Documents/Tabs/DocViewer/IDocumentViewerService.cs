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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// Notifies listeners when certain <see cref="IDocumentViewer"/> events occur. You can
	/// manually import this instance and hook the events or you can export an <see cref="IDocumentViewerListener"/>
	/// </summary>
	public interface IDocumentViewerService {
		/// <summary>
		/// Raised when a new <see cref="IDocumentViewer"/> instance has been created
		/// </summary>
		event EventHandler<DocumentViewerAddedEventArgs> Added;

		/// <summary>
		/// Raised when a <see cref="IDocumentViewer"/> instance has been closed
		/// </summary>
		event EventHandler<DocumentViewerRemovedEventArgs> Removed;

		/// <summary>
		/// Raised when the <see cref="IDocumentViewer"/> instance gets new content
		/// (its <see cref="IDocumentViewer.SetContent(DocumentViewerContent, IContentType)"/>
		/// method was called). It's only raised if the new content is different from the current
		/// content. I.e., calling it twice in a row with the same content won't raise this event
		/// the second time.
		/// </summary>
		event EventHandler<DocumentViewerGotNewContentEventArgs> GotNewContent;
	}

	/// <summary>
	/// Gets notified when a document viewer event occurs. Use <see cref="ExportDocumentViewerListenerAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IDocumentViewerListener {
		/// <summary>
		/// Raised when some event occurred
		/// </summary>
		/// <param name="e">Event arguments</param>
		void OnEvent(DocumentViewerEventArgs e);
	}

	/// <summary>Metadata</summary>
	public interface IDocumentViewerListenerMetadata {
		/// <summary>See <see cref="ExportDocumentViewerListenerAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDocumentViewerListener"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDocumentViewerListenerAttribute : ExportAttribute, IDocumentViewerListenerMetadata {
		/// <summary>Constructor</summary>
		public ExportDocumentViewerListenerAttribute()
			: this(DocumentViewerListenerConstants.ORDER_DEFAULT) {
		}

		/// <summary>Constructor</summary>
		/// <param name="order">Order of this instance, eg. <see cref="DocumentViewerListenerConstants.ORDER_GLYPHTEXTMARKERSERVICE"/></param>
		public ExportDocumentViewerListenerAttribute(double order)
			: base(typeof(IDocumentViewerListener)) {
			Order = order;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}

	/// <summary>
	/// <see cref="IDocumentViewerListener"/> event
	/// </summary>
	public enum DocumentViewerEvent {
		/// <summary>
		/// Raised when a new <see cref="IDocumentViewer"/> instance has been created
		/// </summary>
		Added,

		/// <summary>
		/// Raised when a <see cref="IDocumentViewer"/> instance has been closed
		/// </summary>
		Removed,

		/// <summary>
		/// Raised after the <see cref="IDocumentViewer"/> instance got new content
		/// (its <see cref="IDocumentViewer.SetContent(DocumentViewerContent, IContentType)"/>
		/// method was called). It's only raised if the new content is different from the current
		/// content. I.e., calling it twice in a row with the same content won't raise this event
		/// the second time.
		/// </summary>
		GotNewContent,
	}

	/// <summary>
	/// Document viewer event args base class
	/// </summary>
	public abstract class DocumentViewerEventArgs : EventArgs {
		/// <summary>
		/// Gets the event type
		/// </summary>
		public abstract DocumentViewerEvent EventType { get; }

		/// <summary>
		/// Gets the instance
		/// </summary>
		public IDocumentViewer DocumentViewer { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="documentViewer"><see cref="IDocumentViewer"/> instance</param>
		protected DocumentViewerEventArgs(IDocumentViewer documentViewer) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			DocumentViewer = documentViewer;
		}
	}

	/// <summary>
	/// Document viewer added event args
	/// </summary>
	public sealed class DocumentViewerAddedEventArgs : DocumentViewerEventArgs {
		/// <summary>
		/// Returns the event type, which is <see cref="DocumentViewerEvent.Added"/>
		/// </summary>
		public override DocumentViewerEvent EventType => DocumentViewerEvent.Added;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="documentViewer"><see cref="IDocumentViewer"/> instance</param>
		public DocumentViewerAddedEventArgs(IDocumentViewer documentViewer)
			: base(documentViewer) {
		}
	}

	/// <summary>
	/// Document viewer removed event args
	/// </summary>
	public sealed class DocumentViewerRemovedEventArgs : DocumentViewerEventArgs {
		/// <summary>
		/// Returns the event type, which is <see cref="DocumentViewerEvent.Removed"/>
		/// </summary>
		public override DocumentViewerEvent EventType => DocumentViewerEvent.Removed;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="documentViewer"><see cref="IDocumentViewer"/> instance</param>
		public DocumentViewerRemovedEventArgs(IDocumentViewer documentViewer)
			: base(documentViewer) {
		}
	}

	/// <summary>
	/// New content event args
	/// </summary>
	public sealed class DocumentViewerGotNewContentEventArgs : DocumentViewerEventArgs {
		/// <summary>
		/// Returns the event type, which is <see cref="DocumentViewerEvent.GotNewContent"/>
		/// </summary>
		public override DocumentViewerEvent EventType => DocumentViewerEvent.GotNewContent;

		/// <summary>
		/// New content
		/// </summary>
		public DocumentViewerContent Content { get; }

		/// <summary>
		/// New content type
		/// </summary>
		public IContentType ContentType { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="documentViewer">Document viewer</param>
		/// <param name="content">New content</param>
		/// <param name="contentType">Content type</param>
		public DocumentViewerGotNewContentEventArgs(IDocumentViewer documentViewer, DocumentViewerContent content, IContentType contentType)
			: base(documentViewer) {
			if (content == null)
				throw new ArgumentNullException(nameof(content));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			Content = content;
			ContentType = contentType;
		}
	}
}
