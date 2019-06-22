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
using System.Collections.ObjectModel;
using System.Linq;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Settings;

namespace dnSpy.Bookmarks.Impl {
	readonly struct BookmarksSerializer {
		static readonly Guid SETTINGS_GUID = new Guid("EAA1BE38-7A55-44AF-AD93-5B7EE2327EDD");

		readonly ISettingsService settingsService;
		readonly BookmarkLocationSerializerService bookmarkLocationSerializerService;

		public BookmarksSerializer(ISettingsService settingsService, BookmarkLocationSerializerService bookmarkLocationSerializerService) {
			this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
			this.bookmarkLocationSerializerService = bookmarkLocationSerializerService ?? throw new ArgumentNullException(nameof(bookmarkLocationSerializerService));
		}

		public BookmarkInfo[] Load() {
			var section = settingsService.GetOrCreateSection(SETTINGS_GUID);
			var settings = new List<BookmarkInfo>();
			foreach (var bmSect in section.SectionsWithName("Bookmark")) {
				var isEnabled = bmSect.Attribute<bool?>("IsEnabled");
				if (isEnabled is null)
					continue;
				var location = bookmarkLocationSerializerService.Deserialize(bmSect.TryGetSection("BML"));
				if (location is null)
					continue;
				var bmSettings = new BookmarkSettings {
					IsEnabled = isEnabled.Value,
					Name = bmSect.Attribute<string>("Name") ?? string.Empty,
					Labels = new ReadOnlyCollection<string>(LoadLabels(bmSect)),
				};
				settings.Add(new BookmarkInfo(location, bmSettings));
			}
			return settings.ToArray();
		}

		string[] LoadLabels(ISettingsSection section) {
			var labels = section.Attribute<string>("Labels") ?? string.Empty;
			return labels.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim()).ToArray();
		}

		public void Save(Bookmark[] bookmarks) {
			var section = settingsService.RecreateSection(SETTINGS_GUID);
			foreach (var bm in bookmarks.OrderBy(a => a.Id)) {
				var bmSect = section.CreateSection("Bookmark");
				var bmSettings = bm.Settings;
				bmSect.Attribute("IsEnabled", bmSettings.IsEnabled);
				bookmarkLocationSerializerService.Serialize(bmSect.CreateSection("BML"), bm.Location);
				bmSect.Attribute("Name", bm.Name ?? string.Empty);
				if (!(bmSettings.Labels is null) && bmSettings.Labels.Count != 0)
					SaveLabels(bmSect, bmSettings.Labels.ToArray());
			}
		}

		void SaveLabels(ISettingsSection section, string[] labels) {
			if (labels is null || labels.Length == 0)
				return;
			section.Attribute("Labels", string.Join(", ", labels));
		}
	}
}
