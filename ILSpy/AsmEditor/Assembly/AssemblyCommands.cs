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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace dnSpy.AsmEditor.Assembly {
	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		public void OnLoaded() {
			MainWindow.Instance.treeView.AddCommandBinding(ApplicationCommands.Delete, new TreeViewCommandProxy(new RemoveAssemblyCommand.TheEditCommand()));
			Utils.InstallSettingsCommand(new AssemblySettingsCommand.TheEditCommand(), null);
		}
	}

	[ExportContextMenuEntryAttribute(Header = "Disable Memory Mapped I/O", Order = 960, Category = "Other")]
	sealed class DisableMemoryMappedIOCommand : IContextMenuEntry {
		public bool IsVisible(ContextMenuEntryContext context) {
			return context.Element == MainWindow.Instance.treeView &&
				context.SelectedTreeNodes.Any(a => GetLoadedAssembly(a) != null);
		}

		static LoadedAssembly GetLoadedAssembly(SharpTreeNode node) {
			var asmNode = node as AssemblyTreeNode;
			if (asmNode == null)
				return null;

			var module = asmNode.LoadedAssembly.ModuleDefinition as ModuleDefMD;
			if (module == null)
				return null;
			if (!module.MetaData.PEImage.IsMemoryMappedIO)
				return null;

			return asmNode.LoadedAssembly;
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			return true;
		}

		public void Execute(ContextMenuEntryContext context) {
			if (context.Element != MainWindow.Instance.treeView)
				return;
			var asms = new List<LoadedAssembly>();
			foreach (var node in context.SelectedTreeNodes) {
				var loadedAsm = GetLoadedAssembly(node);
				if (loadedAsm != null)
					asms.Add(loadedAsm);
			}
			if (asms.Count > 0)
				MainWindow.Instance.DisableMemoryMappedIO(asms);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class RemoveAssemblyCommand : IGCUndoCommand {
		const string CMD_NAME = "Remove Assembly";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 300)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							Menu = "_Edit",
							MenuIcon = "Delete",
							MenuInputGestureText = "Del",
							MenuCategory = "AsmEd",
							MenuOrder = 2100)]
		internal sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return RemoveAssemblyCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				RemoveAssemblyCommand.Execute(nodes);
			}

			protected override void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
				if (nodes.Length == 1)
					menuItem.Header = string.Format("Remove {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
				else
					menuItem.Header = string.Format("Remove {0} assemblies", nodes.Length);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is AssemblyTreeNode && !(n.Parent is AssemblyTreeNode));
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNodes = nodes.Select(a => (AssemblyTreeNode)a).ToArray();
			var modNodes = new HashSet<AssemblyTreeNode>(asmNodes);
			modNodes.AddRange(asmNodes.SelectMany(a => !a.IsAssembly ? new AssemblyTreeNode[0] : a.Children.Cast<AssemblyTreeNode>()));
			if (!SaveModule.Saver.AskUserToSaveIfModified(modNodes))
				return;

			var keepNodes = new List<AssemblyTreeNode>();
			var freeNodes = new List<AssemblyTreeNode>();
			var onlyInRedoHistory = new List<AssemblyTreeNode>();
			foreach (var info in UndoCommandManager.Instance.GetUndoRedoInfo(asmNodes)) {
				if (!info.IsInUndo && !info.IsInRedo) {
					// This asm is safe to remove
					freeNodes.Add(info.Node);
				}
				else if (!info.IsInUndo && info.IsInRedo) {
					// If we add a RemoveAssemblyCommand, the redo history will be cleared, so this
					// assembly will be cleared from the history and don't need to be kept.
					onlyInRedoHistory.Add(info.Node);
				}
				else {
					// The asm is in the undo history, and maybe in the redo history. We must keep it.
					keepNodes.Add(info.Node);
				}
			}

			if (keepNodes.Count > 0 || onlyInRedoHistory.Count > 0) {
				// We can't free the asm since older commands might reference it so we must record
				// it in the history. The user can click Clear History to free everything.
				UndoCommandManager.Instance.Add(new RemoveAssemblyCommand(keepNodes.ToArray()));
				// Redo history was cleared
				FreeAssemblies(onlyInRedoHistory);
			}

			FreeAssemblies(freeNodes);
			if (freeNodes.Count > 0 || onlyInRedoHistory.Count > 0)
				UndoCommandManager.Instance.CallGc();
		}

		AssemblyTreeNodeCreator[] savedStates;

		RemoveAssemblyCommand(AssemblyTreeNode[] asmNodes) {
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

		public void Execute() {
			for (int i = 0; i < savedStates.Length; i++)
				savedStates[i].Remove();
		}

		public void Undo() {
			for (int i = savedStates.Length - 1; i >= 0; i--)
				savedStates[i].Add();
		}

		public IEnumerable<object> ModifiedObjects {
			get {
				foreach (var savedState in savedStates)
					yield return savedState.AssemblyTreeNode;
			}
		}

		public bool CallGarbageCollectorAfterDispose {
			get { return true; }
		}

		public void Dispose() {
			// We don't need to call Dispose() on any deleted ModuleDefs since the
			// UndoCommandManager calls the GC
			if (savedStates != null) {
				foreach (var savedState in savedStates) {
					if (savedState != null)
						savedState.Dispose();
				}
			}
			savedStates = null;
		}

		static void FreeAssemblies(IList<AssemblyTreeNode> nodes) {
			foreach (var node in nodes)
				node.Delete();
		}

		static IEnumerable<AssemblyTreeNode> GetAssemblyNodes(AssemblyTreeNode node) {
			if (!node.IsAssembly || node.Children.Count == 0)
				yield return node;
			else {
				foreach (AssemblyTreeNode child in node.Children)
					yield return child;
			}
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class AssemblySettingsCommand : IUndoCommand {
		const string CMD_NAME = "Edit Assembly";
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								InputGestureText = "Alt+Enter",
								Category = "AsmEd",
								Order = 600)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "Settings",
							MenuInputGestureText = "Alt+Enter",
							MenuCategory = "AsmEd",
							MenuOrder = 2400)]
		internal sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return AssemblySettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				AssemblySettingsCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				nodes.Length == 1 &&
				nodes[0] is AssemblyTreeNode &&
				((AssemblyTreeNode)nodes[0]).IsAssembly;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNode = (AssemblyTreeNode)nodes[0];
			var module = asmNode.LoadedAssembly.ModuleDefinition;

			var data = new AssemblyOptionsVM(new AssemblyOptions(asmNode.LoadedAssembly.AssemblyDefinition), module, MainWindow.Instance.CurrentLanguage);
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
		readonly AssemblyRefInfo[] assemblyRefInfos;

		struct AssemblyRefInfo {
			public readonly AssemblyRef AssemblyRef;
			public readonly UTF8String OrigName;
			public readonly PublicKeyBase OrigPublicKeyOrToken;

			public AssemblyRefInfo(AssemblyRef asmRef) {
				this.AssemblyRef = asmRef;
				this.OrigName = asmRef.Name;
				this.OrigPublicKeyOrToken = asmRef.PublicKeyOrToken;
			}
		}

		AssemblySettingsCommand(AssemblyTreeNode asmNode, AssemblyOptions newOptions) {
			this.asmNode = asmNode;
			this.newOptions = newOptions;
			this.origOptions = new AssemblyOptions(asmNode.LoadedAssembly.AssemblyDefinition);

			if (newOptions.Name != origOptions.Name)
				this.assemblyRefInfos = RefFinder.FindAssemblyRefsToThisModule(asmNode.LoadedAssembly.ModuleDefinition).Where(a => AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(a, asmNode.LoadedAssembly.AssemblyDefinition)).Select(a => new AssemblyRefInfo(a)).ToArray();
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			newOptions.CopyTo(asmNode.LoadedAssembly.AssemblyDefinition);
			if (assemblyRefInfos != null) {
				var pkt = newOptions.PublicKey.Token;
				foreach (var info in assemblyRefInfos) {
					info.AssemblyRef.Name = newOptions.Name;
					if (info.AssemblyRef.PublicKeyOrToken is PublicKeyToken)
						info.AssemblyRef.PublicKeyOrToken = pkt;
					else
						info.AssemblyRef.PublicKeyOrToken = newOptions.PublicKey;
				}
			}
			asmNode.RaiseUIPropsChanged();
		}

		public void Undo() {
			origOptions.CopyTo(asmNode.LoadedAssembly.AssemblyDefinition);
			if (assemblyRefInfos != null) {
				foreach (var info in assemblyRefInfos) {
					info.AssemblyRef.Name = info.OrigName;
					info.AssemblyRef.PublicKeyOrToken = info.OrigPublicKeyOrToken;
				}
			}
			asmNode.RaiseUIPropsChanged();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return asmNode; }
		}

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateAssemblyCommand : IUndoCommand {
		const string CMD_NAME = "Create Assembly";
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewAssembly",
								Category = "AsmEd",
								Order = 500)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewAssembly",
							MenuCategory = "AsmEd",
							MenuOrder = 2300)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateAssemblyCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateAssemblyCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				(nodes.Length == 0 || nodes[0] is AssemblyTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var newModule = new ModuleDefUser();

			var data = new AssemblyOptionsVM(AssemblyOptions.Create("MyAssembly"), newModule, MainWindow.Instance.CurrentLanguage);
			data.CanShowClrVersion = true;
			var win = new AssemblyOptionsDlg();
			win.Title = "Create Assembly";
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new CreateAssemblyCommand(newModule, data.CreateAssemblyOptions()));
		}

		AssemblyTreeNodeCreator asmNodeCreator;

		CreateAssemblyCommand(ModuleDef newModule, AssemblyOptions options) {
			var module = Module.ModuleUtils.CreateModule(options.Name, Guid.NewGuid(), options.ClrVersion, ModuleKind.Dll, newModule);
			options.CreateAssemblyDef(module).Modules.Add(module);
			this.asmNodeCreator = new AssemblyTreeNodeCreator(new LoadedAssembly(MainWindow.Instance.CurrentAssemblyList, module));
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			asmNodeCreator.Add();
			UndoCommandManager.Instance.MarkAsModified(asmNodeCreator.AssemblyTreeNode.LoadedAssembly);
		}

		public void Undo() {
			asmNodeCreator.Remove();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return asmNodeCreator.AssemblyTreeNode; }
		}

		public void Dispose() {
			if (asmNodeCreator != null)
				asmNodeCreator.Dispose();
			asmNodeCreator = null;
		}
	}
}
