// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;
using Microsoft.Win32;
using Mono.Cecil;

namespace TestPlugin
{
	[ExportContextMenuEntry(Header = "_Save Assembly")]
	public class SaveAssembly : IContextMenuEntry
	{
		public bool IsVisible(SharpTreeNode[] selectedNodes)
		{
			return selectedNodes.All(n => n is AssemblyTreeNode);
		}
		
		public bool IsEnabled(SharpTreeNode[] selectedNodes)
		{
			return selectedNodes.Length == 1;
		}
		
		public void Execute(SharpTreeNode[] selectedNodes)
		{
			AssemblyTreeNode node = (AssemblyTreeNode)selectedNodes[0];
			AssemblyDefinition asm = node.LoadedAssembly.AssemblyDefinition as AssemblyDefinition;
			if (asm != null) {
				SaveFileDialog dlg = new SaveFileDialog();
				dlg.FileName = node.LoadedAssembly.FileName;
				dlg.Filter = "Assembly|*.dll;*.exe";
				if (dlg.ShowDialog(MainWindow.Instance) == true) {
					asm.MainModule.Write(dlg.FileName);
				}
			}
		}
	}
}
