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
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using dnSpy.Properties;

namespace dnSpy.MainApp {
	public static class StartUpClass {
		static void OptimizeStartup() {
			// Use multicore JIT.
			// This requires .NET Framework 4.5 but dnSpy targets 4.0 at the moment.
			// Simple test: x86: ~18% faster startup, x64: ~12% faster startup.

			var profType = Type.GetType("System.Runtime.ProfileOptimization", false);
			if (profType == null)
				return;
			var setProfileRootMethod = profType.GetMethod("SetProfileRoot", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
			var startProfileMethod = profType.GetMethod("StartProfile", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null);
			if (setProfileRootMethod == null || startProfileMethod == null)
				return;
			try {
				var profileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "dnSpy", "Startup");
				Directory.CreateDirectory(profileDir);
				setProfileRootMethod.Invoke(null, new object[1] { profileDir });
				startProfileMethod.Invoke(null, new object[1] { string.Format("startup-{0}.profile", IntPtr.Size * 8) });
			}
			catch {
			}
		}

		[STAThread]
		public static void Main() {
			OptimizeStartup();

			if (!dnlib.Settings.IsThreadSafe) {
				MessageBox.Show("dnlib wasn't compiled with THREAD_SAFE defined.");
				Environment.Exit(1);
			}

			bool readSettings = (Keyboard.Modifiers & ModifierKeys.Shift) == 0;
			if (!readSettings) {
				// Need to use DefaultDesktopOnly or the dlg box is shown in the background...
				var res = MessageBox.Show(dnSpy_Resources.AskReadSettings, "dnSpy", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly);
				readSettings = res != MessageBoxResult.No;
			}

			var app = new App(readSettings);
			app.InitializeComponent();
			app.Run();
		}
	}
}
