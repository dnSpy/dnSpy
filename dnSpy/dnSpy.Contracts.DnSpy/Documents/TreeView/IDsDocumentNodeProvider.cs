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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Creates <see cref="DsDocumentNode"/>s. Use <see cref="ExportDsDocumentNodeProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IDsDocumentNodeProvider {
		/// <summary>
		/// Creates a new <see cref="DsDocumentNode"/> instance or returns null
		/// </summary>
		/// <param name="documentTreeView">Document treeview</param>
		/// <param name="owner">Owner node or null if owner is the root node</param>
		/// <param name="document">New document</param>
		/// <returns></returns>
		DsDocumentNode? Create(IDocumentTreeView documentTreeView, DsDocumentNode? owner, IDsDocument document);
	}

	/// <summary>Metadata</summary>
	public interface IDsDocumentNodeProviderMetadata {
		/// <summary>See <see cref="ExportDsDocumentNodeProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDsDocumentNodeProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDsDocumentNodeProviderAttribute : ExportAttribute, IDsDocumentNodeProviderMetadata {
		/// <summary>Constructor</summary>
		public ExportDsDocumentNodeProviderAttribute()
			: base(typeof(IDsDocumentNodeProvider)) {
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
