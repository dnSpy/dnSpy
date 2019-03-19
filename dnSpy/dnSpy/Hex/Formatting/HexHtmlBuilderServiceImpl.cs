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
using System.ComponentModel.Composition;
using System.Threading;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Classification;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Settings.AppearanceCategory;
using VSTC = Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Hex.Formatting {
	[Export(typeof(HexHtmlBuilderService))]
	sealed class HexHtmlBuilderServiceImpl : HexHtmlBuilderService {
		readonly HexClassificationFormatMapService classificationFormatMapService;
		readonly HexClassifierAggregatorService classifierAggregatorService;
		readonly HexViewClassifierAggregatorService viewClassifierAggregatorService;

		[ImportingConstructor]
		HexHtmlBuilderServiceImpl(HexClassificationFormatMapService classificationFormatMapService, HexClassifierAggregatorService classifierAggregatorService, HexViewClassifierAggregatorService viewClassifierAggregatorService) {
			this.classificationFormatMapService = classificationFormatMapService;
			this.classifierAggregatorService = classifierAggregatorService;
			this.viewClassifierAggregatorService = viewClassifierAggregatorService;
		}

		public override string GenerateHtmlFragment(NormalizedHexBufferSpanCollection spans, HexBufferLineFormatter bufferLines, string delimiter, CancellationToken cancellationToken) {
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			if (bufferLines == null)
				throw new ArgumentNullException(nameof(bufferLines));
			if (delimiter == null)
				throw new ArgumentNullException(nameof(delimiter));
			if (spans.Count != 0 && spans[0].Buffer != bufferLines.Buffer)
				throw new ArgumentException();

			return GenerateHtmlFragmentCore(bufferLines, spans, null, delimiter, cancellationToken);
		}

		public override string GenerateHtmlFragment(NormalizedHexBufferSpanCollection spans, HexView hexView, string delimiter, CancellationToken cancellationToken) {
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			if (delimiter == null)
				throw new ArgumentNullException(nameof(delimiter));

			return GenerateHtmlFragmentCore(hexView.BufferLines, spans, hexView, delimiter, cancellationToken);
		}

		string GenerateHtmlFragmentCore(HexBufferLineFormatter bufferLines, NormalizedHexBufferSpanCollection spans, HexView hexView, string delimiter, CancellationToken cancellationToken) {
			HexClassifier classifier = null;
			try {
				VSTC.IClassificationFormatMap classificationFormatMap;
				if (hexView != null) {
					classifier = viewClassifierAggregatorService.GetClassifier(hexView);
					classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(hexView);
				}
				else {
					classifier = spans.Count == 0 ? null : classifierAggregatorService.GetClassifier(spans[0].Buffer);
					classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.HexEditor);
				}

				const int tabSize = 4;
				var builder = new HexHtmlBuilder(classificationFormatMap, delimiter, tabSize);
				if (spans.Count != 0)
					builder.Add(bufferLines, classifier, spans, cancellationToken);
				return builder.Create();
			}
			finally {
				classifier.Dispose();
			}
		}
	}
}
