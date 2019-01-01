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
using System.Linq;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Debugger.UI.Wpf {
	[Export(typeof(ITextBlockContentInfoFactory))]
	sealed class TextBlockContentInfoFactory : ITextBlockContentInfoFactory {
		readonly ITextElementFactory textElementFactory;
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly ITextClassifierAggregatorService textClassifierAggregatorService;
		readonly Dictionary<IContentType, ITextClassifierAggregator> toAggregator;

		[ImportingConstructor]
		TextBlockContentInfoFactory(ITextElementFactory textElementFactory, IContentTypeRegistryService contentTypeRegistryService, ITextClassifierAggregatorService textClassifierAggregatorService) {
			this.textElementFactory = textElementFactory;
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.textClassifierAggregatorService = textClassifierAggregatorService;
			toAggregator = new Dictionary<IContentType, ITextClassifierAggregator>();
		}

		public TextBlockContentInfo Create(int version, IClassificationFormatMap classificationFormatMap, TextClassifierContext context, string contentType, TextElementFlags flags, double opacity) {
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (context == null)
				throw new ArgumentNullException(nameof(context));
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			var ct = contentTypeRegistryService.GetContentType(contentType);
			if (ct == null)
				throw new ArgumentException($"Invalid content type: {contentType}");

			if (!toAggregator.TryGetValue(ct, out var aggregator))
				toAggregator.Add(ct, aggregator = textClassifierAggregatorService.Create(ct));

			var tags = aggregator.GetTags(context).ToArray();
			return new TextBlockContentInfo(textElementFactory, version, classificationFormatMap, context.Text, tags, flags, opacity);
		}
	}
}
