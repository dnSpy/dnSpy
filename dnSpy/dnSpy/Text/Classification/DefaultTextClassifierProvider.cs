/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Classification {
	[Export(typeof(ITextClassifierProvider))]
	[ContentType(ContentTypes.Any)]
	sealed class DefaultTextClassifierProvider : ITextClassifierProvider {
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		[ImportingConstructor]
		DefaultTextClassifierProvider(IThemeClassificationTypeService themeClassificationTypeService) => this.themeClassificationTypeService = themeClassificationTypeService;

		public ITextClassifier Create(IContentType contentType) => new DefaultTextClassifier(themeClassificationTypeService);
	}

	sealed class DefaultTextClassifier : ITextClassifier {
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		public DefaultTextClassifier(IThemeClassificationTypeService themeClassificationTypeService) => this.themeClassificationTypeService = themeClassificationTypeService ?? throw new ArgumentNullException(nameof(themeClassificationTypeService));

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			if (!context.Colorize)
				yield break;
			foreach (var spanData in context.Colors) {
				var ct = spanData.Data as IClassificationType ?? themeClassificationTypeService.GetClassificationType(spanData.Data as TextColor? ?? TextColor.Text);
				yield return new TextClassificationTag(spanData.Span, ct);
			}
		}
	}
}
