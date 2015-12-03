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
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Shared.UI.Menus;

namespace dnSpy.Files.Tabs.TextEditor {
	[ExportAutoLoaded]
	sealed class WordWrapInit : IAutoLoaded {
		public static readonly RoutedCommand WordWrap = new RoutedCommand("WordWrap", typeof(WordWrapInit));

		[ImportingConstructor]
		WordWrapInit(IWpfCommandManager wpfCommandManager, TextEditorSettingsImpl textEditorSettings) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(WordWrap, (s, e) => textEditorSettings.WordWrap = !textEditorSettings.WordWrap, (s, e) => e.CanExecute = true, ModifierKeys.Control | ModifierKeys.Alt, Key.W);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "_Word Wrap", Icon = "WordWrap", InputGestureText = "Ctrl+Alt+W", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 0)]
	sealed class WordWrapCommand : MenuItemCommand {
		readonly TextEditorSettingsImpl textEditorSettings;

		[ImportingConstructor]
		WordWrapCommand(TextEditorSettingsImpl textEditorSettings)
			: base(WordWrapInit.WordWrap) {
			this.textEditorSettings = textEditorSettings;
		}

		public override bool IsChecked(IMenuItemContext context) {
			return textEditorSettings.WordWrap;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_VIEW_GUID, Header = "_Highlight Current Line", Group = MenuConstants.GROUP_APP_MENU_VIEW_OPTS, Order = 10)]
	sealed class HighlightCurrentLineCommand : MenuItemBase {
		readonly TextEditorSettingsImpl textEditorSettings;

		[ImportingConstructor]
		HighlightCurrentLineCommand(TextEditorSettingsImpl textEditorSettings) {
			this.textEditorSettings = textEditorSettings;
		}

		public override bool IsChecked(IMenuItemContext context) {
			return textEditorSettings.HighlightCurrentLine;
		}

		public override void Execute(IMenuItemContext context) {
			textEditorSettings.HighlightCurrentLine = !textEditorSettings.HighlightCurrentLine;
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
			var uiContext = context.FindByType<ITextEditorUIContext>();
			return uiContext != null && uiContext.HasSelectedText;
		}
	}

	[ExportAutoLoaded]
	sealed class FindInCodeInit : IAutoLoaded {
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		FindInCodeInit(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(ApplicationCommands.Find, Execute, CanExecute);
		}

		void Execute(object s, ExecutedRoutedEventArgs e) {
			var elem = GetInputElement();
			if (elem != null)
				ApplicationCommands.Find.Execute(null, elem);
		}

		void CanExecute(object s, CanExecuteRoutedEventArgs e) {
			e.CanExecute = GetInputElement() != null;
		}

		IInputElement GetInputElement() {
			var tab = fileTabManager.ActiveTab;
			if (tab == null)
				return null;
			var elem = tab.UIContext.FocusedElement ?? tab.UIContext.UIObject as IInputElement;
			return elem == null ? null : ApplicationCommands.Find.CanExecute(null, elem) ? elem : null;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "_Find", Icon = "Find", InputGestureText = "Ctrl+F", Group = MenuConstants.GROUP_APP_MENU_EDIT_FIND, Order = 0)]
	sealed class FindInCodeCommand : MenuItemCommand {
		public FindInCodeCommand()
			: base(ApplicationCommands.Find) {
		}
	}

	[ExportMenuItem(Header = "Find", Icon = "Find", InputGestureText = "Ctrl+F", Group = MenuConstants.GROUP_CTX_CODE_EDITOR, Order = 10)]
	sealed class FindInCodeContexMenuEntry : MenuItemCommand {
		FindInCodeContexMenuEntry()
			: base(ApplicationCommands.Find) {
		}

		public override bool IsVisible(IMenuItemContext context) {
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID);
		}
	}
}
