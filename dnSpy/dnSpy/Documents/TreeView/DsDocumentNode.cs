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
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.Documents.TreeView {
	abstract class DsDocumentNode : DocumentTreeNodeData, IDsDocumentNode {
		public IDsDocument Document { get; }

		protected DsDocumentNode(IDsDocument dsDocument) {
			if (dsDocument == null)
				throw new ArgumentNullException(nameof(dsDocument));
			this.Document = dsDocument;
		}

		public sealed override NodePathName NodePathName => new NodePathName(Guid, (Document.Filename ?? string.Empty).ToUpperInvariant());
		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResult(Document).FilterType;
	}
}
