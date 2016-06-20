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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Formatting;
using dnSpy.Text.Classification;

namespace dnSpy.Text.Formatting {
	[Export(typeof(IFormattedTextSourceFactoryService))]
	sealed class FormattedTextSourceFactoryService : IFormattedTextSourceFactoryService {
		readonly ITextParagraphPropertiesFactoryServiceSelector textParagraphPropertiesFactoryServiceSelector;

		[ImportingConstructor]
		FormattedTextSourceFactoryService(ITextParagraphPropertiesFactoryServiceSelector textParagraphPropertiesFactoryServiceSelector) {
			this.textParagraphPropertiesFactoryServiceSelector = textParagraphPropertiesFactoryServiceSelector;
		}

		public IFormattedLineSource Create(ITextSnapshot sourceTextSnapshot, ITextSnapshot visualBufferSnapshot, int tabSize, double baseIndent, double wordWrapWidth, double maxAutoIndent, bool useDisplayMode, ITextAndAdornmentSequencer sequencer, IClassificationFormatMap classificationFormatMap) =>
			Create(sourceTextSnapshot, visualBufferSnapshot, tabSize, baseIndent, wordWrapWidth, maxAutoIndent, useDisplayMode, NullClassifier.Instance, sequencer, classificationFormatMap, false);

		public IFormattedLineSource Create(ITextSnapshot sourceTextSnapshot, ITextSnapshot visualBufferSnapshot, int tabSize, double baseIndent, double wordWrapWidth, double maxAutoIndent, bool useDisplayMode, IClassifier aggregateClassifier, ITextAndAdornmentSequencer sequencer, IClassificationFormatMap classificationFormatMap) =>
			Create(sourceTextSnapshot, visualBufferSnapshot, tabSize, baseIndent, wordWrapWidth, maxAutoIndent, useDisplayMode, aggregateClassifier, sequencer, classificationFormatMap, false);

		public IFormattedLineSource Create(ITextSnapshot sourceTextSnapshot, ITextSnapshot visualBufferSnapshot, int tabSize, double baseIndent, double wordWrapWidth, double maxAutoIndent, bool useDisplayMode, IClassifier aggregateClassifier, ITextAndAdornmentSequencer sequencer, IClassificationFormatMap classificationFormatMap, bool isViewWrapEnabled) {
			if (sourceTextSnapshot == null)
				throw new ArgumentNullException(nameof(sourceTextSnapshot));
			if (visualBufferSnapshot == null)
				throw new ArgumentNullException(nameof(visualBufferSnapshot));
			if (aggregateClassifier == null)
				throw new ArgumentNullException(nameof(aggregateClassifier));
			if (sequencer == null)
				throw new ArgumentNullException(nameof(sequencer));
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (tabSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(tabSize));
			var textParagraphPropertiesFactoryService = textParagraphPropertiesFactoryServiceSelector.Select(sourceTextSnapshot.TextBuffer.ContentType);
			return new FormattedLineSource(textParagraphPropertiesFactoryService, sourceTextSnapshot, visualBufferSnapshot, tabSize, baseIndent, wordWrapWidth, maxAutoIndent, useDisplayMode, aggregateClassifier, sequencer, classificationFormatMap, isViewWrapEnabled);
		}
	}
}
