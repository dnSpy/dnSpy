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
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Plugin;
using dnSpy.Settings;
using dnSpy.Shared.UI.MVVM;

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
		CompositionContainer compositionContainer;

		public App(bool readSettings) {
			InitializeComponent();
			UIFixes();

			var asms = new List<Assembly>();
			asms.Add(typeof(EnumVM).Assembly);			// dnSpy.Shared.UI
			compositionContainer = AppCreator.Create(asms, "*.Plugin.dll", readSettings);
			compositionContainer.ComposeParts(this);

			this.Exit += App_Exit;
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
			dnSpyLoaderManager.OnAppLoaded += DnSpyLoaderManager_OnAppLoaded;
			dnSpyLoaderManager.Initialize(appWindow, win);
			pluginManager.LoadPlugins(this.Resources.MergedDictionaries);
			win.Show();
		}

		void DnSpyLoaderManager_OnAppLoaded(object sender, EventArgs e) {
			dnSpyLoaderManager.OnAppLoaded -= DnSpyLoaderManager_OnAppLoaded;
			appWindow.AppLoaded = true;
			pluginManager.OnAppLoaded();
		}
	}
}
