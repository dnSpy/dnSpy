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

namespace dnSpy.AsmEditor.Property {
	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		void IPlugin.EarlyInit() {
		}

		public void OnLoaded() {
			MainWindow.Instance.TreeView.AddCommandBinding(ApplicationCommands.Delete, new EditMenuHandlerCommandProxy(new DeletePropertyDefCommand.EditMenuCommand()));
			MainWindow.Instance.CodeBindings.Add(EditingCommands.Delete, new CodeContextMenuHandlerCommandProxy(new DeletePropertyDefCommand.CodeCommand()), ModifierKeys.None, Key.Delete);
			Utils.InstallSettingsCommand(new PropertyDefSettingsCommand.EditMenuCommand(), new PropertyDefSettingsCommand.CodeCommand());
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeletePropertyDefCommand : IUndoCommand {
		const string CMD_NAME = "Delete Property";
		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_FILES_ASMED_DELETE, Order = 50)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return DeletePropertyDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeletePropertyDefCommand.Execute(context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeletePropertyDefCommand.GetHeader(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 50)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return DeletePropertyDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeletePropertyDefCommand.Execute(context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeletePropertyDefCommand.GetHeader(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_CODE_ASMED_DELTE, Order = 50)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					DeletePropertyDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				DeletePropertyDefCommand.Execute(context.Nodes);
			}

			public override string GetHeader(CodeContext context) {
				return DeletePropertyDefCommand.GetHeader(context.Nodes);
			}
		}

		static string GetHeader(ILSpyTreeNode[] nodes) {
			if (nodes.Length == 1)
				return string.Format("Delete {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format("Delete {0} properties", nodes.Length);
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is PropertyTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var propNodes = nodes.Select(a => (PropertyTreeNode)a).ToArray();
			UndoCommandManager.Instance.Add(new DeletePropertyDefCommand(propNodes));
		}

		public struct DeleteModelNodes {
			ModelInfo[] infos;

			struct ModelInfo {
				public readonly TypeDef OwnerType;
				public readonly int PropertyIndex;
				public readonly int[] MethodIndexes;
				public readonly MethodDef[] Methods;

				public ModelInfo(PropertyDef evt) {
					this.OwnerType = evt.DeclaringType;
					this.PropertyIndex = this.OwnerType.Properties.IndexOf(evt);
					Debug.Assert(this.PropertyIndex >= 0);
					this.Methods = new HashSet<MethodDef>(GetMethods(evt)).ToArray();
					this.MethodIndexes = new int[this.Methods.Length];
				}

				static IEnumerable<MethodDef> GetMethods(PropertyDef evt) {
					foreach (var m in evt.GetMethods) yield return m;
					foreach (var m in evt.SetMethods) yield return m;
					foreach (var m in evt.OtherMethods) yield return m;
				}
			}

			public void Delete(PropertyTreeNode[] nodes) {
				Debug.Assert(infos == null);
				if (infos != null)
					throw new InvalidOperationException();

				infos = new ModelInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];

					var info = new ModelInfo(node.PropertyDef);
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

			public void Restore(PropertyTreeNode[] nodes) {
				Debug.Assert(infos != null);
				if (infos == null)
					throw new InvalidOperationException();
				Debug.Assert(infos.Length == nodes.Length);
				if (infos.Length != nodes.Length)
					throw new InvalidOperationException();

				for (int i = infos.Length - 1; i >= 0; i--) {
					var node = nodes[i];
					var info = infos[i];
					info.OwnerType.Properties.Insert(info.PropertyIndex, node.PropertyDef);

					for (int j = info.Methods.Length - 1; j >= 0; j--)
						info.OwnerType.Methods.Insert(info.MethodIndexes[j], info.Methods[j]);
				}

				infos = null;
			}
		}

		DeletableNodes<PropertyTreeNode> nodes;
		DeleteModelNodes modelNodes;

		DeletePropertyDefCommand(PropertyTreeNode[] propNodes) {
			this.nodes = new DeletableNodes<PropertyTreeNode>(propNodes);
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
	sealed class CreatePropertyDefCommand : IUndoCommand {
		const string CMD_NAME = "Create Property";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewProperty", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 80)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreatePropertyDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreatePropertyDefCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewProperty", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 80)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreatePropertyDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreatePropertyDefCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewProperty", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 80)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					context.Nodes.Length == 1 &&
					context.Nodes[0] is TypeTreeNode;
			}

			public override void Execute(CodeContext context) {
				CreatePropertyDefCommand.Execute(context.Nodes);
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

			bool isInstance = !(typeNode.TypeDef.IsAbstract && typeNode.TypeDef.IsSealed);
			var options = PropertyDefOptions.Create(module, "MyProperty", isInstance);

			var data = new PropertyOptionsVM(options, module, MainWindow.Instance.CurrentLanguage, typeNode.TypeDef);
			var win = new PropertyOptionsDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreatePropertyDefCommand(typeNode, data.CreatePropertyDefOptions());
			UndoCommandManager.Instance.Add(cmd);
			MainWindow.Instance.JumpToReference(cmd.propNode);
		}

		readonly TypeTreeNode ownerNode;
		readonly PropertyTreeNode propNode;

		CreatePropertyDefCommand(TypeTreeNode ownerNode, PropertyDefOptions options) {
			this.ownerNode = ownerNode;
			this.propNode = new PropertyTreeNode(options.CreatePropertyDef(ownerNode.TypeDef.Module), ownerNode);
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			ownerNode.EnsureChildrenFiltered();
			ownerNode.TypeDef.Properties.Add(propNode.PropertyDef);
			ownerNode.AddToChildren(propNode);
		}

		public void Undo() {
			bool b = ownerNode.Children.Remove(propNode) &&
					ownerNode.TypeDef.Properties.Remove(propNode.PropertyDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return ownerNode; }
		}

		public void Dispose() {
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class PropertyDefSettingsCommand : IUndoCommand {
		const string CMD_NAME = "Edit Property";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 60)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return PropertyDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				PropertyDefSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 60)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return PropertyDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				PropertyDefSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_CODE_ASMED_SETTINGS, Order = 60)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return PropertyDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				PropertyDefSettingsCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is PropertyTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var propNode = (PropertyTreeNode)nodes[0];

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new PropertyOptionsVM(new PropertyDefOptions(propNode.PropertyDef), module, MainWindow.Instance.CurrentLanguage, propNode.PropertyDef.DeclaringType);
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

		PropertyDefSettingsCommand(PropertyTreeNode propNode, PropertyDefOptions options) {
			this.propNode = propNode;
			this.newOptions = options;
			this.origOptions = new PropertyDefOptions(propNode.PropertyDef);

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

		public void Execute() {
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.Children.Count && origParentNode.Children[origParentChildIndex] == propNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(propNode.PropertyDef);

				origParentNode.AddToChildren(propNode);
			}
			else
				newOptions.CopyTo(propNode.PropertyDef);
			propNode.RaiseUIPropsChanged();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = origParentNode.Children.Remove(propNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(propNode.PropertyDef);
				origParentNode.Children.Insert(origParentChildIndex, propNode);
			}
			else
				origOptions.CopyTo(propNode.PropertyDef);
			propNode.RaiseUIPropsChanged();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return propNode; }
		}

		public void Dispose() {
		}
	}
}
