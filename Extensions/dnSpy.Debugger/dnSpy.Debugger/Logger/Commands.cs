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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Output;

namespace dnSpy.Debugger.Logger {
	sealed class LogEditorCtxMenuContext {
		public readonly IOutputTextPane TextPane;

		public LogEditorCtxMenuContext(IOutputTextPane pane) {
			TextPane = pane;
		}
	}

	abstract class LogEditorCtxMenuCommand : MenuItemBase<LogEditorCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override LogEditorCtxMenuContext CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_LOG_TEXTEDITORCONTROL_GUID))
				return null;
			var textPane = context.Find<IOutputTextPane>();
			if (textPane == null)
				return null;
			if (textPane.Guid != OutputLogger.GUID_OUTPUT_LOGGER_DEBUG)
				return null;

			return new LogEditorCtxMenuContext(textPane);
		}
	}

	[ExportMenuItem(Header = "res:ShowExceptionMessages", Group = MenuConstants.GROUP_CTX_OUTPUT_USER_COMMANDS, Order = 0)]
	sealed class ShowExceptionMessagesCtxMenuCommand : LogEditorCtxMenuCommand {
		readonly OutputLoggerSettingsImpl settings;

		[ImportingConstructor]
		ShowExceptionMessagesCtxMenuCommand(OutputLoggerSettingsImpl settings) {
			this.settings = settings;
		}

		public override bool IsChecked(LogEditorCtxMenuContext context) => settings.ShowExceptionMessages;
		public override void Execute(LogEditorCtxMenuContext context) => settings.ShowExceptionMessages = !settings.ShowExceptionMessages;
	}

	[ExportMenuItem(Header = "res:ShowStepFilteringMessages", Group = MenuConstants.GROUP_CTX_OUTPUT_USER_COMMANDS, Order = 10)]
	sealed class ShowStepFilteringMessagesCtxMenuCommand : LogEditorCtxMenuCommand {
		readonly OutputLoggerSettingsImpl settings;

		[ImportingConstructor]
		ShowStepFilteringMessagesCtxMenuCommand(OutputLoggerSettingsImpl settings) {
			this.settings = settings;
		}

		public override bool IsChecked(LogEditorCtxMenuContext context) => settings.ShowStepFilteringMessages;
		public override void Execute(LogEditorCtxMenuContext context) => settings.ShowStepFilteringMessages = !settings.ShowStepFilteringMessages;
	}

	[ExportMenuItem(Header = "res:ShowModuleLoadMessages", Group = MenuConstants.GROUP_CTX_OUTPUT_USER_COMMANDS, Order = 20)]
	sealed class ShowModuleLoadMessagesCtxMenuCommand : LogEditorCtxMenuCommand {
		readonly OutputLoggerSettingsImpl settings;

		[ImportingConstructor]
		ShowModuleLoadMessagesCtxMenuCommand(OutputLoggerSettingsImpl settings) {
			this.settings = settings;
		}

		public override bool IsChecked(LogEditorCtxMenuContext context) => settings.ShowModuleLoadMessages;
		public override void Execute(LogEditorCtxMenuContext context) => settings.ShowModuleLoadMessages = !settings.ShowModuleLoadMessages;
	}

	[ExportMenuItem(Header = "res:ShowModuleUnloadMessages", Group = MenuConstants.GROUP_CTX_OUTPUT_USER_COMMANDS, Order = 30)]
	sealed class ShowModuleUnloadMessagesCtxMenuCommand : LogEditorCtxMenuCommand {
		readonly OutputLoggerSettingsImpl settings;

		[ImportingConstructor]
		ShowModuleUnloadMessagesCtxMenuCommand(OutputLoggerSettingsImpl settings) {
			this.settings = settings;
		}

		public override bool IsChecked(LogEditorCtxMenuContext context) => settings.ShowModuleUnloadMessages;
		public override void Execute(LogEditorCtxMenuContext context) => settings.ShowModuleUnloadMessages = !settings.ShowModuleUnloadMessages;
	}

	[ExportMenuItem(Header = "res:ShowProcessExitMessages", Group = MenuConstants.GROUP_CTX_OUTPUT_USER_COMMANDS, Order = 40)]
	sealed class ShowProcessExitMessagesCtxMenuCommand : LogEditorCtxMenuCommand {
		readonly OutputLoggerSettingsImpl settings;

		[ImportingConstructor]
		ShowProcessExitMessagesCtxMenuCommand(OutputLoggerSettingsImpl settings) {
			this.settings = settings;
		}

		public override bool IsChecked(LogEditorCtxMenuContext context) => settings.ShowProcessExitMessages;
		public override void Execute(LogEditorCtxMenuContext context) => settings.ShowProcessExitMessages = !settings.ShowProcessExitMessages;
	}

	[ExportMenuItem(Header = "res:ShowThreadExitMessages", Group = MenuConstants.GROUP_CTX_OUTPUT_USER_COMMANDS, Order = 50)]
	sealed class ShowThreadExitMessagesCtxMenuCommand : LogEditorCtxMenuCommand {
		readonly OutputLoggerSettingsImpl settings;

		[ImportingConstructor]
		ShowThreadExitMessagesCtxMenuCommand(OutputLoggerSettingsImpl settings) {
			this.settings = settings;
		}

		public override bool IsChecked(LogEditorCtxMenuContext context) => settings.ShowThreadExitMessages;
		public override void Execute(LogEditorCtxMenuContext context) => settings.ShowThreadExitMessages = !settings.ShowThreadExitMessages;
	}

	[ExportMenuItem(Header = "res:ShowProgramOutputMessages", Group = MenuConstants.GROUP_CTX_OUTPUT_USER_COMMANDS, Order = 60)]
	sealed class ShowProgramOutputMessagesCtxMenuCommand : LogEditorCtxMenuCommand {
		readonly OutputLoggerSettingsImpl settings;

		[ImportingConstructor]
		ShowProgramOutputMessagesCtxMenuCommand(OutputLoggerSettingsImpl settings) {
			this.settings = settings;
		}

		public override bool IsChecked(LogEditorCtxMenuContext context) => settings.ShowProgramOutputMessages;
		public override void Execute(LogEditorCtxMenuContext context) => settings.ShowProgramOutputMessages = !settings.ShowProgramOutputMessages;
	}

	[ExportMenuItem(Header = "res:ShowMDAMessages", Group = MenuConstants.GROUP_CTX_OUTPUT_USER_COMMANDS, Order = 60)]
	sealed class ShowMDAMessagesCtxMenuCommand : LogEditorCtxMenuCommand {
		readonly OutputLoggerSettingsImpl settings;

		[ImportingConstructor]
		ShowMDAMessagesCtxMenuCommand(OutputLoggerSettingsImpl settings) {
			this.settings = settings;
		}

		public override bool IsChecked(LogEditorCtxMenuContext context) => settings.ShowMDAMessages;
		public override void Execute(LogEditorCtxMenuContext context) => settings.ShowMDAMessages = !settings.ShowMDAMessages;
	}
}
