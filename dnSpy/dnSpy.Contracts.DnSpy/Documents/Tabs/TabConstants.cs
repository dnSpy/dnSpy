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

using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Constants
	/// </summary>
	public static class TabConstants {
		/// <summary>
		/// Order of hex <see cref="IDocumentTabContentFactory"/> instance
		/// </summary>
		public const double ORDER_HEXDOCUMENTTABCONTENTFACTORY = 10000;

		/// <summary>
		/// Order of hex editor <see cref="IDocumentTabContentFactory"/> instance
		/// </summary>
		public const double ORDER_ASMED_HEXVIEWDOCUMENTTABCONTENTFACTORY = 11000;

		/// <summary>
		/// Order of decompile <see cref="IDocumentTabContentFactory"/> instance
		/// </summary>
		public const double ORDER_DECOMPILEDOCUMENTTABCONTENTFACTORY = double.MaxValue;

		/// <summary>
		/// Order of default <see cref="IDecompileNode"/> instance
		/// </summary>
		public const double ORDER_DEFAULTDECOMPILENODE = double.MaxValue;

		/// <summary>
		/// Order of <see cref="IDocumentTabUIContextProvider"/> instance that creates <see cref="IDocumentViewer"/> instances
		/// </summary>
		public const double ORDER_DOCUMENTVIEWERPROVIDER = double.MaxValue;

		/// <summary>
		/// Order of dnlib reference <see cref="IDocumentViewerToolTipProvider"/> instance
		/// </summary>
		public const double ORDER_DNLIBREFTOOLTIPCONTENTPROVIDER = double.MaxValue;

		/// <summary>
		/// Order of hex <see cref="ITabSaverProvider"/> instance
		/// </summary>
		public const double ORDER_HEXTABSAVERPROVIDER = 2000;

		/// <summary>
		/// Order of hex <see cref="TokenReference"/> <see cref="IReferenceDocumentTabContentProvider"/> instance
		/// </summary>
		public const double ORDER_CONTENTPROVIDER_HEXTOKENREF = 1000;

		/// <summary>
		/// Order of hex <see cref="AddressReference"/> <see cref="IReferenceDocumentTabContentProvider"/> instance
		/// </summary>
		public const double ORDER_CONTENTPROVIDER_HEXADDRREF = 2000;

		/// <summary>
		/// Order of default <see cref="IReferenceDocumentTabContentProvider"/> instance
		/// </summary>
		public const double ORDER_CONTENTPROVIDER_TEXTREF = 10000;

		/// <summary>
		/// Order of <see cref="IReferenceDocumentTabContentProvider"/> instance that creates content
		/// from <see cref="DocumentTreeNodeData"/> nodes.
		/// </summary>
		public const double ORDER_CONTENTPROVIDER_NODE = 20000;
	}
}
