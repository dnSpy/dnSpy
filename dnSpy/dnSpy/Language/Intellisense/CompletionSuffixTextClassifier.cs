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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Language.Intellisense.Classification;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	[Export(typeof(ITextClassifierProvider))]
	[ContentType(ContentTypes.CompletionSuffix)]
	sealed class CompletionSuffixTextClassifierProvider : ITextClassifierProvider {
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		[ImportingConstructor]
		CompletionSuffixTextClassifierProvider(IThemeClassificationTypeService themeClassificationTypeService) => this.themeClassificationTypeService = themeClassificationTypeService;

		public ITextClassifier Create(IContentType contentType) => new CompletionSuffixTextClassifier(themeClassificationTypeService);
	}

	sealed class CompletionSuffixTextClassifier : ITextClassifier {
		readonly IClassificationType completionSuffixClassificationType;

		public CompletionSuffixTextClassifier(IThemeClassificationTypeService themeClassificationTypeService) {
			if (themeClassificationTypeService == null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			completionSuffixClassificationType = themeClassificationTypeService.GetClassificationType(TextColor.CompletionSuffix);
		}

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			if (!context.Colorize)
				yield break;
			if (!(context is CompletionSuffixClassifierContext))
				yield break;
			yield return new TextClassificationTag(new Span(0, context.Text.Length), completionSuffixClassificationType);
		}
	}
}
