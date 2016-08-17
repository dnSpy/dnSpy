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
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Files.TreeView {
	[Export(typeof(IFileTreeViewProvider))]
	sealed class FileTreeViewProvider : IFileTreeViewProvider {
		readonly IThemeManager themeManager;
		readonly ITreeViewManager treeViewManager;
		readonly ILanguageManager languageManager;
		readonly IFileManagerProvider fileManagerProvider;
		readonly IFileTreeViewSettings fileTreeViewSettings;
		readonly IMenuManager menuManager;
		readonly IDotNetImageManager dotNetImageManager;
		readonly IWpfCommandManager wpfCommandManager;
		readonly IResourceNodeFactory resourceNodeFactory;
		readonly IAppSettings appSettings;
		readonly Lazy<IDnSpyFileNodeProvider, IDnSpyFileNodeProviderMetadata>[] dnSpyFileNodeProviders;
		readonly Lazy<IFileTreeNodeDataFinder, IFileTreeNodeDataFinderMetadata>[] mefFinders;

		[ImportingConstructor]
		FileTreeViewProvider(IThemeManager themeManager, ITreeViewManager treeViewManager, ILanguageManager languageManager, IFileManagerProvider fileManagerProvider, IFileTreeViewSettings fileTreeViewSettings, IMenuManager menuManager, IDotNetImageManager dotNetImageManager, IWpfCommandManager wpfCommandManager, IResourceNodeFactory resourceNodeFactory, IAppSettings appSettings, [ImportMany] IEnumerable<Lazy<IDnSpyFileNodeProvider, IDnSpyFileNodeProviderMetadata>> dnSpyFileNodeProviders, [ImportMany] IEnumerable<Lazy<IFileTreeNodeDataFinder, IFileTreeNodeDataFinderMetadata>> mefFinders) {
			this.themeManager = themeManager;
			this.treeViewManager = treeViewManager;
			this.languageManager = languageManager;
			this.fileManagerProvider = fileManagerProvider;
			this.fileTreeViewSettings = fileTreeViewSettings;
			this.menuManager = menuManager;
			this.dotNetImageManager = dotNetImageManager;
			this.wpfCommandManager = wpfCommandManager;
			this.resourceNodeFactory = resourceNodeFactory;
			this.appSettings = appSettings;
			this.dnSpyFileNodeProviders = dnSpyFileNodeProviders.ToArray();
			this.mefFinders = mefFinders.ToArray();
		}

		public IFileTreeView Create(IFileTreeNodeFilter filter) =>
			new FileTreeView(false, filter, themeManager, treeViewManager, languageManager, fileManagerProvider.Create(), fileTreeViewSettings, menuManager, dotNetImageManager, wpfCommandManager, resourceNodeFactory, appSettings, dnSpyFileNodeProviders.ToArray(), mefFinders.ToArray());
	}
}
