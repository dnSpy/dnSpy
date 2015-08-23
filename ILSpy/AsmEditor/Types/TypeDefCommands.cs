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
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.Types {
	static class TypeConstants {
		public const string DEFAULT_TYPE_NAME = "MyType";
	}

	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		public void OnLoaded() {
			MainWindow.Instance.treeView.CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, DeleteExecuted, DeleteCanExecute));
			MainWindow.Instance.CodeBindings.Add(EditingCommands.Delete, new TextEditorCommandProxy(new DeleteTypeDefCommand.TheTextEditorCommand()), ModifierKeys.None, Key.Delete);
		}

		void DeleteCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = DeleteTypeDefCommand.CanExecute(MainWindow.Instance.SelectedNodes);
		}

		void DeleteExecuted(object sender, ExecutedRoutedEventArgs e) {
			DeleteTypeDefCommand.Execute(MainWindow.Instance.SelectedNodes);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteTypeDefCommand : IUndoCommand {
		const string CMD_NAME = "Delete Type";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 320)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							Menu = "_Edit",
							MenuIcon = "Delete",
							MenuInputGestureText = "Del",
							MenuCategory = "AsmEd",
							MenuOrder = 2120)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return DeleteTypeDefCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				DeleteTypeDefCommand.Execute(nodes);
			}

			protected override void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
				DeleteTypeDefCommand.Initialize(nodes, menuItem);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Delete",
								InputGestureText = "Del",
								Category = "AsmEd",
								Order = 320)]
		internal sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					DeleteTypeDefCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				DeleteTypeDefCommand.Execute(ctx.Nodes);
			}

			protected override void Initialize(Context ctx, MenuItem menuItem) {
				DeleteTypeDefCommand.Initialize(ctx.Nodes, menuItem);
			}
		}

		static void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
			nodes = DeleteTypeDefCommand.FilterOutGlobalTypes(nodes);
			if (nodes.Length == 1)
				menuItem.Header = string.Format("Delete {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			else
				menuItem.Header = string.Format("Delete {0} types", nodes.Length);
		}

		internal static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is TypeTreeNode) &&
				FilterOutGlobalTypes(nodes).Length > 0;
		}

		static ILSpyTreeNode[] FilterOutGlobalTypes(ILSpyTreeNode[] nodes) {
			return nodes.Where(a => a is TypeTreeNode && !((TypeTreeNode)a).TypeDefinition.IsGlobalModuleType).ToArray();
		}

		internal static void Execute(ILSpyTreeNode[] nodes) {
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

					var info = new ModelInfo(node.TypeDefinition);
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
					info.OwnerList.Insert(info.Index, node.TypeDefinition);
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewClass",
								Category = "AsmEd",
								Order = 540)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewClass",
							MenuCategory = "AsmEd",
							MenuOrder = 2340)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateTypeDefCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateTypeDefCommand.Execute(nodes);
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
			win.Title = "Create Type";
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new CreateTypeDefCommand(module.Types, nodes[0], data.CreateTypeDefOptions()));
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
			this.typeNode = new TypeTreeNode(options.CreateTypeDef(modNode.LoadedAssembly.ModuleDefinition), modNode.Parent as AssemblyTreeNode ?? modNode);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			nsNodeCreator.Add();
			nsNodeCreator.NamespaceTreeNode.EnsureChildrenFiltered();
			ownerList.Add(typeNode.TypeDefinition);
			nsNodeCreator.NamespaceTreeNode.AddToChildren(typeNode);
			typeNode.OnReadded();
		}

		public void Undo() {
			typeNode.OnBeforeRemoved();
			bool b = nsNodeCreator.NamespaceTreeNode.Children.Remove(typeNode) &&
					ownerList.Remove(typeNode.TypeDefinition);
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewClass",
								Category = "AsmEd",
								Order = 550)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "NewClass",
							MenuCategory = "AsmEd",
							MenuOrder = 2350)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return CreateNestedTypeDefCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				CreateNestedTypeDefCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "NewClass",
								Category = "AsmEd",
								Order = 550)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					ctx.Nodes.Length == 1 &&
					ctx.Nodes[0] is TypeTreeNode;
			}

			protected override void Execute(Context ctx) {
				CreateNestedTypeDefCommand.Execute(ctx.Nodes);
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
			win.Title = "Create Nested Type";
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new CreateNestedTypeDefCommand(typeNode, data.CreateTypeDefOptions()));
		}

		readonly TypeTreeNode ownerType;
		readonly TypeTreeNode nestedType;

		CreateNestedTypeDefCommand(TypeTreeNode ownerType, TypeDefOptions options) {
			this.ownerType = ownerType;

			var modNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(ownerType);
			Debug.Assert(modNode != null);
			if (modNode == null)
				throw new InvalidOperationException();
			this.nestedType = new TypeTreeNode(options.CreateTypeDef(modNode.LoadedAssembly.ModuleDefinition), modNode.Parent as AssemblyTreeNode ?? modNode);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			ownerType.EnsureChildrenFiltered();
			ownerType.TypeDefinition.NestedTypes.Add(nestedType.TypeDefinition);
			ownerType.AddToChildren(nestedType);
			nestedType.OnReadded();
		}

		public void Undo() {
			nestedType.OnBeforeRemoved();
			bool b = ownerType.Children.Remove(nestedType) &&
					ownerType.TypeDefinition.NestedTypes.Remove(nestedType.TypeDefinition);
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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 620)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "Settings",
							MenuCategory = "AsmEd",
							MenuOrder = 2420)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return TypeDefSettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				TypeDefSettingsCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "Settings",
								Category = "AsmEd",
								Order = 620)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return TypeDefSettingsCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				TypeDefSettingsCommand.Execute(ctx.Nodes);
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

			var data = new TypeOptionsVM(new TypeDefOptions(typeNode.TypeDefinition), module, MainWindow.Instance.CurrentLanguage, typeNode.TypeDefinition);
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
			this.origOptions = new TypeDefOptions(typeNode.TypeDefinition);

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
				this.typeRefInfos = RefFinder.FindTypeRefsToThisModule(module).Where(a => RefFinder.TypeEqualityComparerInstance.Equals(a, typeNode.TypeDefinition)).Select(a => new TypeRefInfo(a)).ToArray();
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
				newOptions.CopyTo(typeNode.TypeDefinition, module);

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
				newOptions.CopyTo(typeNode.TypeDefinition, module);

				origParentNode.AddToChildren(typeNode);
				typeNode.OnReadded();
			}
			else
				newOptions.CopyTo(typeNode.TypeDefinition, module);
			if (typeRefInfos != null) {
				foreach (var info in typeRefInfos) {
					info.TypeRef.Namespace = typeNode.TypeDefinition.Namespace;
					info.TypeRef.Name = typeNode.TypeDefinition.Name;
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

				origOptions.CopyTo(typeNode.TypeDefinition, module);
				origParentNode.Children.Insert(origParentChildIndex, typeNode);
				typeNode.OnReadded();
			}
			else if (nameChanged) {
				typeNode.OnBeforeRemoved();
				bool b = origParentNode.Children.Remove(typeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(typeNode.TypeDefinition, module);
				origParentNode.Children.Insert(origParentChildIndex, typeNode);
				typeNode.OnReadded();
			}
			else
				origOptions.CopyTo(typeNode.TypeDefinition, module);
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
