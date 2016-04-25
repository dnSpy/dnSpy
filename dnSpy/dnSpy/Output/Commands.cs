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
using dnSpy.Contracts.Output;
using dnSpy.Shared.Menus;

namespace dnSpy.Output {
	sealed class LogEditorCtxMenuContext {
		public readonly IOutputTextPane TextPane;
		public readonly IOutputManagerInternal Owner;

		public LogEditorCtxMenuContext(IOutputTextPane pane, IOutputManagerInternal outputManager) {
			this.TextPane = pane;
			this.Owner = outputManager;
		}
	}

	abstract class LogEditorCtxMenuCommand : MenuItemCommand<LogEditorCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override LogEditorCtxMenuContext CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_LOG_TEXTEDITORCONTROL_GUID))
				return null;
			var textPane = context.Find<IOutputTextPane>();
			if (textPane == null)
				return null;

			var outputManager = context.Find<IOutputManagerInternal>();
			if (outputManager == null)
				return null;

			return new LogEditorCtxMenuContext(textPane, outputManager);
		}

		protected LogEditorCtxMenuCommand(ICommand realCommand)
			: base(realCommand) {
		}
	}

	[ExportMenuItem(Header = "res:CopyCommand", Icon = "Copy", InputGestureText = "res:CopyKey", Group = MenuConstants.GROUP_CTX_OUTPUT_COPY, Order = 0)]
	sealed class CopyOutputEditorCtxMenuCommand : LogEditorCtxMenuCommand {
		CopyOutputEditorCtxMenuCommand()
			: base(ApplicationCommands.Copy) {
		}
	}

	[ExportMenuItem(Header = "res:ClearAllCommand", Icon = "ClearWindowContent", InputGestureText = "res:ShortCutKeyCtrlL", Group = MenuConstants.GROUP_CTX_OUTPUT_COPY, Order = 10)]
	sealed class ClearAllOutputEditorCtxMenuCommand : LogEditorCtxMenuCommand {
		ClearAllOutputEditorCtxMenuCommand()
			: base(OutputCommands.ClearAllCommand) {
		}
	}

	[ExportMenuItem(Header = "res:ShowLineNumbersCommand", Group = MenuConstants.GROUP_CTX_OUTPUT_SETTINGS, Order = 0)]
	sealed class ToggleShowLineNumbersOutputEditorCtxMenuCommand : LogEditorCtxMenuCommand {
		ToggleShowLineNumbersOutputEditorCtxMenuCommand()
			: base(OutputCommands.ToggleShowLineNumbersCommand) {
		}

		public override bool IsChecked(LogEditorCtxMenuContext context) => context.Owner.ShowLineNumbers;
	}

	[ExportMenuItem(Header = "res:ShowTimestampsCommand", Group = MenuConstants.GROUP_CTX_OUTPUT_SETTINGS, Order = 10)]
	sealed class ToggleShowTimestampsOutputEditorCtxMenuCommand : LogEditorCtxMenuCommand {
		ToggleShowTimestampsOutputEditorCtxMenuCommand()
			: base(OutputCommands.ToggleShowTimestampsCommand) {
		}

		public override bool IsChecked(LogEditorCtxMenuContext context) => context.Owner.ShowTimestamps;
	}

	[ExportMenuItem(Header = "res:WordWrapHeader", InputGestureText = "res:WordWrapKey", Group = MenuConstants.GROUP_CTX_OUTPUT_SETTINGS, Order = 20)]
	sealed class ToggleWordWrapOutputEditorCtxMenuCommand : LogEditorCtxMenuCommand {
		ToggleWordWrapOutputEditorCtxMenuCommand()
			: base(OutputCommands.ToggleWordWrapCommand) {
		}

		public override bool IsChecked(LogEditorCtxMenuContext context) => context.Owner.WordWrap;
	}
}
