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
using System.ComponentModel.Composition;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.ToolBars;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;

namespace dnSpy.Search {
	[ExportAutoLoaded]
	sealed class SearchCommandLoader : IAutoLoaded {
		public static readonly RoutedCommand SearchRoutedCommand;

		static SearchCommandLoader() {
			SearchRoutedCommand = new RoutedCommand("SearchRoutedCommand", typeof(SearchCommandLoader));
			SearchRoutedCommand.InputGestures.Add(new KeyGesture(Key.K, ModifierKeys.Control | ModifierKeys.Shift));
		}

		readonly IDsToolWindowService toolWindowService;
		readonly Lazy<ISearchService> searchService;

		[ImportingConstructor]
		SearchCommandLoader(IDsToolWindowService toolWindowService, Lazy<ISearchService> searchService, IWpfCommandService wpfCommandService) {
			this.toolWindowService = toolWindowService;
			this.searchService = searchService;

			var cmds = wpfCommandService.GetCommands(ControlConstants.GUID_MAINWINDOW);
			cmds.Add(SearchRoutedCommand, Search, CanSearch);
		}

		void CanSearch(object? sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;
		void Search(object? sender, ExecutedRoutedEventArgs e) => toolWindowService.Show(SearchToolWindowContent.THE_GUID);
	}

	[ExportToolBarButton(Icon = DsImagesAttribute.Search, Group = ToolBarConstants.GROUP_APP_TB_MAIN_SEARCH, Order = 0)]
	sealed class SearchAssembliesToolBarButtonCommand : ToolBarButtonCommand {
		public SearchAssembliesToolBarButtonCommand()
			: base(SearchCommandLoader.SearchRoutedCommand) {
		}

