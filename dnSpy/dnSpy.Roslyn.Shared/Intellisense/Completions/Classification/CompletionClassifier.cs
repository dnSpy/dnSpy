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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using dnSpy.Contracts.Language.Intellisense.Classification;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Roslyn.Shared.Text;
using dnSpy.Roslyn.Shared.Text.Classification;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Shared.Intellisense.Completions.Classification {
	[Export(typeof(ITextClassifierProvider))]
	[ContentType(RoslynContentTypes.CompletionDisplayTextRoslyn)]
	sealed class CompletionClassifierProvider : ITextClassifierProvider {
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		[ImportingConstructor]
		CompletionClassifierProvider(IThemeClassificationTypeService themeClassificationTypeService) {
			this.themeClassificationTypeService = themeClassificationTypeService;
		}

		public ITextClassifier Create(IContentType contentType) => new CompletionClassifier(themeClassificationTypeService);
	}

	sealed class CompletionClassifier : ITextClassifier {
		readonly IThemeClassificationTypeService themeClassificationTypeService;
		readonly IClassificationType punctuationClassificationType;
		StringBuilder stringBuilder;

		public CompletionClassifier(IThemeClassificationTypeService themeClassificationTypeService) {
			if (themeClassificationTypeService == null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			this.themeClassificationTypeService = themeClassificationTypeService;
			punctuationClassificationType = themeClassificationTypeService.GetClassificationType(TextColor.Punctuation);
		}

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			if (!context.Colorize)
				yield break;
			var completionContext = context as CompletionDisplayTextClassifierContext;
			if (completionContext == null)
				yield break;
			var completion = completionContext.Completion as RoslynCompletion;
			if (completion == null)
				yield break;
			var completionSet = completionContext.CompletionSet as RoslynCompletionSet;
			if (completionSet == null)
				yield break;

			// The completion API doesn't create tagged text so try to extract that information
			// from the string so we get nice colorized text.

			var color = completion.CompletionItem.Tags.ToCompletionKind().ToTextColor();
			var text = context.Text;

			// Check if the namespace or enclosing class name is part of the text
			if (text.IndexOf('.') < 0) {
				// The common case is just an identifier, and in that case, the tag is correct
				int punctIndex = text.IndexOfAny(punctuationChars, 0);
				if (punctIndex < 0) {
					yield return new TextClassificationTag(new Span(0, text.Length), themeClassificationTypeService.GetClassificationType(color));
					yield break;
				}

				// Check for CLASS<> or METHOD()
				if (punctIndex + 2 == text.Length && text.IndexOfAny(punctuationChars, punctIndex + 1) == punctIndex + 1) {
					yield return new TextClassificationTag(new Span(0, punctIndex), themeClassificationTypeService.GetClassificationType(color));
					yield return new TextClassificationTag(new Span(punctIndex, 2), punctuationClassificationType);
					yield break;
				}

				// Check for Visual Basic generics special case
				const string VBOf = "(Of …)";
				if (text.Length - VBOf.Length == punctIndex && text.EndsWith(VBOf)) {
					yield return new TextClassificationTag(new Span(0, punctIndex), themeClassificationTypeService.GetClassificationType(color));
					yield return new TextClassificationTag(new Span(punctIndex, 1), punctuationClassificationType);
					yield return new TextClassificationTag(new Span(punctIndex + 1, 2), themeClassificationTypeService.GetClassificationType(TextColor.Keyword));
					yield return new TextClassificationTag(new Span(punctIndex + VBOf.Length - 1, 1), punctuationClassificationType);
					yield break;
				}
			}

			// The text is usually identical to the description and it's classified
			var description = completionSet.GetDescriptionAsync(completion).GetAwaiter().GetResult();
			var indexes = GetMatchIndexes(completion, description);
			if (indexes != null) {
				int pos = 0;
				var parts = description.TaggedParts;
				int endIndex = indexes.Value.Value;
				for (int i = indexes.Value.Key; i <= endIndex; i++) {
					var part = parts[i];
					if (part.Tag == TextTags.LineBreak)
						break;
					var color2 = TextTagsHelper.ToTextColor(part.Tag);
					yield return new TextClassificationTag(new Span(pos, part.Text.Length), themeClassificationTypeService.GetClassificationType(color2));
					pos += part.Text.Length;
				}
				if (pos < text.Length) {
					// The remaining text is unknown, just use the tag color
					yield return new TextClassificationTag(Span.FromBounds(pos, text.Length), themeClassificationTypeService.GetClassificationType(color));
				}
				yield break;
			}

			// Give up, use the same color for all the text
			yield return new TextClassificationTag(new Span(0, text.Length), themeClassificationTypeService.GetClassificationType(color));
		}
		static readonly char[] punctuationChars = new char[] {
			'<', '>',
			'(', ')',
		};

		KeyValuePair<int, int>? GetMatchIndexes(RoslynCompletion completion, CompletionDescription description) {
			if (completion == null || description == null)
				return null;
			if (stringBuilder == null)
				stringBuilder = new StringBuilder();
			else
				stringBuilder.Clear();
			var displayText = completion.DisplayText;
			int matchIndex = -1;
			int index = -1;
			foreach (var part in description.TaggedParts) {
				index++;
				if (part.Tag == TextTags.LineBreak)
					break;
				if (matchIndex < 0) {
					if (!displayText.StartsWith(part.Text))
						continue;
					matchIndex = index;
				}
				else {
					if (!StartsWith(displayText, stringBuilder.Length, part.Text)) {
						// Partial match, could happen if the type is System.Collections.Generic.List<int> but
						// the documentation is using System.Collections.Generic.List<T>.
						return new KeyValuePair<int, int>(matchIndex, index - 1);
					}
				}
				stringBuilder.Append(part.Text);
				if (stringBuilder.Length == displayText.Length) {
					if (stringBuilder.ToString() == completion.DisplayText)
						return new KeyValuePair<int, int>(matchIndex, index);
					break;
				}
				else if (stringBuilder.Length > displayText.Length)
					break;
			}
			return null;
		}

		bool StartsWith(string displayText, int index, string text) {
			if (index + text.Length > displayText.Length)
				return false;
			for (int i = 0; i < text.Length; i++) {
				if (displayText[index + i] != text[i])
					return false;
			}
			return true;
		}
	}
}
