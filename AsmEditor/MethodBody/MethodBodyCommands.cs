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
using System.Linq;
using System.Windows;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Commands;
using dnSpy.AsmEditor.Properties;
using dnSpy.AsmEditor.UndoRedo;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Menus;

namespace dnSpy.AsmEditor.MethodBody {
	[ExportAutoLoaded]
	sealed class CommandLoader : IAutoLoaded {
		static readonly RoutedCommand EditILInstructionsCommand = new RoutedCommand("EditILInstructionsCommand", typeof(CommandLoader));

		[ImportingConstructor]
		CommandLoader(IWpfCommandManager wpfCommandManager, EditILInstructionsCommand editILCmd) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_TEXTEDITOR_UICONTEXT);
			ICommand editILCmd2 = editILCmd;
			cmds.Add(EditILInstructionsCommand,
				(s, e) => editILCmd2.Execute(null),
				(s, e) => e.CanExecute = editILCmd2.CanExecute(null),
				ModifierKeys.Control, Key.E);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class MethodBodySettingsCommand : IUndoCommand {
		[ExportMenuItem(Header = "res:EditMethodBodyCommand", Icon = "ILEditor", Group = MenuConstants.GROUP_CTX_FILES_ASMED_ILED, Order = 10)]
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

			public override bool IsVisible(AsmEditorContext context) {
				return MethodBodySettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				MethodBodySettingsCommand.Execute(methodAnnotations, undoCommandManager, appWindow, context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:EditMethodBodyCommand", Icon = "ILEditor", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 40)]
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

			public override bool IsVisible(AsmEditorContext context) {
				return MethodBodySettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				MethodBodySettingsCommand.Execute(methodAnnotations, undoCommandManager, appWindow, context.Nodes);
			}
		}

		[ExportMenuItem(Header = "res:EditMethodBodyCommand", Icon = "ILEditor", Group = MenuConstants.GROUP_CTX_CODE_ASMED_ILED, Order = 10)]
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

			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					MethodBodySettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				MethodBodySettingsCommand.Execute(methodAnnotations, undoCommandManager, appWindow, context.Nodes);
			}
		}

		static bool CanExecute(IFileTreeNodeData[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is IMethodNode;
		}

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

			undoCommandManager.Value.Add(new MethodBodySettingsCommand(methodAnnotations.Value, methodNode, data.CreateMethodBodyOptions()));
		}

		readonly IMethodAnnotations methodAnnotations;
		readonly IMethodNode methodNode;
		readonly MethodBodyOptions newOptions;
		readonly dnlib.DotNet.Emit.MethodBody origMethodBody;
		bool isBodyModified;

		MethodBodySettingsCommand(IMethodAnnotations methodAnnotations, IMethodNode methodNode, MethodBodyOptions options) {
			this.methodAnnotations = methodAnnotations;
			this.methodNode = methodNode;
			this.newOptions = options;
			this.origMethodBody = methodNode.MethodDef.MethodBody;
		}

		public string Description {
			get { return dnSpy_AsmEditor_Resources.EditMethodBodyCommand2; }
		}

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

	[Export, ExportMenuItem(Header = "res:EditILInstructionsCommand", Icon = "ILEditor", InputGestureText = "res:ShortCutKeyCtrlE", Group = MenuConstants.GROUP_CTX_CODE_ASMED_ILED, Order = 0)]
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

		public override bool IsVisible(IMenuItemContext context) {
			return IsVisible(GetMappings(context));
		}

		static bool IsVisible(IList<SourceCodeMapping> list) {
			return list != null &&
				list.Count != 0 &&
				list[0].Mapping.Method != null &&
				list[0].Mapping.Method.Body != null &&
				list[0].Mapping.Method.Body.Instructions.Count > 0;
		}

		public override void Execute(IMenuItemContext context) {
			Execute(GetMappings(context));
		}

		void Execute(IList<SourceCodeMapping> list) {
			if (list == null)
				return;

			var method = list[0].Mapping.Method;
			var methodNode = appWindow.FileTreeView.FindNode(method);
			if (methodNode == null) {
				Shared.App.MsgBox.Instance.Show(string.Format(dnSpy_AsmEditor_Resources.Error_CouldNotFindMethod, method));
				return;
			}

			MethodBodySettingsCommand.Execute(methodAnnotations, undoCommandManager, appWindow, new IFileTreeNodeData[] { methodNode }, GetInstructionOffsets(method, list));
		}

		static IList<SourceCodeMapping> GetMappings(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
				return null;
			var uiContext = context.Find<ITextEditorUIContext>();
			if (uiContext == null)
				return null;
			var pos = context.Find<TextEditorLocation?>();
			if (pos == null)
				return null;
			return GetMappings(uiContext, pos.Value.Line, pos.Value.Column);
		}

		internal static IList<SourceCodeMapping> GetMappings(ITextEditorUIContext uiContext, int line, int col) {
			if (uiContext == null)
				return null;
			var cm = uiContext.GetCodeMappings();
			var list = cm.Find(line, col);
			if (list.Count == 0)
				return null;
			if (!(list[0].StartPosition.Line <= line && line <= list[0].EndPosition.Line))
				return null;
			return list;
		}

		static uint[] GetInstructionOffsets(MethodDef method, IList<SourceCodeMapping> list) {
			if (method == null)
				return null;
			var body = method.Body;
			if (body == null)
				return null;

			var foundInstrs = new HashSet<uint>();
			// The instructions' offset field is assumed to be valid
			var instrs = body.Instructions.Select(a => a.Offset).ToArray();
			foreach (var range in list.Select(a => a.ILRange)) {
				int index = Array.BinarySearch(instrs, range.From);
				if (index < 0)
					continue;
				for (int i = index; i < instrs.Length; i++) {
					uint instrOffset = instrs[i];
					if (instrOffset >= range.To)
						break;

					foundInstrs.Add(instrOffset);
				}
			}

			return foundInstrs.ToArray();
		}

		event EventHandler ICommand.CanExecuteChanged {
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		IList<SourceCodeMapping> GetMappings() {
			var uiContext = appWindow.FileTabManager.ActiveTab.TryGetTextEditorUIContext();
			if (uiContext == null)
				return null;
			if (!((UIElement)uiContext.UIObject).IsKeyboardFocusWithin)
				return null;

			var pos = uiContext.Location;
			return GetMappings(uiContext, pos.Line, pos.Column);
		}

		void ICommand.Execute(object parameter) {
			Execute(GetMappings());
		}

		bool ICommand.CanExecute(object parameter) {
			return IsVisible(GetMappings());
		}
	}
}
