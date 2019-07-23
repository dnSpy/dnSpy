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

namespace dnSpy.Bookmarks.Impl {
	class BookmarkDisplaySettingsBase : BookmarkDisplaySettings {
		readonly object lockObj;

		protected BookmarkDisplaySettingsBase() => lockObj = new object();

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
		bool showTokens = true;

		public override bool ShowModuleNames {
			get {
				lock (lockObj)
					return showModuleNames;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showModuleNames != value;
					showModuleNames = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowModuleNames));
			}
		}
		bool showModuleNames = false;

		public override bool ShowParameterTypes {
			get {
				lock (lockObj)
					return showParameterTypes;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showParameterTypes != value;
					showParameterTypes = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowParameterTypes));
			}
		}
		bool showParameterTypes = true;

		public override bool ShowParameterNames {
			get {
				lock (lockObj)
					return showParameterNames;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showParameterNames != value;
					showParameterNames = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowParameterNames));
			}
		}
		bool showParameterNames = true;

		public override bool ShowDeclaringTypes {
			get {
				lock (lockObj)
					return showDeclaringTypes;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showDeclaringTypes != value;
					showDeclaringTypes = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowDeclaringTypes));
			}
		}
		bool showDeclaringTypes = true;

		public override bool ShowReturnTypes {
			get {
				lock (lockObj)
					return showReturnTypes;
			}
			set {
				bool modified;
				lock (lockObj) {
					modified = showReturnTypes != value;
					showReturnTypes = value;
				}
				if (modified)
					OnPropertyChanged(nameof(ShowReturnTypes));
			}
		}
		bool showReturnTypes = true;

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
		bool showNamespaces = false;

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
	}

	[Export(typeof(BookmarkDisplaySettings))]
	sealed class BookmarkDisplaySettingsImpl : BookmarkDisplaySettingsBase {
		static readonly Guid SETTINGS_GUID = new Guid("5E4C09BE-8239-4275-B797-DCBCE63AEFC4");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		BookmarkDisplaySettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			ShowTokens = sect.Attribute<bool?>(nameof(ShowTokens)) ?? ShowTokens;
			ShowModuleNames = sect.Attribute<bool?>(nameof(ShowModuleNames)) ?? ShowModuleNames;
			ShowParameterTypes = sect.Attribute<bool?>(nameof(ShowParameterTypes)) ?? ShowParameterTypes;
			ShowParameterNames = sect.Attribute<bool?>(nameof(ShowParameterNames)) ?? ShowParameterNames;
			ShowDeclaringTypes = sect.Attribute<bool?>(nameof(ShowDeclaringTypes)) ?? ShowDeclaringTypes;
			ShowReturnTypes = sect.Attribute<bool?>(nameof(ShowReturnTypes)) ?? ShowReturnTypes;
			ShowNamespaces = sect.Attribute<bool?>(nameof(ShowNamespaces)) ?? ShowNamespaces;
			ShowIntrinsicTypeKeywords = sect.Attribute<bool?>(nameof(ShowIntrinsicTypeKeywords)) ?? ShowIntrinsicTypeKeywords;
			PropertyChanged += BookmarkDisplaySettingsImpl_PropertyChanged;
		}

		void BookmarkDisplaySettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(ShowTokens), ShowTokens);
			sect.Attribute(nameof(ShowModuleNames), ShowModuleNames);
			sect.Attribute(nameof(ShowParameterTypes), ShowParameterTypes);
			sect.Attribute(nameof(ShowParameterNames), ShowParameterNames);
			sect.Attribute(nameof(ShowDeclaringTypes), ShowDeclaringTypes);
			sect.Attribute(nameof(ShowReturnTypes), ShowReturnTypes);
			sect.Attribute(nameof(ShowNamespaces), ShowNamespaces);
			sect.Attribute(nameof(ShowIntrinsicTypeKeywords), ShowIntrinsicTypeKeywords);
		}
	}
}
