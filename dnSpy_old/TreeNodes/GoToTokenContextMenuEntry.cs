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
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Menus;
using dnSpy.Menus;
using dnSpy.MVVM;
using dnSpy.Tabs;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace dnSpy.TreeNodes {
	[Export(typeof(IPlugin))]
	sealed class GoToTokenPlugin : IPlugin {
		void IPlugin.EarlyInit() {
		}

		public void OnLoaded() {
			MainWindow.Instance.CodeBindings.Add(new RoutedCommand("GoToToken", typeof(GoToTokenPlugin)),
				(s, e) => GoToTokenCommand.ExecuteInternal(),
				(s, e) => e.CanExecute = GoToTokenCommand.CanExecuteInternal(),
				ModifierKeys.Control, Key.D);
		}
	}

	public static class GoToTokenCommand {
		static ITokenResolver GetResolver(out DecompileTabState tabState) {
			tabState = MainWindow.Instance.GetActiveDecompileTabState();
			if (tabState == null)
				return null;
			return ILSpyTreeNode.GetModule(tabState.DecompiledNodes) as ITokenResolver;
		}

		internal static bool CanExecuteInternal() {
			DecompileTabState tabState;
			return GetResolver(out tabState) != null;
		}

		internal static void ExecuteInternal() {
			DecompileTabState tabState;
			var resolver = GetResolver(out tabState);
			if (resolver == null)
				return;

			uint? token = AskForToken("Go to MD Token");
			if (token == null)
				return;

			var memberRef = resolver.ResolveToken(token.Value) as IMemberRef;
			var member = MainWindow.ResolveReference(memberRef);
			if (member == null) {
				if (memberRef == null)
					MainWindow.Instance.ShowMessageBox(string.Format("Invalid metadata token: 0x{0:X8}", token.Value));
				else
					MainWindow.Instance.ShowMessageBox(string.Format("Could not resolve member reference token: 0x{0:X8}", token.Value));
				return;
			}

			MainWindow.Instance.JumpToReference(tabState.TextView, member);
		}

		public static uint? AskForToken(string title) {
			var ask = new AskForInput();
			ask.Owner = MainWindow.Instance;
			ask.Title = title;
			ask.label.Content = "_Metadata token";
			ask.textBox.Text = "";
			ask.textBox.ToolTip = "Enter an MD token: 0x06001234 or 0x0200ABCD";
			ask.ShowDialog();
			if (ask.DialogResult != true)
				return null;
			string tokenText = ask.textBox.Text;
			tokenText = tokenText.Trim();
			if (string.IsNullOrEmpty(tokenText))
				return null;

			string error;
			uint token = NumberVMUtils.ParseUInt32(tokenText, uint.MinValue, uint.MaxValue, out error);
			if (!string.IsNullOrEmpty(error)) {
				MainWindow.Instance.ShowMessageBox(error);
				return null;
			}

			return token;
		}

		[ExportMenuItem(Header = "Go to M_D Token...", InputGestureText = "Ctrl+D", Group = MenuConstants.GROUP_CTX_CODE_TOKENS, Order = 0)]
		public sealed class CodeCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID))
					return false;
				if (!CanExecuteInternal())
					return false;
				return true;
			}

			public override void Execute(IMenuItemContext context) {
				ExecuteInternal();
			}
		}

		[ExportMenuItem(Header = "Go to M_D Token...", InputGestureText = "Ctrl+D", Group = MenuConstants.GROUP_CTX_FILES_TOKENS, Order = 0)]
		public sealed class FilesCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID))
					return false;
				if (!CanExecuteInternal())
					return false;
				var nodes = context.FindByType<SharpTreeNode[]>();
				if (nodes == null || nodes.Length == 0)
					return false;
				var elem = nodes[0];
				return elem is ILSpyTreeNode;
			}

			public override void Execute(IMenuItemContext context) {
				ExecuteInternal();
			}
		}
	}
}
