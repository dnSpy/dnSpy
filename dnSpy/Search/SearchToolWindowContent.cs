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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Properties;
using dnSpy.Shared.MVVM;

namespace dnSpy.Search {
	[ExportAutoLoaded]
	sealed class SearchToolCommandLoader : IAutoLoaded {
		[ImportingConstructor]
		SearchToolCommandLoader(IMainToolWindowManager mainToolWindowManager, Lazy<SearchToolWindowContentCreator> searchToolWindowContentCreator, IWpfCommandManager wpfCommandManager) {
			var cmds = wpfCommandManager.GetCommands(CommandConstants.GUID_SEARCH_CONTROL);
			cmds.Add(new RelayCommand(a => mainToolWindowManager.Close(searchToolWindowContentCreator.Value.SearchToolWindowContent)), ModifierKeys.None, Key.Escape);
		}
	}

	[Export, Export(typeof(IMainToolWindowContentCreator))]
	sealed class SearchToolWindowContentCreator : IMainToolWindowContentCreator {
		readonly Lazy<ISearchManager> searchManager;

		public SearchToolWindowContent SearchToolWindowContent {
			get { return searchToolWindowContent ?? (searchToolWindowContent = new SearchToolWindowContent(searchManager)); }
		}
		SearchToolWindowContent searchToolWindowContent;

		[ImportingConstructor]
		SearchToolWindowContentCreator(Lazy<ISearchManager> searchManager) {
			this.searchManager = searchManager;
		}

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(SearchToolWindowContent.THE_GUID, SearchToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_TOP_SEARCH, false); }
		}

		public IToolWindowContent GetOrCreate(Guid guid) {
			if (guid == SearchToolWindowContent.THE_GUID)
				return SearchToolWindowContent;
			return null;
		}
	}

	sealed class SearchToolWindowContent : IToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("91802684-9D1F-4491-90FD-AFE1DE7C4D46");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.Top;

		public IInputElement FocusedElement {
			get { return searchManager.Value.FocusedElement; }
		}

		public FrameworkElement ScaleElement {
			get { return searchManager.Value.ScaleElement; }
		}

		public Guid Guid {
			get { return THE_GUID; }
		}

		public string Title {
			get { return dnSpy_Resources.SearchWindow_Title; }
		}

		public object ToolTip {
			get { return null; }
		}

		public object UIObject {
			get { return searchManager.Value.UIObject; }
		}

		public bool CanFocus {
			get { return true; }
		}

		readonly Lazy<ISearchManager> searchManager;

		public SearchToolWindowContent(Lazy<ISearchManager> searchManager) {
			this.searchManager = searchManager;
		}

		public void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			if (visEvent == ToolWindowContentVisibilityEvent.Removed)
				searchManager.Value.OnClose();
			else if (visEvent == ToolWindowContentVisibilityEvent.Added)
				searchManager.Value.OnShow();
		}

		public void Focus() {
			searchManager.Value.Focus();
		}
	}
}
