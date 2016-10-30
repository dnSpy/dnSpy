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
using System.Windows.Input;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.MethodBody;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.App;
using dnSpy.Contracts.AsmEditor.Compiler;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;

namespace dnSpy.AsmEditor.Compiler {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		static readonly RoutedCommand EditBodyCommand = new RoutedCommand("EditBodyCommand", typeof(CommandLoader));

		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, EditBodyCommand editBodyCmd) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENTVIEWER_UICONTEXT);
			ICommand editBodyCmd2 = editBodyCmd;
			cmds.Add(EditBodyCommand,
				(s, e) => editBodyCmd2.Execute(null),
				(s, e) => e.CanExecute = editBodyCmd2.CanExecute(null),
				ModifierKeys.Control, Key.E);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class EditMethodBodyCodeCommand : IUndoCommand {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_ILED, Order = 10)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IMethodAnnotations> methodAnnotations, IAppService appService, EditCodeVMCreator editCodeVMCreator) {
				this.undoCommandService = undoCommandService;
				this.methodAnnotations = methodAnnotations;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(AsmEditorContext context) => editCodeVMCreator.GetIcon(CompilationKind.Method);
			public override string GetHeader(AsmEditorContext context) => editCodeVMCreator.GetHeader(CompilationKind.Method);
			public override bool IsVisible(AsmEditorContext context) => EditMethodBodyCodeCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(AsmEditorContext context) => EditMethodBodyCodeCommand.Execute(editCodeVMCreator, methodAnnotations, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 40)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IMethodAnnotations> methodAnnotations, IAppService appService, EditCodeVMCreator editCodeVMCreator)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.methodAnnotations = methodAnnotations;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(AsmEditorContext context) => editCodeVMCreator.GetIcon(CompilationKind.Method);
			public override string GetHeader(AsmEditorContext context) => editCodeVMCreator.GetHeader(CompilationKind.Method);
			public override bool IsVisible(AsmEditorContext context) => EditMethodBodyCodeCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(AsmEditorContext context) => EditMethodBodyCodeCommand.Execute(editCodeVMCreator, methodAnnotations, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_ILED, Order = 10)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppService appService;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IMethodAnnotations> methodAnnotations, IAppService appService, EditCodeVMCreator editCodeVMCreator)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.methodAnnotations = methodAnnotations;
				this.appService = appService;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(CodeContext context) => editCodeVMCreator.GetIcon(CompilationKind.Method);
			public override string GetHeader(CodeContext context) => editCodeVMCreator.GetHeader(CompilationKind.Method);
			public override bool IsEnabled(CodeContext context) => !EditBodyCommand.IsVisibleInternal(editCodeVMCreator, context.MenuItemContextOrNull) && context.IsDefinition && EditMethodBodyCodeCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(CodeContext context) => EditMethodBodyCodeCommand.Execute(editCodeVMCreator, methodAnnotations, undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(EditCodeVMCreator editCodeVMCreator, DocumentTreeNodeData[] nodes) =>
			editCodeVMCreator.CanCreate(CompilationKind.Method) && nodes.Length == 1 && nodes[0] is MethodNode;

		internal static void Execute(EditCodeVMCreator editCodeVMCreator, Lazy<IMethodAnnotations> methodAnnotations, Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes, IList<MethodSourceStatement> statements = null) {
			if (!CanExecute(editCodeVMCreator, nodes))
				return;

			var methodNode = (MethodNode)nodes[0];
			var modNode = methodNode.GetModuleNode();
			Debug.Assert(modNode != null);
			if (modNode == null)
				throw new InvalidOperationException();
			var module = modNode.Document.ModuleDef;
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var vm = editCodeVMCreator.CreateEditMethodCode(methodNode.MethodDef, statements ?? Array.Empty<MethodSourceStatement>());
			var win = new EditCodeDlg();
			win.DataContext = vm;
			win.Owner = appService.MainWindow;
			win.Title = string.Format("{0} - {1}", win.Title, methodNode.ToString());

			if (win.ShowDialog() != true) {
				vm.Dispose();
				return;
			}
			Debug.Assert(vm.Result != null);

			undoCommandService.Value.Add(new EditMethodBodyCodeCommand(methodAnnotations, modNode, vm.Result));
			vm.Dispose();
		}

		readonly AddUpdatedNodesHelper addUpdatedNodesHelper;

		EditMethodBodyCodeCommand(Lazy<IMethodAnnotations> methodAnnotations, ModuleDocumentNode modNode, ModuleImporter importer) {
			this.addUpdatedNodesHelper = new AddUpdatedNodesHelper(methodAnnotations, modNode, importer);
		}

		public string Description => dnSpy_AsmEditor_Resources.EditMethodCode;
		public void Execute() => addUpdatedNodesHelper.Execute();
		public void Undo() => addUpdatedNodesHelper.Undo();
		public IEnumerable<object> ModifiedObjects => addUpdatedNodesHelper.ModifiedObjects;
	}

	[Export, ExportMenuItem(InputGestureText = "res:ShortCutKeyCtrlE", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_ILED, Order = 0)]
	sealed class EditBodyCommand : MenuItemBase, ICommand {
		readonly Lazy<IUndoCommandService> undoCommandService;
		readonly Lazy<IMethodAnnotations> methodAnnotations;
		readonly IAppService appService;
		readonly EditCodeVMCreator editCodeVMCreator;

		[ImportingConstructor]
		EditBodyCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IMethodAnnotations> methodAnnotations, IAppService appService, EditCodeVMCreator editCodeVMCreator) {
			this.undoCommandService = undoCommandService;
			this.methodAnnotations = methodAnnotations;
			this.appService = appService;
			this.editCodeVMCreator = editCodeVMCreator;
		}

		public override ImageReference? GetIcon(IMenuItemContext context) => editCodeVMCreator.GetIcon(CompilationKind.Method);
		public override string GetHeader(IMenuItemContext context) => editCodeVMCreator.GetHeader(CompilationKind.Method);
		public override bool IsVisible(IMenuItemContext context) => IsVisibleInternal(editCodeVMCreator, context);

		internal static bool IsVisibleInternal(EditCodeVMCreator editCodeVMCreator, IMenuItemContext context) => IsVisible(editCodeVMCreator, BodyCommandUtils.GetStatements(context));
		static bool IsVisible(EditCodeVMCreator editCodeVMCreator, IList<MethodSourceStatement> list) {
			return editCodeVMCreator.CanCreate(CompilationKind.Method) &&
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

			EditMethodBodyCodeCommand.Execute(editCodeVMCreator, methodAnnotations, undoCommandService, appService, new DocumentTreeNodeData[] { methodNode }, list);
		}

		event EventHandler ICommand.CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		IList<MethodSourceStatement> GetStatements() {
			var documentViewer = appService.DocumentTabService.ActiveTab.TryGetDocumentViewer();
			if (documentViewer == null)
				return null;
			if (!documentViewer.UIObject.IsKeyboardFocusWithin)
				return null;

			return BodyCommandUtils.GetStatements(documentViewer, documentViewer.Caret.Position.BufferPosition.Position);
		}

		void ICommand.Execute(object parameter) => Execute(GetStatements());
		bool ICommand.CanExecute(object parameter) => IsVisible(editCodeVMCreator, GetStatements());
	}
}
