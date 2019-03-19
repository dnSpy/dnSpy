/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;

namespace dnSpy.AsmEditor.Namespace {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, DeleteNamespaceCommand.EditMenuCommand removeCmd) => wpfCommandService.AddRemoveCommand(removeCmd);
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteNamespaceCommand : IUndoCommand {
		[ExportMenuItem(Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_DELETE, Order = 70)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeleteNamespaceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteNamespaceCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => GetCommandName(context.Nodes.Length);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 70)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => DeleteNamespaceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteNamespaceCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => GetCommandName(context.Nodes.Length);
		}

		static string GetCommandName(int count) =>
			count == 1 ?
			dnSpy_AsmEditor_Resources.DeleteNamespaceCommand :
			string.Format(dnSpy_AsmEditor_Resources.DeleteNamespacesCommand, count);

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes != null &&
			nodes.Length > 0 &&
			nodes.All(a => a is NamespaceNode);

		static void Execute(Lazy<IUndoCommandService> undoCommandService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var nsNodes = nodes.Cast<NamespaceNode>().ToArray();
			undoCommandService.Value.Add(new DeleteNamespaceCommand(nsNodes));
		}

		struct DeleteModelNodes {
			ModuleInfo[] infos;

			sealed class ModuleInfo {
				public readonly ModuleDef Module;
				public readonly TypeDef[] Types;
				public readonly int[] Indexes;

				public ModuleInfo(ModuleDef module, int count) {
					Module = module;
					Types = new TypeDef[count];
					Indexes = new int[count];
				}
			}

			public void Delete(NamespaceNode[] nodes, DocumentTreeNodeData[] parents) {
				Debug.Assert(parents != null && nodes.Length == parents.Length);
				Debug.Assert(infos == null);
				if (infos != null)
					throw new InvalidOperationException();

				infos = new ModuleInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];
					var module = parents[i].GetModule();
					Debug.Assert(module != null);
					if (module == null)
						throw new InvalidOperationException();

					var info = new ModuleInfo(module, node.TreeNode.Children.Count);
					infos[i] = info;

					for (int j = 0; j < node.TreeNode.Children.Count; j++) {
						var typeNode = (TypeNode)node.TreeNode.Children[j].Data;
						int index = module.Types.IndexOf(typeNode.TypeDef);
						Debug.Assert(index >= 0);
						if (index < 0)
							throw new InvalidOperationException();
						module.Types.RemoveAt(index);
						info.Types[j] = typeNode.TypeDef;
						info.Indexes[j] = index;
					}
				}
			}

			public void Restore(NamespaceNode[] nodes, DocumentTreeNodeData[] parents) {
				Debug.Assert(infos != null);
				if (infos == null)
					throw new InvalidOperationException();
				Debug.Assert(infos.Length == nodes.Length);
				if (infos.Length != nodes.Length)
					throw new InvalidOperationException();

				for (int i = infos.Length - 1; i >= 0; i--) {
					var info = infos[i];

					for (int j = info.Types.Length - 1; j >= 0; j--)
						info.Module.Types.Insert(info.Indexes[j], info.Types[j]);
				}

				infos = null;
			}
		}

		DocumentTreeNodeData[] parents;
		DeletableNodes<NamespaceNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteNamespaceCommand(NamespaceNode[] nodes) {
			parents = nodes.Select(a => (DocumentTreeNodeData)a.TreeNode.Parent.Data).ToArray();
			this.nodes = new DeletableNodes<NamespaceNode>(nodes);
			modelNodes = new DeleteModelNodes();
		}

		public string Description => GetCommandName(nodes.Count);

		public void Execute() {
			nodes.Delete();
			modelNodes.Delete(nodes.Nodes, parents);
		}

		public void Undo() {
			modelNodes.Restore(nodes.Nodes, parents);
			nodes.Restore();
		}

		public IEnumerable<object> ModifiedObjects => nodes.Nodes;
	}

	readonly struct TypeRefInfo {
		public readonly TypeRef TypeRef;
		public readonly UTF8String OrigNamespace;

		public TypeRefInfo(TypeRef tr) {
			TypeRef = tr;
			OrigNamespace = tr.Namespace;
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class MoveNamespaceTypesToEmptypNamespaceCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:MoveTypesToEmptyNamespaceCommand", Icon = DsImagesAttribute.Namespace, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_MISC, Order = 0)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => MoveNamespaceTypesToEmptypNamespaceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => MoveNamespaceTypesToEmptypNamespaceCommand.Execute(undoCommandService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:MoveTypesToEmptyNamespaceCommand", Icon = DsImagesAttribute.Namespace, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_MISC, Order = 0)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) => this.undoCommandService = undoCommandService;

			public override bool IsVisible(AsmEditorContext context) => MoveNamespaceTypesToEmptypNamespaceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => MoveNamespaceTypesToEmptypNamespaceCommand.Execute(undoCommandService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes != null &&
			nodes.Length > 0 &&
			nodes.All(a => a is NamespaceNode) &&
			nodes.Any(a => ((NamespaceNode)a).Name != string.Empty) &&
			IsInSameModule(nodes) &&
			nodes[0].TreeNode.Parent != null &&
			nodes[0].TreeNode.Parent.DataChildren.Any(a => a is NamespaceNode && ((NamespaceNode)a).Name == string.Empty);

		static bool IsInSameModule(DocumentTreeNodeData[] nodes) {
			if (nodes == null || nodes.Length == 0)
				return false;
			var module = nodes[0].GetModule();
			if (module == null)
				return false;
			for (int i = 0; i < nodes.Length; i++) {
				if (module != nodes[i].GetModule())
					return false;
			}
			return true;
		}

		static void Execute(Lazy<IUndoCommandService> undoCommandService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			undoCommandService.Value.Add(new MoveNamespaceTypesToEmptypNamespaceCommand(nodes));
		}

		MoveNamespaceTypesToEmptypNamespaceCommand(DocumentTreeNodeData[] nodes) {
			var nsNodes = nodes.Cast<NamespaceNode>().Where(a => a.Name != string.Empty).ToArray();
			Debug.Assert(nsNodes.Length > 0);
			this.nodes = new DeletableNodes<NamespaceNode>(nsNodes);
			nsTarget = GetTarget();
			typeRefInfos = RenameNamespaceCommand.GetTypeRefInfos(nodes[0].GetModule(), nsNodes);
		}

		public string Description => dnSpy_AsmEditor_Resources.MoveTypesToEmptyNamespaceCommand;

		readonly NamespaceNode nsTarget;
		DeletableNodes<NamespaceNode> nodes;
		ModelInfo[] infos;
		readonly TypeRefInfo[] typeRefInfos;

		sealed class ModelInfo {
			public UTF8String[] Namespaces;
			public DeletableNodes<TypeNode> TypeNodes;
		}

		NamespaceNode GetTarget() => nodes.Nodes.Length == 0 ? null : (NamespaceNode)nodes.Nodes[0].TreeNode.Parent.DataChildren.First(a => a is NamespaceNode && ((NamespaceNode)a).Name == string.Empty);

		public void Execute() {
			Debug.Assert(infos == null);
			if (infos != null)
				throw new InvalidOperationException();

			nodes.Delete();

			infos = new ModelInfo[nodes.Count];
			for (int i = 0; i < infos.Length; i++) {
				var nsNode = nodes.Nodes[i];

				var info = new ModelInfo();
				infos[i] = info;
				info.Namespaces = new UTF8String[nsNode.TreeNode.Children.Count];
				info.TypeNodes = new DeletableNodes<TypeNode>(nsNode.TreeNode.DataChildren.Cast<TypeNode>());
				info.TypeNodes.Delete();

				for (int j = 0; j < info.Namespaces.Length; j++) {
					var typeNode = info.TypeNodes.Nodes[j];
					info.Namespaces[j] = typeNode.TypeDef.Namespace;
					typeNode.TypeDef.Namespace = UTF8String.Empty;
					nsTarget.TreeNode.Children.Add(typeNode.TreeNode);
				}
			}

			foreach (var info in typeRefInfos)
				info.TypeRef.Namespace = UTF8String.Empty;
		}

		public void Undo() {
			Debug.Assert(infos != null);
			if (infos == null)
				throw new InvalidOperationException();

			for (int i = infos.Length - 1; i >= 0; i--) {
				var info = infos[i];

				for (int j = info.Namespaces.Length - 1; j >= 0; j--) {
					var typeNode = (TypeNode)nsTarget.TreeNode.Children[nsTarget.TreeNode.Children.Count - 1].Data;
					nsTarget.TreeNode.Children.RemoveAt(nsTarget.TreeNode.Children.Count - 1);
					bool b = info.TypeNodes.Nodes[j] == typeNode;
					Debug.Assert(b);
					if (!b)
						throw new InvalidOperationException();
					typeNode.TypeDef.Namespace = info.Namespaces[j];
				}

				info.TypeNodes.Restore();
			}

			nodes.Restore();

			foreach (var info in typeRefInfos)
				info.TypeRef.Namespace = info.OrigNamespace;

			infos = null;
		}

		public IEnumerable<object> ModifiedObjects {
			get {
				yield return nsTarget;
				foreach (var n in nodes.Nodes)
					yield return n;
			}
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class RenameNamespaceCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:RenameNamespaceCommand", Icon = DsImagesAttribute.Namespace, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_MISC, Order = 10)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => RenameNamespaceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RenameNamespaceCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:RenameNamespaceCommand", Icon = DsImagesAttribute.Namespace, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_MISC, Order = 10)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => RenameNamespaceCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => RenameNamespaceCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes != null &&
			nodes.Length == 1 &&
			nodes[0] is NamespaceNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var nsNode = (NamespaceNode)nodes[0];

			var data = new NamespaceVM(nsNode.Name);
			var win = new NamespaceDlg();
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			if (nsNode.Name == data.Name)
				return;

			undoCommandService.Value.Add(new RenameNamespaceCommand(data.Name, nsNode));
		}

		readonly string newName;
		readonly string origName;
		readonly NamespaceNode nsNode;
		readonly NamespaceNode existingNsNode;
		readonly DocumentTreeNodeData origParentNode;
		readonly int origParentChildIndex;
		readonly UTF8String[] typeNamespaces;
		readonly TypeNode[] origChildren;
		readonly TypeRefInfo[] typeRefInfos;

		internal static TypeRefInfo[] GetTypeRefInfos(ModuleDef module, IEnumerable<NamespaceNode> nsNodes) {
			var types = new HashSet<ITypeDefOrRef>(RefFinder.TypeEqualityComparerInstance);
			foreach (var nsNode in nsNodes) {
				foreach (TypeNode typeNode in nsNode.TreeNode.DataChildren)
					types.Add(typeNode.TypeDef);
			}
			var typeRefs = RefFinder.FindTypeRefsToThisModule(module);
			return typeRefs.Where(a => types.Contains(a)).Select(a => new TypeRefInfo(a)).ToArray();
		}

		RenameNamespaceCommand(string newName, NamespaceNode nsNode) {
			this.newName = newName;
			origName = nsNode.Name;
			this.nsNode = nsNode;
			existingNsNode = (NamespaceNode)nsNode.TreeNode.Parent.DataChildren.FirstOrDefault(a => a is NamespaceNode && newName == ((NamespaceNode)a).Name);

			var module = nsNode.GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			origParentNode = (DocumentTreeNodeData)nsNode.TreeNode.Parent.Data;
			origParentChildIndex = origParentNode.TreeNode.Children.IndexOf(nsNode.TreeNode);
			Debug.Assert(origParentChildIndex >= 0);
			if (origParentChildIndex < 0)
				throw new InvalidOperationException();

			// Make sure the exact same namespace names are restored if we undo. The names are UTF8
			// strings, but not necessarily canonicalized if it's an obfuscated assembly.
			nsNode.TreeNode.EnsureChildrenLoaded();
			origChildren = nsNode.TreeNode.DataChildren.Cast<TypeNode>().ToArray();
			typeNamespaces = new UTF8String[nsNode.TreeNode.Children.Count];
			for (int i = 0; i < typeNamespaces.Length; i++)
				typeNamespaces[i] = origChildren[i].TypeDef.Namespace;

			typeRefInfos = GetTypeRefInfos(module, new[] { nsNode });
		}

		public string Description => dnSpy_AsmEditor_Resources.RenameNamespaceCommand;

		public void Execute() {
			UTF8String newNamespace = newName;
			if (existingNsNode != null) {
				Debug.Assert(origChildren.Length == nsNode.TreeNode.Children.Count);
				nsNode.TreeNode.Children.Clear();
				foreach (var typeNode in origChildren) {
					typeNode.TypeDef.Namespace = newNamespace;
					existingNsNode.TreeNode.AddChild(typeNode.TreeNode);
				}
			}
			else {
				bool b = origParentChildIndex < origParentNode.TreeNode.Children.Count && origParentNode.TreeNode.Children[origParentChildIndex] == nsNode.TreeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.TreeNode.Children.RemoveAt(origParentChildIndex);
				nsNode.Name = newName;

				foreach (var typeNode in origChildren)
					typeNode.TypeDef.Namespace = newNamespace;

				origParentNode.TreeNode.AddChild(nsNode.TreeNode);
				origParentNode.TreeNode.TreeView.SelectItems(new[] { nsNode });
				nsNode.TreeNode.RefreshUI();
			}

			foreach (var info in typeRefInfos)
				info.TypeRef.Namespace = newNamespace;
		}

		public void Undo() {
			if (existingNsNode != null) {
				Debug.Assert(nsNode.TreeNode.Children.Count == 0);
				foreach (var typeNode in origChildren) {
					bool b = existingNsNode.TreeNode.Children.Remove(typeNode.TreeNode);
					Debug.Assert(b);
					if (!b)
						throw new InvalidOperationException();
				}
				for (int i = 0; i < origChildren.Length; i++) {
					var typeNode = origChildren[i];
					typeNode.TypeDef.Namespace = typeNamespaces[i];
					nsNode.TreeNode.Children.Add(typeNode.TreeNode);
				}
			}
			else {
				bool b = origParentNode.TreeNode.Children.Remove(nsNode.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				for (int i = 0; i < origChildren.Length; i++)
					origChildren[i].TypeDef.Namespace = typeNamespaces[i];

				nsNode.Name = origName;
				origParentNode.TreeNode.Children.Insert(origParentChildIndex, nsNode.TreeNode);
				origParentNode.TreeNode.TreeView.SelectItems(new[] { nsNode });
				nsNode.TreeNode.RefreshUI();
			}

			foreach (var info in typeRefInfos)
				info.TypeRef.Namespace = info.OrigNamespace;
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return nsNode; }
		}
	}
}
