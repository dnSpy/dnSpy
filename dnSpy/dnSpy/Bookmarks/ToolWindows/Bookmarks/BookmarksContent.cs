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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;

namespace dnSpy.Bookmarks.ToolWindows.Bookmarks {
	interface IBookmarksContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		void Focus();
		void FocusSearchTextBox();
		ListView ListView { get; }
		BookmarksOperations Operations { get; }
	}

	[Export(typeof(IBookmarksContent))]
	sealed class BookmarksContent : IBookmarksContent {
		public object? UIObject => bookmarksControl;
		public IInputElement? FocusedElement => bookmarksControl.ListView;
		public FrameworkElement? ZoomElement => bookmarksControl;
		public ListView ListView => bookmarksControl.ListView;
		public BookmarksOperations Operations { get; }

		readonly BookmarksControl bookmarksControl;
		readonly IBookmarksVM bookmarksVM;

		sealed class ControlVM : ViewModelBase {
			public IBookmarksVM VM { get; }
			BookmarksOperations Operations { get; }

			public string ToggleCreateBookmarkToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_ToggleCreateBookmark_ToolTip, null);
			public string GoToPreviousBookmarkToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_GoToPreviousBookmark_ToolTip, null);
			public string GoToNextBookmarkToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_GoToNextBookmark_ToolTip, null);
			public string GoToPreviousBookmarkSameLabelToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_GoToPreviousBookmarkSameLabel_ToolTip, null);
			public string GoToNextBookmarkSameLabelToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_GoToNextBookmarkSameLabel_ToolTip, null);
			public string GoToPreviousBookmarkInFileToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_GoToPreviousBookmarkInFile_ToolTip, null);
			public string GoToNextBookmarkInFileToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_GoToNextBookmarkInFile_ToolTip, null);

			public string RemoveBookmarkToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_RemoveBookmark_ToolTip, null);
			public string RemoveMatchingBookmarksToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_RemoveMatchingBookmarks_ToolTip, null);
			public string ToggleMatchingBookmarksToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_ToggleMatchingBookmarks_ToolTip, null);
			public string ExportMatchingBookmarksToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_ExportMatchingBookmarks_ToolTip, null);
			public string ImportBookmarksToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_ImportBookmarks_ToolTip, null);
			public string GoToLocationToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_GoToLocation_ToolTip, null);
			public string SearchToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_Search_ToolTip, dnSpy_Resources.ShortCutKeyCtrlF);
			public string ResetSearchSettingsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Bookmarks_ResetSearchSettings_ToolTip, null);
			public string SearchHelpToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.SearchHelp_ToolTip, null);

			public ICommand ToggleCreateBookmarkCommand => new RelayCommand(a => Operations.ToggleCreateBookmark(), a => Operations.CanToggleCreateBookmark);
			public ICommand GoToPreviousBookmarkCommand => new RelayCommand(a => Operations.SelectPreviousBookmark(), a => Operations.CanSelectPreviousBookmark);
			public ICommand GoToNextBookmarkCommand => new RelayCommand(a => Operations.SelectNextBookmark(), a => Operations.CanSelectNextBookmark);
			public ICommand GoToPreviousBookmarkSameLabelCommand => new RelayCommand(a => Operations.SelectPreviousBookmarkWithSameLabel(), a => Operations.CanSelectPreviousBookmarkWithSameLabel);
			public ICommand GoToNextBookmarkSameLabelCommand => new RelayCommand(a => Operations.SelectNextBookmarkWithSameLabel(), a => Operations.CanSelectNextBookmarkWithSameLabel);
			public ICommand GoToPreviousBookmarkInFileCommand => new RelayCommand(a => Operations.SelectPreviousBookmarkInDocument(), a => Operations.CanSelectPreviousBookmarkInDocument);
			public ICommand GoToNextBookmarkInFileCommand => new RelayCommand(a => Operations.SelectNextBookmarkInDocument(), a => Operations.CanSelectNextBookmarkInDocument);

			public ICommand RemoveBookmarksCommand => new RelayCommand(a => Operations.RemoveBookmarks(), a => Operations.CanRemoveBookmarks);
			public ICommand RemoveMatchingBookmarksCommand => new RelayCommand(a => Operations.RemoveMatchingBookmarks(), a => Operations.CanRemoveMatchingBookmarks);
			public ICommand ToggleMatchingBookmarksCommand => new RelayCommand(a => Operations.ToggleMatchingBookmarks(), a => Operations.CanToggleMatchingBookmarks);
			public ICommand ExportMatchingBookmarksCommand => new RelayCommand(a => Operations.ExportMatchingBookmarks(), a => Operations.CanExportMatchingBookmarks);
			public ICommand ResetSearchSettingsCommand => new RelayCommand(a => Operations.ResetSearchSettings(), a => Operations.CanResetSearchSettings);
			public ICommand ImportBookmarksCommand => new RelayCommand(a => Operations.ImportBookmarks(), a => Operations.CanImportBookmarks);
			public ICommand GoToLocationCommand => new RelayCommand(a => Operations.GoToLocation(false), a => Operations.CanGoToLocation);
			public ICommand SearchHelpCommand => new RelayCommand(a => SearchHelp());

			readonly IMessageBoxService messageBoxService;
			readonly DependencyObject control;

			public ControlVM(IBookmarksVM vm, BookmarksOperations operations, IMessageBoxService messageBoxService, DependencyObject control) {
				VM = vm;
				Operations = operations;
				this.messageBoxService = messageBoxService;
				this.control = control;
			}

			void SearchHelp() => messageBoxService.Show(VM.GetSearchHelpText(), ownerWindow: Window.GetWindow(control));
		}

		[ImportingConstructor]
		BookmarksContent(IWpfCommandService wpfCommandService, IBookmarksVM bookmarksVM, BookmarksOperations bookmarksOperations, IMessageBoxService messageBoxService) {
			Operations = bookmarksOperations;
			bookmarksControl = new BookmarksControl();
			this.bookmarksVM = bookmarksVM;
			bookmarksControl.DataContext = new ControlVM(bookmarksVM, bookmarksOperations, messageBoxService, bookmarksControl);
			bookmarksControl.BookmarksListViewDoubleClick += BookmarksControl_BookmarksListViewDoubleClick;

			wpfCommandService.Add(ControlConstants.GUID_BOOKMARKS_CONTROL, bookmarksControl);
			wpfCommandService.Add(ControlConstants.GUID_BOOKMARKS_LISTVIEW, bookmarksControl.ListView);

			bookmarksControl.ListView.PreviewKeyDown += ListView_PreviewKeyDown;
		}

		void BookmarksControl_BookmarksListViewDoubleClick(object? sender, EventArgs e) {
			bool newTab = Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control;
			if (!Operations.IsEditingValues && Operations.CanGoToLocation)
				Operations.GoToLocation(newTab);
		}

		void ListView_PreviewKeyDown(object? sender, KeyEventArgs e) {
			if (!e.Handled) {
				// Use a KeyDown handler. If we add this as a key command to the listview, the textview
				// (used when editing eg. labels) won't see the space.
				if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None) {
					if (Operations.CanToggleEnabled) {
						Operations.ToggleEnabled();
						e.Handled = true;
						return;
					}
				}
			}
		}

		public void FocusSearchTextBox() => bookmarksControl.FocusSearchTextBox();
		public void Focus() => UIUtilities.FocusSelector(bookmarksControl.ListView);
		public void OnClose() => bookmarksVM.IsOpen = false;
		public void OnShow() => bookmarksVM.IsOpen = true;
		public void OnHidden() => bookmarksVM.IsVisible = false;
		public void OnVisible() => bookmarksVM.IsVisible = true;
	}
}
