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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using dnSpy.Bookmarks.UI;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;

namespace dnSpy.Bookmarks.ToolWindows.Bookmarks {
	sealed class BookmarkVM : ViewModelBase {
		public bool IsEnabled {
			get => settings.IsEnabled;
			set {
				if (settings.IsEnabled == value)
					return;
				Bookmark.IsEnabled = value;
			}
		}

		public bool IsActive {
			get => isActive;
			set {
				if (isActive == value)
					return;
				isActive = value;
				OnPropertyChanged(nameof(NameObject));
			}
		}
		bool isActive;

		public ImageReference ImageReference => BookmarkImageUtilities.GetImage(bookmarkKind);

		public IBookmarkContext Context { get; }
		public Bookmark Bookmark { get; }
		public object NameObject => new FormatterObject<BookmarkVM>(this, PredefinedTextClassifierTags.BookmarksWindowName);
		public object LabelsObject => new FormatterObject<BookmarkVM>(this, PredefinedTextClassifierTags.BookmarksWindowLabels);
		public object LocationObject => new FormatterObject<BookmarkVM>(this, PredefinedTextClassifierTags.BookmarksWindowLocation);
		public object ModuleObject => new FormatterObject<BookmarkVM>(this, PredefinedTextClassifierTags.BookmarksWindowModule);
		internal int Order { get; }

		public IEditableValue NameEditableValue { get; }
		public IEditValueProvider NameEditValueProvider { get; }
		public IEditableValue LabelsEditableValue { get; }
		public IEditValueProvider LabelsEditValueProvider { get; }

		BookmarkSettings settings;
		BookmarkKind bookmarkKind;

		internal BookmarkLocationFormatter BookmarkLocationFormatter { get; }

		public BookmarkVM(Bookmark bookmark, BookmarkLocationFormatter bookmarkLocationFormatter, IBookmarkContext context, int order, IEditValueProvider nameEditValueProvider, IEditValueProvider labelsEditValueProvider) {
			Bookmark = bookmark ?? throw new ArgumentNullException(nameof(bookmark));
			Context = context ?? throw new ArgumentNullException(nameof(context));
			Order = order;
			NameEditValueProvider = nameEditValueProvider ?? throw new ArgumentNullException(nameof(nameEditValueProvider));
			NameEditableValue = new EditableValueImpl(() => Bookmark.Name, s => Bookmark.Name = s ?? string.Empty);
			LabelsEditValueProvider = labelsEditValueProvider ?? throw new ArgumentNullException(nameof(labelsEditValueProvider));
			LabelsEditableValue = new EditableValueImpl(() => GetLabelsString(), s => Bookmark.Labels = CreateLabelsCollection(s));
			BookmarkLocationFormatter = bookmarkLocationFormatter ?? throw new ArgumentNullException(nameof(bookmarkLocationFormatter));
			settings = Bookmark.Settings;
			bookmarkKind = BookmarkImageUtilities.GetBookmarkKind(Bookmark);
			BookmarkLocationFormatter.PropertyChanged += BookmarkLocationFormatter_PropertyChanged;
		}

		internal static ReadOnlyCollection<string> CreateLabelsCollection(string? s) =>
			new ReadOnlyCollection<string>((s ?? string.Empty).Split(new[] { BookmarkFormatter.LabelsSeparatorChar }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray());

		// UI thread
		internal string GetLabelsString() {
			Context.UIDispatcher.VerifyAccess();
			var output = new StringBuilderTextColorOutput();
			Context.Formatter.WriteLabels(output, this);
			return output.ToString();
		}

		// random thread
		void UI(Action callback) => Context.UIDispatcher.UI(callback);

		// random thread
		void BookmarkLocationFormatter_PropertyChanged(object? sender, PropertyChangedEventArgs e) =>
			UI(() => BookmarkLocationFormatter_PropertyChanged_UI(e.PropertyName));

		// UI thread
		void BookmarkLocationFormatter_PropertyChanged_UI(string? propertyName) {
			Context.UIDispatcher.VerifyAccess();
			switch (propertyName) {
			case BookmarkLocationFormatter.LocationProperty:
				OnPropertyChanged(nameof(LocationObject));
				break;

			case BookmarkLocationFormatter.ModuleProperty:
				OnPropertyChanged(nameof(ModuleObject));
				break;

			default:
				Debug.Fail($"Unknown property: {propertyName}");
				break;
			}
		}

		// UI thread
		internal void RefreshThemeFields_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(NameObject));
			OnPropertyChanged(nameof(LabelsObject));
			OnPropertyChanged(nameof(LocationObject));
			OnPropertyChanged(nameof(ModuleObject));
		}

		// UI thread
		internal void RefreshLocationColumn_UI() {
			Context.UIDispatcher.VerifyAccess();
			OnPropertyChanged(nameof(LocationObject));
		}

		// UI thread
		internal void UpdateSettings_UI(BookmarkSettings newSettings) {
			Context.UIDispatcher.VerifyAccess();
			var oldSettings = settings;
			settings = newSettings;
			if (oldSettings.IsEnabled != newSettings.IsEnabled)
				OnPropertyChanged(nameof(IsEnabled));
			var newBookmarkKind = BookmarkImageUtilities.GetBookmarkKind(Bookmark);
			if (newBookmarkKind != bookmarkKind) {
				bookmarkKind = newBookmarkKind;
				OnPropertyChanged(nameof(ImageReference));
			}
			if (oldSettings.Name != newSettings.Name)
				OnPropertyChanged(nameof(NameObject));
			if (!LabelsEquals(oldSettings.Labels, newSettings.Labels))
				OnPropertyChanged(nameof(LabelsObject));
		}

		static bool LabelsEquals(ReadOnlyCollection<string> a, ReadOnlyCollection<string> b) {
			if (a is null)
				a = emptyLabels;
			if (b is null)
				b = emptyLabels;
			if (a == b)
				return true;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (!StringComparer.Ordinal.Equals(a[i], b[i]))
					return false;
			}
			return true;
		}
		static readonly ReadOnlyCollection<string> emptyLabels = new ReadOnlyCollection<string>(Array.Empty<string>());

		// UI thread
		internal void ClearEditingValueProperties() {
			Context.UIDispatcher.VerifyAccess();
			NameEditableValue.IsEditingValue = false;
			LabelsEditableValue.IsEditingValue = false;
		}

		// UI thread
		internal void Dispose() {
			Context.UIDispatcher.VerifyAccess();
			BookmarkLocationFormatter.PropertyChanged -= BookmarkLocationFormatter_PropertyChanged;
			BookmarkLocationFormatter.Dispose();
			ClearEditingValueProperties();
		}

		// UI thread
		internal bool IsEditingValues {
			get {
				Context.UIDispatcher.VerifyAccess();
				return NameEditableValue.IsEditingValue ||
					LabelsEditableValue.IsEditingValue;
			}
		}
	}
}
