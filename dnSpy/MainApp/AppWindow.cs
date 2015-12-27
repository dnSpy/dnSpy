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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.ToolWindows.App;
using dnSpy.Controls;
using dnSpy.Events;
using dnSpy.Files.Tabs.TextEditor;
using ICSharpCode.AvalonEdit.Utils;

namespace dnSpy.MainApp {
	[Export, Export(typeof(IAppWindow)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class AppWindow : IAppWindow, IDnSpyLoaderContentProvider {
		public IFileTabManager FileTabManager {
			get { return fileTabManager; }
		}
		readonly IFileTabManager fileTabManager;

		public IFileTreeView FileTreeView {
			get { return fileTabManager.FileTreeView; }
		}

		public IMainToolWindowManager ToolWindowManager {
			get { return mainWindowControl; }
		}

		public ILanguageManager LanguageManager {
			get { return languageManager; }
		}

		public IAppStatusBar StatusBar {
			get { return statusBar; }
		}
		readonly AppStatusBar statusBar;

		Window IAppWindow.MainWindow {
			get { return mainWindow; }
		}
		internal MainWindow MainWindow {
			get { return mainWindow; }
		}
		MainWindow mainWindow;

		public IWpfCommands MainWindowCommands {
			get { return mainWindowCommands; }
		}
		readonly IWpfCommands mainWindowCommands;

		public IAppSettings AppSettings {
			get { return appSettings; }
		}
		readonly IAppSettings appSettings;

		public bool AppLoaded { get; internal set; }

		sealed class UISettings {
			static readonly Guid SETTINGS_GUID = new Guid("33E1988B-8EFF-4F4C-A064-FA99A7D0C64D");
			const string SAVEDWINDOWSTATE_SECTION = "SavedWindowState";
			const string MAINWINDOWCONTROLSTATE_SECTION = "MainWindowControlState";

			readonly ISettingsManager settingsManager;

			public SavedWindowState SavedWindowState;
			public MainWindowControlState MainWindowControlState;

			public UISettings(ISettingsManager settingsManager) {
				this.settingsManager = settingsManager;
			}

			public void Read() {
				var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
				this.SavedWindowState = new SavedWindowState().Read(sect.GetOrCreateSection(SAVEDWINDOWSTATE_SECTION));
				this.MainWindowControlState = new MainWindowControlState().Read(sect.GetOrCreateSection(MAINWINDOWCONTROLSTATE_SECTION));
			}

			public void Write() {
				var sect = settingsManager.RecreateSection(SETTINGS_GUID);
				SavedWindowState.Write(sect.GetOrCreateSection(SAVEDWINDOWSTATE_SECTION));
				MainWindowControlState.Write(sect.GetOrCreateSection(MAINWINDOWCONTROLSTATE_SECTION));
			}
		}

		readonly UISettings uiSettings;
		readonly IWpfCommandManager wpfCommandManager;
		readonly StackedContent<IStackedContentChild> stackedContent;
		readonly IThemeManager themeManager;
		readonly IImageManager imageManager;
		readonly AppToolBar appToolBar;
		readonly MainWindowControl mainWindowControl;
		readonly ILanguageManager languageManager;

		[ImportingConstructor]
		AppWindow(IThemeManager themeManager, IImageManager imageManager, IAppSettings appSettings, ISettingsManager settingsManager, IFileTabManager fileTabManager, AppToolBar appToolBar, MainWindowControl mainWindowControl, IWpfCommandManager wpfCommandManager, ILanguageManager languageManager) {
			this.uiSettings = new UISettings(settingsManager);
			this.uiSettings.Read();
			this.appSettings = appSettings;
			this.stackedContent = new StackedContent<IStackedContentChild>(margin: new Thickness(6));
			this.themeManager = themeManager;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			this.imageManager = imageManager;
			this.fileTabManager = fileTabManager;
			this.statusBar = new AppStatusBar();
			this.appToolBar = appToolBar;
			this.mainWindowControl = mainWindowControl;
			this.wpfCommandManager = wpfCommandManager;
			this.languageManager = languageManager;
			this.mainWindowCommands = wpfCommandManager.GetCommands(CommandConstants.GUID_MAINWINDOW);
			this.mainWindowClosing = new WeakEventList<CancelEventArgs>();
			this.mainWindowClosed = new WeakEventList<EventArgs>();
			this.appSettings.PropertyChanged += AppSettings_PropertyChanged;
			InitializeTextFormatterProvider();
		}

		void AppSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "UseNewRenderer_TextEditor")
				InitializeTextFormatterProvider();
		}

		void InitializeTextFormatterProvider() {
			var newValue = appSettings.UseNewRenderer_TextEditor ? TextFormatterProvider.GlyphRunFormatter : TextFormatterProvider.BuiltIn;
			TextFormatterFactory.DefaultTextFormatterProvider = newValue;
			var tabs = fileTabManager.VisibleFirstTabs.Where(a => a.UIContext is ITextEditorUIContextImpl).ToArray();
			foreach (var tab in tabs)
				((ITextEditorUIContextImpl)tab.UIContext).OnUseNewRendererChanged();
			fileTabManager.Refresh(tabs);
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			RefreshToolBar();
		}

		static readonly Rect DefaultWindowLocation = new Rect(10, 10, 1300, 730);
		public Window InitializeMainWindow() {
			var sc = new StackedContent<IStackedContentChild>(false);
			sc.AddChild(appToolBar, StackedContentChildInfo.CreateVertical(new GridLength(0, GridUnitType.Auto)));
			sc.AddChild(stackedContent, StackedContentChildInfo.CreateVertical(new GridLength(1, GridUnitType.Star)));
			sc.AddChild(statusBar, StackedContentChildInfo.CreateVertical(new GridLength(0, GridUnitType.Auto)));
			mainWindow = new MainWindow(themeManager, imageManager, sc.UIObject);
			AddTitleInfo(IntPtr.Size == 4 ? "x86" : "x64");
			wpfCommandManager.Add(CommandConstants.GUID_MAINWINDOW, mainWindow);
			new SavedWindowStateRestorer(mainWindow, uiSettings.SavedWindowState, DefaultWindowLocation);
			mainWindow.Closing += MainWindow_Closing;
			mainWindow.Closed += MainWindow_Closed;
			mainWindow.GotKeyboardFocus += MainWindow_GotKeyboardFocus;
			RefreshToolBar();
			return mainWindow;
		}

		void IDnSpyLoaderContentProvider.SetLoadingContent(object content) {
			Debug.Assert(stackedContent.Count == 0);
			stackedContent.AddChild(StackedContentChildImpl.GetOrCreate(content, content));
		}

		void IDnSpyLoaderContentProvider.RemoveLoadingContent() {
			stackedContent.Clear();
			stackedContent.AddChild(mainWindowControl);
			mainWindowControl.Initialize(StackedContentChildImpl.GetOrCreate(fileTabManager.TabGroupManager, fileTabManager.TabGroupManager.UIObject), uiSettings.MainWindowControlState);
		}

		void MainWindow_Closing(object sender, CancelEventArgs e) {
			mainWindowClosing.Raise(this, e);
			if (e.Cancel)
				return;

			uiSettings.SavedWindowState = new SavedWindowState(mainWindow);
			uiSettings.MainWindowControlState = mainWindowControl.CreateState();
			uiSettings.Write();
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

		void UpdateTitle() {
			this.mainWindow.Title = GetDefaultTitle();
		}

		string GetDefaultTitle() {
			var t = string.Format("dnSpy ({0})", string.Join(", ", titleInfos.ToArray()));
			return t;
		}
		readonly List<string> titleInfos = new List<string>();

		public void AddTitleInfo(string info) {
			if (titleInfos.Contains(info))
				return;
			titleInfos.Add(info);
			UpdateTitle();
		}

		public void RemoveTitleInfo(string info) {
			if (titleInfos.Remove(info))
				UpdateTitle();
		}
	}
}
