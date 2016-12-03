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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.Themes;
using dnSpy.Documents;
using dnSpy.Documents.Tabs;
using dnSpy.Documents.TreeView;
using dnSpy.Properties;
using dnSpy.Themes;

namespace dnSpy.MainApp.Settings {
	[Export(typeof(IAppSettingsPageProvider))]
	sealed class GeneralAppSettingsPageProvider : IAppSettingsPageProvider {
		readonly IThemeServiceImpl themeService;
		readonly IWindowsExplorerIntegrationService windowsExplorerIntegrationService;
		readonly IDocumentTabServiceSettings documentTabServiceSettings;
		readonly DocumentTreeViewSettingsImpl documentTreeViewSettings;
		readonly IDsDocumentServiceSettings documentServiceSettings;
		readonly AppSettingsImpl appSettings;
		readonly MessageBoxService messageBoxService;

		[ImportingConstructor]
		GeneralAppSettingsPageProvider(IThemeServiceImpl themeService, IWindowsExplorerIntegrationService windowsExplorerIntegrationService, IDocumentTabServiceSettings documentTabServiceSettings, DocumentTreeViewSettingsImpl documentTreeViewSettings, IDsDocumentServiceSettings documentServiceSettings, AppSettingsImpl appSettings, MessageBoxService messageBoxService) {
			this.themeService = themeService;
			this.windowsExplorerIntegrationService = windowsExplorerIntegrationService;
			this.documentTabServiceSettings = documentTabServiceSettings;
			this.documentTreeViewSettings = documentTreeViewSettings;
			this.documentServiceSettings = documentServiceSettings;
			this.appSettings = appSettings;
			this.messageBoxService = messageBoxService;
		}

		public IEnumerable<AppSettingsPage> Create() {
			yield return new GeneralAppSettingsPage(themeService, windowsExplorerIntegrationService, documentTabServiceSettings, documentTreeViewSettings, documentServiceSettings, appSettings, messageBoxService);
		}
	}

	sealed class GeneralAppSettingsPage : AppSettingsPage, IAppSettingsPage2, INotifyPropertyChanged {
		public override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_ENVIRONMENT);
		public override Guid Guid => new Guid("776184ED-10F6-466C-8B66-716936C29A5A");
		public override double Order => AppSettingsConstants.ORDER_ENVIRONMENT_GENERAL;
		public override string Title => dnSpy_Resources.GeneralSettings;
		public override object UIObject => this;

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		readonly IThemeServiceImpl themeService;
		readonly IWindowsExplorerIntegrationService windowsExplorerIntegrationService;
		readonly IDocumentTabServiceSettings documentTabServiceSettings;
		readonly DocumentTreeViewSettingsImpl documentTreeViewSettings;
		readonly IDsDocumentServiceSettings documentServiceSettings;
		readonly MessageBoxService messageBoxService;

		public ICommand ClearAllWarningsCommand => new RelayCommand(a => messageBoxService.EnableAllWarnings(), a => messageBoxService.CanEnableAllWarnings);

		public ObservableCollection<ThemeVM> ThemesVM { get; }

		public ThemeVM SelectedThemeVM {
			get { return selectedThemeVM; }
			set {
				if (selectedThemeVM != value) {
					selectedThemeVM = value;
					OnPropertyChanged(nameof(SelectedThemeVM));
				}
			}
		}
		ThemeVM selectedThemeVM;

		public bool? WindowsExplorerIntegration {
			get { return windowsExplorerIntegration; }
			set {
				if (windowsExplorerIntegration != value) {
					windowsExplorerIntegration = value;
					OnPropertyChanged(nameof(WindowsExplorerIntegration));
				}
			}
		}
		bool? windowsExplorerIntegration;

		public bool DecompileFullType {
			get { return decompileFullType; }
			set {
				if (decompileFullType != value) {
					decompileFullType = value;
					OnPropertyChanged(nameof(DecompileFullType));
				}
			}
		}
		bool decompileFullType;

		public bool RestoreTabs {
			get { return restoreTabs; }
			set {
				if (restoreTabs != value) {
					restoreTabs = value;
					OnPropertyChanged(nameof(RestoreTabs));
				}
			}
		}
		bool restoreTabs;

		public bool DeserializeResources {
			get { return deserializeResources; }
			set {
				if (deserializeResources != value) {
					deserializeResources = value;
					OnPropertyChanged(nameof(DeserializeResources));
				}
			}
		}
		bool deserializeResources;

		public bool UseMemoryMappedIO {
			get { return useMemoryMappedIO; }
			set {
				if (useMemoryMappedIO != value) {
					useMemoryMappedIO = value;
					OnPropertyChanged(nameof(UseMemoryMappedIO));
				}
			}
		}
		bool useMemoryMappedIO;

		public UseNewRendererVM UseNewRendererVM { get; }

