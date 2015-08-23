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
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.PE;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.Module {
	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		public void OnLoaded() {
			MainWindow.Instance.treeView.AddCommandBinding(ApplicationCommands.Delete, new TreeViewCommandProxy(new RemoveNetModuleFromAssemblyCommand.TheEditCommand()));
			Utils.InstallSettingsCommand(new ModuleSettingsCommand.TheEditCommand(), null);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateNetModuleCommand : IUndoCommand {
		const string CMD_NAME = "Create NetModule";
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewAssemblyModule",
								Category = "AsmEd",
								Order = 530)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewAssemblyModule",
							MenuCategory = "AsmEd",
							MenuOrder = 2330)]
		sealed class TheEditCommand : EditCommand {
			public TheEditCommand()
				: base(true) {
			}

			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateNetModuleCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateNetModuleCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				(nodes.Length == 0 || nodes.Any(a => a is AssemblyTreeNode));
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var win = new NetModuleOptionsDlg();
			var data = new NetModuleOptionsVM();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new CreateNetModuleCommand(data.CreateNetModuleOptions()));
		}

		AssemblyTreeNodeCreator asmNodeCreator;

		CreateNetModuleCommand(NetModuleOptions options) {
			var module = ModuleUtils.CreateNetModule(options.Name, options.Mvid, options.ClrVersion);
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

	[DebuggerDisplay("{Description}")]
	sealed class ConvertNetModuleToAssemblyCommand : IUndoCommand {
		const string CMD_NAME = "Convert NetModule to Assembly";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "ModuleToAssembly",
								Category = "AsmEd",
								Order = 410)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							Menu = "_Edit",
							MenuIcon = "ModuleToAssembly",
							MenuCategory = "AsmEd",
							MenuOrder = 2210)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return ConvertNetModuleToAssemblyCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				ConvertNetModuleToAssemblyCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				nodes.Length > 0 &&
				nodes.All(n => n is AssemblyTreeNode && ((AssemblyTreeNode)n).IsNetModule);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			UndoCommandManager.Instance.Add(new ConvertNetModuleToAssemblyCommand(nodes));
		}

		readonly AssemblyTreeNode[] nodes;
		SavedState[] savedStates;
		bool hasExecuted;
		sealed class SavedState {
			public ModuleKind ModuleKind;
			public Characteristics Characteristics;
			public AssemblyTreeNode AssemblyTreeNode;
		}

		ConvertNetModuleToAssemblyCommand(ILSpyTreeNode[] nodes) {
			this.nodes = nodes.Select(n => (AssemblyTreeNode)n).ToArray();
			this.savedStates = new SavedState[this.nodes.Length];
			for (int i = 0; i < this.savedStates.Length; i++)
				this.savedStates[i] = new SavedState();
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			Debug.Assert(!hasExecuted);
			if (hasExecuted)
				throw new InvalidOperationException();
			for (int i = 0; i < nodes.Length; i++) {
				var node = nodes[i];
				var savedState = savedStates[i];

				var module = node.LoadedAssembly.ModuleDefinition;
				bool b = module != null && module.Assembly == null;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				savedState.ModuleKind = module.Kind;
				ModuleUtils.AddToNewAssemblyDef(module, ModuleKind.Dll, out savedState.Characteristics);
				node.OnConvertedToAssembly(savedState.AssemblyTreeNode);
			}
			hasExecuted = true;
		}

		public void Undo() {
			Debug.Assert(hasExecuted);
			if (!hasExecuted)
				throw new InvalidOperationException();

			for (int i = nodes.Length - 1; i >= 0; i--) {
				var node = nodes[i];
				var savedState = savedStates[i];
				var module = node.LoadedAssembly.ModuleDefinition;
				bool b = module != null && module.Assembly != null &&
						module.Assembly.Modules.Count == 1 &&
						module.Assembly.ManifestModule == module;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				module.Assembly.Modules.Remove(module);
				module.Kind = savedState.ModuleKind;
				module.Characteristics = savedState.Characteristics;
				savedState.AssemblyTreeNode = node.OnConvertedToNetModule();
			}
			hasExecuted = false;
		}

		public IEnumerable<object> ModifiedObjects {
			get { return nodes; }
		}

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class ConvertAssemblyToNetModuleCommand : IUndoCommand {
		const string CMD_NAME = "Convert Assembly to NetModule";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "AssemblyToModule",
								Category = "AsmEd",
								Order = 420)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							Menu = "_Edit",
							MenuIcon = "AssemblyToModule",
							MenuCategory = "AsmEd",
							MenuOrder = 2220)]
		sealed class TheEditCommand : EditCommand {
			protected override bool IsVisible(ILSpyTreeNode[] nodes) {
				return ConvertAssemblyToNetModuleCommand.IsVisible(nodes);
			}

			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return ConvertAssemblyToNetModuleCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				ConvertAssemblyToNetModuleCommand.Execute(nodes);
			}
		}

		static bool IsVisible(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				nodes.Length > 0 &&
				nodes.All(n => n is AssemblyTreeNode && ((AssemblyTreeNode)n).IsAssembly);
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				nodes.Length > 0 &&
				nodes.All(n => n is AssemblyTreeNode && ((AssemblyTreeNode)n).IsAssembly &&
					((AssemblyTreeNode)n).LoadedAssembly.AssemblyDefinition.Modules.Count == 1);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			UndoCommandManager.Instance.Add(new ConvertAssemblyToNetModuleCommand(nodes));
		}

		readonly AssemblyTreeNode[] nodes;
		SavedState[] savedStates;
		sealed class SavedState {
			public AssemblyDef AssemblyDef;
			public ModuleKind ModuleKind;
			public Characteristics Characteristics;
			public AssemblyTreeNode ModuleNode;
		}

		ConvertAssemblyToNetModuleCommand(ILSpyTreeNode[] nodes) {
			this.nodes = nodes.Select(n => (AssemblyTreeNode)n).ToArray();
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			Debug.Assert(savedStates == null);
			if (savedStates != null)
				throw new InvalidOperationException();
			this.savedStates = new SavedState[this.nodes.Length];
			for (int i = 0; i < nodes.Length; i++) {
				var node = nodes[i];
				savedStates[i] = new SavedState();
				var savedState = savedStates[i];
				var module = node.LoadedAssembly.ModuleDefinition;
				bool b = module != null && module.Assembly != null &&
						module.Assembly.Modules.Count == 1 &&
						module.Assembly.ManifestModule == module;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				node.EnsureChildrenFiltered();
				savedState.AssemblyDef = module.Assembly;
				module.Assembly.Modules.Remove(module);
				savedState.ModuleKind = module.Kind;
				ModuleUtils.WriteNewModuleKind(module, ModuleKind.NetModule, out savedState.Characteristics);
				savedState.ModuleNode = node.OnConvertedToNetModule();
			}
		}

		public void Undo() {
			Debug.Assert(savedStates != null);
			if (savedStates == null)
				throw new InvalidOperationException();

			for (int i = nodes.Length - 1; i >= 0; i--) {
				var node = nodes[i];
				var savedState = savedStates[i];

				var module = node.LoadedAssembly.ModuleDefinition;
				bool b = module != null && module.Assembly == null;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				module.Kind = savedState.ModuleKind;
				module.Characteristics = savedState.Characteristics;
				savedState.AssemblyDef.Modules.Add(module);
				node.OnConvertedToAssembly(savedState.ModuleNode);
			}
			savedStates = null;
		}

		public IEnumerable<object> ModifiedObjects {
			get { return nodes; }
		}

		public void Dispose() {
		}
	}

	abstract class AddNetModuleToAssemblyCommand : IUndoCommand2 {
		internal static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				nodes.Length == 1 &&
				nodes[0] is AssemblyTreeNode &&
				(((AssemblyTreeNode)nodes[0]).IsAssembly || ((AssemblyTreeNode)nodes[0]).IsModuleInAssembly);
		}

		readonly AssemblyTreeNode asmNode;
		readonly AssemblyTreeNode modNode;
		readonly bool modNodeWasCreated;

		protected AddNetModuleToAssemblyCommand(AssemblyTreeNode asmNode, AssemblyTreeNode modNode, bool modNodeWasCreated) {
			if (asmNode.Parent is AssemblyTreeNode)
				asmNode = (AssemblyTreeNode)asmNode.Parent;
			Debug.Assert(!(asmNode.Parent is AssemblyTreeNode));
			Debug.Assert(asmNode.IsAssembly);
			this.asmNode = asmNode;
			this.modNode = modNode;
			this.modNodeWasCreated = modNodeWasCreated;
		}

		public abstract string Description { get; }

		public void Execute() {
			Debug.Assert(modNode.Parent == null);
			if (modNode.Parent != null)
				throw new InvalidOperationException();
			asmNode.EnsureChildrenFiltered();
			asmNode.LoadedAssembly.AssemblyDefinition.Modules.Add(modNode.LoadedAssembly.ModuleDefinition);
			asmNode.Children.Add(modNode);
			if (modNodeWasCreated)
				UndoCommandManager.Instance.MarkAsModified(modNode.LoadedAssembly);
		}

		public void Undo() {
			Debug.Assert(modNode.Parent != null);
			if (modNode.Parent == null)
				throw new InvalidOperationException();
			asmNode.LoadedAssembly.AssemblyDefinition.Modules.Remove(modNode.LoadedAssembly.ModuleDefinition);
			bool b = asmNode.Children.Remove(modNode);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return asmNode; }
		}

		public IEnumerable<object> NonModifiedObjects {
			get {
				if (modNodeWasCreated)
					yield return modNode;
			}
		}

		public void Dispose() {
		}
	}

	sealed class AddNewNetModuleToAssemblyCommand : AddNetModuleToAssemblyCommand {
		const string CMD_NAME = "Add New NetModule to Assembly";
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewAssemblyModule",
								Category = "AsmEd",
								Order = 510)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewAssemblyModule",
							MenuCategory = "AsmEd",
							MenuOrder = 2310)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return AddNetModuleToAssemblyCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				AddNewNetModuleToAssemblyCommand.Execute(nodes);
			}
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!AddNetModuleToAssemblyCommand.CanExecute(nodes))
				return;

			var asmNode = (AssemblyTreeNode)nodes[0];
			if (asmNode.Parent is AssemblyTreeNode)
				asmNode = (AssemblyTreeNode)asmNode.Parent;

			var win = new NetModuleOptionsDlg();
			var data = new NetModuleOptionsVM(asmNode.LoadedAssembly.ModuleDefinition);
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new AddNewNetModuleToAssemblyCommand((AssemblyTreeNode)nodes[0], data.CreateNetModuleOptions()));
		}

		AddNewNetModuleToAssemblyCommand(AssemblyTreeNode asmNode, NetModuleOptions options)
			: base(asmNode, new AssemblyTreeNode(new LoadedAssembly(MainWindow.Instance.CurrentAssemblyList, ModuleUtils.CreateNetModule(options.Name, options.Mvid, options.ClrVersion))), true) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class AddExistingNetModuleToAssemblyCommand : AddNetModuleToAssemblyCommand {
		const string CMD_NAME = "Add Existing NetModule to Assembly";
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewAssemblyModule",
								Category = "AsmEd",
								Order = 520)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewAssemblyModule",
							MenuCategory = "AsmEd",
							MenuOrder = 2320)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return AddNetModuleToAssemblyCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				AddExistingNetModuleToAssemblyCommand.Execute(nodes);
			}
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!AddNetModuleToAssemblyCommand.CanExecute(nodes))
				return;

			var dialog = new System.Windows.Forms.OpenFileDialog() {
				Filter = ".NET NetModules (*.netmodule)|*.netmodule|All files (*.*)|*.*",
				RestoreDirectory = true,
			};
			if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;
			if (string.IsNullOrEmpty(dialog.FileName))
				return;

			var asm = new LoadedAssembly(MainWindow.Instance.CurrentAssemblyList, dialog.FileName);
			if (asm.ModuleDefinition == null || asm.AssemblyDefinition != null) {
				MainWindow.Instance.ShowMessageBox(string.Format("{0} is not a NetModule", asm.FileName), System.Windows.MessageBoxButton.OK);
				asm.TheLoadedFile.Dispose();
				return;
			}

			UndoCommandManager.Instance.Add(new AddExistingNetModuleToAssemblyCommand((AssemblyTreeNode)nodes[0], asm));
		}

		AddExistingNetModuleToAssemblyCommand(AssemblyTreeNode asmNode, LoadedAssembly asm)
			: base(asmNode, new AssemblyTreeNode(asm), false) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class RemoveNetModuleFromAssemblyCommand : IUndoCommand2 {
		const string CMD_NAME = "Remove NetModule from Assembly";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 310)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							Menu = "_Edit",
							MenuIcon = "Delete",
							MenuInputGestureText = "Del",
							MenuCategory = "AsmEd",
							MenuOrder = 2110)]
		internal sealed class TheEditCommand : EditCommand {
			protected override bool IsVisible(ILSpyTreeNode[] nodes) {
				return RemoveNetModuleFromAssemblyCommand.IsVisible(nodes);
			}

			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return RemoveNetModuleFromAssemblyCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				RemoveNetModuleFromAssemblyCommand.Execute(nodes);
			}
		}

		static bool IsVisible(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				nodes.Length == 1 &&
				nodes[0] is AssemblyTreeNode &&
				((AssemblyTreeNode)nodes[0]).IsModuleInAssembly;
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				nodes.Length == 1 &&
				nodes[0] is AssemblyTreeNode &&
				((AssemblyTreeNode)nodes[0]).IsNetModuleInAssembly;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNode = (AssemblyTreeNode)nodes[0];
			if (!SaveModule.Saver.AskUserToSaveIfModified(asmNode))
				return;

			UndoCommandManager.Instance.Add(new RemoveNetModuleFromAssemblyCommand(asmNode));
		}

		readonly AssemblyTreeNode asmNode;
		readonly AssemblyTreeNode modNode;
		readonly int removeIndex;

		RemoveNetModuleFromAssemblyCommand(AssemblyTreeNode modNode) {
			this.asmNode = (AssemblyTreeNode)modNode.Parent;
			Debug.Assert(this.asmNode != null);
			this.modNode = modNode;
			this.removeIndex = asmNode.Children.IndexOf(modNode);
			Debug.Assert(this.removeIndex > 0);
			Debug.Assert(asmNode.LoadedAssembly.AssemblyDefinition != null &&
				asmNode.LoadedAssembly.AssemblyDefinition.Modules.IndexOf(modNode.LoadedAssembly.ModuleDefinition) == this.removeIndex);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			Debug.Assert(modNode.Parent != null);
			if (modNode.Parent == null)
				throw new InvalidOperationException();
			bool b = removeIndex < asmNode.Children.Count && asmNode.Children[removeIndex] == modNode &&
				removeIndex < asmNode.LoadedAssembly.AssemblyDefinition.Modules.Count &&
				asmNode.LoadedAssembly.AssemblyDefinition.Modules[removeIndex] == modNode.LoadedAssembly.ModuleDefinition;
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
			asmNode.LoadedAssembly.AssemblyDefinition.Modules.RemoveAt(removeIndex);
			asmNode.Children.RemoveAt(removeIndex);
			UndoCommandManager.Instance.MarkAsModified(asmNode.LoadedAssembly);
		}

		public void Undo() {
			Debug.Assert(modNode.Parent == null);
			if (modNode.Parent != null)
				throw new InvalidOperationException();
			asmNode.LoadedAssembly.AssemblyDefinition.Modules.Insert(removeIndex, modNode.LoadedAssembly.ModuleDefinition);
			asmNode.Children.Insert(removeIndex, modNode);
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return asmNode; }
		}

		public IEnumerable<object> NonModifiedObjects {
			get { yield return modNode; }
		}

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class ModuleSettingsCommand : IUndoCommand {
		const string CMD_NAME = "Edit Module";
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								InputGestureText = "Alt+Enter",
								Category = "AsmEd",
								Order = 610)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "Settings",
							MenuInputGestureText = "Alt+Enter",
							MenuCategory = "AsmEd",
							MenuOrder = 2410)]
		internal sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return ModuleSettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				ModuleSettingsCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				nodes.Length == 1 &&
				nodes[0] is AssemblyTreeNode &&
				((AssemblyTreeNode)nodes[0]).IsModule;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNode = (AssemblyTreeNode)nodes[0];

			var module = asmNode.LoadedAssembly.ModuleDefinition;
			var data = new ModuleOptionsVM(module, new ModuleOptions(module), MainWindow.Instance.CurrentLanguage);
			var win = new ModuleOptionsDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new ModuleSettingsCommand(asmNode, data.CreateModuleOptions()));
		}

		readonly AssemblyTreeNode modNode;
		readonly ModuleOptions newOptions;
		readonly ModuleOptions origOptions;

		ModuleSettingsCommand(AssemblyTreeNode modNode, ModuleOptions newOptions) {
			this.modNode = modNode;
			this.newOptions = newOptions;
			this.origOptions = new ModuleOptions(modNode.LoadedAssembly.ModuleDefinition);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		void WriteModuleOptions(ModuleOptions theOptions) {
			theOptions.CopyTo(modNode.LoadedAssembly.ModuleDefinition);
			modNode.RaiseUIPropsChanged();
		}

		public void Execute() {
			WriteModuleOptions(newOptions);
		}

		public void Undo() {
			WriteModuleOptions(origOptions);
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return modNode; }
		}

		public void Dispose() {
		}
	}
}
