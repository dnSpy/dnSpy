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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.Dialogs {
	[Export(typeof(ITextClassifierProvider))]
	[ContentType(ContentTypes.DocListDialog)]
	sealed class OpenDocumentListTextClassifierProvider : ITextClassifierProvider {
		readonly IClassificationType documentListMatchHighlightClassificationType;

		[ImportingConstructor]
		OpenDocumentListTextClassifierProvider(IThemeClassificationTypeService themeClassificationTypeService) {
			this.documentListMatchHighlightClassificationType = themeClassificationTypeService.GetClassificationType(TextColor.DocumentListMatchHighlight);
		}

		public ITextClassifier Create(IContentType contentType) => new OpenDocumentListTextClassifier(documentListMatchHighlightClassificationType);
	}

	sealed class OpenDocumentListTextClassifier : ITextClassifier {
		readonly IClassificationType documentListMatchHighlightClassificationType;

		public OpenDocumentListTextClassifier(IClassificationType documentListMatchHighlightClassificationType) {
			if (documentListMatchHighlightClassificationType == null)
				throw new ArgumentNullException(nameof(documentListMatchHighlightClassificationType));
			this.documentListMatchHighlightClassificationType = documentListMatchHighlightClassificationType;
		}

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			var listContext = context as OpenDocumentListTextClassifierContext;
			if (listContext == null)
				yield break;
			if (listContext.Tag != PredefinedTextClassifierTags.DocListDialogName)
				yield break;
			foreach (var part in listContext.SearchText.Split(seps, StringSplitOptions.RemoveEmptyEntries)) {
				int index = listContext.Text.IndexOf(part, StringComparison.CurrentCultureIgnoreCase);
				if (index >= 0)
					yield return new TextClassificationTag(new Span(index, part.Length), documentListMatchHighlightClassificationType);
			}
		}
		static readonly char[] seps = new char[] { ' ' };
	}
}
