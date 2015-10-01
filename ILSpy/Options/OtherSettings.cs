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

using System;
using System.IO;
using System.Security;
using System.Threading;
using System.Windows.Input;
using System.Xml.Linq;
using dnSpy.MVVM;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Options;
using Microsoft.Win32;

namespace dnSpy.Options {
	[ExportOptionPage(Title = "Other", Order = 3)]
	sealed class OtherSettingsCreator : IOptionPageCreator {
		public OptionPage Create() {
			return new OtherSettings();
		}
	}

	sealed class OtherSettings : OptionPage {
		public ICommand EnableAllWarningsCommand {
			get { return new RelayCommand(a => EnableAllWarnings(), a => EnableAllWarningsCanExecute()); }
		}

		public static OtherSettings Instance {
			get {
				if (settings != null)
					return settings;
				var s = new OtherSettings();
				s.Load(ILSpySettings.Load());
				Interlocked.CompareExchange(ref settings, s, null);
				return settings;
			}
		}
		static OtherSettings settings;

		public bool UseMemoryMappedIO {
			get { return useMemoryMappedIO; }
			set {
				if (useMemoryMappedIO != value) {
					useMemoryMappedIO = value;
					OnPropertyChanged("UseMemoryMappedIO");
				}
			}
		}
		bool useMemoryMappedIO;

		public bool? WindowsExplorerIntegration {
			get { return HasExplorerIntegration(); }
			set {
				SetExplorerIntegration(value ?? false);
				OnPropertyChanged("WindowsExplorerIntegration");
			}
		}

		public bool DeserializeResources {
			get { return deserializeResources; }
			set {
				if (deserializeResources != value) {
					deserializeResources = value;
					OnPropertyChanged("DeserializeResources");
				}
			}
		}
		bool deserializeResources;

		const string SETTINGS_SECTION_NAME = "OtherSettings";
		public override void Load(ILSpySettings settings) {
			var xelem = settings[SETTINGS_SECTION_NAME];
			this.UseMemoryMappedIO = (bool?)xelem.Attribute("UseMemoryMappedIO") ?? true;
			this.DeserializeResources = (bool?)xelem.Attribute("DeserializeResources") ?? true;
		}

		public override RefreshFlags Save(XElement root) {
			var flags = RefreshFlags.None;

			if (!this.UseMemoryMappedIO && Instance.UseMemoryMappedIO)
				flags |= RefreshFlags.DisableMmap;

			var xelem = new XElement(SETTINGS_SECTION_NAME);
			xelem.SetAttributeValue("UseMemoryMappedIO", this.UseMemoryMappedIO);
			xelem.SetAttributeValue("DeserializeResources", this.DeserializeResources);


			var currElem = root.Element(SETTINGS_SECTION_NAME);
			if (currElem != null)
				currElem.ReplaceWith(xelem);
			else
				root.Add(xelem);

			WriteTo(Instance);

			return flags;
		}

		void WriteTo(OtherSettings other) {
			other.UseMemoryMappedIO = this.UseMemoryMappedIO;
			other.DeserializeResources = this.DeserializeResources;
		}

		const string EXPLORER_MENU_TEXT = "Open with dnSpy";
		static readonly string[] openExtensions = new string[] {
			"exe", "dll", "netmodule", "winmd",
		};

		bool? HasExplorerIntegration() {
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

		void SetExplorerIntegration(bool enabled) {
			var path = System.Reflection.Assembly.GetEntryAssembly().Location;
			if (!File.Exists(path)) {
				MainWindow.Instance.ShowMessageBox("Cannot locate dnSpy!");
				return;
			}
			path = string.Format("\"{0}\" \"%1\"", path);

			try {
				foreach (var ext in openExtensions) {
					string name;
					using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\." + ext))
						name = key == null ? null : key.GetValue(string.Empty) as string;

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
				MainWindow.Instance.ShowMessageBox("Cannot obtain access to registry!");
			}
			catch (SecurityException) {
				MainWindow.Instance.ShowMessageBox("Cannot obtain access to registry!");
			}
			catch (Exception ex) {
				MainWindow.Instance.ShowMessageBox("Cannot add context menu item!" + Environment.NewLine + ex.ToString());
			}
		}

		void EnableAllWarnings() {
			MainWindow.Instance.SessionSettings.IgnoredWarnings.Clear();
		}

		bool EnableAllWarningsCanExecute() {
			return MainWindow.Instance.SessionSettings.IgnoredWarnings.Count > 0;
		}
	}
}