		public GeneralAppSettingsPage(IThemeServiceImpl themeService, IWindowsExplorerIntegrationService windowsExplorerIntegrationService, IDocumentTabServiceSettings documentTabServiceSettings, DocumentTreeViewSettingsImpl documentTreeViewSettings, IDsDocumentServiceSettings documentServiceSettings, AppSettingsImpl appSettings, MessageBoxService messageBoxService) {
			if (themeService == null)
				throw new ArgumentNullException(nameof(themeService));
			if (windowsExplorerIntegrationService == null)
				throw new ArgumentNullException(nameof(windowsExplorerIntegrationService));
			if (documentTabServiceSettings == null)
				throw new ArgumentNullException(nameof(documentTabServiceSettings));
			if (documentTreeViewSettings == null)
				throw new ArgumentNullException(nameof(documentTreeViewSettings));
			if (documentServiceSettings == null)
				throw new ArgumentNullException(nameof(documentServiceSettings));
			if (appSettings == null)
				throw new ArgumentNullException(nameof(appSettings));
			if (messageBoxService == null)
				throw new ArgumentNullException(nameof(messageBoxService));
			this.themeService = themeService;
			this.windowsExplorerIntegrationService = windowsExplorerIntegrationService;
			this.documentTabServiceSettings = documentTabServiceSettings;
			this.documentTreeViewSettings = documentTreeViewSettings;
			this.documentServiceSettings = documentServiceSettings;
			this.messageBoxService = messageBoxService;

			ThemesVM = new ObservableCollection<ThemeVM>(themeService.VisibleThemes.Select(a => new ThemeVM(a)));
			if (!ThemesVM.Any(a => a.Theme == themeService.Theme))
				ThemesVM.Add(new ThemeVM(themeService.Theme));
			SelectedThemeVM = ThemesVM.FirstOrDefault(a => a.Theme == themeService.Theme);
			Debug.Assert(SelectedThemeVM != null);

			WindowsExplorerIntegration = windowsExplorerIntegrationService.WindowsExplorerIntegration;
			DecompileFullType = documentTabServiceSettings.DecompileFullType;
			RestoreTabs = documentTabServiceSettings.RestoreTabs;
			DeserializeResources = documentTreeViewSettings.DeserializeResources;
			UseMemoryMappedIO = documentServiceSettings.UseMemoryMappedIO;
			UseNewRendererVM = new UseNewRendererVM(appSettings);
		}

		public override string[] GetSearchStrings() => ThemesVM.Select(a => a.Name).ToArray();

		public override void OnApply() { throw new InvalidOperationException(); }

		public void OnApply(IAppRefreshSettings appRefreshSettings) {
			if (SelectedThemeVM != null)
				themeService.Theme = SelectedThemeVM.Theme;
			windowsExplorerIntegrationService.WindowsExplorerIntegration = WindowsExplorerIntegration;
			documentTabServiceSettings.DecompileFullType = DecompileFullType;
			documentTabServiceSettings.RestoreTabs = RestoreTabs;
			documentTreeViewSettings.DeserializeResources = DeserializeResources;

			if (documentServiceSettings.UseMemoryMappedIO != UseMemoryMappedIO) {
				documentServiceSettings.UseMemoryMappedIO = UseMemoryMappedIO;
				if (!documentServiceSettings.UseMemoryMappedIO)
					appRefreshSettings.Add(AppSettingsConstants.DISABLE_MEMORY_MAPPED_IO);
			}

			UseNewRendererVM.Save();
		}
	}

	sealed class ThemeVM {
		public ITheme Theme { get; }
		public string Name => Theme.GetName();

		public ThemeVM(ITheme theme) {
			if (theme == null)
				throw new ArgumentNullException(nameof(theme));
			Theme = theme;
		}
	}

	sealed class UseNewRendererVM : ViewModelBase {
		public bool? UseNewRenderer {
			get {
				const int MAX = 2;
				int count = (UseNewRenderer_HexEditor ? 1 : 0) +
							(UseNewRenderer_DocumentTreeView ? 1 : 0);
				return count == 0 ? false : count == MAX ? (bool?)true : null;
			}
			set {
				if (value == null)
					return;
				UseNewRenderer_TextEditor = value.Value;
				UseNewRenderer_HexEditor = value.Value;
				UseNewRenderer_DocumentTreeView = value.Value;
			}
		}

		public bool UseNewRenderer_TextEditor {
			get { return useNewRenderer_TextEditor; }
			set {
				if (useNewRenderer_TextEditor != value) {
					useNewRenderer_TextEditor = value;
					OnPropertyChanged(nameof(UseNewRenderer_TextEditor));
					OnPropertyChanged(nameof(UseNewRenderer));
				}
			}
		}
		bool useNewRenderer_TextEditor = false;

		public bool UseNewRenderer_HexEditor {
			get { return useNewRenderer_HexEditor; }
			set {
				if (useNewRenderer_HexEditor != value) {
					useNewRenderer_HexEditor = value;
					OnPropertyChanged(nameof(UseNewRenderer_HexEditor));
					OnPropertyChanged(nameof(UseNewRenderer));
				}
			}
		}
		bool useNewRenderer_HexEditor = false;

		public bool UseNewRenderer_DocumentTreeView {
			get { return useNewRenderer_DocumentTreeView; }
			set {
				if (useNewRenderer_DocumentTreeView != value) {
					useNewRenderer_DocumentTreeView = value;
					OnPropertyChanged(nameof(UseNewRenderer_DocumentTreeView));
					OnPropertyChanged(nameof(UseNewRenderer));
				}
			}
		}
		bool useNewRenderer_DocumentTreeView = false;

		readonly AppSettingsImpl appSettings;

		public UseNewRendererVM(AppSettingsImpl appSettings) {
			this.appSettings = appSettings;
			UseNewRenderer_TextEditor = appSettings.UseNewRenderer_TextEditor;
			UseNewRenderer_HexEditor = appSettings.UseNewRenderer_HexEditor;
			UseNewRenderer_DocumentTreeView = appSettings.UseNewRenderer_DocumentTreeView;
		}

		public void Save() {
			appSettings.UseNewRenderer_TextEditor = UseNewRenderer_TextEditor;
			appSettings.UseNewRenderer_HexEditor = UseNewRenderer_HexEditor;
			appSettings.UseNewRenderer_DocumentTreeView = UseNewRenderer_DocumentTreeView;
		}
	}
}
