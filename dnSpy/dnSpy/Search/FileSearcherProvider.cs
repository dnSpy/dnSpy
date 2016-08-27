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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Search;

namespace dnSpy.Search {
	[Export(typeof(IFileSearcherProvider))]
	sealed class FileSearcherProvider : IFileSearcherProvider {
		readonly IFileTreeView fileTreeView;
		readonly IImageManager imageManager;
		readonly IDotNetImageManager dotNetImageManager;
		readonly IDecompilerManager decompilerManager;

		[ImportingConstructor]
		FileSearcherProvider(IFileTreeView fileTreeView, IImageManager imageManager, IDotNetImageManager dotNetImageManager, IDecompilerManager decompilerManager) {
			this.fileTreeView = fileTreeView;
			this.imageManager = imageManager;
			this.dotNetImageManager = dotNetImageManager;
			this.decompilerManager = decompilerManager;
		}

		public IFileSearcher Create(FileSearcherOptions options) {
			var searchResultContext = new SearchResultContext {
				SyntaxHighlight = true,
				Decompiler = decompilerManager.Decompiler,
				ImageManager = imageManager,
				BackgroundType = BackgroundType.Search,
			};
			return new FileSearcher(options, fileTreeView, dotNetImageManager, searchResultContext);
		}
	}
}
