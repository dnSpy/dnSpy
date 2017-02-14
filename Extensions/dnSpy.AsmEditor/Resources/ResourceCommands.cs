/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;
using WF = System.Windows.Forms;

namespace dnSpy.AsmEditor.Resources {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService,
			DeleteResourceCommand.EditMenuCommand removeCmd1,
			DeleteResourceElementCommand.EditMenuCommand removeCmd2,
			DeleteResourceCommand.CodeCommand removeCmd3,
			DeleteResourceElementCommand.CodeCommand removeCmd4,
			ResourceSettingsCommand.EditMenuCommand settingsCmd10,
			ResourceSettingsCommand.CodeCommand settingsCmd11,
			ResourceElementSettingsCommand.EditMenuCommand settingsCmd20,
			ResourceElementSettingsCommand.CodeCommand settingsCmd21,
			ImageResourceElementSettingsCommand.EditMenuCommand settingsCmd30,
			ImageResourceElementSettingsCommand.CodeCommand settingsCmd31,
			SerializedImageResourceElementSettingsCommand.EditMenuCommand settingsCmd40,
			SerializedImageResourceElementSettingsCommand.CodeCommand settingsCmd41,
			SerializedImageListStreamerResourceElementSettingsCommand.EditMenuCommand settingsCmd50,
			SerializedImageListStreamerResourceElementSettingsCommand.CodeCommand settingsCmd51) {
			wpfCommandService.AddRemoveCommand(removeCmd1);
			wpfCommandService.AddRemoveCommand(removeCmd2);
			wpfCommandService.AddRemoveCommand(removeCmd3, documentTabService);
			wpfCommandService.AddRemoveCommand(removeCmd4, documentTabService);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd10, settingsCmd11);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd20, settingsCmd21);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd30, settingsCmd31);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd40, settingsCmd41);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd50, settingsCmd51);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteResourceCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:DeleteResourceCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_DELETE, Order = 80)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeleteResourceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteResourceCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => DeleteResourceCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:DeleteResourceCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 80)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeleteResourceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteResourceCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => DeleteResourceCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:DeleteResourceCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_DELETE, Order = 80)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && DeleteResourceCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => DeleteResourceCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(CodeContext context) => DeleteResourceCommand.GetHeader(context.Nodes);
		}

		static string GetHeader(DocumentTreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.DeleteX, UIUtilities.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.DeleteResourcesCommand, nodes.Length);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length > 0 && nodes.All(n => n is ResourceNode);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcNodes = nodes.Cast<ResourceNode>().ToArray();
			undoCommandService.Value.Add(new DeleteResourceCommand(rsrcNodes));
		}

		struct DeleteModelNodes {
			ModelInfo[] infos;

			struct ModelInfo {
				public readonly ModuleDef OwnerModule;
				public readonly int Index;

				public ModelInfo(ModuleDef module, Resource rsrc) {
					OwnerModule = module;
					Index = module.Resources.IndexOf(rsrc);
					Debug.Assert(Index >= 0);
					if (Index < 0)
						throw new InvalidOperationException();
				}
			}

			public void Delete(ResourceNode[] nodes, DocumentTreeNodeData[] parents) {
				Debug.Assert(infos == null);
				if (infos != null)
					throw new InvalidOperationException();

				infos = new ModelInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];

					var module = parents[i].GetModule();
					Debug.Assert(module != null);
					if (module == null)
						throw new InvalidOperationException();
					var info = new ModelInfo(module, node.Resource);
					infos[i] = info;
					info.OwnerModule.Resources.RemoveAt(info.Index);
				}
			}

			public void Restore(ResourceNode[] nodes) {
				Debug.Assert(infos != null);
				if (infos == null)
					throw new InvalidOperationException();
				Debug.Assert(infos.Length == nodes.Length);
				if (infos.Length != nodes.Length)
					throw new InvalidOperationException();

				for (int i = infos.Length - 1; i >= 0; i--) {
					var node = nodes[i];
					var info = infos[i];
					info.OwnerModule.Resources.Insert(info.Index, node.Resource);
				}

				infos = null;
			}
		}

		DeletableNodes<ResourceNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteResourceCommand(ResourceNode[] rsrcNodes) => nodes = new DeletableNodes<ResourceNode>(rsrcNodes);

		public string Description => dnSpy_AsmEditor_Resources.DeleteResourceCommand;

		public void Execute() {
			nodes.Delete();
			modelNodes.Delete(nodes.Nodes, nodes.Parents);
		}

		public void Undo() {
			modelNodes.Restore(nodes.Nodes);
			nodes.Restore();
		}

		public IEnumerable<object> ModifiedObjects => nodes.Nodes;
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteResourceElementCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:DeleteResourceCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_DELETE, Order = 90)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeleteResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteResourceElementCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => DeleteResourceElementCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:DeleteResourceCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 90)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeleteResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteResourceElementCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => DeleteResourceElementCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:DeleteResourceCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_DELETE, Order = 90)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && DeleteResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => DeleteResourceElementCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(CodeContext context) => DeleteResourceElementCommand.GetHeader(context.Nodes);
		}

		static string GetHeader(DocumentTreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.DeleteX, UIUtilities.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.DeleteResourcesCommand, nodes.Length);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length > 0 && nodes.All(n => n is ResourceElementNode);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcNodes = nodes.Cast<ResourceElementNode>().ToArray();
			undoCommandService.Value.Add(new DeleteResourceElementCommand(rsrcNodes));
		}

		sealed class ModuleInfo {
			public readonly ResourceElementSetNode Node;
			readonly ModuleDef Module;
			readonly int Index;
			readonly Resource Resource;
			Resource NewResource;

			public ModuleInfo(ResourceElementSetNode node) {
				Node = node ?? throw new InvalidOperationException();
				var module = node.GetModule();
				Debug.Assert(module != null);
				Module = module ?? throw new InvalidOperationException();
				Index = module.Resources.IndexOf(node.Resource);
				Debug.Assert(Index >= 0);
				if (Index < 0)
					throw new InvalidOperationException();
				Resource = node.Resource;
			}

			public void Replace() {
				Debug.Assert(Index < Module.Resources.Count && Module.Resources[Index] == Node.Resource);
				if (NewResource == null) {
					Node.RegenerateEmbeddedResource();
					NewResource = Node.Resource;
				}
				else
					Node.Resource = NewResource;
				Module.Resources[Index] = NewResource;
			}

			public void Restore() {
				Debug.Assert(Index < Module.Resources.Count && Module.Resources[Index] == Node.Resource);
				Node.Resource = Resource;
				Module.Resources[Index] = Resource;
			}
		}

		DeletableNodes<ResourceElementNode> nodes;
		readonly List<ModuleInfo> savedResources = new List<ModuleInfo>();

		DeleteResourceElementCommand(ResourceElementNode[] rsrcNodes) => nodes = new DeletableNodes<ResourceElementNode>(rsrcNodes);

		public string Description => dnSpy_AsmEditor_Resources.DeleteResourceCommand;

		public void Execute() {
			Debug.Assert(savedResources.Count == 0);
			savedResources.AddRange(nodes.Nodes.Select(a => a.GetAncestorOrSelf<ResourceElementSetNode>()).Distinct().Select(a => new ModuleInfo(a)));

			nodes.Delete();

			for (int i = 0; i < savedResources.Count; i++)
				savedResources[i].Replace();
		}

		public void Undo() {
			Debug.Assert(savedResources.Count > 0);
			for (int i = savedResources.Count - 1; i >= 0; i--)
				savedResources[i].Restore();
			savedResources.Clear();

			nodes.Restore();
		}

		public IEnumerable<object> ModifiedObjects => nodes.Nodes;
	}

	abstract class SaveResourcesCommandBase : MenuItemBase {
		readonly bool useSubDirs;
		readonly ResourceDataType resourceDataType;

		protected SaveResourcesCommandBase(bool useSubDirs, ResourceDataType resourceDataType) {
			this.useSubDirs = useSubDirs;
			this.resourceDataType = resourceDataType;
		}

		protected abstract IResourceDataProvider[] GetResourceNodes(IMenuItemContext context);

		IResourceDataProvider[] Filter(IResourceDataProvider[] nodes) {
			nodes = nodes.Where(a => a.GetResourceData(resourceDataType).Any()).ToArray();
			return nodes.Length == 0 ? null : nodes;
		}

		protected IResourceDataProvider[] CodeGetResourceNodes(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return null;

			var @ref = context.Find<TextReference>();
			if (@ref == null)
				return null;
			var rsrcNode = @ref.Reference as IResourceDataProvider;
			if (rsrcNode == null)
				return null;

			return Filter(new[] { rsrcNode });
		}

		protected IResourceDataProvider[] FilesGetResourceNodes(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID))
				return null;

			var selNodes = context.Find<TreeNodeData[]>();
			if (selNodes == null)
				return null;

			if (selNodes.Length == 1 && selNodes[0] is ResourcesFolderNode) {
				var rlist = (ResourcesFolderNode)selNodes[0];
				rlist.TreeNode.EnsureChildrenLoaded();
				selNodes = rlist.TreeNode.DataChildren.Cast<DocumentTreeNodeData>().ToArray();
			}
			return Filter(selNodes.Where(a => a is IResourceDataProvider).Cast<IResourceDataProvider>().ToArray());
		}

		protected ResourceData[] GetResourceData(IResourceDataProvider[] nodes) => SaveResources.GetResourceData(nodes, resourceDataType);
		public override bool IsVisible(IMenuItemContext context) => GetResourceNodes(context) != null;
		public override void Execute(IMenuItemContext context) => SaveResources.Save(GetResourceNodes(context), useSubDirs, resourceDataType);
	}

	static class SaveResourcesCommand {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SAVE, Order = 0)]
		sealed class CodeCommand : SaveResourcesCommandBase {
			public CodeCommand()
				: base(false, ResourceDataType.Deserialized) {
			}

			public override string GetHeader(IMenuItemContext context) => GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			protected override IResourceDataProvider[] GetResourceNodes(IMenuItemContext context) => CodeGetResourceNodes(context);
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SAVE, Order = 0)]
		sealed class DocumentsCommand : SaveResourcesCommandBase {
			public DocumentsCommand()
				: base(false, ResourceDataType.Deserialized) {
			}

			public override string GetHeader(IMenuItemContext context) => GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			protected override IResourceDataProvider[] GetResourceNodes(IMenuItemContext context) => FilesGetResourceNodes(context);
		}

		static string GetHeaderInternal(ResourceData[] infos) {
			if (infos.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.SaveResourceCommand, UIUtilities.EscapeMenuItemHeader(infos[0].Name));
			return string.Format(dnSpy_AsmEditor_Resources.SaveResourcesCommand, infos.Length);
		}
	}

	static class SaveWithPathResourcesCommand {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SAVE, Order = 10)]
		sealed class CodeCommand : SaveResourcesCommandBase {
			public CodeCommand()
				: base(true, ResourceDataType.Deserialized) {
			}

			public override string GetHeader(IMenuItemContext context) => GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			protected override IResourceDataProvider[] GetResourceNodes(IMenuItemContext context) => CodeGetResourceNodes(context);
			public override bool IsVisible(IMenuItemContext context) => IsVisibleInternal(GetResourceData(GetResourceNodes(context)));
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SAVE, Order = 10)]
		sealed class DocumentsCommand : SaveResourcesCommandBase {
			public DocumentsCommand()
				: base(true, ResourceDataType.Deserialized) {
			}

			public override string GetHeader(IMenuItemContext context) => GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			protected override IResourceDataProvider[] GetResourceNodes(IMenuItemContext context) => FilesGetResourceNodes(context);
			public override bool IsVisible(IMenuItemContext context) => IsVisibleInternal(GetResourceData(GetResourceNodes(context)));
		}

		static string GetHeaderInternal(ResourceData[] infos) => string.Format(dnSpy_AsmEditor_Resources.SaveResourcesSubDirectoriesCommand, infos.Length);
		internal static bool IsVisibleInternal(ResourceData[] infos) => infos.Length > 1 && infos.Any(a => a.Name.Contains('/') || a.Name.Contains('\\'));
	}

	static class SaveRawResourcesCommand {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SAVE, Order = 20)]
		sealed class CodeCommand : SaveResourcesCommandBase {
			public CodeCommand()
				: base(false, ResourceDataType.Serialized) {
			}

			public override string GetHeader(IMenuItemContext context) => GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			protected override IResourceDataProvider[] GetResourceNodes(IMenuItemContext context) => CodeGetResourceNodes(context);
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SAVE, Order = 20)]
		sealed class DocumentsCommand : SaveResourcesCommandBase {
			public DocumentsCommand()
				: base(false, ResourceDataType.Serialized) {
			}

			public override string GetHeader(IMenuItemContext context) => GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			protected override IResourceDataProvider[] GetResourceNodes(IMenuItemContext context) => FilesGetResourceNodes(context);
		}

		static string GetHeaderInternal(ResourceData[] infos) {
			if (infos.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.RawSaveResourceCommand, UIUtilities.EscapeMenuItemHeader(infos[0].Name));
			return string.Format(dnSpy_AsmEditor_Resources.RawSaveResourcesCommand, infos.Length);
		}
	}

	static class SaveRawWithPathResourcesCommand {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SAVE, Order = 30)]
		sealed class CodeCommand : SaveResourcesCommandBase {
			public CodeCommand()
				: base(true, ResourceDataType.Serialized) {
			}

			public override string GetHeader(IMenuItemContext context) => GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			protected override IResourceDataProvider[] GetResourceNodes(IMenuItemContext context) => CodeGetResourceNodes(context);
			public override bool IsVisible(IMenuItemContext context) => IsVisibleInternal(GetResourceData(GetResourceNodes(context)));
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SAVE, Order = 30)]
		sealed class DocumentsCommand : SaveResourcesCommandBase {
			public DocumentsCommand()
				: base(true, ResourceDataType.Serialized) {
			}

			public override string GetHeader(IMenuItemContext context) => GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			protected override IResourceDataProvider[] GetResourceNodes(IMenuItemContext context) => FilesGetResourceNodes(context);
			public override bool IsVisible(IMenuItemContext context) => IsVisibleInternal(GetResourceData(GetResourceNodes(context)));
		}

		static string GetHeaderInternal(ResourceData[] infos) => string.Format(dnSpy_AsmEditor_Resources.RawSaveResourcesSubDirectoriesCommand, infos.Length);
		static bool IsVisibleInternal(ResourceData[] infos) => SaveWithPathResourcesCommand.IsVisibleInternal(infos);
	}

	static class ResUtils {
		public static bool CanExecuteResourceListCommand(DocumentTreeNodeData[] nodes) => GetResourceListTreeNode(nodes) != null;

		public static ResourcesFolderNode GetResourceListTreeNode(DocumentTreeNodeData[] nodes) {
			if (nodes.Length != 1)
				return null;
			if (nodes[0] is ResourcesFolderNode rsrcListNode)
				return rsrcListNode;
			rsrcListNode = nodes[0].TreeNode.Parent?.Data as ResourcesFolderNode;
			if (rsrcListNode != null)
				return rsrcListNode;

			var modNode = nodes[0] as ModuleDocumentNode;
			if (modNode == null)
				return null;
			modNode.TreeNode.EnsureChildrenLoaded();
			rsrcListNode = (ResourcesFolderNode)modNode.TreeNode.DataChildren.FirstOrDefault(a => a is ResourcesFolderNode);
			if (rsrcListNode == null)	// If not a module node
				return null;
			rsrcListNode.TreeNode.EnsureChildrenLoaded();
			if (rsrcListNode.TreeNode.Children.Count == 0)
				return rsrcListNode;
			return null;
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateFileResourceCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:CreateFileResourceCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 100)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateFileResourceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateFileResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateFileResourceCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 100)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateFileResourceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateFileResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateFileResourceCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 100)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && CreateFileResourceCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => CreateFileResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => ResUtils.CanExecuteResourceListCommand(nodes);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = ResUtils.GetResourceListTreeNode(nodes);

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var dlg = new WF.OpenFileDialog {
				RestoreDirectory = true,
				Multiselect = true,
			};
			if (dlg.ShowDialog() != WF.DialogResult.OK)
				return;
			var fnames = dlg.FileNames;
			if (fnames.Length == 0)
				return;

			var newNodes = new ResourceNode[fnames.Length];
			var treeView = appService.DocumentTreeView.TreeView;
			var treeNodeGroup = appService.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ResourceTreeNodeGroup);
			for (int i = 0; i < fnames.Length; i++) {
				var fn = fnames[i];
				try {
					var rsrc = new EmbeddedResource(Path.GetFileName(fn), File.ReadAllBytes(fn), ManifestResourceAttributes.Public);
					newNodes[i] = (ResourceNode)treeView.Create(resourceNodeFactory.Create(module, rsrc, treeNodeGroup)).Data;
				}
				catch (Exception ex) {
					MsgBox.Instance.Show(string.Format(dnSpy_AsmEditor_Resources.Error_ReadingFiles, ex.Message));
					return;
				}
			}

			undoCommandService.Value.Add(new CreateFileResourceCommand(rsrcListNode, newNodes));
			appService.DocumentTabService.FollowReference(newNodes[0]);
		}

		readonly ModuleDef module;
		readonly ResourcesFolderNode rsrcListNode;
		readonly ResourceNode[] nodes;

		CreateFileResourceCommand(ResourcesFolderNode rsrcListNode, ResourceNode[] nodes) {
			module = rsrcListNode.GetModule();
			Debug.Assert(module != null);
			this.rsrcListNode = rsrcListNode;
			this.nodes = nodes;
		}

		public string Description => dnSpy_AsmEditor_Resources.CreateFileResourceCommand2;

		public void Execute() {
			foreach (var node in nodes) {
				module.Resources.Add(node.Resource);
				rsrcListNode.TreeNode.AddChild(node.TreeNode);
			}
		}

		public void Undo() {
			for (int i = nodes.Length - 1; i >= 0; i--) {
				var node = nodes[i];
				bool b = rsrcListNode.TreeNode.Children.Remove(node.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				b = module.Resources.Remove(node.Resource);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
			}
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return rsrcListNode; }
		}
	}

	[DebuggerDisplay("{Description}")]
	abstract class CreateResourceTreeNodeCommand : IUndoCommand {
		readonly ModuleDef module;
		readonly ResourcesFolderNode rsrcListNode;
		readonly ResourceNode resTreeNode;

		protected CreateResourceTreeNodeCommand(ResourcesFolderNode rsrcListNode, ResourceNode resTreeNode) {
			module = rsrcListNode.GetModule();
			Debug.Assert(module != null);
			this.rsrcListNode = rsrcListNode;
			this.resTreeNode = resTreeNode;
		}

		public abstract string Description { get; }

		public void Execute() {
			module.Resources.Add(resTreeNode.Resource);
			rsrcListNode.TreeNode.AddChild(resTreeNode.TreeNode);
		}

		public void Undo() {
			bool b = rsrcListNode.TreeNode.Children.Remove(resTreeNode.TreeNode);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
			b = module.Resources.Remove(resTreeNode.Resource);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return rsrcListNode; }
		}
	}

	sealed class CreateMultiFileResourceCommand : CreateResourceTreeNodeCommand {
		[ExportMenuItem(Header = "res:CreateMultiFileResourceCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 110)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateMultiFileResourceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateMultiFileResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateMultiFileResourceCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 110)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateMultiFileResourceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateMultiFileResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateMultiFileResourceCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 110)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && CreateMultiFileResourceCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => CreateMultiFileResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => ResUtils.CanExecuteResourceListCommand(nodes);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = ResUtils.GetResourceListTreeNode(nodes);

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var options = new ResourceOptions {
				ResourceType = ResourceType.Embedded,
				Name = "my.resources",
				Attributes = ManifestResourceAttributes.Public,
			};
			var data = new ResourceVM(options, module);
			var win = new ResourceDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateMultiFileResourceCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var outStream = new MemoryStream();
			ResourceWriter.Write(module, outStream, new ResourceElementSet());
			var er = new EmbeddedResource(data.Name, outStream.ToArray(), data.Attributes);
			var treeView = appService.DocumentTreeView.TreeView;
			var treeNodeGroup = appService.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ResourceTreeNodeGroup);
			var node = (ResourceNode)treeView.Create(resourceNodeFactory.Create(module, er, treeNodeGroup)).Data;

			undoCommandService.Value.Add(new CreateMultiFileResourceCommand(rsrcListNode, node));
			appService.DocumentTabService.FollowReference(node);
		}

		CreateMultiFileResourceCommand(ResourcesFolderNode rsrcListNode, ResourceNode resTreeNode)
			: base(rsrcListNode, resTreeNode) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.CreateMultiFileResourceCommand2;
	}

	sealed class CreateAssemblyLinkedResourceCommand : CreateResourceTreeNodeCommand {
		[ExportMenuItem(Header = "res:CreateAssemblyLinkedResourceCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 120)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateAssemblyLinkedResourceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateAssemblyLinkedResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateAssemblyLinkedResourceCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 120)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateAssemblyLinkedResourceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateAssemblyLinkedResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateAssemblyLinkedResourceCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 120)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && CreateAssemblyLinkedResourceCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => CreateAssemblyLinkedResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => ResUtils.CanExecuteResourceListCommand(nodes);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = ResUtils.GetResourceListTreeNode(nodes);

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var options = new ResourceOptions {
				ResourceType = ResourceType.AssemblyLinked,
				Name = "asmlinked",
				Attributes = ManifestResourceAttributes.Public,
				Assembly = module.CorLibTypes.AssemblyRef,
			};
			var data = new ResourceVM(options, module);
			var win = new ResourceDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateAssemblyLinkedResourceCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var treeView = appService.DocumentTreeView.TreeView;
			var treeNodeGroup = appService.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ResourceTreeNodeGroup);
			var node = (ResourceNode)treeView.Create(resourceNodeFactory.Create(module, new AssemblyLinkedResource(data.Name, data.Assembly, data.Attributes), treeNodeGroup)).Data;
			undoCommandService.Value.Add(new CreateAssemblyLinkedResourceCommand(rsrcListNode, node));
			appService.DocumentTabService.FollowReference(node);
		}

		CreateAssemblyLinkedResourceCommand(ResourcesFolderNode rsrcListNode, ResourceNode resTreeNode)
			: base(rsrcListNode, resTreeNode) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.CreateAssemblyLinkedResourceCommand2;
	}

	sealed class CreateFileLinkedResourceCommand : CreateResourceTreeNodeCommand {
		[ExportMenuItem(Header = "res:CreateFileLinkedResourceCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 130)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateFileLinkedResourceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateFileLinkedResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateFileLinkedResourceCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 130)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateFileLinkedResourceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateFileLinkedResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateFileLinkedResourceCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 130)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && CreateFileLinkedResourceCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => CreateFileLinkedResourceCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => ResUtils.CanExecuteResourceListCommand(nodes);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = ResUtils.GetResourceListTreeNode(nodes);

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var options = new ResourceOptions {
				ResourceType = ResourceType.Linked,
				Name = "filelinked",
				Attributes = ManifestResourceAttributes.Public,
				File = new FileDefUser("somefile", dnlib.DotNet.FileAttributes.ContainsNoMetaData, Array.Empty<byte>()),
			};
			var data = new ResourceVM(options, module);
			var win = new ResourceDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateFileLinkedResourceCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var opts = data.CreateResourceOptions();
			var treeView = appService.DocumentTreeView.TreeView;
			var treeNodeGroup = appService.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ResourceTreeNodeGroup);
			var node = (ResourceNode)treeView.Create(resourceNodeFactory.Create(module, new LinkedResource(opts.Name, opts.File, opts.Attributes), treeNodeGroup)).Data;
			undoCommandService.Value.Add(new CreateFileLinkedResourceCommand(rsrcListNode, node));
			appService.DocumentTabService.FollowReference(node);
		}

		CreateFileLinkedResourceCommand(ResourcesFolderNode rsrcListNode, ResourceNode resTreeNode)
			: base(rsrcListNode, resTreeNode) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.CreateFileLinkedResourceCommand2;
	}

	[DebuggerDisplay("{Description}")]
	sealed class ResourceSettingsCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 80)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => ResourceSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ResourceSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 90)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => ResourceSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ResourceSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SETTINGS, Order = 90)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) => ResourceSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => ResourceSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length == 1 && nodes[0] is ResourceNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcNode = (ResourceNode)nodes[0];

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new ResourceVM(new ResourceOptions(rsrcNode.Resource), module);
			var win = new ResourceDlg();
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandService.Value.Add(new ResourceSettingsCommand(rsrcNode, data.CreateResourceOptions()));
		}

		readonly ResourceNode rsrcNode;
		readonly ResourceOptions newOptions;
		readonly ResourceOptions origOptions;
		readonly DocumentTreeNodeData origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;

		ResourceSettingsCommand(ResourceNode rsrcNode, ResourceOptions options) {
			this.rsrcNode = rsrcNode;
			newOptions = options;
			origOptions = new ResourceOptions(rsrcNode.Resource);

			origParentNode = (DocumentTreeNodeData)rsrcNode.TreeNode.Parent.Data;
			origParentChildIndex = origParentNode.TreeNode.Children.IndexOf(rsrcNode.TreeNode);
			Debug.Assert(origParentChildIndex >= 0);
			if (origParentChildIndex < 0)
				throw new InvalidOperationException();

			nameChanged = origOptions.Name != newOptions.Name;
		}

		public string Description => dnSpy_AsmEditor_Resources.EditResourceCommand2;

		public void Execute() {
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.TreeNode.Children.Count && origParentNode.TreeNode.Children[origParentChildIndex] == rsrcNode.TreeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.TreeNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(rsrcNode.Resource);

				origParentNode.TreeNode.AddChild(rsrcNode.TreeNode);
			}
			else
				newOptions.CopyTo(rsrcNode.Resource);
			rsrcNode.TreeNode.RefreshUI();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = origParentNode.TreeNode.Children.Remove(rsrcNode.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(rsrcNode.Resource);
				origParentNode.TreeNode.Children.Insert(origParentChildIndex, rsrcNode.TreeNode);
			}
			else
				origOptions.CopyTo(rsrcNode.Resource);
			rsrcNode.TreeNode.RefreshUI();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return rsrcNode; }
		}
	}

	[DebuggerDisplay("{Description}")]
	abstract class CreateResourceElementCommandBase : IUndoCommand {
		readonly ModuleDef module;
		readonly ResourceElementSetNode rsrcSetNode;
		readonly ResourceElementNode[] nodes;
		readonly Resource resource;
		readonly int resourceIndex;
		Resource newResource;

		protected CreateResourceElementCommandBase(ResourceElementSetNode rsrcSetNode, ResourceElementNode[] nodes) {
			module = rsrcSetNode.GetModule();
			Debug.Assert(module != null);
			this.rsrcSetNode = rsrcSetNode;
			this.nodes = nodes;
			resource = rsrcSetNode.Resource;
			resourceIndex = module.Resources.IndexOf(resource);
			Debug.Assert(resourceIndex >= 0);
			if (resourceIndex < 0)
				throw new InvalidOperationException();
		}

		public abstract string Description { get; }

		public void Execute() {
			Debug.Assert(resource == rsrcSetNode.Resource);
			Debug.Assert(module.Resources[resourceIndex] == resource);
			foreach (var node in nodes)
				rsrcSetNode.TreeNode.AddChild(node.TreeNode);
			if (newResource == null) {
				rsrcSetNode.RegenerateEmbeddedResource();
				newResource = rsrcSetNode.Resource;
			}
			else
				rsrcSetNode.Resource = newResource;
			module.Resources[resourceIndex] = newResource;
		}

		public void Undo() {
			for (int i = nodes.Length - 1; i >= 0; i--) {
				var node = nodes[i];
				bool b = rsrcSetNode.TreeNode.Children.Remove(node.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
			}
			rsrcSetNode.Resource = resource;
			module.Resources[resourceIndex] = resource;
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return rsrcSetNode; }
		}
	}

	sealed class CreateImageResourceElementCommand : CreateResourceElementCommandBase {
		[ExportMenuItem(Header = "res:CreateBitMapIconResourceCommand", Icon = DsImagesAttribute.NewImage, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 140)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateImageResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateImageResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateBitMapIconResourceCommand", Icon = DsImagesAttribute.NewImage, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 140)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateImageResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateImageResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateBitMapIconResourceCommand", Icon = DsImagesAttribute.NewImage, Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 140)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && CreateImageResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => CreateImageResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is ResourceElementSetNode || (nodes[0].TreeNode.Parent != null && nodes[0].TreeNode.Parent.Data is ResourceElementSetNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcSetNode = nodes[0] as ResourceElementSetNode;
			if (rsrcSetNode == null)
				rsrcSetNode = nodes[0].TreeNode.Parent.Data as ResourceElementSetNode;
			Debug.Assert(rsrcSetNode != null);

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var dlg = new WF.OpenFileDialog {
				RestoreDirectory = true,
				Multiselect = true,
				Filter = PickFilenameConstants.ImagesFilter,
			};
			if (dlg.ShowDialog() != WF.DialogResult.OK)
				return;
			var fnames = dlg.FileNames;
			if (fnames.Length == 0)
				return;

			var newNodes = new List<ResourceElementNode>(fnames.Length);
			var treeView = appService.DocumentTreeView.TreeView;
			var treeNodeGroup = appService.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ResourceElementTreeNodeGroup);
			string error = null;
			for (int i = 0; i < fnames.Length; i++) {
				var fn = fnames[i];
				try {
					newNodes.Add((ResourceElementNode)treeView.Create(resourceNodeFactory.Create(module, SerializationUtilities.CreateSerializedImage(fn), treeNodeGroup)).Data);
				}
				catch (Exception ex) {
					if (error == null)
						error = string.Format(dnSpy_AsmEditor_Resources.Error_ReadingFiles, ex.Message);
				}
			}
			if (error != null)
				MsgBox.Instance.Show(error);
			if (newNodes.Count == 0)
				return;

			undoCommandService.Value.Add(new CreateImageResourceElementCommand(rsrcSetNode, newNodes.ToArray()));
			appService.DocumentTabService.FollowReference(newNodes[0]);
		}

		CreateImageResourceElementCommand(ResourceElementSetNode rsrcSetNode, ResourceElementNode[] nodes)
			: base(rsrcSetNode, nodes) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.CreateBitMapIconResourceCommand2;
	}

	sealed class CreateImageListResourceElementCommand : CreateResourceElementCommandBase {
		[ExportMenuItem(Header = "res:CreateImageListStreamerResourceCommand", Icon = DsImagesAttribute.NewImage, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 150)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateImageListResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateImageListResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateImageListStreamerResourceCommand", Icon = DsImagesAttribute.NewImage, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 150)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateImageListResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateImageListResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateImageListStreamerResourceCommand", Icon = DsImagesAttribute.NewImage, Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 150)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && CreateImageListResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => CreateImageListResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is ResourceElementSetNode || (nodes[0].TreeNode.Parent != null && nodes[0].TreeNode.Parent.Data is ResourceElementSetNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcSetNode = nodes[0] as ResourceElementSetNode;
			if (rsrcSetNode == null)
				rsrcSetNode = nodes[0].TreeNode.Parent.Data as ResourceElementSetNode;
			Debug.Assert(rsrcSetNode != null);

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new ImageListVM(new ImageListOptions() { Name = "my.ImageStream" });
			var win = new ImageListDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateImageListStreamerResourceCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			if (data.ImageListStreamerVM.Collection.Count == 0) {
				MsgBox.Instance.Show(dnSpy_AsmEditor_Resources.Error_EmptyImageList);
				return;
			}

			var listOpts = data.CreateImageListOptions();
			ResourceElementOptions opts = null;
			string error;
			try {
				opts = new ResourceElementOptions(SerializedImageListStreamerUtilities.Serialize(listOpts));
				error = SerializedImageListStreamerUtilities.CheckCanUpdateData(module, opts.Create());
			}
			catch (Exception ex) {
				error = string.Format(dnSpy_AsmEditor_Resources.Error_CouldNotSerializeImages, ex.Message);
			}
			if (!string.IsNullOrEmpty(error)) {
				MsgBox.Instance.Show(error);
				return;
			}

			var treeView = appService.DocumentTreeView.TreeView;
			var treeNodeGroup = appService.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ResourceElementTreeNodeGroup);
			var newNode = (SerializedImageListStreamerResourceElementNode)treeView.Create(resourceNodeFactory.Create(module, opts.Create(), treeNodeGroup)).Data;
			undoCommandService.Value.Add(new CreateImageListResourceElementCommand(rsrcSetNode, newNode));
			appService.DocumentTabService.FollowReference(newNode);
		}

		CreateImageListResourceElementCommand(ResourceElementSetNode rsrcSetNode, SerializedImageListStreamerResourceElementNode node)
			: base(rsrcSetNode, new ResourceElementNode[] { node }) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.CreateImageListStreamerResourceCommand2;
	}

	sealed class CreateByteArrayResourceElementCommand : CreateResourceElementCommandBase {
		[ExportMenuItem(Header = "res:CreateByteArrayResourceCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 160)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateByteArrayResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateByteArrayResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateByteArrayResourceCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 170)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateByteArrayResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateByteArrayResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateByteArrayResourceCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 170)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && CreateByteArrayResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => CreateByteArrayResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is ResourceElementSetNode || (nodes[0].TreeNode.Parent != null && nodes[0].TreeNode.Parent.Data is ResourceElementSetNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;
			Execute(undoCommandService, appService, resourceNodeFactory, nodes, ResourceTypeCode.ByteArray, (a, b) => new CreateByteArrayResourceElementCommand(a, b));
		}

		internal static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, DocumentTreeNodeData[] nodes, ResourceTypeCode typeCode, Func<ResourceElementSetNode, ResourceElementNode[], IUndoCommand> createCommand) {
			var rsrcSetNode = nodes[0] as ResourceElementSetNode;
			if (rsrcSetNode == null)
				rsrcSetNode = nodes[0].TreeNode.Parent.Data as ResourceElementSetNode;
			Debug.Assert(rsrcSetNode != null);

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var dlg = new WF.OpenFileDialog {
				RestoreDirectory = true,
				Multiselect = true,
				Filter = PickFilenameConstants.AnyFilenameFilter,
			};
			if (dlg.ShowDialog() != WF.DialogResult.OK)
				return;
			var fnames = dlg.FileNames;
			if (fnames.Length == 0)
				return;

			var newNodes = new ResourceElementNode[fnames.Length];
			var treeView = appService.DocumentTreeView.TreeView;
			var treeNodeGroup = appService.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ResourceElementTreeNodeGroup);
			for (int i = 0; i < fnames.Length; i++) {
				var fn = fnames[i];
				try {
					var rsrcElem = new ResourceElement {
						Name = Path.GetFileName(fn),
						ResourceData = new BuiltInResourceData(typeCode, File.ReadAllBytes(fn)),
					};
					newNodes[i] = (ResourceElementNode)treeView.Create(resourceNodeFactory.Create(module, rsrcElem, treeNodeGroup)).Data;
				}
				catch (Exception ex) {
					MsgBox.Instance.Show(string.Format(dnSpy_AsmEditor_Resources.Error_ReadingFiles, ex.Message));
					return;
				}
			}

			undoCommandService.Value.Add(createCommand(rsrcSetNode, newNodes.ToArray()));
			appService.DocumentTabService.FollowReference(newNodes[0]);
		}

		CreateByteArrayResourceElementCommand(ResourceElementSetNode rsrcSetNode, ResourceElementNode[] nodes)
			: base(rsrcSetNode, nodes) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.CreateByteArrayResourceCommand2;
	}

	sealed class CreateStreamResourceElementCommand : CreateResourceElementCommandBase {
		[ExportMenuItem(Header = "res:CreateStreamResourceCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 170)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateStreamResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateStreamResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateStreamResourceCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 180)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateStreamResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateStreamResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateStreamResourceCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 180)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && CreateStreamResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => CreateStreamResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is ResourceElementSetNode || (nodes[0].TreeNode.Parent != null && nodes[0].TreeNode.Parent.Data is ResourceElementSetNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;
			CreateByteArrayResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, nodes, ResourceTypeCode.Stream, (a, b) => new CreateStreamResourceElementCommand(a, b));
		}

		CreateStreamResourceElementCommand(ResourceElementSetNode rsrcSetNode, ResourceElementNode[] nodes)
			: base(rsrcSetNode, nodes) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.CreateStreamResourceCommand2;
	}

	sealed class CreateResourceElementCommand : CreateResourceElementCommandBase {
		[ExportMenuItem(Header = "res:CreateResourceCommand", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 190)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;
			readonly IDocumentTreeViewSettings documentTreeViewSettings;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, IDocumentTreeViewSettings documentTreeViewSettings) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
				this.documentTreeViewSettings = documentTreeViewSettings;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, documentTreeViewSettings, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateResourceCommand", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 190)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;
			readonly IDocumentTreeViewSettings documentTreeViewSettings;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, IDocumentTreeViewSettings documentTreeViewSettings)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
				this.documentTreeViewSettings = documentTreeViewSettings;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, documentTreeViewSettings, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateResourceCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 190)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IResourceNodeFactory resourceNodeFactory;
			readonly IDocumentTreeViewSettings documentTreeViewSettings;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, IDocumentTreeViewSettings documentTreeViewSettings)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.resourceNodeFactory = resourceNodeFactory;
				this.documentTreeViewSettings = documentTreeViewSettings;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && CreateResourceElementCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => CreateResourceElementCommand.Execute(undoCommandService, appService, resourceNodeFactory, documentTreeViewSettings, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is ResourceElementSetNode || (nodes[0].TreeNode.Parent != null && nodes[0].TreeNode.Parent.Data is ResourceElementSetNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IResourceNodeFactory resourceNodeFactory, IDocumentTreeViewSettings documentTreeViewSettings, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcSetNode = nodes[0] as ResourceElementSetNode;
			if (rsrcSetNode == null)
				rsrcSetNode = nodes[0].TreeNode.Parent.Data as ResourceElementSetNode;
			Debug.Assert(rsrcSetNode != null);

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var options = new ResourceElementOptions(new ResourceElement {
				Name = string.Empty,
				ResourceData = new BuiltInResourceData(ResourceTypeCode.String, string.Empty),
			});
			var data = new ResourceElementVM(options, module, documentTreeViewSettings.DeserializeResources);
			var win = new ResourceElementDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateResourceCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var opts = data.CreateResourceElementOptions();
			var treeView = appService.DocumentTreeView.TreeView;
			var treeNodeGroup = appService.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.ResourceElementTreeNodeGroup);
			var node = (ResourceElementNode)treeView.Create(resourceNodeFactory.Create(module, opts.Create(), treeNodeGroup)).Data;
			undoCommandService.Value.Add(new CreateResourceElementCommand(rsrcSetNode, node));
			appService.DocumentTabService.FollowReference(node);
		}

		CreateResourceElementCommand(ResourceElementSetNode rsrcSetNode, ResourceElementNode node)
			: base(rsrcSetNode, new[] { node }) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.CreateResourceCommand2;
	}

	[DebuggerDisplay("{Description}")]
	abstract class ResourceElementSettingsBaseCommand : IUndoCommand {
		readonly ModuleDef module;
		readonly Resource resource;
		readonly int resourceIndex;
		Resource newResource;
		readonly ResourceElementSetNode rsrcSetNode;
		readonly ResourceElementNode rsrcElNode;
		readonly ResourceElement newOptions;
		readonly ResourceElement origOptions;
		readonly int origParentChildIndex;
		readonly bool nameChanged;

		protected ResourceElementSettingsBaseCommand(ResourceElementNode rsrcElNode, ResourceElementOptions options) {
			rsrcSetNode = (ResourceElementSetNode)rsrcElNode.TreeNode.Parent.Data;
			this.rsrcElNode = rsrcElNode;
			newOptions = options.Create();
			origOptions = rsrcElNode.ResourceElement;

			module = rsrcSetNode.GetModule();
			Debug.Assert(module != null);
			resource = rsrcSetNode.Resource;
			resourceIndex = module.Resources.IndexOf(resource);
			Debug.Assert(resourceIndex >= 0);
			if (resourceIndex < 0)
				throw new InvalidOperationException();

			origParentChildIndex = rsrcSetNode.TreeNode.Children.IndexOf(rsrcElNode.TreeNode);
			Debug.Assert(origParentChildIndex >= 0);
			if (origParentChildIndex < 0)
				throw new InvalidOperationException();

			nameChanged = origOptions.Name != newOptions.Name;
		}

		public abstract string Description { get; }

		public void Execute() {
			Debug.Assert(resource == rsrcSetNode.Resource);
			Debug.Assert(module.Resources[resourceIndex] == resource);
			if (nameChanged) {
				bool b = origParentChildIndex < rsrcSetNode.TreeNode.Children.Count && rsrcSetNode.TreeNode.Children[origParentChildIndex] == rsrcElNode.TreeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				rsrcSetNode.TreeNode.Children.RemoveAt(origParentChildIndex);
				rsrcElNode.UpdateData(newOptions);

				rsrcSetNode.TreeNode.AddChild(rsrcElNode.TreeNode);
			}
			else
				rsrcElNode.UpdateData(newOptions);

			if (newResource == null) {
				rsrcSetNode.RegenerateEmbeddedResource();
				newResource = rsrcSetNode.Resource;
			}
			else
				rsrcSetNode.Resource = newResource;
			module.Resources[resourceIndex] = newResource;
			rsrcElNode.TreeNode.RefreshUI();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = rsrcSetNode.TreeNode.Children.Remove(rsrcElNode.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				rsrcElNode.UpdateData(origOptions);
				rsrcSetNode.TreeNode.Children.Insert(origParentChildIndex, rsrcElNode.TreeNode);
			}
			else
				rsrcElNode.UpdateData(origOptions);

			rsrcSetNode.Resource = resource;
			module.Resources[resourceIndex] = resource;
			rsrcElNode.TreeNode.RefreshUI();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return rsrcElNode; }
		}
	}

	sealed class ResourceElementSettingsCommand : ResourceElementSettingsBaseCommand {
		[ExportMenuItem(Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 90)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IDocumentTreeViewSettings documentTreeViewSettings;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IDocumentTreeViewSettings documentTreeViewSettings) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.documentTreeViewSettings = documentTreeViewSettings;
			}

			public override bool IsVisible(AsmEditorContext context) => ResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ResourceElementSettingsCommand.Execute(undoCommandService, appService, documentTreeViewSettings, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 100)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IDocumentTreeViewSettings documentTreeViewSettings;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IDocumentTreeViewSettings documentTreeViewSettings)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.documentTreeViewSettings = documentTreeViewSettings;
			}

			public override bool IsVisible(AsmEditorContext context) => ResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ResourceElementSettingsCommand.Execute(undoCommandService, appService, documentTreeViewSettings, context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SETTINGS, Order = 100)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;
			readonly IDocumentTreeViewSettings documentTreeViewSettings;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IDocumentTreeViewSettings documentTreeViewSettings)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
				this.documentTreeViewSettings = documentTreeViewSettings;
			}

			public override bool IsEnabled(CodeContext context) => ResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => ResourceElementSettingsCommand.Execute(undoCommandService, appService, documentTreeViewSettings, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is BuiltInResourceElementNode ||
			nodes[0] is UnknownSerializedResourceElementNode);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, IDocumentTreeViewSettings documentTreeViewSettings, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcElNode = (ResourceElementNode)nodes[0];
			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var options = new ResourceElementOptions(rsrcElNode.ResourceElement);
			var data = new ResourceElementVM(options, module, documentTreeViewSettings.DeserializeResources);
			data.CanChangeType = false;
			var win = new ResourceElementDlg();
			win.Title = dnSpy_AsmEditor_Resources.EditResourceCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var opts = data.CreateResourceElementOptions();
			string error;
			try {
				error = rsrcElNode.CheckCanUpdateData(opts.Create());
			}
			catch (Exception ex) {
				error = string.Format(dnSpy_AsmEditor_Resources.Error_InvalidResourceData, ex.Message);
			}
			if (!string.IsNullOrEmpty(error)) {
				MsgBox.Instance.Show(error);
				return;
			}

			undoCommandService.Value.Add(new ResourceElementSettingsCommand(rsrcElNode, opts));
		}

		ResourceElementSettingsCommand(ResourceElementNode rsrcElNode, ResourceElementOptions options)
			: base(rsrcElNode, options) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.EditResourceCommand2;
	}

	sealed class ImageResourceElementSettingsCommand : ResourceElementSettingsBaseCommand {
		[ExportMenuItem(Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 100)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => ImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ImageResourceElementSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 110)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => ImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => ImageResourceElementSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SETTINGS, Order = 110)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) => ImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => ImageResourceElementSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 && nodes[0] is ImageResourceElementNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var imgRsrcElNode = (ImageResourceElementNode)nodes[0];

			var options = new ResourceElementOptions(imgRsrcElNode.ResourceElement);
			var data = new ImageResourceElementVM(options);
			var win = new ImageResourceElementDlg();
			win.Title = dnSpy_AsmEditor_Resources.EditResourceCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var opts = data.CreateResourceElementOptions();
			string error;
			try {
				error = imgRsrcElNode.CheckCanUpdateData(opts.Create());
			}
			catch (Exception ex) {
				error = string.Format(dnSpy_AsmEditor_Resources.Error_NewResourceDataMustBeImage, ex.Message);
			}
			if (!string.IsNullOrEmpty(error)) {
				MsgBox.Instance.Show(error);
				return;
			}

			undoCommandService.Value.Add(new ImageResourceElementSettingsCommand(imgRsrcElNode, opts));
		}

		ImageResourceElementSettingsCommand(ResourceElementNode rsrcElNode, ResourceElementOptions options)
			: base(rsrcElNode, options) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.EditResourceCommand2;
	}

	sealed class SerializedImageResourceElementSettingsCommand : ResourceElementSettingsBaseCommand {
		[ExportMenuItem(Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 110)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => SerializedImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => SerializedImageResourceElementSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 120)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => SerializedImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => SerializedImageResourceElementSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SETTINGS, Order = 120)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) => SerializedImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => SerializedImageResourceElementSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length == 1 && nodes[0] is SerializedImageResourceElementNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var imgRsrcElNode = (SerializedImageResourceElementNode)nodes[0];
			var options = new ResourceElementOptions(imgRsrcElNode.GetAsRawImage());
			var data = new ImageResourceElementVM(options);
			var win = new ImageResourceElementDlg();
			win.Title = dnSpy_AsmEditor_Resources.EditResourceCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var opts = data.CreateResourceElementOptions();
			string error;
			try {
				opts = new ResourceElementOptions(SerializedImageUtilities.Serialize(opts.Create()));
				error = imgRsrcElNode.CheckCanUpdateData(opts.Create());
			}
			catch (Exception ex) {
				error = string.Format(dnSpy_AsmEditor_Resources.Error_NewResourceDataMustBeImage, ex.Message);
			}
			if (!string.IsNullOrEmpty(error)) {
				MsgBox.Instance.Show(error);
				return;
			}

			undoCommandService.Value.Add(new SerializedImageResourceElementSettingsCommand(imgRsrcElNode, opts));
		}

		SerializedImageResourceElementSettingsCommand(ResourceElementNode rsrcElNode, ResourceElementOptions options)
			: base(rsrcElNode, options) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.EditResourceCommand2;
	}

	sealed class SerializedImageListStreamerResourceElementSettingsCommand : ResourceElementSettingsBaseCommand {
		[ExportMenuItem(Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 120)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => SerializedImageListStreamerResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => SerializedImageListStreamerResourceElementSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 130)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => SerializedImageListStreamerResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => SerializedImageListStreamerResourceElementSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:EditResourceCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SETTINGS, Order = 130)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) => SerializedImageListStreamerResourceElementSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => SerializedImageListStreamerResourceElementSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 && nodes[0] is SerializedImageListStreamerResourceElementNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var imgNode = (SerializedImageListStreamerResourceElementNode)nodes[0];
			var options = new ImageListOptions(imgNode.ImageListOptions);
			var data = new ImageListVM(options);
			var win = new ImageListDlg();
			win.Title = dnSpy_AsmEditor_Resources.EditResourceCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var listOpts = data.CreateImageListOptions();

			if (listOpts.ImageSources.Count == 0) {
				MsgBox.Instance.Show(dnSpy_AsmEditor_Resources.Error_EmptyImageList);
				return;
			}

			ResourceElementOptions opts = null;
			string error;
			try {
				opts = new ResourceElementOptions(SerializedImageListStreamerUtilities.Serialize(listOpts));
				error = imgNode.CheckCanUpdateData(opts.Create());
			}
			catch (Exception ex) {
				error = string.Format(dnSpy_AsmEditor_Resources.Error_CouldNotSerializeImages, ex.Message);
			}
			if (!string.IsNullOrEmpty(error)) {
				MsgBox.Instance.Show(error);
				return;
			}

			undoCommandService.Value.Add(new SerializedImageListStreamerResourceElementSettingsCommand(imgNode, opts));
		}

		SerializedImageListStreamerResourceElementSettingsCommand(ResourceElementNode rsrcElNode, ResourceElementOptions options)
			: base(rsrcElNode, options) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.EditResourceCommand2;
	}
}
