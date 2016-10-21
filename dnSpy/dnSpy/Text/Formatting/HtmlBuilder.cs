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
using System.Text;
using System.Threading;
using System.Windows.Media;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Formatting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Formatting {
	sealed class HtmlBuilder {
		readonly IClassificationFormatMap classificationFormatMap;
		readonly string delimiter;
		readonly HtmlClipboardFormatWriter htmlWriter;
		readonly StringBuilder cssWriter;
		int spansCount;

		public HtmlBuilder(IClassificationFormatMap classificationFormatMap, string delimiter, int tabSize) {
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (delimiter == null)
				throw new ArgumentNullException(nameof(delimiter));
			if (tabSize < 1)
				throw new ArgumentOutOfRangeException(nameof(tabSize));
			this.classificationFormatMap = classificationFormatMap;
			this.delimiter = delimiter;
			this.htmlWriter = new HtmlClipboardFormatWriter() { TabSize = tabSize };
			this.cssWriter = new StringBuilder();
		}

		public void Add(ISynchronousClassifier classifier, NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken) {
			if (classifier == null)
				throw new ArgumentNullException(nameof(classifier));
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			foreach (var span in spans) {
				if (spansCount > 0)
					htmlWriter.WriteRaw(delimiter);
				spansCount++;
				var tagSpans = classifier.GetClassificationSpans(span, cancellationToken);
				var text = span.GetText();
				int pos = span.Start.Position;
				foreach (var tagSpan in tagSpans) {
					if (pos < tagSpan.Span.Start) {
						WriteCss(classificationFormatMap.DefaultTextProperties);
						htmlWriter.WriteSpan(cssWriter.ToString(), text, pos - span.Start.Position, tagSpan.Span.Start.Position - pos);
					}
					WriteCss(classificationFormatMap.GetTextProperties(tagSpan.ClassificationType));
					htmlWriter.WriteSpan(cssWriter.ToString(), text, tagSpan.Span.Start - span.Start.Position, tagSpan.Span.Length);
					pos = tagSpan.Span.End;
				}
				if (pos < span.End) {
					WriteCss(classificationFormatMap.DefaultTextProperties);
					htmlWriter.WriteSpan(cssWriter.ToString(), text, pos - span.Start.Position, span.End.Position - pos);
				}
			}
		}

		void WriteCss(TextFormattingRunProperties props) {
			cssWriter.Clear();

			if (!props.ForegroundBrushEmpty)
				WriteCssColor("color", props.ForegroundBrush);

			if (!props.BoldEmpty && props.Bold)
				cssWriter.Append($"font-weight: bold; ");
			if (!props.ItalicEmpty && props.Italic)
				cssWriter.Append($"font-style: italic; ");
		}

		void WriteCssColor(string name, Brush brush) {
			var scb = brush as SolidColorBrush;
			if (scb != null)
				cssWriter.Append(string.Format(name + ": rgb({0}, {1}, {2}); ", scb.Color.R, scb.Color.G, scb.Color.B));
		}

		public string Create() => htmlWriter.ToString();
	}
}
