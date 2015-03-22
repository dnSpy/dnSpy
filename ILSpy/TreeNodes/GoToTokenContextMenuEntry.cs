using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using dnlib.DotNet;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes
{
	[ExportContextMenuEntryAttribute(Header = "G_o to MD Token...", Order = 300, Category = "Tokens", InputGestureText = "Ctrl+D")]
	class GoToTokenContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return CanExecute();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			Execute();
		}

		static ModuleDefMD GetModule(out TabState tabState)
		{
			tabState = MainWindow.Instance.ActiveTabState;
			if (tabState == null)
				return null;
			return GetModule(tabState.DecompiledNodes);
		}

		internal static ModuleDefMD GetModule(SharpTreeNode[] nodes)
		{
			if (nodes == null || nodes.Length < 1)
				return null;
			var node = nodes[0];
			while (node != null) {
				var asmNode = node as AssemblyTreeNode;
				if (asmNode != null)
					return asmNode.LoadedAssembly.ModuleDefinition as ModuleDefMD;
				node = node.Parent;
			}
			return null;
		}

		internal static bool CanExecute()
		{
			TabState tabState;
			return GetModule(out tabState) != null;
		}

		internal static void Execute()
		{
			TabState tabState;
			var module = GetModule(out tabState);
			if (module == null)
				return;

			var ask = new AskForInput();
			ask.Owner = MainWindow.Instance;
			ask.Title = "Go to MD Token";
			ask.textBlock.Text = "Metadata token";
			ask.textBox.Text = "";
			ask.ToolTip = "Enter a hexadecimal MD token: 0x06001234 or 0200ABCD";
			ask.ShowDialog();
			if (ask.DialogResult != true)
				return;
			uint token;
			string tokenText = ask.textBox.Text;
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
