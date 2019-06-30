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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Properties;

namespace dnSpy.Bookmarks.ToolWindows.Bookmarks {
	[Export(typeof(IToolWindowContentProvider))]
	sealed class BookmarksToolWindowContentProvider : IToolWindowContentProvider {
		readonly Lazy<IBookmarksContent> bookmarksContent;

		public BookmarksToolWindowContent BookmarksToolWindowContent => bookmarksToolWindowContent ??= new BookmarksToolWindowContent(bookmarksContent);
		BookmarksToolWindowContent? bookmarksToolWindowContent;

		[ImportingConstructor]
		BookmarksToolWindowContentProvider(Lazy<IBookmarksContent> bookmarksContent) => this.bookmarksContent = bookmarksContent;

		public IEnumerable<ToolWindowContentInfo> ContentInfos {
			get { yield return new ToolWindowContentInfo(BookmarksToolWindowContent.THE_GUID, BookmarksToolWindowContent.DEFAULT_LOCATION, AppToolWindowConstants.DEFAULT_CONTENT_ORDER_BOTTOM_BOOKMARKS, false); }
		}

		public ToolWindowContent? GetOrCreate(Guid guid) => guid == BookmarksToolWindowContent.THE_GUID ? BookmarksToolWindowContent : null;
	}

	sealed class BookmarksToolWindowContent : ToolWindowContent, IFocusable {
		public static readonly Guid THE_GUID = new Guid("A13FFEAD-9F74-4456-9204-034EDBDE3244");
		public const AppToolWindowLocation DEFAULT_LOCATION = AppToolWindowLocation.DefaultHorizontal;

		public override IInputElement? FocusedElement => bookmarksContent.Value.FocusedElement;
		public override FrameworkElement? ZoomElement => bookmarksContent.Value.ZoomElement;
		public override Guid Guid => THE_GUID;
		public override string Title => dnSpy_Resources.Window_Bookmarks;
		public override object? UIObject => bookmarksContent.Value.UIObject;
		public bool CanFocus => true;

		readonly Lazy<IBookmarksContent> bookmarksContent;

		public BookmarksToolWindowContent(Lazy<IBookmarksContent> bookmarksContent) => this.bookmarksContent = bookmarksContent;

		public override void OnVisibilityChanged(ToolWindowContentVisibilityEvent visEvent) {
			switch (visEvent) {
			case ToolWindowContentVisibilityEvent.Added:
				bookmarksContent.Value.OnShow();
				break;
			case ToolWindowContentVisibilityEvent.Removed:
				bookmarksContent.Value.OnClose();
				break;
			case ToolWindowContentVisibilityEvent.Visible:
				bookmarksContent.Value.OnVisible();
				break;
			case ToolWindowContentVisibilityEvent.Hidden:
				bookmarksContent.Value.OnHidden();
				break;
			}
		}

		public void Focus() => bookmarksContent.Value.Focus();
	}
}
