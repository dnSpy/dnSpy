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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.App;
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

		readonly TextEditorSettingsImpl textEditorSettings;
		readonly IAppSettings appSettings;
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		WordWrapInit(IAppWindow appWindow, TextEditorSettingsImpl textEditorSettings, IAppSettings appSettings, IMessageBoxManager messageBoxManager) {
			this.textEditorSettings = textEditorSettings;
			this.appSettings = appSettings;
			this.messageBoxManager = messageBoxManager;
			appWindow.MainWindowCommands.Add(WordWrap, (s, e) => ToggleWordWrap(), (s, e) => e.CanExecute = true, ModifierKeys.Control | ModifierKeys.Alt, Key.W);
		}

		void ToggleWordWrap() {
			textEditorSettings.WordWrap = !textEditorSettings.WordWrap;
			if (textEditorSettings.WordWrap && appSettings.UseNewRenderer_TextEditor)
				messageBoxManager.ShowIgnorableMessage("text editor word wrap new renderer", "The text editor is using the new faster text formatter. It doesn't support word wrap or all unicode characters. Enable/disable it in the options.");
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
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
				return false;
			var uiContext = context.Find<ITextEditorUIContext>();
			return uiContext != null && uiContext.HasSelectedText;
		}
	}

	[ExportAutoLoaded]
	sealed class FindInCodeInit : IAutoLoaded {
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		FindInCodeInit(IAppWindow appWindow, IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
			appWindow.MainWindowCommands.Add(ApplicationCommands.Find, Execute, CanExecute);
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
			return elem != null && tab.TryGetTextEditorUIContext() != null && ApplicationCommands.Find.CanExecute(null, elem) ? elem : null;
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
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID);
		}
	}

	[ExportAutoLoaded]
	sealed class GoToCommand : IAutoLoaded {
		static readonly RoutedCommand GoToRoutedCommand = new RoutedCommand("GoToRoutedCommand", typeof(GoToCommand));
		readonly IFileTabManager fileTabManager;
		readonly IMessageBoxManager messageBoxManager;

		[ImportingConstructor]
		GoToCommand(IWpfCommandManager wpfCommandManager, IFileTabManager fileTabManager, IMessageBoxManager messageBoxManager) {
			this.fileTabManager = fileTabManager;
			this.messageBoxManager = messageBoxManager;
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_TEXTEDITOR_UICONTEXT);
			cmds.Add(GoToRoutedCommand, Execute, CanExecute, ModifierKeys.Control, Key.G);
		}

		void Execute(object s, ExecutedRoutedEventArgs e) {
			GoTo();
		}

		void CanExecute(object s, CanExecuteRoutedEventArgs e) {
			e.CanExecute = fileTabManager.ActiveTab != null && fileTabManager.ActiveTab.UIContext is ITextEditorUIContext;
		}

		void GoTo() {
			var tab = fileTabManager.ActiveTab;
			var uiContext = tab == null ? null : tab.UIContext as ITextEditorUIContext;
			if (uiContext == null)
				return;

			var res = messageBoxManager.Ask<Tuple<int, int>>("_Line [, column]", null, "Go to Line", s => {
				int? line, column;
				TryGetRowCol(s, uiContext.Location.Line, out line, out column);
				return Tuple.Create(line.Value, column.Value);
			}, s => {
				int? line, column;
				return TryGetRowCol(s, uiContext.Location.Line, out line, out column);
			});
			if (res != null)
				uiContext.ScrollAndMoveCaretTo(res.Item1, res.Item2);
		}

		string TryGetRowCol(string s, int currentLine, out int? line, out int? column) {
			line = null;
			column = null;
			Match match;
			if ((match = goToLineRegex1.Match(s)) != null && match.Groups.Count == 4) {
				line = TryParse(match.Groups[1].Value);
				column = match.Groups[3].Value != string.Empty ? TryParse(match.Groups[3].Value) : 1;
			}
			else if ((match = goToLineRegex2.Match(s)) != null && match.Groups.Count == 2) {
				line = currentLine;
				column = TryParse(match.Groups[1].Value);
			}
			if (line == null || column == null) {
				if (string.IsNullOrWhiteSpace(s))
					return "Enter a line number";
				return string.Format("Invalid line: {0}", s);
			}
			return string.Empty;
		}
		static readonly Regex goToLineRegex1 = new Regex(@"^\s*(\d+)\s*(,\s*(\d+))?\s*$");
		static readonly Regex goToLineRegex2 = new Regex(@"^\s*,\s*(\d+)\s*$");

		static int? TryParse(string valText) {
			int val;
			return int.TryParse(valText, out val) ? (int?)val : null;
		}
	}
}
