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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Properties;

namespace dnSpy.Search {
	[Export(typeof(IMainToolWindowContentProvider))]
	sealed class SearchToolWindowContentProvider : IMainToolWindowContentProvider {
		readonly Lazy<ISearchManager> searchManager;

		SearchToolWindowContent SearchToolWindowContent => searchToolWindowContent ?? (searchToolWindowContent = new SearchToolWindowContent(searchManager));
		SearchToolWindowContent searchToolWindowContent;

		[ImportingConstructor]
		SearchToolWindowContentProvider(Lazy<ISearchManager> searchManager) {
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
		public static readonly Guid THE_GUID = new Guid("8E359BE0-C8CD-4CA7-B228-8C836219AF85");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public IInputElement FocusedElement => searchManager.Value.FocusedElement;
		public FrameworkElement ScaleElement => searchManager.Value.ScaleElement;
		public Guid Guid => THE_GUID;
		public string Title => dnSpy_Resources.SearchWindow_Title;
		public object ToolTip => null;
		public object UIObject => searchManager.Value.UIObject;
		public bool CanFocus => true;

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

		public void Focus() => searchManager.Value.Focus();
	}
}
