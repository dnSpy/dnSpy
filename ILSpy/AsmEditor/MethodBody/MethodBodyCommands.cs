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
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor.MethodBody {
	[Export(typeof(IPlugin))]
	sealed class MethodBodyPlugin : IPlugin {
		static readonly ICommand editILInstructionsCommand = new EditILInstructionsCommand();

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
		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "ILEditor",
								Category = "AsmEd",
								Order = 640)]
		[ExportMainMenuCommand(MenuHeader = CMD_NAME + "…",
							Menu = "_Edit",
							MenuIcon = "ILEditor",
							MenuCategory = "AsmEd",
							MenuOrder = 2440)]
		sealed class TheEditCommand : EditCommand {
			protected override bool CanExecuteInternal(ILSpyTreeNode[] nodes) {
				return MethodBodySettingsCommand.CanExecute(nodes);
			}

			protected override void ExecuteInternal(ILSpyTreeNode[] nodes) {
				MethodBodySettingsCommand.Execute(nodes);
			}
		}

		[ExportContextMenuEntry(Header = CMD_NAME + "…",
								Icon = "ILEditor",
								Category = "AsmEd",
								Order = 640)]
		sealed class TheTextEditorCommand : TextEditorCommand {
			protected override bool CanExecute(Context ctx) {
				return ctx.ReferenceSegment.IsLocalTarget &&
					MethodBodySettingsCommand.CanExecute(ctx.Nodes);
			}

			protected override void Execute(Context ctx) {
				MethodBodySettingsCommand.Execute(ctx.Nodes);
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

			var data = new MethodBodyVM(new MethodBodyOptions(methodNode.MethodDefinition), module, MainWindow.Instance.CurrentLanguage, methodNode.MethodDefinition.DeclaringType, methodNode.MethodDefinition);
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
			this.origMethodBody = methodNode.MethodDefinition.MethodBody;
		}

		public string Description {
			get { return CMD_NAME; }
		}

		public void Execute() {
			isBodyModified = MethodAnnotations.Instance.IsBodyModified(methodNode.MethodDefinition);
			MethodAnnotations.Instance.SetBodyModified(methodNode.MethodDefinition, true);
			newOptions.CopyTo(methodNode.MethodDefinition);
		}

		public void Undo() {
			methodNode.MethodDefinition.MethodBody = origMethodBody;
			MethodAnnotations.Instance.SetBodyModified(methodNode.MethodDefinition, isBodyModified);
		}

		public IEnumerable<object> ModifiedObjects {
			get { yield return methodNode; }
		}

		public void Dispose() {
		}
	}

	[ExportContextMenuEntry(Header = "Edit IL Instruction_s…",
							Icon = "ILEditor",
							Category = "AsmEd",
							Order = 639.99,
							InputGestureText = "Ctrl+E")]
	sealed class EditILInstructionsCommand : IContextMenuEntry, ICommand {
		public bool IsVisible(ContextMenuEntryContext context) {
			var list = GetMappings(context);
			return list != null &&
				list.Count != 0 &&
				list[0].MemberMapping.MethodDefinition != null &&
				list[0].MemberMapping.MethodDefinition.Body != null &&
				list[0].MemberMapping.MethodDefinition.Body.Instructions.Count > 0;
		}

		internal static IList<SourceCodeMapping> GetMappings(ContextMenuEntryContext context) {
			if (!(context.Element is DecompilerTextView) || context.Position == null)
				return null;
			var list = SourceCodeMappingUtils.Find((DecompilerTextView)context.Element, context.Position.Value.Line, context.Position.Value.Column);
			if (list.Count == 0)
				return null;
			if (!(list[0].StartLocation.Line <= context.Position.Value.Line && context.Position.Value.Line <= list[0].EndLocation.Line))
				return null;
			return list;
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			return true;
		}

		public void Execute(ContextMenuEntryContext context) {
			var list = GetMappings(context);
			if (list == null)
				return;

			var method = list[0].MemberMapping.MethodDefinition;
			var methodNode = MainWindow.Instance.AssemblyListTreeNode.FindMethodNode(method);
			if (methodNode == null) {
				MainWindow.Instance.ShowMessageBox(string.Format("Could not find method: {0}", method));
				return;
			}

			MethodBodySettingsCommand.Execute(new ILSpyTreeNode[] { methodNode }, GetInstructionOffsets(method, list));
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

		static ContextMenuEntryContext CreateContext() {
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView != null && textView.IsKeyboardFocusWithin)
				return ContextMenuEntryContext.Create(textView, true);

			return ContextMenuEntryContext.Create(null, true);
		}

		void ICommand.Execute(object parameter) {
			Execute(CreateContext());
		}

		bool ICommand.CanExecute(object parameter) {
			return IsVisible(CreateContext());
		}
	}
}
