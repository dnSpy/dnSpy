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

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Finds <see cref="IDocumentTreeNodeData"/> nodes. Use <see cref="ExportDocumentTreeNodeDataFinderAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IDocumentTreeNodeDataFinder {
		/// <summary>
		/// Returns an existing <see cref="IDocumentTreeNodeData"/> node or null
		/// </summary>
		/// <param name="documentTreeView">Owner</param>
		/// <param name="ref">Reference</param>
		/// <returns></returns>
		IDocumentTreeNodeData FindNode(IDocumentTreeView documentTreeView, object @ref);
	}

	/// <summary>Metadata</summary>
	public interface IDocumentTreeNodeDataFinderMetadata {
		/// <summary>See <see cref="ExportDocumentTreeNodeDataFinderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDocumentTreeNodeDataFinder"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDocumentTreeNodeDataFinderAttribute : ExportAttribute, IDocumentTreeNodeDataFinderMetadata {
		/// <summary>Constructor</summary>
		public ExportDocumentTreeNodeDataFinderAttribute()
			: base(typeof(IDocumentTreeNodeDataFinder)) {
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
