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
using dnSpy.Contracts.TreeView.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Settings.Dialog {
	[Export(typeof(ITextClassifierProvider))]
	[ContentType(TreeViewContentTypes.TreeViewNodeAppSettings)]
	sealed class AppSettingsTreeViewNodeSearchTextClassifierProvider : ITextClassifierProvider {
		readonly IClassificationType appSettingsTreeViewNodeMatchHighlightClassificationType;

		[ImportingConstructor]
		AppSettingsTreeViewNodeSearchTextClassifierProvider(IThemeClassificationTypeService themeClassificationTypeService) {
			appSettingsTreeViewNodeMatchHighlightClassificationType = themeClassificationTypeService.GetClassificationType(TextColor.AppSettingsTreeViewNodeMatchHighlight);
		}

		public ITextClassifier Create(IContentType contentType) => new AppSettingsTreeViewNodeSearchTextClassifier(appSettingsTreeViewNodeMatchHighlightClassificationType);
	}

	sealed class AppSettingsTreeViewNodeSearchTextClassifier : ITextClassifier {
		readonly IClassificationType appSettingsTreeViewNodeMatchHighlightClassificationType;

		public AppSettingsTreeViewNodeSearchTextClassifier(IClassificationType appSettingsTreeViewNodeMatchHighlightClassificationType) {
			if (appSettingsTreeViewNodeMatchHighlightClassificationType == null)
				throw new ArgumentNullException(nameof(appSettingsTreeViewNodeMatchHighlightClassificationType));
			this.appSettingsTreeViewNodeMatchHighlightClassificationType = appSettingsTreeViewNodeMatchHighlightClassificationType;
		}

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			var tvContext = context as AppSettingsTreeViewNodeClassifierContext;
			if (tvContext == null)
				yield break;
			foreach (var span in tvContext.SearchMatcher.GetMatchSpans(tvContext.Text))
				yield return new TextClassificationTag(span, appSettingsTreeViewNodeMatchHighlightClassificationType);
		}
	}
}
