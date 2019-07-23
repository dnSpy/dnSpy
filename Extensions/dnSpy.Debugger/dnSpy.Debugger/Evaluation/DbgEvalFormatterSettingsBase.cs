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
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Evaluation {
	abstract class DbgEvalFormatterSettingsBase : DbgEvalFormatterSettings {
		readonly object lockObj;

		protected DbgEvalFormatterSettingsBase() => lockObj = new object();

		public override bool ShowNamespaces {
			get {
				lock (lockObj)
					return showNamespaces;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showNamespaces != value;
					showNamespaces = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowNamespaces));
			}
		}
		bool showNamespaces = true;

		public override bool ShowIntrinsicTypeKeywords {
			get {
				lock (lockObj)
					return showIntrinsicTypeKeywords;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showIntrinsicTypeKeywords != value;
					showIntrinsicTypeKeywords = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowIntrinsicTypeKeywords));
			}
		}
		bool showIntrinsicTypeKeywords = true;

		public override bool ShowTokens {
			get {
				lock (lockObj)
					return showTokens;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showTokens != value;
					showTokens = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowTokens));
			}
		}
		bool showTokens = false;
	}

	[Export(typeof(DbgEvalFormatterSettings))]
	sealed class DbgEvalFormatterSettingsImpl : DbgEvalFormatterSettingsBase {
		static readonly Guid SETTINGS_GUID = new Guid("33608C69-6696-4721-8011-81ECCCC80C64");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		DbgEvalFormatterSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			ShowNamespaces = sect.Attribute<bool?>(nameof(ShowNamespaces)) ?? ShowNamespaces;
			ShowIntrinsicTypeKeywords = sect.Attribute<bool?>(nameof(ShowIntrinsicTypeKeywords)) ?? ShowIntrinsicTypeKeywords;
			ShowTokens = sect.Attribute<bool?>(nameof(ShowTokens)) ?? ShowTokens;
			PropertyChanged += DbgEvalFormatterSettingsImpl_PropertyChanged;
		}

		void DbgEvalFormatterSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(ShowNamespaces), ShowNamespaces);
			sect.Attribute(nameof(ShowIntrinsicTypeKeywords), ShowIntrinsicTypeKeywords);
			sect.Attribute(nameof(ShowTokens), ShowTokens);
		}
	}
}
