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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class ViewScroller : IViewScroller {
		readonly ITextView textView;

		public ViewScroller(ITextView textView) {
			this.textView = textView;
		}

		public void EnsureSpanVisible(SnapshotSpan span) =>
			EnsureSpanVisible(new VirtualSnapshotSpan(span), EnsureSpanVisibleOptions.None);
		public void EnsureSpanVisible(SnapshotSpan span, EnsureSpanVisibleOptions options) =>
			EnsureSpanVisible(new VirtualSnapshotSpan(span), options);
		public void EnsureSpanVisible(VirtualSnapshotSpan span, EnsureSpanVisibleOptions options) {
			if (span.Snapshot != textView.TextSnapshot)
				throw new ArgumentException();
			throw new NotImplementedException();//TODO:
		}

		public void ScrollViewportHorizontallyByPixels(double distanceToScroll) =>
			textView.ViewportLeft += distanceToScroll;

		public void ScrollViewportVerticallyByPixels(double distanceToScroll) {
			var lines = textView.TextViewLines;
			if (lines.Count == 0)
				return;
			var line = distanceToScroll >= 0 ? lines[0] : lines[lines.Count - 1];
			textView.DisplayTextLineContainingBufferPosition(line.Start, line.Top - textView.ViewportTop + distanceToScroll, ViewRelativePosition.Top);
		}

		public void ScrollViewportVerticallyByLine(ScrollDirection direction) => ScrollViewportVerticallyByLines(direction, 1);
		public void ScrollViewportVerticallyByLines(ScrollDirection direction, int count) {
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (direction == ScrollDirection.Up) {
				for (int i = 0; i < count; i++) {
					double pixels = 0;
					var line = textView.TextViewLines.FirstVisibleLine;
					if (line.VisibilityState == VisibilityState.PartiallyVisible)
						pixels += textView.ViewportTop - line.Top;
					if (line.Start.Position != 0) {
						var prevLine = textView.GetTextViewLineContainingBufferPosition(new SnapshotPoint(line.Snapshot, line.Start.Position - 1));
						pixels += prevLine?.Height ?? 0;
					}
					if (pixels == 0)
						break;
					ScrollViewportVerticallyByPixels(pixels);
				}
			}
			else {
				for (int i = 0; i < count; i++) {
					double pixels = 0;
					var line = textView.TextViewLines.FirstVisibleLine;
					if (line.VisibilityState == VisibilityState.FullyVisible)
						pixels += line.Height;
					else {
						pixels += line.Bottom - textView.ViewportTop;
						var nextLine = textView.GetTextViewLineContainingBufferPosition(line.EndIncludingLineBreak);
						pixels += nextLine?.Height ?? 0;
					}
					if (pixels == 0)
						break;
					ScrollViewportVerticallyByPixels(-pixels);
				}
			}
		}

		public bool ScrollViewportVerticallyByPage(ScrollDirection direction) {
			throw new NotImplementedException();//TODO:
		}
	}
}
