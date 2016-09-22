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
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Language.Intellisense.Classification;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense.Classification {
	[Export(typeof(ICompletionClassifierProvider))]
	[ContentType(ContentTypes.Any)]
	sealed class FilterMatchCompletionClassifierProvider : ICompletionClassifierProvider {
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		[ImportingConstructor]
		FilterMatchCompletionClassifierProvider(IThemeClassificationTypeService themeClassificationTypeService) {
			this.themeClassificationTypeService = themeClassificationTypeService;
		}

		public ICompletionClassifier Create(CompletionCollection completionSet) => new FilterMatchCompletionClassifier(themeClassificationTypeService, completionSet);
	}

	sealed class FilterMatchCompletionClassifier : ICompletionClassifier {
		readonly CompletionCollection completionSet;
		readonly IClassificationType completionMatchHighlightClassificationType;

		public FilterMatchCompletionClassifier(IThemeClassificationTypeService themeClassificationTypeService, CompletionCollection completionSet) {
			if (themeClassificationTypeService == null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			if (completionSet == null)
				throw new ArgumentNullException(nameof(completionSet));
			this.completionMatchHighlightClassificationType = themeClassificationTypeService.GetClassificationType(TextColor.CompletionMatchHighlight);
			this.completionSet = completionSet;
		}

		public IEnumerable<CompletionClassificationTag> GetTags(CompletionClassifierContext context) {
			var spans = completionSet.GetHighlightedSpansInDisplayText(context.DisplayText);
			if (spans == null)
				yield break;
			foreach (var span in spans)
				yield return new CompletionClassificationTag(span, completionMatchHighlightClassificationType);
		}
	}
}
