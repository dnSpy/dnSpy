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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using dnlib.DotNet;
using dnSpy.AsmEditor.Properties;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Decompiler.Shared;
using dnSpy.Languages.IL;
using dnSpy.Shared.Menus;

namespace dnSpy.AsmEditor.Commands {
	[ExportAutoLoaded]
	sealed class CopyILBytesLoader : IAutoLoaded {
		static readonly RoutedCommand CopyILBytesCommand = new RoutedCommand("CopyILBytesCommand", typeof(CopyILBytesLoader));
		readonly IFileTabManager fileTabManager;
		readonly Lazy<IMethodAnnotations> methodAnnotations;

		[ImportingConstructor]
		CopyILBytesLoader(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, Lazy<IMethodAnnotations> methodAnnotations) {
			this.fileTabManager = fileTabManager;
			this.methodAnnotations = methodAnnotations;
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_TEXTEDITOR_UICONTEXT);
			cmds.Add(CopyILBytesCommand, CopyILBytesExecuted, CopyILBytesCanExecute, ModifierKeys.Control, Key.B);
		}

		void CopyILBytesCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = CopyILBytesCodeCommand.CanExecute(fileTabManager.ActiveTab.TryGetTextEditorUIContext());
		void CopyILBytesExecuted(object sender, ExecutedRoutedEventArgs e) =>
			CopyILBytesCodeCommand.Execute(fileTabManager.ActiveTab.TryGetTextEditorUIContext(), methodAnnotations);
	}

	[ExportMenuItem(Header = "res:CopyILBytesCommand", Icon = "Copy", InputGestureText = "res:CopyILBytesKey", Group = MenuConstants.GROUP_CTX_CODE_EDITOR, Order = 20)]
	sealed class CopyILBytesCodeCommand : MenuItemBase {
		readonly Lazy<IMethodAnnotations> methodAnnotations;

		[ImportingConstructor]
		CopyILBytesCodeCommand(Lazy<IMethodAnnotations> methodAnnotations) {
			this.methodAnnotations = methodAnnotations;
		}

		public override bool IsVisible(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
				return false;
			return CanExecute(context.Find<ITextEditorUIContext>());
		}

		public override void Execute(IMenuItemContext context) {
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
				Execute(context.Find<ITextEditorUIContext>(), methodAnnotations);
		}

		public static bool CanExecute(ITextEditorUIContext uiContext) =>
			uiContext != null && FindInstructions(uiContext).Any();

		public static void Execute(ITextEditorUIContext uiContext, Lazy<IMethodAnnotations> methodAnnotations) {
			if (!CanExecute(uiContext))
				return;

			var copier = new InstructionILBytesCopier();
			var text = copier.Copy(FindInstructions(uiContext), methodAnnotations);
			if (text.Length > 0) {
				try {
					Clipboard.SetText(text);
				}
				catch (ExternalException) { }
				if (copier.FoundUnknownBytes) {
					MsgBox.Instance.ShowIgnorableMessage(new Guid("141A1744-13CD-4835-A804-08D93D8E0D2B"),
						dnSpy_AsmEditor_Resources.UnknownBytesMsg,
						MsgBoxButton.OK);
				}
			}
		}

		static IEnumerable<CodeReference> FindInstructions(ITextEditorUIContext uiContext) {
			foreach (var r in uiContext.GetSelectedCodeReferences()) {
				if (r.IsLocalTarget && r.Reference is InstructionReference)
					yield return r;
			}
		}
	}

	struct InstructionILBytesCopier {
		public bool FoundUnknownBytes { get; private set; }

		public string Copy(IEnumerable<CodeReference> refs, Lazy<IMethodAnnotations> methodAnnotations) {
			var sb = new StringBuilder();

			IInstructionBytesReader reader = null;
			try {
				MethodDef method = null;
				int index = 0;
				foreach (var r in refs) {
					var ir = (InstructionReference)r.Reference;
					var instr = ir.Instruction;
					if (ir.Method != method) {
						if (reader != null)
							reader.Dispose();
						method = ir.Method;
						reader = InstructionBytesReader.Create(method, methodAnnotations.Value.IsBodyModified(method));
						index = method.Body.Instructions.IndexOf(instr);
						if (index < 0)
							throw new InvalidOperationException();
						reader.SetInstruction(index, instr.Offset);
					}
					else if (index >= method.Body.Instructions.Count)
						throw new InvalidOperationException();
					else if (method.Body.Instructions[index + 1] != ir.Instruction) {
						index = method.Body.Instructions.IndexOf(instr);
						if (index < 0)
							throw new InvalidOperationException();
						reader.SetInstruction(index, instr.Offset);
					}
					else
						index++;

					int size = instr.GetSize();
					for (int i = 0; i < size; i++) {
						int b = reader.ReadByte();
						if (b < 0) {
							sb.Append("??");
							FoundUnknownBytes = true;
						}
						else
							sb.Append(string.Format("{0:X2}", b));
					}
				}
			}
			finally {
				if (reader != null)
					reader.Dispose();
			}

			return sb.ToString();
		}
	}
}
