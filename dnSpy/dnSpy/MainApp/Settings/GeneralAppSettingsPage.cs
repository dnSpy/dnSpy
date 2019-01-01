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

	sealed class GeneralAppSettingsPage : AppSettingsPage, IAppSettingsPage2 {
		public override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_ENVIRONMENT);
		public override Guid Guid => new Guid("776184ED-10F6-466C-8B66-716936C29A5A");
		public override double Order => AppSettingsConstants.ORDER_ENVIRONMENT_GENERAL;
		public override string Title => dnSpy_Resources.GeneralSettings;
		public override object UIObject => this;

		readonly IThemeServiceImpl themeService;
		readonly IWindowsExplorerIntegrationService windowsExplorerIntegrationService;
		readonly IDocumentTabServiceSettings documentTabServiceSettings;
		readonly DocumentTreeViewSettingsImpl documentTreeViewSettings;
		readonly IDsDocumentServiceSettings documentServiceSettings;
		readonly AppSettingsImpl appSettings;
		readonly MessageBoxService messageBoxService;

		public ICommand ClearAllWarningsCommand => new RelayCommand(a => messageBoxService.EnableAllWarnings(), a => messageBoxService.CanEnableAllWarnings);

		public ObservableCollection<ThemeVM> ThemesVM { get; }

		public ThemeVM SelectedThemeVM {
			get => selectedThemeVM;
			set {
				if (selectedThemeVM != value) {
					selectedThemeVM = value;
					OnPropertyChanged(nameof(SelectedThemeVM));
				}
			}
		}
		ThemeVM selectedThemeVM;

		public bool? WindowsExplorerIntegration {
			get => windowsExplorerIntegration;
			set {
				if (windowsExplorerIntegration != value) {
					windowsExplorerIntegration = value;
					OnPropertyChanged(nameof(WindowsExplorerIntegration));
				}
			}
		}
		bool? windowsExplorerIntegration;

		public bool AllowMoreThanOneInstance {
			get => allowMoreThanOneInstance;
			set {
				if (allowMoreThanOneInstance != value) {
					allowMoreThanOneInstance = value;
					OnPropertyChanged(nameof(AllowMoreThanOneInstance));
				}
			}
		}
		bool allowMoreThanOneInstance;

		public bool DecompileFullType {
			get => decompileFullType;
			set {
				if (decompileFullType != value) {
					decompileFullType = value;
					OnPropertyChanged(nameof(DecompileFullType));
				}
			}
		}
		bool decompileFullType;

		public bool RestoreTabs {
			get => restoreTabs;
			set {
				if (restoreTabs != value) {
					restoreTabs = value;
					OnPropertyChanged(nameof(RestoreTabs));
				}
			}
		}
		bool restoreTabs;

		public bool DeserializeResources {
			get => deserializeResources;
			set {
				if (deserializeResources != value) {
					deserializeResources = value;
					OnPropertyChanged(nameof(DeserializeResources));
				}
			}
		}
		bool deserializeResources;

		public bool UseMemoryMappedIO {
			get => useMemoryMappedIO;
			set {
				if (useMemoryMappedIO != value) {
					useMemoryMappedIO = value;
					OnPropertyChanged(nameof(UseMemoryMappedIO));
				}
			}
		}
		bool useMemoryMappedIO;

		public GeneralAppSettingsPage(IThemeServiceImpl themeService, IWindowsExplorerIntegrationService windowsExplorerIntegrationService, IDocumentTabServiceSettings documentTabServiceSettings, DocumentTreeViewSettingsImpl documentTreeViewSettings, IDsDocumentServiceSettings documentServiceSettings, AppSettingsImpl appSettings, MessageBoxService messageBoxService) {
			this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
			this.windowsExplorerIntegrationService = windowsExplorerIntegrationService ?? throw new ArgumentNullException(nameof(windowsExplorerIntegrationService));
			this.documentTabServiceSettings = documentTabServiceSettings ?? throw new ArgumentNullException(nameof(documentTabServiceSettings));
			this.documentTreeViewSettings = documentTreeViewSettings ?? throw new ArgumentNullException(nameof(documentTreeViewSettings));
			this.documentServiceSettings = documentServiceSettings ?? throw new ArgumentNullException(nameof(documentServiceSettings));
			this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
			this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));

			ThemesVM = new ObservableCollection<ThemeVM>(themeService.VisibleThemes.Select(a => new ThemeVM(a)));
			if (!ThemesVM.Any(a => a.Theme == themeService.Theme))
				ThemesVM.Add(new ThemeVM(themeService.Theme));
			SelectedThemeVM = ThemesVM.FirstOrDefault(a => a.Theme == themeService.Theme);
			Debug.Assert(SelectedThemeVM != null);

			WindowsExplorerIntegration = windowsExplorerIntegrationService.WindowsExplorerIntegration;
			AllowMoreThanOneInstance = appSettings.AllowMoreThanOneInstance;
			DecompileFullType = documentTabServiceSettings.DecompileFullType;
			RestoreTabs = documentTabServiceSettings.RestoreTabs;
			DeserializeResources = documentTreeViewSettings.DeserializeResources;
			UseMemoryMappedIO = documentServiceSettings.UseMemoryMappedIO;
		}

		public override string[] GetSearchStrings() => ThemesVM.Select(a => a.Name).ToArray();

		public override void OnApply() => throw new InvalidOperationException();
		public void OnApply(IAppRefreshSettings appRefreshSettings) {
			if (SelectedThemeVM != null)
				themeService.Theme = SelectedThemeVM.Theme;
			windowsExplorerIntegrationService.WindowsExplorerIntegration = WindowsExplorerIntegration;
			appSettings.AllowMoreThanOneInstance = AllowMoreThanOneInstance;
			documentTabServiceSettings.DecompileFullType = DecompileFullType;
			documentTabServiceSettings.RestoreTabs = RestoreTabs;
			documentTreeViewSettings.DeserializeResources = DeserializeResources;

			if (documentServiceSettings.UseMemoryMappedIO != UseMemoryMappedIO) {
				documentServiceSettings.UseMemoryMappedIO = UseMemoryMappedIO;
				if (!documentServiceSettings.UseMemoryMappedIO)
					appRefreshSettings.Add(AppSettingsConstants.DISABLE_MEMORY_MAPPED_IO);
			}

		}
	}

	sealed class ThemeVM : ViewModelBase {
		public ITheme Theme { get; }
		public string Name => Theme.GetName();

		public ThemeVM(ITheme theme) => Theme = theme ?? throw new ArgumentNullException(nameof(theme));
	}
}
