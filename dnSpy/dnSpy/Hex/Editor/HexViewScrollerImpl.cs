/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using VST = Microsoft.VisualStudio.Text;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Editor {
	sealed class HexViewScrollerImpl : HexViewScroller {
		readonly HexView hexView;

		public HexViewScrollerImpl(HexView hexView) => this.hexView = hexView ?? throw new ArgumentNullException(nameof(hexView));

		public override void EnsureSpanVisible(HexLineSpan lineSpan, VSTE.EnsureSpanVisibleOptions options) {
			if (lineSpan.IsDefault)
				throw new ArgumentException();
			if (lineSpan.BufferSpan.Buffer != hexView.Buffer)
				throw new ArgumentException();
			EnsureSpanVisibleCore(lineSpan, options);
		}

		public override void EnsureSpanVisible(HexBufferSpan span, HexSpanSelectionFlags flags, VSTE.EnsureSpanVisibleOptions options) {
			if (span.Buffer != hexView.Buffer)
				throw new ArgumentException();
			EnsureSpanVisibleCore(new HexLineSpan(span, flags), options);
		}

		public override void EnsureSpanVisible(HexBufferLine line, VST.Span span, VSTE.EnsureSpanVisibleOptions options) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			if (line.Buffer != hexView.Buffer)
				throw new ArgumentException();
			EnsureSpanVisibleCore(new HexLineSpan(line, span), options);
		}

		void EnsureSpanVisibleCore(HexLineSpan lineSpan, VSTE.EnsureSpanVisibleOptions options) {
			if (lineSpan.BufferSpan.Buffer != hexView.Buffer)
				throw new ArgumentException();

			if ((hexView.HexViewLines?.Count ?? 0) == 0)
				return;

			EnsureSpanVisibleY(lineSpan.BufferSpan, options);
			EnsureSpanVisibleX(lineSpan, options);
		}

		void EnsureSpanVisibleX(HexLineSpan lineSpan, VSTE.EnsureSpanVisibleOptions options) {
			var span = lineSpan.BufferSpan;
			if (hexView.ViewportWidth == 0)
				return;

			var lines = hexView.HexViewLines.GetHexViewLinesIntersectingSpan(span);
			if (lines.Count == 0)
				return;

			var ispan = span.Intersection(hexView.HexViewLines.FormattedSpan);
			if (ispan == null)
				return;
			span = ispan.Value;

			double left = double.PositiveInfinity, right = double.NegativeInfinity;
			for (int i = 0; i < lines.Count; i++) {
				var line = lines[i];
				var intersection = line.BufferSpan.Intersection(span);
				if (intersection == null)
					continue;

				var bounds = lineSpan.IsTextSpan ?
					line.GetNormalizedTextBounds(lineSpan.TextSpan.Value) :
					line.GetNormalizedTextBounds(intersection.Value, lineSpan.SelectionFlags.Value);
				foreach (var b in bounds) {
					if (left > b.Left)
						left = b.Left;
					if (right < b.Right)
						right = b.Right;
				}
			}
			if (double.IsInfinity(left) || double.IsInfinity(right))
				return;
			Debug.Assert(left <= right);
			if (left > right)
				right = left;
			double width = right - left;

			double availWidth = Math.Max(0, hexView.ViewportWidth - width);
			double extraScroll;
			const double EXTRA_WIDTH = 4;
			if (availWidth >= EXTRA_WIDTH)
				extraScroll = EXTRA_WIDTH;
			else
				extraScroll = availWidth / 2;

			if (hexView.ViewportLeft <= right && right <= hexView.ViewportRight) {
			}
			else if (right > hexView.ViewportRight)
				hexView.ViewportLeft = right + extraScroll - hexView.ViewportWidth;
			else {
				var newLeft = left - extraScroll;
				if (newLeft + hexView.ViewportWidth < right)
					newLeft = right - hexView.ViewportWidth;
				hexView.ViewportLeft = newLeft;
			}
		}

		void EnsureSpanVisibleY(HexBufferSpan span, VSTE.EnsureSpanVisibleOptions options) {
			bool showStart = (options & VSTE.EnsureSpanVisibleOptions.ShowStart) != 0;
			bool minimumScroll = (options & VSTE.EnsureSpanVisibleOptions.MinimumScroll) != 0;
			bool alwaysCenter = (options & VSTE.EnsureSpanVisibleOptions.AlwaysCenter) != 0;

			var visibleSpan = VisibleSpan;
			bool spanIsInView = span.Start >= visibleSpan.Start && span.End <= visibleSpan.End;
			if (!spanIsInView) {
				ShowSpan(span, options);
				alwaysCenter = true;
				visibleSpan = VisibleSpan;
				spanIsInView = span.Start >= visibleSpan.Start && span.End <= visibleSpan.End;
			}

			if (spanIsInView) {
				var lines = hexView.HexViewLines.GetHexViewLinesIntersectingSpan(span);
				Debug.Assert(lines.Count > 0);
				if (lines.Count == 0)
					return;
				var first = lines[0];
				var last = lines[lines.Count - 1];
				if (lines.Count > 1 && last.BufferStart == span.End)
					last = lines[lines.Count - 2];
				var firstSpan = first.BufferSpan;
				var lastSpan = last.BufferSpan;

				bool allLinesFullyVisible = first.VisibilityState == VSTF.VisibilityState.FullyVisible && last.VisibilityState == VSTF.VisibilityState.FullyVisible;

				if (alwaysCenter || (!allLinesFullyVisible && !minimumScroll)) {
					double height = last.Bottom - first.Top;
					double verticalDistance = (hexView.ViewportHeight - height) / 2;
					hexView.DisplayHexLineContainingBufferPosition(first.BufferStart, verticalDistance, VSTE.ViewRelativePosition.Top);
					return;
				}

				if (first.VisibilityState != VSTF.VisibilityState.FullyVisible) {
					if (first != last || !minimumScroll || first.VisibilityState != VSTF.VisibilityState.PartiallyVisible)
						hexView.DisplayHexLineContainingBufferPosition(first.BufferStart, 0, VSTE.ViewRelativePosition.Top);
					else if (first.Bottom > hexView.ViewportBottom)
						hexView.DisplayHexLineContainingBufferPosition(first.BufferStart, 0, VSTE.ViewRelativePosition.Bottom);
					else
						hexView.DisplayHexLineContainingBufferPosition(first.BufferStart, 0, VSTE.ViewRelativePosition.Top);
				}
				else if (last.VisibilityState != VSTF.VisibilityState.FullyVisible)
					hexView.DisplayHexLineContainingBufferPosition(last.BufferStart, 0, VSTE.ViewRelativePosition.Bottom);

				if (showStart) {
					var line = hexView.HexViewLines.GetHexViewLineContainingBufferPosition(firstSpan.Start);
					if (line == null || line.VisibilityState != VSTF.VisibilityState.FullyVisible)
						ShowSpan(span, options);
				}
				else {
					var line = hexView.HexViewLines.GetHexViewLineContainingBufferPosition(lastSpan.Start);
					if (line == null || line.VisibilityState != VSTF.VisibilityState.FullyVisible)
						ShowSpan(span, options);
				}
			}
		}

		void ShowSpan(HexBufferSpan bufferSpan, VSTE.EnsureSpanVisibleOptions options) {
			if ((options & VSTE.EnsureSpanVisibleOptions.ShowStart) != 0)
				hexView.DisplayHexLineContainingBufferPosition(bufferSpan.Start, 0, VSTE.ViewRelativePosition.Top);
			else {
				var end = bufferSpan.End;
				if (end > hexView.BufferLines.BufferStart)
					end = end - 1;
				hexView.DisplayHexLineContainingBufferPosition(end, 0, VSTE.ViewRelativePosition.Bottom);
			}
		}

		HexBufferSpan VisibleSpan => new HexBufferSpan(hexView.HexViewLines.FirstVisibleLine.BufferStart, hexView.HexViewLines.LastVisibleLine.BufferEnd);

		public override void ScrollViewportHorizontallyByPixels(double distanceToScroll) =>
			hexView.ViewportLeft += distanceToScroll;

		public override void ScrollViewportVerticallyByPixels(double distanceToScroll) {
			var lines = hexView.HexViewLines;
			if (lines == null)
				return;
			var line = distanceToScroll >= 0 ? lines.FirstVisibleLine : lines.LastVisibleLine;
			hexView.DisplayHexLineContainingBufferPosition(line.BufferStart, line.Top - hexView.ViewportTop + distanceToScroll, VSTE.ViewRelativePosition.Top);
		}

		public override void ScrollViewportVerticallyByLines(VSTE.ScrollDirection direction, int count) {
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (direction == VSTE.ScrollDirection.Up) {
				double pixels = 0;
				var line = hexView.HexViewLines.FirstVisibleLine;
				for (int i = 0; i < count; i++) {
					if (i == 0) {
						if (line.VisibilityState == VSTF.VisibilityState.PartiallyVisible)
							pixels += hexView.ViewportTop - line.Top;
						if (line.BufferStart > hexView.BufferLines.BufferStart) {
							line = hexView.GetHexViewLineContainingBufferPosition(line.BufferStart - 1);
							pixels += line.Height;
						}
					}
					else {
						if (line.VisibilityState == VSTF.VisibilityState.Unattached) {
							// Height is only fully initialized once it's been shown on the screen
							// (its LineTransform property is used to calculate Height)
							var lineStart = line.BufferStart;
							hexView.DisplayHexLineContainingBufferPosition(lineStart, 0, VSTE.ViewRelativePosition.Top);
							line = hexView.GetHexViewLineContainingBufferPosition(lineStart);
							Debug.Assert(line.VisibilityState != VSTF.VisibilityState.Unattached);
							pixels = 0;
						}
						else
							pixels += line.Height;
					}
					if (line.BufferStart <= hexView.BufferLines.BufferStart)
						break;
					line = hexView.GetHexViewLineContainingBufferPosition(line.BufferStart - 1);
				}
				if (pixels != 0)
					ScrollViewportVerticallyByPixels(pixels);
			}
			else {
				Debug.Assert(direction == VSTE.ScrollDirection.Down);
				double pixels = 0;
				var line = hexView.HexViewLines.FirstVisibleLine;
				for (int i = 0; i < count; i++) {
					if (line.IsLastDocumentLine())
						break;
					if (i == 0) {
						if (line.VisibilityState == VSTF.VisibilityState.FullyVisible)
							pixels += line.Height;
						else {
							pixels += line.Bottom - hexView.ViewportTop;
							line = hexView.GetHexViewLineContainingBufferPosition(line.BufferEnd);
							pixels += line.Height;
						}
					}
					else {
						if (line.VisibilityState == VSTF.VisibilityState.Unattached) {
							// Height is only fully initialized once it's been shown on the screen
							// (its LineTransform property is used to calculate Height)
							var lineStart = line.BufferStart;
							hexView.DisplayHexLineContainingBufferPosition(lineStart, 0, VSTE.ViewRelativePosition.Top);
							line = hexView.GetHexViewLineContainingBufferPosition(lineStart);
							Debug.Assert(line.VisibilityState != VSTF.VisibilityState.Unattached);
							pixels = 0;
						}
						else
							pixels += line.Height;
					}
					if (line.IsLastDocumentLine())
						break;
					line = hexView.GetHexViewLineContainingBufferPosition(line.BufferEnd);
				}
				if (pixels != 0)
					ScrollViewportVerticallyByPixels(-pixels);
			}
		}

		public override bool ScrollViewportVerticallyByPage(VSTE.ScrollDirection direction) {
			bool hasFullyVisibleLines = hexView.HexViewLines.Any(a => a.VisibilityState == VSTF.VisibilityState.FullyVisible);

			if (direction == VSTE.ScrollDirection.Up) {
				var firstVisibleLine = hexView.HexViewLines.FirstVisibleLine;
				if (firstVisibleLine.Height > hexView.ViewportHeight) {
					ScrollViewportVerticallyByPixels(hexView.ViewportHeight);
					return hasFullyVisibleLines;
				}
				double top;
				if (firstVisibleLine.VisibilityState == VSTF.VisibilityState.FullyVisible) {
					if (firstVisibleLine.IsFirstDocumentLine())
						return hasFullyVisibleLines;
					top = firstVisibleLine.Top;
				}
				else
					top = firstVisibleLine.Bottom; // Top of next line, which is possibly not in HexViewLines so we can't use its Top prop
				var line = firstVisibleLine;
				// Top is only valid if the line is in HexViewLines, so use this variable to track the correct line top value
				double lineTop = line.Top;
				var prevLine = line;
				// Cache this since prevLine could've been disposed when we need to access this property
				var prevLineStart = prevLine.BufferStart;
				while (lineTop + hexView.ViewportHeight >= top) {
					prevLine = line;
					prevLineStart = prevLine.BufferStart;
					if (line.IsFirstDocumentLine())
						break;
					line = hexView.GetHexViewLineContainingBufferPosition(line.BufferStart - 1);
					if (line.VisibilityState == VSTF.VisibilityState.Unattached) {
						// Height is only fully initialized once it's been shown on the screen
						// (its LineTransform property is used to calculate Height)
						var lineStart = line.BufferStart;
						hexView.DisplayHexLineContainingBufferPosition(lineStart, 0, VSTE.ViewRelativePosition.Bottom);
						line = hexView.GetHexViewLineContainingBufferPosition(lineStart);
						Debug.Assert(line.VisibilityState != VSTF.VisibilityState.Unattached);
					}
					lineTop -= line.Height;
				}
				hexView.DisplayHexLineContainingBufferPosition(prevLineStart, 0, VSTE.ViewRelativePosition.Top);
			}
			else {
				Debug.Assert(direction == VSTE.ScrollDirection.Down);
				double pixels = hexView.ViewportHeight;
				var lastVisibleLine = hexView.HexViewLines.LastVisibleLine;
				if (lastVisibleLine.Height > hexView.ViewportHeight) {
					// This line intentionally left blank
				}
				else if (lastVisibleLine.VisibilityState == VSTF.VisibilityState.FullyVisible) {
					if (lastVisibleLine.IsLastDocumentLine()) {
						hexView.DisplayHexLineContainingBufferPosition(lastVisibleLine.BufferStart, 0, VSTE.ViewRelativePosition.Top);
						return hasFullyVisibleLines;
					}
				}
				else
					pixels -= hexView.ViewportBottom - lastVisibleLine.Top;
				ScrollViewportVerticallyByPixels(-pixels);
			}

			return hasFullyVisibleLines;
		}
	}
}
