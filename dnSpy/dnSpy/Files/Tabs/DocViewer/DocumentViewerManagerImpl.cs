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
using System.Diagnostics;
using dnSpy.Contracts.Files.Tabs.DocViewer;

namespace dnSpy.Files.Tabs.DocViewer {
	interface IDocumentViewerManagerImpl : IDocumentViewerManager {
		void RaiseAddedEvent(IDocumentViewer documentViewer);
		void RaiseRemovedEvent(IDocumentViewer documentViewer);
		void RaiseNewContentEvent(IDocumentViewer documentViewer, DnSpyTextOutputResult result);
	}

	[Export(typeof(IDocumentViewerManager)), Export(typeof(IDocumentViewerManagerImpl))]
	sealed class DocumentViewerManagerImpl : IDocumentViewerManagerImpl {
		sealed class ListenerInfo {
			public readonly DocumentViewerListener Listener;

			public double Order { get; }

			public void Execute(DocumentViewerEvent @event, IDocumentViewer documentViewer, object data) =>
				Listener(@event, documentViewer, data);

			public ListenerInfo(DocumentViewerListener listener, double order) {
				this.Listener = listener;
				this.Order = order;
			}
		}

		readonly List<ListenerInfo> listeners;

		DocumentViewerManagerImpl() {
			this.listeners = new List<ListenerInfo>();
		}

		static void Sort(List<ListenerInfo> listeners) =>
			listeners.Sort((a, b) => a.Order.CompareTo(b.Order));

		public void Add(DocumentViewerListener listener, double order = double.MaxValue) {
			if (listener == null)
				throw new ArgumentNullException();
			listeners.Add(new ListenerInfo(listener, order));
			Sort(listeners);
		}

		public void Remove(DocumentViewerListener listener) {
			if (listener == null)
				throw new ArgumentNullException();
			for (int i = 0; i < listeners.Count; i++) {
				var info = listeners[i];
				if (info.Listener.Method == listener.Method && info.Listener.Target == listener.Target) {
					listeners.RemoveAt(i);
					return;
				}
			}
			Debug.Fail(string.Format("Couldn't remove a listener: {0}", listener));
		}

		public void RaiseAddedEvent(IDocumentViewer documentViewer) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			foreach (var info in listeners.ToArray())
				info.Execute(DocumentViewerEvent.Added, documentViewer, null);
		}

		public void RaiseRemovedEvent(IDocumentViewer documentViewer) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			foreach (var info in listeners.ToArray())
				info.Execute(DocumentViewerEvent.Removed, documentViewer, null);
		}

		public void RaiseNewContentEvent(IDocumentViewer documentViewer, DnSpyTextOutputResult result) {
			if (documentViewer == null)
				throw new ArgumentNullException(nameof(documentViewer));
			if (result == null)
				throw new ArgumentNullException(nameof(result));
			foreach (var info in listeners.ToArray())
				info.Execute(DocumentViewerEvent.NewContent, documentViewer, result);
		}
	}
}
