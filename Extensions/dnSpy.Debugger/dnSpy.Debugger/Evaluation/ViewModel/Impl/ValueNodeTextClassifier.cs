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
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Debugger.Evaluation.ViewModel.Impl {
	[Export(typeof(ITextClassifierProvider))]
	[ContentType(ContentTypes.VariablesWindow)]
	sealed class ValueNodeTextClassifierProvider : ITextClassifierProvider {
		readonly IClassificationType debuggerValueChangedHighlightClassificationType;

		[ImportingConstructor]
		ValueNodeTextClassifierProvider(IThemeClassificationTypeService themeClassificationTypeService) =>
			debuggerValueChangedHighlightClassificationType = themeClassificationTypeService.GetClassificationType(TextColor.DebuggerValueChangedHighlight);

		public ITextClassifier? Create(IContentType contentType) => new ValueNodeTextClassifier(debuggerValueChangedHighlightClassificationType);
	}

	sealed class ValueNodeTextClassifier : ITextClassifier {
		readonly IClassificationType debuggerValueChangedHighlightClassificationType;

		public ValueNodeTextClassifier(IClassificationType debuggerValueChangedHighlightClassificationType) =>
			this.debuggerValueChangedHighlightClassificationType = debuggerValueChangedHighlightClassificationType ?? throw new ArgumentNullException(nameof(debuggerValueChangedHighlightClassificationType));

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			var classifierContext = context as ValueNodeTextClassifierContext;
			if (classifierContext is null)
				yield break;
			if (classifierContext.TextChanged)
				yield return new TextClassificationTag(classifierContext.TextChangedSpan, debuggerValueChangedHighlightClassificationType);
		}
	}
}
