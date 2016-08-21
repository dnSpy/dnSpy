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
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;

namespace dnSpy.AsmEditor.MethodBody {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		static readonly RoutedCommand EditILInstructionsCommand = new RoutedCommand("EditILInstructionsCommand", typeof(CommandLoader));

		[ImportingConstructor]
		CommandLoader(IWpfCommandManager wpfCommandManager, EditILInstructionsCommand editILCmd) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_DOCUMENTVIEWER_UICONTEXT);
			ICommand editILCmd2 = editILCmd;
			cmds.Add(EditILInstructionsCommand,
				(s, e) => editILCmd2.Execute(null),
				(s, e) => e.CanExecute = editILCmd2.CanExecute(null),
				ModifierKeys.Control | ModifierKeys.Shift, Key.E);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class EditMethodBodyILCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditMethodBodyCommand", Icon = "ILEditor", Group = MenuConstants.GROUP_CTX_FILES_ASMED_ILED, Order = 20)]
		sealed class FilesCommand : FilesContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			FilesCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IMethodAnnotations> methodAnnotations, IAppWindow appWindow) {
				this.undoCommandManager = undoCommandManager;
				this.methodAnnotations = methodAnnotations;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => EditMethodBodyILCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => EditMethodBodyILCommand.Execute(methodAnnotations, undoCommandManager, appWindow, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditMethodBodyCommand", Icon = "ILEditor", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 50)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IMethodAnnotations> methodAnnotations, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.methodAnnotations = methodAnnotations;
				this.appWindow = appWindow;
			}

			public override bool IsVisible(AsmEditorContext context) => EditMethodBodyILCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => EditMethodBodyILCommand.Execute(methodAnnotations, undoCommandManager, appWindow, context.Nodes);
		}

		[ExportMenuItem(Header = "res:EditMethodBodyCommand", Icon = "ILEditor", Group = MenuConstants.GROUP_CTX_CODE_ASMED_ILED, Order = 20)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandManager> undoCommandManager;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppWindow appWindow;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IMethodAnnotations> methodAnnotations, IAppWindow appWindow)
				: base(appWindow.FileTreeView) {
				this.undoCommandManager = undoCommandManager;
				this.methodAnnotations = methodAnnotations;
				this.appWindow = appWindow;
			}

			public override bool IsEnabled(CodeContext context) => !EditILInstructionsCommand.IsVisibleInternal(context.MenuItemContextOrNull) && context.IsDefinition && EditMethodBodyILCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => EditMethodBodyILCommand.Execute(methodAnnotations, undoCommandManager, appWindow, context.Nodes);
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) => nodes.Length == 1 && nodes[0] is IMethodNode;

		internal static void Execute(Lazy<IMethodAnnotations> methodAnnotations, Lazy<IUndoCommandManager> undoCommandManager, IAppWindow appWindow, IFileTreeNodeData[] nodes, uint[] offsets = null) {
			if (!CanExecute(nodes))
				return;

			var methodNode = (IMethodNode)nodes[0];

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new MethodBodyVM(new MethodBodyOptions(methodNode.MethodDef), module, appWindow.LanguageManager, methodNode.MethodDef.DeclaringType, methodNode.MethodDef);
			var win = new MethodBodyDlg();
			win.DataContext = data;
			win.Owner = appWindow.MainWindow;
			win.Title = string.Format("{0} - {1}", win.Title, methodNode.ToString());

			if (data.IsCilBody && offsets != null)
				data.CilBodyVM.Select(offsets);

			if (win.ShowDialog() != true)
				return;

			undoCommandManager.Value.Add(new EditMethodBodyILCommand(methodAnnotations.Value, methodNode, data.CreateMethodBodyOptions()));
		}

		readonly IMethodAnnotations methodAnnotations;
		readonly IMethodNode methodNode;
		readonly MethodBodyOptions newOptions;
		readonly dnlib.DotNet.Emit.MethodBody origMethodBody;
		bool isBodyModified;

		EditMethodBodyILCommand(IMethodAnnotations methodAnnotations, IMethodNode methodNode, MethodBodyOptions options) {
			this.methodAnnotations = methodAnnotations;
			this.methodNode = methodNode;
			this.newOptions = options;
			this.origMethodBody = methodNode.MethodDef.MethodBody;
		}

		public string Description => dnSpy_AsmEditor_Resources.EditMethodBodyCommand2;

		public void Execute() {
			isBodyModified = methodAnnotations.IsBodyModified(methodNode.MethodDef);
			methodAnnotations.SetBodyModified(methodNode.MethodDef, true);
			newOptions.CopyTo(methodNode.MethodDef);
		}

		public void Undo() {
			methodNode.MethodDef.MethodBody = origMethodBody;
			methodAnnotations.SetBodyModified(methodNode.MethodDef, isBodyModified);
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return methodNode; }
		}
	}

	[Export, ExportMenuItem(Header = "res:EditILInstructionsCommand", Icon = "ILEditor", InputGestureText = "res:ShortCutKeyCtrlShiftE", Group = MenuConstants.GROUP_CTX_CODE_ASMED_ILED, Order = 10)]
	sealed class EditILInstructionsCommand : MenuItemBase, ICommand {
		readonly Lazy<IUndoCommandManager> undoCommandManager;
		readonly Lazy<IMethodAnnotations> methodAnnotations;
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		EditILInstructionsCommand(Lazy<IUndoCommandManager> undoCommandManager, Lazy<IMethodAnnotations> methodAnnotations, IAppWindow appWindow) {
			this.undoCommandManager = undoCommandManager;
			this.methodAnnotations = methodAnnotations;
			this.appWindow = appWindow;
		}

		public override bool IsVisible(IMenuItemContext context) => IsVisibleInternal(context);

		internal static bool IsVisibleInternal(IMenuItemContext context) => IsVisible(BodyCommandUtils.GetStatements(context));
		static bool IsVisible(IList<MethodSourceStatement> list) {
			return list != null &&
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

			EditMethodBodyILCommand.Execute(methodAnnotations, undoCommandManager, appWindow, new IFileTreeNodeData[] { methodNode }, BodyCommandUtils.GetInstructionOffsets(method, list));
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

			return BodyCommandUtils.GetStatements(documentViewer, documentViewer.Caret.Position.BufferPosition);
		}

		void ICommand.Execute(object parameter) => Execute(GetStatements());
		bool ICommand.CanExecute(object parameter) => IsVisible(GetStatements());
	}
}
