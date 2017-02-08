/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.AsmEditor.MethodBody;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.App;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.AsmEditor.Compiler {
	[DebuggerDisplay("{Description}")]
	sealed class EditClassCommand : EditCodeCommandBase {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_ILED, Order = 12)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, EditCodeVMCreator editCodeVMCreator) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(AsmEditorContext context) => editCodeVMCreator.GetIcon(CompilationKind.EditClass);
			public override string GetHeader(AsmEditorContext context) => editCodeVMCreator.GetHeader(CompilationKind.EditClass);
			public override bool IsVisible(AsmEditorContext context) => EditClassCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(AsmEditorContext context) => EditClassCommand.Execute(editCodeVMCreator, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 42)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, EditCodeVMCreator editCodeVMCreator)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(AsmEditorContext context) => editCodeVMCreator.GetIcon(CompilationKind.EditClass);
			public override string GetHeader(AsmEditorContext context) => editCodeVMCreator.GetHeader(CompilationKind.EditClass);
			public override bool IsVisible(AsmEditorContext context) => EditClassCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(AsmEditorContext context) => EditClassCommand.Execute(editCodeVMCreator, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_ILED, Order = 12)]
		sealed class CodeCommand : NodesCodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, EditCodeVMCreator editCodeVMCreator)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(CodeContext context) => editCodeVMCreator.GetIcon(CompilationKind.EditClass);
			public override string GetHeader(CodeContext context) => editCodeVMCreator.GetHeader(CompilationKind.EditClass);
			public override bool IsEnabled(CodeContext context) => !EditClass2Command.IsVisibleInternal(editCodeVMCreator, context.MenuItemContextOrNull) && EditClassCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(CodeContext context) => EditClassCommand.Execute(editCodeVMCreator, addUpdatedNodesHelperProvider, undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(EditCodeVMCreator editCodeVMCreator, DocumentTreeNodeData[] nodes) =>
			editCodeVMCreator.CanCreate(CompilationKind.EditClass) &&
			nodes.Length == 1 &&
			(nodes[0] as IMDTokenNode)?.Reference is IMemberDef;

		internal static void Execute(EditCodeVMCreator editCodeVMCreator, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes, IList<MethodSourceStatement> statements = null) {
			if (!CanExecute(editCodeVMCreator, nodes))
				return;

			var node = nodes[0];
			var tokNode = node as IMDTokenNode;
			var defToEdit = tokNode?.Reference as IMemberDef;
			if (defToEdit == null)
				return;

			TypeNode typeNode = null;
			for (TreeNodeData n = node; n != null;) {
				var t = n as TypeNode;
				if (t != null)
					typeNode = t;
				n = n.TreeNode.Parent?.Data;
			}
			if (typeNode == null)
				return;

			var asmNode = nodes[0] as AssemblyDocumentNode;
			ModuleDocumentNode modNode;
			if (asmNode != null) {
				asmNode.TreeNode.EnsureChildrenLoaded();
				modNode = asmNode.TreeNode.DataChildren.FirstOrDefault() as ModuleDocumentNode;
			}
			else
				modNode = nodes[0].GetModuleNode();
			Debug.Assert(modNode != null);
			if (modNode == null)
				return;

			var vm = editCodeVMCreator.CreateEditClass(defToEdit, statements ?? Array.Empty<MethodSourceStatement>());
			var win = new EditCodeDlg();
			win.DataContext = vm;
			win.Owner = appService.MainWindow;
			win.Title = string.Format("{0} - {1}", dnSpy_AsmEditor_Resources.EditCodeEditClass, typeNode.ToString());

			if (win.ShowDialog() != true) {
				vm.Dispose();
				return;
			}
			Debug.Assert(vm.Result != null);

			undoCommandService.Value.Add(new EditClassCommand(addUpdatedNodesHelperProvider, modNode, vm.Result));
			vm.Dispose();
		}

		EditClassCommand(Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, ModuleDocumentNode modNode, ModuleImporter importer)
			: base(addUpdatedNodesHelperProvider, modNode, importer) {
		}

		public override string Description => dnSpy_AsmEditor_Resources.EditCodeEditClass;
	}

	[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_ILED, Order = 2)]
	sealed class EditClass2Command : MenuItemBase {
		readonly Lazy<IUndoCommandService> undoCommandService;
		readonly Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider;
		readonly IAppService appService;
		readonly EditCodeVMCreator editCodeVMCreator;

		[ImportingConstructor]
		EditClass2Command(Lazy<IUndoCommandService> undoCommandService, Lazy<IAddUpdatedNodesHelperProvider> addUpdatedNodesHelperProvider, IAppService appService, EditCodeVMCreator editCodeVMCreator) {
			this.undoCommandService = undoCommandService;
			this.addUpdatedNodesHelperProvider = addUpdatedNodesHelperProvider;
			this.appService = appService;
			this.editCodeVMCreator = editCodeVMCreator;
		}

		public override ImageReference? GetIcon(IMenuItemContext context) => editCodeVMCreator.GetIcon(CompilationKind.EditClass);
		public override string GetHeader(IMenuItemContext context) => editCodeVMCreator.GetHeader(CompilationKind.EditClass);
		public override bool IsVisible(IMenuItemContext context) => IsVisibleInternal(editCodeVMCreator, context);

		internal static bool IsVisibleInternal(EditCodeVMCreator editCodeVMCreator, IMenuItemContext context) => IsVisible(editCodeVMCreator, BodyCommandUtils.GetStatements(context));
		static bool IsVisible(EditCodeVMCreator editCodeVMCreator, IList<MethodSourceStatement> list) {
			return editCodeVMCreator.CanCreate(CompilationKind.EditClass) &&
				list != null &&
				list.Count != 0 &&
				list[0].Method.Body != null &&
				list[0].Method.Body.Instructions.Count > 0;
		}

		public override void Execute(IMenuItemContext context) => Execute(BodyCommandUtils.GetStatements(context));

		void Execute(IList<MethodSourceStatement> list) {
			if (list == null)
				return;

			var method = list[0].Method;
			var methodNode = appService.DocumentTreeView.FindNode(method);
			if (methodNode == null) {
				MsgBox.Instance.Show(string.Format(dnSpy_AsmEditor_Resources.Error_CouldNotFindMethod, method));
				return;
			}

			EditClassCommand.Execute(editCodeVMCreator, addUpdatedNodesHelperProvider, undoCommandService, appService, new DocumentTreeNodeData[] { methodNode }, list);
		}

		IList<MethodSourceStatement> GetStatements() {
			var documentViewer = appService.DocumentTabService.ActiveTab.TryGetDocumentViewer();
			if (documentViewer == null)
				return null;
			if (!documentViewer.UIObject.IsKeyboardFocusWithin)
				return null;

			return BodyCommandUtils.GetStatements(documentViewer, documentViewer.Caret.Position.BufferPosition.Position);
		}
	}
}
