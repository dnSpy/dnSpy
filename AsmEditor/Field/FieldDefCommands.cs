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
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.Contracts.Menus;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.Field {
	[Export(typeof(IPlugin))]
	sealed class AssemblyPlugin : IPlugin {
		void IPlugin.EarlyInit() {
		}

		public void OnLoaded() {
			MainWindow.Instance.TreeView.AddCommandBinding(ApplicationCommands.Delete, new EditMenuHandlerCommandProxy(new DeleteFieldDefCommand.EditMenuCommand()));
			MainWindow.Instance.CodeBindings.Add(EditingCommands.Delete, new CodeContextMenuHandlerCommandProxy(new DeleteFieldDefCommand.CodeCommand()), ModifierKeys.None, Key.Delete);
			Utils.InstallSettingsCommand(new FieldDefSettingsCommand.EditMenuCommand(), new FieldDefSettingsCommand.CodeCommand());
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteFieldDefCommand : IUndoCommand {
		const string CMD_NAME = "Delete Field";
		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_FILES_ASMED_DELETE, Order = 40)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return DeleteFieldDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteFieldDefCommand.Execute(context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteFieldDefCommand.GetHeader(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 40)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return DeleteFieldDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteFieldDefCommand.Execute(context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteFieldDefCommand.GetHeader(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME, Icon = "Delete", InputGestureText = "Del", Group = MenuConstants.GROUP_CTX_CODE_ASMED_DELTE, Order = 40)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					DeleteFieldDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				DeleteFieldDefCommand.Execute(context.Nodes);
			}

			public override string GetHeader(CodeContext context) {
				return DeleteFieldDefCommand.GetHeader(context.Nodes);
			}
		}

		static string GetHeader(ILSpyTreeNode[] nodes) {
			if (nodes.Length == 1)
				return string.Format("Delete {0}", UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format("Delete {0} fields", nodes.Length);
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is FieldTreeNode);
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			if (!Method.DeleteMethodDefCommand.AskDeleteDef("field"))
				return;

			var fieldNodes = nodes.Select(a => (FieldTreeNode)a).ToArray();
			UndoCommandManager.Instance.Add(new DeleteFieldDefCommand(fieldNodes));
		}

		public struct DeleteModelNodes {
			ModelInfo[] infos;

			struct ModelInfo {
				public readonly TypeDef OwnerType;
				public readonly int FieldIndex;

				public ModelInfo(FieldDef field) {
					this.OwnerType = field.DeclaringType;
					this.FieldIndex = this.OwnerType.Fields.IndexOf(field);
					Debug.Assert(this.FieldIndex >= 0);
				}
			}

			public void Delete(FieldTreeNode[] nodes) {
				Debug.Assert(infos == null);
				if (infos != null)
					throw new InvalidOperationException();

				infos = new ModelInfo[nodes.Length];

				for (int i = 0; i < infos.Length; i++) {
					var node = nodes[i];

					var info = new ModelInfo(node.FieldDef);
					infos[i] = info;
					info.OwnerType.Fields.RemoveAt(info.FieldIndex);
				}
			}

			public void Restore(FieldTreeNode[] nodes) {
				Debug.Assert(infos != null);
				if (infos == null)
					throw new InvalidOperationException();
				Debug.Assert(infos.Length == nodes.Length);
				if (infos.Length != nodes.Length)
					throw new InvalidOperationException();

				for (int i = infos.Length - 1; i >= 0; i--) {
					var node = nodes[i];
					var info = infos[i];
					info.OwnerType.Fields.Insert(info.FieldIndex, node.FieldDef);
				}

				infos = null;
			}
		}

		DeletableNodes<FieldTreeNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteFieldDefCommand(FieldTreeNode[] fieldNodes) {
			this.nodes = new DeletableNodes<FieldTreeNode>(fieldNodes);
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
	sealed class CreateFieldDefCommand : IUndoCommand {
		const string CMD_NAME = "Create Field";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewField", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 70)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateFieldDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateFieldDefCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "NewField", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 70)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return CreateFieldDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateFieldDefCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "NewField", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 70)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					context.Nodes.Length == 1 &&
					context.Nodes[0] is TypeTreeNode;
			}

			public override void Execute(CodeContext context) {
				CreateFieldDefCommand.Execute(context.Nodes);
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

			FieldDefOptions options;
			var type = typeNode.TypeDef;
			if (type.IsEnum) {
				var ts = type.GetEnumUnderlyingType();
				if (ts != null) {
					options = FieldDefOptions.Create("MyField", new FieldSig(new ValueTypeSig(typeNode.TypeDef)));
					options.Constant = module.UpdateRowId(new ConstantUser(ModelUtils.GetDefaultValue(ts), ts.RemovePinnedAndModifiers().GetElementType()));
					options.Attributes |= FieldAttributes.Literal | FieldAttributes.Static | FieldAttributes.HasDefault;
				}
				else {
					options = FieldDefOptions.Create("value__", new FieldSig(module.CorLibTypes.Int32));
					options.Attributes |= FieldAttributes.SpecialName | FieldAttributes.RTSpecialName;
				}
			}
			else if (type.IsAbstract && type.IsSealed) {
				options = FieldDefOptions.Create("MyField", new FieldSig(module.CorLibTypes.Int32));
				options.Attributes |= FieldAttributes.Static;
			}
			else
				options = FieldDefOptions.Create("MyField", new FieldSig(module.CorLibTypes.Int32));

			var data = new FieldOptionsVM(options, module, MainWindow.Instance.CurrentLanguage, type);
			var win = new FieldOptionsDlg();
			win.Title = CMD_NAME;
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateFieldDefCommand(typeNode, data.CreateFieldDefOptions());
			UndoCommandManager.Instance.Add(cmd);
			MainWindow.Instance.JumpToReference(cmd.fieldNode);
		}

		readonly TypeTreeNode ownerNode;
		readonly FieldTreeNode fieldNode;

		CreateFieldDefCommand(TypeTreeNode ownerNode, FieldDefOptions options) {
			this.ownerNode = ownerNode;
			this.fieldNode = new FieldTreeNode(options.CreateFieldDef(ownerNode.TypeDef.Module));
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			ownerNode.EnsureChildrenFiltered();
			ownerNode.TypeDef.Fields.Add(fieldNode.FieldDef);
			ownerNode.AddToChildren(fieldNode);
		}

		public void Undo() {
			bool b = ownerNode.Children.Remove(fieldNode) &&
					ownerNode.TypeDef.Fields.Remove(fieldNode.FieldDef);
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

	struct MemberRefInfo {
		public readonly MemberRef MemberRef;
		public readonly UTF8String OrigName;

		public MemberRefInfo(MemberRef mr) {
			this.MemberRef = mr;
			this.OrigName = mr.Name;
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class FieldDefSettingsCommand : IUndoCommand {
		const string CMD_NAME = "Edit Field";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 50)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return FieldDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				FieldDefSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 50)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return FieldDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				FieldDefSettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "Settings", InputGestureText = "Alt+Enter", Group = MenuConstants.GROUP_CTX_CODE_ASMED_SETTINGS, Order = 50)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return FieldDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				FieldDefSettingsCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is FieldTreeNode;
		}

		static void Execute(ILSpyTreeNode[] nodes) {
			if (!CanExecute(nodes))
				return;

			var fieldNode = (FieldTreeNode)nodes[0];

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new FieldOptionsVM(new FieldDefOptions(fieldNode.FieldDef), module, MainWindow.Instance.CurrentLanguage, fieldNode.FieldDef.DeclaringType);
			var win = new FieldOptionsDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new FieldDefSettingsCommand(fieldNode, data.CreateFieldDefOptions()));
		}

		readonly FieldTreeNode fieldNode;
		readonly FieldDefOptions newOptions;
		readonly FieldDefOptions origOptions;
		readonly ILSpyTreeNode origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;
		readonly MemberRefInfo[] memberRefInfos;

		FieldDefSettingsCommand(FieldTreeNode fieldNode, FieldDefOptions options) {
			this.fieldNode = fieldNode;
			this.newOptions = options;
			this.origOptions = new FieldDefOptions(fieldNode.FieldDef);

			this.origParentNode = (ILSpyTreeNode)fieldNode.Parent;
			this.origParentChildIndex = this.origParentNode.Children.IndexOf(fieldNode);
			Debug.Assert(this.origParentChildIndex >= 0);
			if (this.origParentChildIndex < 0)
				throw new InvalidOperationException();

			this.nameChanged = origOptions.Name != newOptions.Name;
			if (this.nameChanged)
				this.memberRefInfos = RefFinder.FindMemberRefsToThisModule(ILSpyTreeNode.GetModule(fieldNode)).Where(a => RefFinder.FieldEqualityComparerInstance.Equals(a, fieldNode.FieldDef)).Select(a => new MemberRefInfo(a)).ToArray();
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.Children.Count && origParentNode.Children[origParentChildIndex] == fieldNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(fieldNode.FieldDef);

				origParentNode.AddToChildren(fieldNode);
			}
			else
				newOptions.CopyTo(fieldNode.FieldDef);
			if (memberRefInfos != null) {
				foreach (var info in memberRefInfos)
					info.MemberRef.Name = fieldNode.FieldDef.Name;
			}
			fieldNode.RaiseUIPropsChanged();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = origParentNode.Children.Remove(fieldNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(fieldNode.FieldDef);
				origParentNode.Children.Insert(origParentChildIndex, fieldNode);
			}
			else
				origOptions.CopyTo(fieldNode.FieldDef);
			if (memberRefInfos != null) {
				foreach (var info in memberRefInfos)
					info.MemberRef.Name = info.OrigName;
			}
			fieldNode.RaiseUIPropsChanged();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return fieldNode; }
		}

		public void Dispose() {
		}
	}
}
