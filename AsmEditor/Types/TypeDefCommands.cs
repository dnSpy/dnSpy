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
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Menus;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.Types {
	static class TypeConstants {
		public const string DEFAULT_TYPE_NAME = "MyType";
	}

	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		void IPlugin.EarlyInit() {
		}

		public void OnLoaded() {
			MainWindow.Instance.TreeView.AddCommandBinding(ApplicationCommands.Delete, new EditMenuHandlerCommandProxy(new DeleteTypeDefCommand.EditMenuCommand()));
			MainWindow.Instance.CodeBindings.Add(EditingCommands.Delete, new CodeContextMenuHandlerCommandProxy(new DeleteTypeDefCommand.CodeCommand()), ModifierKeys.None, Key.Delete);
			Utils.InstallSettingsCommand(new TypeDefSettingsCommand.EditMenuCommand(), new TypeDefSettingsCommand.CodeCommand());
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteTypeDefCommand : IUndoCommand {
		const string CMD_NAME = "Delete Type";
		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_FILES_ASMED_DELETE, Order = 20)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return DeleteTypeDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteTypeDefCommand.Execute(context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteTypeDefCommand.GetHeader(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 20)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return DeleteTypeDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteTypeDefCommand.Execute(context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteTypeDefCommand.GetHeader(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_CODE_ASMED_DELTE, Order = 20)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					DeleteTypeDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				DeleteTypeDefCommand.Execute(context.Nodes);
			}

			public override string GetHeader(CodeContext context) {
				return DeleteTypeDefCommand.GetHeader(context.Nodes);
			}
		}

		static string GetHeader(ILSpyTreeNode[] nodes) {
			nodes = DeleteTypeDefCommand.FilterOutGlobalTypes(nodes);
			if (nodes.Length == 1)
				return string.Format("Delete {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format("Delete {0} types", nodes.Length);
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is TypeTreeNode) &&
				FilterOutGlobalTypes(nodes).Length > 0;
		}

		static ILSpyTreeNode[] FilterOutGlobalTypes(ILSpyTreeNode[] nodes) {
			return nodes.Where(a => a is TypeTreeNode && !((TypeTreeNode)a).TypeDef.IsGlobalModuleType).ToArray();
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			if (!Method.DeleteMethodDefCommand.AskDeleteDef("type"))
				return;

			var typeNodes = FilterOutGlobalTypes(nodes).Select(a => (TypeTreeNode)a).ToArray();
			UndoCommandManager.Instance.Add(new DeleteTypeDefCommand(typeNodes));
		}

		public struct DeleteModelNodes {
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

			public void Delete(TypeTreeNode[] nodes) {
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

			public void Restore(TypeTreeNode[] nodes) {
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

		DeletableNodes<TypeTreeNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteTypeDefCommand(TypeTreeNode[] asmNodes) {
			nodes = new DeletableNodes<TypeTreeNode>(asmNodes);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			nodes.Delete();
			modelNodes.Delete(nodes.Nodes);
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
	sealed class CreateTypeDefCommand : IUndoCommand {
		const string CMD_NAME = "Create Type";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewClass", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 40)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateTypeDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateTypeDefCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewClass", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 40)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateTypeDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateTypeDefCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is TypeTreeNode ||
				nodes[0] is NamespaceTreeNode ||
				(nodes[0] is AssemblyTreeNode && ((AssemblyTreeNode)nodes[0]).IsModule));
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var nsNode = ILSpyTreeNode.GetNode<NamespaceTreeNode>(nodes[0]);
			string ns = nsNode == null ? string.Empty : nsNode.Name;

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();
			var options = TypeDefOptions.Create(ns, TypeConstants.DEFAULT_TYPE_NAME, module.CorLibTypes.Object.TypeDefOrRef, false);

			var data = new TypeOptionsVM(options, module, MainWindow.Instance.CurrentLanguage, null);
			var win = new TypeOptionsDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateTypeDefCommand(module.Types, nodes[0], data.CreateTypeDefOptions());
			UndoCommandManager.Instance.Add(cmd);
			MainWindow.Instance.JumpToReference(cmd.typeNode);
		}

		readonly IList<TypeDef> ownerList;
		readonly NamespaceTreeNodeCreator nsNodeCreator;
		readonly TypeTreeNode typeNode;

		CreateTypeDefCommand(IList<TypeDef> ownerList, ILSpyTreeNode ownerNode, TypeDefOptions options) {
			this.ownerList = ownerList;
			var modNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(ownerNode);
			Debug.Assert(modNode != null);
			if (modNode == null)
				throw new InvalidOperationException();
			this.nsNodeCreator = new NamespaceTreeNodeCreator(options.Namespace, modNode);
			this.typeNode = new TypeTreeNode(options.CreateTypeDef(modNode.DnSpyFile.ModuleDef), modNode.Parent as AssemblyTreeNode ?? modNode);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			nsNodeCreator.Add();
			nsNodeCreator.NamespaceTreeNode.EnsureChildrenFiltered();
			ownerList.Add(typeNode.TypeDef);
			nsNodeCreator.NamespaceTreeNode.AddToChildren(typeNode);
			typeNode.OnReadded();
		}

		public void Undo() {
			typeNode.OnBeforeRemoved();
			bool b = nsNodeCreator.NamespaceTreeNode.Children.Remove(typeNode) &&
					ownerList.Remove(typeNode.TypeDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
			nsNodeCreator.Remove();
		}

		public IEnumerable<object> ModifiedObjects {
			get { return nsNodeCreator.OriginalNodes; }
		}

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateNestedTypeDefCommand : IUndoCommand {
		const string CMD_NAME = "Create Nested Type";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewClass", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 50)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateNestedTypeDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateNestedTypeDefCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewClass", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 50)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateNestedTypeDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateNestedTypeDefCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewClass", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 50)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					context.Nodes.Length == 1 &&
					context.Nodes[0] is TypeTreeNode;
			}

			public override void Execute(CodeContext context) {
				CreateNestedTypeDefCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is TypeTreeNode || nodes[0].Parent is TypeTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var ownerNode = nodes[0];
			if (!(ownerNode is TypeTreeNode))
				ownerNode = (ILSpyTreeNode)ownerNode.Parent;
			var typeNode = ownerNode as TypeTreeNode;
			Debug.Assert(typeNode != null);
			if (typeNode == null)
				throw new InvalidOperationException();

			var module = ILSpyTreeNode.GetModule(typeNode);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();
			var options = TypeDefOptions.Create(UTF8String.Empty, TypeConstants.DEFAULT_TYPE_NAME, module.CorLibTypes.Object.TypeDefOrRef, true);

			var data = new TypeOptionsVM(options, module, MainWindow.Instance.CurrentLanguage, null);
			var win = new TypeOptionsDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateNestedTypeDefCommand(typeNode, data.CreateTypeDefOptions());
			UndoCommandManager.Instance.Add(cmd);
			MainWindow.Instance.JumpToReference(cmd.nestedType);
		}

		readonly TypeTreeNode ownerType;
		readonly TypeTreeNode nestedType;

		CreateNestedTypeDefCommand(TypeTreeNode ownerType, TypeDefOptions options) {
			this.ownerType = ownerType;

			var modNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(ownerType);
			Debug.Assert(modNode != null);
			if (modNode == null)
				throw new InvalidOperationException();
			this.nestedType = new TypeTreeNode(options.CreateTypeDef(modNode.DnSpyFile.ModuleDef), modNode.Parent as AssemblyTreeNode ?? modNode);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			ownerType.EnsureChildrenFiltered();
			ownerType.TypeDef.NestedTypes.Add(nestedType.TypeDef);
			ownerType.AddToChildren(nestedType);
			nestedType.OnReadded();
		}

		public void Undo() {
			nestedType.OnBeforeRemoved();
			bool b = ownerType.Children.Remove(nestedType) &&
					ownerType.TypeDef.NestedTypes.Remove(nestedType.TypeDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return ownerType; }
		}

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class TypeDefSettingsCommand : IUndoCommand {
		const string CMD_NAME = "Edit Type";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 20)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return TypeDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				TypeDefSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 20)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return TypeDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				TypeDefSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_CODE_ASMED_SETTINGS, Order = 20)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return TypeDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				TypeDefSettingsCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is TypeTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var typeNode = (TypeTreeNode)nodes[0];

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new TypeOptionsVM(new TypeDefOptions(typeNode.TypeDef), module, MainWindow.Instance.CurrentLanguage, typeNode.TypeDef);
			var win = new TypeOptionsDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new TypeDefSettingsCommand(module, typeNode, data.CreateTypeDefOptions()));
		}

		readonly ModuleDef module;
		readonly TypeTreeNode typeNode;
		readonly TypeDefOptions newOptions;
		readonly TypeDefOptions origOptions;
		readonly NamespaceTreeNodeCreator nsNodeCreator;
		readonly ILSpyTreeNode origParentNode;
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

		TypeDefSettingsCommand(ModuleDef module, TypeTreeNode typeNode, TypeDefOptions options) {
			this.module = module;
			this.typeNode = typeNode;
			this.newOptions = options;
			this.origOptions = new TypeDefOptions(typeNode.TypeDef);

			this.origParentNode = (ILSpyTreeNode)typeNode.Parent;
			this.origParentChildIndex = this.origParentNode.Children.IndexOf(typeNode);
			Debug.Assert(this.origParentChildIndex >= 0);
			if (this.origParentChildIndex < 0)
				throw new InvalidOperationException();

			this.nameChanged = origOptions.Name != newOptions.Name;
			if (this.origParentNode is NamespaceTreeNode) {
				var asmNode = (AssemblyTreeNode)this.origParentNode.Parent;
				if (!AssemblyTreeNode.NamespaceStringEqualsComparer.Equals(newOptions.Namespace, origOptions.Namespace))
					this.nsNodeCreator = new NamespaceTreeNodeCreator(newOptions.Namespace, asmNode);
			}

			if (this.nameChanged || origOptions.Namespace != newOptions.Namespace)
				this.typeRefInfos = RefFinder.FindTypeRefsToThisModule(module).Where(a => RefFinder.TypeEqualityComparerInstance.Equals(a, typeNode.TypeDef)).Select(a => new TypeRefInfo(a)).ToArray();
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			if (nsNodeCreator != null) {
				typeNode.OnBeforeRemoved();
				bool b = origParentChildIndex < origParentNode.Children.Count && origParentNode.Children[origParentChildIndex] == typeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(typeNode.TypeDef, module);

				nsNodeCreator.Add();
				nsNodeCreator.NamespaceTreeNode.AddToChildren(typeNode);
				typeNode.OnReadded();
			}
			else if (nameChanged) {
				typeNode.OnBeforeRemoved();
				bool b = origParentChildIndex < origParentNode.Children.Count && origParentNode.Children[origParentChildIndex] == typeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(typeNode.TypeDef, module);

				origParentNode.AddToChildren(typeNode);
				typeNode.OnReadded();
			}
			else
				newOptions.CopyTo(typeNode.TypeDef, module);
			if (typeRefInfos != null) {
				foreach (var info in typeRefInfos) {
					info.TypeRef.Namespace = typeNode.TypeDef.Namespace;
					info.TypeRef.Name = typeNode.TypeDef.Name;
				}
			}
			typeNode.RaiseUIPropsChanged();
			typeNode.InvalidateInterfacesNode();
		}

		public void Undo() {
			if (nsNodeCreator != null) {
				typeNode.OnBeforeRemoved();
				bool b = nsNodeCreator.NamespaceTreeNode.Children.Remove(typeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				nsNodeCreator.Remove();

				origOptions.CopyTo(typeNode.TypeDef, module);
				origParentNode.Children.Insert(origParentChildIndex, typeNode);
				typeNode.OnReadded();
			}
			else if (nameChanged) {
				typeNode.OnBeforeRemoved();
				bool b = origParentNode.Children.Remove(typeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(typeNode.TypeDef, module);
				origParentNode.Children.Insert(origParentChildIndex, typeNode);
				typeNode.OnReadded();
			}
			else
				origOptions.CopyTo(typeNode.TypeDef, module);
			if (typeRefInfos != null) {
				foreach (var info in typeRefInfos) {
					info.TypeRef.Namespace = info.OrigNamespace;
					info.TypeRef.Name = info.OrigName;
				}
			}
			typeNode.RaiseUIPropsChanged();
			typeNode.InvalidateInterfacesNode();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return typeNode; }
		}

		public void Dispose() {
		}
	}
}
