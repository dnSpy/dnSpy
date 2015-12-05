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
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.Menus;
using ICSharpCode.AvalonEdit;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.MethodBody {
	[Export(typeof(IPlugin))]
	sealed class MethodBodyPlugin : IPlugin {
		static readonly ICommand editILInstructionsCommand = new EditILInstructionsCommand();

		void IPlugin.EarlyInit() {
		}

		public void OnLoaded() {
			MainWindow.Instance.CodeBindings.Add(new RoutedCommand("EditILInstructionsCommand", typeof(MethodBodyPlugin)),
				(s, e) => editILInstructionsCommand.Execute(null),
				(s, e) => e.CanExecute = editILInstructionsCommand.CanExecute(null),
				ModifierKeys.Control, Key.E);
		}
	}

	[DebuggerDisplay("{Description}")]
	sealed class MethodBodySettingsCommand : IUndoCommand {
		const string CMD_NAME = "Edit Method Body";
		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "ILEditor", Group = MenuConstants.GROUP_CTX_FILES_ASMED_ILED, Order = 10)]
		sealed class FilesCommand : FilesContextMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return MethodBodySettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				MethodBodySettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = CMD_NAME + "...", Icon = "ILEditor", Group = MenuConstants.GROUP_APP_MENU_EDIT_ASMED_SETTINGS, Order = 40)]
		sealed class EditMenuCommand : EditMenuHandler {
			public override bool IsVisible(AsmEditorContext context) {
				return MethodBodySettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(AsmEditorContext context) {
				MethodBodySettingsCommand.Execute(context.Nodes);
			}
		}

		[ExportMenuItem(Header = CMD_NAME + "...", Icon = "ILEditor", Group = MenuConstants.GROUP_CTX_CODE_ASMED_ILED, Order = 10)]
		sealed class CodeCommand : CodeContextMenuHandler {
			public override bool IsEnabled(CodeContext context) {
				return context.IsLocalTarget &&
					MethodBodySettingsCommand.CanExecute(context.Nodes);
			}

			public override void Execute(CodeContext context) {
				MethodBodySettingsCommand.Execute(context.Nodes);
			}
		}

		static bool CanExecute(ILSpyTreeNode[] nodes) {
			return nodes.Length == 1 &&
				nodes[0] is MethodTreeNode;
		}

		internal static void Execute(ILSpyTreeNode[] nodes, uint[] offsets = null) {
			if (!CanExecute(nodes))
				return;

			var methodNode = (MethodTreeNode)nodes[0];

			var module = ILSpyTreeNode.GetModule(nodes[0]);
			Debug.Assert(module != null);
			if (module == null)
				throw new InvalidOperationException();

			var data = new MethodBodyVM(new MethodBodyOptions(methodNode.MethodDef), module, MainWindow.Instance.CurrentLanguage, methodNode.MethodDef.DeclaringType, methodNode.MethodDef);
			var win = new MethodBodyDlg();
			win.DataContext = data;
			win.Owner = MainWindow.Instance;
			win.Title = string.Format("{0} - {1}", win.Title, methodNode.ToString());

			if (data.IsCilBody && offsets != null)
				data.CilBodyVM.Select(offsets);

			if (win.ShowDialog() != true)
				return;

			UndoCommandManager.Instance.Add(new MethodBodySettingsCommand(methodNode, data.CreateMethodBodyOptions()));
		}

		readonly MethodTreeNode methodNode;
		readonly MethodBodyOptions newOptions;
		readonly dnlib.DotNet.Emit.MethodBody origMethodBody;
		bool isBodyModified;

		MethodBodySettingsCommand(MethodTreeNode methodNode, MethodBodyOptions options) {
			this.methodNode = methodNode;
			this.newOptions = options;
			this.origMethodBody = methodNode.MethodDef.MethodBody;
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			isBodyModified = MethodAnnotations.Instance.IsBodyModified(methodNode.MethodDef);
			MethodAnnotations.Instance.SetBodyModified(methodNode.MethodDef, true);
			newOptions.CopyTo(methodNode.MethodDef);
		}

		public void Undo() {
			methodNode.MethodDef.MethodBody = origMethodBody;
			MethodAnnotations.Instance.SetBodyModified(methodNode.MethodDef, isBodyModified);
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return methodNode; }
		}

		public void Dispose() {
		}
	}

	[ExportMenuItem(Header = "Edit IL Instruction_s...", Icon = "ILEditor", InputGestureText = "Ctrl+E", Group = MenuConstants.GROUP_CTX_CODE_ASMED_ILED, Order = 0)]
	sealed class EditILInstructionsCommand : MenuItemBase, ICommand {
		public override bool IsVisible(IMenuItemContext context) {
			return IsVisible(GetMappings(context));
		}

		static bool IsVisible(IList<SourceCodeMapping> list) {
			return list != null &&
				list.Count != 0 &&
				list[0].MemberMapping.MethodDef != null &&
				list[0].MemberMapping.MethodDef.Body != null &&
				list[0].MemberMapping.MethodDef.Body.Instructions.Count > 0;
		}

		public override void Execute(IMenuItemContext context) {
			Execute(GetMappings(context));
		}

		static void Execute(IList<SourceCodeMapping> list) {
			if (list == null)
				return;

			var method = list[0].MemberMapping.MethodDef;
			var methodNode = MainWindow.Instance.DnSpyFileListTreeNode.FindMethodNode(method);
			if (methodNode == null) {
				MainWindow.Instance.ShowMessageBox(string.Format("Could not find method: {0}", method));
				return;
			}

			MethodBodySettingsCommand.Execute(new ILSpyTreeNode[] { methodNode }, GetInstructionOffsets(method, list));
		}

		static IList<SourceCodeMapping> GetMappings(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
				return null;
			var textView = context.CreatorObject.Object as DecompilerTextView;
			if (textView == null)
				return null;
			var pos = context.FindByType<TextViewPosition?>();
			if (pos == null)
				return null;
			return GetMappings(textView, pos.Value.Line, pos.Value.Column);
		}

		internal static IList<SourceCodeMapping> GetMappings(DecompilerTextView textView, int line, int col) {
			if (textView == null)
				return null;
			var list = SourceCodeMappingUtils.Find(textView, line, col);
			if (list.Count == 0)
				return null;
			if (!(list[0].StartLocation.Line <= line && line <= list[0].EndLocation.Line))
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
			foreach (var range in list.Select(a => a.ILInstructionOffset)) {
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

		static IList<SourceCodeMapping> GetMappings() {
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView == null || !textView.IsKeyboardFocusWithin)
				return null;

			var pos = textView.TextEditor.TextArea.Caret.Position;
			return GetMappings(textView, pos.Line, pos.Column);
		}

		void ICommand.Execute(object parameter) {
			Execute(GetMappings());
		}

		bool ICommand.CanExecute(object parameter) {
			return IsVisible(GetMappings());
		}
	}
}
