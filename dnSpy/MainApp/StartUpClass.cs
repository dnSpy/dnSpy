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
using System.Runtime;
using System.Windows;
using System.Windows.Input;
using dnSpy.Properties;

namespace dnSpy.MainApp {
	public static class StartUpClass {
		static void OptimizeStartup() {
			// Use multicore JIT.
			// Simple test: x86: ~18% faster startup, x64: ~12% faster startup.

			try {
				var profileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "dnSpy", "Startup");
				Directory.CreateDirectory(profileDir);
				ProfileOptimization.SetProfileRoot(profileDir);
				ProfileOptimization.StartProfile(string.Format("startup-{0}.profile", IntPtr.Size * 8));
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
