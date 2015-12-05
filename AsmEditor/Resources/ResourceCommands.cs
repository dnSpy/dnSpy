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
using System.IO;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Menus;
using dnSpy.MVVM;
using dnSpy.Options;
using dnSpy.Shared.UI.Menus;
using dnSpy.TreeNodes;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;
using WF = System.Windows.Forms;

namespace dnSpy.AsmEditor.Resources {
	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		void IPlugin.EarlyInit() {
		}

		public void OnLoaded() {
			MainWindow.Instance.TreeView.AddCommandBinding(ApplicationCommands.Delete, new EditMenuHandlerCommandProxy(new DeleteResourceCommand.EditMenuCommand()));
			MainWindow.Instance.TreeView.AddCommandBinding(ApplicationCommands.Delete, new EditMenuHandlerCommandProxy(new DeleteResourceElementCommand.EditMenuCommand()));
			MainWindow.Instance.CodeBindings.Add(EditingCommands.Delete, new CodeContextMenuHandlerCommandProxy(new DeleteResourceCommand.CodeCommand()), ModifierKeys.None, Key.Delete);
			MainWindow.Instance.CodeBindings.Add(EditingCommands.Delete, new CodeContextMenuHandlerCommandProxy(new DeleteResourceElementCommand.CodeCommand()), ModifierKeys.None, Key.Delete);
			Utils.InstallSettingsCommand(new ResourceSettingsCommand.EditMenuCommand(), new ResourceSettingsCommand.CodeCommand());
			Utils.InstallSettingsCommand(new ResourceElementSettingsCommand.EditMenuCommand(), new ResourceElementSettingsCommand.CodeCommand());
			Utils.InstallSettingsCommand(new ImageResourceElementSettingsCommand.EditMenuCommand(), new ImageResourceElementSettingsCommand.CodeCommand());
			Utils.InstallSettingsCommand(new SerializedImageResourceElementSettingsCommand.EditMenuCommand(), new SerializedImageResourceElementSettingsCommand.CodeCommand());
			Utils.InstallSettingsCommand(new SerializedImageListStreamerResourceElementSettingsCommand.EditMenuCommand(), new SerializedImageListStreamerResourceElementSettingsCommand.CodeCommand());
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteResourceCommand : IUndoCommand {
		const string CMD_NAME = "Delete Resource";
		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_FILES_ASMED_DELETE, Order = 80)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return DeleteResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteResourceCommand.Execute(context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteResourceCommand.GetHeader(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 80)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return DeleteResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteResourceCommand.Execute(context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteResourceCommand.GetHeader(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_CODE_ASMED_DELTE, Order = 80)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					DeleteResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				DeleteResourceCommand.Execute(context.Nodes);
			}

			public override string GetHeader(CodeContext context) {
				return DeleteResourceCommand.GetHeader(context.Nodes);
			}
		}

		static string GetHeader(ILSpyTreeNode[] nodes) {
			if (nodes.Length == 1)
				return string.Format("Delete {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format("Delete {0} resources", nodes.Length);
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is ResourceTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcNodes = nodes.Select(a => (ResourceTreeNode)a).ToArray();
			UndoCommandManager.Instance.Add(new DeleteResourceCommand(rsrcNodes));
		}

		public struct DeleteModelNodes {
			ModelInfo[] infos;

			struct ModelInfo {
				public readonly ModuleDef OwnerModule;
				public readonly int Index;

				public ModelInfo(ModuleDef module, Resource rsrc) {
					this.OwnerModule = module;
					this.Index = module.Resources.IndexOf(rsrc);
					Debug.Assert(this.Index >= 0);
					if (this.Index < 0)
						throw new InvalidOperationException();
				}
			}

			public void Delete(ResourceTreeNode[] nodes, ILSpyTreeNode[] parents) {
				Debug.Assert(infos == null);
				if (infos != null)
					throw new InvalidOperationException();

				infos = new ModelInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];

					var module = ILSpyTreeNode.GetModule(parents[i]);
					Debug.Assert(module != null);
					if (module == null)
						throw new InvalidOperationException();
					var info = new ModelInfo(module, node.Resource);
					infos[i] = info;
					info.OwnerModule.Resources.RemoveAt(info.Index);
				}
			}

			public void Restore(ResourceTreeNode[] nodes) {
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

		DeletableNodes<ResourceTreeNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteResourceCommand(ResourceTreeNode[] rsrcNodes) {
			this.nodes = new DeletableNodes<ResourceTreeNode>(rsrcNodes);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			nodes.Delete();
			modelNodes.Delete(nodes.Nodes, nodes.Parents);
		}

		public void Undo() {
			modelNodes.Restore(nodes.Nodes);
			nodes.Restore();
		}

		public IEnumerable<object> ModifiedObjects {
			get { return nodes.Nodes; }
		}

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteResourceElementCommand : IUndoCommand {
		const string CMD_NAME = "Delete Resource";
		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_FILES_ASMED_DELETE, Order = 90)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return DeleteResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteResourceElementCommand.Execute(context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteResourceElementCommand.GetHeader(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 90)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return DeleteResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteResourceElementCommand.Execute(context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteResourceElementCommand.GetHeader(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_CODE_ASMED_DELTE, Order = 90)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					DeleteResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				DeleteResourceElementCommand.Execute(context.Nodes);
			}

			public override string GetHeader(CodeContext context) {
				return DeleteResourceElementCommand.GetHeader(context.Nodes);
			}
		}

		static string GetHeader(ILSpyTreeNode[] nodes) {
			if (nodes.Length == 1)
				return string.Format("Delete {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format("Delete {0} resources", nodes.Length);
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is ResourceElementTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcNodes = nodes.Select(a => (ResourceElementTreeNode)a).ToArray();
			UndoCommandManager.Instance.Add(new DeleteResourceElementCommand(rsrcNodes));
		}

		sealed class ModuleInfo {
			public readonly ResourceTreeNode Node;
			readonly ModuleDef Module;
			readonly int Index;
			readonly Resource Resource;
			Resource NewResource;

			public ModuleInfo(ResourceTreeNode node) {
				if (node == null)
					throw new InvalidOperationException();
				this.Node = node;
				var module = ILSpyTreeNode.GetModule(node);
				Debug.Assert(module != null);
				if (module == null)
					throw new InvalidOperationException();
				this.Module = module;
				this.Index = module.Resources.IndexOf(node.Resource);
				Debug.Assert(this.Index >= 0);
				if (this.Index < 0)
					throw new InvalidOperationException();
				this.Resource = node.Resource;
			}

			public void Replace() {
				Debug.Assert(this.Index < this.Module.Resources.Count && this.Module.Resources[this.Index] == this.Node.Resource);
				if (NewResource == null) {
					this.Node.RegenerateEmbeddedResource();
					this.NewResource = this.Node.Resource;
				}
				else
					this.Node.Resource = this.NewResource;
				this.Module.Resources[this.Index] = this.NewResource;
			}

			public void Restore() {
				Debug.Assert(this.Index < this.Module.Resources.Count && this.Module.Resources[this.Index] == this.Node.Resource);
				this.Node.Resource = this.Resource;
				this.Module.Resources[this.Index] = this.Resource;
			}
		}

		DeletableNodes<ResourceElementTreeNode> nodes;
		readonly List<ModuleInfo> savedResources = new List<ModuleInfo>();

		DeleteResourceElementCommand(ResourceElementTreeNode[] rsrcNodes) {
			this.nodes = new DeletableNodes<ResourceElementTreeNode>(rsrcNodes);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			Debug.Assert(savedResources.Count == 0);
			savedResources.AddRange(nodes.Nodes.Select(a => ILSpyTreeNode.GetNode<ResourceTreeNode>(a)).Distinct().Select(a => new ModuleInfo(a)));

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

		public IEnumerable<object> ModifiedObjects {
			get { return nodes.Nodes; }
		}

		public void Dispose() {
		}
	}

	abstract class SaveResourcesCommandBase : MenuItemBase {
		readonly bool useSubDirs;
		readonly ResourceDataType resourceDataType;

		protected SaveResourcesCommandBase(bool useSubDirs, ResourceDataType resourceDataType) {
			this.useSubDirs = useSubDirs;
			this.resourceDataType = resourceDataType;
		}

		protected abstract IResourceNode[] GetResourceNodes(IMenuItemContext context);

		IResourceNode[] Filter(IResourceNode[] nodes) {
			nodes = nodes.Where(a => a.GetResourceData(resourceDataType).Any()).ToArray();
			return nodes.Length == 0 ? null : nodes;
		}

		protected IResourceNode[] CodeGetResourceNodes(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
				return null;

			var @ref = context.FindByType<CodeReferenceSegment>();
			if (@ref == null)
				return null;
			var rsrcNode = @ref.Reference as IResourceNode;
			if (rsrcNode == null)
				return null;

			return Filter(new[] { rsrcNode });
		}

		protected IResourceNode[] FilesGetResourceNodes(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID))
				return null;

			var selNodes = context.FindByType<SharpTreeNode[]>();
			if (selNodes == null)
				return null;

			if (selNodes.Length == 1 && selNodes[0] is ResourceListTreeNode) {
				var rlist = (ResourceListTreeNode)selNodes[0];
				rlist.EnsureChildrenFiltered();
				selNodes = rlist.Children.Cast<ILSpyTreeNode>().ToArray();
			}
			return Filter(selNodes.Where(a => a is IResourceNode).Cast<IResourceNode>().ToArray());
		}

		protected ResourceData[] GetResourceData(IResourceNode[] nodes) {
			return SaveResources.GetResourceData(nodes, resourceDataType);
		}

		public override bool IsVisible(IMenuItemContext context) {
			return GetResourceNodes(context) != null;
		}

		public override void Execute(IMenuItemContext context) {
			SaveResources.Save(GetResourceNodes(context), useSubDirs, resourceDataType);
		}
	}

	static class SaveResourcesCommand {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_CODE_ASMED_SAVE, Order = 0)]
		sealed class CodeCommand : SaveResourcesCommandBase {
			public CodeCommand()
				: base(false, ResourceDataType.Deserialized) {
			}

