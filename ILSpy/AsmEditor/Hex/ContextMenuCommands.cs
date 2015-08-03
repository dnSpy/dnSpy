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

using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.HexEditor;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IPlugin))]
	sealed class HexContextMenuPlugin : IPlugin {
		public void OnLoaded() {
			GoToOffsetContextMenuEntry.OnLoaded();
		}
	}

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

		internal static AssemblyTreeNode GetAssemblyTreeNode(TextViewContext context) {
			if (context.TextView != null)
				return GetActiveAssemblyTreeNode();
			if (context.TreeView == MainWindow.Instance.treeView) {
				return context.SelectedTreeNodes != null &&
					context.SelectedTreeNodes.Length == 1 ?
					context.SelectedTreeNodes[0] as AssemblyTreeNode : null;
            }
			return null;
		}

		static AssemblyTreeNode GetActiveAssemblyTreeNode() {
			var tabState = MainWindow.Instance.GetActiveDecompileTabState();
			if (tabState == null || tabState.DecompiledNodes.Length == 0)
				return null;
			return ILSpyTreeNode.GetNode<AssemblyTreeNode>(tabState.DecompiledNodes[0]);
		}

		static AssemblyTreeNode GetNode(TextViewContext context) {
			return GetAssemblyTreeNode(context);
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
			var node = OpenHexEditorContextMenuEntry.GetAssemblyTreeNode(context);
			if (node == null)
				return null;
			return MainWindow.Instance.GetHexTabState(node) == null ? null : node;
		}
	}

	[ExportContextMenuEntry(Header = "Go to Offset", Order = 520, Category = "Hex", InputGestureText = "Ctrl+G")]
	sealed class GoToOffsetContextMenuEntry : IContextMenuEntry {
		internal static void OnLoaded() {
			MainWindow.Instance.HexBindings.Add(new RoutedCommand("GoToOffset", typeof(HexContextMenuPlugin)),
				(s, e) => Execute(),
				(s, e) => e.CanExecute = CanExecute(),
				ModifierKeys.Control, Key.G);
		}

		static HexTabState GetHexTabState(TextViewContext context) {
			return TabState.GetTabState(context.HexBox) as HexTabState;
		}

		static void Execute() {
			Execute(MainWindow.Instance.ActiveTabState as HexTabState);
		}

		static bool CanExecute() {
			return CanExecute(MainWindow.Instance.ActiveTabState as HexTabState);
		}

		public void Execute(TextViewContext context) {
			Execute(GetHexTabState(context));
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public bool IsVisible(TextViewContext context) {
			return CanExecute(GetHexTabState(context));
		}

		static bool CanExecute(HexTabState tabState) {
			return tabState != null;
		}

		static void Execute(HexTabState tabState) {
			if (!CanExecute(tabState))
				return;

			var hb = tabState.HexBox;
			var data = new GoToOffsetVM(hb.PhysicalToVisibleOffset(hb.CaretPosition.Offset), hb.PhysicalToVisibleOffset(hb.StartOffset), hb.PhysicalToVisibleOffset(hb.EndOffset));
			var win = new GoToOffsetDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			hb.CaretPosition = new HexBoxPosition(hb.VisibleToPhysicalOffset(data.OffsetVM.Value), hb.CaretPosition.Kind, 0);
		}
	}
}
