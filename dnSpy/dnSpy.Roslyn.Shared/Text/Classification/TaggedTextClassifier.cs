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
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Shared.Text.Classification {
	sealed class TaggedTextClassifier : ITextClassifier {
		readonly IThemeClassificationTypeService themeClassificationTypeService;

		public TaggedTextClassifier(IThemeClassificationTypeService themeClassificationTypeService) {
			if (themeClassificationTypeService == null)
				throw new ArgumentNullException(nameof(themeClassificationTypeService));
			this.themeClassificationTypeService = themeClassificationTypeService;
		}

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			var tagContext = context as TaggedTextClassifierContext;
			Debug.Assert(tagContext != null);
			if (tagContext == null)
				yield break;
			int pos = 0;
			foreach (var part in tagContext.TaggedParts) {
				var color = TextTagsHelper.ToTextColor(part.Tag);
				int len = part.Text.Length;
				yield return new TextClassificationTag(new Span(pos, len), themeClassificationTypeService.GetClassificationType(color));
				pos += len;
			}
			Debug.Assert(pos == context.Text.Length);
		}
	}
}
