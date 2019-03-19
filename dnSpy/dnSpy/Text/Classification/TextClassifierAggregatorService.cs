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

namespace dnSpy.Text.Classification {
	[Export(typeof(ITextClassifierAggregatorService))]
	sealed class TextClassifierAggregatorService : ITextClassifierAggregatorService {
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;
		readonly Lazy<ITextClassifierProvider, IContentTypeMetadata>[] textClassifierProviders;

		[ImportingConstructor]
		TextClassifierAggregatorService(IClassificationTypeRegistryService classificationTypeRegistryService, [ImportMany] IEnumerable<Lazy<ITextClassifierProvider, IContentTypeMetadata>> textClassifierProviders) {
			this.classificationTypeRegistryService = classificationTypeRegistryService;
			this.textClassifierProviders = textClassifierProviders.ToArray();
		}

		public ITextClassifierAggregator Create(IContentType contentType) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			var list = new List<ITextClassifier>();
			foreach (var lz in textClassifierProviders) {
				if (!contentType.IsOfAnyType(lz.Metadata.ContentTypes))
					continue;
				var classifier = lz.Value.Create(contentType);
				if (classifier != null)
					list.Add(classifier);
			}
			return new TextClassifierAggregator(classificationTypeRegistryService, list);
		}
	}
}
