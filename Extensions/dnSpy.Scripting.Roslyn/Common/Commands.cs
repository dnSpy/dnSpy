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
using dnSpy.Scripting.Roslyn.Commands;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class ReplEditorCtxMenuContext {
		public readonly IReplEditor UI;
		public readonly ScriptControlVM VM;

		public ReplEditorCtxMenuContext(IReplEditor ui) {
			UI = ui;
			VM = ScriptContent.GetScriptContent(ui).ScriptControlVM;
		}
	}

	abstract class ReplEditorCtxMenuCommand : MenuItemBase<ReplEditorCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override ReplEditorCtxMenuContext? CreateContext(IMenuItemContext context) => CreateContextInternal(context);

		internal static ReplEditorCtxMenuContext? CreateContextInternal(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_REPL_TEXTEDITORCONTROL_GUID))
				return null;
			var ui = context.Find<IReplEditor>();
			if (ui is null)
				return null;

			return new ReplEditorCtxMenuContext(ui);
		}
	}

	abstract class ReplEditorCtxMenuCommandTargetCommand : CommandTargetMenuItemBase<ReplEditorCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override ReplEditorCtxMenuContext? CreateContext(IMenuItemContext context) => CreateContextInternal(context);

		internal static ReplEditorCtxMenuContext? CreateContextInternal(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_REPL_TEXTEDITORCONTROL_GUID))
				return null;
			var ui = context.Find<IReplEditor>();
			if (ui is null)
				return null;

			return new ReplEditorCtxMenuContext(ui);
		}

		protected ReplEditorCtxMenuCommandTargetCommand(StandardIds cmdId)
			: base(CommandConstants.StandardGroup, (int)cmdId) {
		}

		protected ReplEditorCtxMenuCommandTargetCommand(TextEditorIds cmdId)
			: base(CommandConstants.TextEditorGroup, (int)cmdId) {
		}

		protected ReplEditorCtxMenuCommandTargetCommand(ReplIds cmdId)
			: base(CommandConstants.ReplGroup, (int)cmdId) {
		}

		protected ReplEditorCtxMenuCommandTargetCommand(RoslynReplIds cmdId)
			: base(RoslynReplCommandConstants.RoslynReplGroup, (int)cmdId) {
		}

		protected sealed override ICommandTarget? GetCommandTarget(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget;
	}

	[ExportMenuItem(Header = "res:Script_ToolTip_Reset", Icon = DsImagesAttribute.Restart, Group = MenuConstants.GROUP_CTX_REPL_RESET, Order = 0)]
	sealed class ResetReplEditorCtxMenuCommand : ReplEditorCtxMenuCommandTargetCommand {
		ResetReplEditorCtxMenuCommand() : base(RoslynReplIds.Reset) { }
	}

	[ExportMenuItem(Header = "res:CutCommand", Icon = DsImagesAttribute.Cut, InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_CTX_REPL_COPY, Order = 0)]
	sealed class CutReplEditorCtxMenuCommand : ReplEditorCtxMenuCommandTargetCommand {
		CutReplEditorCtxMenuCommand() : base(StandardIds.Cut) { }
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = DsImagesAttribute.Copy, InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_REPL_COPY, Order = 10)]
	sealed class CopyReplEditorCtxMenuCommand : ReplEditorCtxMenuCommandTargetCommand {
		CopyReplEditorCtxMenuCommand() : base(StandardIds.Copy) { }
	}

	[ExportMenuItem(Header = "res:CopyCodeCommand", Icon = DsImagesAttribute.CopyItem, InputGestureText = "res:ShortCutKeyCtrlShiftC", Group = MenuConstants.GROUP_CTX_REPL_COPY, Order = 20)]
	sealed class CopyCodeReplEditorCtxMenuCommand : ReplEditorCtxMenuCommandTargetCommand {
		CopyCodeReplEditorCtxMenuCommand() : base(ReplIds.CopyCode) { }
	}

	[ExportMenuItem(Header = "res:PasteCommand", Icon = DsImagesAttribute.Paste, InputGestureText = "res:ShortCutKeyCtrlV", Group = MenuConstants.GROUP_CTX_REPL_COPY, Order = 30)]
	sealed class PasteReplEditorCtxMenuCommand : ReplEditorCtxMenuCommandTargetCommand {
		PasteReplEditorCtxMenuCommand() : base(StandardIds.Paste) { }
	}

	[ExportMenuItem(Header = "res:SaveCommand", Icon = DsImagesAttribute.Save, InputGestureText = "res:ShortCutKeyCtrlS", Group = MenuConstants.GROUP_CTX_REPL_SAVE, Order = 0)]
	sealed class SaveReplEditorCtxMenuCommand : ReplEditorCtxMenuCommandTargetCommand {
		SaveReplEditorCtxMenuCommand() : base(RoslynReplIds.SaveText) { }
	}

	[ExportMenuItem(Header = "res:SaveCodeCommand", InputGestureText = "res:ShortCutKeyCtrlShiftS", Group = MenuConstants.GROUP_CTX_REPL_SAVE, Order = 10)]
	sealed class SaveCodeReplEditorCtxMenuCommand : ReplEditorCtxMenuCommandTargetCommand {
		SaveCodeReplEditorCtxMenuCommand() : base(RoslynReplIds.SaveCode) { }
	}

	[ExportMenuItem(Header = "res:ClearScreenCommand", Icon = DsImagesAttribute.ClearWindowContent, InputGestureText = "res:ShortCutKeyCtrlL", Group = MenuConstants.GROUP_CTX_REPL_CLEAR, Order = 0)]
	sealed class ClearReplEditorCtxMenuCommand : ReplEditorCtxMenuCommandTargetCommand {
		ClearReplEditorCtxMenuCommand() : base(ReplIds.ClearScreen) { }
	}

	[ExportMenuItem(Header = "res:ShowLineNumbersCommand", Group = MenuConstants.GROUP_CTX_REPL_SETTINGS, Order = 0)]
	sealed class ShowLineNumbersReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.UI.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, !context.UI.TextView.Options.IsLineNumberMarginEnabled());
		public override bool IsChecked(ReplEditorCtxMenuContext context) => context.UI.TextView.Options.IsLineNumberMarginEnabled();
	}

	[ExportMenuItem(Header = "res:WordWrapHeader", InputGestureText = "res:ShortCutKeyCtrlECtrlW", Icon = DsImagesAttribute.WordWrap, Group = MenuConstants.GROUP_CTX_REPL_SETTINGS, Order = 10)]
	sealed class WordWrapReplEditorCtxMenuCommand : ReplEditorCtxMenuCommandTargetCommand {
		WordWrapReplEditorCtxMenuCommand() : base(TextEditorIds.TOGGLEWORDWRAP) { }
		public override bool IsChecked(ReplEditorCtxMenuContext context) => (context.UI.TextView.Options.WordWrapStyle() & WordWrapStyles.WordWrap) != 0;
	}
}
