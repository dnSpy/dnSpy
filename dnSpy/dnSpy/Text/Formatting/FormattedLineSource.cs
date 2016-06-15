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
using System.Collections.ObjectModel;
using System.Windows.Media.TextFormatting;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Formatting {
	sealed class FormattedLineSource : IFormattedLineSource {
		public ITextSnapshot SourceTextSnapshot { get; }
		public ITextSnapshot TopTextSnapshot { get; }
		public bool UseDisplayMode { get; }
		public int TabSize { get; }
		public double BaseIndentation { get; }
		public double WordWrapWidth { get; }
		public double MaxAutoIndent { get; }
		public double ColumnWidth { get; }
		public double LineHeight { get { throw new NotImplementedException();/*TODO:*/ } }
		public double TextHeightAboveBaseline { get { throw new NotImplementedException();/*TODO:*/ } }
		public double TextHeightBelowBaseline { get { throw new NotImplementedException();/*TODO:*/ } }
		public ITextAndAdornmentSequencer TextAndAdornmentSequencer { get; }

		public TextRunProperties DefaultTextProperties {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		readonly IClassifier aggregateClassifier;
		readonly ITextAndAdornmentSequencer textAndAdornmentSequencer;
		readonly IClassificationFormatMap classificationFormatMap;

		public FormattedLineSource(ITextSnapshot sourceTextSnapshot, ITextSnapshot visualBufferSnapshot, int tabSize, double baseIndent, double wordWrapWidth, double maxAutoIndent, bool useDisplayMode, IClassifier aggregateClassifier, ITextAndAdornmentSequencer sequencer, IClassificationFormatMap classificationFormatMap, bool isViewWrapEnabled) {
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
			if (sourceTextSnapshot != visualBufferSnapshot)
				throw new NotSupportedException("Text snapshot must be identical to visual snapshot");

			SourceTextSnapshot = sourceTextSnapshot;
			TopTextSnapshot = visualBufferSnapshot;
			UseDisplayMode = useDisplayMode;
			TabSize = tabSize;
			BaseIndentation = baseIndent;
			WordWrapWidth = wordWrapWidth;
			MaxAutoIndent = Math.Round(maxAutoIndent);
			double columnWidth = 10;//TODO:
			ColumnWidth = columnWidth;
			TextAndAdornmentSequencer = textAndAdornmentSequencer;
			this.aggregateClassifier = aggregateClassifier;
			this.textAndAdornmentSequencer = sequencer;
			this.classificationFormatMap = classificationFormatMap;
		}

		public Collection<IFormattedLine> FormatLineInVisualBuffer(ITextSnapshotLine visualLine) {
			if (visualLine == null)
				throw new ArgumentNullException(nameof(visualLine));
			if (visualLine.Snapshot != TopTextSnapshot)
				throw new ArgumentException();
			throw new NotImplementedException();//TODO:
		}
	}
}
