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

using dndbg.Engine;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.Debugger.IMModules {
	[ExportContextMenuEntry(Header = "Reload All Method Bodies", Icon = "Refresh", Order = 600, Category = "DebugRT")]
	sealed class ReloadAllMethodBodiesContextMenuEntry : IContextMenuEntry {
		public void Execute(ContextMenuEntryContext context) {
			var file = GetFile(context);
			if (file != null)
				InMemoryModuleManager.Instance.UpdateModuleMemory(file);
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			return IsVisible(context);
		}

		public bool IsVisible(ContextMenuEntryContext context) {
			return GetFile(context) != null;
		}

		static MemoryModuleDefFile GetFile(ContextMenuEntryContext context) {
			var modNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(GetTreeNode(context));
			var mfile = modNode == null ? null : modNode.DnSpyFile as MemoryModuleDefFile;
			if (mfile == null)
				return null;
			if (mfile.Process.HasExited || mfile.Process.Debugger.ProcessState == DebuggerProcessState.Terminated)
				return null;
			return mfile;
		}

		static ILSpyTreeNode GetTreeNode(ContextMenuEntryContext context) {
			if (context.Element is DecompilerTextView)
				return MainWindow.Instance.treeView.SelectedItem as ILSpyTreeNode;
			if (context.SelectedTreeNodes != null && context.SelectedTreeNodes.Length != 0)
				return context.SelectedTreeNodes[0] as ILSpyTreeNode;
			return null;
		}
	}
}
