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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using dnSpy.Contracts;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.ToolBars;
using dnSpy.Contracts.TreeView;
using dnSpy.Files.Tabs;
using dnSpy.Files.TreeView;
using dnSpy.Images;
using dnSpy.Languages;
using dnSpy.Menus;
using dnSpy.Settings;
using dnSpy.Tabs;
using dnSpy.Themes;
using dnSpy.ToolBars;
using dnSpy.TreeView;

namespace dnSpy.MainApp {
	[Export, Export(typeof(IApp)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class AppImpl : IApp {
		public Version Version {
			get { return GetType().Assembly.GetName().Version; }
		}

		public IMenuManager MenuManager {
			get { return menuManager; }
		}
		[Import]
		/*readonly*/ MenuManager menuManager = null;

		public IToolBarManager ToolBarManager {
			get { return toolBarManager; }
		}
		[Import]
		/*readonly*/ ToolBarManager toolBarManager = null;

		public IThemeManager ThemeManager {
			get { return themeManager; }
		}
		[Import]
		/*readonly*/ ThemeManager themeManager = null;

		public IImageManager ImageManager {
			get { return imageManager; }
		}
		[Import]
		/*readonly*/ ImageManager imageManager = null;

		public IDotNetImageManager DotNetImageManager {
			get { return dotNetImageManager; }
		}
		[Import]
		/*readonly*/ DotNetImageManager dotNetImageManager = null;

		public ISettingsManager SettingsManager {
			get { return settingsManager; }
		}
		[Import]
		/*readonly*/ SettingsManager settingsManager = null;

		public ITreeViewManager TreeViewManager {
			get { return treeViewManager; }
		}
		[Import]
		/*readonly*/ TreeViewManager treeViewManager = null;

		public IFileTreeView FileTreeView {
			get { return fileTreeView; }
		}
		[Import]
		/*readonly*/ FileTreeView fileTreeView = null;

		public ILanguageManager LanguageManager {
			get { return languageManager; }
		}
		[Import]
		/*readonly*/ LanguageManager languageManager = null;

		public ITabManagerCreator TabManagerCreator {
			get { return tabManagerCreator; }
		}
		[Import]
		/*readonly*/ TabManagerCreator tabManagerCreator = null;

		public IFileTabManager FileTabManager {
			get { return fileTabManager; }
		}
		[Import]
		/*readonly*/ FileTabManager fileTabManager = null;

		public IAppWindow AppWindow {
			get { return appWindow; }
		}
		[Import]
		/*readonly*/ IAppWindow appWindow = null;

		public CompositionContainer CompositionContainer {
			get { return compositionContainer; }
			internal set { compositionContainer = value; }
		}
		CompositionContainer compositionContainer;

		AppImpl() {
			DnSpy.App = this;
		}
	}
}
