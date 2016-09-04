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
using System.Linq;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Language.Intellisense.Classification;
using dnSpy.Text;
using dnSpy.Text.Classification;
using dnSpy.Text.MEF;

namespace dnSpy.Language.Intellisense.Classification {
	[Export(typeof(ICompletionClassifierAggregatorService))]
	sealed class CompletionClassifierAggregatorService : ICompletionClassifierAggregatorService {
		readonly Lazy<ITextClassifierAggregatorService> textClassifierAggregatorService;
		readonly Lazy<ICompletionClassifierProvider, IContentTypeMetadata>[] completionClassifierProviders;

		[ImportingConstructor]
		CompletionClassifierAggregatorService(Lazy<ITextClassifierAggregatorService> textClassifierAggregatorService, [ImportMany] IEnumerable<Lazy<ICompletionClassifierProvider, IContentTypeMetadata>> completionClassifierProviders) {
			this.textClassifierAggregatorService = textClassifierAggregatorService;
			this.completionClassifierProviders = completionClassifierProviders.ToArray();
		}

		public ICompletionClassifier Create(CompletionCollection collection) {
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));
			var contentType = collection.ApplicableTo.TextBuffer.ContentType;
			var classifiers = new List<ICompletionClassifier>();
			foreach (var lz in completionClassifierProviders) {
				if (!contentType.ContainsAny(lz.Metadata.ContentTypes))
					continue;
				var classifier = lz.Value.Create(collection);
				if (classifier != null)
					classifiers.Add(classifier);
			}
			return new CompletionClassifierAggregator(textClassifierAggregatorService.Value, classifiers.ToArray());
		}
	}
}
