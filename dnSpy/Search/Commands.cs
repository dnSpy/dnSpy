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
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.ToolBars;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Shared.Menus;
using dnSpy.Shared.ToolBars;

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

	[ExportToolBarButton(Icon = "Find", ToolTip = "res:SearchAssembliesToolBarToolTip", Group = ToolBarConstants.GROUP_APP_TB_MAIN_SEARCH, Order = 0)]
	sealed class SearchAssembliesToolBarButtonCommand : ToolBarButtonCommand {
		public SearchAssembliesToolBarButtonCommand()
			: base(SearchCommandLoader.SearchRoutedCommand) {
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:SearchAssembliesCommand", InputGestureText = "res:SearchAssembliesKey", Icon = "Find", Group = MenuConstants.GROUP_APP_MENU_EDIT_FIND, Order = 10)]
	sealed class SearchAssembliesMenuItemCommand : MenuItemCommand {
		public SearchAssembliesMenuItemCommand()
			: base(SearchCommandLoader.SearchRoutedCommand) {
		}
	}

	abstract class OpenReferenceCtxMenuCommandBase : MenuItemBase {
		readonly Lazy<ISearchManager> searchManager;
		readonly bool newTab;

		protected OpenReferenceCtxMenuCommandBase(Lazy<ISearchManager> searchManager, bool newTab) {
			this.searchManager = searchManager;
			this.newTab = newTab;
		}

		public override void Execute(IMenuItemContext context) {
			var res = GetReference(context);
			if (res == null)
				return;
			searchManager.Value.FollowResult(res, newTab);
		}

		public override bool IsVisible(IMenuItemContext context) {
			return GetReference(context) != null;
		}

		ISearchResult GetReference(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_SEARCH_GUID))
				return null;
			return context.Find<ISearchResult>();
		}
	}

	[ExportMenuItem(Header = "res:GoToReferenceCommand", InputGestureText = "res:GoToReferenceKey", Group = MenuConstants.GROUP_CTX_SEARCH_TABS, Order = 0)]
	sealed class OpenReferenceCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceCtxMenuCommand(Lazy<ISearchManager> searchManager)
			: base(searchManager, false) {
		}
	}

	[ExportMenuItem(Header = "res:OpenInNewTabCommand", InputGestureText = "res:OpenInNewTabKey4", Group = MenuConstants.GROUP_CTX_SEARCH_TABS, Order = 10)]
	sealed class OpenReferenceNewTabCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceNewTabCtxMenuCommand(Lazy<ISearchManager> searchManager)
			: base(searchManager, true) {
		}
	}

	[ExportMenuItem(Header = "res:SyntaxHighlightCommand", Group = MenuConstants.GROUP_CTX_SEARCH_OPTIONS, Order = 0)]
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
