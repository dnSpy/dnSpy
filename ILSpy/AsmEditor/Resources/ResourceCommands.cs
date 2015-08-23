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
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Options;
using dnSpy.TreeNodes;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;
using WF = System.Windows.Forms;

namespace dnSpy.AsmEditor.Resources {
	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		public void OnLoaded() {
			MainWindow.Instance.treeView.CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, DeleteResourceExecuted, DeleteResourceCanExecute));
			MainWindow.Instance.treeView.CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, DeleteResourceElementExecuted, DeleteResourceElementCanExecute));
			MainWindow.Instance.CodeBindings.Add(EditingCommands.Delete, new TextEditorCommandProxy(new DeleteResourceCommand.TheTextEditorCommand()), ModifierKeys.None, Key.Delete);
			MainWindow.Instance.CodeBindings.Add(EditingCommands.Delete, new TextEditorCommandProxy(new DeleteResourceElementCommand.TheTextEditorCommand()), ModifierKeys.None, Key.Delete);
		}

		void DeleteResourceCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = DeleteResourceCommand.CanExecute(MainWindow.Instance.SelectedNodes);
		}

		void DeleteResourceExecuted(object sender, ExecutedRoutedEventArgs e) {
			DeleteResourceCommand.Execute(MainWindow.Instance.SelectedNodes);
		}

		void DeleteResourceElementCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = DeleteResourceElementCommand.CanExecute(MainWindow.Instance.SelectedNodes);
		}

		void DeleteResourceElementExecuted(object sender, ExecutedRoutedEventArgs e) {
			DeleteResourceElementCommand.Execute(MainWindow.Instance.SelectedNodes);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteResourceCommand : IUndoCommand {
		const string CMD_NAME = "Delete Resource";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 380)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							Menu = "_Edit",
							MenuIcon = "Delete",
							MenuInputGestureText = "Del",
							MenuCategory = "AsmEd",
							MenuOrder = 2180)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return DeleteResourceCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				DeleteResourceCommand.Execute(nodes);
			}

			protected override void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
				DeleteResourceCommand.Initialize(nodes, menuItem);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 380)]
		internal sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					DeleteResourceCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				DeleteResourceCommand.Execute(ctx.Nodes);
			}

			protected override void Initialize(Context ctx, MenuItem menuItem) {
				DeleteResourceCommand.Initialize(ctx.Nodes, menuItem);
			}
		}

		static void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
			if (nodes.Length == 1)
				menuItem.Header = string.Format("Delete {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			else
				menuItem.Header = string.Format("Delete {0} resources", nodes.Length);
		}

		internal static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is ResourceTreeNode);
		}

		internal static void Execute(ILSpyTreeNode[] nodes) {
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
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 390)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							Menu = "_Edit",
							MenuIcon = "Delete",
							MenuInputGestureText = "Del",
							MenuCategory = "AsmEd",
							MenuOrder = 2190)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return DeleteResourceElementCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				DeleteResourceElementCommand.Execute(nodes);
			}

			protected override void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
				DeleteResourceElementCommand.Initialize(nodes, menuItem);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 390)]
		internal sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					DeleteResourceElementCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				DeleteResourceElementCommand.Execute(ctx.Nodes);
			}

			protected override void Initialize(Context ctx, MenuItem menuItem) {
				DeleteResourceElementCommand.Initialize(ctx.Nodes, menuItem);
			}
		}

		static void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
			if (nodes.Length == 1)
				menuItem.Header = string.Format("Delete {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			else
				menuItem.Header = string.Format("Delete {0} resources", nodes.Length);
		}

		internal static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is ResourceElementTreeNode);
		}

		internal static void Execute(ILSpyTreeNode[] nodes) {
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

	abstract class SaveResourcesContextMenuEntryBase : IContextMenuEntry2 {
		readonly bool useSubDirs;
		readonly ResourceDataType resourceDataType;

		protected SaveResourcesContextMenuEntryBase(bool useSubDirs, ResourceDataType resourceDataType) {
			this.useSubDirs = useSubDirs;
			this.resourceDataType = resourceDataType;
		}

		public virtual bool IsVisible(ContextMenuEntryContext context) {
			return GetResourceNodes(context) != null;
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			return true;
		}

		public void Execute(ContextMenuEntryContext context) {
			SaveResources.Save(GetResourceNodes(context), useSubDirs, resourceDataType);
		}

		public abstract void Initialize(ContextMenuEntryContext context, MenuItem menuItem);

		protected IResourceNode[] GetResourceNodes(ContextMenuEntryContext context) {
			IResourceNode[] nodes;
			if (context.SelectedTreeNodes != null) {
				var inputNodes = context.SelectedTreeNodes;
				if (context.SelectedTreeNodes.Length == 1 && context.SelectedTreeNodes[0] is ResourceListTreeNode) {
					var rlist = (ResourceListTreeNode)context.SelectedTreeNodes[0];
					rlist.EnsureChildrenFiltered();
					inputNodes = rlist.Children.Cast<ILSpyTreeNode>().ToArray();
				}
				nodes = inputNodes.Where(a => a is IResourceNode).Cast<IResourceNode>().ToArray();
			}
			else if (context.Reference != null && context.Reference.Reference is IResourceNode)
				nodes = new[] { (IResourceNode)context.Reference.Reference };
			else
				return null;

			nodes = nodes.Where(a => a.GetResourceData(resourceDataType).Any()).ToArray();
			return nodes.Length == 0 ? null : nodes;
		}

		protected ResourceData[] GetResourceData(IResourceNode[] nodes) {
			return SaveResources.GetResourceData(nodes, resourceDataType);
		}
	}

	[ExportContextMenuEntryAttribute(Order = 300, Category = "AsmEd")]
	sealed class SaveResourcesContextMenuEntry : SaveResourcesContextMenuEntryBase {
		public SaveResourcesContextMenuEntry()
			: base(false, ResourceDataType.Deserialized) {
		}

		public override void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			var infos = GetResourceData(GetResourceNodes(context));
			if (infos.Length == 1)
				menuItem.Header = string.Format("Save {0}", UIUtils.EscapeMenuItemHeader(infos[0].Name));
			else
				menuItem.Header = string.Format("Save {0} resources", infos.Length);
		}
	}

	[ExportContextMenuEntryAttribute(Order = 310, Category = "AsmEd")]
	sealed class SaveWithPathResourcesContextMenuEntry : SaveResourcesContextMenuEntryBase {
		public SaveWithPathResourcesContextMenuEntry()
			: base(true, ResourceDataType.Deserialized) {
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			var nodes = GetResourceNodes(context);
			if (nodes == null)
				return false;
			var infos = GetResourceData(nodes);
			return infos.Length > 1 &&
				infos.Any(a => a.Name.Contains('/') || a.Name.Contains('\\'));
		}

		public override void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			var infos = GetResourceData(GetResourceNodes(context));
			menuItem.Header = string.Format("Save {0} resources, use sub dirs", infos.Length);
		}
	}

	[ExportContextMenuEntryAttribute(Order = 320, Category = "AsmEd")]
	sealed class SaveRawResourcesContextMenuEntry : SaveResourcesContextMenuEntryBase {
		public SaveRawResourcesContextMenuEntry()
			: base(false, ResourceDataType.Serialized) {
		}

		public override void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			var infos = GetResourceData(GetResourceNodes(context));
			if (infos.Length == 1)
				menuItem.Header = string.Format("Raw Save {0}", UIUtils.EscapeMenuItemHeader(infos[0].Name));
			else
				menuItem.Header = string.Format("Raw Save {0} resources", infos.Length);
		}
	}

	[ExportContextMenuEntryAttribute(Order = 330, Category = "AsmEd")]
	sealed class SaveRawWithPathResourcesContextMenuEntry : SaveResourcesContextMenuEntryBase {
		public SaveRawWithPathResourcesContextMenuEntry()
			: base(true, ResourceDataType.Serialized) {
		}

		public override bool IsVisible(ContextMenuEntryContext context) {
			var nodes = GetResourceNodes(context);
			if (nodes == null)
				return false;
			var infos = GetResourceData(nodes);
			return infos.Length > 1 &&
				infos.Any(a => a.Name.Contains('/') || a.Name.Contains('\\'));
		}

		public override void Initialize(ContextMenuEntryContext context, MenuItem menuItem) {
			var infos = GetResourceData(GetResourceNodes(context));
			menuItem.Header = string.Format("Raw Save {0} resources, use sub dirs", infos.Length);
		}
	}

	static class Utils {
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewResource",
								Category = "AsmEd",
								Order = 600)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewResource",
							MenuCategory = "AsmEd",
							MenuOrder = 2400)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateFileResourceCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateFileResourceCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewResource",
								Category = "AsmEd",
								Order = 600)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					CreateFileResourceCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				CreateFileResourceCommand.Execute(ctx.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return Utils.CanExecuteResourceListCommand(nodes);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = Utils.GetResourceListTreeNode(nodes);

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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewResourcesFile",
								Category = "AsmEd",
								Order = 610)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewResourcesFile",
							MenuCategory = "AsmEd",
							MenuOrder = 2410)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateMultiFileResourceCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateMultiFileResourceCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewResourcesFile",
								Category = "AsmEd",
								Order = 610)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					CreateMultiFileResourceCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				CreateMultiFileResourceCommand.Execute(ctx.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return Utils.CanExecuteResourceListCommand(nodes);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = Utils.GetResourceListTreeNode(nodes);

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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewAssembly",
								Category = "AsmEd",
								Order = 620)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewAssembly",
							MenuCategory = "AsmEd",
							MenuOrder = 2420)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateAssemblyLinkedResourceCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateAssemblyLinkedResourceCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewAssembly",
								Category = "AsmEd",
								Order = 620)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					CreateAssemblyLinkedResourceCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				CreateAssemblyLinkedResourceCommand.Execute(ctx.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return Utils.CanExecuteResourceListCommand(nodes);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = Utils.GetResourceListTreeNode(nodes);

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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewAssemblyModule",
								Category = "AsmEd",
								Order = 630)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewAssemblyModule",
							MenuCategory = "AsmEd",
							MenuOrder = 2430)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateFileLinkedResourceCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateFileLinkedResourceCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewAssemblyModule",
								Category = "AsmEd",
								Order = 630)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					CreateFileLinkedResourceCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				CreateFileLinkedResourceCommand.Execute(ctx.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return Utils.CanExecuteResourceListCommand(nodes);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var rsrcListNode = Utils.GetResourceListTreeNode(nodes);

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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 680)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "Settings",
							MenuCategory = "AsmEd",
							MenuOrder = 2490)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return ResourceSettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				ResourceSettingsCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 690)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ResourceSettingsCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				ResourceSettingsCommand.Execute(ctx.Nodes);
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewImage",
								Category = "AsmEd",
								Order = 640)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewImage",
							MenuCategory = "AsmEd",
							MenuOrder = 2440)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateImageResourceElementCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateImageResourceElementCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewImage",
								Category = "AsmEd",
								Order = 640)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					CreateImageResourceElementCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				CreateImageResourceElementCommand.Execute(ctx.Nodes);
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
				Filter = "Images|*.png;*.gif;*.bmp;*.dib;*.jpg;*.jpeg;*.jpe;*.jif;*.jfif;*.jfi;*.ico;*.cur|All files (*.*)|*.*",
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewImage",
								Category = "AsmEd",
								Order = 650)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewImage",
							MenuCategory = "AsmEd",
							MenuOrder = 2450)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateImageListResourceElementCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateImageListResourceElementCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewImage",
								Category = "AsmEd",
								Order = 650)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					CreateImageListResourceElementCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				CreateImageListResourceElementCommand.Execute(ctx.Nodes);
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewBinary",
								Category = "AsmEd",
								Order = 660)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewBinary",
							MenuCategory = "AsmEd",
							MenuOrder = 2470)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateByteArrayResourceElementCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateByteArrayResourceElementCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewBinary",
								Category = "AsmEd",
								Order = 670)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					CreateByteArrayResourceElementCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				CreateByteArrayResourceElementCommand.Execute(ctx.Nodes);
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
				Filter = "All files (*.*)|*.*",
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewBinary",
								Category = "AsmEd",
								Order = 670)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewBinary",
							MenuCategory = "AsmEd",
							MenuOrder = 2480)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateStreamResourceElementCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateStreamResourceElementCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewBinary",
								Category = "AsmEd",
								Order = 680)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					CreateStreamResourceElementCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				CreateStreamResourceElementCommand.Execute(ctx.Nodes);
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewResource",
								Category = "AsmEd",
								Order = 650)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewResource",
							MenuCategory = "AsmEd",
							MenuOrder = 2460)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateResourceElementCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateResourceElementCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewResource",
								Category = "AsmEd",
								Order = 660)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					CreateResourceElementCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				CreateResourceElementCommand.Execute(ctx.Nodes);
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 690)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "Settings",
							MenuCategory = "AsmEd",
							MenuOrder = 2500)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return ResourceElementSettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				ResourceElementSettingsCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 700)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ResourceElementSettingsCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				ResourceElementSettingsCommand.Execute(ctx.Nodes);
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 700)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "Settings",
							MenuCategory = "AsmEd",
							MenuOrder = 2510)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return ImageResourceElementSettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				ImageResourceElementSettingsCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 710)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ImageResourceElementSettingsCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				ImageResourceElementSettingsCommand.Execute(ctx.Nodes);
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 710)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "Settings",
							MenuCategory = "AsmEd",
							MenuOrder = 2520)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return SerializedImageResourceElementSettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				SerializedImageResourceElementSettingsCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 720)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return SerializedImageResourceElementSettingsCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				SerializedImageResourceElementSettingsCommand.Execute(ctx.Nodes);
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 720)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "Settings",
							MenuCategory = "AsmEd",
							MenuOrder = 2530)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return SerializedImageListStreamerResourceElementSettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				SerializedImageListStreamerResourceElementSettingsCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 730)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return SerializedImageListStreamerResourceElementSettingsCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				SerializedImageListStreamerResourceElementSettingsCommand.Execute(ctx.Nodes);
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
