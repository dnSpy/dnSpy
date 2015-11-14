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

//TODO: All commands that modify the Modules prop must also update the IDnSpyFile.Children prop

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Menus;
using dnSpy.MVVM;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.Module {
	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		void IPlugin.EarlyInit() {
		}

		public void OnLoaded() {
			MainWindow.Instance.TreeView.AddCommandBinding(ApplicationCommands.Delete, new EditMenuHandlerCommandProxy(new RemoveNetModuleFromAssemblyCommand.EditMenuCommand()));
			Utils.InstallSettingsCommand(new ModuleSettingsCommand.EditMenuCommand(), null);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateNetModuleCommand : IUndoCommand {
		const string CMD_NAME = "Create NetModule";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 30)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateNetModuleCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateNetModuleCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 30)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateNetModuleCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateNetModuleCommand.Execute(context.Nodes);
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

			var cmd = new CreateNetModuleCommand(data.CreateNetModuleOptions());
			UndoCommandManager.Instance.Add(cmd);
			MainWindow.Instance.JumpToReference(cmd.asmNodeCreator.AssemblyTreeNode);
		}

		AssemblyTreeNodeCreator asmNodeCreator;

		CreateNetModuleCommand(NetModuleOptions options) {
			var module = ModuleUtils.CreateNetModule(options.Name, options.Mvid, options.ClrVersion);
			this.asmNodeCreator = new AssemblyTreeNodeCreator(MainWindow.Instance.DnSpyFileList.CreateDnSpyFile(module, false));
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			asmNodeCreator.Add();
			UndoCommandManager.Instance.MarkAsModified(asmNodeCreator.AssemblyTreeNode.DnSpyFile);
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
		[ExportMenuItem(Header = CMD_NAME, Icon = "ModuleToAssembly", Group = MenuConstants.GROUP_CTX_FILES_ASMED_MISC, Order = 20)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ConvertNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ConvertNetModuleToAssemblyCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME, Icon = "ModuleToAssembly", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_MISC, Order = 20)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ConvertNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ConvertNetModuleToAssemblyCommand.Execute(context.Nodes);
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

				var module = node.DnSpyFile.ModuleDef;
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
				var module = node.DnSpyFile.ModuleDef;
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
		[ExportMenuItem(Header = CMD_NAME, Icon = "AssemblyToModule", Group = MenuConstants.GROUP_CTX_FILES_ASMED_MISC, Order = 30)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ConvertAssemblyToNetModuleCommand.IsVisible(context.Nodes);
			}

			public override bool IsEnabled(AsmEditorContext context) {
				return ConvertAssemblyToNetModuleCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ConvertAssemblyToNetModuleCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME, Icon = "AssemblyToModule", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_MISC, Order = 30)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ConvertAssemblyToNetModuleCommand.IsVisible(context.Nodes);
			}

			public override bool IsEnabled(AsmEditorContext context) {
				return ConvertAssemblyToNetModuleCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ConvertAssemblyToNetModuleCommand.Execute(context.Nodes);
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
					((AssemblyTreeNode)n).DnSpyFile.AssemblyDef.Modules.Count == 1);
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
				var module = node.DnSpyFile.ModuleDef;
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

				var module = node.DnSpyFile.ModuleDef;
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
		internal readonly AssemblyTreeNode modNode;
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
			asmNode.DnSpyFile.AssemblyDef.Modules.Add(modNode.DnSpyFile.ModuleDef);
			asmNode.Children.Add(modNode);
			if (modNodeWasCreated)
				UndoCommandManager.Instance.MarkAsModified(modNode.DnSpyFile);
		}

		public void Undo() {
			Debug.Assert(modNode.Parent != null);
			if (modNode.Parent == null)
				throw new InvalidOperationException();
			asmNode.DnSpyFile.AssemblyDef.Modules.Remove(modNode.DnSpyFile.ModuleDef);
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
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 10)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return AddNewNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				AddNewNetModuleToAssemblyCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 10)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return AddNewNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				AddNewNetModuleToAssemblyCommand.Execute(context.Nodes);
			}
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!AddNetModuleToAssemblyCommand.CanExecute(nodes))
				return;

			var asmNode = (AssemblyTreeNode)nodes[0];
			if (asmNode.Parent is AssemblyTreeNode)
				asmNode = (AssemblyTreeNode)asmNode.Parent;

			var win = new NetModuleOptionsDlg();
			var data = new NetModuleOptionsVM(asmNode.DnSpyFile.ModuleDef);
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var cmd = new AddNewNetModuleToAssemblyCommand((AssemblyTreeNode)nodes[0], data.CreateNetModuleOptions());
			UndoCommandManager.Instance.Add(cmd);
			MainWindow.Instance.JumpToReference(cmd.modNode);
		}

		AddNewNetModuleToAssemblyCommand(AssemblyTreeNode asmNode, NetModuleOptions options)
			: base(asmNode, new AssemblyTreeNode(MainWindow.Instance.DnSpyFileList.CreateDnSpyFile(ModuleUtils.CreateNetModule(options.Name, options.Mvid, options.ClrVersion), false)), true) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class AddExistingNetModuleToAssemblyCommand : AddNetModuleToAssemblyCommand {
		const string CMD_NAME = "Add Existing NetModule to Assembly";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 20)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return AddExistingNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				AddExistingNetModuleToAssemblyCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 20)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return AddExistingNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				AddExistingNetModuleToAssemblyCommand.Execute(context.Nodes);
			}
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!AddNetModuleToAssemblyCommand.CanExecute(nodes))
				return;

			var dialog = new System.Windows.Forms.OpenFileDialog() {
				Filter = PickFilenameConstants.NetModuleFilter,
				RestoreDirectory = true,
			};
			if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;
			if (string.IsNullOrEmpty(dialog.FileName))
				return;

			var asm = MainWindow.Instance.DnSpyFileList.CreateDnSpyFile(dialog.FileName);
			if (asm.ModuleDef == null || asm.AssemblyDef != null) {
				MainWindow.Instance.ShowMessageBox(string.Format("{0} is not a NetModule", asm.Filename), System.Windows.MessageBoxButton.OK);
				var id = asm as IDisposable;
				if (id != null)
					id.Dispose();
				return;
			}

			var cmd = new AddExistingNetModuleToAssemblyCommand((AssemblyTreeNode)nodes[0], asm);
			UndoCommandManager.Instance.Add(cmd);
			MainWindow.Instance.JumpToReference(cmd.modNode);
		}

		AddExistingNetModuleToAssemblyCommand(AssemblyTreeNode asmNode, IDnSpyFile asm)
			: base(asmNode, new AssemblyTreeNode(asm), false) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class RemoveNetModuleFromAssemblyCommand : IUndoCommand2 {
		const string CMD_NAME = "Remove NetModule from Assembly";
		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_FILES_ASMED_DELETE, Order = 10)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return RemoveNetModuleFromAssemblyCommand.IsVisible(context.Nodes);
			}

			public override bool IsEnabled(AsmEditorContext context) {
				return RemoveNetModuleFromAssemblyCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				RemoveNetModuleFromAssemblyCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 10)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return RemoveNetModuleFromAssemblyCommand.IsVisible(context.Nodes);
			}

			public override bool IsEnabled(AsmEditorContext context) {
				return RemoveNetModuleFromAssemblyCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				RemoveNetModuleFromAssemblyCommand.Execute(context.Nodes);
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
			Debug.Assert(asmNode.DnSpyFile.AssemblyDef != null &&
				asmNode.DnSpyFile.AssemblyDef.Modules.IndexOf(modNode.DnSpyFile.ModuleDef) == this.removeIndex);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			Debug.Assert(modNode.Parent != null);
			if (modNode.Parent == null)
				throw new InvalidOperationException();
			bool b = removeIndex < asmNode.Children.Count && asmNode.Children[removeIndex] == modNode &&
				removeIndex < asmNode.DnSpyFile.AssemblyDef.Modules.Count &&
				asmNode.DnSpyFile.AssemblyDef.Modules[removeIndex] == modNode.DnSpyFile.ModuleDef;
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
			asmNode.DnSpyFile.AssemblyDef.Modules.RemoveAt(removeIndex);
			asmNode.Children.RemoveAt(removeIndex);
			UndoCommandManager.Instance.MarkAsModified(asmNode.DnSpyFile);
		}

		public void Undo() {
			Debug.Assert(modNode.Parent == null);
			if (modNode.Parent != null)
				throw new InvalidOperationException();
			asmNode.DnSpyFile.AssemblyDef.Modules.Insert(removeIndex, modNode.DnSpyFile.ModuleDef);
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
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 10)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ModuleSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ModuleSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 10)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ModuleSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ModuleSettingsCommand.Execute(context.Nodes);
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

			var module = asmNode.DnSpyFile.ModuleDef;
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
			this.origOptions = new ModuleOptions(modNode.DnSpyFile.ModuleDef);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		void WriteModuleOptions(ModuleOptions theOptions) {
			theOptions.CopyTo(modNode.DnSpyFile.ModuleDef);
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
