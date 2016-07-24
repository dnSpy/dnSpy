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
using dnSpy.Contracts.Text;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Classification {
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(ClassificationTag))]
	[ContentType(ContentTypes.Any)]
	sealed class ClassifierTaggerProvider : ITaggerProvider {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly Lazy<IClassifierProvider, INamedContentTypeMetadata>[] classifierProviders;

		[ImportingConstructor]
		ClassifierTaggerProvider(IContentTypeRegistryService contentTypeRegistryService, [ImportMany] IEnumerable<Lazy<IClassifierProvider, INamedContentTypeMetadata>> classifierProviders) {
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.classifierProviders = classifierProviders.ToArray();
		}

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));

			var classifiers = GetClassifiers(buffer).ToArray();
			if (classifiers.Length != 0)
				return new ClassifierTagger(classifiers) as ITagger<T>;

			return null;
		}

		IEnumerable<IClassifier> GetClassifiers(ITextBuffer buffer) {
			var bufferContentType = buffer.ContentType;
			foreach (var info in classifierProviders) {
				foreach (var ct in info.Metadata.ContentTypes) {
					if (bufferContentType.IsOfType(ct)) {
						var classifier = info.Value.GetClassifier(buffer);
						if (classifier != null)
							yield return classifier;
						break;
					}
				}
			}
		}
	}
}
