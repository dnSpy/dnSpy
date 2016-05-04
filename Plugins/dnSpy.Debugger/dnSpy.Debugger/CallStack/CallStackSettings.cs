/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnSpy.Shared.MVVM;

namespace dnSpy.Debugger.CallStack {
	interface ICallStackSettings : INotifyPropertyChanged {
		bool ShowModuleNames { get; }
		bool ShowParameterTypes { get; }
		bool ShowParameterNames { get; }
		bool ShowParameterValues { get; }
		bool ShowIP { get; }
		bool ShowOwnerTypes { get; }
		bool ShowNamespaces { get; }
		bool ShowTypeKeywords { get; }
		bool ShowTokens { get; }
		bool ShowReturnTypes { get; }
	}

	class CallStackSettings : ViewModelBase, ICallStackSettings {
		protected virtual void OnModified() { }

		public bool ShowModuleNames {
			get { return showModuleNames; }
			set {
				if (showModuleNames != value) {
					showModuleNames = value;
					OnPropertyChanged(nameof(ShowModuleNames));
					OnModified();
				}
			}
		}
		bool showModuleNames = true;

		public bool ShowParameterTypes {
			get { return showParameterTypes; }
			set {
				if (showParameterTypes != value) {
					showParameterTypes = value;
					OnPropertyChanged(nameof(ShowParameterTypes));
					OnModified();
				}
			}
		}
		bool showParameterTypes = true;

		public bool ShowParameterNames {
			get { return showParameterNames; }
			set {
				if (showParameterNames != value) {
					showParameterNames = value;
					OnPropertyChanged(nameof(ShowParameterNames));
					OnModified();
				}
			}
		}
		bool showParameterNames = true;

		public bool ShowParameterValues {
			get { return showParameterValues; }
			set {
				if (showParameterValues != value) {
					showParameterValues = value;
					OnPropertyChanged(nameof(ShowParameterValues));
					OnModified();
				}
			}
		}
		bool showParameterValues = false;

		public bool ShowIP {
			get { return showIP; }
			set {
				if (showIP != value) {
					showIP = value;
					OnPropertyChanged(nameof(ShowIP));
					OnModified();
				}
			}
		}
		bool showIP = true;

		public bool ShowOwnerTypes {
			get { return showOwnerTypes; }
			set {
				if (showOwnerTypes != value) {
					showOwnerTypes = value;
					OnPropertyChanged(nameof(ShowOwnerTypes));
					OnModified();
				}
			}
		}
		bool showOwnerTypes = true;

		public bool ShowNamespaces {
			get { return showNamespaces; }
			set {
				if (showNamespaces != value) {
					showNamespaces = value;
					OnPropertyChanged(nameof(ShowNamespaces));
					OnModified();
				}
			}
		}
		bool showNamespaces = true;

		public bool ShowTypeKeywords {
			get { return showTypeKeywords; }
			set {
				if (showTypeKeywords != value) {
					showTypeKeywords = value;
					OnPropertyChanged(nameof(ShowTypeKeywords));
					OnModified();
				}
			}
		}
		bool showTypeKeywords = true;

		public bool ShowTokens {
			get { return showTokens; }
			set {
				if (showTokens != value) {
					showTokens = value;
					OnPropertyChanged(nameof(ShowTokens));
					OnModified();
				}
			}
		}
		bool showTokens = false;

		public bool ShowReturnTypes {
			get { return showReturnTypes; }
			set {
				if (showReturnTypes != value) {
					showReturnTypes = value;
					OnPropertyChanged(nameof(ShowReturnTypes));
					OnModified();
				}
			}
		}
		bool showReturnTypes = false;
	}

	[Export, Export(typeof(ICallStackSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class CallStackSettingsImpl : CallStackSettings {
		static readonly Guid SETTINGS_GUID = new Guid("7280C4EB-1135-4F39-B6E0-57BD0A2454D6");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		CallStackSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			ShowModuleNames = sect.Attribute<bool?>(nameof(ShowModuleNames)) ?? ShowModuleNames;
			ShowParameterTypes = sect.Attribute<bool?>(nameof(ShowParameterTypes)) ?? ShowParameterTypes;
			ShowParameterNames = sect.Attribute<bool?>(nameof(ShowParameterNames)) ?? ShowParameterNames;
			ShowParameterValues = sect.Attribute<bool?>(nameof(ShowParameterValues)) ?? ShowParameterValues;
			ShowIP = sect.Attribute<bool?>(nameof(ShowIP)) ?? ShowIP;
			ShowOwnerTypes = sect.Attribute<bool?>(nameof(ShowOwnerTypes)) ?? ShowOwnerTypes;
			ShowNamespaces = sect.Attribute<bool?>(nameof(ShowNamespaces)) ?? ShowNamespaces;
			ShowTypeKeywords = sect.Attribute<bool?>(nameof(ShowTypeKeywords)) ?? ShowTypeKeywords;
			ShowTokens = sect.Attribute<bool?>(nameof(ShowTokens)) ?? ShowTokens;
			ShowReturnTypes = sect.Attribute<bool?>(nameof(ShowReturnTypes)) ?? ShowReturnTypes;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(ShowModuleNames), ShowModuleNames);
			sect.Attribute(nameof(ShowParameterTypes), ShowParameterTypes);
			sect.Attribute(nameof(ShowParameterNames), ShowParameterNames);
			sect.Attribute(nameof(ShowParameterValues), ShowParameterValues);
			sect.Attribute(nameof(ShowIP), ShowIP);
			sect.Attribute(nameof(ShowOwnerTypes), ShowOwnerTypes);
			sect.Attribute(nameof(ShowNamespaces), ShowNamespaces);
			sect.Attribute(nameof(ShowTypeKeywords), ShowTypeKeywords);
			sect.Attribute(nameof(ShowTokens), ShowTokens);
			sect.Attribute(nameof(ShowReturnTypes), ShowReturnTypes);
		}
	}
}
