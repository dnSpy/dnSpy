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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.HexEditor;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.Hex {
	[Export(typeof(IPlugin))]
	sealed class HexContextMenuPlugin : IPlugin {
		public void OnLoaded() {
			GoToOffsetHexBoxContextMenuEntry.OnLoaded();
		}
	}

	[ExportContextMenuEntry(Header = "Open Hex Editor", Order = 500, Category = "Hex", Icon = "Binary")]
	sealed class OpenHexEditorContextMenuEntry : IContextMenuEntry2 {
		public void Execute(TextViewContext context) {
			var node = GetNode(context);
			if (node != null)
				MainWindow.Instance.OpenOrShowHexBox(node);
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

		public void Initialize(TextViewContext context, MenuItem menuItem) {
			menuItem.Header = MainWindow.Instance.GetHexTabState(GetAssemblyTreeNode(context)) == null ? "Open Hex Editor" : "Show Hex Editor";
		}
	}

	abstract class HexBoxContextMenuEntry : IContextMenuEntry2 {
		public void Execute(TextViewContext context) {
			var tabState = GetHexTabState(context.HexBox);
			if (tabState != null)
				Execute(tabState);
		}

		public void Initialize(TextViewContext context, MenuItem menuItem) {
			var tabState = GetHexTabState(context.HexBox);
			if (tabState != null)
				Initialize(tabState, menuItem);
		}

		public bool IsEnabled(TextViewContext context) {
			var tabState = GetHexTabState(context.HexBox);
			if (tabState != null)
				return IsEnabled(tabState);
			return false;
		}

		public bool IsVisible(TextViewContext context) {
			var tabState = GetHexTabState(context.HexBox);
			if (tabState != null)
				return IsVisible(tabState);
			return false;
		}

		static HexTabState GetHexTabState(HexBox hexBox) {
			return (HexTabState)TabState.GetTabState(hexBox);
		}

		protected abstract void Execute(HexTabState tabState);
		protected virtual void Initialize(HexTabState tabState, MenuItem menuItem) {
		}
		protected virtual bool IsEnabled(HexTabState tabState) {
			return IsVisible(tabState);
		}
		protected abstract bool IsVisible(HexTabState tabState);
	}

	[ExportContextMenuEntry(Header = "Go to Offset…", Order = 100, Category = "Misc", InputGestureText = "Ctrl+G")]
	sealed class GoToOffsetHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		internal static void OnLoaded() {
			MainWindow.Instance.HexBindings.Add(new RoutedCommand("GoToOffset", typeof(GoToOffsetHexBoxContextMenuEntry)),
				(s, e) => Execute(),
				(s, e) => e.CanExecute = CanExecute(),
				ModifierKeys.Control, Key.G);
		}

		static HexTabState GetHexTabState(TextViewContext context) {
			return TabState.GetTabState(context.HexBox) as HexTabState;
		}

		static void Execute() {
			Execute2(MainWindow.Instance.ActiveTabState as HexTabState);
		}

		static bool CanExecute() {
			return CanExecute(MainWindow.Instance.ActiveTabState as HexTabState);
		}

		protected override void Execute(HexTabState tabState) {
			Execute2(tabState);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return CanExecute(tabState);
		}

		static bool CanExecute(HexTabState tabState) {
			return tabState != null;
		}

		static void Execute2(HexTabState tabState) {
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

	[ExportContextMenuEntry(Header = "Select…", Order = 110, Category = "Misc")]
	sealed class SelectRangeHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			var hb = tabState.HexBox;
			ulong start = hb.CaretPosition.Offset;
			ulong end = start;
			if (hb.Selection != null) {
				start = hb.Selection.Value.StartOffset;
				end = hb.Selection.Value.EndOffset;
			}
			var data = new SelectVM(hb.PhysicalToVisibleOffset(start), hb.PhysicalToVisibleOffset(end), hb.PhysicalToVisibleOffset(hb.StartOffset), hb.PhysicalToVisibleOffset(hb.EndOffset));
			var win = new SelectDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			hb.Selection = new HexSelection(hb.VisibleToPhysicalOffset(data.StartVM.Value), hb.VisibleToPhysicalOffset(data.EndVM.Value));
			hb.CaretPosition = new HexBoxPosition(hb.VisibleToPhysicalOffset(data.StartVM.Value), hb.CaretPosition.Kind, 0);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}
	}

	[ExportContextMenuEntry(Header = "Use 0x Prefix (offset)", Order = 500, Category = "Options")]
	sealed class UseHexPrefixHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.UseHexPrefix = !(tabState.UseHexPrefix ?? HexSettings.Instance.UseHexPrefix);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override void Initialize(HexTabState tabState, MenuItem menuItem) {
			menuItem.IsChecked = tabState.UseHexPrefix ?? HexSettings.Instance.UseHexPrefix;
		}
	}

	[ExportContextMenuEntry(Header = "Show ASCII", Order = 510, Category = "Options")]
	sealed class ShowAsciiHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.ShowAscii = !(tabState.ShowAscii ?? HexSettings.Instance.ShowAscii);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override void Initialize(HexTabState tabState, MenuItem menuItem) {
			menuItem.IsChecked = tabState.ShowAscii ?? HexSettings.Instance.ShowAscii;
		}
	}

	[ExportContextMenuEntry(Header = "Lower Case Hex", Order = 520, Category = "Options")]
	sealed class LowerCaseHexHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.LowerCaseHex = !(tabState.LowerCaseHex ?? HexSettings.Instance.LowerCaseHex);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override void Initialize(HexTabState tabState, MenuItem menuItem) {
			menuItem.IsChecked = tabState.LowerCaseHex ?? HexSettings.Instance.LowerCaseHex;
		}
	}

	[ExportContextMenuEntry(Header = "Bytes per Line", Order = 530, Category = "Options")]
	sealed class BytesPerLineHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		static readonly Tuple<int?, string>[] subMenus = new Tuple<int?, string>[] {
			Tuple.Create((int?)0, "_Fit to Width"),
			Tuple.Create((int?)8, "_8 Bytes"),
			Tuple.Create((int?)16, "_16 Bytes"),
			Tuple.Create((int?)32, "_32 Bytes"),
			Tuple.Create((int?)48, "_48 Bytes"),
			Tuple.Create((int?)64, "_64 Bytes"),
			Tuple.Create((int?)null, "_Default"),
		};

		protected override void Initialize(HexTabState tabState, MenuItem menuItem) {
			foreach (var info in subMenus) {
				var mi = new MenuItem {
					Header = info.Item2,
					IsChecked = info.Item1 == tabState.BytesPerLine,
				};
				var tmpInfo = info;
				mi.Click += (s, e) => tabState.BytesPerLine = tmpInfo.Item1;
				menuItem.Items.Add(mi);
			}
		}
	}

	[ExportContextMenuEntry(Header = "Settings…", Order = 599, Category = "Options")]
	sealed class LocalSettingsHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			var data = new LocalSettingsVM(new LocalHexSettings(tabState));
			var win = new LocalSettingsDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			data.CreateLocalHexSettings().CopyTo(tabState);
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}
	}

	abstract class CopyBaseHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}

		protected override bool IsEnabled(HexTabState tabState) {
			return tabState.HexBox.Selection != null;
		}
	}

	[ExportContextMenuEntry(Header = "Cop_y", Order = 600, Category = "Copy", Icon = "Copy", InputGestureText = "Ctrl+C")]
	sealed class CopyHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.Copy();
		}
	}

	[ExportContextMenuEntry(Header = "Copy UTF-8 String", Order = 610, Category = "Copy", InputGestureText = "Ctrl+Shift+8")]
	sealed class CopyUtf8StringHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyUTF8String();
		}
	}

	[ExportContextMenuEntry(Header = "Copy Unicode String", Order = 620, Category = "Copy", InputGestureText = "Ctrl+Shift+U")]
	sealed class CopyUnicodeStringHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyUnicodeString();
		}
	}

	[ExportContextMenuEntry(Header = "Copy C# Array", Order = 630, Category = "Copy", InputGestureText = "Ctrl+Shift+P")]
	sealed class CopyCSharpArrayHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyCSharpArray();
		}
	}

	[ExportContextMenuEntry(Header = "Copy VB Array", Order = 640, Category = "Copy", InputGestureText = "Ctrl+Shift+B")]
	sealed class CopyVBArrayHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyVBArray();
		}
	}

	[ExportContextMenuEntry(Header = "Copy UI Contents", Order = 650, Category = "Copy", InputGestureText = "Ctrl+Shift+C")]
	sealed class CopyUIContentsHexBoxContextMenuEntry : CopyBaseHexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyUIContents();
		}
	}

	[ExportContextMenuEntry(Header = "Copy Offset", Order = 660, Category = "Copy", InputGestureText = "Ctrl+Alt+A")]
	sealed class CopyOffsetHexBoxContextMenuEntry : HexBoxContextMenuEntry {
		protected override void Execute(HexTabState tabState) {
			tabState.HexBox.CopyAddress();
		}

		protected override bool IsVisible(HexTabState tabState) {
			return true;
		}
	}
}
