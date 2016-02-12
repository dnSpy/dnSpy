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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Settings;
using dnSpy.Culture;
using dnSpy.Files.Tabs;
using dnSpy.Plugin;
using dnSpy.Settings;
using dnSpy.Shared.Controls;
using dnSpy.Shared.MVVM;

namespace dnSpy.MainApp {
	sealed partial class App : Application {
		static App() {
			InstallExceptionHandlers();
		}

		static void InstallExceptionHandlers() {
			TaskScheduler.UnobservedTaskException += (s, e) => e.SetObserved();
			if (!Debugger.IsAttached) {
				AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowException(e.ExceptionObject as Exception);
				Dispatcher.CurrentDispatcher.UnhandledException += (s, e) => {
					ShowException(e.Exception);
					e.Handled = true;
				};
			}
		}

		static void ShowException(Exception ex) {
			string msg = ex == null ? "Unknown exception" : ex.ToString();
			MessageBox.Show(msg, "dnSpy", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		[Import]
		AppWindow appWindow = null;
		[Import]
		SettingsManager settingsManager = null;
		[Import]
		PluginManager pluginManager = null;
		[Import]
		IDnSpyLoaderManager dnSpyLoaderManager = null;
		[Import]
		Lazy<IFileTreeView> fileTreeView = null;
		[Import]
		Lazy<ILanguageManager> languageManager = null;
		readonly List<LoadedPlugin> loadedPlugins = new List<LoadedPlugin>();
		CompositionContainer compositionContainer;

		readonly IAppCommandLineArgs args;

		public App(bool readSettings) {
			this.args = new AppCommandLineArgs();
			if (args.SingleInstance)
				SwitchToOtherInstance();

			InitializeComponent();
			UIFixes();

			InitializeMEF(readSettings);
			compositionContainer.ComposeParts(this);
			this.pluginManager.LoadedPlugins = this.loadedPlugins;
			this.appWindow.CommandLineArgs = this.args;

			this.Exit += App_Exit;
		}

		void InitializeMEF(bool readSettings) {
			compositionContainer = InitializeCompositionContainer();
			if (readSettings) {
				var settingsManager = compositionContainer.GetExportedValue<ISettingsManager>();
				try {
					new XmlSettingsReader(settingsManager).Read();
				}
				catch {
				}
			}

			var cultureManager = compositionContainer.GetExportedValue<CultureManager>();
			cultureManager.Initialize(this.args);
		}

		CompositionContainer InitializeCompositionContainer() {
			var aggregateCatalog = new AggregateCatalog();
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(GetType().Assembly));
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(typeof(EnumVM).Assembly));// dnSpy.Shared
			AddPluginFiles(aggregateCatalog);
			return new CompositionContainer(aggregateCatalog);
		}

		void AddPluginFiles(AggregateCatalog aggregateCatalog) {
			var dir = Path.GetDirectoryName(GetType().Assembly.Location);
			var random = new Random();
			var files = Directory.GetFiles(dir, "*.Plugin.dll").OrderBy(a => random.Next()).ToArray();
			foreach (var file in files) {
				try {
					var asm = Assembly.LoadFile(file);
					aggregateCatalog.Catalogs.Add(new AssemblyCatalog(asm));
					loadedPlugins.Add(new LoadedPlugin(asm));
				}
				catch (Exception ex) {
					Debug.Fail(string.Format("Failed to load file '{0}', msg: {1}", file, ex.Message));
				}
			}
		}

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("user32")]
		static extern bool SetForegroundWindow(IntPtr hWnd);
		[DllImport("user32")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
		[return: MarshalAs(UnmanagedType.Bool)]
		delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
		[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
		[DllImport("user32", CharSet = CharSet.Auto)]
		static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);
		[StructLayout(LayoutKind.Sequential)]
		struct COPYDATASTRUCT {
			public IntPtr dwData;
			public int cbData;
			public IntPtr lpData;
		}
		static readonly IntPtr COPYDATASTRUCT_dwData = new IntPtr(0x11C9B152);
		static readonly IntPtr COPYDATASTRUCT_result = new IntPtr(0x615F9D6E);
		const string COPYDATASTRUCT_HEADER = "dnSpy";	// One line only

		void SwitchToOtherInstance() {
			EnumWindows(EnumWindowsHandler, IntPtr.Zero);
		}

		unsafe bool EnumWindowsHandler(IntPtr hWnd, IntPtr lParam) {
			var sb = new StringBuilder(256);
			GetWindowText(hWnd, sb, sb.Capacity);
			if (sb.ToString().StartsWith("dnSpy ", StringComparison.Ordinal)) {
				var args = Environment.GetCommandLineArgs();
				args[0] = COPYDATASTRUCT_HEADER;
				var msg = string.Join(Environment.NewLine, args);
				COPYDATASTRUCT data;
				data.dwData = COPYDATASTRUCT_dwData;
				data.cbData = msg.Length * 2;
				fixed (void* pmsg = msg) {
					data.lpData = new IntPtr(pmsg);
					var res = SendMessage(hWnd, 0x4A, IntPtr.Zero, ref data);
					if (res == COPYDATASTRUCT_result) {
						if (this.args.Activate)
							SetForegroundWindow(hWnd);
						Environment.Exit(0);
					}
				}
			}

			return true;
		}

		unsafe IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
			if (msg != 0x4A)
				return IntPtr.Zero;

			var data = *(COPYDATASTRUCT*)lParam;
			if (data.dwData != COPYDATASTRUCT_dwData)
				return IntPtr.Zero;

			var argsString = new string((char*)data.lpData, 0, data.cbData / 2);
			var args = argsString.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			if (args[0] != COPYDATASTRUCT_HEADER)
				return IntPtr.Zero;

			HandleAppArgs(new AppCommandLineArgs(args.Skip(1).ToArray()));
			handled = true;
			return COPYDATASTRUCT_result;
		}

