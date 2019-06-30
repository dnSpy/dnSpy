/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.AsmEditor.Compiler {
	sealed class CodeEditorContext {
		public EditCodeVM VM { get; }
		public ICodeEditor CodeEditor { get; }
		public CodeEditorContext(EditCodeVM vm, ICodeEditor codeEditor) {
			VM = vm;
			CodeEditor = codeEditor;
		}
	}

	abstract class CodeEditorCommandTargetMenuItemBase : CommandTargetMenuItemBase<CodeEditorContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected CodeEditorCommandTargetMenuItemBase(StandardIds cmdId)
			: base(cmdId) {
		}

		protected CodeEditorCommandTargetMenuItemBase(EditCodeIds cmdId)
			: base(EditCodeCommandConstants.EditCodeGroup, (int)cmdId) {
		}

		protected override CodeEditorContext? CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_CODE_EDITOR_GUID))
				return null;
			var codeEditor = context.Find<ICodeEditor>();
			if (codeEditor is null)
				return null;
			var vm = EditCodeVM.TryGet(codeEditor.TextView);
			if (vm is null)
				return null;
			return new CodeEditorContext(vm, codeEditor);
		}

		protected override ICommandTarget? GetCommandTarget(CodeEditorContext context) => context.CodeEditor.TextView.CommandTarget;
	}

	[ExportMenuItem(Header = "res:Button_Compile", Icon = DsImagesAttribute.BuildSolution, InputGestureText = "res:ShortCutKeyF6", Group = MenuConstants.GROUP_CTX_CODEEDITOR_COMPILE, Order = 0)]
	sealed class CompileContextMenuEntry : CodeEditorCommandTargetMenuItemBase {
		CompileContextMenuEntry()
			: base(EditCodeIds.Compile) {
		}
	}

	[ExportMenuItem(Header = "res:CutCommand", Icon = DsImagesAttribute.Cut, InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_CTX_CODEEDITOR_COPY, Order = 0)]
	sealed class CutContextMenuEntry : CodeEditorCommandTargetMenuItemBase {
		CutContextMenuEntry()
			: base(StandardIds.Cut) {
		}
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_CODEEDITOR_COPY, Order = 10)]
	sealed class CopyContextMenuEntry : CodeEditorCommandTargetMenuItemBase {
		CopyContextMenuEntry()
			: base(StandardIds.Copy) {
		}
	}

	[ExportMenuItem(Header = "res:PasteCommand", Icon = DsImagesAttribute.Paste, InputGestureText = "res:ShortCutKeyCtrlV", Group = MenuConstants.GROUP_CTX_CODEEDITOR_COPY, Order = 20)]
	sealed class PasteContextMenuEntry : CodeEditorCommandTargetMenuItemBase {
		PasteContextMenuEntry()
			: base(StandardIds.Paste) {
		}
	}

	[ExportMenuItem(Header = "res:FindCommand", Icon = DsImagesAttribute.Search, InputGestureText = "res:ShortCutKeyCtrlF", Group = MenuConstants.GROUP_CTX_CODEEDITOR_FIND, Order = 0)]
	sealed class FindInCodeContextMenuEntry : CodeEditorCommandTargetMenuItemBase {
		FindInCodeContextMenuEntry()
			: base(StandardIds.Find) {
		}
	}

	[ExportMenuItem(Header = "res:IncrementalSearchCommand", Icon = DsImagesAttribute.Search, InputGestureText = "res:ShortCutKeyCtrlI", Group = MenuConstants.GROUP_CTX_CODEEDITOR_FIND, Order = 10)]
	sealed class IncrementalSearchForwardContextMenuEntry : CodeEditorCommandTargetMenuItemBase {
		IncrementalSearchForwardContextMenuEntry()
			: base(StandardIds.IncrementalSearchForward) {
		}
	}
}