			public override string GetHeader(IMenuItemContext context) {
				return GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			}

			protected override IResourceNode[] GetResourceNodes(IMenuItemContext context) {
				return CodeGetResourceNodes(context);
			}
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_FILES_ASMED_SAVE, Order = 0)]
		sealed class FilesCommand : SaveResourcesCommandBase {
			public FilesCommand()
				: base(false, ResourceDataType.Deserialized) {
			}

			public override string GetHeader(IMenuItemContext context) {
				return GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			}

			protected override IResourceNode[] GetResourceNodes(IMenuItemContext context) {
				return FilesGetResourceNodes(context);
			}
		}

		static string GetHeaderInternal(ResourceData[] infos) {
			if (infos.Length == 1)
				return string.Format("Save {0}", UIUtils.EscapeMenuItemHeader(infos[0].Name));
			return string.Format("Save {0} resources", infos.Length);
		}
	}

	static class SaveWithPathResourcesCommand {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_CODE_ASMED_SAVE, Order = 10)]
		sealed class CodeCommand : SaveResourcesCommandBase {
			public CodeCommand()
				: base(true, ResourceDataType.Deserialized) {
			}

			public override string GetHeader(IMenuItemContext context) {
				return GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			}

			protected override IResourceNode[] GetResourceNodes(IMenuItemContext context) {
				return CodeGetResourceNodes(context);
			}

			public override bool IsVisible(IMenuItemContext context) {
				return IsVisibleInternal(GetResourceData(GetResourceNodes(context)));
			}
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_FILES_ASMED_SAVE, Order = 10)]
		sealed class FilesCommand : SaveResourcesCommandBase {
			public FilesCommand()
				: base(true, ResourceDataType.Deserialized) {
			}

			public override string GetHeader(IMenuItemContext context) {
				return GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			}

			protected override IResourceNode[] GetResourceNodes(IMenuItemContext context) {
				return FilesGetResourceNodes(context);
			}

			public override bool IsVisible(IMenuItemContext context) {
				return IsVisibleInternal(GetResourceData(GetResourceNodes(context)));
			}
		}

		static string GetHeaderInternal(ResourceData[] infos) {
			return string.Format("Save {0} resources, use sub dirs", infos.Length);
		}

		internal static bool IsVisibleInternal(ResourceData[] infos) {
			return infos.Length > 1 &&
				infos.Any(a => a.Name.Contains('/') || a.Name.Contains('\\'));
		}
	}

	static class SaveRawResourcesCommand {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_CODE_ASMED_SAVE, Order = 20)]
		sealed class CodeCommand : SaveResourcesCommandBase {
			public CodeCommand()
				: base(false, ResourceDataType.Serialized) {
			}

			public override string GetHeader(IMenuItemContext context) {
				return GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			}

			protected override IResourceNode[] GetResourceNodes(IMenuItemContext context) {
				return CodeGetResourceNodes(context);
			}
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_FILES_ASMED_SAVE, Order = 20)]
		sealed class FilesCommand : SaveResourcesCommandBase {
			public FilesCommand()
				: base(false, ResourceDataType.Serialized) {
			}

			public override string GetHeader(IMenuItemContext context) {
				return GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			}

			protected override IResourceNode[] GetResourceNodes(IMenuItemContext context) {
				return FilesGetResourceNodes(context);
			}
		}

		static string GetHeaderInternal(ResourceData[] infos) {
			if (infos.Length == 1)
				return string.Format("Raw Save {0}", UIUtils.EscapeMenuItemHeader(infos[0].Name));
			return string.Format("Raw Save {0} resources", infos.Length);
		}
	}

	static class SaveRawWithPathResourcesCommand {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_CODE_ASMED_SAVE, Order = 30)]
		sealed class CodeCommand : SaveResourcesCommandBase {
			public CodeCommand()
				: base(true, ResourceDataType.Serialized) {
			}

			public override string GetHeader(IMenuItemContext context) {
				return GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			}

			protected override IResourceNode[] GetResourceNodes(IMenuItemContext context) {
				return CodeGetResourceNodes(context);
			}

			public override bool IsVisible(IMenuItemContext context) {
				return IsVisibleInternal(GetResourceData(GetResourceNodes(context)));
			}
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_FILES_ASMED_SAVE, Order = 30)]
		sealed class FilesCommand : SaveResourcesCommandBase {
			public FilesCommand()
				: base(true, ResourceDataType.Serialized) {
			}

			public override string GetHeader(IMenuItemContext context) {
				return GetHeaderInternal(GetResourceData(GetResourceNodes(context)));
			}

			protected override IResourceNode[] GetResourceNodes(IMenuItemContext context) {
				return FilesGetResourceNodes(context);
			}

			public override bool IsVisible(IMenuItemContext context) {
				return IsVisibleInternal(GetResourceData(GetResourceNodes(context)));
			}
		}

		static string GetHeaderInternal(ResourceData[] infos) {
			return string.Format("Raw Save {0} resources, use sub dirs", infos.Length);
		}

		static bool IsVisibleInternal(ResourceData[] infos) {
			return SaveWithPathResourcesCommand.IsVisibleInternal(infos);
		}
	}

	static class ResUtils {
		public static bool CanExecuteResourceListCommand(ILSpyTreeNode[] nodes) {
			return GetResourceListTreeNode(nodes) != null;
		}

		public static ResourceListTreeNode GetResourceListTreeNode(ILSpyTreeNode[] nodes) {
			if (nodes.Length != 1)
				return null;
			var rsrcListNode = nodes[0] as ResourceListTreeNode;
			if (rsrcListNode != null)
				return rsrcListNode;
			rsrcListNode = nodes[0].Parent as ResourceListTreeNode;
			if (rsrcListNode != null)
				return rsrcListNode;

			var asmNode = nodes[0] as AssemblyTreeNode;
			if (asmNode == null)
				return null;
			asmNode.EnsureChildrenFiltered();
			rsrcListNode = (ResourceListTreeNode)asmNode.Children.FirstOrDefault(a => a is ResourceListTreeNode);
			if (rsrcListNode == null)	// If not a module node
				return null;
			rsrcListNode.EnsureChildrenFiltered();
			if (rsrcListNode.Children.Count == 0)
				return rsrcListNode;
			return null;
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateFileResourceCommand : IUndoCommand {
		const string CMD_NAME = "Create File Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewResource", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 100)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateFileResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateFileResourceCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewResource", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 100)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateFileResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateFileResourceCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewResource", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 100)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					CreateFileResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				CreateFileResourceCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return ResUtils.CanExecuteResourceListCommand(nodes);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = ResUtils.GetResourceListTreeNode(nodes);

			var module = ILSpyTreeNode.GetModule(nodes[0]);
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

			var newNodes = new ResourceTreeNode[fnames.Length];
			for (int i = 0; i < fnames.Length; i++) {
				var fn = fnames[i];
				try {
					var rsrc = new EmbeddedResource(Path.GetFileName(fn), File.ReadAllBytes(fn), ManifestResourceAttributes.Public);
					newNodes[i] = ResourceFactory.Create(module, rsrc);
				}
				catch (Exception ex) {
					MainWindow.Instance.ShowMessageBox(string.Format("Error reading files: {0}", ex.Message));
					return;
				}
			}

			UndoCommandManager.Instance.Add(new CreateFileResourceCommand(rsrcListNode, newNodes));
			MainWindow.Instance.JumpToReference(newNodes[0]);
		}

		readonly ModuleDef module;
		readonly ResourceListTreeNode rsrcListNode;
		readonly ResourceTreeNode[] nodes;

		CreateFileResourceCommand(ResourceListTreeNode rsrcListNode, ResourceTreeNode[] nodes) {
			this.module = ILSpyTreeNode.GetModule(rsrcListNode);
			Debug.Assert(this.module != null);
			this.rsrcListNode = rsrcListNode;
			this.nodes = nodes;
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			foreach (var node in nodes) {
				module.Resources.Add(node.Resource);
				rsrcListNode.AddToChildren(node);
			}
		}

		public void Undo() {
			for (int i = nodes.Length - 1; i >= 0; i--) {
				var node = nodes[i];
				bool b = rsrcListNode.Children.Remove(node);
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

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	abstract class CreateResourceTreeNodeCommand : IUndoCommand {
		readonly ModuleDef module;
		readonly ResourceListTreeNode rsrcListNode;
		readonly ResourceTreeNode resTreeNode;

		protected CreateResourceTreeNodeCommand(ResourceListTreeNode rsrcListNode, ResourceTreeNode resTreeNode) {
			this.module = ILSpyTreeNode.GetModule(rsrcListNode);
			Debug.Assert(this.module != null);
			this.rsrcListNode = rsrcListNode;
			this.resTreeNode = resTreeNode;
		}

		public abstract string Description { get; }

		public void Execute() {
			module.Resources.Add(resTreeNode.Resource);
			rsrcListNode.AddToChildren(resTreeNode);
		}

		public void Undo() {
			bool b = rsrcListNode.Children.Remove(resTreeNode);
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

		public void Dispose() {
		}
	}

	sealed class CreateMultiFileResourceCommand : CreateResourceTreeNodeCommand {
		const string CMD_NAME = "Create Multi File Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewResourcesFile", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 110)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateMultiFileResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateMultiFileResourceCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewResourcesFile", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 110)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateMultiFileResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateMultiFileResourceCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewResourcesFile", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 110)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					CreateMultiFileResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				CreateMultiFileResourceCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return ResUtils.CanExecuteResourceListCommand(nodes);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = ResUtils.GetResourceListTreeNode(nodes);

			var module = ILSpyTreeNode.GetModule(nodes[0]);
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
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var node = new ResourceElementSetTreeNode(module, data.Name, data.Attributes);
			UndoCommandManager.Instance.Add(new CreateMultiFileResourceCommand(rsrcListNode, node));
			MainWindow.Instance.JumpToReference(node);
		}

		CreateMultiFileResourceCommand(ResourceListTreeNode rsrcListNode, ResourceTreeNode resTreeNode)
			: base(rsrcListNode, resTreeNode) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class CreateAssemblyLinkedResourceCommand : CreateResourceTreeNodeCommand {
		const string CMD_NAME = "Create Assembly Linked Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewAssembly", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 120)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateAssemblyLinkedResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateAssemblyLinkedResourceCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewAssembly", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 120)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateAssemblyLinkedResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateAssemblyLinkedResourceCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewAssembly", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 120)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					CreateAssemblyLinkedResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				CreateAssemblyLinkedResourceCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return ResUtils.CanExecuteResourceListCommand(nodes);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = ResUtils.GetResourceListTreeNode(nodes);

			var module = ILSpyTreeNode.GetModule(nodes[0]);
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
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var node = ResourceFactory.Create(module, new AssemblyLinkedResource(data.Name, data.Assembly, data.Attributes));
			UndoCommandManager.Instance.Add(new CreateAssemblyLinkedResourceCommand(rsrcListNode, node));
			MainWindow.Instance.JumpToReference(node);
		}

		CreateAssemblyLinkedResourceCommand(ResourceListTreeNode rsrcListNode, ResourceTreeNode resTreeNode)
			: base(rsrcListNode, resTreeNode) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class CreateFileLinkedResourceCommand : CreateResourceTreeNodeCommand {
		const string CMD_NAME = "Create File Linked Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 130)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateFileLinkedResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateFileLinkedResourceCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 130)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateFileLinkedResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateFileLinkedResourceCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewAssemblyModule", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 130)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					CreateFileLinkedResourceCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				CreateFileLinkedResourceCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return ResUtils.CanExecuteResourceListCommand(nodes);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = ResUtils.GetResourceListTreeNode(nodes);

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var options = new ResourceOptions {
				ResourceType = ResourceType.Linked,
				Name = "filelinked",
				Attributes = ManifestResourceAttributes.Public,
				File = new FileDefUser("somefile", dnlib.DotNet.FileAttributes.ContainsNoMetaData, new byte[0]),
			};
			var data = new ResourceVM(options, module);
			var win = new ResourceDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var opts = data.CreateResourceOptions();
			var node = ResourceFactory.Create(module, new LinkedResource(opts.Name, opts.File, opts.Attributes));
			UndoCommandManager.Instance.Add(new CreateFileLinkedResourceCommand(rsrcListNode, node));
			MainWindow.Instance.JumpToReference(node);
		}

		CreateFileLinkedResourceCommand(ResourceListTreeNode rsrcListNode, ResourceTreeNode resTreeNode)
			: base(rsrcListNode, resTreeNode) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class ResourceSettingsCommand : IUndoCommand {
		const string CMD_NAME = "Edit Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 80)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ResourceSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ResourceSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 90)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ResourceSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ResourceSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_CODE_ASMED_SETTINGS, Order = 90)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return ResourceSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				ResourceSettingsCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is ResourceTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcNode = (ResourceTreeNode)nodes[0];

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new ResourceVM(new ResourceOptions(rsrcNode.Resource), module);
			var win = new ResourceDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new ResourceSettingsCommand(rsrcNode, data.CreateResourceOptions()));
		}

		readonly ResourceTreeNode rsrcNode;
		readonly ResourceOptions newOptions;
		readonly ResourceOptions origOptions;
		readonly ILSpyTreeNode origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;

		ResourceSettingsCommand(ResourceTreeNode rsrcNode, ResourceOptions options) {
			this.rsrcNode = rsrcNode;
			this.newOptions = options;
			this.origOptions = new ResourceOptions(rsrcNode.Resource);

			this.origParentNode = (ILSpyTreeNode)rsrcNode.Parent;
			this.origParentChildIndex = this.origParentNode.Children.IndexOf(rsrcNode);
			Debug.Assert(this.origParentChildIndex >= 0);
			if (this.origParentChildIndex < 0)
				throw new InvalidOperationException();

			this.nameChanged = origOptions.Name != newOptions.Name;
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.Children.Count && origParentNode.Children[origParentChildIndex] == rsrcNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(rsrcNode.Resource);

				origParentNode.AddToChildren(rsrcNode);
			}
			else
				newOptions.CopyTo(rsrcNode.Resource);
			rsrcNode.RaiseUIPropsChanged();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = origParentNode.Children.Remove(rsrcNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(rsrcNode.Resource);
				origParentNode.Children.Insert(origParentChildIndex, rsrcNode);
			}
			else
				origOptions.CopyTo(rsrcNode.Resource);
			rsrcNode.RaiseUIPropsChanged();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return rsrcNode; }
		}

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	abstract class CreateResourceElementCommandBase : IUndoCommand {
		readonly ModuleDef module;
		readonly ResourceElementSetTreeNode rsrcSetNode;
		readonly ResourceElementTreeNode[] nodes;
		readonly Resource resource;
		readonly int resourceIndex;
		Resource newResource;

		protected CreateResourceElementCommandBase(ResourceElementSetTreeNode rsrcSetNode, ResourceElementTreeNode[] nodes) {
			this.module = ILSpyTreeNode.GetModule(rsrcSetNode);
			Debug.Assert(this.module != null);
			this.rsrcSetNode = rsrcSetNode;
			this.nodes = nodes;
			this.resource = rsrcSetNode.Resource;
			this.resourceIndex = module.Resources.IndexOf(this.resource);
			Debug.Assert(this.resourceIndex >= 0);
			if (this.resourceIndex < 0)
				throw new InvalidOperationException();
		}

		public abstract string Description { get; }

		public void Execute() {
			Debug.Assert(resource == rsrcSetNode.Resource);
			Debug.Assert(module.Resources[resourceIndex] == resource);
			foreach (var node in nodes)
				rsrcSetNode.AddToChildren(node);
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
				bool b = rsrcSetNode.Children.Remove(node);
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

		public void Dispose() {
		}
	}

	sealed class CreateImageResourceElementCommand : CreateResourceElementCommandBase {
		const string CMD_NAME = "Create System.Data.Bitmap/Icon Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewImage", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 140)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateImageResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateImageResourceElementCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewImage", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 140)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateImageResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateImageResourceElementCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewImage", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 140)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					CreateImageResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				CreateImageResourceElementCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is ResourceElementSetTreeNode || nodes[0].Parent is ResourceElementSetTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcSetNode = nodes[0] as ResourceElementSetTreeNode;
			if (rsrcSetNode == null)
				rsrcSetNode = nodes[0].Parent as ResourceElementSetTreeNode;
			Debug.Assert(rsrcSetNode != null);

			var module = ILSpyTreeNode.GetModule(nodes[0]);
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

			var newNodes = new List<ResourceElementTreeNode>(fnames.Length);
			string error = null;
			for (int i = 0; i < fnames.Length; i++) {
				var fn = fnames[i];
				try {
					newNodes.Add(ResourceFactory.Create(module, SerializationUtils.CreateSerializedImage(fn)));
				}
				catch (Exception ex) {
					if (error == null)
						error = string.Format("Error reading files: {0}", ex.Message);
				}
			}
			if (error != null)
				MainWindow.Instance.ShowMessageBox(error);
			if (newNodes.Count == 0)
				return;

			UndoCommandManager.Instance.Add(new CreateImageResourceElementCommand(rsrcSetNode, newNodes.ToArray()));
			MainWindow.Instance.JumpToReference(newNodes[0]);
		}

		CreateImageResourceElementCommand(ResourceElementSetTreeNode rsrcSetNode, ResourceElementTreeNode[] nodes)
			: base(rsrcSetNode, nodes) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class CreateImageListResourceElementCommand : CreateResourceElementCommandBase {
		const string CMD_NAME = "Create System.Windows.Forms.ImageListStreamer Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewImage", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 150)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateImageListResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateImageListResourceElementCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewImage", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 150)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateImageListResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateImageListResourceElementCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewImage", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 150)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					CreateImageListResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				CreateImageListResourceElementCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is ResourceElementSetTreeNode || nodes[0].Parent is ResourceElementSetTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcSetNode = nodes[0] as ResourceElementSetTreeNode;
			if (rsrcSetNode == null)
				rsrcSetNode = nodes[0].Parent as ResourceElementSetTreeNode;
			Debug.Assert(rsrcSetNode != null);

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new ImageListVM(new ImageListOptions() { Name = "my.ImageStream" });
			var win = new ImageListDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			if (data.ImageListStreamerVM.Collection.Count == 0) {
				MainWindow.Instance.ShowMessageBox("It's not possible to create an empty image list");
				return;
			}

			var listOpts = data.CreateImageListOptions();
			ResourceElementOptions opts = null;
			string error;
			try {
				opts = new ResourceElementOptions(SerializedImageListStreamerResourceElementTreeNode.Serialize(listOpts));
				error = SerializedImageListStreamerResourceElementTreeNode.CheckCanUpdateData(module, opts.Create());
			}
			catch (Exception ex) {
				error = string.Format("Couldn't serialize the images. Error: {0}", ex.Message);
			}
			if (!string.IsNullOrEmpty(error)) {
				MainWindow.Instance.ShowMessageBox(error);
				return;
			}

			var newNode = (SerializedImageListStreamerResourceElementTreeNode)ResourceFactory.Create(module, opts.Create());
			UndoCommandManager.Instance.Add(new CreateImageListResourceElementCommand(rsrcSetNode, newNode));
			MainWindow.Instance.JumpToReference(newNode);
		}

		CreateImageListResourceElementCommand(ResourceElementSetTreeNode rsrcSetNode, SerializedImageListStreamerResourceElementTreeNode node)
			: base(rsrcSetNode, new ResourceElementTreeNode[] { node }) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class CreateByteArrayResourceElementCommand : CreateResourceElementCommandBase {
		const string CMD_NAME = "Create Byte Array Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewBinary", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 160)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateByteArrayResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateByteArrayResourceElementCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewBinary", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 170)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateByteArrayResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateByteArrayResourceElementCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewBinary", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 170)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					CreateByteArrayResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				CreateByteArrayResourceElementCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is ResourceElementSetTreeNode || nodes[0].Parent is ResourceElementSetTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;
			Execute(nodes, ResourceTypeCode.ByteArray, (a, b) => new CreateByteArrayResourceElementCommand(a, b));
		}

		internal static void Execute(ILSpyTreeNode[] nodes, ResourceTypeCode typeCode, Func<ResourceElementSetTreeNode, ResourceElementTreeNode[], IUndoCommand> createCommand) {
			var rsrcSetNode = nodes[0] as ResourceElementSetTreeNode;
			if (rsrcSetNode == null)
				rsrcSetNode = nodes[0].Parent as ResourceElementSetTreeNode;
			Debug.Assert(rsrcSetNode != null);

			var module = ILSpyTreeNode.GetModule(nodes[0]);
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

			var newNodes = new ResourceElementTreeNode[fnames.Length];
			for (int i = 0; i < fnames.Length; i++) {
				var fn = fnames[i];
				try {
					var rsrcElem = new ResourceElement {
						Name = Path.GetFileName(fn),
						ResourceData = new BuiltInResourceData(typeCode, File.ReadAllBytes(fn)),
					};
					newNodes[i] = ResourceFactory.Create(module, rsrcElem);
				}
				catch (Exception ex) {
					MainWindow.Instance.ShowMessageBox(string.Format("Error reading files: {0}", ex.Message));
					return;
				}
			}

			UndoCommandManager.Instance.Add(createCommand(rsrcSetNode, newNodes.ToArray()));
			MainWindow.Instance.JumpToReference(newNodes[0]);
		}

		CreateByteArrayResourceElementCommand(ResourceElementSetTreeNode rsrcSetNode, ResourceElementTreeNode[] nodes)
			: base(rsrcSetNode, nodes) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class CreateStreamResourceElementCommand : CreateResourceElementCommandBase {
		const string CMD_NAME = "Create System.IO.Stream Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewBinary", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 170)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateStreamResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateStreamResourceElementCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewBinary", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 180)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateStreamResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateStreamResourceElementCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewBinary", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 180)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					CreateStreamResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				CreateStreamResourceElementCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is ResourceElementSetTreeNode || nodes[0].Parent is ResourceElementSetTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;
			CreateByteArrayResourceElementCommand.Execute(nodes, ResourceTypeCode.Stream, (a, b) => new CreateStreamResourceElementCommand(a, b));
		}

		CreateStreamResourceElementCommand(ResourceElementSetTreeNode rsrcSetNode, ResourceElementTreeNode[] nodes)
			: base(rsrcSetNode, nodes) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class CreateResourceElementCommand : CreateResourceElementCommandBase {
		const string CMD_NAME = "Create Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewResource", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 190)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateResourceElementCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewResource", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 190)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateResourceElementCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewResource", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 190)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					CreateResourceElementCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				CreateResourceElementCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is ResourceElementSetTreeNode || nodes[0].Parent is ResourceElementSetTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcSetNode = nodes[0] as ResourceElementSetTreeNode;
			if (rsrcSetNode == null)
				rsrcSetNode = nodes[0].Parent as ResourceElementSetTreeNode;
			Debug.Assert(rsrcSetNode != null);

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var options = new ResourceElementOptions(new ResourceElement {
				Name = string.Empty,
				ResourceData = new BuiltInResourceData(ResourceTypeCode.String, string.Empty),
			});
			var data = new ResourceElementVM(options, module, OtherSettings.Instance.DeserializeResources);
			var win = new ResourceElementDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var opts = data.CreateResourceElementOptions();
			var node = ResourceFactory.Create(module, opts.Create());
			UndoCommandManager.Instance.Add(new CreateResourceElementCommand(rsrcSetNode, node));
			MainWindow.Instance.JumpToReference(node);
		}

		CreateResourceElementCommand(ResourceElementSetTreeNode rsrcSetNode, ResourceElementTreeNode node)
			: base(rsrcSetNode, new[] { node }) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	[DebuggerDisplay("{Description}")]
	abstract class ResourceElementSettingsBaseCommand : IUndoCommand {
		readonly ModuleDef module;
		readonly Resource resource;
		readonly int resourceIndex;
		Resource newResource;
		readonly ResourceElementSetTreeNode rsrcSetNode;
		readonly ResourceElementTreeNode rsrcElNode;
		readonly ResourceElement newOptions;
		readonly ResourceElement origOptions;
		readonly int origParentChildIndex;
		readonly bool nameChanged;

		protected ResourceElementSettingsBaseCommand(ResourceElementTreeNode rsrcElNode, ResourceElementOptions options) {
			this.rsrcSetNode = (ResourceElementSetTreeNode)rsrcElNode.Parent;
			this.rsrcElNode = rsrcElNode;
			this.newOptions = options.Create();
			this.origOptions = rsrcElNode.ResourceElement;

			this.module = ILSpyTreeNode.GetModule(rsrcSetNode);
			Debug.Assert(this.module != null);
			this.resource = rsrcSetNode.Resource;
			this.resourceIndex = module.Resources.IndexOf(this.resource);
			Debug.Assert(this.resourceIndex >= 0);
			if (this.resourceIndex < 0)
				throw new InvalidOperationException();

			this.origParentChildIndex = this.rsrcSetNode.Children.IndexOf(rsrcElNode);
			Debug.Assert(this.origParentChildIndex >= 0);
			if (this.origParentChildIndex < 0)
				throw new InvalidOperationException();

			this.nameChanged = origOptions.Name != newOptions.Name;
		}

		public abstract string Description { get; }

		public void Execute() {
			Debug.Assert(resource == rsrcSetNode.Resource);
			Debug.Assert(module.Resources[resourceIndex] == resource);
			if (nameChanged) {
				bool b = origParentChildIndex < rsrcSetNode.Children.Count && rsrcSetNode.Children[origParentChildIndex] == rsrcElNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				rsrcSetNode.Children.RemoveAt(origParentChildIndex);
				rsrcElNode.UpdateData(newOptions);

				rsrcSetNode.AddToChildren(rsrcElNode);
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
			rsrcElNode.RaiseUIPropsChanged();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = rsrcSetNode.Children.Remove(rsrcElNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				rsrcElNode.UpdateData(origOptions);
				rsrcSetNode.Children.Insert(origParentChildIndex, rsrcElNode);
			}
			else
				rsrcElNode.UpdateData(origOptions);

			rsrcSetNode.Resource = resource;
			module.Resources[resourceIndex] = resource;
			rsrcElNode.RaiseUIPropsChanged();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return rsrcElNode; }
		}

		public void Dispose() {
		}
	}

	sealed class ResourceElementSettingsCommand : ResourceElementSettingsBaseCommand {
		const string CMD_NAME = "Edit Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 90)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 100)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_CODE_ASMED_SETTINGS, Order = 100)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return ResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				ResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is BuiltInResourceElementTreeNode ||
				nodes[0] is UnknownSerializedResourceElementTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcElNode = (ResourceElementTreeNode)nodes[0];
			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var options = new ResourceElementOptions(rsrcElNode.ResourceElement);
			var data = new ResourceElementVM(options, module, OtherSettings.Instance.DeserializeResources);
			data.CanChangeType = false;
			var win = new ResourceElementDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var opts = data.CreateResourceElementOptions();
			string error;
			try {
				error = rsrcElNode.CheckCanUpdateData(opts.Create());
			}
			catch (Exception ex) {
				error = string.Format("Can't use this data: {0}", ex.Message);
			}
			if (!string.IsNullOrEmpty(error)) {
				MainWindow.Instance.ShowMessageBox(error);
				return;
			}

			UndoCommandManager.Instance.Add(new ResourceElementSettingsCommand(rsrcElNode, opts));
		}

		ResourceElementSettingsCommand(ResourceElementTreeNode rsrcElNode, ResourceElementOptions options)
			: base(rsrcElNode, options) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class ImageResourceElementSettingsCommand : ResourceElementSettingsBaseCommand {
		const string CMD_NAME = "Edit Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 100)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ImageResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 110)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return ImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				ImageResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_CODE_ASMED_SETTINGS, Order = 110)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return ImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				ImageResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is ImageResourceElementTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var imgRsrcElNode = (ImageResourceElementTreeNode)nodes[0];

			var options = new ResourceElementOptions(imgRsrcElNode.ResourceElement);
			var data = new ImageResourceElementVM(options);
			var win = new ImageResourceElementDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var opts = data.CreateResourceElementOptions();
			string error;
			try {
				error = imgRsrcElNode.CheckCanUpdateData(opts.Create());
			}
			catch (Exception ex) {
				error = string.Format("New data must be an image. Error: {0}", ex.Message);
			}
			if (!string.IsNullOrEmpty(error)) {
				MainWindow.Instance.ShowMessageBox(error);
				return;
			}

			UndoCommandManager.Instance.Add(new ImageResourceElementSettingsCommand(imgRsrcElNode, opts));
		}

		ImageResourceElementSettingsCommand(ResourceElementTreeNode rsrcElNode, ResourceElementOptions options)
			: base(rsrcElNode, options) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class SerializedImageResourceElementSettingsCommand : ResourceElementSettingsBaseCommand {
		const string CMD_NAME = "Edit Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 110)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return SerializedImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				SerializedImageResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 120)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return SerializedImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				SerializedImageResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_CODE_ASMED_SETTINGS, Order = 120)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return SerializedImageResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				SerializedImageResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is SerializedImageResourceElementTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var imgRsrcElNode = (SerializedImageResourceElementTreeNode)nodes[0];
			var options = new ResourceElementOptions(imgRsrcElNode.GetAsRawImage());
			var data = new ImageResourceElementVM(options);
			var win = new ImageResourceElementDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var opts = data.CreateResourceElementOptions();
			string error;
			try {
				opts = new ResourceElementOptions(imgRsrcElNode.Serialize(opts.Create()));
				error = imgRsrcElNode.CheckCanUpdateData(opts.Create());
			}
			catch (Exception ex) {
				error = string.Format("New data must be an image. Error: {0}", ex.Message);
			}
			if (!string.IsNullOrEmpty(error)) {
				MainWindow.Instance.ShowMessageBox(error);
				return;
			}

			UndoCommandManager.Instance.Add(new SerializedImageResourceElementSettingsCommand(imgRsrcElNode, opts));
		}

		SerializedImageResourceElementSettingsCommand(ResourceElementTreeNode rsrcElNode, ResourceElementOptions options)
			: base(rsrcElNode, options) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}

	sealed class SerializedImageListStreamerResourceElementSettingsCommand : ResourceElementSettingsBaseCommand {
		const string CMD_NAME = "Edit Resource";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 120)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return SerializedImageListStreamerResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				SerializedImageListStreamerResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 130)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return SerializedImageListStreamerResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				SerializedImageListStreamerResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_CODE_ASMED_SETTINGS, Order = 130)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return SerializedImageListStreamerResourceElementSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				SerializedImageListStreamerResourceElementSettingsCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is SerializedImageListStreamerResourceElementTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var imgNode = (SerializedImageListStreamerResourceElementTreeNode)nodes[0];
			var options = new ImageListOptions(imgNode.ImageListOptions);
			var data = new ImageListVM(options);
			var win = new ImageListDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var listOpts = data.CreateImageListOptions();

			if (listOpts.ImageSources.Count == 0) {
				MainWindow.Instance.ShowMessageBox("It's not possible to create an empty image list");
				return;
			}

			ResourceElementOptions opts = null;
			string error;
			try {
				opts = new ResourceElementOptions(SerializedImageListStreamerResourceElementTreeNode.Serialize(listOpts));
				error = imgNode.CheckCanUpdateData(opts.Create());
			}
			catch (Exception ex) {
				error = string.Format("Couldn't serialize the images. Error: {0}", ex.Message);
			}
			if (!string.IsNullOrEmpty(error)) {
				MainWindow.Instance.ShowMessageBox(error);
				return;
			}

			UndoCommandManager.Instance.Add(new SerializedImageListStreamerResourceElementSettingsCommand(imgNode, opts));
		}

		SerializedImageListStreamerResourceElementSettingsCommand(ResourceElementTreeNode rsrcElNode, ResourceElementOptions options)
			: base(rsrcElNode, options) {
		}

		public override string Description {
			get { return CMD_NAME; }
		}
	}
}
