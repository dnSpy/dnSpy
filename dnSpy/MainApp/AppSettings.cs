/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.ComponentModel.Composition;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Settings;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.MainApp {
	class AppSettings : ViewModelBase, IAppSettings {
		protected virtual void OnModified() {
		}

		public bool SyntaxHighlightFileTreeView {
			get { return syntaxHighlightFileTreeView; }
			set {
				if (syntaxHighlightFileTreeView != value) {
					syntaxHighlightFileTreeView = value;
					OnPropertyChanged("SyntaxHighlightFileTreeView");
					OnModified();
				}
			}
		}
		bool syntaxHighlightFileTreeView = true;

		public bool SingleClickExpandsTreeViewChildren {
			get { return singleClickExpandsTreeViewChildren; }
			set {
				if (singleClickExpandsTreeViewChildren != value) {
					singleClickExpandsTreeViewChildren = value;
					OnPropertyChanged("SingleClickExpandsTreeViewChildren");
					OnModified();
				}
			}
		}
		bool singleClickExpandsTreeViewChildren = true;

		public bool ShowAssemblyVersion {
			get { return showAssemblyVersion; }
			set {
				if (showAssemblyVersion != value) {
					showAssemblyVersion = value;
					OnPropertyChanged("ShowAssemblyVersion");
					OnModified();
				}
			}
		}
		bool showAssemblyVersion = true;

		public bool ShowAssemblyPublicKeyToken {
			get { return showAssemblyPublicKeyToken; }
			set {
				if (showAssemblyPublicKeyToken != value) {
					showAssemblyPublicKeyToken = value;
					OnPropertyChanged("ShowAssemblyPublicKeyToken");
					OnModified();
				}
			}
		}
		bool showAssemblyPublicKeyToken = false;

		public bool ShowToken {
			get { return showToken; }
			set {
				if (showToken != value) {
					showToken = value;
					OnPropertyChanged("ShowToken");
					OnModified();
				}
			}
		}
		bool showToken = true;

		public string LanguageName {
			get { return languageName; }
			protected set {
				if (languageName != value) {
					languageName = value;
					OnPropertyChanged("LanguageName");
					OnModified();
				}
			}
		}
		string languageName = "C#";

		public bool UseNewRenderer {
			get { return useNewRenderer; }
			set {
				if (useNewRenderer != value) {
					useNewRenderer = value;
					OnPropertyChanged("UseNewRenderer");
					OnModified();
				}
			}
		}
		bool useNewRenderer = false;
	}

	[Export, Export(typeof(IAppSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class AppSettingsImpl : AppSettings {
		const string SETTINGS_NAME = "071CF92D-ACFA-46A1-8EEF-DFAC1D01E644";

		public SavedWindowState SavedWindowState {
			get { return savedWindowState; }
			set {
				if (savedWindowState != value) {
					savedWindowState = value;
					OnPropertyChanged("SavedWindowState");
					OnModified();
				}
			}
		}
		SavedWindowState savedWindowState;

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		AppSettingsImpl(ISettingsManager settingsManager, ILanguageManager languageManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_NAME);
			this.SyntaxHighlightFileTreeView = sect.Attribute<bool?>("SyntaxHighlightFileTreeView") ?? this.SyntaxHighlightFileTreeView;
			this.SingleClickExpandsTreeViewChildren = sect.Attribute<bool?>("SingleClickExpandsTreeViewChildren") ?? this.SingleClickExpandsTreeViewChildren;
			this.ShowAssemblyVersion = sect.Attribute<bool?>("ShowAssemblyVersion") ?? this.ShowAssemblyVersion;
			this.ShowAssemblyPublicKeyToken = sect.Attribute<bool?>("ShowAssemblyPublicKeyToken") ?? this.ShowAssemblyPublicKeyToken;
			this.ShowToken = sect.Attribute<bool?>("ShowToken") ?? this.ShowToken;
			this.LanguageName = sect.Attribute<string>("LanguageName") ?? this.LanguageName;
			this.UseNewRenderer = sect.Attribute<bool?>("UseNewRenderer") ?? this.UseNewRenderer;
			this.SavedWindowState = new SavedWindowState().Read(sect.GetOrCreateSection("SavedWindowState"));
			this.disableSave = false;

			languageManager.LanguageChanged += LanguageManager_LanguageChanged;
			languageManager.SelectedLanguage = languageManager.FindOrDefault(this.LanguageName);
		}
		readonly bool disableSave;

		void LanguageManager_LanguageChanged(object sender, System.EventArgs e) {
			LanguageName = ((ILanguageManager)sender).SelectedLanguage.NameUI;
		}

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_NAME);
			sect.Attribute("SyntaxHighlightFileTreeView", SyntaxHighlightFileTreeView);
			sect.Attribute("SingleClickExpandsTreeViewChildren", SingleClickExpandsTreeViewChildren);
			sect.Attribute("ShowAssemblyVersion", ShowAssemblyVersion);
			sect.Attribute("ShowAssemblyPublicKeyToken", ShowAssemblyPublicKeyToken);
			sect.Attribute("ShowToken", ShowToken);
			sect.Attribute("LanguageName", LanguageName);
			sect.Attribute("UseNewRenderer", UseNewRenderer);
			SavedWindowState.Write(sect.GetOrCreateSection("SavedWindowState"));
		}
	}
}
