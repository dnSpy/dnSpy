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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.IL;

namespace dnSpy.AsmEditor.Commands {
	[ExportAutoLoaded]
	sealed class CopyILBytesLoader : IAutoLoaded {
		static readonly RoutedCommand CopyILBytesCommand = new RoutedCommand("CopyILBytesCommand", typeof(CopyILBytesLoader));
		readonly IDocumentTabService documentTabService;
		readonly Lazy<IMethodAnnotations> methodAnnotations;

		[ImportingConstructor]
		CopyILBytesLoader(IWpfCommandService wpfCommandService, IDocumentTabService documentTabService, Lazy<IMethodAnnotations> methodAnnotations) {
			this.documentTabService = documentTabService;
			this.methodAnnotations = methodAnnotations;
			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_DOCUMENTVIEWER_UICONTEXT);
			cmds.Add(CopyILBytesCommand, CopyILBytesExecuted, CopyILBytesCanExecute, ModifierKeys.Control, Key.B);
		}

		void CopyILBytesCanExecute(object sender, CanExecuteRoutedEventArgs e) =>
			e.CanExecute = CopyILBytesCodeCommand.CanExecute(documentTabService.ActiveTab.TryGetDocumentViewer());
		void CopyILBytesExecuted(object sender, ExecutedRoutedEventArgs e) =>
			CopyILBytesCodeCommand.Execute(documentTabService.ActiveTab.TryGetDocumentViewer(), methodAnnotations);
	}

	[ExportMenuItem(Header = "res:CopyILBytesCommand", Icon = "Copy", InputGestureText = "res:CopyILBytesKey", Group = MenuConstants.GROUP_CTX_DOCVIEWER_EDITOR, Order = 20)]
	sealed class CopyILBytesCodeCommand : MenuItemBase {
		readonly Lazy<IMethodAnnotations> methodAnnotations;

		[ImportingConstructor]
		CopyILBytesCodeCommand(Lazy<IMethodAnnotations> methodAnnotations) {
			this.methodAnnotations = methodAnnotations;
		}

		public override bool IsVisible(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return false;
			return CanExecute(context.Find<IDocumentViewer>());
		}

		public override void Execute(IMenuItemContext context) {
			if (context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				Execute(context.Find<IDocumentViewer>(), methodAnnotations);
		}

		public static bool CanExecute(IDocumentViewer documentViewer) =>
			documentViewer != null && FindInstructions(documentViewer).Any();

		public static void Execute(IDocumentViewer documentViewer, Lazy<IMethodAnnotations> methodAnnotations) {
			if (!CanExecute(documentViewer))
				return;

			var copier = new InstructionILBytesCopier();
			var text = copier.Copy(FindInstructions(documentViewer), methodAnnotations);
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

		static IEnumerable<SpanData<ReferenceInfo>> FindInstructions(IDocumentViewer documentViewer) {
			foreach (var refInfo in documentViewer.GetSelectedReferences()) {
				if (refInfo.Data.IsDefinition && refInfo.Data.Reference is InstructionReference)
					yield return refInfo;
			}
		}
	}

	struct InstructionILBytesCopier {
		public bool FoundUnknownBytes { get; private set; }

		public string Copy(IEnumerable<SpanData<ReferenceInfo>> refs, Lazy<IMethodAnnotations> methodAnnotations) {
			var sb = new StringBuilder();

			IInstructionBytesReader reader = null;
			try {
				MethodDef method = null;
				int index = 0;
				foreach (var r in refs) {
					var ir = (InstructionReference)r.Data.Reference;
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
