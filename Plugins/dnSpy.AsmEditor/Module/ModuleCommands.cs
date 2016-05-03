/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.SaveModule;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Shared.Files;
using dnSpy.Shared.MVVM;

namespace dnSpy.AsmEditor.Module {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, RemoveNetModuleFromAssemblyCommand.EditMenuCommand removeCmd, ModuleSettingsCommand.EditMenuCommand settingsCmd) {
			wpfCommandManager.AddRemoveCommand(removeCmd);
			wpfCommandManager.AddSettingsCommand(fileTabManager, settingsCmd, null);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateNetModuleCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:CreateNetModuleCommand", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 30)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateNetModuleCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateNetModuleCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateNetModuleCommand", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 30)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateNetModuleCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateNetModuleCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) => nodes != null &&
	(nodes.Length == 0 || nodes.Any(a => a is IDnSpyFileNode));

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var win = new NetModuleOptionsDlg();
			var data = new NetModuleOptionsVM();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateNetModuleCommand(undoCommandManager, appWindow.FileTreeView, data.CreateNetModuleOptions());
			undoCommandManager.Value.Add(cmd);
			appWindow.FileTabManager.FollowReference(cmd.fileNodeCreator.DnSpyFileNode);
		}

		readonly RootDnSpyFileNodeCreator fileNodeCreator;
		readonly Lazy<IUndoCommandManager> undoCommandManager;

		CreateNetModuleCommand(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeView fileTreeView, NetModuleOptions options) {
			this.undoCommandManager = undoCommandManager;
			var module = ModuleUtils.CreateNetModule(options.Name, options.Mvid, options.ClrVersion);
			var file = DnSpyDotNetFile.CreateModule(DnSpyFileInfo.CreateFile(string.Empty), module, fileTreeView.FileManager.Settings.LoadPDBFiles);
			this.fileNodeCreator = RootDnSpyFileNodeCreator.CreateModule(fileTreeView, file);
		}

		public string Description => dnSpy_AsmEditor_Resources.CreateNetModuleCommand2;

		public void Execute() {
			fileNodeCreator.Add();
			undoCommandManager.Value.MarkAsModified(undoCommandManager.Value.GetUndoObject(fileNodeCreator.DnSpyFileNode.DnSpyFile));
		}

		public void Undo() => fileNodeCreator.Remove();

		public IEnumerable<object> ModifiedObjects {
			get { yield return fileNodeCreator.DnSpyFileNode; }
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class ConvertNetModuleToAssemblyCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:ConvNetModuleToAssemblyCommand", Icon = "ModuleToAssembly", Group = MenuConstants.GROUP_CTX_FILES_ASMED_MISC, Order = 20)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsVisible(AsmEditorContext context) => ConvertNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ConvertNetModuleToAssemblyCommand.Execute(undoCommandManager, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ConvNetModuleToAssemblyCommand", Icon = "ModuleToAssembly", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_MISC, Order = 20)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeView fileTreeView)
				: base(fileTreeView) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsVisible(AsmEditorContext context) => ConvertNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ConvertNetModuleToAssemblyCommand.Execute(undoCommandManager, context.Nodes);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) =>
			nodes != null &&
			nodes.Length > 0 &&
			nodes.All(n => n is IModuleFileNode &&
					n.TreeNode.Parent != null &&
					!(n.TreeNode.Parent.Data is IAssemblyFileNode));

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			undoCommandManager.Value.Add(new ConvertNetModuleToAssemblyCommand(nodes));
		}

		readonly IModuleFileNode[] nodes;
		SavedState[] savedStates;
		bool hasExecuted;
		sealed class SavedState {
			public ModuleKind ModuleKind;
			public Characteristics Characteristics;
			public IAssemblyFileNode AssemblyFileNode;
		}

		ConvertNetModuleToAssemblyCommand(IFileTreeNodeData[] nodes) {
			this.nodes = nodes.Cast<IModuleFileNode>().ToArray();
			this.savedStates = new SavedState[this.nodes.Length];
			for (int i = 0; i < this.savedStates.Length; i++)
				this.savedStates[i] = new SavedState();
		}

		public string Description => dnSpy_AsmEditor_Resources.ConvNetModuleToAssemblyCommand;

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

				if (savedState.AssemblyFileNode == null) {
					var asmFile = DnSpyDotNetFile.CreateAssembly(node.DnSpyFile);
					savedState.AssemblyFileNode = node.Context.FileTreeView.CreateAssembly(asmFile);
				}

				Debug.Assert(savedState.AssemblyFileNode.DnSpyFile.Children.Count == 1);
				savedState.AssemblyFileNode.DnSpyFile.Children.Remove(node.DnSpyFile);
				Debug.Assert(savedState.AssemblyFileNode.DnSpyFile.Children.Count == 0);
				savedState.AssemblyFileNode.TreeNode.EnsureChildrenLoaded();
				Debug.Assert(savedState.AssemblyFileNode.TreeNode.Children.Count == 0);
				savedState.AssemblyFileNode.DnSpyFile.Children.Add(node.DnSpyFile);

				int index = node.Context.FileTreeView.TreeView.Root.DataChildren.ToList().IndexOf(node);
				b = index >= 0;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				node.Context.FileTreeView.TreeView.Root.Children[index] = savedState.AssemblyFileNode.TreeNode;
				savedState.AssemblyFileNode.TreeNode.AddChild(node.TreeNode);
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

				int index = node.Context.FileTreeView.TreeView.Root.DataChildren.ToList().IndexOf(savedState.AssemblyFileNode);
				bool b = index >= 0;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				savedState.AssemblyFileNode.TreeNode.Children.Remove(node.TreeNode);
				node.Context.FileTreeView.TreeView.Root.Children[index] = node.TreeNode;

				var module = node.DnSpyFile.ModuleDef;
				b = module != null && module.Assembly != null &&
					module.Assembly.Modules.Count == 1 &&
					module.Assembly.ManifestModule == module;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				module.Assembly.Modules.Remove(module);
				module.Kind = savedState.ModuleKind;
				module.Characteristics = savedState.Characteristics;
			}
			hasExecuted = false;
		}

		public IEnumerable<object> ModifiedObjects => nodes;
	}

	[DebuggerDisplay("{Description}")]
	sealed class ConvertAssemblyToNetModuleCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:ConvAssemblyToNetModuleCommand", Icon = "AssemblyToModule", Group = MenuConstants.GROUP_CTX_FILES_ASMED_MISC, Order = 30)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsVisible(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.IsVisible(context.Nodes);
			public override bool IsEnabled(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.Execute(undoCommandManager, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:ConvAssemblyToNetModuleCommand", Icon = "AssemblyToModule", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_MISC, Order = 30)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeView fileTreeView)
				: base(fileTreeView) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsVisible(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.IsVisible(context.Nodes);
			public override bool IsEnabled(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ConvertAssemblyToNetModuleCommand.Execute(undoCommandManager, context.Nodes);
		}

		static bool IsVisible(IFileTreeNodeData[] nodes) =>
			nodes != null &&
			nodes.Length > 0 &&
			nodes.All(n => n is IAssemblyFileNode);

		static bool CanExecute(IFileTreeNodeData[] nodes) =>
			nodes != null &&
			nodes.Length > 0 &&
			nodes.All(n => n is IAssemblyFileNode &&
					((IAssemblyFileNode)n).DnSpyFile.AssemblyDef != null &&
					((IAssemblyFileNode)n).DnSpyFile.AssemblyDef.Modules.Count == 1);

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			undoCommandManager.Value.Add(new ConvertAssemblyToNetModuleCommand(nodes));
		}

		readonly IAssemblyFileNode[] nodes;
		SavedState[] savedStates;
		sealed class SavedState {
			public AssemblyDef AssemblyDef;
			public ModuleKind ModuleKind;
			public Characteristics Characteristics;
			public IModuleFileNode ModuleNode;
		}

		ConvertAssemblyToNetModuleCommand(IFileTreeNodeData[] nodes) {
			this.nodes = nodes.Cast<IAssemblyFileNode>().ToArray();
		}

		public string Description => dnSpy_AsmEditor_Resources.ConvAssemblyToNetModuleCommand;

		public void Execute() {
			Debug.Assert(savedStates == null);
			if (savedStates != null)
				throw new InvalidOperationException();
			this.savedStates = new SavedState[this.nodes.Length];
			for (int i = 0; i < nodes.Length; i++) {
				var node = nodes[i];
				savedStates[i] = new SavedState();
				var savedState = savedStates[i];

				int index = node.TreeNode.Parent.Children.IndexOf(node.TreeNode);
				bool b = index >= 0;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				savedState.ModuleNode = (IModuleFileNode)node.TreeNode.Children.First(a => a.Data is IModuleFileNode).Data;
				node.TreeNode.Children.Remove(savedState.ModuleNode.TreeNode);
				node.TreeNode.Parent.Children[index] = savedState.ModuleNode.TreeNode;

				var module = node.DnSpyFile.ModuleDef;
				b = module != null && module.Assembly != null &&
					module.Assembly.Modules.Count == 1 &&
					module.Assembly.ManifestModule == module;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				node.TreeNode.EnsureChildrenLoaded();
				savedState.AssemblyDef = module.Assembly;
				module.Assembly.Modules.Remove(module);
				savedState.ModuleKind = module.Kind;
				ModuleUtils.WriteNewModuleKind(module, ModuleKind.NetModule, out savedState.Characteristics);
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

				var parent = savedState.ModuleNode.TreeNode.Parent;
				int index = parent.Children.IndexOf(savedState.ModuleNode.TreeNode);
				b = index >= 0;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				parent.Children[index] = node.TreeNode;
				node.TreeNode.AddChild(savedState.ModuleNode.TreeNode);
			}
			savedStates = null;
		}

		public IEnumerable<object> ModifiedObjects => nodes;
	}

	abstract class AddNetModuleToAssemblyCommand : IUndoCommand2 {
		internal static bool CanExecute(IFileTreeNodeData[] nodes) {
			return nodes != null &&
				nodes.Length == 1 &&
				(nodes[0] is IAssemblyFileNode || nodes[0] is IModuleFileNode);
		}

		readonly IUndoCommandManager undoCommandManager;
		readonly IAssemblyFileNode asmNode;
		internal readonly IModuleFileNode modNode;
		readonly bool modNodeWasCreated;

		protected AddNetModuleToAssemblyCommand(IUndoCommandManager undoCommandManager, IDnSpyFileNode asmNode, IModuleFileNode modNode, bool modNodeWasCreated) {
			this.undoCommandManager = undoCommandManager;
			if (!(asmNode is IAssemblyFileNode))
				asmNode = (IAssemblyFileNode)asmNode.TreeNode.Parent.Data;
			this.asmNode = (IAssemblyFileNode)asmNode;
			this.modNode = modNode;
			this.modNodeWasCreated = modNodeWasCreated;
		}

		public abstract string Description { get; }

		public void Execute() {
			Debug.Assert(modNode.TreeNode.Parent == null);
			if (modNode.TreeNode.Parent != null)
				throw new InvalidOperationException();
			asmNode.TreeNode.EnsureChildrenLoaded();
			asmNode.DnSpyFile.AssemblyDef.Modules.Add(modNode.DnSpyFile.ModuleDef);
			asmNode.DnSpyFile.Children.Add(modNode.DnSpyFile);
			asmNode.TreeNode.AddChild(modNode.TreeNode);
			if (modNodeWasCreated)
				undoCommandManager.MarkAsModified(undoCommandManager.GetUndoObject(modNode.DnSpyFile));
		}

		public void Undo() {
			Debug.Assert(modNode.TreeNode.Parent != null);
			if (modNode.TreeNode.Parent == null)
				throw new InvalidOperationException();
			asmNode.DnSpyFile.AssemblyDef.Modules.Remove(modNode.DnSpyFile.ModuleDef);
			asmNode.DnSpyFile.Children.Remove(modNode.DnSpyFile);
			bool b = asmNode.TreeNode.Children.Remove(modNode.TreeNode);
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
	}

	sealed class AddNewNetModuleToAssemblyCommand : AddNetModuleToAssemblyCommand {
		[ExportMenuItem(Header = "res:AddNewNetModuleToAssemblyCommand", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 10)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => AddNewNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AddNewNetModuleToAssemblyCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:AddNewNetModuleToAssemblyCommand", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 10)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => AddNewNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AddNewNetModuleToAssemblyCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!AddNetModuleToAssemblyCommand.CanExecute(nodes))
				return;

			var asmNode = (IDnSpyFileNode)nodes[0];
			if (asmNode is IModuleFileNode)
				asmNode = (IAssemblyFileNode)asmNode.TreeNode.Parent.Data;

			var win = new NetModuleOptionsDlg();
			var data = new NetModuleOptionsVM(asmNode.DnSpyFile.ModuleDef);
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var options = data.CreateNetModuleOptions();
			var newModule = ModuleUtils.CreateNetModule(options.Name, options.Mvid, options.ClrVersion);
			var newFile = DnSpyDotNetFile.CreateModule(DnSpyFileInfo.CreateFile(string.Empty), newModule, appWindow.FileTreeView.FileManager.Settings.LoadPDBFiles);
			var newModNode = asmNode.Context.FileTreeView.CreateModule(newFile);
			var cmd = new AddNewNetModuleToAssemblyCommand(undoCommandManager.Value, (IDnSpyFileNode)nodes[0], newModNode);
			undoCommandManager.Value.Add(cmd);
			appWindow.FileTabManager.FollowReference(cmd.modNode);
		}

		AddNewNetModuleToAssemblyCommand(IUndoCommandManager undoCommandManager, IDnSpyFileNode asmNode, IModuleFileNode modNode)
			: base(undoCommandManager, asmNode, modNode, true) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.AddNewNetModuleToAssemblyCommand2;
	}

	sealed class AddExistingNetModuleToAssemblyCommand : AddNetModuleToAssemblyCommand {
		[ExportMenuItem(Header = "res:AddExistingNetModuleToAssemblyCommand", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 20)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => AddExistingNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AddExistingNetModuleToAssemblyCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:AddExistingNetModuleToAssemblyCommand", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 20)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => AddExistingNetModuleToAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AddExistingNetModuleToAssemblyCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
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

			var fm = appWindow.FileTreeView.FileManager;
			var file = DnSpyFile.CreateDnSpyFileFromFile(DnSpyFileInfo.CreateFile(dialog.FileName), dialog.FileName, fm.Settings.UseMemoryMappedIO, fm.Settings.LoadPDBFiles, fm.AssemblyResolver, true);
			if (file.ModuleDef == null || file.AssemblyDef != null || !(file is IDnSpyDotNetFile)) {
				Shared.App.MsgBox.Instance.Show(string.Format(dnSpy_AsmEditor_Resources.Error_NotNetModule, file.Filename), MsgBoxButton.OK);
				var id = file as IDisposable;
				if (id != null)
					id.Dispose();
				return;
			}

			var node = (IDnSpyFileNode)nodes[0];
			var newModNode = node.Context.FileTreeView.CreateModule((IDnSpyDotNetFile)file);
			var cmd = new AddExistingNetModuleToAssemblyCommand(undoCommandManager.Value, node, newModNode);
			undoCommandManager.Value.Add(cmd);
			appWindow.FileTabManager.FollowReference(cmd.modNode);
		}

		AddExistingNetModuleToAssemblyCommand(IUndoCommandManager undoCommandManager, IDnSpyFileNode asmNode, IModuleFileNode modNode)
			: base(undoCommandManager, asmNode, modNode, false) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.AddExistingNetModuleToAssemblyCommand2;
	}

	sealed class RemoveNetModuleFromAssemblyCommand : IUndoCommand2 {
		[ExportMenuItem(Header = "res:RemoveNetModuleCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_FILES_ASMED_DELETE, Order = 10)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IDocumentSaver> documentSaver;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IDocumentSaver> documentSaver, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.documentSaver = documentSaver;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.IsVisible(context.Nodes);
			public override bool IsEnabled(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.Execute(undoCommandManager, documentSaver, appWindow, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:RemoveNetModuleCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 10)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IDocumentSaver> documentSaver;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IDocumentSaver> documentSaver, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.documentSaver = documentSaver;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.IsVisible(context.Nodes);
			public override bool IsEnabled(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RemoveNetModuleFromAssemblyCommand.Execute(undoCommandManager, documentSaver, appWindow, context.Nodes);
		}

		static bool IsVisible(IFileTreeNodeData[] nodes) {
			return nodes != null &&
				nodes.Length == 1 &&
				nodes[0] is IModuleFileNode &&
				nodes[0].TreeNode.Parent != null &&
				nodes[0].TreeNode.Parent.Data is IAssemblyFileNode;
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) {
			return nodes != null &&
				nodes.Length == 1 &&
				nodes[0] is IModuleFileNode &&
				nodes[0].TreeNode.Parent != null &&
				nodes[0].TreeNode.Parent.DataChildren.ToList().IndexOf(nodes[0]) > 0;
		}

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IDocumentSaver> documentSaver, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var modNode = (IModuleFileNode)nodes[0];
			if (!documentSaver.Value.AskUserToSaveIfModified(new[] { modNode.DnSpyFile }))
				return;

			undoCommandManager.Value.Add(new RemoveNetModuleFromAssemblyCommand(undoCommandManager, modNode));
		}

		readonly IAssemblyFileNode asmNode;
		readonly IModuleFileNode modNode;
		readonly int removeIndex, removeIndexDnSpyFile;
		readonly Lazy<IUndoCommandManager> undoCommandManager;

		RemoveNetModuleFromAssemblyCommand(Lazy<IUndoCommandManager> undoCommandManager, IModuleFileNode modNode) {
			this.undoCommandManager = undoCommandManager;
			this.asmNode = (IAssemblyFileNode)modNode.TreeNode.Parent.Data;
			Debug.Assert(this.asmNode != null);
			this.modNode = modNode;
			this.removeIndex = asmNode.TreeNode.DataChildren.ToList().IndexOf(modNode);
			Debug.Assert(this.removeIndex > 0);
			Debug.Assert(asmNode.DnSpyFile.AssemblyDef != null &&
				asmNode.DnSpyFile.AssemblyDef.Modules.IndexOf(modNode.DnSpyFile.ModuleDef) == this.removeIndex);
			this.removeIndexDnSpyFile = asmNode.DnSpyFile.Children.IndexOf(modNode.DnSpyFile);
			Debug.Assert(this.removeIndexDnSpyFile >= 0);
		}

		public string Description => dnSpy_AsmEditor_Resources.RemoveNetModuleCommand;

		public void Execute() {
			Debug.Assert(modNode.TreeNode.Parent != null);
			if (modNode.TreeNode.Parent == null)
				throw new InvalidOperationException();

			var children = asmNode.TreeNode.DataChildren.ToArray();
			bool b = removeIndex < children.Length && children[removeIndex] == modNode &&
				removeIndex < asmNode.DnSpyFile.AssemblyDef.Modules.Count &&
				asmNode.DnSpyFile.AssemblyDef.Modules[removeIndex] == modNode.DnSpyFile.ModuleDef &&
				removeIndexDnSpyFile < asmNode.DnSpyFile.Children.Count &&
				asmNode.DnSpyFile.Children[removeIndexDnSpyFile] == modNode.DnSpyFile;
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
			asmNode.DnSpyFile.AssemblyDef.Modules.RemoveAt(removeIndex);
			asmNode.DnSpyFile.Children.RemoveAt(removeIndexDnSpyFile);
			asmNode.TreeNode.Children.RemoveAt(removeIndex);
			undoCommandManager.Value.MarkAsModified(undoCommandManager.Value.GetUndoObject(asmNode.DnSpyFile));
		}

		public void Undo() {
			Debug.Assert(modNode.TreeNode.Parent == null);
			if (modNode.TreeNode.Parent != null)
				throw new InvalidOperationException();
			asmNode.DnSpyFile.AssemblyDef.Modules.Insert(removeIndex, modNode.DnSpyFile.ModuleDef);
			asmNode.DnSpyFile.Children.Insert(removeIndexDnSpyFile, modNode.DnSpyFile);
			asmNode.TreeNode.Children.Insert(removeIndex, modNode.TreeNode);
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return asmNode; }
		}

		public IEnumerable<object> NonModifiedObjects {
			get { yield return modNode; }
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class ModuleSettingsCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditModuleCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 10)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => ModuleSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ModuleSettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditModuleCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 10)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => ModuleSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ModuleSettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) {
			return nodes != null &&
				nodes.Length == 1 &&
				nodes[0] is IModuleFileNode;
		}

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNode = (IModuleFileNode)nodes[0];

			var module = asmNode.DnSpyFile.ModuleDef;
			var data = new ModuleOptionsVM(module, new ModuleOptions(module), appWindow.LanguageManager);
			var win = new ModuleOptionsDlg();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandManager.Value.Add(new ModuleSettingsCommand(asmNode, data.CreateModuleOptions()));
		}

		readonly IModuleFileNode modNode;
		readonly ModuleOptions newOptions;
		readonly ModuleOptions origOptions;

		ModuleSettingsCommand(IModuleFileNode modNode, ModuleOptions newOptions) {
			this.modNode = modNode;
			this.newOptions = newOptions;
			this.origOptions = new ModuleOptions(modNode.DnSpyFile.ModuleDef);
		}

		public string Description => dnSpy_AsmEditor_Resources.EditModuleCommand2;

		void WriteModuleOptions(ModuleOptions theOptions) {
			theOptions.CopyTo(modNode.DnSpyFile.ModuleDef);
			modNode.TreeNode.RefreshUI();
		}

		public void Execute() => WriteModuleOptions(newOptions);
		public void Undo() => WriteModuleOptions(origOptions);

		public IEnumerable<object> ModifiedObjects {
			get { yield return modNode; }
		}
	}
}
