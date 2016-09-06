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
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Language.Intellisense.Classification;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Roslyn.Shared.Intellisense.Completions.Classification {
	[Export(typeof(ICompletionClassifierProvider))]
	[ContentType(ContentTypes.RoslynCode)]
	sealed class CompletionClassifierProvider : ICompletionClassifierProvider {
		readonly IThemeClassificationTypes themeClassificationTypes;

		[ImportingConstructor]
		CompletionClassifierProvider(IThemeClassificationTypes themeClassificationTypes) {
			this.themeClassificationTypes = themeClassificationTypes;
		}

		public ICompletionClassifier Create(CompletionCollection collection) => new CompletionClassifier(themeClassificationTypes);
	}

	sealed class CompletionClassifier : ICompletionClassifier {
		readonly IThemeClassificationTypes themeClassificationTypes;
		readonly IClassificationType punctuationClassificationType;

		public CompletionClassifier(IThemeClassificationTypes themeClassificationTypes) {
			if (themeClassificationTypes == null)
				throw new ArgumentNullException(nameof(themeClassificationTypes));
			this.themeClassificationTypes = themeClassificationTypes;
			this.punctuationClassificationType = themeClassificationTypes.GetClassificationType(TextColor.Punctuation);
		}

		const string VBOf = "Of …";

		public IEnumerable<CompletionClassificationTag> GetTags(CompletionClassifierContext context) {
			var completion = context.Completion as RoslynCompletion;
			if (completion == null)
				yield break;
			var color = completion.CompletionItem.Tags.ToCompletionKind().ToTextColor();
			if (color != TextColor.Text) {
				var text = context.DisplayText;
				bool seenSpecial = false;
				for (int textOffset = 0; textOffset < text.Length;) {
					int specialIndex = text.IndexOfAny(punctuationChars, textOffset);
					int len = specialIndex < 0 ? text.Length - textOffset : specialIndex - textOffset;
					if (len > 0) {
						bool wasSpecialCaseString = false;
						if (seenSpecial) {
							var s = text.Substring(textOffset, len);
							if (s == VBOf) {
								yield return new CompletionClassificationTag(new Span(textOffset, 2), themeClassificationTypes.GetClassificationType(TextColor.Keyword));
								wasSpecialCaseString = true;
							}
						}
						if (!wasSpecialCaseString)
							yield return new CompletionClassificationTag(new Span(textOffset, len), themeClassificationTypes.GetClassificationType(color));
						textOffset += len;
					}

					if (specialIndex >= 0) {
						seenSpecial = true;
						yield return new CompletionClassificationTag(new Span(textOffset, 1), punctuationClassificationType);
						textOffset++;
					}
				}
			}
		}
		static readonly char[] punctuationChars = new char[] {
			'<', '>',
			'(', ')',
		};
	}
}
