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

using System.ComponentModel.Composition;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Search;
using dnSpy.Images;

namespace dnSpy.Search {
	[Export, Export(typeof(IFileSearcherCreator)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class FileSearcherCreator : IFileSearcherCreator {
		readonly IFileTreeView fileTreeView;
		readonly IImageManager imageManager;
		readonly DotNetImageManager dotNetImageManager;
		readonly ILanguageManager languageManager;

		[ImportingConstructor]
		FileSearcherCreator(IFileTreeView fileTreeView, IImageManager imageManager, DotNetImageManager dotNetImageManager, ILanguageManager languageManager) {
			this.fileTreeView = fileTreeView;
			this.imageManager = imageManager;
			this.dotNetImageManager = dotNetImageManager;
			this.languageManager = languageManager;
		}

		public IFileSearcher Create(FileSearcherOptions options) {
			var searchResultContext = new SearchResultContext {
				SyntaxHighlight = true,
				Language = languageManager.SelectedLanguage,
				ImageManager = imageManager,
				BackgroundType = BackgroundType.Search,
			};
			return new FileSearcher(options, fileTreeView, dotNetImageManager, searchResultContext);
		}
	}
}
