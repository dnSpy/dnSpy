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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using dnSpy.Contracts;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.ToolBars;
using dnSpy.Contracts.TreeView;
using dnSpy.Files.TreeView;
using dnSpy.Images;
using dnSpy.Languages;
using dnSpy.Menus;
using dnSpy.Settings;
using dnSpy.Tabs;
using dnSpy.Themes;
using dnSpy.ToolBars;
using dnSpy.TreeView;

namespace dnSpy {
	public static class AppCreator {//TODO: Shouldn't be public
		public static void Create(IEnumerable<Assembly> asms, string pattern) {
			var container = InitializeCompositionContainer(asms, pattern);
			var appImpl = container.GetExportedValue<AppImpl>();
			appImpl.CompositionContainer = container;
		}

		static CompositionContainer InitializeCompositionContainer(IEnumerable<Assembly> asms, string pattern) {
			var aggregateCatalog = new AggregateCatalog();
			var ourAsm = typeof(AppCreator).Assembly;
			aggregateCatalog.Catalogs.Add(new AssemblyCatalog(ourAsm));
			foreach (var asm in asms) {
				if (ourAsm != asm)
					aggregateCatalog.Catalogs.Add(new AssemblyCatalog(asm));
			}
			AddFiles(aggregateCatalog, pattern);
			return new CompositionContainer(aggregateCatalog);
		}

		static void AddFiles(AggregateCatalog aggregateCatalog, string pattern) {
			var dir = Path.GetDirectoryName(typeof(AppCreator).Assembly.Location);
			var random = new Random();
			var files = Directory.GetFiles(dir, pattern).OrderBy(a => random.Next()).ToArray();
			foreach (var file in files) {
				try {
					aggregateCatalog.Catalogs.Add(new AssemblyCatalog(Assembly.LoadFile(file)));
				}
				catch {
					Debug.Fail(string.Format("Failed to load file '{0}'", file));
				}
			}
		}
	}

	[Export, Export(typeof(IApp)), PartCreationPolicy(CreationPolicy.Shared)]
	public sealed class AppImpl : IApp {//TODO: REMOVE public
		public Version Version {
			get { return GetType().Assembly.GetName().Version; }
		}

		public IMenuManager MenuManager {
			get { return menuManager; }
		}
		[Import]
		/*readonly*/ MenuManager menuManager;

		public IToolBarManager ToolBarManager {
			get { return toolBarManager; }
		}
		[Import]
		/*readonly*/ ToolBarManager toolBarManager;

		public IThemeManager ThemeManager {
			get { return themeManager; }
		}
		[Import]
		/*readonly*/ ThemeManager themeManager;

		public IImageManager ImageManager {
			get { return imageManager; }
		}
		[Import]
		/*readonly*/ ImageManager imageManager;

		public IDotNetImageManager DotNetImageManager {
			get { return dotNetImageManager; }
		}
		[Import]
		/*readonly*/ DotNetImageManager dotNetImageManager;

		public ISettingsManager SettingsManager {
			get { return settingsManager; }
		}
		[Import]
		/*readonly*/ SettingsManager settingsManager;

		public ITreeViewManager TreeViewManager {
			get { return treeViewManager; }
		}
		[Import]
		/*readonly*/ TreeViewManager treeViewManager;

		public IFileTreeView FileTreeView {
			get { return fileTreeView; }
		}
		[Import]
		/*readonly*/ FileTreeView fileTreeView;

		public ILanguageManager LanguageManager {
			get { return languageManager; }
		}
		[Import]
		/*readonly*/ LanguageManager languageManager;

		public ITabManagerCreator TabManagerCreator {
			get { return tabManagerCreator; }
		}
		[Import]
		/*readonly*/ TabManagerCreator tabManagerCreator;

		public CompositionContainer CompositionContainer {
			get { return compositionContainer; }
			internal set { compositionContainer = value; }
		}
		CompositionContainer compositionContainer;

		AppImpl() {
			DnSpy.App = this;
			this.themeManager = null;
			this.imageManager = null;
			this.dotNetImageManager = null;
			this.menuManager = null;
			this.toolBarManager = null;
			this.settingsManager = null;
			this.treeViewManager = null;
			this.fileTreeView = null;
			this.languageManager = null;
			this.tabManagerCreator = null;
		}

		public void InitializeThemes(string themeName) {
			themeManager.Initialize(themeName);
		}

		public void UpdateResources(ITheme theme, System.Windows.ResourceDictionary resources) {//TODO: REMOVE
			((Theme)theme).UpdateResources(resources);
		}

		public void InitializeSettings() {//TODO: REMOVE
			try {
				new XmlSettingsReader(settingsManager).Read();
			}
			catch {
				//TODO: Show error to user
			}
		}

		public void SaveSettings() {//TODO: REMOVE
			try {
				new XmlSettingsWriter(settingsManager).Write();
			}
			catch {
				//TODO: Show error to user
			}
		}
	}
}
