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
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Controls;
using dnSpy.Contracts.Text.Classification;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	sealed class TaggedTextElementProvider : ITaggedTextElementProvider {
		readonly ITextClassifier classifier;
		readonly IClassificationFormatMap classificationFormatMap;

		public TaggedTextElementProvider(ITextClassifierAggregatorService textClassifierAggregatorService, IClassificationFormatMap classificationFormatMap, ITextClassifier[] classifiers) {
			if (textClassifierAggregatorService == null)
				throw new ArgumentNullException(nameof(textClassifierAggregatorService));
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (classifiers == null)
				throw new ArgumentNullException(nameof(classifiers));
			this.classifier = textClassifierAggregatorService.Create(classifiers);
			this.classificationFormatMap = classificationFormatMap;
		}

		public TextBlock Create(ImmutableArray<TaggedText> taggedParts) {
			var context = TaggedTextClassifierContext.Create(taggedParts);
			return TextBlockFactory.Create(context.Text, classificationFormatMap.DefaultTextProperties,
				classifier.GetTags(context).Select(a => new TextRunPropertiesAndSpan(a.Span, classificationFormatMap.GetTextProperties(a.ClassificationType))), TextBlockFactory.Flags.DisableSetTextBlockFontFamily | TextBlockFactory.Flags.DisableFontSize);
		}

		public void Dispose() => (classifier as IDisposable)?.Dispose();
	}
}
