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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Language.Intellisense.Classification;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionTextElementProvider : ICompletionTextElementProvider {
		readonly ITextClassifierAggregatorService textClassifierAggregatorService;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly Dictionary<IContentType, ITextClassifier> toClassifier;

		public CompletionTextElementProvider(ITextClassifierAggregatorService textClassifierAggregatorService, IClassificationFormatMap classificationFormatMap, IContentTypeRegistryService contentTypeRegistryService) {
			if (textClassifierAggregatorService == null)
				throw new ArgumentNullException(nameof(textClassifierAggregatorService));
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (contentTypeRegistryService == null)
				throw new ArgumentNullException(nameof(contentTypeRegistryService));
			this.textClassifierAggregatorService = textClassifierAggregatorService;
			this.classificationFormatMap = classificationFormatMap;
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.toClassifier = new Dictionary<IContentType, ITextClassifier>();
		}

		ITextClassifier GetTextClassifier(IContentType contentType) {
			ITextClassifier completionClassifier;
			if (!toClassifier.TryGetValue(contentType, out completionClassifier))
				toClassifier.Add(contentType, completionClassifier = textClassifierAggregatorService.Create(contentType));
			return completionClassifier;
		}

		public FrameworkElement Create(CompletionSet completionSet, Completion completion, CompletionClassifierKind kind, bool colorize) {
			if (completionSet == null)
				throw new ArgumentNullException(nameof(completionSet));
			if (completion == null)
				throw new ArgumentNullException(nameof(completion));
			Debug.Assert(completionSet.Completions.Contains(completion));

			CompletionClassifierContext context;
			string defaultContentType;
			switch (kind) {
			case CompletionClassifierKind.DisplayText:
				var inputText = completionSet.ApplicableTo.GetText(completionSet.ApplicableTo.TextBuffer.CurrentSnapshot);
				context = new CompletionDisplayTextClassifierContext(completionSet, completion, completion.DisplayText, inputText, colorize);
				defaultContentType = ContentTypes.CompletionDisplayText;
				break;

			case CompletionClassifierKind.Suffix:
				var suffix = (completion as Completion4)?.Suffix ?? string.Empty;
				context = new CompletionSuffixClassifierContext(completionSet, completion, suffix, colorize);
				defaultContentType = ContentTypes.CompletionSuffix;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(kind));
			}

			var contentType = (completionSet as ICompletionSetContentTypeProvider)?.GetContentType(contentTypeRegistryService, kind);
			if (contentType == null)
				contentType = contentTypeRegistryService.GetContentType(defaultContentType);
			var classifier = GetTextClassifier(contentType);
			return TextBlockFactory.Create(context.Text, classificationFormatMap.DefaultTextProperties,
				classifier.GetTags(context).Select(a => new TextRunPropertiesAndSpan(a.Span, classificationFormatMap.GetTextProperties(a.ClassificationType))), TextBlockFactory.Flags.DisableSetTextBlockFontFamily | TextBlockFactory.Flags.DisableFontSize);
		}

		public void Dispose() {
			foreach (var classifier in toClassifier.Values)
				(classifier as IDisposable)?.Dispose();
			toClassifier.Clear();
		}
	}
}
