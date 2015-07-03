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
using System.Xml.Linq;
using Microsoft.Win32;

namespace ICSharpCode.ILSpy.Options
{
	sealed class OtherSettings : AsmEditor.ViewModelBase
	{
		public static OtherSettings Instance {
			get {
				if (settings != null)
					return settings;
				Interlocked.CompareExchange(ref settings, Load(ILSpySettings.Load()), null);
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

		public bool WindowsExplorerIntegration {
			get { return HasExplorerIntegration(); }
			set {
				SetExplorerIntegration(value);
				OnPropertyChanged("WindowsExplorerIntegration");
			}
		}

		const string SETTINGS_SECTION_NAME = "OtherSettings";
		internal static OtherSettings Load(ILSpySettings settings)
		{
			var xelem = settings[SETTINGS_SECTION_NAME];
			var s = new OtherSettings();
			s.UseMemoryMappedIO = (bool?)xelem.Attribute("UseMemoryMappedIO") ?? true;
			return s;
		}

		internal RefreshFlags Save(XElement root)
		{
			var flags = RefreshFlags.None;

			if (!this.UseMemoryMappedIO && Instance.UseMemoryMappedIO)
				flags |= RefreshFlags.DisableMmap;

			var xelem = new XElement(SETTINGS_SECTION_NAME);
			xelem.SetAttributeValue("UseMemoryMappedIO", this.UseMemoryMappedIO);


			var currElem = root.Element(SETTINGS_SECTION_NAME);
			if (currElem != null)
				currElem.ReplaceWith(xelem);
			else
				root.Add(xelem);

			WriteTo(Instance);

			return flags;
		}

		void WriteTo(OtherSettings other)
		{
			other.UseMemoryMappedIO = this.UseMemoryMappedIO;
		}

		protected override string Verify(string columnName) {
			return string.Empty;
		}

		public override bool HasError {
			get { return false; }
		}

		const string EXPLORER_MENU_TEXT = "Open with dnSpy";

		bool HasExplorerIntegration() {
			bool hasIntegration = false;
			try {
				using (var key = Registry.ClassesRoot.CreateSubKey("exefile\\shell"))
				using (var ctxMenu = key.OpenSubKey(EXPLORER_MENU_TEXT)) {
					if (ctxMenu != null)
						hasIntegration = true;
				}
			}
			catch {
			}
			return hasIntegration;
		}

		void SetExplorerIntegration(bool enabled) {
			var path = typeof(MainWindow).Assembly.Location;
			if (!File.Exists(path)) {
				MainWindow.Instance.ShowMessageBox("Cannot locate dnSpy!");
				return;
			}
			path = string.Format("\"{0}\" \"%1\"", path);

			try {
				using (var key = Registry.ClassesRoot.CreateSubKey("exefile\\shell")) {
					if (enabled) {
						using (var ctxMenu = key.CreateSubKey(EXPLORER_MENU_TEXT))
						using (var cmdKey = ctxMenu.CreateSubKey("command"))
							cmdKey.SetValue("", path);
					}
					else
						key.DeleteSubKey(EXPLORER_MENU_TEXT, false);
				}

				using (var key = Registry.ClassesRoot.CreateSubKey("dllfile\\shell")) {
					if (enabled) {
						using (var ctxMenu = key.CreateSubKey(EXPLORER_MENU_TEXT))
						using (var cmdKey = ctxMenu.CreateSubKey("command"))
							cmdKey.SetValue("", path);
					}
					else
						key.DeleteSubKey(EXPLORER_MENU_TEXT, false);
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
	}
}
