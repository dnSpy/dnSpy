/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Diagnostics;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Event args
	/// </summary>
	public sealed class NotifyDocumentTreeViewCollectionChangedEventArgs : EventArgs {
		/// <summary>
		/// Event type
		/// </summary>
		public NotifyDocumentTreeViewCollection Type { get; private set; }

		/// <summary>
		/// All document nodes
		/// </summary>
		public DsDocumentNode[] Nodes { get; private set; }

		NotifyDocumentTreeViewCollectionChangedEventArgs(DsDocumentNode[] nodes) => Nodes = nodes;

		/// <summary>
		/// Creates a <see cref="NotifyDocumentTreeViewCollection.Clear"/> instance
		/// </summary>
		/// <param name="clearedDocuments">All cleared documents</param>
		/// <returns></returns>
		public static NotifyDocumentTreeViewCollectionChangedEventArgs CreateClear(DsDocumentNode[] clearedDocuments) {
			Debug2.Assert(!(clearedDocuments is null));
			var e = new NotifyDocumentTreeViewCollectionChangedEventArgs(clearedDocuments);
			e.Type = NotifyDocumentTreeViewCollection.Clear;
			return e;
		}

		/// <summary>
		/// Creates a <see cref="NotifyDocumentTreeViewCollection.Add"/> instance
		/// </summary>
		/// <param name="document">Added document</param>
		/// <returns></returns>
		public static NotifyDocumentTreeViewCollectionChangedEventArgs CreateAdd(DsDocumentNode document) {
			Debug2.Assert(!(document is null));
			var e = new NotifyDocumentTreeViewCollectionChangedEventArgs(new DsDocumentNode[] { document });
			e.Type = NotifyDocumentTreeViewCollection.Add;
			return e;
		}

		/// <summary>
		/// Creates a <see cref="NotifyDocumentTreeViewCollection.Remove"/> instance
		/// </summary>
		/// <param name="documents">Removed documents</param>
		/// <returns></returns>
		public static NotifyDocumentTreeViewCollectionChangedEventArgs CreateRemove(DsDocumentNode[] documents) {
			Debug2.Assert(!(documents is null));
			var e = new NotifyDocumentTreeViewCollectionChangedEventArgs(documents);
			e.Type = NotifyDocumentTreeViewCollection.Remove;
			return e;
		}
	}
}
