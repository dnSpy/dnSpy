/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using dnSpy.Contracts.Menus;
using dnSpy.Menus;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace dnSpy.TreeNodes {
	[ExportMenuItem(Header = "_Open Containing Folder", Group = MenuConstants.GROUP_CTX_FILES_OTHER, Order = 20)]
	sealed class OpenContainingFolderCtxMenuCommand : MenuItemBase {
		public override bool IsVisible(IMenuItemContext context) {
			return GetFilename(context) != null;
		}

		static string GetFilename(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID))
				return null;
			var nodes = context.FindByType<SharpTreeNode[]>();
			if (nodes == null || nodes.Length != 1)
				return null;
			var asmNode = nodes[0] as AssemblyTreeNode;
			if (asmNode == null)
				return null;
			var filename = asmNode.DnSpyFile.Filename;
			if (!File.Exists(filename))
				return null;
			return filename;
		}

		public override void Execute(IMenuItemContext context) {
			// Known problem: explorer can't show files in the .NET 2.0 GAC.
			var filename = GetFilename(context);
			if (filename == null)
				return;
			var args = string.Format("/select,{0}", filename);
			try {
				Process.Start(new ProcessStartInfo("explorer.exe", args));
			}
			catch (IOException) {
			}
			catch (Win32Exception) {
			}
		}
	}
}
