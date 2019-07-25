/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Security;
using dnSpy.Contracts.App;
using dnSpy.Properties;
using Microsoft.Win32;

namespace dnSpy.MainApp.Settings {
	interface IWindowsExplorerIntegrationService {
		bool? WindowsExplorerIntegration { get; set; }
	}

	[Export(typeof(IWindowsExplorerIntegrationService))]
	sealed class WindowsExplorerIntegrationService : IWindowsExplorerIntegrationService {
		static readonly string EXPLORER_MENU_TEXT = dnSpy_Resources.ExplorerOpenWithDnSpy;
		static readonly string[] openExtensions = new string[] {
			"exe", "dll", "netmodule", "winmd",
		};

		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		WindowsExplorerIntegrationService(IMessageBoxService messageBoxService) => this.messageBoxService = messageBoxService;

		public bool? WindowsExplorerIntegration {
			get {
				int count = 0;
				try {
					foreach (var ext in openExtensions) {
						string? name;
						using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\." + ext))
							name = key is null ? null : key.GetValue(string.Empty) as string;
						if (string.IsNullOrEmpty(name))
							continue;

						using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + name + @"\shell\" + EXPLORER_MENU_TEXT)) {
							if (!(key is null))
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
				if (value is null)
					return;
				bool enabled = value.Value;

				var path = Assembly.GetEntryAssembly()!.Location;
#if NETCOREAPP
				// Use the native exe and not the managed file
				path = Path.ChangeExtension(path, "exe");
				if (!File.Exists(path)) {
					// All .NET files could be in a bin sub dir
					if (Path.GetDirectoryName(Path.GetDirectoryName(path)) is string baseDir)
						path = Path.Combine(baseDir, Path.GetFileName(path));
				}
#endif
				if (!File.Exists(path)) {
					messageBoxService.Show("Cannot locate dnSpy!");
					return;
				}
				path = $"\"{path}\" -- \"%1\"";

				try {
					foreach (var ext in openExtensions) {
						string? name;
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
					messageBoxService.Show("Cannot obtain access to registry!");
				}
				catch (SecurityException) {
					messageBoxService.Show("Cannot obtain access to registry!");
				}
				catch (Exception ex) {
					messageBoxService.Show("Cannot add context menu item!" + Environment.NewLine + ex.ToString());
				}
			}
		}
	}
}
