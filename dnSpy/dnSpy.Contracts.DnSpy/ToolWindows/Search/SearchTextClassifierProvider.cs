/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.ToolWindows.Search {
	abstract class SearchTextClassifierProviderBase : ITextClassifierProvider {
		readonly IClassificationType listFindMatchHighlightClassificationType;

		protected SearchTextClassifierProviderBase(IThemeClassificationTypeService themeClassificationTypeService) =>
			listFindMatchHighlightClassificationType = themeClassificationTypeService.GetClassificationType(TextColor.ListFindMatchHighlight);

		public ITextClassifier? Create(IContentType contentType) => new SearchTextClassifier(listFindMatchHighlightClassificationType);
	}

	sealed class SearchTextClassifier : ITextClassifier {
		readonly IClassificationType listFindMatchHighlightClassificationType;

		public SearchTextClassifier(IClassificationType listFindMatchHighlightClassificationType) =>
			this.listFindMatchHighlightClassificationType = listFindMatchHighlightClassificationType ?? throw new ArgumentNullException(nameof(listFindMatchHighlightClassificationType));

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			var searchContext = context as SearchTextClassifierContext;
			if (searchContext is null)
				yield break;
			foreach (var span in searchContext.SearchMatcher.GetMatchSpans(searchContext.Tag, searchContext.Text))
				yield return new TextClassificationTag(span, listFindMatchHighlightClassificationType);
		}
	}
}
