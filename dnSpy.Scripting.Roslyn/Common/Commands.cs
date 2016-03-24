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
using System.Windows.Input;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TextEditor;
using dnSpy.Shared.Menus;

namespace dnSpy.Scripting.Roslyn.Common {
	sealed class ReplEditorCtxMenuContext {
		public readonly IReplEditorUI UI;
		public readonly ScriptControlVM VM;

		public ReplEditorCtxMenuContext(IReplEditorUI ui) {
			this.UI = ui;
			this.VM = ScriptContent.GetScriptContent(ui).ScriptControlVM;
		}
	}

	abstract class ReplEditorCtxMenuCommand : MenuItemBase<ReplEditorCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override ReplEditorCtxMenuContext CreateContext(IMenuItemContext context) {
			return CreateContextInternal(context);
		}

		internal static ReplEditorCtxMenuContext CreateContextInternal(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_REPL_TEXTEDITORCONTROL_GUID))
				return null;
			var ui = context.Find<IReplEditorUI>();
			if (ui == null)
				return null;

			return new ReplEditorCtxMenuContext(ui);
		}
	}

	abstract class ReplEditorCtxMenuCommand2 : MenuItemCommand<ReplEditorCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override ReplEditorCtxMenuContext CreateContext(IMenuItemContext context) =>
			ReplEditorCtxMenuCommand.CreateContextInternal(context);

		protected ReplEditorCtxMenuCommand2(ICommand realCommand)
			: base(realCommand) {
		}
	}

	[ExportMenuItem(Header = "res:Script_ToolTip_Reset", Icon = "Reset", Group = MenuConstants.GROUP_CTX_REPL_RESET, Order = 0)]
	sealed class ResetReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.VM.Reset();
		public override bool IsEnabled(ReplEditorCtxMenuContext context) => context.VM.CanReset;
	}

	[ExportMenuItem(Header = "res:CutCommand", Icon = "Cut", InputGestureText = "res:ShortCutKeyCtrlX", Group = MenuConstants.GROUP_CTX_REPL_COPY, Order = 0)]
	sealed class CutReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand2 {
		CutReplEditorCtxMenuCommand()
			: base(ApplicationCommands.Cut) {
		}
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = "Copy", InputGestureText = "res:ShortCutKeyCtrlC", Group = MenuConstants.GROUP_CTX_REPL_COPY, Order = 10)]
	sealed class CopyReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand2 {
		CopyReplEditorCtxMenuCommand()
			: base(ApplicationCommands.Copy) {
		}
	}

	[ExportMenuItem(Header = "res:PasteCommand", Icon = "Paste", InputGestureText = "res:ShortCutKeyCtrlV", Group = MenuConstants.GROUP_CTX_REPL_COPY, Order = 20)]
	sealed class PasteReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand2 {
		PasteReplEditorCtxMenuCommand()
			: base(ApplicationCommands.Paste) {
		}
	}

	[ExportMenuItem(Header = "res:ClearScreenCommand", Icon = "ClearWindowContent", InputGestureText = "res:ShortCutKeyCtrlL", Group = MenuConstants.GROUP_CTX_REPL_CLEAR, Order = 0)]
	sealed class ClearReplEditorCtxMenuCommand : ReplEditorCtxMenuCommand {
		public override void Execute(ReplEditorCtxMenuContext context) => context.UI.Clear();
		public override bool IsEnabled(ReplEditorCtxMenuContext context) => context.UI.CanClear;
	}
}
