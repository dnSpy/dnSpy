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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using ICSharpCode.ILSpy.TreeNodes;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.AsmEditor.Assembly
{
	sealed class RemoveAssemblyCommand : IUndoCommand
	{
		const string CMD_NAME = "Remove Assembly";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Images/Delete.png",
								Category = "AsmEd",
								Order = 240)]//TODO: Update Order
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							Menu = "_Edit",
							MenuIcon = "Images/Delete.png",
							MenuCategory = "AsmEd",
							MenuOrder = 2100)]//TODO: Set menu order
		sealed class MainMenuEntry : EditCommand
		{
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
			{
				return RemoveAssemblyCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
			{
				RemoveAssemblyCommand.Execute(nodes);
			}

			protected override void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem)
			{
				if (nodes.Length == 1)
					menuItem.Header = string.Format("Remove {0}", nodes[0].Text);
				else
					menuItem.Header = string.Format("Remove {0} assemblies", nodes.Length);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes)
		{
			return nodes.Length > 0 &&
				nodes.All(n => n is AssemblyTreeNode && !(n.Parent is AssemblyTreeNode));
		}

		static void Execute(ILSpyTreeNode[] nodes)
		{
			if (!CanExecute(nodes))
				return;

			var asmNodes = nodes.Select(a => (AssemblyTreeNode)a).ToArray();
			var modNodes = new HashSet<AssemblyTreeNode>(asmNodes);
			modNodes.AddRange(asmNodes.SelectMany(a => a.IsNetModule ? new AssemblyTreeNode[0] : a.Children.Cast<AssemblyTreeNode>()));
			if (!SaveModule.Saver.AskUserToSaveIfModified(modNodes))
				return;

			UndoCommandManager.Instance.Add(new RemoveAssemblyCommand(asmNodes));
		}

		AssemblyTreeNodeCreator[] savedStates;

		RemoveAssemblyCommand(AssemblyTreeNode[] asmNodes)
		{
			this.savedStates = new AssemblyTreeNodeCreator[asmNodes.Length];
			try {
				for (int i = 0; i < this.savedStates.Length; i++)
					this.savedStates[i] = new AssemblyTreeNodeCreator(asmNodes[i]);
			}
			catch {
				Dispose();
				throw;
			}
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute()
		{
			for (int i = 0; i < savedStates.Length; i++)
				savedStates[i].Remove();
		}

		public void Undo()
		{
			for (int i = savedStates.Length - 1; i >= 0; i--)
				savedStates[i].Add();
		}

		public IEnumerable<ILSpyTreeNode> TreeNodes {
			get {
				foreach (var savedState in savedStates)
					yield return savedState.AssemblyTreeNode;
			}
		}

		public void Dispose()
		{
			if (savedStates != null) {
				foreach (var savedState in savedStates) {
					if (savedState != null)
						savedState.Dispose();
				}
			}
			savedStates = null;
		}
	}

	sealed class AssemblySettingsCommand : IUndoCommand
	{
		const string CMD_NAME = "Assembly Settings";
		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "Images/Settings.png",
								Category = "AsmEd",
								Order = 240)]//TODO: Update Order
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "...",
							Menu = "_Edit",
							MenuIcon = "Images/Settings.png",
							MenuCategory = "AsmEd",
							MenuOrder = 2100)]//TODO: Set menu order
		sealed class MainMenuEntry : EditCommand
		{
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
			{
				return AssemblySettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
			{
				AssemblySettingsCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes)
		{
			return nodes != null &&
				nodes.Length == 1 &&
				nodes[0] is AssemblyTreeNode &&
				((AssemblyTreeNode)nodes[0]).IsAssembly;
		}

		static void Execute(ILSpyTreeNode[] nodes)
		{
			if (!CanExecute(nodes))
				return;

			var asmNode = (AssemblyTreeNode)nodes[0];

			var data = new AssemblyOptionsVM(new AssemblyOptions(asmNode.LoadedAssembly.AssemblyDefinition));
			var win = new AssemblyOptionsDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new AssemblySettingsCommand(asmNode, data.CreateAssemblyOptions()));
		}

		readonly AssemblyTreeNode asmNode;
		readonly AssemblyOptions newOptions;
		readonly AssemblyOptions origOptions;

		AssemblySettingsCommand(AssemblyTreeNode asmNode, AssemblyOptions newOptions)
		{
			this.asmNode = asmNode;
			this.newOptions = newOptions;
			this.origOptions = new AssemblyOptions(asmNode.LoadedAssembly.AssemblyDefinition);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute()
		{
			newOptions.CopyTo(asmNode.LoadedAssembly.AssemblyDefinition);
			asmNode.RaiseUIPropsChanged();
		}

		public void Undo()
		{
			origOptions.CopyTo(asmNode.LoadedAssembly.AssemblyDefinition);
			asmNode.RaiseUIPropsChanged();
		}

		public IEnumerable<ILSpyTreeNode> TreeNodes {
			get { yield return asmNode; }
		}

		public void Dispose()
		{
		}
	}

	sealed class CreateAssemblyCommand : IUndoCommand
	{
		const string CMD_NAME = "Create Assembly";
		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "Images/Assembly.png",
								Category = "AsmEd",
								Order = 240)]//TODO: Update Order
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "...",
							Menu = "_Edit",
							MenuIcon = "Images/Assembly.png",
							MenuCategory = "AsmEd",
							MenuOrder = 2100)]//TODO: Set menu order
		sealed class MainMenuEntry : EditCommand
		{
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
			{
				return CreateAssemblyCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
			{
				CreateAssemblyCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes)
		{
			return nodes != null &&
				nodes.Length == 0 || nodes[0] is AssemblyTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes)
		{
			if (!CanExecute(nodes))
				return;

			var data = new AssemblyOptionsVM(AssemblyOptions.Create("MyAssembly"));
			data.CanShowClrVersion = true;
			var win = new AssemblyOptionsDlg();
			win.Title = "Create Assembly";
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new CreateAssemblyCommand(data.CreateAssemblyOptions()));
		}

		AssemblyTreeNodeCreator asmNodeCreator;

		CreateAssemblyCommand(AssemblyOptions options)
		{
			var module = Module.ModuleUtils.CreateModule(options.Name, Guid.NewGuid(), options.ClrVersion, ModuleKind.Dll);
			var asm = new AssemblyDefUser();
			options.CopyTo(asm);
			asm.Modules.Add(module);
			this.asmNodeCreator = new AssemblyTreeNodeCreator(new LoadedAssembly(MainWindow.Instance.CurrentAssemblyList, module));
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute()
		{
			asmNodeCreator.Add();
			UndoCommandManager.Instance.MarkAsModified(asmNodeCreator.AssemblyTreeNode.LoadedAssembly);
		}

		public void Undo()
		{
			asmNodeCreator.Remove();
		}

		public IEnumerable<ILSpyTreeNode> TreeNodes {
			get { return new ILSpyTreeNode[0]; }
		}

		public void Dispose()
		{
			if (asmNodeCreator != null)
				asmNodeCreator.Dispose();
			asmNodeCreator = null;
		}
	}
}
