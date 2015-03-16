using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using dnlib.DotNet;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes
{
	[ExportContextMenuEntryAttribute(Header = "G_o to Token...", Order = 300, Category = "Tokens", InputGestureText = "Ctrl+D")]
	class GoToTokenContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return GetModule(context) != null;
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		static ModuleDefMD GetModule(TextViewContext context)
		{
			if (context.SelectedTreeNodes != null)
				return GetModule(context.SelectedTreeNodes);
			if (context.TextView != null)
				return GetModule(MainWindow.Instance.SelectedNodes.ToArray());
			return null;
		}

		public void Execute(TextViewContext context) {
			if (context.SelectedTreeNodes != null)
				Execute(context.SelectedTreeNodes);
			else if (context.TextView != null)
				Execute(MainWindow.Instance.SelectedNodes.ToArray());
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

		internal static void Execute(SharpTreeNode[] nodes)
		{
			var module = GetModule(nodes);
			if (module == null)
				return;

			var ask = new AskForInput();
			ask.Owner = MainWindow.Instance;
			ask.Title = "Go to Token";
			ask.textBlock.Text = "Enter token";
			ask.textBox.Text = "";
			ask.ToolTip = "Enter a hexadecimal token: 0x06001234 or 0200ABCD";
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

			MainWindow.Instance.JumpToReference(member);
		}
	}
}