		void MainWindow_SourceInitialized(object sender, EventArgs e) {
			appWindow.MainWindow.SourceInitialized -= MainWindow_SourceInitialized;

			var hwndSource = PresentationSource.FromVisual(appWindow.MainWindow) as HwndSource;
			Debug.Assert(hwndSource != null);
			if (hwndSource != null)
				hwndSource.AddHook(WndProc);
		}

		void App_Exit(object sender, ExitEventArgs e) {
			pluginManager.OnAppExit();
			dnSpyLoaderManager.Save();
			try {
				new XmlSettingsWriter(settingsManager).Write();
			}
			catch {
			}
			compositionContainer.Dispose();
		}

		void UIFixes() {
			// Add Ctrl+Shift+Z as a redo command. Don't know why it isn't enabled by default.
			ApplicationCommands.Redo.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift));
			FixEditorContextMenuStyle();
		}

		// The text editor creates an EditorContextMenu which derives from ContextMenu. This
		// class is private in the assembly and can't be referenced from XAML. In order to style
		// this class we must get the type at runtime and add its style to the Resources.
		void FixEditorContextMenuStyle() {
			var module = typeof(ContextMenu).Module;
			var type = module.GetType("System.Windows.Documents.TextEditorContextMenu+EditorContextMenu", false, false);
			Debug.Assert(type != null);
			if (type == null)
				return;
			const string styleKey = "EditorContextMenuStyle";
			var style = this.Resources[styleKey];
			Debug.Assert(style != null);
			if (style == null)
				return;
			this.Resources.Remove(styleKey);
			this.Resources.Add(type, style);
		}

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			var win = appWindow.InitializeMainWindow();
			appWindow.MainWindow.SourceInitialized += MainWindow_SourceInitialized;
			dnSpyLoaderManager.OnAppLoaded += DnSpyLoaderManager_OnAppLoaded;
			dnSpyLoaderManager.Initialize(appWindow, win);
			pluginManager.LoadPlugins(this.Resources.MergedDictionaries);
			win.Show();
		}

		void DnSpyLoaderManager_OnAppLoaded(object sender, EventArgs e) {
			dnSpyLoaderManager.OnAppLoaded -= DnSpyLoaderManager_OnAppLoaded;
			appWindow.AppLoaded = true;
			pluginManager.OnAppLoaded();
			HandleAppArgs(args);
		}

		void HandleAppArgs(IAppCommandLineArgs appArgs) {
			if (appArgs.Activate && appWindow.MainWindow.WindowState == WindowState.Minimized)
				WindowUtils.SetState(appWindow.MainWindow, WindowState.Normal);

			var lang = GetLanguage(appArgs.Language);
			if (lang != null)
				languageManager.Value.Language = lang;

			var files = appArgs.Filenames.ToArray();
			if (files.Length > 0)
				OpenFileInit.OpenFiles(fileTreeView.Value, appWindow.MainWindow, files);
		}

		ILanguage GetLanguage(string language) {
			if (string.IsNullOrEmpty(language))
				return null;

			Guid guid;
			if (Guid.TryParse(language, out guid)) {
				var lang = languageManager.Value.Find(guid);
				if (lang != null)
					return lang;
			}

			return languageManager.Value.AllLanguages.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.UniqueNameUI, language)) ??
				languageManager.Value.AllLanguages.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.GenericNameUI, language));
		}
	}
}