		public override string? GetToolTip(IToolBarItemContext context) =>
			ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.SearchAssembliesToolBarToolTip, dnSpy_Resources.ShortCutKeyCtrlShiftK);
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_EDIT_GUID, Header = "res:SearchAssembliesCommand", InputGestureText = "res:ShortCutKeyCtrlShiftK", Icon = DsImagesAttribute.Search, Group = MenuConstants.GROUP_APP_MENU_EDIT_FIND, Order = 10)]
	sealed class SearchAssembliesMenuItemCommand : MenuItemCommand {
		public SearchAssembliesMenuItemCommand()
			: base(SearchCommandLoader.SearchRoutedCommand) {
		}
	}

	abstract class OpenReferenceCtxMenuCommandBase : MenuItemBase {
		readonly Lazy<ISearchService> searchService;
		readonly bool newTab;

		protected OpenReferenceCtxMenuCommandBase(Lazy<ISearchService> searchService, bool newTab) {
			this.searchService = searchService;
			this.newTab = newTab;
		}

		public override void Execute(IMenuItemContext context) {
			var res = GetReference(context);
			if (res is null)
				return;
			searchService.Value.FollowResult(res, newTab);
		}

		public override bool IsVisible(IMenuItemContext context) => GetReference(context) is not null;

		ISearchResult? GetReference(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_SEARCH_GUID))
				return null;
			return context.Find<ISearchResult>();
		}
	}

	[ExportMenuItem(Header = "res:GoToReferenceCommand", InputGestureText = "res:GoToReferenceKey", Group = MenuConstants.GROUP_CTX_SEARCH_TABS, Order = 0)]
	sealed class OpenReferenceCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceCtxMenuCommand(Lazy<ISearchService> searchService)
			: base(searchService, false) {
		}
	}

	[ExportMenuItem(Header = "res:OpenInNewTabCommand", InputGestureText = "res:OpenInNewTabKey4", Group = MenuConstants.GROUP_CTX_SEARCH_TABS, Order = 10)]
	sealed class OpenReferenceNewTabCtxMenuCommand : OpenReferenceCtxMenuCommandBase {
		[ImportingConstructor]
		OpenReferenceNewTabCtxMenuCommand(Lazy<ISearchService> searchService)
			: base(searchService, true) {
		}
	}

	[ExportMenuItem(Header = "res:SyntaxHighlightCommand", Group = MenuConstants.GROUP_CTX_SEARCH_OPTIONS, Order = 0)]
	sealed class SyntaxHighlightCtxMenuCommand : MenuItemBase {
		readonly SearchSettingsImpl searchSettings;

		[ImportingConstructor]
		SyntaxHighlightCtxMenuCommand(SearchSettingsImpl searchSettings) => this.searchSettings = searchSettings;

		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_SEARCH_GUID);
		public override bool IsChecked(IMenuItemContext context) => searchSettings.SyntaxHighlight;
		public override void Execute(IMenuItemContext context) => searchSettings.SyntaxHighlight = !searchSettings.SyntaxHighlight;
	}

	[ExportMenuItem(Header = "res:SearchWindow_MatchWholeWords", Group = MenuConstants.GROUP_CTX_SEARCH_OPTIONS, Order = 10)]
	sealed class MatchWholeWordsCtxMenuCommand : MenuItemBase {
		readonly SearchSettingsImpl searchSettings;

		[ImportingConstructor]
		MatchWholeWordsCtxMenuCommand(SearchSettingsImpl searchSettings) => this.searchSettings = searchSettings;

		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_SEARCH_GUID);
		public override bool IsChecked(IMenuItemContext context) => searchSettings.MatchWholeWords;
		public override void Execute(IMenuItemContext context) => searchSettings.MatchWholeWords = !searchSettings.MatchWholeWords;
	}

	[ExportMenuItem(Header = "res:SearchWindow_CaseSensitiveSearch", Group = MenuConstants.GROUP_CTX_SEARCH_OPTIONS, Order = 20)]
	sealed class CaseSensitiveCtxMenuCommand : MenuItemBase {
		readonly SearchSettingsImpl searchSettings;

		[ImportingConstructor]
		CaseSensitiveCtxMenuCommand(SearchSettingsImpl searchSettings) => this.searchSettings = searchSettings;

		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_SEARCH_GUID);
		public override bool IsChecked(IMenuItemContext context) => searchSettings.CaseSensitive;
		public override void Execute(IMenuItemContext context) => searchSettings.CaseSensitive = !searchSettings.CaseSensitive;
	}

	[ExportMenuItem(Header = "res:SearchWindow_MatchAny", Group = MenuConstants.GROUP_CTX_SEARCH_OPTIONS, Order = 30)]
	sealed class MatchAnyCtxMenuCommand : MenuItemBase {
		readonly SearchSettingsImpl searchSettings;

		[ImportingConstructor]
		MatchAnyCtxMenuCommand(SearchSettingsImpl searchSettings) => this.searchSettings = searchSettings;

		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_SEARCH_GUID);
		public override bool IsChecked(IMenuItemContext context) => searchSettings.MatchAnySearchTerm;
		public override void Execute(IMenuItemContext context) => searchSettings.MatchAnySearchTerm = !searchSettings.MatchAnySearchTerm;
	}

	[ExportMenuItem(Header = "res:SearchWindow_DecompileResources", Group = MenuConstants.GROUP_CTX_SEARCH_OPTIONS, Order = 40)]
	sealed class DecompileResourcesCtxMenuCommand : MenuItemBase {
		readonly SearchSettingsImpl searchSettings;

		[ImportingConstructor]
		DecompileResourcesCtxMenuCommand(SearchSettingsImpl searchSettings) => this.searchSettings = searchSettings;

		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_SEARCH_GUID);
		public override bool IsChecked(IMenuItemContext context) => searchSettings.SearchDecompiledData;
		public override void Execute(IMenuItemContext context) => searchSettings.SearchDecompiledData = !searchSettings.SearchDecompiledData;
	}

	[ExportMenuItem(Header = "res:SearchWindow_SearchFrameworkAssemblies", Group = MenuConstants.GROUP_CTX_SEARCH_OPTIONS, Order = 50)]
	sealed class SearchFrameworkAssembliesCtxMenuCommand : MenuItemBase {
		readonly SearchSettingsImpl searchSettings;

		[ImportingConstructor]
		SearchFrameworkAssembliesCtxMenuCommand(SearchSettingsImpl searchSettings) => this.searchSettings = searchSettings;

		public override bool IsVisible(IMenuItemContext context) => context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_SEARCH_GUID);
		public override bool IsChecked(IMenuItemContext context) => searchSettings.SearchFrameworkAssemblies;
		public override void Execute(IMenuItemContext context) => searchSettings.SearchFrameworkAssemblies = !searchSettings.SearchFrameworkAssemblies;
	}
}
