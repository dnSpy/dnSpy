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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;

namespace dnSpy.AsmEditor.Compiler {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		static readonly RoutedCommand EditBodyCommand = new RoutedCommand("EditBodyCommand", typeof(CommandLoader));

		[ImportingConstructor]
		CommandLoader(IWpfCommandManager wpfCommandManager, EditBodyCommand editBodyCmd) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_DOCUMENTVIEWER_UICONTEXT);
			ICommand editBodyCmd2 = editBodyCmd;
			cmds.Add(EditBodyCommand,
				(s, e) => editBodyCmd2.Execute(null),
				(s, e) => e.CanExecute = editBodyCmd2.CanExecute(null),
				ModifierKeys.Control, Key.E);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class EditMethodBodyCodeCommand : IUndoCommand {
		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_FILES_ASMED_ILED, Order = 10)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppWindow appWindow;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IMethodAnnotations> methodAnnotations, IAppWindow appWindow, EditCodeVMCreator editCodeVMCreator) {
				this.undoCommandManager = undoCommandManager;
				this.methodAnnotations = methodAnnotations;
				this.appWindow = appWindow;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(AsmEditorContext context) => editCodeVMCreator.GetIcon();
			public override string GetHeader(AsmEditorContext context) => editCodeVMCreator.GetHeader();
			public override bool IsVisible(AsmEditorContext context) => EditMethodBodyCodeCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(AsmEditorContext context) => EditMethodBodyCodeCommand.Execute(editCodeVMCreator, methodAnnotations, undoCommandManager, appWindow, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 40)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppWindow appWindow;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IMethodAnnotations> methodAnnotations, IAppWindow appWindow, EditCodeVMCreator editCodeVMCreator)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.methodAnnotations = methodAnnotations;
				this.appWindow = appWindow;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(AsmEditorContext context) => editCodeVMCreator.GetIcon();
			public override string GetHeader(AsmEditorContext context) => editCodeVMCreator.GetHeader();
			public override bool IsVisible(AsmEditorContext context) => EditMethodBodyCodeCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(AsmEditorContext context) => EditMethodBodyCodeCommand.Execute(editCodeVMCreator, methodAnnotations, undoCommandManager, appWindow, context.Nodes);
		}

		[ExportMenuItem(Group = MenuConstants.GROUP_CTX_CODE_ASMED_ILED, Order = 10)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppWindow appWindow;
			readonly EditCodeVMCreator editCodeVMCreator;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IMethodAnnotations> methodAnnotations, IAppWindow appWindow, EditCodeVMCreator editCodeVMCreator)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.methodAnnotations = methodAnnotations;
				this.appWindow = appWindow;
				this.editCodeVMCreator = editCodeVMCreator;
			}

			public override ImageReference? GetIcon(CodeContext context) => editCodeVMCreator.GetIcon();
			public override string GetHeader(CodeContext context) => editCodeVMCreator.GetHeader();
			public override bool IsEnabled(CodeContext context) => !EditBodyCommand.IsVisibleInternal(editCodeVMCreator, context.MenuItemContextOrNull) && context.IsDefinition && EditMethodBodyCodeCommand.CanExecute(editCodeVMCreator, context.Nodes);
			public override void Execute(CodeContext context) => EditMethodBodyCodeCommand.Execute(editCodeVMCreator, methodAnnotations, undoCommandManager, appWindow, context.Nodes);
		}

		static bool CanExecute(EditCodeVMCreator editCodeVMCreator, IFileTreeNodeData[] nodes) =>
			editCodeVMCreator.CanCreate && nodes.Length == 1 && nodes[0] is IMethodNode;

		internal static void Execute(EditCodeVMCreator editCodeVMCreator, Lazy<IMethodAnnotations> methodAnnotations, Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes, uint[] offsets = null) {
			if (!CanExecute(editCodeVMCreator, nodes))
				return;

			var methodNode = (IMethodNode)nodes[0];
			var modNode = methodNode.GetModuleNode();
			Debug.Assert(modNode != null);
			if (modNode == null)
				throw new InvalidOperationException();
			var module = modNode.DnSpyFile.ModuleDef;
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var vm = editCodeVMCreator.Create(methodNode.MethodDef);
			var win = new EditCodeDlg();
			win.DataContext = vm;
			win.Owner = appWindow.MainWindow;
			win.Title = string.Format("{0} - {1}", win.Title, methodNode.ToString());

			if (win.ShowDialog() != true) {
				vm.Dispose();
				return;
			}
			Debug.Assert(vm.Result != null);

			undoCommandManager.Value.Add(new EditMethodBodyCodeCommand(methodAnnotations, modNode, vm.Result));
			vm.Dispose();
		}

		readonly AddUpdatedNodesHelper addUpdatedNodesHelper;

		EditMethodBodyCodeCommand(Lazy<IMethodAnnotations> methodAnnotations, IModuleFileNode modNode, ModuleImporter importer) {
			this.addUpdatedNodesHelper = new AddUpdatedNodesHelper(methodAnnotations, modNode, importer);
		}

		public string Description => dnSpy_AsmEditor_Resources.EditMethodCode;
		public void Execute() => addUpdatedNodesHelper.Execute();
		public void Undo() => addUpdatedNodesHelper.Undo();
		public IEnumerable<object> ModifiedObjects => addUpdatedNodesHelper.ModifiedObjects;
	}

	[Export, ExportMenuItem(InputGestureText = "res:ShortCutKeyCtrlE", Group = MenuConstants.GROUP_CTX_CODE_ASMED_ILED, Order = 0)]
	sealed class EditBodyCommand : MenuItemBase, ICommand {
		readonly Lazy<IUndoCommandManager> undoCommandManager;
		readonly Lazy<IMethodAnnotations> methodAnnotations;
		readonly IAppWindow appWindow;
		readonly EditCodeVMCreator editCodeVMCreator;

		[ImportingConstructor]
		EditBodyCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IMethodAnnotations> methodAnnotations, IAppWindow appWindow, EditCodeVMCreator editCodeVMCreator) {
			this.undoCommandManager = undoCommandManager;
			this.methodAnnotations = methodAnnotations;
			this.appWindow = appWindow;
			this.editCodeVMCreator = editCodeVMCreator;
		}

		public override ImageReference? GetIcon(IMenuItemContext context) => editCodeVMCreator.GetIcon();
		public override string GetHeader(IMenuItemContext context) => editCodeVMCreator.GetHeader();
		public override bool IsVisible(IMenuItemContext context) => IsVisibleInternal(editCodeVMCreator, context);

		internal static bool IsVisibleInternal(EditCodeVMCreator editCodeVMCreator, IMenuItemContext context) => IsVisible(editCodeVMCreator, BodyCommandUtils.GetStatements(context));
		static bool IsVisible(EditCodeVMCreator editCodeVMCreator, IList<MethodSourceStatement> list) {
			return editCodeVMCreator.CanCreate &&
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
			var methodNode = appWindow.FileTreeView.FindNode(method);
			if (methodNode == null) {
				MsgBox.Instance.Show(string.Format(dnSpy_AsmEditor_Resources.Error_CouldNotFindMethod, method));
				return;
			}

			EditMethodBodyCodeCommand.Execute(editCodeVMCreator, methodAnnotations, undoCommandManager, appWindow, new IFileTreeNodeData[] { methodNode }, BodyCommandUtils.GetInstructionOffsets(method, list));
		}

		event EventHandler ICommand.CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		IList<MethodSourceStatement> GetStatements() {
			var documentViewer = appWindow.FileTabManager.ActiveTab.TryGetDocumentViewer();
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
