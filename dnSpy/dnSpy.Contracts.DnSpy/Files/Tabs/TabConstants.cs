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

using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Contracts.Files.Tabs {
	/// <summary>
	/// Constants
	/// </summary>
	public static class TabConstants {
		/// <summary>
		/// Order of hex <see cref="IFileTabContentFactory"/> instance
		/// </summary>
		public const double ORDER_HEXFILETABCONTENTFACTORY = 10000;

		/// <summary>
		/// Order of hex editor <see cref="IFileTabContentFactory"/> instance
		/// </summary>
		public const double ORDER_HEXBOXFILETABCONTENTFACTORY = 11000;

		/// <summary>
		/// Order of decompile <see cref="IFileTabContentFactory"/> instance
		/// </summary>
		public const double ORDER_DECOMPILEFILETABCONTENTFACTORY = double.MaxValue;

		/// <summary>
		/// Order of default <see cref="IDecompileNode"/> instance
		/// </summary>
		public const double ORDER_DEFAULTDECOMPILENODE = double.MaxValue;

		/// <summary>
		/// Order of <see cref="IFileTabUIContextCreator"/> instance that creates <see cref="ITextEditorUIContext"/> instances
		/// </summary>
		public const double ORDER_TEXTEDITORUICONTEXTCREATOR = double.MaxValue;

		/// <summary>
		/// Order of dnlib reference <see cref="IToolTipContentCreator"/> instance
		/// </summary>
		public const double ORDER_DNLIBREFTOOLTIPCONTENTCREATOR = double.MaxValue;

		/// <summary>
		/// Order of default <see cref="ITabSaverCreator"/> instance
		/// </summary>
		public const double ORDER_DEFAULTTABSAVERCREATOR = double.MaxValue;

		/// <summary>
		/// Order of baml <see cref="ITabSaverCreator"/> instance
		/// </summary>
		public const double ORDER_BAMLTABSAVERCREATOR = 1000;

		/// <summary>
		/// Order of hex <see cref="ITabSaverCreator"/> instance
		/// </summary>
		public const double ORDER_HEXTABSAVERCREATOR = 2000;

		/// <summary>
		/// Order of hex <see cref="TokenReference"/> <see cref="IReferenceFileTabContentCreator"/> instance
		/// </summary>
		public const double ORDER_CONTENTCREATOR_HEXTOKENREF = 1000;

		/// <summary>
		/// Order of hex <see cref="AddressReference"/> <see cref="IReferenceFileTabContentCreator"/> instance
		/// </summary>
		public const double ORDER_CONTENTCREATOR_HEXADDRREF = 2000;

		/// <summary>
		/// Order of default <see cref="IReferenceFileTabContentCreator"/> instance
		/// </summary>
		public const double ORDER_CONTENTCREATOR_TEXTREF = 10000;

		/// <summary>
		/// Order of <see cref="IReferenceFileTabContentCreator"/> instance that creates content
		/// from <see cref="IFileTreeNodeData"/> nodes.
		/// </summary>
		public const double ORDER_CONTENTCREATOR_NODE = 20000;
	}
}
