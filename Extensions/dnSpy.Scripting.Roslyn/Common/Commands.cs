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
using dnSpy.Contracts.Command;
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
			this.UI = ui;
			this.VM = ScriptContent.GetScriptContent(ui).ScriptControlVM;
		}
	}

	abstract class ReplEditorCtxMenuCommand : MenuItemBase<ReplEditorCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override ReplEditorCtxMenuContext CreateContext(IMenuItemContext context) => CreateContextInternal(context);

		internal static ReplEditorCtxMenuContext CreateContextInternal(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_REPL_TEXTEDITORCONTROL_GUID))
				return null;
			var ui = context.Find<IReplEditor>();
			if (ui == null)
				return null;

			return new ReplEditorCtxMenuContext(ui);
		}
	}

	[ExportMenuItem(Header = "res:Script_ToolTip_Reset", Icon = "Reset", Group = MenuConstants.GROUP_CTX_REPL_RESET, Order = 0)]
	sealed class ResetReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.Execute(RoslynReplCommandConstants.RoslynReplGroup, (int)RoslynReplIds.Reset);
		public override bool IsEnabled(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.CanExecute(RoslynReplCommandConstants.RoslynReplGroup, (int)RoslynReplIds.Reset) == CommandTargetStatus.Handled;
	}

	[ExportMenuItem(Header = "res:CutCommand", Icon = "Cut", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_CTX_REPL_COPY, Order = 0)]
	sealed class CutReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.Execute(CommandConstants.StandardGroup, (int)StandardIds.Cut);
		public override bool IsEnabled(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.CanExecute(CommandConstants.StandardGroup, (int)StandardIds.Cut) == CommandTargetStatus.Handled;
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = "Copy", InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_REPL_COPY, Order = 10)]
	sealed class CopyReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.Execute(CommandConstants.StandardGroup, (int)StandardIds.Copy);
		public override bool IsEnabled(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.CanExecute(CommandConstants.StandardGroup, (int)StandardIds.Copy) == CommandTargetStatus.Handled;
	}

	[ExportMenuItem(Header = "res:CopyCodeCommand", Icon = "CopyItem", InputGestureText = "res:ShortCutKeyCtrlShiftC", Group = MenuConstants.GROUP_CTX_REPL_COPY, Order = 20)]
	sealed class CopyCodeReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.Execute(CommandConstants.ReplGroup, (int)ReplIds.CopyCode);
		public override bool IsEnabled(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.CanExecute(CommandConstants.ReplGroup, (int)ReplIds.CopyCode) == CommandTargetStatus.Handled;
	}

	[ExportMenuItem(Header = "res:PasteCommand", Icon = "Paste", InputGestureText = "res:ShortCutKeyCtrlV", Group = MenuConstants.GROUP_CTX_REPL_COPY, Order = 30)]
	sealed class PasteReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.Execute(CommandConstants.StandardGroup, (int)StandardIds.Paste);
		public override bool IsEnabled(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.CanExecute(CommandConstants.StandardGroup, (int)StandardIds.Paste) == CommandTargetStatus.Handled;
	}

	[ExportMenuItem(Header = "res:SaveCommand", Icon = "Save", InputGestureText = "res:ShortCutKeyCtrlS", Group = MenuConstants.GROUP_CTX_REPL_SAVE, Order = 0)]
	sealed class SaveReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.Execute(RoslynReplCommandConstants.RoslynReplGroup, (int)RoslynReplIds.SaveText);
		public override bool IsEnabled(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.CanExecute(RoslynReplCommandConstants.RoslynReplGroup, (int)RoslynReplIds.SaveText) == CommandTargetStatus.Handled;
	}

	[ExportMenuItem(Header = "res:SaveCodeCommand", InputGestureText = "res:ShortCutKeyCtrlShiftS", Group = MenuConstants.GROUP_CTX_REPL_SAVE, Order = 10)]
	sealed class SaveCodeReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.Execute(RoslynReplCommandConstants.RoslynReplGroup, (int)RoslynReplIds.SaveCode);
		public override bool IsEnabled(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.CanExecute(RoslynReplCommandConstants.RoslynReplGroup, (int)RoslynReplIds.SaveCode) == CommandTargetStatus.Handled;
	}

	[ExportMenuItem(Header = "res:ClearScreenCommand", Icon = "ClearWindowContent", InputGestureText = "res:ShortCutKeyCtrlL", Group = MenuConstants.GROUP_CTX_REPL_CLEAR, Order = 0)]
	sealed class ClearReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.Execute(CommandConstants.ReplGroup, (int)ReplIds.ClearScreen);
		public override bool IsEnabled(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.CanExecute(CommandConstants.ReplGroup, (int)ReplIds.ClearScreen) == CommandTargetStatus.Handled;
	}

	[ExportMenuItem(Header = "res:ShowLineNumbersCommand", Group = MenuConstants.GROUP_CTX_REPL_SETTINGS, Order = 0)]
	sealed class ShowLineNumbersReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.UI.TextView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, !context.UI.TextView.Options.IsLineNumberMarginEnabled());
		public override bool IsChecked(ReplEditorCtxMenuContext context) => context.UI.TextView.Options.IsLineNumberMarginEnabled();
	}

	[ExportMenuItem(Header = "res:WordWrapHeader", InputGestureText = "res:ShortCutKeyCtrlECtrlW", Icon = "WordWrap", Group = MenuConstants.GROUP_CTX_REPL_SETTINGS, Order = 10)]
	sealed class WordWrapReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.Execute(CommandConstants.TextEditorGroup, (int)TextEditorIds.TOGGLEWORDWRAP);
		public override bool IsEnabled(ReplEditorCtxMenuContext context) => context.VM.ReplEditor.CommandTarget.CanExecute(CommandConstants.TextEditorGroup, (int)TextEditorIds.TOGGLEWORDWRAP) == CommandTargetStatus.Handled;
		public override bool IsChecked(ReplEditorCtxMenuContext context) => (context.UI.TextView.Options.WordWrapStyle() & WordWrapStyles.WordWrap) != 0;
	}
}
