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
using dnSpy.Contracts.Text;
using dnSpy.Shared.AvalonEdit;
using dnSpy.Shared.Text;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnSpy.Text {
	sealed class DnSpyTextEditorHighlighter : IHighlighter {
		readonly DnSpyTextEditor textEditor;
		readonly TextDocument document;

		public DnSpyTextEditorHighlighter(DnSpyTextEditor textEditor, TextDocument document) {
			this.textEditor = textEditor;
			this.document = document;
		}

		public IDocument Document => document;
		readonly ColorAggregator colorAggregator = new ColorAggregator();

		public HighlightedLine HighlightLine(int lineNumber) {
			var line = document.GetLineByNumber(lineNumber);
			int lineStartOffs = line.Offset;
			int lineEndOffs = line.EndOffset;
			var hl = new HighlightedLine(document, line);
			if (lineStartOffs >= lineEndOffs)
				return hl;

			var span = Span.FromBounds(lineStartOffs, lineEndOffs);
			colorAggregator.Initialize(textEditor.ThemeManager.Theme, span);
			var snapshotSpan = new SnapshotSpan(textEditor.TextBuffer.CurrentSnapshot, span);
			foreach (var colorizer in textEditor.GetAllColorizers())
				colorAggregator.Add(colorizer.GetColorSpans(snapshotSpan));

			foreach (var info in colorAggregator.Finish()) {
				hl.Sections.Add(new HighlightedSection {
					Offset = info.Span.Start,
					Length = info.Span.Length,
					Color = info.TextColor.ToHighlightingColor(),
				});
			}

			colorAggregator.CleanUp();

			return hl;
		}

		public event HighlightingStateChangedEventHandler HighlightingStateChanged {
			add { }
			remove { }
		}

		public IEnumerable<HighlightingColor> GetColorStack(int lineNumber) => Array.Empty<HighlightingColor>();
		public void UpdateHighlightingState(int lineNumber) { }
		public void BeginHighlighting() { }
		public void EndHighlighting() { }
		public HighlightingColor GetNamedColor(string name) => null;
		public HighlightingColor DefaultTextColor => null;
		public void Dispose() { }
	}
}
