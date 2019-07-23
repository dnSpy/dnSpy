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
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Settings;

namespace dnSpy.Bookmarks.Settings {
	class BookmarksSettingsBase : BookmarksSettings {
		readonly object lockObj;

		protected BookmarksSettingsBase() => lockObj = new object();

		public override bool SyntaxHighlight {
			get {
				lock (lockObj)
					return syntaxHighlight;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = syntaxHighlight != value;
					syntaxHighlight = value;
				}
				if (modified)
					OnPropertyChanged(nameof(SyntaxHighlight));
			}
		}
		bool syntaxHighlight = true;

		public BookmarksSettingsBase Clone() => CopyTo(new BookmarksSettingsBase());

		public BookmarksSettingsBase CopyTo(BookmarksSettingsBase other) {
			other.SyntaxHighlight = SyntaxHighlight;
			return other;
		}
	}

	[Export(typeof(BookmarksSettingsImpl))]
	[Export(typeof(BookmarksSettings))]
	sealed class BookmarksSettingsImpl : BookmarksSettingsBase {
		static readonly Guid SETTINGS_GUID = new Guid("0118143A-DAAA-4614-9DE4-034B028F791E");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		BookmarksSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			SyntaxHighlight = sect.Attribute<bool?>(nameof(SyntaxHighlight)) ?? SyntaxHighlight;
			PropertyChanged += BookmarksSettingsImpl_PropertyChanged;
		}

		void BookmarksSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(SyntaxHighlight), SyntaxHighlight);
		}
	}
}
