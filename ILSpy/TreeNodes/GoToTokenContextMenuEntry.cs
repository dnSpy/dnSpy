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
using System.Globalization;
using System.Linq;
using System.Windows;
using dnlib.DotNet;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes
{
	[ExportContextMenuEntryAttribute(Header = "Go to M_D Token…", Order = 300, Category = "Tokens", InputGestureText = "Ctrl+D")]
	class GoToTokenContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return CanExecute() &&
				(context.SelectedTreeNodes != null || context.TextView != null);
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			Execute();
		}

		static ModuleDefMD GetModule(out TabStateDecompile tabState)
		{
			tabState = MainWindow.Instance.ActiveTabState;
			if (tabState == null)
				return null;
			return ILSpyTreeNode.GetModule(tabState.DecompiledNodes) as ModuleDefMD;
		}

		internal static bool CanExecute()
		{
			TabStateDecompile tabState;
			return GetModule(out tabState) != null;
		}

		internal static void Execute()
		{
			TabStateDecompile tabState;
			var module = GetModule(out tabState);
			if (module == null)
				return;

			var ask = new AskForInput();
			ask.Owner = MainWindow.Instance;
			ask.Title = "Go to MD Token";
			ask.label.Content = "_Metadata token";
			ask.textBox.Text = "";
			ask.ToolTip = "Enter a hexadecimal MD token: 0x06001234 or 0200ABCD";
			ask.ShowDialog();
			if (ask.DialogResult != true)
				return;
			uint token;
			string tokenText = ask.textBox.Text;
			tokenText = tokenText.Trim();
			if (string.IsNullOrEmpty(tokenText))
				return;
			if (tokenText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				tokenText = tokenText.Substring(2);
			if (!uint.TryParse(tokenText, NumberStyles.HexNumber, null, out token)) {
				MessageBox.Show(MainWindow.Instance, string.Format("Invalid hex number: {0}", tokenText));
				return;
			}

			var memberRef = module.ResolveToken(token) as IMemberRef;
			var member = MainWindow.ResolveReference(memberRef);
			if (member == null) {
				if (memberRef == null)
					MessageBox.Show(MainWindow.Instance, string.Format("Invalid metadata token: 0x{0:X8}", token));
				else
					MessageBox.Show(MainWindow.Instance, string.Format("Could not resolve member reference token: 0x{0:X8}", token));
				return;
			}

			MainWindow.Instance.JumpToReference(tabState.TextView, member);
		}
	}
}
