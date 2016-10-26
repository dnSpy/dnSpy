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
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;

namespace dnSpy.AsmEditor.MethodBody {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		static readonly RoutedCommand EditILInstructionsCommand = new RoutedCommand("EditILInstructionsCommand", typeof(CommandLoader));

		[ImportingConstructor]
		CommandLoader(IWpfCommandService wpfCommandService, EditILInstructionsCommand editILCmd) {
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENTVIEWER_UICONTEXT);
			ICommand editILCmd2 = editILCmd;
			cmds.Add(EditILInstructionsCommand,
				(s, e) => editILCmd2.Execute(null),
				(s, e) => e.CanExecute = editILCmd2.CanExecute(null),
				ModifierKeys.Control | ModifierKeys.Shift, Key.E);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class EditMethodBodyILCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditMethodBodyCommand", Icon = DsImagesAttribute.Editor, Group = MenuConstants.GROUP_CTX_DOCUMENTS_ASMED_ILED, Order = 20)]
		sealed class DocumentsCommand : DocumentsContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppService appService;

			[ImportingConstructor]
			DocumentsCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IMethodAnnotations> methodAnnotations, IAppService appService) {
				this.undoCommandService = undoCommandService;
				this.methodAnnotations = methodAnnotations;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => EditMethodBodyILCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => EditMethodBodyILCommand.Execute(methodAnnotations, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditMethodBodyCommand", Icon = DsImagesAttribute.Editor, Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 50)]
		sealed class EditMenuCommand : EditMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppService appService;

			[ImportingConstructor]
			EditMenuCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IMethodAnnotations> methodAnnotations, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.methodAnnotations = methodAnnotations;
				this.appService = appService;
			}

			public override bool IsVisible(AsmEditorContext context) => EditMethodBodyILCommand.CanExecute(context.Nodes);
			public override void Execute(AsmEditorContext context) => EditMethodBodyILCommand.Execute(methodAnnotations, undoCommandService, appService, context.Nodes);
		}

		[ExportMenuItem(Header = "res:EditMethodBodyCommand", Icon = DsImagesAttribute.Editor, Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_ILED, Order = 20)]
		sealed class CodeCommand : CodeContextMenuHandler {
			readonly Lazy<IUndoCommandService> undoCommandService;
			readonly Lazy<IMethodAnnotations> methodAnnotations;
			readonly IAppService appService;

			[ImportingConstructor]
			CodeCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IMethodAnnotations> methodAnnotations, IAppService appService)
				: base(appService.DocumentTreeView) {
				this.undoCommandService = undoCommandService;
				this.methodAnnotations = methodAnnotations;
				this.appService = appService;
			}

			public override bool IsEnabled(CodeContext context) => !EditILInstructionsCommand.IsVisibleInternal(context.MenuItemContextOrNull) && context.IsDefinition && EditMethodBodyILCommand.CanExecute(context.Nodes);
			public override void Execute(CodeContext context) => EditMethodBodyILCommand.Execute(methodAnnotations, undoCommandService, appService, context.Nodes);
		}

		static bool CanExecute(DocumentTreeNodeData[] nodes) => nodes.Length == 1 && nodes[0] is MethodNode;

		internal static void Execute(Lazy<IMethodAnnotations> methodAnnotations, Lazy<IUndoCommandService> undoCommandService, IAppService appService, DocumentTreeNodeData[] nodes, uint[] offsets = null) {
			if (!CanExecute(nodes))
				return;

			var methodNode = (MethodNode)nodes[0];

			var module = nodes[0].GetModule();
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new MethodBodyVM(new MethodBodyOptions(methodNode.MethodDef), module, appService.DecompilerService, methodNode.MethodDef.DeclaringType, methodNode.MethodDef);
			var win = new MethodBodyDlg();
			win.DataContext = data;
			win.Owner = appService.MainWindow;
			win.Title = string.Format("{0} - {1}", win.Title, methodNode.ToString());

			if (data.IsCilBody && offsets != null)
				data.CilBodyVM.Select(offsets);

			if (win.ShowDialog() != true)
				return;

			undoCommandService.Value.Add(new EditMethodBodyILCommand(methodAnnotations.Value, methodNode, data.CreateMethodBodyOptions()));
		}

		readonly IMethodAnnotations methodAnnotations;
		readonly MethodNode methodNode;
		readonly MethodBodyOptions newOptions;
		readonly dnlib.DotNet.Emit.MethodBody origMethodBody;
		bool isBodyModified;

		EditMethodBodyILCommand(IMethodAnnotations methodAnnotations, MethodNode methodNode, MethodBodyOptions options) {
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

	[Export, ExportMenuItem(Header = "res:EditILInstructionsCommand", Icon = DsImagesAttribute.Editor, InputGestureText = "res:ShortCutKeyCtrlShiftE", Group = MenuConstants.GROUP_CTX_DOCVIEWER_ASMED_ILED, Order = 10)]
	sealed class EditILInstructionsCommand : MenuItemBase, ICommand {
		readonly Lazy<IUndoCommandService> undoCommandService;
		readonly Lazy<IMethodAnnotations> methodAnnotations;
		readonly IAppService appService;

		[ImportingConstructor]
		EditILInstructionsCommand(Lazy<IUndoCommandService> undoCommandService, Lazy<IMethodAnnotations> methodAnnotations, IAppService appService) {
			this.undoCommandService = undoCommandService;
			this.methodAnnotations = methodAnnotations;
			this.appService = appService;
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
			var methodNode = appService.DocumentTreeView.FindNode(method);
			if (methodNode == null) {
				MsgBox.Instance.Show(string.Format(dnSpy_AsmEditor_Resources.Error_CouldNotFindMethod, method));
				return;
			}

			EditMethodBodyILCommand.Execute(methodAnnotations, undoCommandService, appService, new DocumentTreeNodeData[] { methodNode }, BodyCommandUtils.GetInstructionOffsets(method, list));
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

			return BodyCommandUtils.GetStatements(documentViewer, documentViewer.Caret.Position.BufferPosition);
		}

		void ICommand.Execute(object parameter) => Execute(GetStatements());
		bool ICommand.CanExecute(object parameter) => IsVisible(GetStatements());
	}
}
