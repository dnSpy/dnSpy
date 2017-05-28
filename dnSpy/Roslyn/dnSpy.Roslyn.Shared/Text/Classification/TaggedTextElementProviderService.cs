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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	[Export(typeof(ITaggedTextElementProviderService))]
	sealed class TaggedTextElementProviderService : ITaggedTextElementProviderService {
		readonly ITextClassifierAggregatorService textClassifierAggregatorService;
		readonly IClassificationFormatMapService classificationFormatMapService;

		[ImportingConstructor]
		TaggedTextElementProviderService(ITextClassifierAggregatorService textClassifierAggregatorService, IClassificationFormatMapService classificationFormatMapService) {
			this.textClassifierAggregatorService = textClassifierAggregatorService;
			this.classificationFormatMapService = classificationFormatMapService;
		}

		public ITaggedTextElementProvider Create(IContentType contentType, string category) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			if (category == null)
				throw new ArgumentNullException(nameof(category));
			return new TaggedTextElementProvider(contentType, textClassifierAggregatorService, classificationFormatMapService.GetClassificationFormatMap(category));
		}
	}
}
