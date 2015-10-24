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
using dnlib.DotNet;
using dnSpy.MVVM;
using dnSpy.Tabs;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.ILSpy.TreeNodes.Analyzer;

namespace dnSpy.TreeNodes {
	[Export(typeof(IPlugin))]
	sealed class GoToTokenPlugin : IPlugin {
		public void OnLoaded() {
			MainWindow.Instance.CodeBindings.Add(new RoutedCommand("GoToToken", typeof(GoToTokenPlugin)),
				(s, e) => GoToTokenContextMenuEntry.Execute(),
				(s, e) => e.CanExecute = GoToTokenContextMenuEntry.CanExecute(),
				ModifierKeys.Control, Key.D);
		}
	}

	[ExportContextMenuEntry(Header = "Go to M_D Token...", Order = 400, Category = "Tokens", InputGestureText = "Ctrl+D")]
	sealed class GoToTokenContextMenuEntry : IContextMenuEntry {
		public bool IsVisible(ContextMenuEntryContext context) {
			if (!CanExecute())
				return false;
			if (context.Element is DecompilerTextView)
				return true;
			if (context.SelectedTreeNodes == null || context.SelectedTreeNodes.Length == 0)
				return false;
			var elem = context.SelectedTreeNodes[0];
			return elem is ILSpyTreeNode || elem is AnalyzerTreeNode;
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			return true;
		}

		public void Execute(ContextMenuEntryContext context) {
			Execute();
		}

		static ITokenResolver GetResolver(out DecompileTabState tabState) {
			tabState = MainWindow.Instance.GetActiveDecompileTabState();
			if (tabState == null)
				return null;
			return ILSpyTreeNode.GetModule(tabState.DecompiledNodes) as ITokenResolver;
		}

		internal static bool CanExecute() {
			DecompileTabState tabState;
			return GetResolver(out tabState) != null;
		}

		internal static void Execute() {
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

		internal static uint? AskForToken(string title) {
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
	}
}
