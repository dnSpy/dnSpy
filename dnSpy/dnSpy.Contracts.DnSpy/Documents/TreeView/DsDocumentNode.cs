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

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// A document node
	/// </summary>
	public abstract class DsDocumentNode : DocumentTreeNodeData {
		/// <summary>
		/// Gets the <see cref="IDsDocument"/> instance
		/// </summary>
		public IDsDocument Document { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="document">Document</param>
		protected DsDocumentNode(IDsDocument document) {
			if (document == null)
				throw new ArgumentNullException(nameof(document));
			this.Document = document;
		}

		/// <summary>
		/// Gets the node path name
		/// </summary>
		public sealed override NodePathName NodePathName => new NodePathName(Guid, (Document.Filename ?? string.Empty).ToUpperInvariant());

		/// <summary>
		/// Gets the <see cref="FilterType"/> to filter this instance
		/// </summary>
		/// <param name="filter">Filter to call</param>
		/// <returns></returns>
		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResult(Document).FilterType;
	}
}
