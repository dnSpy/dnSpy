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
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.ToolBars;
using dnSpy.Shared.UI.Menus;
using dnSpy.Shared.UI.ToolBars;
using dnSpy.Tabs;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;

namespace dnSpy.Commands {
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "_Word Wrap", Icon = "WordWrap", InputGestureText = "Ctrl+Alt+W", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 0)]
	sealed class WordWrapCommand : MenuItemBase {
		public override bool IsChecked(IMenuItemContext context) {
			return MainWindow.Instance.SessionSettings.WordWrap;
		}

		public override void Execute(IMenuItemContext context) {
			MainWindow.Instance.SessionSettings.WordWrap = !MainWindow.Instance.SessionSettings.WordWrap;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "_Highlight Current Line", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 10)]
	sealed class HighlightCurrentLineCommand : MenuItemBase {
		public override bool IsChecked(IMenuItemContext context) {
			return MainWindow.Instance.SessionSettings.HighlightCurrentLine;
		}

		public override void Execute(IMenuItemContext context) {
			MainWindow.Instance.SessionSettings.HighlightCurrentLine = !MainWindow.Instance.SessionSettings.HighlightCurrentLine;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "F_ull Screen", InputGestureText = "Shift+Alt+Enter", Icon = "FullScreen", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 20)]
	sealed class FullScreenCommand : MenuItemBase {
		public override bool IsChecked(IMenuItemContext context) {
			return MainWindow.Instance.IsFullScreen;
		}

		public override void Execute(IMenuItemContext context) {
			MainWindow.Instance.IsFullScreen = !MainWindow.Instance.IsFullScreen;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Icon = "Save", Group = MenuConstants.GROUP_APP_MENU_FILE_SAVE, Order = 0)]
	sealed class MenuSaveCommand : MenuItemCommand {
		public MenuSaveCommand()
			: base(ApplicationCommands.Save) {
		}

		public override string GetHeader(IMenuItemContext context) {
			return GetHeaderInternal();
		}

		internal static string GetHeaderInternal() {
			return MainWindow.Instance.ActiveTabState is DecompileTabState ? "_Save Code..." : "_Save...";
		}
	}

	[ExportMenuItem(InputGestureText = "Ctrl+S", Icon = "Save", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 0)]
	sealed class SaveTabCtxMenuCommand : MenuItemCommand {
		public SaveTabCtxMenuCommand()
			: base(ApplicationCommands.Save) {
		}

		public override bool IsVisible(IMenuItemContext context) {
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_TABCONTROL_GUID) &&
				MainWindow.Instance.IsDecompilerTabControl(context.CreatorObject.Object as TabControl) &&
				base.IsVisible(context);
		}

		public override string GetHeader(IMenuItemContext context) {
			return MenuSaveCommand.GetHeaderInternal();
		}
	}

	[ExportMenuItem(Header = "Cop_y", Icon = "Copy", InputGestureText = "Ctrl+C", Group = MenuConstants.GROUP_CTX_CODE_EDITOR, Order = 0)]
	internal sealed class CopyCodeCtxMenuCommand : MenuItemCommand {
		public CopyCodeCtxMenuCommand()
			: base(ApplicationCommands.Copy) {
		}

		public override bool IsVisible(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID))
				return false;
			var textView = context.CreatorObject.Object as DecompilerTextView;
			return textView != null && textView.TextEditor.SelectionLength > 0 && base.IsVisible(context);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "E_xit", Icon = "Close", InputGestureText = "Alt+F4", Group = MenuConstants.GROUP_APP_MENU_FILE_EXIT, Order = 1000000)]
	sealed class MenuFileExitCommand : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			MainWindow.Instance.Close();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_FILE_GUID, Header = "_Open...", Icon = "Open", Group = MenuConstants.GROUP_APP_MENU_FILE_OPEN, Order = 0)]
	sealed class MenuFileOpenCommand : MenuItemCommand {
		public MenuFileOpenCommand()
			: base(ApplicationCommands.Open) {
		}
	}

	[ExportToolBarButton(OwnerGuid = ToolBarConstants.APP_TB_GUID, Icon = "Open", ToolTip = "Open (Ctrl+O)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_OPEN, Order = 0)]
	sealed class ToolbarFileOpenCommand : ToolBarButtonCommand {
		public ToolbarFileOpenCommand()
			: base(ApplicationCommands.Open) {
		}
	}
}
