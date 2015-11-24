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
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Themes;
using dnSpy.Controls;
using dnSpy.Events;
using dnSpy.Files.Tabs;
using dnSpy.Settings;
using ICSharpCode.AvalonEdit.Utils;

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

		public IWpfCommandManager WpfCommandManager {
			get { return wpfCommandManager; }
		}
		readonly IWpfCommandManager wpfCommandManager;

		[ImportingConstructor]
		AppWindow(IThemeManager themeManager, IImageManager imageManager, AppSettingsImpl appSettings, SettingsManager settingsManager, FileTabManager fileTabManager, AppToolBar appToolBar, IWpfCommandManager wpfCommandManager) {
			this.appSettings = appSettings;
			this.stackedContent = new StackedContent<IStackedContentChild>();
			this.themeManager = themeManager;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			this.imageManager = imageManager;
			this.settingsManager = settingsManager;
			this.fileTabManager = fileTabManager;
			this.statusBar = new AppStatusBar();
			this.appToolBar = appToolBar;
			this.wpfCommandManager = wpfCommandManager;
			this.mainWindowClosing = new WeakEventList<CancelEventArgs>();
			this.mainWindowClosed = new WeakEventList<EventArgs>();
			this.textFormatterChanged = new WeakEventList<EventArgs>();
			InitializeTextFormatterProvider();
		}

		void InitializeTextFormatterProvider() {
			var newValue = appSettings.UseNewRenderer ? TextFormatterProvider.GlyphRunFormatter : TextFormatterProvider.BuiltIn;
			if (TextFormatterFactory.TextFormatterProvider != newValue) {
				TextFormatterFactory.TextFormatterProvider = newValue;
				fileTabManager.FileTreeView.OnTextFormatterChanged();
				textFormatterChanged.Raise(this, EventArgs.Empty);
				//TODO: Refresh all text editors, hex editors
			}
		}

		public event EventHandler<EventArgs> TextFormatterChanged {
			add { textFormatterChanged.Add(value); }
			remove { textFormatterChanged.Remove(value); }
		}
		readonly WeakEventList<EventArgs> textFormatterChanged;

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			RefreshToolBar();
		}

		static Rect DefaultWindowLocation = new Rect(10, 10, 1300, 730);
		public Window InitializeMainWindow() {
			var sc = new StackedContent<IStackedContentChild>(false);
			sc.AddChild(appToolBar, StackedContentChildInfo.CreateVertical(new GridLength(0, GridUnitType.Auto)));
			sc.AddChild(stackedContent, StackedContentChildInfo.CreateVertical(new GridLength(1, GridUnitType.Star)));
			sc.AddChild(statusBar, StackedContentChildInfo.CreateVertical(new GridLength(0, GridUnitType.Auto)));
			mainWindow = new MainWindow(themeManager, imageManager, sc.UIObject);
			wpfCommandManager.Add(CommandConstants.GUID_MAINWINDOW, mainWindow);
			new SavedWindowStateRestorer(mainWindow, appSettings.SavedWindowState, DefaultWindowLocation);
			mainWindow.Closing += MainWindow_Closing;
			mainWindow.Closed += MainWindow_Closed;
			mainWindow.GotKeyboardFocus += MainWindow_GotKeyboardFocus;
			stackedContent.AddChild(StackedContentChildImpl.GetOrCreate(fileTabManager.FileTreeView.TreeView, fileTabManager.FileTreeView.TreeView.UIObject), StackedContentChildInfo.CreateHorizontal(new GridLength(250, GridUnitType.Pixel), 100));
			stackedContent.AddChild(StackedContentChildImpl.GetOrCreate(fileTabManager.TabGroupManager, fileTabManager.TabGroupManager.UIObject), StackedContentChildInfo.CreateHorizontal(new GridLength(1, GridUnitType.Star), 100));
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

		void MainWindow_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			if (e.NewFocus == MainWindow) {
				var g = fileTabManager.TabGroupManager.ActiveTabGroup;
				if (g != null && g.ActiveTabContent != null) {
					g.SetFocus(g.ActiveTabContent);
					e.Handled = true;
					return;
				}
			}
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
