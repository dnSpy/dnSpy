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
using System.Diagnostics;
using System.Linq;
using System.Windows;
using dnSpy.Text.Formatting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed partial class WpfTextView {
		sealed class LayoutHelper {
			readonly ILineTransformProvider lineTransformProvider;
			readonly List<PhysicalLine> oldLines;
			readonly IFormattedLineSource formattedLineSource;
			readonly ITextViewModel textViewModel;
			readonly ITextSnapshot visualSnapshot;
			readonly Dictionary<IFormattedLine, PhysicalLine> toPhysicalLine;
			readonly HashSet<ITextViewLine> oldVisibleLines;

			public double NewViewportTop { get; private set; }
			public List<PhysicalLine>? AllVisiblePhysicalLines { get; private set; }
			public List<IWpfTextViewLine>? AllVisibleLines { get; private set; }
			public List<IWpfTextViewLine>? NewOrReformattedLines { get; private set; }
			public List<IWpfTextViewLine>? TranslatedLines { get; private set; }
			readonly double requestedViewportTop;

			public LayoutHelper(ILineTransformProvider lineTransformProvider, double newViewportTop, HashSet<ITextViewLine> oldVisibleLines, List<PhysicalLine> oldLines, IFormattedLineSource formattedLineSource, ITextViewModel textViewModel, ITextSnapshot visualSnapshot, ITextSnapshot editSnapshot) {
				this.lineTransformProvider = lineTransformProvider;
				requestedViewportTop = newViewportTop;
				this.oldLines = oldLines;
				this.formattedLineSource = formattedLineSource;
				this.textViewModel = textViewModel;
				this.visualSnapshot = visualSnapshot;
				toPhysicalLine = new Dictionary<IFormattedLine, PhysicalLine>();
				this.oldVisibleLines = oldVisibleLines;

				Debug.Assert(oldLines.All(a => a.BufferSpan.Snapshot == editSnapshot));
				foreach (var physLine in oldLines) {
					physLine.TranslateLinesTo(visualSnapshot, editSnapshot);
					foreach (var line in physLine.Lines)
						toPhysicalLine[line] = physLine;
				}
			}

			readonly struct LineInfo {
				public IFormattedLine Line { get; }
				public double Y { get; }
				public LineInfo(IFormattedLine line, double y) {
					Line = line;
					Y = y;
				}
			}

			// Try to cause as little changes to the lines' Top property as possible.
			// Existing lines should have a delta == 0 if possible.
			double GetNewViewportTop(List<LineInfo> infos, double tempViewportTop) {
				foreach (var info in infos) {
					if (info.Line.VisibilityState == VisibilityState.Unattached)
						continue;
					return tempViewportTop - info.Y + info.Line.Top;
				}
				return tempViewportTop;
			}

			public void LayoutLines(SnapshotPoint bufferPosition, ViewRelativePosition relativeTo, double verticalDistance, double viewportLeft, double viewportWidthOverride, double viewportHeightOverride) {
				NewViewportTop = requestedViewportTop;
				var infos = CreateLineInfos(bufferPosition, relativeTo, verticalDistance, viewportHeightOverride);

				// The first line of the file must always be shown at the top of the view
				if (infos[0].Y > NewViewportTop) {
					infos = CreateLineInfos(new SnapshotPoint(bufferPosition.Snapshot, 0), ViewRelativePosition.Top, 0, viewportHeightOverride);
					Debug.Assert(infos[0].Y == NewViewportTop);
				}

				// Include a hidden line before the first line and one after the last line,
				// just like in VS' IWpfTextViewLine collection.
				var firstInfo = infos[0];
				var prevLine = AddLineTransform(GetLineBefore(firstInfo.Line), firstInfo.Y, ViewRelativePosition.Bottom);
				if (prevLine is not null)
					infos.Insert(0, new LineInfo(prevLine, firstInfo.Y - prevLine.Height));
				var lastInfo = infos[infos.Count - 1];
				var nextLine = AddLineTransform(GetLineAfter(lastInfo.Line), lastInfo.Y + lastInfo.Line.Height, ViewRelativePosition.Top);
				if (nextLine is not null)
					infos.Add(new LineInfo(nextLine, lastInfo.Y + lastInfo.Line.Height));

				var keptLines = new HashSet<PhysicalLine>();
				foreach (var info in infos)
					keptLines.Add(toPhysicalLine[info.Line]);
				foreach (var physLine in toPhysicalLine.Values) {
					if (!keptLines.Contains(physLine))
						physLine.Dispose();
				}

				var newTop = GetNewViewportTop(infos, NewViewportTop);
				var delta = -NewViewportTop + newTop;
				NewViewportTop = newTop;
				var visibleLines = new HashSet<IWpfTextViewLine>();
				var visibleArea = new Rect(viewportLeft, NewViewportTop, viewportWidthOverride, viewportHeightOverride);
				NewOrReformattedLines = new List<IWpfTextViewLine>();
				TranslatedLines = new List<IWpfTextViewLine>();
				AllVisibleLines = new List<IWpfTextViewLine>();
				foreach (var info in infos) {
					var line = info.Line;
					visibleLines.Add(line);
					AllVisibleLines.Add(line);
					double newLineTop = delta + info.Y;
					if (!oldVisibleLines.Contains(line)) {
						line.SetChange(TextViewLineChange.NewOrReformatted);
						line.SetDeltaY(0);
					}
					else {
						var deltaY = newLineTop - line.Top;
						line.SetDeltaY(deltaY);
						// If it got a new line transform, it will have Change == NewOrReformatted,
						// and that change has priority over Translated.
						if (deltaY != 0 && line.Change == TextViewLineChange.None)
							line.SetChange(TextViewLineChange.Translated);
					}
					line.SetTop(newLineTop);
					line.SetVisibleArea(visibleArea);
					if (line.Change == TextViewLineChange.Translated)
						TranslatedLines.Add(line);
					else if (line.Change == TextViewLineChange.NewOrReformatted)
						NewOrReformattedLines.Add(line);
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
				Debug.Assert(NewOrReformattedLines.Count + TranslatedLines.Count <= AllVisibleLines.Count);
				Debug.Assert(AllVisibleLines.Count >= 1);
				if (AllVisibleLines.Count == 0)
					throw new InvalidOperationException();
				AllVisiblePhysicalLines = new List<PhysicalLine>(keptLines);
			}

			List<LineInfo> CreateLineInfos(SnapshotPoint bufferPosition, ViewRelativePosition relativeTo, double verticalDistance, double viewportHeightOverride) {
				var lineInfos = new List<LineInfo>();
				var startLine = GetLine(bufferPosition);
				Debug2.Assert(startLine is not null);

				double newViewportBottom = NewViewportTop + viewportHeightOverride;
				double lineStartY;
				if (relativeTo == ViewRelativePosition.Top) {
					lineStartY = NewViewportTop + verticalDistance;
					AddLineTransform(startLine, lineStartY, ViewRelativePosition.Top);
				}
				else {
					Debug.Assert(relativeTo == ViewRelativePosition.Bottom);
					lineStartY = NewViewportTop + viewportHeightOverride - verticalDistance;
					AddLineTransform(startLine, lineStartY, ViewRelativePosition.Bottom);
					lineStartY -= startLine.Height;
				}

				IFormattedLine? currentLine = startLine;
				double y = lineStartY;
				if (y + currentLine.Height > NewViewportTop) {
					for (;;) {
						lineInfos.Add(new LineInfo(currentLine, y));
						if (y <= NewViewportTop)
							break;
						currentLine = AddLineTransform(GetLineBefore(currentLine), y, ViewRelativePosition.Bottom);
						if (currentLine is null)
							break;
						y -= currentLine.Height;
					}
					lineInfos.Reverse();
				}

				currentLine = startLine;
				for (y = lineStartY + currentLine.Height; y < newViewportBottom;) {
					currentLine = AddLineTransform(GetLineAfter(currentLine), y, ViewRelativePosition.Top);
					if (currentLine is null)
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

			IFormattedLine? AddLineTransform(IFormattedLine? line, double yPosition, ViewRelativePosition placement) {
				if (line is not null) {
					var lineTransform = lineTransformProvider.GetLineTransform(line, yPosition, placement);
					if (lineTransform != line.LineTransform) {
						line.SetLineTransform(lineTransform);
						line.SetChange(TextViewLineChange.NewOrReformatted);
					}
				}
				return line;
			}

			IFormattedLine? GetLine(SnapshotPoint point) => GetPhysicalLine(point).FindFormattedLineByBufferPosition(point);

			IFormattedLine? GetLineBefore(IFormattedLine line) {
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

			IFormattedLine? GetLineAfter(IFormattedLine line) {
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
					if (line.Contains(point))
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
