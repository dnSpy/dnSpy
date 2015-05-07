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
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using ICSharpCode.ILSpy.TreeNodes;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.AsmEditor.Namespace
{
	sealed class DeleteNamespaceCommand : IUndoCommand
	{
		const string CMD_NAME_SINGULAR = "Delete Namespace";
		const string CMD_NAME_PLURAL_FORMAT = "Delete {0} Namespaces";
		[ExportContextMenuEntry(Category = "AsmEd",
								Icon = "Images/Delete.png",
								Order = 240)]//TODO: Update Order
		[ExportMainMenuCommand(Menu = "_Edit",
							MenuIcon = "Images/Delete.png",
							MenuCategory = "AsmEd",
							MenuOrder = 2100)]//TODO: Set menu order
		sealed class MainMenuEntry : EditCommand
		{
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
			{
				return DeleteNamespaceCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
			{
				DeleteNamespaceCommand.Execute(nodes);
			}

			protected override void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem)
			{
				menuItem.Header = GetCommandName(nodes.Length);
			}
		}

		static string GetCommandName(int count)
		{
			return count == 1 ?
				CMD_NAME_SINGULAR :
				string.Format(CMD_NAME_PLURAL_FORMAT, count);
		}

		static bool CanExecute(ILSpyTreeNode[] nodes)
		{
			return nodes != null &&
				nodes.All(a => a is NamespaceTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes)
		{
			if (!CanExecute(nodes))
				return;

			var nsNodes = nodes.Select(a => (NamespaceTreeNode)a).ToArray();
			UndoCommandManager.Instance.Add(new DeleteNamespaceCommand(nsNodes));
		}

		public struct DeleteModelNodes
		{
			ModuleInfo[] infos;

			class ModuleInfo
			{
				public readonly ModuleDef Module;
				public readonly TypeDef[] Types;
				public readonly int[] Indexes;

				public ModuleInfo(ModuleDef module, int count)
				{
					this.Module = module;
					this.Types = new TypeDef[count];
					this.Indexes = new int[count];
				}
			}

			public void Delete(NamespaceTreeNode[] nodes, ILSpyTreeNode[] parents)
			{
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

			public void Restore(NamespaceTreeNode[] nodes, ILSpyTreeNode[] parents)
			{
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

		DeleteNamespaceCommand(NamespaceTreeNode[] nodes)
		{
			this.parents = nodes.Select(a => (ILSpyTreeNode)a.Parent).ToArray();
			this.nodes = new DeletableNodes<NamespaceTreeNode>(nodes);
			this.modelNodes = new DeleteModelNodes();
		}

		public string Description {
			get { return GetCommandName(nodes.Count); }
		}

		public void Execute()
		{
			nodes.Delete();
			modelNodes.Delete(nodes.Nodes, parents);
		}

		public void Undo()
		{
			modelNodes.Restore(nodes.Nodes, parents);
			nodes.Restore();
		}

		public IEnumerable<ILSpyTreeNode> TreeNodes {
			get { return nodes.Nodes; }
		}

		public void Dispose()
		{
		}
	}

	sealed class MoveNamespaceTypesToEmptypNamespaceCommand : IUndoCommand
	{
		const string CMD_NAME = "Move Types to Empty Namespace";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Images/NameSpace.png",
								Category = "AsmEd",
								Order = 240)]//TODO: Update Order
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							   Menu = "_Edit",
							   MenuIcon = "Images/NameSpace.png",
							   MenuCategory = "AsmEd",
							   MenuOrder = 2100)]//TODO: Set menu order
		sealed class MainMenuEntry : EditCommand
		{
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
			{
				return MoveNamespaceTypesToEmptypNamespaceCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
			{
				MoveNamespaceTypesToEmptypNamespaceCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes)
		{
			return nodes != null &&
				nodes.All(a => a is NamespaceTreeNode) &&
				nodes.Any(a => ((NamespaceTreeNode)a).Name != string.Empty) &&
				nodes.IsInSameModule() &&
				nodes[0].Parent.Children.Any(a => a is NamespaceTreeNode && ((NamespaceTreeNode)a).Name == string.Empty);
		}

		static void Execute(ILSpyTreeNode[] nodes)
		{
			if (!CanExecute(nodes))
				return;

			UndoCommandManager.Instance.Add(new MoveNamespaceTypesToEmptypNamespaceCommand(nodes));
		}

		MoveNamespaceTypesToEmptypNamespaceCommand(ILSpyTreeNode[] nodes)
		{
			if (!CanExecute(nodes))
				throw new ArgumentException();
			var nsNodes = nodes.Where(a => ((NamespaceTreeNode)a).Name != string.Empty).Select(a => (NamespaceTreeNode)a).ToArray();
			Debug.Assert(nsNodes.Length > 0);
			this.nodes = new DeletableNodes<NamespaceTreeNode>(nsNodes);
			this.nsTarget = GetTarget();
		}

		public string Description {
			get { return CMD_NAME; }
		}

		readonly NamespaceTreeNode nsTarget;
		DeletableNodes<NamespaceTreeNode> nodes;
		ModelInfo[] infos;

		class ModelInfo
		{
			public UTF8String[] Namespaces;
			public DeletableNodes<TypeTreeNode> TypeNodes;
		}

		NamespaceTreeNode GetTarget()
		{
			return nodes.Nodes.Length == 0 ? null : (NamespaceTreeNode)nodes.Nodes[0].Parent.Children.First(a => a is NamespaceTreeNode && ((NamespaceTreeNode)a).Name == string.Empty);
		}

		public void Execute()
		{
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
		}

		public void Undo()
		{
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

			infos = null;
		}

		public IEnumerable<ILSpyTreeNode> TreeNodes {
			get {
				yield return nsTarget;
				foreach (var n in nodes.Nodes)
					yield return n;
			}
		}

		public void Dispose()
		{
		}
	}
}
