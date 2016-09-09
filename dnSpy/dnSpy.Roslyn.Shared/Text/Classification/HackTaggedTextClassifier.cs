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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	sealed class HackTaggedTextClassifier : ITextClassifier {
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		public HackTaggedTextClassifier(IThemeClassificationTypeService themeClassificationTypeService) {
			if (themeClassificationTypeService == null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			this.themeClassificationTypeService = themeClassificationTypeService;
		}

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
			var tagContext = context as TaggedTextClassifierContext;
			Debug.Assert(tagContext != null);
			if (tagContext == null)
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
					if (partText.IndexOf(s, 0, endOfLineIndex, StringComparison.Ordinal) == endOfLineIndex - s.Length) {
						yield return new TextClassificationTag(new Span(0, endOfLineIndex - s.Length), themeClassificationTypeService.GetClassificationType(TextColor.Keyword));
						break;
					}
				}
			}
		}
	}
}
