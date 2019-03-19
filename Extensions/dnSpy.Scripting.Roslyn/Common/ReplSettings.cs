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
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Scripting.Roslyn.Common {
	abstract class ReplSettings : ViewModelBase {
		protected virtual void OnModified() { }

		public WordWrapStyles WordWrapStyle {
			get => wordWrapStyle;
			set {
				if (wordWrapStyle != value) {
					wordWrapStyle = value;
					OnPropertyChanged(nameof(WordWrapStyle));
					OnModified();
				}
			}
		}
		WordWrapStyles wordWrapStyle = WordWrapStylesConstants.DefaultValue;

		public bool ShowLineNumbers {
			get => showLineNumbers;
			set {
				if (showLineNumbers != value) {
					showLineNumbers = value;
					OnPropertyChanged(nameof(ShowLineNumbers));
					OnModified();
				}
			}
		}
		bool showLineNumbers = true;
	}

	abstract class ReplSettingsImplBase : ReplSettings {
		readonly ISettingsService settingsService;
		readonly Guid guid;

		protected ReplSettingsImplBase(Guid guid, ISettingsService settingsService) {
			this.settingsService = settingsService;
			this.guid = guid;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(guid);
			WordWrapStyle = sect.Attribute<WordWrapStyles?>(nameof(WordWrapStyle)) ?? WordWrapStyle;
			ShowLineNumbers = sect.Attribute<bool?>(nameof(ShowLineNumbers)) ?? ShowLineNumbers;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(guid);
			sect.Attribute(nameof(WordWrapStyle), WordWrapStyle);
			sect.Attribute(nameof(ShowLineNumbers), ShowLineNumbers);
		}
	}
}
