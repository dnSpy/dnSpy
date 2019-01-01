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
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Controls;
using dnSpy.Contracts.Text.Classification;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Text.Classification {
	sealed class TaggedTextElementProvider : ITaggedTextElementProvider {
		readonly ITextClassifierAggregator classifierAggregator;
		readonly IClassificationFormatMap classificationFormatMap;

		public TaggedTextElementProvider(IContentType contentType, ITextClassifierAggregatorService textClassifierAggregatorService, IClassificationFormatMap classificationFormatMap) {
			if (contentType == null)
				throw new ArgumentNullException(nameof(contentType));
			if (textClassifierAggregatorService == null)
				throw new ArgumentNullException(nameof(textClassifierAggregatorService));
			classifierAggregator = textClassifierAggregatorService.Create(contentType);
			this.classificationFormatMap = classificationFormatMap ?? throw new ArgumentNullException(nameof(classificationFormatMap));
		}

		public TextBlock Create(string tag, ImmutableArray<TaggedText> taggedParts, bool colorize) {
			var context = TaggedTextClassifierContext.Create(tag, taggedParts, colorize);
			return TextBlockFactory.Create(context.Text, classificationFormatMap.DefaultTextProperties,
				classifierAggregator.GetTags(context).Select(a => new TextRunPropertiesAndSpan(a.Span, classificationFormatMap.GetTextProperties(a.ClassificationType))), TextBlockFactory.Flags.DisableSetTextBlockFontFamily | TextBlockFactory.Flags.DisableFontSize);
		}

		public void Dispose() => classifierAggregator?.Dispose();
	}
}
