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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using dnlib.DotNet;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;

namespace dnSpy.AsmEditor {
	[Export(typeof(IPlugin))]
	sealed class InstructionCommandsLoader : IPlugin {
		void IPlugin.EarlyInit() {
		}

		void IPlugin.OnLoaded() {
			var cmd = new RoutedCommand();
			cmd.InputGestures.Add(new KeyGesture(Key.B, ModifierKeys.Control));
			MainWindow.Instance.CommandBindings.Add(new CommandBinding(cmd, CopyILBytesExecuted, CopyILBytesCanExecute));
		}

		void CopyILBytesCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = CopyILBytesCommand.CanExecute(MainWindow.Instance.ActiveTextView);
		}

		void CopyILBytesExecuted(object sender, ExecutedRoutedEventArgs e) {
			CopyILBytesCommand.Execute(MainWindow.Instance.ActiveTextView);
		}
	}

	[ExportContextMenuEntry(Header = "Copy IL Bytes", Icon = "Copy", Category = "Editor", InputGestureText = "Ctrl+B", Order = 1010)]
	sealed class CopyILBytesCommand : IContextMenuEntry {
		public bool IsVisible(ContextMenuEntryContext context) {
			return CanExecute(context.Element as DecompilerTextView);
		}

		public bool IsEnabled(ContextMenuEntryContext context) {
			return true;
		}

		public void Execute(ContextMenuEntryContext context) {
			Execute(context.Element as DecompilerTextView);
		}

		public static bool CanExecute(DecompilerTextView textView) {
			return textView != null &&
				FindInstructions(textView).Any();
		}

		public static void Execute(DecompilerTextView textView) {
			if (!CanExecute(textView))
				return;

			var copier = new InstructionILBytesCopier();
			var text = copier.Copy(FindInstructions(textView));
			if (text.Length > 0) {
				Clipboard.SetText(text);
				if (copier.FoundUnknownBytes) {
					MainWindow.Instance.ShowIgnorableMessageBox("instr: unknown bytes",
						"Some of the copied bytes are unknown because the method has been edited. New tokens and string offsets are only known once the file has been saved to disk.",
						MessageBoxButton.OK);
				}
			}
		}

		static IEnumerable<ReferenceSegment> FindInstructions(DecompilerTextView textView) {
			if (textView.TextEditor.SelectionLength <= 0)
				yield break;
			int start = textView.TextEditor.SelectionStart;
			int end = start + textView.TextEditor.SelectionLength;

			var refs = textView.References;
			if (refs == null)
				yield break;
			var r = refs.FindFirstSegmentWithStartAfter(start);
			while (r != null) {
				if (r.StartOffset >= end)
					break;
				if (r.IsLocalTarget && r.Reference is InstructionReference)
					yield return r;

				r = refs.GetNextSegment(r);
			}
		}
	}

	struct InstructionILBytesCopier {
		public bool FoundUnknownBytes { get; private set; }

		public string Copy(IEnumerable<ReferenceSegment> refs) {
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
						reader = InstructionBytesReader.Create(method);
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
