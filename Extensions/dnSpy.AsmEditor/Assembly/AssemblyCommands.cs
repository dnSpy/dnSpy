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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
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
			(context.Find<TreeNodeData[]>() ?? Array.Empty<TreeNodeData>()).Any(a => GetDocument(a) != null);

		static IDsDocument GetDocument(TreeNodeData node) {
			var fileNode = node as DsDocumentNode;
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
			foreach (var node in (context.Find<TreeNodeData[]>() ?? Array.Empty<TreeNodeData>())) {
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
		[ExportMenuItem(Header = "res:RemoveAssemblyCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_DELETE, Order = 0)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IDocumentSaver> documentSaver;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IDocumentSaver> documentSaver, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.documentSaver = documentSaver;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => RemoveAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RemoveAssemblyCommand.Execute(undoCommandService, documentSaver, appService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => RemoveAssemblyCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:RemoveAssemblyCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 0)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IDocumentSaver> documentSaver;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IDocumentSaver> documentSaver, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.documentSaver = documentSaver;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => RemoveAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RemoveAssemblyCommand.Execute(undoCommandService, documentSaver, appService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => RemoveAssemblyCommand.GetHeader(context.Nodes);
		}

		static string GetHeader(TreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.RemoveCommand, UIUtilities.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.RemoveAssembliesCommand, nodes.Length);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length > 0 &&
			nodes.All(n => n is DsDocumentNode && n.TreeNode.Parent == n.Context.DocumentTreeView.TreeView.Root);

		internal static void Execute(Lazy<IUndoCommandService> undoCommandService, Lazy<IDocumentSaver> documentSaver, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNodes = nodes.Cast<DsDocumentNode>().ToArray();
			var files = asmNodes.SelectMany(a => a.Document.GetAllChildrenAndSelf());
			if (!documentSaver.Value.AskUserToSaveIfModified(files))
				return;

			var keepNodes = new List<DsDocumentNode>();
			var freeNodes = new List<DsDocumentNode>();
			var onlyInRedoHistory = new List<DsDocumentNode>();
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
					undoCommandService.Value.Add(new RemoveAssemblyCommand(appService.DocumentTreeView, keepNodes.ToArray()));
				else
					undoCommandService.Value.ClearRedo();
				// Redo history was cleared
				FreeAssemblies(onlyInRedoHistory);
			}

			FreeAssemblies(freeNodes);
			if (freeNodes.Count > 0 || onlyInRedoHistory.Count > 0)
				undoCommandService.Value.CallGc();
		}

		static void FreeAssemblies(IList<DsDocumentNode> nodes) {
			if (nodes.Count == 0)
				return;
			nodes[0].Context.DocumentTreeView.Remove(nodes);
		}

		struct UndoRedoInfo {
			public bool IsInUndo;
			public bool IsInRedo;
			public DsDocumentNode Node;

			public UndoRedoInfo(DsDocumentNode node, bool isInUndo, bool isInRedo) {
				IsInUndo = isInUndo;
				IsInRedo = isInRedo;
				Node = node;
			}
		}

		static IEnumerable<UndoRedoInfo> GetUndoRedoInfo(IUndoCommandService undoCommandService, IEnumerable<DsDocumentNode> nodes) {
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

		RemoveAssemblyCommand(IDocumentTreeView documentTreeView, DsDocumentNode[] asmNodes) {
			savedStates = new RootDocumentNodeCreator[asmNodes.Length];
			for (int i = 0; i < savedStates.Length; i++)
				savedStates[i] = new RootDocumentNodeCreator(documentTreeView, asmNodes[i]);
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
		[ExportMenuItem(Header = "res:EditAssemblyCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 0)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => AssemblySettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AssemblySettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditAssemblyCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 0)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => AssemblySettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => AssemblySettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes != null &&
			nodes.Length == 1 &&
			nodes[0] is AssemblyDocumentNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var asmNode = (AssemblyDocumentNode)nodes[0];
			var module = asmNode.Document.ModuleDef;

			var data = new AssemblyOptionsVM(new AssemblyOptions(asmNode.Document.AssemblyDef), module, appService.DecompilerService);
			var win = new AssemblyOptionsDlg();
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandService.Value.Add(new AssemblySettingsCommand(asmNode, data.CreateAssemblyOptions()));
		}

		readonly AssemblyDocumentNode asmNode;
		readonly AssemblyOptions newOptions;
		readonly AssemblyOptions origOptions;
		readonly AssemblyRefInfo[] assemblyRefInfos;

		struct AssemblyRefInfo {
			public readonly AssemblyRef AssemblyRef;
			public readonly UTF8String OrigName;
			public readonly PublicKeyBase OrigPublicKeyOrToken;

			public AssemblyRefInfo(AssemblyRef asmRef) {
				AssemblyRef = asmRef;
				OrigName = asmRef.Name;
				OrigPublicKeyOrToken = asmRef.PublicKeyOrToken;
			}
		}

		AssemblySettingsCommand(AssemblyDocumentNode asmNode, AssemblyOptions newOptions) {
			this.asmNode = asmNode;
			this.newOptions = newOptions;
			origOptions = new AssemblyOptions(asmNode.Document.AssemblyDef);

			if (newOptions.Name != origOptions.Name)
				assemblyRefInfos = RefFinder.FindAssemblyRefsToThisModule(asmNode.Document.ModuleDef).Where(a => AssemblyNameComparer.NameAndPublicKeyTokenOnly.Equals(a, asmNode.Document.AssemblyDef)).Select(a => new AssemblyRefInfo(a)).ToArray();
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
		[ExportMenuItem(Header = "res:CreateAssemblyCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 0)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateAssemblyCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateAssemblyCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 0)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateAssemblyCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateAssemblyCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes != null &&
			(nodes.Length == 0 || nodes[0] is DsDocumentNode);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var newModule = new ModuleDefUser();

			var data = new AssemblyOptionsVM(AssemblyOptions.Create("MyAssembly"), newModule, appService.DecompilerService);
			data.CanShowClrVersion = true;
			var win = new AssemblyOptionsDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateAssemblyCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateAssemblyCommand(undoCommandService.Value, appService.DocumentTreeView, newModule, data.CreateAssemblyOptions());
			undoCommandService.Value.Add(cmd);
			appService.DocumentTabService.FollowReference(cmd.fileNodeCreator.DocumentNode);
		}

		readonly RootDocumentNodeCreator fileNodeCreator;
		readonly IUndoCommandService undoCommandService;

		CreateAssemblyCommand(IUndoCommandService undoCommandService, IDocumentTreeView documentTreeView, ModuleDef newModule, AssemblyOptions options) {
			this.undoCommandService = undoCommandService;
			var module = Module.ModuleUtils.CreateModule(options.Name, Guid.NewGuid(), options.ClrVersion, ModuleKind.Dll, newModule);
			options.CreateAssemblyDef(module).Modules.Add(module);
			var file = DsDotNetDocument.CreateAssembly(DsDocumentInfo.CreateDocument(string.Empty), module, true);
			fileNodeCreator = RootDocumentNodeCreator.CreateAssembly(documentTreeView, file);
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
