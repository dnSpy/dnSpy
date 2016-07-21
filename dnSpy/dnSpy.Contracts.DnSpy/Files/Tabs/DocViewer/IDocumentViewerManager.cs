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

namespace dnSpy.Contracts.Files.Tabs.DocViewer {
	/// <summary>
	/// Called at various times
	/// </summary>
	/// <param name="event">Event</param>
	/// <param name="documentViewer">Document viewer</param>
	/// <param name="data">Data, see <see cref="DocumentViewerEvent"/></param>
	public delegate void DocumentViewerListener(DocumentViewerEvent @event, IDocumentViewer documentViewer, object data);

	/// <summary>
	/// Notifies listeners of <see cref="IDocumentViewer"/> events
	/// </summary>
	public interface IDocumentViewerManager {
		/// <summary>
		/// Adds a listener
		/// </summary>
		/// <param name="listener">Listener</param>
		/// <param name="order">Order, see constants in <see cref="DocumentViewerManagerConstants"/></param>
		void Add(DocumentViewerListener listener, double order = DocumentViewerManagerConstants.ORDER_DEFAULT);

		/// <summary>
		/// Removes a listener
		/// </summary>
		/// <param name="listener">Listener</param>
		void Remove(DocumentViewerListener listener);
	}
}
