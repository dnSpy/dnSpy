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
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.Namespace {
	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		public void OnLoaded() {
			MainWindow.Instance.treeView.AddCommandBinding(ApplicationCommands.Delete, new TreeViewCommandProxy(new DeleteNamespaceCommand.TheEditCommand()));
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteNamespaceCommand : IUndoCommand {
		const string CMD_NAME_SINGULAR = "Delete Namespace";
		const string CMD_NAME_PLURAL_FORMAT = "Delete {0} Namespaces";
		[ExportContextMenuEntry(Category = "AsmEd",
								Icon = "Delete",
								InputGestureText = "Del",
								Order = 370)]
		[ExportMainMenuCommand(Menu = "_Edit",
							MenuIcon = "Delete",
							MenuInputGestureText = "Del",
							MenuCategory = "AsmEd",
							MenuOrder = 2170)]
		internal sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return DeleteNamespaceCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				DeleteNamespaceCommand.Execute(nodes);
			}

			protected override void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem) {
				menuItem.Header = GetCommandName(nodes.Length);
			}
		}

		static string GetCommandName(int count) {
			return count == 1 ?
				CMD_NAME_SINGULAR :
				string.Format(CMD_NAME_PLURAL_FORMAT, count);
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				nodes.All(a => a is NamespaceTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var nsNodes = nodes.Select(a => (NamespaceTreeNode)a).ToArray();
			UndoCommandManager.Instance.Add(new DeleteNamespaceCommand(nsNodes));
		}

		public struct DeleteModelNodes {
			ModuleInfo[] infos;

			class ModuleInfo {
				public readonly ModuleDef Module;
				public readonly TypeDef[] Types;
				public readonly int[] Indexes;

				public ModuleInfo(ModuleDef module, int count) {
					this.Module = module;
					this.Types = new TypeDef[count];
					this.Indexes = new int[count];
				}
			}

			public void Delete(NamespaceTreeNode[] nodes, ILSpyTreeNode[] parents) {
				Debug.Assert(parents != null && nodes.Length == parents.Length);
				Debug.Assert(infos == null);
				if (infos != null)
					throw new InvalidOperationException();

				infos = new ModuleInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];
					var module = ILSpyTreeNode.GetModule(parents[i]);
					Debug.Assert(module != null);
					if (module == null)
						throw new InvalidOperationException();

					var info = new ModuleInfo(module, node.Children.Count);
					infos[i] = info;

					for (int j = 0; j < node.Children.Count; j++) {
						var typeNode = (TypeTreeNode)node.Children[j];
						int index = module.Types.IndexOf(typeNode.TypeDefinition);
						Debug.Assert(index >= 0);
						if (index < 0)
							throw new InvalidOperationException();
						module.Types.RemoveAt(index);
						info.Types[j] = typeNode.TypeDefinition;
						info.Indexes[j] = index;
					}
				}
			}

			public void Restore(NamespaceTreeNode[] nodes, ILSpyTreeNode[] parents) {
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

		ILSpyTreeNode[] parents;
		DeletableNodes<NamespaceTreeNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteNamespaceCommand(NamespaceTreeNode[] nodes) {
			this.parents = nodes.Select(a => (ILSpyTreeNode)a.Parent).ToArray();
			this.nodes = new DeletableNodes<NamespaceTreeNode>(nodes);
			this.modelNodes = new DeleteModelNodes();
		}

		public string Description {
			get { return GetCommandName(nodes.Count); }
		}

		public void Execute() {
			nodes.Delete();
			modelNodes.Delete(nodes.Nodes, parents);
		}

		public void Undo() {
			modelNodes.Restore(nodes.Nodes, parents);
			nodes.Restore();
		}

		public IEnumerable<object> ModifiedObjects {
			get { return nodes.Nodes; }
		}

		public void Dispose() {
		}
	}

	struct TypeRefInfo {
		public readonly TypeRef TypeRef;
		public readonly UTF8String OrigNamespace;

		public TypeRefInfo(TypeRef tr) {
			this.TypeRef = tr;
			this.OrigNamespace = tr.Namespace;
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class MoveNamespaceTypesToEmptypNamespaceCommand : IUndoCommand {
		const string CMD_NAME = "Move Types to Empty Namespace";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Namespace",
								Category = "AsmEd",
								Order = 400)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							   Menu = "_Edit",
							   MenuIcon = "Namespace",
							   MenuCategory = "AsmEd",
							   MenuOrder = 2200)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return MoveNamespaceTypesToEmptypNamespaceCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				MoveNamespaceTypesToEmptypNamespaceCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				nodes.Length > 0 &&
				nodes.All(a => a is NamespaceTreeNode) &&
				nodes.Any(a => ((NamespaceTreeNode)a).Name != string.Empty) &&
				nodes.IsInSameModule() &&
				nodes[0].Parent.Children.Any(a => a is NamespaceTreeNode && ((NamespaceTreeNode)a).Name == string.Empty);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			UndoCommandManager.Instance.Add(new MoveNamespaceTypesToEmptypNamespaceCommand(nodes));
		}

		MoveNamespaceTypesToEmptypNamespaceCommand(ILSpyTreeNode[] nodes) {
			var nsNodes = nodes.Where(a => ((NamespaceTreeNode)a).Name != string.Empty).Select(a => (NamespaceTreeNode)a).ToArray();
			Debug.Assert(nsNodes.Length > 0);
			this.nodes = new DeletableNodes<NamespaceTreeNode>(nsNodes);
			this.nsTarget = GetTarget();
			this.typeRefInfos = RenameNamespaceCommand.GetTypeRefInfos(ILSpyTreeNode.GetModule(nodes[0]), nsNodes);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		readonly NamespaceTreeNode nsTarget;
		DeletableNodes<NamespaceTreeNode> nodes;
		ModelInfo[] infos;
		readonly TypeRefInfo[] typeRefInfos;

		class ModelInfo {
			public UTF8String[] Namespaces;
			public DeletableNodes<TypeTreeNode> TypeNodes;
		}

		NamespaceTreeNode GetTarget() {
			return nodes.Nodes.Length == 0 ? null : (NamespaceTreeNode)nodes.Nodes[0].Parent.Children.First(a => a is NamespaceTreeNode && ((NamespaceTreeNode)a).Name == string.Empty);
		}

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
				info.Namespaces = new UTF8String[nsNode.Children.Count];
				info.TypeNodes = new DeletableNodes<TypeTreeNode>(nsNode.Children.Cast<TypeTreeNode>());
				info.TypeNodes.Delete();

				for (int j = 0; j < info.Namespaces.Length; j++) {
					var typeNode = info.TypeNodes.Nodes[j];
					info.Namespaces[j] = typeNode.TypeDefinition.Namespace;
					typeNode.TypeDefinition.Namespace = UTF8String.Empty;
					nsTarget.Append(typeNode);
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
					var typeNode = nsTarget.RemoveLast();
					bool b = info.TypeNodes.Nodes[j] == typeNode;
					Debug.Assert(b);
					if (!b)
						throw new InvalidOperationException();
					typeNode.TypeDefinition.Namespace = info.Namespaces[j];
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

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class RenameNamespaceCommand : IUndoCommand {
		const string CMD_NAME = "Rename Namespace";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Namespace",
								Category = "AsmEd",
								Order = 401)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							   Menu = "_Edit",
							   MenuIcon = "Namespace",
							   MenuCategory = "AsmEd",
							   MenuOrder = 2201)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return RenameNamespaceCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				RenameNamespaceCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes != null &&
				nodes.Length == 1 &&
				nodes[0] is NamespaceTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var nsNode = (NamespaceTreeNode)nodes[0];

			var data = new NamespaceVM(nsNode.Name);
			var win = new NamespaceDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			if (AssemblyTreeNode.NamespaceStringEqualsComparer.Equals(nsNode.Name, data.Name))
				return;

			UndoCommandManager.Instance.Add(new RenameNamespaceCommand(data.Name, nsNode));
		}

		readonly string newName;
		readonly string origName;
		readonly NamespaceTreeNode nsNode;
		readonly NamespaceTreeNode existingNsNode;
		readonly ILSpyTreeNode origParentNode;
		readonly int origParentChildIndex;
		readonly UTF8String[] typeNamespaces;
		readonly TypeTreeNode[] origChildren;
		readonly TypeRefInfo[] typeRefInfos;

		internal static TypeRefInfo[] GetTypeRefInfos(ModuleDef module, IEnumerable<NamespaceTreeNode> nsNodes) {
			var types = new HashSet<ITypeDefOrRef>(RefFinder.TypeEqualityComparerInstance);
			foreach (var nsNode in nsNodes) {
				foreach (TypeTreeNode typeNode in nsNode.Children)
					types.Add(typeNode.TypeDefinition);
			}
			var typeRefs = RefFinder.FindTypeRefsToThisModule(module);
			return typeRefs.Where(a => types.Contains(a)).Select(a => new TypeRefInfo(a)).ToArray();
		}

		RenameNamespaceCommand(string newName, NamespaceTreeNode nsNode) {
			this.newName = newName;
			this.origName = nsNode.Name;
			this.nsNode = nsNode;
			this.existingNsNode = (NamespaceTreeNode)nsNode.Parent.Children.FirstOrDefault(a => a is NamespaceTreeNode && AssemblyTreeNode.NamespaceStringEqualsComparer.Equals(newName, ((NamespaceTreeNode)a).Name));

			var module = ILSpyTreeNode.GetModule(nsNode);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			this.origParentNode = (ILSpyTreeNode)nsNode.Parent;
			this.origParentChildIndex = this.origParentNode.Children.IndexOf(nsNode);
			Debug.Assert(this.origParentChildIndex >= 0);
			if (this.origParentChildIndex < 0)
				throw new InvalidOperationException();

			// Make sure the exact same namespace names are restored if we undo. The names are UTF8
			// strings, but not necessarily canonicalized if it's an obfuscated assembly.
			nsNode.EnsureChildrenFiltered();
			this.origChildren = nsNode.Children.Cast<TypeTreeNode>().ToArray();
			this.typeNamespaces = new UTF8String[nsNode.Children.Count];
			for (int i = 0; i < this.typeNamespaces.Length; i++)
				this.typeNamespaces[i] = origChildren[i].TypeDefinition.Namespace;

			this.typeRefInfos = GetTypeRefInfos(module, new[] { nsNode });
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			UTF8String newNamespace = newName;
			if (existingNsNode != null) {
				Debug.Assert(origChildren.Length == nsNode.Children.Count);
				foreach (var typeNode in origChildren)
					typeNode.OnBeforeRemoved();
				nsNode.Children.Clear();
				foreach (var typeNode in origChildren) {
					typeNode.TypeDefinition.Namespace = newNamespace;
					existingNsNode.AddToChildren(typeNode);
					typeNode.OnReadded();
				}
			}
			else {
				nsNode.OnBeforeRemoved();
				bool b = origParentChildIndex < origParentNode.Children.Count && origParentNode.Children[origParentChildIndex] == nsNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.Children.RemoveAt(origParentChildIndex);
				nsNode.Name = newName;

				foreach (var typeNode in origChildren)
					typeNode.TypeDefinition.Namespace = newNamespace;

				origParentNode.AddToChildren(nsNode);
				nsNode.OnReadded();
			}

			foreach (var info in typeRefInfos)
				info.TypeRef.Namespace = newNamespace;
		}

		public void Undo() {
			if (existingNsNode != null) {
				Debug.Assert(nsNode.Children.Count == 0);
				foreach (var typeNode in origChildren) {
					typeNode.OnBeforeRemoved();
					bool b = existingNsNode.Children.Remove(typeNode);
					Debug.Assert(b);
					if (!b)
						throw new InvalidOperationException();
				}
				for (int i = 0; i < origChildren.Length; i++) {
					var typeNode = origChildren[i];
					typeNode.TypeDefinition.Namespace = typeNamespaces[i];
					nsNode.Children.Add(typeNode);
					typeNode.OnReadded();
				}
			}
			else {
				nsNode.OnBeforeRemoved();
				bool b = origParentNode.Children.Remove(nsNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				for (int i = 0; i < origChildren.Length; i++)
					origChildren[i].TypeDefinition.Namespace = typeNamespaces[i];

				nsNode.Name = origName;
				origParentNode.Children.Insert(origParentChildIndex, nsNode);

				nsNode.OnReadded();
			}

			foreach (var info in typeRefInfos)
				info.TypeRef.Namespace = info.OrigNamespace;
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return nsNode; }
		}

		public void Dispose() {
		}
	}
}
