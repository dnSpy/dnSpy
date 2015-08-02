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

using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.Hex {
	[ExportContextMenuEntry(Header = "Open Hex Editor", Order = 500, Category = "Hex")]
	sealed class OpenHexEditorContextMenuEntry : IContextMenuEntry {
		public void Execute(TextViewContext context) {
			var node = GetNode(context);
			if (node != null)
				MainWindow.Instance.OpenHexBox(node);
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public bool IsVisible(TextViewContext context) {
			var node = GetNode(context);
			return node != null && !string.IsNullOrEmpty(node.LoadedAssembly.FileName);
		}

		static AssemblyTreeNode GetNode(TextViewContext context) {
			return context.TreeView == MainWindow.Instance.treeView &&
				context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Length == 1 ?
				context.SelectedTreeNodes[0] as AssemblyTreeNode : null;
		}
	}

	[ExportContextMenuEntry(Header = "Show Hex Editor", Order = 510, Category = "Hex")]
	sealed class ShowHexEditorContextMenuEntry : IContextMenuEntry {
		public void Execute(TextViewContext context) {
			var node = GetNode(context);
			if (node != null)
				MainWindow.Instance.ShowHexBox(node);
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public bool IsVisible(TextViewContext context) {
			var node = GetNode(context);
			return node != null && !string.IsNullOrEmpty(node.LoadedAssembly.FileName);
		}

		static AssemblyTreeNode GetNode(TextViewContext context) {
			var node = context.TreeView == MainWindow.Instance.treeView &&
				context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Length == 1 ?
				context.SelectedTreeNodes[0] as AssemblyTreeNode : null;
			if (node == null)
				return null;
			return MainWindow.Instance.GetHexTabState(node) == null ? null : node;
		}
	}
}
