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
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Assembly {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, RemoveAssemblyCommand.EditMenuCommand removeCmd, AssemblySettingsCommand.EditMenuCommand settingsCmd) {
			wpfCommandService.AddRemoveCommand(removeCmd);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd, null);
		}
	}

	[ExportMenuItem(Header = "res:DisableMMapIOCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_OTHER, Order = 50)]
	sealed class DisableMemoryMappedIOCommand : MenuItemBase {
		public override bool IsVisible(IMenuItemContext context) =>
			context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID) &&
			(context.Find<ITreeNodeData[]>() ?? Array.Empty<ITreeNodeData>()).Any(a => GetDocument(a) != null);

		static IDsDocument GetDocument(ITreeNodeData node) {
			var fileNode = node as IDsDocumentNode;
			if (fileNode == null)
				return null;

			var peImage = fileNode.Document.PEImage;
			if (peImage == null)
				peImage = (fileNode.Document.ModuleDef as ModuleDefMD)?.MetaData?.PEImage;

			return peImage != null && peImage.IsMemoryMappedIO ? fileNode.Document : null;
		}

		public override void Execute(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID))
				return;
			var asms = new List<IDsDocument>();
			foreach (var node in (context.Find<ITreeNodeData[]>() ?? Array.Empty<ITreeNodeData>())) {
				var file = GetDocument(node);
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
		[ExportMenuItem(Header = "res:RemoveAssemblyCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_DELETE, Order = 0)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IDocumentSaver> documentSaver;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IDocumentSaver> documentSaver, IAppWindow appWindow) {
				this.undoCommandService = undoCommandService;
				this.documentSaver = documentSaver;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => RemoveAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RemoveAssemblyCommand.Execute(undoCommandService, documentSaver, appWindow, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => RemoveAssemblyCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:RemoveAssemblyCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 0)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IDocumentSaver> documentSaver;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IDocumentSaver> documentSaver, IAppWindow appWindow)
				: base(appWindow.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.documentSaver = documentSaver;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => RemoveAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RemoveAssemblyCommand.Execute(undoCommandService, documentSaver, appWindow, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => RemoveAssemblyCommand.GetHeader(context.Nodes);
		}

		static string GetHeader(ITreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.RemoveCommand, UIUtilities.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.RemoveAssembliesCommand, nodes.Length);
		}

		static bool CanExecute(IDocumentTreeNodeData[] nodes) =>
			nodes.Length > 0 &&
			nodes.All(n => n is IDsDocumentNode && n.TreeNode.Parent == n.Context.DocumentTreeView.TreeView.Root);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, Lazy<IDocumentSaver> documentSaver, IAppWindow appWindow, IDocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNodes = nodes.Cast<IDsDocumentNode>().ToArray();
			var files = asmNodes.SelectMany(a => a.Document.GetAllChildrenAndSelf());
			if (!documentSaver.Value.AskUserToSaveIfModified(files))
				return;

			var keepNodes = new List<IDsDocumentNode>();
			var freeNodes = new List<IDsDocumentNode>();
			var onlyInRedoHistory = new List<IDsDocumentNode>();
			foreach (var info in GetUndoRedoInfo(undoCommandService.Value, asmNodes)) {
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
					foreach (var f in node.Document.GetAllChildrenAndSelf())
						MemoryMappedIOHelper.DisableMemoryMappedIO(f);
				}
				if (keepNodes.Count != 0)
					undoCommandService.Value.Add(new RemoveAssemblyCommand(appWindow.DocumentTreeView, keepNodes.ToArray()));
				else
					undoCommandService.Value.ClearRedo();
				// Redo history was cleared
				FreeAssemblies(onlyInRedoHistory);
			}

			FreeAssemblies(freeNodes);
			if (freeNodes.Count > 0 || onlyInRedoHistory.Count > 0)
				undoCommandService.Value.CallGc();
		}

		static void FreeAssemblies(IList<IDsDocumentNode> nodes) {
			if (nodes.Count == 0)
				return;
			nodes[0].Context.DocumentTreeView.Remove(nodes);
		}

		struct UndoRedoInfo {
			public bool IsInUndo;
			public bool IsInRedo;
			public IDsDocumentNode Node;

			public UndoRedoInfo(IDsDocumentNode node, bool isInUndo, bool isInRedo) {
				this.IsInUndo = isInUndo;
				this.IsInRedo = isInRedo;
				this.Node = node;
			}
		}

		static IEnumerable<UndoRedoInfo> GetUndoRedoInfo(IUndoCommandService undoCommandService, IEnumerable<IDsDocumentNode> nodes) {
			var modifiedUndoAsms = new HashSet<IUndoObject>(undoCommandService.UndoObjects);
			var modifiedRedoAsms = new HashSet<IUndoObject>(undoCommandService.RedoObjects);
			foreach (var node in nodes) {
				var uo = undoCommandService.GetUndoObject(node.Document);
				bool isInUndo = modifiedUndoAsms.Contains(uo);
				bool isInRedo = modifiedRedoAsms.Contains(uo);
				yield return new UndoRedoInfo(node, isInUndo, isInRedo);
			}
		}

		RootDocumentNodeCreator[] savedStates;

		RemoveAssemblyCommand(IDocumentTreeView documentTreeView, IDsDocumentNode[] asmNodes) {
			this.savedStates = new RootDocumentNodeCreator[asmNodes.Length];
			for (int i = 0; i < this.savedStates.Length; i++)
				this.savedStates[i] = new RootDocumentNodeCreator(documentTreeView, asmNodes[i]);
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
					yield return savedState.DocumentNode;
			}
		}

		public bool CallGarbageCollectorAfterDispose => true;
	}

	[DebuggerDisplay("{Description}")]
	sealed class AssemblySettingsCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditAssemblyCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 0)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppWindow appWindow) {
				this.undoCommandService = undoCommandService;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => AssemblySettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AssemblySettingsCommand.Execute(undoCommandService, appWindow, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditAssemblyCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 0)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppWindow appWindow)
				: base(appWindow.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => AssemblySettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AssemblySettingsCommand.Execute(undoCommandService, appWindow, context.Nodes);
		}

		static bool CanExecute(IDocumentTreeNodeData[] nodes) =>
			nodes != null &&
			nodes.Length == 1 &&
			nodes[0] is IAssemblyDocumentNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppWindow appWindow, IDocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNode = (IAssemblyDocumentNode)nodes[0];
			var module = asmNode.Document.ModuleDef;

			var data = new AssemblyOptionsVM(new AssemblyOptions(asmNode.Document.AssemblyDef), module, appWindow.DecompilerManager);
			var win = new AssemblyOptionsDlg();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandService.Value.Add(new AssemblySettingsCommand(asmNode, data.CreateAssemblyOptions()));
		}

		readonly IAssemblyDocumentNode asmNode;
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

		AssemblySettingsCommand(IAssemblyDocumentNode asmNode, AssemblyOptions newOptions) {
			this.asmNode = asmNode;
			this.newOptions = newOptions;
			this.origOptions = new AssemblyOptions(asmNode.Document.AssemblyDef);

			if (newOptions.Name != origOptions.Name)
				this.assemblyRefInfos = RefFinder.FindAssemblyRefsToThisModule(asmNode.Document.ModuleDef).Where(a => AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(a, asmNode.Document.AssemblyDef)).Select(a => new AssemblyRefInfo(a)).ToArray();
		}

		public string Description => dnSpy_AsmEditor_Resources.EditAssemblyCommand2;

		public void Execute() {
			newOptions.CopyTo(asmNode.Document.AssemblyDef);
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
			origOptions.CopyTo(asmNode.Document.AssemblyDef);
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
		[ExportMenuItem(Header = "res:CreateAssemblyCommand", Icon = "NewAssembly", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 0)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppWindow appWindow) {
				this.undoCommandService = undoCommandService;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateAssemblyCommand.Execute(undoCommandService, appWindow, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateAssemblyCommand", Icon = "NewAssembly", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 0)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppWindow appWindow)
				: base(appWindow.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateAssemblyCommand.Execute(undoCommandService, appWindow, context.Nodes);
		}

		static bool CanExecute(IDocumentTreeNodeData[] nodes) =>
			nodes != null &&
			(nodes.Length == 0 || nodes[0] is IDsDocumentNode);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppWindow appWindow, IDocumentTreeNodeData[] nodes) {
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

			var cmd = new CreateAssemblyCommand(undoCommandService.Value, appWindow.DocumentTreeView, newModule, data.CreateAssemblyOptions());
			undoCommandService.Value.Add(cmd);
			appWindow.DocumentTabService.FollowReference(cmd.fileNodeCreator.DocumentNode);
		}

		readonly RootDocumentNodeCreator fileNodeCreator;
		readonly IUndoCommandService undoCommandService;

		CreateAssemblyCommand(IUndoCommandService undoCommandService, IDocumentTreeView documentTreeView, ModuleDef newModule, AssemblyOptions options) {
			this.undoCommandService = undoCommandService;
			var module = Module.ModuleUtils.CreateModule(options.Name, Guid.NewGuid(), options.ClrVersion, ModuleKind.Dll, newModule);
			options.CreateAssemblyDef(module).Modules.Add(module);
			var file = DsDotNetDocument.CreateAssembly(DsDocumentInfo.CreateDocument(string.Empty), module, documentTreeView.DocumentService.Settings.LoadPDBFiles);
			this.fileNodeCreator = RootDocumentNodeCreator.CreateAssembly(documentTreeView, file);
		}

		public string Description => dnSpy_AsmEditor_Resources.CreateAssemblyCommand2;

		public void Execute() {
			fileNodeCreator.Add();
			undoCommandService.MarkAsModified(undoCommandService.GetUndoObject(fileNodeCreator.DocumentNode.Document));
		}

		public void Undo() => fileNodeCreator.Remove();

		public IEnumerable<object> ModifiedObjects {
			get { yield return fileNodeCreator.DocumentNode; }
		}
	}
}
