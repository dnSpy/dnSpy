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
using System.Text;
using System.Threading;
using System.Windows.Media;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Classification;
using CTF = dnSpy.Contracts.Text.Formatting;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Formatting {
	sealed class HexHtmlBuilder {
		readonly VSTC.IClassificationFormatMap classificationFormatMap;
		readonly string delimiter;
		readonly CTF.HtmlClipboardFormatWriter htmlWriter;
		readonly StringBuilder cssWriter;
		int spansCount;

		public HexHtmlBuilder(VSTC.IClassificationFormatMap classificationFormatMap, string delimiter, int tabSize) {
			if (classificationFormatMap == null)
				throw new ArgumentNullException(nameof(classificationFormatMap));
			if (delimiter == null)
				throw new ArgumentNullException(nameof(delimiter));
			if (tabSize < 1)
				throw new ArgumentOutOfRangeException(nameof(tabSize));
			this.classificationFormatMap = classificationFormatMap;
			this.delimiter = delimiter;
			htmlWriter = new CTF.HtmlClipboardFormatWriter() { TabSize = tabSize };
			cssWriter = new StringBuilder();
		}

		public void Add(HexBufferLineFormatter bufferLines, HexClassifier classifier, NormalizedHexBufferSpanCollection spans, CancellationToken cancellationToken) {
			if (bufferLines == null)
				throw new ArgumentNullException(nameof(bufferLines));
			if (classifier == null)
				throw new ArgumentNullException(nameof(classifier));
			if (spans == null)
				throw new ArgumentNullException(nameof(spans));
			if (spans.Count != 0 && spans[0].Buffer != bufferLines.Buffer)
				throw new ArgumentException();

			var classificationSpans = new List<HexClassificationSpan>();
			foreach (var span in spans) {
				if (spansCount > 0)
					htmlWriter.WriteRaw(delimiter);
				spansCount++;

				var pos = span.Start;
				for (;;) {
					classificationSpans.Clear();
					var line = bufferLines.GetLineFromPosition(pos);
					var text = line.GetText(span);
					var context = new HexClassificationContext(line, line.TextSpan);
					classifier.GetClassificationSpans(classificationSpans, context, cancellationToken);

					int textPos = 0;
					foreach (var tagSpan in classificationSpans) {
						if (textPos < tagSpan.Span.Start) {
							WriteCss(classificationFormatMap.DefaultTextProperties);
							htmlWriter.WriteSpan(cssWriter.ToString(), text, textPos, tagSpan.Span.Start - textPos);
						}
						WriteCss(classificationFormatMap.GetTextProperties(tagSpan.ClassificationType));
						htmlWriter.WriteSpan(cssWriter.ToString(), text, tagSpan.Span.Start, tagSpan.Span.Length);
						textPos = tagSpan.Span.End;
					}
					if (textPos < text.Length) {
						WriteCss(classificationFormatMap.DefaultTextProperties);
						htmlWriter.WriteSpan(cssWriter.ToString(), text, textPos, text.Length - textPos);
					}
					htmlWriter.WriteRaw("<br/>");

					pos = line.BufferEnd;
					if (pos >= span.End)
						break;
				}
			}
		}

		void WriteCss(VSTF.TextFormattingRunProperties props) {
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
