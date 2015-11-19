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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;
using dnSpy.Controls;
using dnSpy.Events;
using dnSpy.Files.Tabs;
using dnSpy.Settings;

namespace dnSpy.MainApp {
	[Export, Export(typeof(IAppWindow)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class AppWindow : IAppWindow {
		public IFileTabManager FileTabManager {
			get { return fileTabManager; }
		}
		readonly FileTabManager fileTabManager;

		public IAppStatusBar StatusBar {
			get { return statusBar; }
		}
		readonly AppStatusBar statusBar;

		readonly StackedContent<IStackedContentChild> stackedContent;
		readonly IThemeManager themeManager;
		readonly IImageManager imageManager;
		readonly SettingsManager settingsManager;
		readonly AppToolBar appToolBar;

		internal MainWindow MainWindow {
			get { return mainWindow; }
		}
		MainWindow mainWindow;

		public IAppSettings AppSettings {
			get { return appSettings; }
		}
		readonly AppSettingsImpl appSettings;

		[ImportingConstructor]
		AppWindow(IThemeManager themeManager, IImageManager imageManager, AppSettingsImpl appSettings, SettingsManager settingsManager, FileTabManager fileTabManager, AppToolBar appToolBar) {
			this.appSettings = appSettings;
			this.stackedContent = new StackedContent<IStackedContentChild>();
			this.themeManager = themeManager;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			this.imageManager = imageManager;
			this.settingsManager = settingsManager;
			this.fileTabManager = fileTabManager;
			this.statusBar = new AppStatusBar();
			this.appToolBar = appToolBar;
			this.mainWindowClosing = new WeakEventList<CancelEventArgs>();
			this.mainWindowClosed = new WeakEventList<EventArgs>();
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			RefreshToolBar();
		}

		static Rect DefaultWindowLocation = new Rect(10, 10, 1300, 730);
		public Window InitializeMainWindow() {
			var sc = new StackedContent<IStackedContentChild>(false);
			sc.AddChild(appToolBar, new GridLength(0, GridUnitType.Auto));
			sc.AddChild(stackedContent, new GridLength(1, GridUnitType.Star));
			sc.AddChild(statusBar, new GridLength(0, GridUnitType.Auto));
			mainWindow = new MainWindow(themeManager, imageManager, sc.UIObject);
			new SavedWindowStateRestorer(mainWindow, appSettings.SavedWindowState, DefaultWindowLocation);
			mainWindow.Closing += MainWindow_Closing;
			mainWindow.Closed += MainWindow_Closed;
			RefreshToolBar();
			return mainWindow;
		}

		void MainWindow_Closing(object sender, CancelEventArgs e) {
			mainWindowClosing.Raise(this, e);
			if (e.Cancel)
				return;

			appSettings.SavedWindowState = new SavedWindowState(mainWindow);
		}

		void MainWindow_Closed(object sender, EventArgs e) {
			mainWindowClosed.Raise(this, e);
		}

		public event EventHandler<CancelEventArgs> MainWindowClosing {
			add { mainWindowClosing.Add(value); }
			remove { mainWindowClosing.Remove(value); }
		}
		readonly WeakEventList<CancelEventArgs> mainWindowClosing;

		public event EventHandler<EventArgs> MainWindowClosed {
			add { mainWindowClosed.Add(value); }
			remove { mainWindowClosed.Remove(value); }
		}
		readonly WeakEventList<EventArgs> mainWindowClosed;

		public void RefreshToolBar() {
			if (mainWindow != null)
				appToolBar.Initialize(mainWindow);
		}
	}
}
