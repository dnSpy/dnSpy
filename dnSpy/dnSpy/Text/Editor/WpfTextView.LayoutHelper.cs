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
using System.Windows;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Formatting;
using dnSpy.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed partial class WpfTextView {
		sealed class LayoutHelper {
			readonly List<PhysicalLine> oldLines;
			readonly IFormattedLineSource formattedLineSource;
			readonly ITextViewModel textViewModel;
			readonly ITextSnapshot visualSnapshot;
			readonly ITextSnapshot editSnapshot;
			readonly Dictionary<IFormattedLine, PhysicalLine> toPhysicalLine;
			readonly HashSet<IFormattedLine> oldVisibleLines;

			public double NewViewportTop { get; private set; }
			public List<PhysicalLine> AllVisiblePhysicalLines { get; private set; }
			public List<IWpfTextViewLine> AllVisibleLines { get; private set; }
			public List<IWpfTextViewLine> NewOrReformattedLines { get; private set; }
			public List<IWpfTextViewLine> TranslatedLines { get; private set; }

			public LayoutHelper(List<PhysicalLine> oldLines, IFormattedLineSource formattedLineSource, ITextViewModel textViewModel, ITextSnapshot visualSnapshot, ITextSnapshot editSnapshot) {
				this.oldLines = oldLines;
				this.formattedLineSource = formattedLineSource;
				this.textViewModel = textViewModel;
				this.visualSnapshot = visualSnapshot;
				this.editSnapshot = editSnapshot;
				this.toPhysicalLine = new Dictionary<IFormattedLine, PhysicalLine>();
				this.oldVisibleLines = new HashSet<IFormattedLine>();

				Debug.Assert(oldLines.All(a => a.BufferSpan.Snapshot == editSnapshot));
				foreach (var physLine in oldLines) {
					physLine.TranslateLinesTo(visualSnapshot, editSnapshot);
					foreach (var line in physLine.Lines) {
						if (line.IsVisible())
							oldVisibleLines.Add(line);
						toPhysicalLine[line] = physLine;
					}
				}
			}

			struct LineInfo {
				public IFormattedLine Line { get; }
				public double Y { get; }
				public LineInfo(IFormattedLine line, double y) {
					Line = line;
					Y = y;
				}
			}

			double GetNewViewportTop() {
				// So the code doesn't get used to a constant viewport top...
				viewportTop = -viewportTop;
				return viewportTop;
			}
			double viewportTop = 4242;

			public void LayoutLines(SnapshotPoint bufferPosition, ViewRelativePosition relativeTo, double verticalDistance, double viewportLeft, double viewportWidthOverride, double viewportHeightOverride) {
				NewViewportTop = 0;
				var infos = CreateLineInfos(bufferPosition, relativeTo, verticalDistance, viewportHeightOverride);

				// The first line of the file must always be shown at the top of the view
				if (infos[0].Y > NewViewportTop) {
					infos = CreateLineInfos(new SnapshotPoint(bufferPosition.Snapshot, 0), ViewRelativePosition.Top, 0, viewportHeightOverride);
					Debug.Assert(infos[0].Y == NewViewportTop);
				}

				var keptLines = new HashSet<PhysicalLine>();
				foreach (var info in infos)
					keptLines.Add(toPhysicalLine[info.Line]);
				foreach (var physLine in toPhysicalLine.Values) {
					if (!keptLines.Contains(physLine))
						physLine.Dispose();
				}

				var delta = GetNewViewportTop();
				NewViewportTop += delta;
				var visibleLines = new HashSet<IWpfTextViewLine>();
				var visibleArea = new Rect(viewportLeft, NewViewportTop, viewportWidthOverride, viewportHeightOverride);
				NewOrReformattedLines = new List<IWpfTextViewLine>();
				TranslatedLines = new List<IWpfTextViewLine>();
				AllVisibleLines = new List<IWpfTextViewLine>();
				foreach (var info in infos) {
					var line = info.Line;
					visibleLines.Add(line);
					AllVisibleLines.Add(line);
					if (!oldVisibleLines.Contains(line)) {
						line.SetChange(TextViewLineChange.NewOrReformatted);
						line.SetDeltaY(0);
						NewOrReformattedLines.Add(line);
					}
					else {
						line.SetChange(TextViewLineChange.Translated);
						line.SetDeltaY(info.Y - line.Top);
						TranslatedLines.Add(line);
					}
					line.SetTop(delta + info.Y);
					line.SetVisibleArea(visibleArea);
				}
				bool foundVisibleLine = false;
				foreach (var info in infos) {
					if (visibleLines.Contains(info.Line)) {
						foundVisibleLine = true;
						continue;
					}
					info.Line.SetChange(TextViewLineChange.None);
					info.Line.SetDeltaY(0);
					info.Line.SetTop(foundVisibleLine ? double.PositiveInfinity : double.NegativeInfinity);
					info.Line.SetVisibleArea(visibleArea);
				}
				Debug.Assert(NewOrReformattedLines.Count + TranslatedLines.Count == AllVisibleLines.Count);
				Debug.Assert(AllVisibleLines.Count >= 1);
				if (AllVisibleLines.Count == 0)
					throw new InvalidOperationException();
				AllVisiblePhysicalLines = new List<PhysicalLine>(keptLines);
			}

			List<LineInfo> CreateLineInfos(SnapshotPoint bufferPosition, ViewRelativePosition relativeTo, double verticalDistance, double viewportHeightOverride) {
				var lineInfos = new List<LineInfo>();
				var startLine = GetLine(bufferPosition);

				double newViewportBottom = NewViewportTop + viewportHeightOverride;
				double lineStartY;
				if (relativeTo == ViewRelativePosition.Top)
					lineStartY = NewViewportTop + verticalDistance;
				else {
					Debug.Assert(relativeTo == ViewRelativePosition.Bottom);
					lineStartY = NewViewportTop + viewportHeightOverride - verticalDistance - startLine.Height;
				}

				var currentLine = startLine;
				double y = lineStartY;
				if (y + currentLine.Height > NewViewportTop) {
					for (;;) {
						lineInfos.Add(new LineInfo(currentLine, y));
						if (y <= NewViewportTop)
							break;
						currentLine = GetLineBefore(currentLine);
						if (currentLine == null)
							break;
						y -= currentLine.Height;
					}
					lineInfos.Reverse();
				}

				currentLine = startLine;
				for (y = lineStartY + currentLine.Height; y < newViewportBottom;) {
					currentLine = GetLineAfter(currentLine);
					if (currentLine == null)
						break;
					lineInfos.Add(new LineInfo(currentLine, y));
					y += currentLine.Height;
				}
				Debug.Assert(new HashSet<IWpfTextViewLine>(lineInfos.Select(a => a.Line)).Count == lineInfos.Count);

				// At least one line must be included
				if (lineInfos.Count == 0)
					lineInfos.Add(new LineInfo(startLine, NewViewportTop));

				// Make sure that at least one line is shown
				var last = lineInfos[lineInfos.Count - 1];
				if (last.Y + last.Line.Height <= NewViewportTop)
					NewViewportTop = last.Y;
				var first = lineInfos[0];
				if (first.Y >= newViewportBottom)
					NewViewportTop = first.Y;

				return lineInfos;
			}

			IFormattedLine GetLine(SnapshotPoint point) {
				var physLine = GetPhysicalLine(point);
				if (point <= physLine.Lines[0].Start)
					return physLine.Lines[0];
				return physLine.Lines[physLine.Lines.Count - 1];
			}

			IFormattedLine GetLineBefore(IFormattedLine line) {
				var physLine = GetPhysicalLine(line);
				int index = physLine.Lines.IndexOf(line);
				if (index < 0)
					throw new InvalidOperationException();
				if (index > 0)
					return physLine.Lines[index - 1];
				if (physLine.BufferSpan.Start.Position == 0)
					return null;
				physLine = GetPhysicalLine(physLine.BufferSpan.Start - 1);
				return physLine.Lines[physLine.Lines.Count - 1];
			}

			IFormattedLine GetLineAfter(IFormattedLine line) {
				var physLine = GetPhysicalLine(line);
				int index = physLine.Lines.IndexOf(line);
				if (index < 0)
					throw new InvalidOperationException();
				if (index + 1 < physLine.Lines.Count)
					return physLine.Lines[index + 1];
				if (physLine.IsLastLine)
					return null;
				physLine = GetPhysicalLine(physLine.BufferSpan.End);
				return physLine.Lines[0];
			}

			PhysicalLine GetPhysicalLine(IFormattedLine line) => toPhysicalLine[line];

			PhysicalLine GetPhysicalLine(SnapshotPoint point) {
				foreach (var line in oldLines) {
					if (line.BufferSpan.Contains(point.Position))
						return line;
				}

				var result = CreatePhysicalLineNoCache(formattedLineSource, textViewModel, visualSnapshot, point);
				foreach (var line in result.Lines)
					toPhysicalLine[line] = result;
				oldLines.Add(result);

				return result;
			}
		}
	}
}
