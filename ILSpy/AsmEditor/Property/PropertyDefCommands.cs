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

namespace ICSharpCode.ILSpy.AsmEditor.Property
{
	sealed class DeletePropertyDefCommand : IUndoCommand
	{
		const string CMD_NAME = "Delete Property";
		[ExportContextMenuEntry(Header = CMD_NAME,
								Icon = "Images/Delete.png",
								Category = "AsmEd",
								Order = 240)]//TODO: Update Order
		[ExportMainMenuCommand(MenuHeader = CMD_NAME,
							Menu = "_Edit",
							MenuIcon = "Images/Delete.png",
							MenuCategory = "AsmEd",
							MenuOrder = 2100)]//TODO: Set menu order
		sealed class MainMenuEntry : EditCommand
		{
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
			{
				return DeletePropertyDefCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
			{
				DeletePropertyDefCommand.Execute(nodes);
			}

			protected override void Initialize(ILSpyTreeNode[] nodes, MenuItem menuItem)
			{
				if (nodes.Length == 1)
					menuItem.Header = string.Format("Delete {0}", nodes[0].Text);
				else
					menuItem.Header = string.Format("Delete {0} properties", nodes.Length);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes)
		{
			return nodes.Length > 0 &&
				nodes.All(n => n is PropertyTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes)
		{
			if (!CanExecute(nodes))
				return;

			var propNodes = nodes.Select(a => (PropertyTreeNode)a).ToArray();
			UndoCommandManager.Instance.Add(new DeletePropertyDefCommand(propNodes));
		}

		public struct DeleteModelNodes
		{
			ModelInfo[] infos;

			struct ModelInfo
			{
				public readonly TypeDef OwnerType;
				public readonly int PropertyIndex;
				public readonly int[] MethodIndexes;
				public readonly MethodDef[] Methods;

				public ModelInfo(PropertyDef evt)
				{
					this.OwnerType = evt.DeclaringType;
					this.PropertyIndex = this.OwnerType.Properties.IndexOf(evt);
					Debug.Assert(this.PropertyIndex >= 0);
					this.Methods = new HashSet<MethodDef>(GetMethods(evt)).ToArray();
					this.MethodIndexes = new int[this.Methods.Length];
				}

				static IEnumerable<MethodDef> GetMethods(PropertyDef evt)
				{
					foreach (var m in evt.GetMethods) yield return m;
					foreach (var m in evt.SetMethods) yield return m;
					foreach (var m in evt.OtherMethods) yield return m;
				}
			}

			public void Delete(PropertyTreeNode[] nodes)
			{
				Debug.Assert(infos == null);
				if (infos != null)
					throw new InvalidOperationException();

				infos = new ModelInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];

					var info = new ModelInfo(node.PropertyDefinition);
					infos[i] = info;
					info.OwnerType.Properties.RemoveAt(info.PropertyIndex);

					for (int j = 0; j < info.Methods.Length; j++) {
						int index = info.OwnerType.Methods.IndexOf(info.Methods[j]);
						Debug.Assert(index >= 0);
						if (index < 0)
							throw new InvalidOperationException();
						info.OwnerType.Methods.RemoveAt(index);
						info.MethodIndexes[j] = index;
					}
				}
			}

			public void Restore(PropertyTreeNode[] nodes)
			{
				Debug.Assert(infos != null);
				if (infos == null)
					throw new InvalidOperationException();
				Debug.Assert(infos.Length == nodes.Length);
				if (infos.Length != nodes.Length)
					throw new InvalidOperationException();

				for (int i = infos.Length - 1; i >= 0; i--) {
					var node = nodes[i];
					var info = infos[i];
					info.OwnerType.Properties.Insert(info.PropertyIndex, node.PropertyDefinition);

					for (int j = info.Methods.Length - 1; j >= 0; j--)
						info.OwnerType.Methods.Insert(info.MethodIndexes[j], info.Methods[j]);
				}

				infos = null;
			}
		}

		DeletableNodes<PropertyTreeNode> nodes;
		DeleteModelNodes modelNodes;

		DeletePropertyDefCommand(PropertyTreeNode[] propNodes)
		{
			this.nodes = new DeletableNodes<PropertyTreeNode>(propNodes);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute()
		{
			nodes.Delete();
			modelNodes.Delete(nodes.Nodes);
		}

		public void Undo()
		{
			modelNodes.Restore(nodes.Nodes);
			nodes.Restore();
		}

		public IEnumerable<ILSpyTreeNode> TreeNodes {
			get { return nodes.Nodes; }
		}

		public void Dispose()
		{
		}
	}

