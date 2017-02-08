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
using System.Diagnostics;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Roslyn.Internal.QuickInfo;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Roslyn.Shared.Intellisense.QuickInfo {
	interface IQuickInfoContentCreatorProvider {
		IQuickInfoContentCreator Create(ITextView textView);
	}

	interface IQuickInfoContentCreator {
		IEnumerable<object> Create(QuickInfoItem item);
	}

	[Export(typeof(IQuickInfoContentCreatorProvider))]
	sealed class QuickInfoContentCreatorProvider : IQuickInfoContentCreatorProvider {
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		[ImportingConstructor]
		QuickInfoContentCreatorProvider(IClassificationFormatMapService classificationFormatMapService, IThemeClassificationTypeService themeClassificationTypeService) {
			this.classificationFormatMapService = classificationFormatMapService;
			this.themeClassificationTypeService = themeClassificationTypeService;
		}

		public IQuickInfoContentCreator Create(ITextView textView) => new QuickInfoContentCreator(classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc), themeClassificationTypeService, textView);
	}

	sealed class QuickInfoContentCreator : IQuickInfoContentCreator {
		readonly IClassificationFormatMap classificationFormatMap;
		readonly IThemeClassificationTypeService themeClassificationTypeService;
		readonly ITextView textView;

		public QuickInfoContentCreator(IClassificationFormatMap classificationFormatMap, IThemeClassificationTypeService themeClassificationTypeService, ITextView textView) {
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (themeClassificationTypeService == null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.classificationFormatMap = classificationFormatMap;
			this.themeClassificationTypeService = themeClassificationTypeService;
			this.textView = textView;
		}

		public IEnumerable<object> Create(QuickInfoItem item) {
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			switch (item.Content.Type) {
			case PredefinedQuickInfoContentTypes.Information:
				return Create((InformationQuickInfoContent)item.Content);

			case PredefinedQuickInfoContentTypes.CodeSpan:
				return Create((CodeSpanQuickInfoContent)item.Content);

			default:
				Debug.Fail($"Unknown QuickInfo content: {item.Content.Type}");
				return Array.Empty<object>();
			}
		}

		IEnumerable<object> Create(InformationQuickInfoContent content) {
			yield return new InformationQuickInfoContentControl {
				DataContext = new InformationQuickInfoContentVM(textView, content, classificationFormatMap, themeClassificationTypeService),
			};
		}

		IEnumerable<object> Create(CodeSpanQuickInfoContent content) {
			yield break;//TODO:
		}
	}
}
