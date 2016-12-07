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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Search {
	[Export(typeof(IDocumentSearcherProvider))]
	sealed class DocumentSearcherProvider : IDocumentSearcherProvider {
		readonly IDotNetImageService dotNetImageService;
		readonly IDecompilerService decompilerService;
		readonly ITextElementProvider textElementProvider;
		readonly IClassificationFormatMap classificationFormatMap;

		[ImportingConstructor]
		DocumentSearcherProvider(IDotNetImageService dotNetImageService, IDecompilerService decompilerService, ITextElementProvider textElementProvider, IClassificationFormatMapService classificationFormatMapService) {
			this.dotNetImageService = dotNetImageService;
			this.decompilerService = decompilerService;
			this.textElementProvider = textElementProvider;
			classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
		}

		public IDocumentSearcher Create(DocumentSearcherOptions options, IDocumentTreeView documentTreeView) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (documentTreeView == null)
				throw new ArgumentNullException(nameof(documentTreeView));
			var searchResultContext = new SearchResultContext(classificationFormatMap, textElementProvider) {
				SyntaxHighlight = true,
				Decompiler = decompilerService.Decompiler,
			};
			return new DocumentSearcher(options, documentTreeView, dotNetImageService, searchResultContext);
		}
	}
}