	sealed class CreatePropertyDefCommand : IUndoCommand
	{
		const string CMD_NAME = "Create Property";
		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "Images/Class.png",
								Category = "AsmEd",
								Order = 240)]//TODO: Update Order
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "...",
							Menu = "_Edit",
							MenuIcon = "Images/Class.png",
							MenuCategory = "AsmEd",
							MenuOrder = 2100)]//TODO: Set menu order
		sealed class MainMenuEntry : EditCommand
		{
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
			{
				return CreatePropertyDefCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
			{
				CreatePropertyDefCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes)
		{
			return nodes.Length == 1 &&
				(nodes[0] is TypeTreeNode || nodes[0].Parent is TypeTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes)
		{
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
			var options = PropertyDefOptions.Create(module, "MyProperty");

			var data = new PropertyOptionsVM(options, module, MainWindow.Instance.CurrentLanguage, typeNode.TypeDefinition);
			var win = new PropertyOptionsDlg();
			win.Title = "Create Property";
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new CreatePropertyDefCommand((TypeTreeNode)ownerNode, data.CreatePropertyDefOptions()));
		}

		readonly TypeTreeNode ownerNode;
		readonly PropertyTreeNode propNode;

		CreatePropertyDefCommand(TypeTreeNode ownerNode, PropertyDefOptions options)
		{
			this.ownerNode = ownerNode;
			this.propNode = new PropertyTreeNode(options.CreatePropertyDef());
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute()
		{
			ownerNode.EnsureChildrenFiltered();
			ownerNode.TypeDefinition.Properties.Add(propNode.PropertyDefinition);
			ownerNode.AddToChildren(propNode);
		}

		public void Undo()
		{
			bool b = ownerNode.Children.Remove(propNode) &&
					ownerNode.TypeDefinition.Properties.Remove(propNode.PropertyDefinition);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<ILSpyTreeNode> TreeNodes {
			get { yield return ownerNode; }
		}

		public void Dispose()
		{
		}
	}

	sealed class PropertyDefSettingsCommand : IUndoCommand
	{
		const string CMD_NAME = "Property Settings";
		[ExportContextMenuEntry(Header = CMD_NAME + "...",
								Icon = "Images/Settings.png",
								Category = "AsmEd",
								Order = 240)]//TODO: Update Order
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "...",
							Menu = "_Edit",
							MenuIcon = "Images/Settings.png",
							MenuCategory = "AsmEd",
							MenuOrder = 2100)]//TODO: Set menu order
		sealed class MainMenuEntry : EditCommand
		{
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes)
			{
				return PropertyDefSettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes)
			{
				PropertyDefSettingsCommand.Execute(nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes)
		{
			return nodes.Length == 1 &&
				nodes[0] is PropertyTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes)
		{
			if (!CanExecute(nodes))
				return;

			var propNode = (PropertyTreeNode)nodes[0];

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new PropertyOptionsVM(new PropertyDefOptions(propNode.PropertyDefinition), module, MainWindow.Instance.CurrentLanguage, propNode.PropertyDefinition.DeclaringType);
			var win = new PropertyOptionsDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new PropertyDefSettingsCommand(propNode, data.CreatePropertyDefOptions()));
		}

		readonly PropertyTreeNode propNode;
		readonly PropertyDefOptions newOptions;
		readonly PropertyDefOptions origOptions;
		readonly ILSpyTreeNode origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;

		PropertyDefSettingsCommand(PropertyTreeNode propNode, PropertyDefOptions options)
		{
			this.propNode = propNode;
			this.newOptions = options;
			this.origOptions = new PropertyDefOptions(propNode.PropertyDefinition);

			this.origParentNode = (ILSpyTreeNode)propNode.Parent;
			this.origParentChildIndex = this.origParentNode.Children.IndexOf(propNode);
			Debug.Assert(this.origParentChildIndex >= 0);
			if (this.origParentChildIndex < 0)
				throw new InvalidOperationException();

			this.nameChanged = origOptions.Name != newOptions.Name;
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute()
		{
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.Children.Count && origParentNode.Children[origParentChildIndex] == propNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(propNode.PropertyDefinition);

				origParentNode.AddToChildren(propNode);
			}
			else
				newOptions.CopyTo(propNode.PropertyDefinition);
			propNode.RaiseUIPropsChanged();
		}

		public void Undo()
		{
			if (nameChanged) {
				bool b = origParentNode.Children.Remove(propNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(propNode.PropertyDefinition);
				origParentNode.Children.Insert(origParentChildIndex, propNode);
			}
			else
				origOptions.CopyTo(propNode.PropertyDefinition);
			propNode.RaiseUIPropsChanged();
		}

		public IEnumerable<ILSpyTreeNode> TreeNodes {
			get { yield return propNode; }
		}

		public void Dispose()
		{
		}
	}
}
