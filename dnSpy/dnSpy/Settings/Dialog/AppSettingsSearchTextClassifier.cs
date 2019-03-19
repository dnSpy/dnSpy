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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Settings.Dialog {
	[Export(typeof(ITextClassifierProvider))]
	[ContentType(ContentTypes.OptionsDialogText)]
	sealed class AppSettingsSearchTextClassifierProvider : ITextClassifierProvider {
		readonly IClassificationType appSettingsTextMatchHighlightClassificationType;

		[ImportingConstructor]
		AppSettingsSearchTextClassifierProvider(IThemeClassificationTypeService themeClassificationTypeService) => appSettingsTextMatchHighlightClassificationType = themeClassificationTypeService.GetClassificationType(TextColor.AppSettingsTextMatchHighlight);

		public ITextClassifier Create(IContentType contentType) => new AppSettingsSearchTextClassifier(appSettingsTextMatchHighlightClassificationType);
	}

	sealed class AppSettingsSearchTextClassifier : ITextClassifier {
		readonly IClassificationType appSettingsTextMatchHighlightClassificationType;

		public AppSettingsSearchTextClassifier(IClassificationType appSettingsTextMatchHighlightClassificationType) => this.appSettingsTextMatchHighlightClassificationType = appSettingsTextMatchHighlightClassificationType ?? throw new ArgumentNullException(nameof(appSettingsTextMatchHighlightClassificationType));

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			var optionsContext = context as AppSettingsTextClassifierContext;
			if (optionsContext == null)
				yield break;
			foreach (var span in optionsContext.SearchMatcher.GetMatchSpans(optionsContext.Text))
				yield return new TextClassificationTag(span, appSettingsTextMatchHighlightClassificationType);
		}
	}
}
