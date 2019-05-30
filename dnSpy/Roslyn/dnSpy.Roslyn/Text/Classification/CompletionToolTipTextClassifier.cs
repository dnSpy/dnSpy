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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Text.Classification {
	[Export(typeof(ITextClassifierProvider))]
	[ContentType(RoslynContentTypes.CompletionToolTipRoslyn)]
	sealed class CompletionToolTipTextClassifierProvider : ITextClassifierProvider {
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		[ImportingConstructor]
		CompletionToolTipTextClassifierProvider(IThemeClassificationTypeService themeClassificationTypeService) => this.themeClassificationTypeService = themeClassificationTypeService;

		public ITextClassifier? Create(IContentType contentType) =>
			new CompletionToolTipTextClassifier(themeClassificationTypeService);
	}

	sealed class CompletionToolTipTextClassifier : ITextClassifier {
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		public CompletionToolTipTextClassifier(IThemeClassificationTypeService themeClassificationTypeService) => this.themeClassificationTypeService = themeClassificationTypeService ?? throw new ArgumentNullException(nameof(themeClassificationTypeService));

		static string[] keywordSuffixes = new string[] {
			// C# / Visual Basic
			" Keyword",
			// Visual Basic
			" function",
			// Visual Basic: If() expression
			" function (+1 overload)",
			// Visual Basic
			" statement",
		};

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			if (!context.Colorize)
				yield break;
			var tagContext = context as TaggedTextClassifierContext;
			if (tagContext is null)
				yield break;
			if (tagContext.TaggedParts.Length == 0)
				yield break;
			var part = tagContext.TaggedParts[0];
			if (part.Tag == TextTags.Text) {
				var partText = part.Text;
				// Eg. "AddHandler statement\r\n[...]" contains CRLF
				int endOfLineIndex = partText.IndexOf("\r\n");
				if (endOfLineIndex < 0)
					endOfLineIndex = partText.Length;
				foreach (var s in keywordSuffixes) {
					int endOfKeywordPart = endOfLineIndex - s.Length;
					if (partText.IndexOf(s, 0, endOfLineIndex, StringComparison.Ordinal) == endOfKeywordPart) {
						var keywords = part.Text.Substring(0, endOfKeywordPart);
						int keywordOffset = 0;
						while (keywordOffset < keywords.Length) {
							if (keywords[keywordOffset] == ' ') {
								keywordOffset++;
								continue;
							}
							int end = keywords.IndexOf(' ', keywordOffset);
							if (end < 0)
								end = keywords.Length;
							int keywordLen = end - keywordOffset;
							if (keywordLen > 0)
								yield return new TextClassificationTag(new Span(keywordOffset, keywordLen), themeClassificationTypeService.GetClassificationType(TextColor.Keyword));
							keywordOffset += keywordLen;
						}
						break;
					}
				}
			}
		}
	}
}
