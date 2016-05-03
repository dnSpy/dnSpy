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
using System.Linq;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.Themes;
using dnSpy.Shared.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnSpy.TextEditor {
	sealed class DnSpyTextEditorHighlighter : IHighlighter {
		readonly DnSpyTextEditor textEditor;
		readonly TextDocument document;

		public DnSpyTextEditorHighlighter(DnSpyTextEditor textEditor, TextDocument document) {
			this.textEditor = textEditor;
			this.document = document;
		}

		public IDocument Document => document;

		struct ColorInfo {
			public readonly Span Span;
			public readonly ITextColor Foreground;
			public readonly ITextColor Background;
			public readonly double Priority;
			public ITextColor TextColor {
				get {
					if (Foreground == Background)
						return Foreground ?? Contracts.Themes.TextColor.Null;
					return new TextColor(Foreground?.Foreground, Background?.Background, Foreground?.FontWeight, Foreground?.FontStyle);
				}
			}
			public ColorInfo(Span span, ITextColor color, double priority) {
				Span = span;
				Foreground = color;
				Background = color;
				Priority = priority;
			}
			public ColorInfo(Span span, ITextColor fg, ITextColor bg, double priority) {
				Span = span;
				Foreground = fg;
				Background = bg;
				Priority = priority;
			}
		}

		public HighlightedLine HighlightLine(int lineNumber) {
			var line = document.GetLineByNumber(lineNumber);
			int lineStartOffs = line.Offset;
			int lineEndOffs = line.EndOffset;
			var hl = new HighlightedLine(document, line);
			if (lineStartOffs >= lineEndOffs)
				return hl;

			var span = Span.FromBounds(lineStartOffs, lineEndOffs);
			var theme = textEditor.ThemeManager.Theme;
			var allInfos = new List<ColorInfo>();
			var snapshot = textEditor.TextBuffer.CurrentSnapshot;
			foreach (var colorizer in textEditor.TextBuffer.Colorizers) {
				foreach (var cspan in colorizer.GetColorSpans(snapshot, span)) {
					var colorSpan = cspan.Span.Intersection(span);
					if (colorSpan == null || colorSpan.Value.IsEmpty)
						continue;
					var color = cspan.Color.ToTextColor(theme);
					if (color.Foreground == null && color.Background == null)
						continue;
					allInfos.Add(new ColorInfo(colorSpan.Value, color, cspan.Priority));
				}
			}

			allInfos.Sort((a, b) => a.Span.Start - b.Span.Start);

			List<ColorInfo> list;
			// Check if it's the common case
			if (!HasOverlaps(allInfos))
				list = allInfos;
			else {
				Debug.Assert(allInfos.Count != 0);

				list = new List<ColorInfo>(allInfos.Count);
				var stack = new List<ColorInfo>();
				int currOffs = 0;
				for (int i = 0; i < allInfos.Count;) {
					if (stack.Count == 0)
						currOffs = allInfos[i].Span.Start;
					for (; i < allInfos.Count; i++) {
						var curr = allInfos[i];
						if (curr.Span.Start != currOffs)
							break;
						stack.Add(curr);
					}
					Debug.Assert(stack.Count != 0);
					Debug.Assert(stack.All(a => a.Span.Start == currOffs));
					stack.Sort((a, b) => b.Priority.CompareTo(a.Priority));
					int end = stack.Min(a => a.Span.End);
					end = Math.Min(end, i < allInfos.Count ? allInfos[i].Span.Start : lineEndOffs);
					var fgColor = stack.FirstOrDefault(a => a.Foreground?.Foreground != null);
					var bgColor = stack.FirstOrDefault(a => a.Background?.Background != null);
					var newInfo = new ColorInfo(Span.FromBounds(currOffs, end), fgColor.Foreground, bgColor.Background, 0);
					Debug.Assert(list.Count == 0 || list[list.Count - 1].Span.End <= newInfo.Span.Start);
					list.Add(newInfo);
					for (int j = stack.Count - 1; j >= 0; j--) {
						var info = stack[j];
						if (newInfo.Span.End >= info.Span.End)
							stack.RemoveAt(j);
						else
							stack[j] = new ColorInfo(Span.FromBounds(newInfo.Span.End, info.Span.End), info.Foreground, info.Background, info.Priority);
					}
					currOffs = newInfo.Span.End;
				}
			}
			Debug.Assert(!HasOverlaps(list));

			foreach (var info in list) {
				hl.Sections.Add(new HighlightedSection {
					Offset = info.Span.Start,
					Length = info.Span.Length,
					Color = info.TextColor.ToHighlightingColor(),
				});
			}

			return hl;
		}

		bool HasOverlaps(List<ColorInfo> sortedList) {
			for (int i = 1; i < sortedList.Count; i++) {
				if (sortedList[i - 1].Span.End > sortedList[i].Span.Start)
					return true;
			}
			return false;
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
