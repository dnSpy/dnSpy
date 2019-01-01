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
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Settings;

namespace dnSpy.Debugger.Breakpoints.Code {
	class DbgCodeBreakpointDisplaySettingsBase : DbgCodeBreakpointDisplaySettings {
		protected virtual void OnModified() { }

		readonly object lockObj;

		protected DbgCodeBreakpointDisplaySettingsBase() => lockObj = new object();

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
				if (modified) {
					OnPropertyChanged(nameof(ShowTokens));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(ShowModuleNames));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(ShowParameterTypes));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(ShowParameterNames));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(ShowDeclaringTypes));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(ShowReturnTypes));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(ShowNamespaces));
					OnModified();
				}
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
				if (modified) {
					OnPropertyChanged(nameof(ShowIntrinsicTypeKeywords));
					OnModified();
				}
			}
		}
		bool showIntrinsicTypeKeywords = true;
	}

	[Export(typeof(DbgCodeBreakpointDisplaySettings))]
	sealed class DbgCodeBreakpointDisplaySettingsImpl : DbgCodeBreakpointDisplaySettingsBase {
		static readonly Guid SETTINGS_GUID = new Guid("42CB1310-641D-4EB7-971D-16DC5CF9A40D");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		DbgCodeBreakpointDisplaySettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			ShowTokens = sect.Attribute<bool?>(nameof(ShowTokens)) ?? ShowTokens;
			ShowModuleNames = sect.Attribute<bool?>(nameof(ShowModuleNames)) ?? ShowModuleNames;
			ShowParameterTypes = sect.Attribute<bool?>(nameof(ShowParameterTypes)) ?? ShowParameterTypes;
			ShowParameterNames = sect.Attribute<bool?>(nameof(ShowParameterNames)) ?? ShowParameterNames;
			ShowDeclaringTypes = sect.Attribute<bool?>(nameof(ShowDeclaringTypes)) ?? ShowDeclaringTypes;
			ShowReturnTypes = sect.Attribute<bool?>(nameof(ShowReturnTypes)) ?? ShowReturnTypes;
			ShowNamespaces = sect.Attribute<bool?>(nameof(ShowNamespaces)) ?? ShowNamespaces;
			ShowIntrinsicTypeKeywords = sect.Attribute<bool?>(nameof(ShowIntrinsicTypeKeywords)) ?? ShowIntrinsicTypeKeywords;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
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
