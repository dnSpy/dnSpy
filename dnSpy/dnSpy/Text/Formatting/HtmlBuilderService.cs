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
using System.ComponentModel.Composition;
using System.Threading;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Formatting;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Text.Formatting {
	[Export(typeof(IHtmlBuilderService))]
	sealed class HtmlBuilderService : IHtmlBuilderService {
		const string defaultDelimiter = "<br/>";
		const int defaultTabSize = 4;

		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly ISynchronousClassifierAggregatorService synchronousClassifierAggregatorService;
		readonly ISynchronousViewClassifierAggregatorService synchronousViewClassifierAggregatorService;

		[ImportingConstructor]
		HtmlBuilderService(IClassificationFormatMapService classificationFormatMapService, ISynchronousClassifierAggregatorService synchronousClassifierAggregatorService, ISynchronousViewClassifierAggregatorService synchronousViewClassifierAggregatorService) {
			this.classificationFormatMapService = classificationFormatMapService;
			this.synchronousClassifierAggregatorService = synchronousClassifierAggregatorService;
			this.synchronousViewClassifierAggregatorService = synchronousViewClassifierAggregatorService;
		}

		public string GenerateHtmlFragment(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken) =>
			GenerateHtmlFragment(spans, defaultDelimiter, cancellationToken);
		public string GenerateHtmlFragment(NormalizedSnapshotSpanCollection spans, string delimiter, CancellationToken cancellationToken) {
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			if (delimiter == null)
				throw new ArgumentNullException(nameof(delimiter));

			return GenerateHtmlFragmentCore(spans, null, delimiter, cancellationToken);
		}

		public string GenerateHtmlFragment(NormalizedSnapshotSpanCollection spans, ITextView textView, CancellationToken cancellationToken) =>
			GenerateHtmlFragment(spans, textView, defaultDelimiter, cancellationToken);
		public string GenerateHtmlFragment(NormalizedSnapshotSpanCollection spans, ITextView textView, string delimiter, CancellationToken cancellationToken) {
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (delimiter == null)
				throw new ArgumentNullException(nameof(delimiter));

			return GenerateHtmlFragmentCore(spans, textView, delimiter, cancellationToken);
		}

		string GenerateHtmlFragmentCore(NormalizedSnapshotSpanCollection spans, ITextView textView, string delimiter, CancellationToken cancellationToken) {
			ISynchronousClassifier classifier = null;
			try {
				int tabSize;
				IClassificationFormatMap classificationFormatMap;
				if (textView != null) {
					classifier = synchronousViewClassifierAggregatorService.GetSynchronousClassifier(textView);
					classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(textView);
					tabSize = textView.Options.GetTabSize();
				}
				else {
					classifier = spans.Count == 0 ? null : synchronousClassifierAggregatorService.GetSynchronousClassifier(spans[0].Snapshot.TextBuffer);
					classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.TextEditor);
					tabSize = defaultTabSize;
				}
				tabSize = OptionsHelpers.FilterTabSize(tabSize);

				var builder = new HtmlBuilder(classificationFormatMap, delimiter, tabSize);
				if (spans.Count != 0)
					builder.Add(classifier, spans, cancellationToken);
				return builder.Create();
			}
			finally {
				(classifier as IDisposable)?.Dispose();
			}
		}
	}
}
