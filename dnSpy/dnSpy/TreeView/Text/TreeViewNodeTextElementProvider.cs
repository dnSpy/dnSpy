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
using System.Windows;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView.Text;
using dnSpy.Controls;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.TreeView.Text {
	[Export(typeof(ITreeViewNodeTextElementProvider))]
	sealed class TreeViewNodeTextElementProvider : ITreeViewNodeTextElementProvider {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly ITextClassifierAggregatorService textClassifierAggregatorService;
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly List<TextClassificationTag> tagsList;

		[ImportingConstructor]
		TreeViewNodeTextElementProvider(IContentTypeRegistryService contentTypeRegistryService, ITextClassifierAggregatorService textClassifierAggregatorService, IClassificationFormatMapService classificationFormatMapService) {
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.textClassifierAggregatorService = textClassifierAggregatorService;
			this.classificationFormatMapService = classificationFormatMapService;
			this.classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(TreeViewAppearanceCategory);
			this.tagsList = new List<TextClassificationTag>();
		}

		const string TreeViewAppearanceCategory = "dnSpy-TreeView";
		[Export(typeof(TextEditorFormatDefinition))]
		[Name(TreeViewAppearanceCategory)]
		[BaseDefinition(AppearanceCategoryConstants.TextEditor)]
		sealed class TreeViewTextEditorFormatDefinition : TextEditorFormatDefinition {
		}

		public FrameworkElement CreateTextElement(TreeViewNodeClassifierContext context, string contentType, bool filterOutNewLines, bool useNewFormatter) {
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			var ct = contentTypeRegistryService.GetContentType(contentType);
			if (ct == null)
				throw new ArgumentException($"Invalid content type: {contentType}");

			ITextClassifierAggregator aggregator = null;
			try {
				aggregator = textClassifierAggregatorService.Create(ct);
				tagsList.AddRange(aggregator.GetTags(context));
				return TextElementFactory.Create(classificationFormatMap, context.Text, context.Colorize ? tagsList : null, useNewFormatter: useNewFormatter, filterOutNewLines: filterOutNewLines);
			}
			finally {
				tagsList.Clear();
				aggregator?.Dispose();
			}
		}
	}
}
