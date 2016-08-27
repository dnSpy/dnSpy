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
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.SaveModule;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Assembly {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, RemoveAssemblyCommand.EditMenuCommand removeCmd, AssemblySettingsCommand.EditMenuCommand settingsCmd) {
			wpfCommandManager.AddRemoveCommand(removeCmd);
			wpfCommandManager.AddSettingsCommand(fileTabManager, settingsCmd, null);
		}
	}

	[ExportMenuItem(Header = "res:DisableMMapIOCommand", Group = MenuConstants.GROUP_CTX_FILES_OTHER, Order = 50)]
	sealed class DisableMemoryMappedIOCommand : MenuItemBase {
		public override bool IsVisible(IMenuItemContext context) =>
			context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID) &&
			(context.Find<ITreeNodeData[]>() ?? Array.Empty<ITreeNodeData>()).Any(a => GetDnSpyFile(a) != null);

		static IDnSpyFile GetDnSpyFile(ITreeNodeData node) {
			var fileNode = node as IDnSpyFileNode;
			if (fileNode == null)
				return null;

			var peImage = fileNode.DnSpyFile.PEImage;
			if (peImage == null)
				peImage = (fileNode.DnSpyFile.ModuleDef as ModuleDefMD)?.MetaData?.PEImage;

			return peImage != null && peImage.IsMemoryMappedIO ? fileNode.DnSpyFile : null;
		}

		public override void Execute(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID))
				return;
			var asms = new List<IDnSpyFile>();
			foreach (var node in (context.Find<ITreeNodeData[]>() ?? Array.Empty<ITreeNodeData>())) {
				var file = GetDnSpyFile(node);
				if (file != null)
					asms.Add(file);
			}
			foreach (var asm in asms) {
				var peImage = asm.PEImage;
				if (peImage != null)
					peImage.UnsafeDisableMemoryMappedIO();
			}
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class RemoveAssemblyCommand : IGCUndoCommand {
		[ExportMenuItem(Header = "res:RemoveAssemblyCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_FILES_ASMED_DELETE, Order = 0)]
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

			public override bool IsVisible(AsmEditorContext context) => RemoveAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RemoveAssemblyCommand.Execute(undoCommandManager, documentSaver, appWindow, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => RemoveAssemblyCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:RemoveAssemblyCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 0)]
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

			public override bool IsVisible(AsmEditorContext context) => RemoveAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RemoveAssemblyCommand.Execute(undoCommandManager, documentSaver, appWindow, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => RemoveAssemblyCommand.GetHeader(context.Nodes);
		}

		static string GetHeader(ITreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.RemoveCommand, UIUtilities.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.RemoveAssembliesCommand, nodes.Length);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) =>
			nodes.Length > 0 &&
			nodes.All(n => n is IDnSpyFileNode && n.TreeNode.Parent == n.Context.FileTreeView.TreeView.Root);

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IDocumentSaver> documentSaver, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNodes = nodes.Cast<IDnSpyFileNode>().ToArray();
			var files = asmNodes.SelectMany(a => a.DnSpyFile.GetAllChildrenAndSelf());
			if (!documentSaver.Value.AskUserToSaveIfModified(files))
				return;

			var keepNodes = new List<IDnSpyFileNode>();
			var freeNodes = new List<IDnSpyFileNode>();
			var onlyInRedoHistory = new List<IDnSpyFileNode>();
			foreach (var info in GetUndoRedoInfo(undoCommandManager.Value, asmNodes)) {
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
				foreach (var node in keepNodes) {
					foreach (var f in node.DnSpyFile.GetAllChildrenAndSelf())
						MemoryMappedIOHelper.DisableMemoryMappedIO(f);
				}
				if (keepNodes.Count != 0)
					undoCommandManager.Value.Add(new RemoveAssemblyCommand(appWindow.FileTreeView, keepNodes.ToArray()));
				else
					undoCommandManager.Value.ClearRedo();
				// Redo history was cleared
				FreeAssemblies(onlyInRedoHistory);
			}

			FreeAssemblies(freeNodes);
			if (freeNodes.Count > 0 || onlyInRedoHistory.Count > 0)
				undoCommandManager.Value.CallGc();
		}

		static void FreeAssemblies(IList<IDnSpyFileNode> nodes) {
			if (nodes.Count == 0)
				return;
			nodes[0].Context.FileTreeView.Remove(nodes);
		}

		struct UndoRedoInfo {
			public bool IsInUndo;
			public bool IsInRedo;
			public IDnSpyFileNode Node;

			public UndoRedoInfo(IDnSpyFileNode node, bool isInUndo, bool isInRedo) {
				this.IsInUndo = isInUndo;
				this.IsInRedo = isInRedo;
				this.Node = node;
			}
		}

		static IEnumerable<UndoRedoInfo> GetUndoRedoInfo(IUndoCommandManager undoCommandManager, IEnumerable<IDnSpyFileNode> nodes) {
			var modifiedUndoAsms = new HashSet<IUndoObject>(undoCommandManager.UndoObjects);
			var modifiedRedoAsms = new HashSet<IUndoObject>(undoCommandManager.RedoObjects);
			foreach (var node in nodes) {
				var uo = undoCommandManager.GetUndoObject(node.DnSpyFile);
				bool isInUndo = modifiedUndoAsms.Contains(uo);
				bool isInRedo = modifiedRedoAsms.Contains(uo);
				yield return new UndoRedoInfo(node, isInUndo, isInRedo);
			}
		}

		RootDnSpyFileNodeCreator[] savedStates;

		RemoveAssemblyCommand(IFileTreeView fileTreeView, IDnSpyFileNode[] asmNodes) {
			this.savedStates = new RootDnSpyFileNodeCreator[asmNodes.Length];
			for (int i = 0; i < this.savedStates.Length; i++)
				this.savedStates[i] = new RootDnSpyFileNodeCreator(fileTreeView, asmNodes[i]);
		}

		public string Description => dnSpy_AsmEditor_Resources.RemoveAssemblyCommand;

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
					yield return savedState.DnSpyFileNode;
			}
		}

		public bool CallGarbageCollectorAfterDispose => true;
	}

	[DebuggerDisplay("{Description}")]
	sealed class AssemblySettingsCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditAssemblyCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 0)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => AssemblySettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AssemblySettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditAssemblyCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 0)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => AssemblySettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AssemblySettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) =>
			nodes != null &&
			nodes.Length == 1 &&
			nodes[0] is IAssemblyFileNode;

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNode = (IAssemblyFileNode)nodes[0];
			var module = asmNode.DnSpyFile.ModuleDef;

			var data = new AssemblyOptionsVM(new AssemblyOptions(asmNode.DnSpyFile.AssemblyDef), module, appWindow.DecompilerManager);
			var win = new AssemblyOptionsDlg();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandManager.Value.Add(new AssemblySettingsCommand(asmNode, data.CreateAssemblyOptions()));
		}

		readonly IAssemblyFileNode asmNode;
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

		AssemblySettingsCommand(IAssemblyFileNode asmNode, AssemblyOptions newOptions) {
			this.asmNode = asmNode;
			this.newOptions = newOptions;
			this.origOptions = new AssemblyOptions(asmNode.DnSpyFile.AssemblyDef);

			if (newOptions.Name != origOptions.Name)
				this.assemblyRefInfos = RefFinder.FindAssemblyRefsToThisModule(asmNode.DnSpyFile.ModuleDef).Where(a => AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(a, asmNode.DnSpyFile.AssemblyDef)).Select(a => new AssemblyRefInfo(a)).ToArray();
		}

		public string Description => dnSpy_AsmEditor_Resources.EditAssemblyCommand2;

		public void Execute() {
			newOptions.CopyTo(asmNode.DnSpyFile.AssemblyDef);
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
			asmNode.TreeNode.RefreshUI();
		}

		public void Undo() {
			origOptions.CopyTo(asmNode.DnSpyFile.AssemblyDef);
			if (assemblyRefInfos != null) {
				foreach (var info in assemblyRefInfos) {
					info.AssemblyRef.Name = info.OrigName;
					info.AssemblyRef.PublicKeyOrToken = info.OrigPublicKeyOrToken;
				}
			}
			asmNode.TreeNode.RefreshUI();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return asmNode; }
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateAssemblyCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:CreateAssemblyCommand", Icon = "NewAssembly", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 0)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateAssemblyCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateAssemblyCommand", Icon = "NewAssembly", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 0)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateAssemblyCommand.Execute(undoCommandManager, appWindow, context.Nodes);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) =>
			nodes != null &&
			(nodes.Length == 0 || nodes[0] is IDnSpyFileNode);

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var newModule = new ModuleDefUser();

			var data = new AssemblyOptionsVM(AssemblyOptions.Create("MyAssembly"), newModule, appWindow.DecompilerManager);
			data.CanShowClrVersion = true;
			var win = new AssemblyOptionsDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateAssemblyCommand2;
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateAssemblyCommand(undoCommandManager.Value, appWindow.FileTreeView, newModule, data.CreateAssemblyOptions());
			undoCommandManager.Value.Add(cmd);
			appWindow.FileTabManager.FollowReference(cmd.fileNodeCreator.DnSpyFileNode);
		}

		readonly RootDnSpyFileNodeCreator fileNodeCreator;
		readonly IUndoCommandManager undoCommandManager;

		CreateAssemblyCommand(IUndoCommandManager undoCommandManager, IFileTreeView fileTreeView, ModuleDef newModule, AssemblyOptions options) {
			this.undoCommandManager = undoCommandManager;
			var module = Module.ModuleUtils.CreateModule(options.Name, Guid.NewGuid(), options.ClrVersion, ModuleKind.Dll, newModule);
			options.CreateAssemblyDef(module).Modules.Add(module);
			var file = DnSpyDotNetFile.CreateAssembly(DnSpyFileInfo.CreateFile(string.Empty), module, fileTreeView.FileManager.Settings.LoadPDBFiles);
			this.fileNodeCreator = RootDnSpyFileNodeCreator.CreateAssembly(fileTreeView, file);
		}

		public string Description => dnSpy_AsmEditor_Resources.CreateAssemblyCommand2;

		public void Execute() {
			fileNodeCreator.Add();
			undoCommandManager.MarkAsModified(undoCommandManager.GetUndoObject(fileNodeCreator.DnSpyFileNode.DnSpyFile));
		}

		public void Undo() => fileNodeCreator.Remove();

		public IEnumerable<object> ModifiedObjects {
			get { yield return fileNodeCreator.DnSpyFileNode; }
		}
	}
}
