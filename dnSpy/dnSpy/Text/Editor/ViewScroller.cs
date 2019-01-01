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
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class ViewScroller : IViewScroller {
		readonly ITextView textView;

		public ViewScroller(ITextView textView) => this.textView = textView;

		public void EnsureSpanVisible(SnapshotSpan span) =>
			EnsureSpanVisible(new VirtualSnapshotSpan(span), EnsureSpanVisibleOptions.None);
		public void EnsureSpanVisible(SnapshotSpan span, EnsureSpanVisibleOptions options) =>
			EnsureSpanVisible(new VirtualSnapshotSpan(span), options);
		public void EnsureSpanVisible(VirtualSnapshotSpan span, EnsureSpanVisibleOptions options) {
			if (span.Snapshot != textView.TextSnapshot)
				throw new ArgumentException();

			if ((textView.TextViewLines?.Count ?? 0) == 0)
				return;

			EnsureSpanVisibleY(span, options);
			EnsureSpanVisibleX(span, options);
		}

		void EnsureSpanVisibleX(VirtualSnapshotSpan span, EnsureSpanVisibleOptions options) {
			if ((textView.Options.WordWrapStyle() & WordWrapStyles.WordWrap) != 0)
				return;
			if (textView.ViewportWidth == 0)
				return;

			var lines = textView.TextViewLines.GetTextViewLinesIntersectingSpan(span.SnapshotSpan);
			if (lines.Count == 0)
				return;

			var ispan = span.Intersection(new VirtualSnapshotSpan(textView.TextViewLines.FormattedSpan));
			if (ispan == null)
				return;
			span = ispan.Value;

			double left = double.PositiveInfinity, right = double.NegativeInfinity;
			for (int i = 0; i < lines.Count; i++) {
				var line = lines[i];
				var lineSpan = line.ExtentIncludingLineBreak.Intersection(span.SnapshotSpan);
				if (lineSpan == null)
					continue;

				var startPoint = new VirtualSnapshotPoint(lineSpan.Value.Start);
				var endPoint = new VirtualSnapshotPoint(lineSpan.Value.End);
				if (startPoint.Position == span.Start.Position)
					startPoint = span.Start;
				if (endPoint.Position == span.End.Position)
					endPoint = span.End;

				var startBounds = line.GetExtendedCharacterBounds(startPoint);
				var endBounds = line.GetExtendedCharacterBounds(endPoint);

				if (left > startBounds.Left)
					left = startBounds.Left;
				if (right < startBounds.Right)
					right = startBounds.Right;
				if (left > endBounds.Left)
					left = endBounds.Left;
				if (right < endBounds.Right)
					right = endBounds.Right;

			}
			if (double.IsInfinity(left) || double.IsInfinity(right))
				return;
			Debug.Assert(left <= right);
			if (left > right)
				right = left;
			double width = right - left;

			double availWidth = Math.Max(0, textView.ViewportWidth - width);
			double extraScroll;
			const double EXTRA_WIDTH = 4;
			if (availWidth >= EXTRA_WIDTH)
				extraScroll = EXTRA_WIDTH;
			else
				extraScroll = availWidth / 2;

			if (textView.ViewportLeft <= right && right <= textView.ViewportRight) {
			}
			else if (right > textView.ViewportRight)
				textView.ViewportLeft = right + extraScroll - textView.ViewportWidth;
			else {
				var newLeft = left - extraScroll;
				if (newLeft + textView.ViewportWidth < right)
					newLeft = right - textView.ViewportWidth;
				textView.ViewportLeft = newLeft;
			}
		}

		void EnsureSpanVisibleY(VirtualSnapshotSpan span, EnsureSpanVisibleOptions options) {
			bool showStart = (options & EnsureSpanVisibleOptions.ShowStart) != 0;
			bool minimumScroll = (options & EnsureSpanVisibleOptions.MinimumScroll) != 0;
			bool alwaysCenter = (options & EnsureSpanVisibleOptions.AlwaysCenter) != 0;

			var bufferSpan = span.SnapshotSpan;
			var visibleSpan = VisibleSpan;
			bool spanIsInView = bufferSpan.Start >= visibleSpan.Start && bufferSpan.End <= visibleSpan.End;
			if (!spanIsInView) {
				ShowSpan(bufferSpan, options);
				alwaysCenter = true;
				visibleSpan = VisibleSpan;
				spanIsInView = bufferSpan.Start >= visibleSpan.Start && bufferSpan.End <= visibleSpan.End;
			}

			if (spanIsInView) {
				var lines = textView.TextViewLines.GetTextViewLinesIntersectingSpan(bufferSpan);
				Debug.Assert(lines.Count > 0);
				if (lines.Count == 0)
					return;
				var first = lines[0];
				var last = lines[lines.Count - 1];
				var firstSpan = first.ExtentIncludingLineBreak;
				var lastSpan = last.ExtentIncludingLineBreak;

				bool allLinesFullyVisible = first.VisibilityState == VisibilityState.FullyVisible && last.VisibilityState == VisibilityState.FullyVisible;

				if (alwaysCenter || (!allLinesFullyVisible && !minimumScroll)) {
					double height = last.Bottom - first.Top;
					double verticalDistance = (textView.ViewportHeight - height) / 2;
					textView.DisplayTextLineContainingBufferPosition(first.Start, verticalDistance, ViewRelativePosition.Top);
					return;
				}

				if (first.VisibilityState != VisibilityState.FullyVisible) {
					if (first != last || !minimumScroll || first.VisibilityState != VisibilityState.PartiallyVisible)
						textView.DisplayTextLineContainingBufferPosition(first.Start, 0, ViewRelativePosition.Top);
					else if (first.Bottom > textView.ViewportBottom)
						textView.DisplayTextLineContainingBufferPosition(first.Start, 0, ViewRelativePosition.Bottom);
					else
						textView.DisplayTextLineContainingBufferPosition(first.Start, 0, ViewRelativePosition.Top);
				}
				else if (last.VisibilityState != VisibilityState.FullyVisible)
					textView.DisplayTextLineContainingBufferPosition(last.Start, 0, ViewRelativePosition.Bottom);

				if (showStart) {
					var line = textView.TextViewLines.GetTextViewLineContainingBufferPosition(firstSpan.Start);
					if (line == null || line.VisibilityState != VisibilityState.FullyVisible)
						ShowSpan(bufferSpan, options);
				}
				else {
					var line = textView.TextViewLines.GetTextViewLineContainingBufferPosition(lastSpan.Start);
					if (line == null || line.VisibilityState != VisibilityState.FullyVisible)
						ShowSpan(bufferSpan, options);
				}
			}
		}

		void ShowSpan(SnapshotSpan bufferSpan, EnsureSpanVisibleOptions options) {
			if ((options & EnsureSpanVisibleOptions.ShowStart) != 0)
				textView.DisplayTextLineContainingBufferPosition(bufferSpan.Start, 0, ViewRelativePosition.Top);
			else {
				var end = bufferSpan.End;
				if (end.Position != 0)
					end = end - 1;
				textView.DisplayTextLineContainingBufferPosition(end, 0, ViewRelativePosition.Bottom);
			}
		}

		SnapshotSpan VisibleSpan => new SnapshotSpan(textView.TextViewLines.FirstVisibleLine.Start, textView.TextViewLines.LastVisibleLine.EndIncludingLineBreak);

		public void ScrollViewportHorizontallyByPixels(double distanceToScroll) =>
			textView.ViewportLeft += distanceToScroll;

		public void ScrollViewportVerticallyByPixels(double distanceToScroll) {
			var lines = textView.TextViewLines;
			if (lines == null)
				return;
			var line = distanceToScroll >= 0 ? lines.FirstVisibleLine : lines.LastVisibleLine;
			textView.DisplayTextLineContainingBufferPosition(line.Start, line.Top - textView.ViewportTop + distanceToScroll, ViewRelativePosition.Top);
		}

		public void ScrollViewportVerticallyByLine(ScrollDirection direction) => ScrollViewportVerticallyByLines(direction, 1);
		public void ScrollViewportVerticallyByLines(ScrollDirection direction, int count) {
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (direction == ScrollDirection.Up) {
				double pixels = 0;
				var line = textView.TextViewLines.FirstVisibleLine;
				for (int i = 0; i < count; i++) {
					if (i == 0) {
						if (line.VisibilityState == VisibilityState.PartiallyVisible)
							pixels += textView.ViewportTop - line.Top;
						if (line.Start.Position != 0) {
							line = textView.GetTextViewLineContainingBufferPosition(line.Start - 1);
							pixels += line.Height;
						}
					}
					else {
						if (line.VisibilityState == VisibilityState.Unattached) {
							// Height is only fully initialized once it's been shown on the screen
							// (its LineTransform property is used to calculate Height)
							var lineStart = line.Start;
							textView.DisplayTextLineContainingBufferPosition(lineStart, 0, ViewRelativePosition.Top);
							line = textView.GetTextViewLineContainingBufferPosition(lineStart);
							Debug.Assert(line.VisibilityState != VisibilityState.Unattached);
							pixels = 0;
						}
						else
							pixels += line.Height;
					}
					if (line.Start.Position == 0)
						break;
					line = textView.GetTextViewLineContainingBufferPosition(line.Start - 1);
				}
				if (pixels != 0)
					ScrollViewportVerticallyByPixels(pixels);
			}
			else {
				Debug.Assert(direction == ScrollDirection.Down);
				double pixels = 0;
				var line = textView.TextViewLines.FirstVisibleLine;
				for (int i = 0; i < count; i++) {
					if (line.IsLastDocumentLine())
						break;
					if (i == 0) {
						if (line.VisibilityState == VisibilityState.FullyVisible)
							pixels += line.Height;
						else {
							pixels += line.Bottom - textView.ViewportTop;
							line = textView.GetTextViewLineContainingBufferPosition(line.GetPointAfterLineBreak());
							pixels += line.Height;
						}
					}
					else {
						if (line.VisibilityState == VisibilityState.Unattached) {
							// Height is only fully initialized once it's been shown on the screen
							// (its LineTransform property is used to calculate Height)
							var lineStart = line.Start;
							textView.DisplayTextLineContainingBufferPosition(lineStart, 0, ViewRelativePosition.Top);
							line = textView.GetTextViewLineContainingBufferPosition(lineStart);
							Debug.Assert(line.VisibilityState != VisibilityState.Unattached);
							pixels = 0;
						}
						else
							pixels += line.Height;
					}
					if (line.IsLastDocumentLine())
						break;
					line = textView.GetTextViewLineContainingBufferPosition(line.GetPointAfterLineBreak());
				}
				if (pixels != 0)
					ScrollViewportVerticallyByPixels(-pixels);
			}
		}

		public bool ScrollViewportVerticallyByPage(ScrollDirection direction) {
			bool hasFullyVisibleLines = textView.TextViewLines.Any(a => a.VisibilityState == VisibilityState.FullyVisible);

			if (direction == ScrollDirection.Up) {
				var firstVisibleLine = textView.TextViewLines.FirstVisibleLine;
				if (firstVisibleLine.Height > textView.ViewportHeight) {
					ScrollViewportVerticallyByPixels(textView.ViewportHeight);
					return hasFullyVisibleLines;
				}
				double top;
				if (firstVisibleLine.VisibilityState == VisibilityState.FullyVisible) {
					if (firstVisibleLine.IsFirstDocumentLine())
						return hasFullyVisibleLines;
					top = firstVisibleLine.Top;
				}
				else
					top = firstVisibleLine.Bottom; // Top of next line, which is possibly not in TextViewLines so we can't use its Top prop
				var line = firstVisibleLine;
				// Top is only valid if the line is in TextViewLines, so use this variable to track the correct line top value
				double lineTop = line.Top;
				var prevLine = line;
				// Cache this since prevLine could've been disposed when we need to access this property
				var prevLineStart = prevLine.Start;
				while (lineTop + textView.ViewportHeight >= top) {
					prevLine = line;
					prevLineStart = prevLine.Start;
					if (line.IsFirstDocumentLine())
						break;
					line = textView.GetTextViewLineContainingBufferPosition(line.Start - 1);
					if (line.VisibilityState == VisibilityState.Unattached) {
						// Height is only fully initialized once it's been shown on the screen
						// (its LineTransform property is used to calculate Height)
						var lineStart = line.Start;
						textView.DisplayTextLineContainingBufferPosition(lineStart, 0, ViewRelativePosition.Bottom);
						line = textView.GetTextViewLineContainingBufferPosition(lineStart);
						Debug.Assert(line.VisibilityState != VisibilityState.Unattached);
					}
					lineTop -= line.Height;
				}
				textView.DisplayTextLineContainingBufferPosition(prevLineStart, 0, ViewRelativePosition.Top);
			}
			else {
				Debug.Assert(direction == ScrollDirection.Down);
				double pixels = textView.ViewportHeight;
				var lastVisibleLine = textView.TextViewLines.LastVisibleLine;
				if (lastVisibleLine.Height > textView.ViewportHeight) {
					// This line intentionally left blank
				}
				else if (lastVisibleLine.VisibilityState == VisibilityState.FullyVisible) {
					if (lastVisibleLine.IsLastDocumentLine()) {
						textView.DisplayTextLineContainingBufferPosition(lastVisibleLine.Start, 0, ViewRelativePosition.Top);
						return hasFullyVisibleLines;
					}
				}
				else
					pixels -= textView.ViewportBottom - lastVisibleLine.Top;
				ScrollViewportVerticallyByPixels(-pixels);
			}

			return hasFullyVisibleLines;
		}
	}
}
