/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using dnSpy.Bookmarks.Impl;
using dnSpy.Bookmarks.TextEditor;
using dnSpy.Bookmarks.UI;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text;

namespace dnSpy.Bookmarks.ToolWindows.Bookmarks {
	abstract class BookmarksOperations {
		public abstract bool CanCopy { get; }
		public abstract void Copy();
		public abstract bool CanSelectAll { get; }
		public abstract void SelectAll();
		public abstract bool CanRemoveBookmarks { get; }
		public abstract void RemoveBookmarks();
		public abstract bool CanRemoveMatchingBookmarks { get; }
		public abstract void RemoveMatchingBookmarks();
		public abstract bool IsEditingValues { get; }
		public abstract bool CanToggleCreateBookmark { get; }
		public abstract void ToggleCreateBookmark();
		public abstract bool CanGoToPreviousBookmark { get; }
		public abstract void GoToPreviousBookmark();
		public abstract bool CanGoToNextBookmark { get; }
		public abstract void GoToNextBookmark();
		public abstract bool CanGoToPreviousBookmarkSameLabel { get; }
		public abstract void GoToPreviousBookmarkSameLabel();
		public abstract bool CanGoToNextBookmarkSameLabel { get; }
		public abstract void GoToNextBookmarkSameLabel();
		public abstract bool CanGoToPreviousBookmarkInFile { get; }
		public abstract void GoToPreviousBookmarkInFile();
		public abstract bool CanGoToNextBookmarkInFile { get; }
		public abstract void GoToNextBookmarkInFile();
		public abstract bool CanToggleEnabled { get; }
		public abstract void ToggleEnabled();
		public abstract bool CanToggleMatchingBookmarks { get; }
		public abstract void ToggleMatchingBookmarks();
		public abstract bool CanEnableBookmarks { get; }
		public abstract void EnableBookmarks();
		public abstract bool CanDisableBookmarks { get; }
		public abstract void DisableBookmarks();
		public abstract bool CanExportSelectedBookmarks { get; }
		public abstract void ExportSelectedBookmarks();
		public abstract bool CanExportMatchingBookmarks { get; }
		public abstract void ExportMatchingBookmarks();
		public abstract bool CanImportBookmarks { get; }
		public abstract void ImportBookmarks();
		public abstract bool CanResetSearchSettings { get; }
		public abstract void ResetSearchSettings();
		public abstract bool CanGoToLocation { get; }
		public abstract void GoToLocation();
		public abstract bool CanEditName { get; }
		public abstract void EditName();
		public abstract bool CanEditLabels { get; }
		public abstract void EditLabels();
		public abstract bool ShowTokens { get; set; }
		public abstract bool ShowModuleNames { get; set; }
		public abstract bool ShowParameterTypes { get; set; }
		public abstract bool ShowParameterNames { get; set; }
		public abstract bool ShowDeclaringTypes { get; set; }
		public abstract bool ShowReturnTypes { get; set; }
		public abstract bool ShowNamespaces { get; set; }
		public abstract bool ShowIntrinsicTypeKeywords { get; set; }
	}

	[Export(typeof(BookmarksOperations))]
	sealed class BookmarksOperationsImpl : BookmarksOperations {
		readonly IBookmarksVM bookmarksVM;
		readonly BookmarkDisplaySettings bookmarkDisplaySettings;
		readonly Lazy<BookmarksService> bookmarksService;
		readonly Lazy<BookmarkLocationSerializerService> bookmarkLocationSerializerService;
		readonly Lazy<ISettingsServiceFactory> settingsServiceFactory;
		readonly IPickFilename pickFilename;
		readonly IMessageBoxService messageBoxService;
		readonly Lazy<BookmarkSerializerService> bookmarkSerializerService;
		readonly Lazy<TextViewBookmarkService> textViewBookmarkService;

		BulkObservableCollection<BookmarkVM> AllItems => bookmarksVM.AllItems;
		ObservableCollection<BookmarkVM> SelectedItems => bookmarksVM.SelectedItems;
		//TODO: This should be view order
		IEnumerable<BookmarkVM> SortedSelectedItems => SelectedItems.OrderBy(a => a.Order);
		//TODO: This should be view order
		IEnumerable<BookmarkVM> SortedAllItems => AllItems.OrderBy(a => a.Order);

