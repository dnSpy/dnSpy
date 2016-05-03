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

// Original "WindowsExplorerIntegration" code was written by Ki: Copyright (c) 2015 Ki

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Security;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;
using dnSpy.Shared.MVVM;
using dnSpy.Shared.Settings.Dialog;
using Microsoft.Win32;

namespace dnSpy.MainApp {
	[ExportSimpleAppOptionCreator(Guid = AppSettingsConstants.GUID_DYNTAB_MISC)]
	sealed class AppMiscOptionCreator : ISimpleAppOptionCreator {
		readonly MessageBoxManager messageBoxManager;
		readonly AppSettingsImpl appSettings;

		[ImportingConstructor]
		AppMiscOptionCreator(MessageBoxManager messageBoxManager, AppSettingsImpl appSettings) {
			this.messageBoxManager = messageBoxManager;
			this.appSettings = appSettings;
		}

		public IEnumerable<ISimpleAppOption> Create() {
			yield return new SimpleAppOptionCheckBox(WindowsExplorerIntegration, (saveSettings, appRefreshSettings, newValue) => {
				if (!saveSettings)
					return;
				WindowsExplorerIntegration = newValue;
			}) {
				Order = AppSettingsConstants.ORDER_MISC_EXPLORERINTEGRATION,
				Text = dnSpy_Resources.Options_Misc_ExplorerIntegration,
				ToolTip = dnSpy_Resources.Options_Misc_ExplorerIntegration_ToolTip,
			};

			yield return new SimpleAppOptionButton() {
				Order = AppSettingsConstants.ORDER_MISC_ENABLEALLWARNINGS,
				Text = dnSpy_Resources.Options_Misc_Button_EnableAllWarnings,
				Command = new RelayCommand(a => messageBoxManager.EnableAllWarnings(), a => messageBoxManager.CanEnableAllWarnings),
			};

			yield return new SimpleAppOptionUserContent<UseNewRendererVM>(new UseNewRendererVM(appSettings), (saveSettings, appRefreshSettings, vm) => {
				if (saveSettings)
					vm.Save();
			}) {
				Order = AppSettingsConstants.ORDER_MISC_USENEWRENDERER,
			};
		}

		static readonly string EXPLORER_MENU_TEXT = dnSpy_Resources.ExplorerOpenWithDnSpy;
		static readonly string[] openExtensions = new string[] {
			"exe", "dll", "netmodule", "winmd",
		};

		bool? WindowsExplorerIntegration {
			get {
				int count = 0;
				try {
					foreach (var ext in openExtensions) {
						string name;
						using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\." + ext))
							name = key == null ? null : key.GetValue(string.Empty) as string;
						if (string.IsNullOrEmpty(name))
							continue;

						using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + name + @"\shell\" + EXPLORER_MENU_TEXT)) {
							if (key != null)
								count++;
						}
					}
				}
				catch {
				}
				return count == openExtensions.Length ? true :
					count == 0 ? (bool?)false :
					null;
			}
			set {
				if (value == null)
					return;
				bool enabled = value.Value;

				var path = System.Reflection.Assembly.GetEntryAssembly().Location;
				if (!File.Exists(path)) {
					messageBoxManager.Show("Cannot locate dnSpy!");
					return;
				}
				path = string.Format("\"{0}\" -- \"%1\"", path);

				try {
					foreach (var ext in openExtensions) {
						string name;
						using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\." + ext))
							name = key?.GetValue(string.Empty) as string;

						if (string.IsNullOrEmpty(name)) {
							if (!enabled)
								continue;

							using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\." + ext))
								key.SetValue(string.Empty, name = string.Format(ext + "file"));
						}

						using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + name + @"\shell")) {
							if (enabled) {
								using (var cmdKey = key.CreateSubKey(EXPLORER_MENU_TEXT + @"\command"))
									cmdKey.SetValue("", path);
							}
							else
								key.DeleteSubKeyTree(EXPLORER_MENU_TEXT, false);
						}
					}
				}
				catch (UnauthorizedAccessException) {
					messageBoxManager.Show("Cannot obtain access to registry!");
				}
				catch (SecurityException) {
					messageBoxManager.Show("Cannot obtain access to registry!");
				}
				catch (Exception ex) {
					messageBoxManager.Show("Cannot add context menu item!" + Environment.NewLine + ex.ToString());
				}
			}
		}
	}

	sealed class UseNewRendererVM : ViewModelBase {
		public bool? UseNewRenderer {
			get {
				const int MAX = 3;
				int count = (UseNewRenderer_TextEditor ? 1 : 0) +
							(UseNewRenderer_HexEditor ? 1 : 0) +
							(UseNewRenderer_FileTreeView ? 1 : 0);
				return count == 0 ? false : count == MAX ? (bool?)true : null;
			}
			set {
				if (value == null)
					return;
				UseNewRenderer_TextEditor = value.Value;
				UseNewRenderer_HexEditor = value.Value;
				UseNewRenderer_FileTreeView = value.Value;
			}
		}

		public bool UseNewRenderer_TextEditor {
			get { return useNewRenderer_TextEditor; }
			set {
				if (useNewRenderer_TextEditor != value) {
					useNewRenderer_TextEditor = value;
					OnPropertyChanged("UseNewRenderer_TextEditor");
					OnPropertyChanged("UseNewRenderer");
				}
			}
		}
		bool useNewRenderer_TextEditor = false;

		public bool UseNewRenderer_HexEditor {
			get { return useNewRenderer_HexEditor; }
			set {
				if (useNewRenderer_HexEditor != value) {
					useNewRenderer_HexEditor = value;
					OnPropertyChanged("UseNewRenderer_HexEditor");
					OnPropertyChanged("UseNewRenderer");
				}
			}
		}
		bool useNewRenderer_HexEditor = false;

		public bool UseNewRenderer_FileTreeView {
			get { return useNewRenderer_FileTreeView; }
			set {
				if (useNewRenderer_FileTreeView != value) {
					useNewRenderer_FileTreeView = value;
					OnPropertyChanged("UseNewRenderer_FileTreeView");
					OnPropertyChanged("UseNewRenderer");
				}
			}
		}
		bool useNewRenderer_FileTreeView = false;

		readonly AppSettingsImpl appSettings;

		public UseNewRendererVM(AppSettingsImpl appSettings) {
			this.appSettings = appSettings;
			this.UseNewRenderer_TextEditor = appSettings.UseNewRenderer_TextEditor;
			this.UseNewRenderer_HexEditor = appSettings.UseNewRenderer_HexEditor;
			this.UseNewRenderer_FileTreeView = appSettings.UseNewRenderer_FileTreeView;
		}

		public void Save() {
			appSettings.UseNewRenderer_TextEditor = this.UseNewRenderer_TextEditor;
			appSettings.UseNewRenderer_HexEditor = this.UseNewRenderer_HexEditor;
			appSettings.UseNewRenderer_FileTreeView = this.UseNewRenderer_FileTreeView;
		}
	}
}
