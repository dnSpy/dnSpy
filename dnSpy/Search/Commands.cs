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
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.ToolBars;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Shared.UI.Menus;
using dnSpy.Shared.UI.ToolBars;

namespace dnSpy.Search {
	[ExportAutoLoaded]
	sealed class SearchCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand SearchRoutedCommand;

		static SearchCommandLoader() {
			SearchRoutedCommand = new RoutedCommand("SearchRoutedCommand", typeof(SearchCommandLoader));
			SearchRoutedCommand.InputGestures.Add(new KeyGesture(Key.K, ModifierKeys.Control));
		}

		readonly IMainToolWindowManager mainToolWindowManager;
		readonly Lazy<ISearchManager> searchManager;

		[ImportingConstructor]
		SearchCommandLoader(IMainToolWindowManager mainToolWindowManager, Lazy<ISearchManager> searchManager, IWpfCommandManager wpfCommandManager) {
			this.mainToolWindowManager = mainToolWindowManager;
			this.searchManager = searchManager;

			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			cmds.Add(SearchRoutedCommand, Search, CanSearch);
		}

		void CanSearch(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = true;
		}

		void Search(object sender, ExecutedRoutedEventArgs e) {
			mainToolWindowManager.Show(SearchToolWindowContent.THE_GUID);
		}
	}

	[ExportToolBarButton(Icon = "Find", ToolTip = "Search Assemblies (Ctrl+K)", Group = ToolBarConstants.GROUP_APP_TB_MAIN_SEARCH, Order = 0)]
	sealed class SearchAssembliesToolBarButtonCommand : ToolBarButtonCommand {
		public SearchAssembliesToolBarButtonCommand()
			: base(SearchCommandLoader.SearchRoutedCommand) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "_Search Assemblies", InputGestureText = "Ctrl+K", Icon = "Find", Group = MenuConstants.GROUP_APP_MENU_EDIT_FIND, Order = 10)]
	sealed class SearchAssembliesMenuItemCommand : MenuItemCommand {
		public SearchAssembliesMenuItemCommand()
			: base(SearchCommandLoader.SearchRoutedCommand) {
		}
	}

	abstract class OpenReferenceCtxMenuCommandBase : MenuItemBase {
		readonly IFileTabManager fileTabManager;
		readonly bool newTab;

		protected OpenReferenceCtxMenuCommandBase(IFileTabManager fileTabManager, bool newTab) {
			this.fileTabManager = fileTabManager;
			this.newTab = newTab;
		}

		public override void Execute(IMenuItemContext context) {
			var @ref = GetReference(context);
			if (@ref == null)
				return;
			fileTabManager.FollowReference(@ref, newTab);
			fileTabManager.SetFocus(fileTabManager.ActiveTab);
		}

		public override bool IsVisible(IMenuItemContext context) {
			return GetReference(context) != null;
		}

		object GetReference(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_SEARCH_GUID))
				return null;
			var @ref = context.Find<CodeReference>();
			return @ref == null ? null : @ref.Reference;
		}
	}

	[ExportMenuItem(Header = "Go to Reference", InputGestureText = "Dbl Click", Group = MenuConstants.GROUP_CTX_SEARCH_TABS, Order = 0)]
	sealed class OpenReferenceCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager, false) {
		}
	}

	[ExportMenuItem(Header = "Open in New _Tab", InputGestureText = "Shift+Dbl Click", Group = MenuConstants.GROUP_CTX_SEARCH_TABS, Order = 10)]
	sealed class OpenReferenceNewTabCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceNewTabCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager, true) {
		}
	}

	[ExportMenuItem(Header = "Syntax Highlight", Group = MenuConstants.GROUP_CTX_SEARCH_OPTIONS, Order = 0)]
	sealed class SyntaxHighlightCtxMenuCommand : MenuItemBase {
		readonly SearchSettingsImpl searchSettings;

		[ImportingConstructor]
		SyntaxHighlightCtxMenuCommand(SearchSettingsImpl searchSettings) {
			this.searchSettings = searchSettings;
		}

		public override bool IsVisible(IMenuItemContext context) {
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_SEARCH_GUID);
		}

		public override bool IsChecked(IMenuItemContext context) {
			return searchSettings.SyntaxHighlight;
		}

		public override void Execute(IMenuItemContext context) {
			searchSettings.SyntaxHighlight = !searchSettings.SyntaxHighlight;
		}
	}
}