		[ImportingConstructor]
		BookmarksOperationsImpl(IBookmarksVM bookmarksVM, BookmarkDisplaySettings bookmarkDisplaySettings, Lazy<BookmarksService> bookmarksService, Lazy<BookmarkLocationSerializerService> bookmarkLocationSerializerService, Lazy<ISettingsServiceFactory> settingsServiceFactory, IPickFilename pickFilename, IMessageBoxService messageBoxService, Lazy<BookmarkSerializerService> bookmarkSerializerService, Lazy<TextViewBookmarkService> textViewBookmarkService) {
			this.bookmarksVM = bookmarksVM;
			this.bookmarkDisplaySettings = bookmarkDisplaySettings;
			this.bookmarksService = bookmarksService;
			this.bookmarkLocationSerializerService = bookmarkLocationSerializerService;
			this.settingsServiceFactory = settingsServiceFactory;
			this.pickFilename = pickFilename;
			this.messageBoxService = messageBoxService;
			this.bookmarkSerializerService = bookmarkSerializerService;
			this.textViewBookmarkService = textViewBookmarkService;
		}

		public override bool CanCopy => SelectedItems.Count != 0;
		public override void Copy() {
			var output = new StringBuilderTextColorOutput();
			foreach (var vm in SortedSelectedItems) {
				var formatter = vm.Context.Formatter;
				formatter.WriteName(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteLabels(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteLocation(output, vm);
				output.Write(BoxedTextColor.Text, "\t");
				formatter.WriteModule(output, vm);
				output.WriteLine();
			}
			var s = output.ToString();
			if (s.Length > 0) {
				try {
					Clipboard.SetText(s);
				}
				catch (ExternalException) { }
			}
		}

		public override bool CanSelectAll => SelectedItems.Count != AllItems.Count;
		public override void SelectAll() {
			SelectedItems.Clear();
			foreach (var vm in AllItems)
				SelectedItems.Add(vm);
		}

		public override bool CanRemoveBookmarks => SelectedItems.Count > 0;
		public override void RemoveBookmarks() {
			var bms = SelectedItems.Select(a => a.Bookmark).ToArray();
			bookmarksService.Value.Remove(bms);
		}

		public override bool CanRemoveMatchingBookmarks => AllItems.Count > 0;
		public override void RemoveMatchingBookmarks() => bookmarksService.Value.Remove(AllItems.Select(a => a.Bookmark).ToArray());

		public override bool IsEditingValues {
			get {
				foreach (var vm in SelectedItems) {
					if (vm.IsEditingValues)
						return true;
				}
				return false;
			}
		}

		public override bool CanToggleCreateBookmark => textViewBookmarkService.Value.CanToggleCreateBookmark;
		public override void ToggleCreateBookmark() => textViewBookmarkService.Value.ToggleCreateBookmark();

		public override bool CanGoToPreviousBookmark => true;//TODO:
		public override void GoToPreviousBookmark() {
			//TODO:
		}

		public override bool CanGoToNextBookmark => true;//TODO:
		public override void GoToNextBookmark() {
			//TODO:
		}

		public override bool CanGoToPreviousBookmarkSameLabel => true;//TODO:
		public override void GoToPreviousBookmarkSameLabel() {
			//TODO:
		}

		public override bool CanGoToNextBookmarkSameLabel => true;//TODO:
		public override void GoToNextBookmarkSameLabel() {
			//TODO:
		}

		public override bool CanGoToPreviousBookmarkInFile => true;//TODO:
		public override void GoToPreviousBookmarkInFile() {
			//TODO:
		}

		public override bool CanGoToNextBookmarkInFile => true;//TODO:
		public override void GoToNextBookmarkInFile() {
			//TODO:
		}

		public override bool CanToggleEnabled => SelectedItems.Count > 0 && !IsEditingValues;
		public override void ToggleEnabled() => ToggleBookmarks(SelectedItems);

		public override bool CanToggleMatchingBookmarks => AllItems.Count > 0;
		public override void ToggleMatchingBookmarks() => ToggleBookmarks(AllItems);

		public override bool CanEnableBookmarks => SelectedItems.Count != 0 && SelectedItems.Any(a => !a.IsEnabled);
		public override void EnableBookmarks() => EnableDisableBookmarks(SelectedItems, enable: true);

		public override bool CanDisableBookmarks => SelectedItems.Count != 0 && SelectedItems.Any(a => a.IsEnabled);
		public override void DisableBookmarks() => EnableDisableBookmarks(SelectedItems, enable: false);

		void ToggleBookmarks(IList<BookmarkVM> bookmarks) {
			// Toggling everything seems to be less useful, it's more likely that you'd want
			// to enable all selected bookmarks or disable all of them.
			bool allSet = bookmarks.All(a => a.IsEnabled);
			EnableDisableBookmarks(bookmarks, enable: !allSet);
		}

		void EnableDisableBookmarks(IList<BookmarkVM> bookmarks, bool enable) {
			var newSettings = new List<BookmarkAndSettings>(bookmarks.Count);
			for (int i = 0; i < bookmarks.Count; i++) {
				var vm = bookmarks[i];
				var settings = vm.Bookmark.Settings;
				if (settings.IsEnabled == enable)
					continue;
				settings.IsEnabled = enable;
				newSettings.Add(new BookmarkAndSettings(vm.Bookmark, settings));
			}
			if (newSettings.Count > 0)
				bookmarksService.Value.Modify(newSettings.ToArray());
		}

		public override bool CanExportSelectedBookmarks => SelectedItems.Count > 0;
		public override void ExportSelectedBookmarks() => SaveBookmarks(SortedSelectedItems);

		public override bool CanExportMatchingBookmarks => AllItems.Count > 0;
		public override void ExportMatchingBookmarks() => SaveBookmarks(SortedAllItems);

		void SaveBookmarks(IEnumerable<BookmarkVM> vms) {
			if (!vms.Any())
				return;
			bookmarkSerializerService.Value.Save(vms.Select(a => a.Bookmark).ToArray());
		}

		public override bool CanImportBookmarks => true;
		public override void ImportBookmarks() {
			var filename = pickFilename.GetFilename(null, "xml", PickFilenameConstants.XmlFilenameFilter);
			if (!File.Exists(filename))
				return;
			var settingsService = settingsServiceFactory.Value.Create();
			try {
				settingsService.Open(filename);
			}
			catch (Exception ex) {
				messageBoxService.Show(ex);
				return;
			}
			var bookmarks = new BookmarksSerializer(settingsService, bookmarkLocationSerializerService.Value).Load();
			bookmarksService.Value.Add(bookmarks);
		}

		public override bool CanResetSearchSettings => true;
		public override void ResetSearchSettings() => bookmarksVM.ResetSearchSettings();

		public override bool CanGoToLocation => SelectedItems.Count == 1;
		public override void GoToLocation() {
			if (!CanGoToLocation)
				return;
			//TODO:
		}

		public override bool CanEditName => SelectedItems.Count == 1 && !SelectedItems[0].NameEditableValue.IsEditingValue;
		public override void EditName() {
			if (!CanEditName)
				return;
			SelectedItems[0].ClearEditingValueProperties();
			SelectedItems[0].NameEditableValue.IsEditingValue = true;
		}

		public override bool CanEditLabels => SelectedItems.Count == 1 && !SelectedItems[0].LabelsEditableValue.IsEditingValue;
		public override void EditLabels() {
			if (!CanEditLabels)
				return;
			SelectedItems[0].ClearEditingValueProperties();
			SelectedItems[0].LabelsEditableValue.IsEditingValue = true;
		}

		public override bool ShowTokens {
			get => bookmarkDisplaySettings.ShowTokens;
			set => bookmarkDisplaySettings.ShowTokens = value;
		}

		public override bool ShowModuleNames {
			get => bookmarkDisplaySettings.ShowModuleNames;
			set => bookmarkDisplaySettings.ShowModuleNames = value;
		}

		public override bool ShowParameterTypes {
			get => bookmarkDisplaySettings.ShowParameterTypes;
			set => bookmarkDisplaySettings.ShowParameterTypes = value;
		}

		public override bool ShowParameterNames {
			get => bookmarkDisplaySettings.ShowParameterNames;
			set => bookmarkDisplaySettings.ShowParameterNames = value;
		}

		public override bool ShowDeclaringTypes {
			get => bookmarkDisplaySettings.ShowDeclaringTypes;
			set => bookmarkDisplaySettings.ShowDeclaringTypes = value;
		}

		public override bool ShowReturnTypes {
			get => bookmarkDisplaySettings.ShowReturnTypes;
			set => bookmarkDisplaySettings.ShowReturnTypes = value;
		}

		public override bool ShowNamespaces {
			get => bookmarkDisplaySettings.ShowNamespaces;
			set => bookmarkDisplaySettings.ShowNamespaces = value;
		}

		public override bool ShowIntrinsicTypeKeywords {
			get => bookmarkDisplaySettings.ShowIntrinsicTypeKeywords;
			set => bookmarkDisplaySettings.ShowIntrinsicTypeKeywords = value;
		}
	}
}
