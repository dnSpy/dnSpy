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
using dnlib.DotNet;
using dnSpy.AsmEditor.DnlibDialogs;
using dnSpy.Contracts.Menus;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Plugin;
using dnSpy.Shared.UI.MVVM;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.App;
using dnSpy.AsmEditor.Properties;

namespace dnSpy.AsmEditor.Field {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		[ImportingConstructor]
		CommandLoader(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, DeleteFieldDefCommand.EditMenuCommand removeCmd, DeleteFieldDefCommand.CodeCommand removeCmd2, FieldDefSettingsCommand.EditMenuCommand settingsCmd, FieldDefSettingsCommand.CodeCommand settingsCmd2) {
			wpfCommandManager.AddRemoveCommand(removeCmd);
			wpfCommandManager.AddRemoveCommand(removeCmd2, fileTabManager);
			wpfCommandManager.AddSettingsCommand(fileTabManager, settingsCmd, settingsCmd2);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class DeleteFieldDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:DeleteFieldCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_FILES_ASMED_DELETE, Order = 40)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return DeleteFieldDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteFieldDefCommand.Execute(undoCommandManager, context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteFieldDefCommand.GetHeader(context.Nodes);
			}
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:DeleteFieldCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_DELETE, Order = 40)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeView fileTreeView)
				: base(fileTreeView) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return DeleteFieldDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				DeleteFieldDefCommand.Execute(undoCommandManager, context.Nodes);
			}

			public override string GetHeader(AsmEditorContext context) {
				return DeleteFieldDefCommand.GetHeader(context.Nodes);
			}
		}

		[Export, ExportMenuItem(Header = "res:DeleteFieldCommand", Icon = "Delete", InputGestureText = "res:DeleteCommandKey", Group = MenuConstants.GROUP_CTX_CODE_ASMED_DELTE, Order = 40)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeView fileTreeView)
				: base(fileTreeView) {
				this.undoCommandManager = undoCommandManager;
			}

			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					DeleteFieldDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				DeleteFieldDefCommand.Execute(undoCommandManager, context.Nodes);
			}

			public override string GetHeader(CodeContext context) {
				return DeleteFieldDefCommand.GetHeader(context.Nodes);
			}
		}

		static string GetHeader(IFileTreeNodeData[] nodes) {
			if (nodes.Length == 1)
				return string.Format(dnSpy_AsmEditor_Resources.DeleteX, UIUtils.EscapeMenuItemHeader(nodes[0].ToString()));
			return string.Format(dnSpy_AsmEditor_Resources.DeleteFieldsCommand, nodes.Length);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) {
			return nodes.Length > 0 &&
				nodes.All(n => n is IFieldNode);
		}

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			if (!Method.DeleteMethodDefCommand.AskDeleteDef(dnSpy_AsmEditor_Resources.AskDeleteField))
				return;

			var fieldNodes = nodes.Cast<IFieldNode>().ToArray();
			undoCommandManager.Value.Add(new DeleteFieldDefCommand(fieldNodes));
		}

		struct DeleteModelNodes {
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

			public void Delete(IFieldNode[] nodes) {
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

			public void Restore(IFieldNode[] nodes) {
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

		DeletableNodes<IFieldNode> nodes;
		DeleteModelNodes modelNodes;

		DeleteFieldDefCommand(IFieldNode[] fieldNodes) {
			this.nodes = new DeletableNodes<IFieldNode>(fieldNodes);
		}

		public string Description {
			get { return dnSpy_AsmEditor_Resources.DeleteFieldCommand; }
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
	}

	[DebuggerDisplay("{Description}")]
	sealed class CreateFieldDefCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:CreateFieldCommand", Icon = "NewField", Group = MenuConstants.GROUP_CTX_FILES_ASMED_NEW, Order = 70)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return CreateFieldDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateFieldDefCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:CreateFieldCommand", Icon = "NewField", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_NEW, Order = 70)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return CreateFieldDefCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				CreateFieldDefCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		[ExportMenuItem(Header = "res:CreateFieldCommand", Icon = "NewField", Group = MenuConstants.GROUP_CTX_CODE_ASMED_NEW, Order = 70)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					context.Nodes.Length == 1 &&
					context.Nodes[0] is ITypeNode;
			}

			public override void Execute(CodeContext context) {
				CreateFieldDefCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) {
			return nodes.Length == 1 &&
				(nodes[0] is ITypeNode || (nodes[0].TreeNode.Parent != null && nodes[0].TreeNode.Parent.Data is ITypeNode));
		}

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var ownerNode = nodes[0];
			if (!(ownerNode is ITypeNode))
				ownerNode = (IFileTreeNodeData)ownerNode.TreeNode.Parent.Data;
			var typeNode = ownerNode as ITypeNode;
			Debug.Assert(typeNode != null);
			if (typeNode == null)
				throw new InvalidOperationException();

			var module = typeNode.GetModule();
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

			var data = new FieldOptionsVM(options, module, appWindow.LanguageManager, type);
			var win = new FieldOptionsDlg();
			win.Title = dnSpy_AsmEditor_Resources.CreateFieldCommand2;
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			var cmd = new CreateFieldDefCommand(typeNode, data.CreateFieldDefOptions());
			undoCommandManager.Value.Add(cmd);
			appWindow.FileTabManager.FollowReference(cmd.fieldNode);
		}

		readonly ITypeNode ownerNode;
		readonly IFieldNode fieldNode;

		CreateFieldDefCommand(ITypeNode ownerNode, FieldDefOptions options) {
			this.ownerNode = ownerNode;
			this.fieldNode = ownerNode.Create(options.CreateFieldDef(ownerNode.TypeDef.Module));
		}

		public string Description {
			get { return dnSpy_AsmEditor_Resources.CreateFieldCommand2; }
		}

		public void Execute() {
			ownerNode.TreeNode.EnsureChildrenLoaded();
			ownerNode.TypeDef.Fields.Add(fieldNode.FieldDef);
			ownerNode.TreeNode.AddChild(fieldNode.TreeNode);
		}

		public void Undo() {
			bool b = ownerNode.TreeNode.Children.Remove(fieldNode.TreeNode) &&
					ownerNode.TypeDef.Fields.Remove(fieldNode.FieldDef);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return ownerNode; }
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
		[ExportMenuItem(Header = "res:EditFieldCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_FILES_ASMED_SETTINGS, Order = 50)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return FieldDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				FieldDefSettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		[Export, ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditFieldCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 50)]
		internal sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) {
				return FieldDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				FieldDefSettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		[Export, ExportMenuItem(Header = "res:EditFieldCommand", Icon = "Settings", InputGestureText = "res:ShortcutKeyAltEnter", Group = MenuConstants.GROUP_CTX_CODE_ASMED_SETTINGS, Order = 50)]
		internal sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.appWindow = appWindow;
			}

			public override bool IsEnabled(CodeContext context) {
				return FieldDefSettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				FieldDefSettingsCommand.Execute(undoCommandManager, appWindow, context.Nodes);
			}
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is IFieldNode;
		}

		static void Execute(Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes) {
			if (!CanExecute(nodes))
				return;

			var fieldNode = (IFieldNode)nodes[0];

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new FieldOptionsVM(new FieldDefOptions(fieldNode.FieldDef), module, appWindow.LanguageManager, fieldNode.FieldDef.DeclaringType);
			var win = new FieldOptionsDlg();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			if (win.ShowDialog() != true)
				return;

			undoCommandManager.Value.Add(new FieldDefSettingsCommand(fieldNode, data.CreateFieldDefOptions()));
		}

		readonly IFieldNode fieldNode;
		readonly FieldDefOptions newOptions;
		readonly FieldDefOptions origOptions;
		readonly IFileTreeNodeData origParentNode;
		readonly int origParentChildIndex;
		readonly bool nameChanged;
		readonly MemberRefInfo[] memberRefInfos;

		FieldDefSettingsCommand(IFieldNode fieldNode, FieldDefOptions options) {
			this.fieldNode = fieldNode;
			this.newOptions = options;
			this.origOptions = new FieldDefOptions(fieldNode.FieldDef);

			this.origParentNode = (IFileTreeNodeData)fieldNode.TreeNode.Parent.Data;
			this.origParentChildIndex = this.origParentNode.TreeNode.Children.IndexOf(fieldNode.TreeNode);
			Debug.Assert(this.origParentChildIndex >= 0);
			if (this.origParentChildIndex < 0)
				throw new InvalidOperationException();

			this.nameChanged = origOptions.Name != newOptions.Name;
			if (this.nameChanged)
				this.memberRefInfos = RefFinder.FindMemberRefsToThisModule(fieldNode.GetModule()).Where(a => RefFinder.FieldEqualityComparerInstance.Equals(a, fieldNode.FieldDef)).Select(a => new MemberRefInfo(a)).ToArray();
		}

		public string Description {
			get { return dnSpy_AsmEditor_Resources.EditFieldCommand2; }
		}

		public void Execute() {
			if (nameChanged) {
				bool b = origParentChildIndex < origParentNode.TreeNode.Children.Count && origParentNode.TreeNode.Children[origParentChildIndex] == fieldNode.TreeNode;
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();
				origParentNode.TreeNode.Children.RemoveAt(origParentChildIndex);
				newOptions.CopyTo(fieldNode.FieldDef);

				origParentNode.TreeNode.AddChild(fieldNode.TreeNode);
			}
			else
				newOptions.CopyTo(fieldNode.FieldDef);
			if (memberRefInfos != null) {
				foreach (var info in memberRefInfos)
					info.MemberRef.Name = fieldNode.FieldDef.Name;
			}
			fieldNode.TreeNode.RefreshUI();
		}

		public void Undo() {
			if (nameChanged) {
				bool b = origParentNode.TreeNode.Children.Remove(fieldNode.TreeNode);
				Debug.Assert(b);
				if (!b)
					throw new InvalidOperationException();

				origOptions.CopyTo(fieldNode.FieldDef);
				origParentNode.TreeNode.Children.Insert(origParentChildIndex, fieldNode.TreeNode);
			}
			else
				origOptions.CopyTo(fieldNode.FieldDef);
			if (memberRefInfos != null) {
				foreach (var info in memberRefInfos)
					info.MemberRef.Name = info.OrigName;
			}
			fieldNode.TreeNode.RefreshUI();
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return fieldNode; }
		}
	}
}
