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
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;

namespace dnSpy.AsmEditor.Types {
	static class TypeConstants {
		public const string DEFAULT_TYPE_NAME = "MyType";
	}

	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, DeleteTypeDefCommand.EditMenuCommand removeCmd, DeleteTypeDefCommand.CodeCommand removeCmd2, TypeDefSettingsCommand.EditMenuCommand settingsCmd, TypeDefSettingsCommand.CodeCommand settingsCmd2) {
			wpfCommandService.AddRemoveCommand(removeCmd);
			wpfCommandService.AddRemoveCommand(removeCmd2, documentTabService);
			wpfCommandService.AddSettingsCommand(documentTabService, settingsCmd, settingsCmd2);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteTypeDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:DeleteTypeCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_DELETE, Order = 20)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService) {
				this.undoCommandService = undoCommandService;
			}

			public override bool IsVisible(AsmEditorContext context) => DeleteTypeDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteTypeDefCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => DeleteTypeDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:DeleteTypeCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 20)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) {
				this.undoCommandService = undoCommandService;
			}

			public override bool IsVisible(AsmEditorContext context) => DeleteTypeDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => DeleteTypeDefCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(AsmEditorContext context) => DeleteTypeDefCommand.GetHeader(context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:DeleteTypeCommand", Icon = DsImagesAttribute.Cancel, InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_DELETE, Order = 20)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IDocumentTreeView documentTreeView)
				: base(documentTreeView) {
				this.undoCommandService = undoCommandService;
			}

			public override bool IsEnabled(CodeContext context) => context.IsDefinition && DeleteTypeDefCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => DeleteTypeDefCommand.Execute(undoCommandService, context.Nodes);
			public override string GetHeader(CodeContext context) => DeleteTypeDefCommand.GetHeader(context.Nodes);
		}

		static string GetHeader(DocumentTreeNodeData[] nodes) {
			nodes = DeleteTypeDefCommand.FilterOutGlobalTypes(nodes);
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.DeleteX, UIUtilities.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.DeleteTypesCommand, nodes.Length);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is TypeNode) &&
				FilterOutGlobalTypes(nodes).Length > 0;
		}

		static DocumentTreeNodeData[] FilterOutGlobalTypes(DocumentTreeNodeData[] nodes) => nodes.Where(a => a is TypeNode && !((TypeNode)a).TypeDef.IsGlobalModuleType).ToArray();

		static void Execute(Lazy<IUndoCommandService> undoCommandService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			if (!Method.DeleteMethodDefCommand.AskDeleteDef(dnSpy_AsmEditor_Resources.AskDeleteType))
				return;

			var typeNodes = FilterOutGlobalTypes(nodes).Cast<TypeNode>().ToArray();
			undoCommandService.Value.Add(new DeleteTypeDefCommand(typeNodes));
		}

		struct DeleteModelNodes {
			ModelInfo[] infos;

			struct ModelInfo {
				public readonly IList<TypeDef> OwnerList;
				public readonly int Index;

				public ModelInfo(TypeDef type) {
					this.OwnerList = type.DeclaringType == null ? type.Module.Types : type.DeclaringType.NestedTypes;
					this.Index = this.OwnerList.IndexOf(type);
					Debug.Assert(this.Index >= 0);
				}
			}

			public void Delete(TypeNode[] nodes) {
				Debug.Assert(infos == null);
				if (infos != null)
					throw new InvalidOperationException();

				infos = new ModelInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];

					var info = new ModelInfo(node.TypeDef);
					infos[i] = info;
					info.OwnerList.RemoveAt(info.Index);
				}
			}

			public void Restore(TypeNode[] nodes) {
				Debug.Assert(infos != null);
				if (infos == null)
					throw new InvalidOperationException();
				Debug.Assert(infos.Length == nodes.Length);
				if (infos.Length != nodes.Length)
					throw new InvalidOperationException();

				for (int i = infos.Length - 1; i >= 0; i--) {
					var node = nodes[i];
					var info = infos[i];
					info.OwnerList.Insert(info.Index, node.TypeDef);
				}

				infos = null;
			}
		}

		DeletableNodes<TypeNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteTypeDefCommand(TypeNode[] asmNodes) {
			nodes = new DeletableNodes<TypeNode>(asmNodes);
		}

		public string Description => dnSpy_AsmEditor_Resources.DeleteTypeCommand;

		public void Execute() {
			nodes.Delete();
			modelNodes.Delete(nodes.Nodes);
		}

		public void Undo() {
			modelNodes.Restore(nodes.Nodes);
			nodes.Restore();
		}

		public IEnumerable<object> ModifiedObjects => nodes.Nodes;
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateTypeDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:CreateTypeCommand", Icon = DsImagesAttribute.NewClass, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 40)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateTypeDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateTypeDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateTypeCommand", Icon = DsImagesAttribute.NewClass, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 40)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateTypeDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateTypeDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is TypeNode ||
				nodes[0] is NamespaceNode ||
				nodes[0] is ModuleDocumentNode);
		}

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var nsNode = nodes[0].GetAncestorOrSelf<NamespaceNode>();
			string ns = nsNode?.Name ?? string.Empty;

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();
			var options = TypeDefOptions.Create(ns, TypeConstants.DEFAULT_TYPE_NAME, module.CorLibTypes.Object.TypeDefOrRef, false);

			var data = new TypeOptionsVM(options, module, appService.DecompilerService, null);
			var win = new TypeOptionsDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateTypeCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateTypeDefCommand(module.Types, nodes[0], data.CreateTypeDefOptions());
			undoCommandService.Value.Add(cmd);
			appService.DocumentTabService.FollowReference(cmd.typeNode);
		}

		readonly IList<TypeDef> ownerList;
		readonly NamespaceNodeCreator nsNodeCreator;
		readonly TypeNode typeNode;

		CreateTypeDefCommand(IList<TypeDef> ownerList, DocumentTreeNodeData ownerNode, TypeDefOptions options) {
			this.ownerList = ownerList;
			var modNode = ownerNode.GetModuleNode();
			Debug.Assert(modNode != null);
			if (modNode == null)
				throw new InvalidOperationException();
			this.nsNodeCreator = new NamespaceNodeCreator(options.Namespace, modNode);
			this.typeNode = modNode.Context.DocumentTreeView.Create(options.CreateTypeDef(modNode.Document.ModuleDef));
		}

		public string Description => dnSpy_AsmEditor_Resources.CreateTypeCommand2;

		public void Execute() {
			nsNodeCreator.Add();
			nsNodeCreator.NamespaceNode.TreeNode.EnsureChildrenLoaded();
			ownerList.Add(typeNode.TypeDef);
			nsNodeCreator.NamespaceNode.TreeNode.AddChild(typeNode.TreeNode);
		}

		public void Undo() {
			bool b = nsNodeCreator.NamespaceNode.TreeNode.Children.Remove(typeNode.TreeNode) &&
					ownerList.Remove(typeNode.TypeDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
			nsNodeCreator.Remove();
		}

		public IEnumerable<object> ModifiedObjects => nsNodeCreator.OriginalNodes;
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateNestedTypeDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:CreateNestedTypeCommand", Icon = DsImagesAttribute.NewClass, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_NEW, Order = 50)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateNestedTypeDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateNestedTypeDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateNestedTypeCommand", Icon = DsImagesAttribute.NewClass, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 50)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => CreateNestedTypeDefCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => CreateNestedTypeDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(Header = "res:CreateNestedTypeCommand", Icon = DsImagesAttribute.NewClass, Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_NEW, Order = 50)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) {
				return context.IsDefinition &&
					context.Nodes.Length == 1 &&
					context.Nodes[0] is TypeNode;
			}

			public override void Execute(CodeContext context) => CreateNestedTypeDefCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) =>
			nodes.Length == 1 &&
			(nodes[0] is TypeNode || (nodes[0].TreeNode.Parent != null && nodes[0].TreeNode.Parent.Data is TypeNode));

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var ownerNode = nodes[0];
			if (!(ownerNode is TypeNode))
				ownerNode = (DocumentTreeNodeData)ownerNode.TreeNode.Parent.Data;
			var typeNode = ownerNode as TypeNode;
			Debug.Assert(typeNode != null);
			if (typeNode == null)
				throw new InvalidOperationException();

			var module = typeNode.GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();
			var options = TypeDefOptions.Create(UTF8String.Empty, TypeConstants.DEFAULT_TYPE_NAME, module.CorLibTypes.Object.TypeDefOrRef, true);

			var data = new TypeOptionsVM(options, module, appService.DecompilerService, null);
			var win = new TypeOptionsDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateNestedTypeCommand2;
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateNestedTypeDefCommand(typeNode, data.CreateTypeDefOptions());
			undoCommandService.Value.Add(cmd);
			appService.DocumentTabService.FollowReference(cmd.nestedType);
		}

		readonly TypeNode ownerType;
		readonly TypeNode nestedType;

		CreateNestedTypeDefCommand(TypeNode ownerType, TypeDefOptions options) {
			this.ownerType = ownerType;

			var modNode = ownerType.GetModuleNode();
			Debug.Assert(modNode != null);
			if (modNode == null)
				throw new InvalidOperationException();
			this.nestedType = ownerType.Create(options.CreateTypeDef(modNode.Document.ModuleDef));
		}

		public string Description => dnSpy_AsmEditor_Resources.CreateNestedTypeCommand2;

		public void Execute() {
			ownerType.TreeNode.EnsureChildrenLoaded();
			ownerType.TypeDef.NestedTypes.Add(nestedType.TypeDef);
			ownerType.TreeNode.AddChild(nestedType.TreeNode);
		}

		public void Undo() {
			bool b = ownerType.TreeNode.Children.Remove(nestedType.TreeNode) &&
					ownerType.TypeDef.NestedTypes.Remove(nestedType.TypeDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return ownerType; }
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class TypeDefSettingsCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditTypeCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_SETTINGS, Order = 20)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => TypeDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => TypeDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditTypeCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 20)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => TypeDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => TypeDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		[Export, ExportMenuItem(Header = "res:EditTypeCommand", Icon = DsImagesAttribute.Settings, InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_SETTINGS, Order = 20)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) => TypeDefSettingsCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => TypeDefSettingsCommand.Execute(undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length == 1 && nodes[0] is TypeNode;

		static void Execute(Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var typeNode = (TypeNode)nodes[0];

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new TypeOptionsVM(new TypeDefOptions(typeNode.TypeDef), module, appService.DecompilerService, typeNode.TypeDef);
			var win = new TypeOptionsDlg();
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandService.Value.Add(new TypeDefSettingsCommand(module, typeNode, data.CreateTypeDefOptions()));
		}

		readonly ModuleDef module;
		readonly TypeNode typeNode;
		readonly TypeDefOptions newOptions;
		readonly TypeDefOptions origOptions;
		readonly NamespaceNodeCreator nsNodeCreator;
		readonly DocumentTreeNodeData origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;
		readonly TypeRefInfo[] typeRefInfos;

		struct TypeRefInfo {
			public readonly TypeRef TypeRef;
			public readonly UTF8String OrigNamespace;
			public readonly UTF8String OrigName;

			public TypeRefInfo(TypeRef tr) {
				this.TypeRef = tr;
				this.OrigNamespace = tr.Namespace;
				this.OrigName = tr.Name;
			}
		}

		TypeDefSettingsCommand(ModuleDef module, TypeNode typeNode, TypeDefOptions options) {
			this.module = module;
			this.typeNode = typeNode;
			this.newOptions = options;
			this.origOptions = new TypeDefOptions(typeNode.TypeDef);

			this.origParentNode = (DocumentTreeNodeData)typeNode.TreeNode.Parent.Data;
			this.origParentChildIndex = this.origParentNode.TreeNode.Children.IndexOf(typeNode.TreeNode);
			Debug.Assert(this.origParentChildIndex >= 0);
			if (this.origParentChildIndex < 0)
				throw new InvalidOperationException();

			this.nameChanged = origOptions.Name != newOptions.Name;
			if (this.origParentNode is NamespaceNode) {
				var modNode = (ModuleDocumentNode)this.origParentNode.TreeNode.Parent.Data;
				if (newOptions.Namespace != origOptions.Namespace)
					this.nsNodeCreator = new NamespaceNodeCreator(newOptions.Namespace, modNode);
			}

			if (this.nameChanged || origOptions.Namespace != newOptions.Namespace)
				this.typeRefInfos = RefFinder.FindTypeRefsToThisModule(module).Where(a => RefFinder.TypeEqualityComparerInstance.Equals(a, typeNode.TypeDef)).Select(a => new TypeRefInfo(a)).ToArray();
		}

		public string Description => dnSpy_AsmEditor_Resources.EditTypeCommand2;

		public void Execute() {
			if (nsNodeCreator != null) {
				bool b = origParentChildIndex < origParentNode.TreeNode.Children.Count && origParentNode.TreeNode.Children[origParentChildIndex] == typeNode.TreeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.TreeNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(typeNode.TypeDef, module);

				nsNodeCreator.Add();
				nsNodeCreator.NamespaceNode.TreeNode.AddChild(typeNode.TreeNode);
			}
			else if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.TreeNode.Children.Count && origParentNode.TreeNode.Children[origParentChildIndex] == typeNode.TreeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.TreeNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(typeNode.TypeDef, module);

				origParentNode.TreeNode.AddChild(typeNode.TreeNode);
			}
			else
				newOptions.CopyTo(typeNode.TypeDef, module);
			if (typeRefInfos != null) {
				foreach (var info in typeRefInfos) {
					info.TypeRef.Namespace = typeNode.TypeDef.Namespace;
					info.TypeRef.Name = typeNode.TypeDef.Name;
				}
			}
			typeNode.TreeNode.RefreshUI();
			InvalidateBaseTypeFolderNode(typeNode);
		}

		public void Undo() {
			if (nsNodeCreator != null) {
				bool b = nsNodeCreator.NamespaceNode.TreeNode.Children.Remove(typeNode.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				nsNodeCreator.Remove();

				origOptions.CopyTo(typeNode.TypeDef, module);
				origParentNode.TreeNode.Children.Insert(origParentChildIndex, typeNode.TreeNode);
			}
			else if (nameChanged) {
				bool b = origParentNode.TreeNode.Children.Remove(typeNode.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(typeNode.TypeDef, module);
				origParentNode.TreeNode.Children.Insert(origParentChildIndex, typeNode.TreeNode);
			}
			else
				origOptions.CopyTo(typeNode.TypeDef, module);
			if (typeRefInfos != null) {
				foreach (var info in typeRefInfos) {
					info.TypeRef.Namespace = info.OrigNamespace;
					info.TypeRef.Name = info.OrigName;
				}
			}
			typeNode.TreeNode.RefreshUI();
			InvalidateBaseTypeFolderNode(typeNode);
		}

		void InvalidateBaseTypeFolderNode(TypeNode typeNode) {
			var btNode = (BaseTypeFolderNode)typeNode.TreeNode.DataChildren.FirstOrDefault(a => a is BaseTypeFolderNode);
			Debug.Assert(btNode != null || typeNode.TreeNode.Children.Count == 0);
			if (btNode != null)
				btNode.InvalidateChildren();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return typeNode; }
		}
	}
}
