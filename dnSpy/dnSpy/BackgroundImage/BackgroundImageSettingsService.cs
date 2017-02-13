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
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.BackgroundImage;
using dnSpy.Contracts.Settings;

namespace dnSpy.BackgroundImage {
	struct ImageSettingsInfo {
		public Lazy<IBackgroundImageOptionDefinition, IBackgroundImageOptionDefinitionMetadata> Lazy { get; }
		public RawSettings RawSettings { get; }
		public ImageSettingsInfo(Lazy<IBackgroundImageOptionDefinition, IBackgroundImageOptionDefinitionMetadata> lazy, RawSettings rawSettings) {
			Lazy = lazy;
			RawSettings = rawSettings;
		}
	}

	interface IBackgroundImageSettingsService {
		IBackgroundImageSettings GetSettings(Lazy<IBackgroundImageOptionDefinition, IBackgroundImageOptionDefinitionMetadata> lazySettings);
		ImageSettingsInfo[] GetRawSettings();
		void SetRawSettings(RawSettings[] settings);
		string LastSelectedId { get; set; }
	}

	[Export(typeof(IBackgroundImageSettingsService))]
	sealed class BackgroundImageSettingsService : IBackgroundImageSettingsService {
		static readonly Guid SETTINGS_GUID = new Guid("7CAEF193-7F6A-4710-8E47-547A71D6FDBC");
		const string SettingsName = "Settings";

		public string LastSelectedId { get; set; }

		readonly ISettingsService settingsService;
		readonly Dictionary<string, SettingsInfo> settingsInfos;

		sealed class SettingsInfo {
			public Lazy<IBackgroundImageOptionDefinition, IBackgroundImageOptionDefinitionMetadata> Lazy { get; }
			public ISettingsSection SettingsSection { get; set; }
			public BackgroundImageSettings BackgroundImageSettings { get; }
			public RawSettings RawSettings { get; }

			public SettingsInfo(Lazy<IBackgroundImageOptionDefinition, IBackgroundImageOptionDefinitionMetadata> lazy) {
				Lazy = lazy;
				var defaultSettings = lazy.Value.GetDefaultImageSettings();
				RawSettings = defaultSettings == null ? new RawSettings(lazy.Value.Id) : new RawSettings(lazy.Value.Id, defaultSettings);
				BackgroundImageSettings = new BackgroundImageSettings(RawSettings);
			}
		}

		[ImportingConstructor]
		BackgroundImageSettingsService(ISettingsService settingsService, IBackgroundImageOptionDefinitionService backgroundImageOptionDefinitionService) {
			this.settingsService = settingsService;
			settingsInfos = new Dictionary<string, SettingsInfo>(backgroundImageOptionDefinitionService.AllSettings.Length, StringComparer.Ordinal);

			foreach (var lazy in backgroundImageOptionDefinitionService.AllSettings) {
				if (settingsInfos.ContainsKey(lazy.Value.Id))
					continue;
				settingsInfos[lazy.Value.Id] = new SettingsInfo(lazy);
			}

			var allSettingsIds = new HashSet<string>(backgroundImageOptionDefinitionService.AllSettings.Select(a => a.Value.Id), StringComparer.Ordinal);
			var rootSection = settingsService.GetOrCreateSection(SETTINGS_GUID);
			foreach (var section in rootSection.SectionsWithName(SettingsName)) {
				var rawSettings = new RawSettings(section);
				if (!rawSettings.IsValid)
					continue;
				if (!settingsInfos.TryGetValue(rawSettings.Id, out var info))
					continue;
				if (!allSettingsIds.Contains(rawSettings.Id))
					continue;
				allSettingsIds.Remove(rawSettings.Id);
				if (info.Lazy.Value.UserVisible) {
					info.SettingsSection = section;
					info.RawSettings.CopyFrom(rawSettings);
				}
			}
		}

		public IBackgroundImageSettings GetSettings(Lazy<IBackgroundImageOptionDefinition, IBackgroundImageOptionDefinitionMetadata> lazySettings) =>
			settingsInfos[lazySettings.Value.Id].BackgroundImageSettings;

		public ImageSettingsInfo[] GetRawSettings() {
			var list = new List<ImageSettingsInfo>(settingsInfos.Count);
			foreach (var info in settingsInfos.Values) {
				if (info.Lazy.Value.UserVisible)
					list.Add(new ImageSettingsInfo(info.Lazy, info.RawSettings.Clone()));
			}
			return list.ToArray();
		}

		public void SetRawSettings(RawSettings[] settings) {
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			foreach (var rs in settings) {
				if (rs.Id == null)
					continue;
				if (!settingsInfos.TryGetValue(rs.Id, out var info))
					continue;
				if (info.RawSettings.Equals(rs))
					continue;
				info.RawSettings.CopyFrom(rs);
				info.BackgroundImageSettings.RaiseSettingsChanged();
				if (info.Lazy.Value.UserVisible) {
					var rootSection = settingsService.GetOrCreateSection(SETTINGS_GUID);
					if (info.SettingsSection != null)
						rootSection.RemoveSection(info.SettingsSection);
					info.SettingsSection = rootSection.CreateSection(SettingsName);
					info.RawSettings.SaveSettings(info.SettingsSection);
				}
			}
		}
	}
}
