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
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	static class MarkerHelper {
		public static readonly Thickness LineMarkerPadding = new Thickness();
		public static readonly Thickness TextMarkerPadding = new Thickness(0, 0, 0, 1);

		public static bool IsMultiLineSpan(ITextView textView, SnapshotSpan bufferSpan) {
			var line1 = textView.GetTextViewLineContainingBufferPosition(bufferSpan.Start);
			var line2 = textView.GetTextViewLineContainingBufferPosition(bufferSpan.End);
			return line1.ExtentIncludingLineBreak != line2.ExtentIncludingLineBreak;
		}

		public static Geometry CreateGeometry(IWpfTextView textView, VirtualSnapshotSpan span, bool isBoxMode, bool isMultiLine, bool clipToViewport = false, int maxLines = 1) {
			// Force multiline mode if it's box selection or there'll be 1px-sized non-selected lines
			// between the lines if you zoom in (eg. 110% zoom level)
			if (isBoxMode)
				isMultiLine = true;

			var padding = isMultiLine ? LineMarkerPadding : TextMarkerPadding;
			var pos = span.Start;
			PathGeometry geo = null;
			bool createOutlinedPath = false;

			int lines = 0;
			while (pos <= span.End && lines < maxLines) {
				var line = textView.GetTextViewLineContainingBufferPosition(pos.Position);
				bool useVspaces = line.IsLastDocumentLine() || lines + 1 == maxLines;
				var lineExtent = new VirtualSnapshotSpan(new VirtualSnapshotPoint(line.Start), new VirtualSnapshotPoint(line.EndIncludingLineBreak, useVspaces ? span.End.VirtualSpaces : 0));
				var extentTmp = lineExtent.Intersection(new VirtualSnapshotSpan(pos, span.End));
				Debug.Assert(extentTmp != null);
				if (extentTmp != null) {
					var extent = extentTmp.Value;
					Collection<TextBounds> textBounds;
					if (extent.Start.IsInVirtualSpace) {
						var leading = line.TextRight + extent.Start.VirtualSpaces * textView.FormattedLineSource.ColumnWidth;
						double width = isBoxMode ? 0 : line.EndOfLineWidth;
						int vspaces = span.End.VirtualSpaces - span.Start.VirtualSpaces;
						if (vspaces > 0)
							width = vspaces * textView.FormattedLineSource.ColumnWidth;
						textBounds = new Collection<TextBounds>();
						textBounds.Add(new TextBounds(leading, line.Top, width, line.Height, line.TextTop, line.TextHeight));
					}
					else if (extent.End.IsInVirtualSpace) {
						textBounds = line.GetNormalizedTextBounds(extent.SnapshotSpan);
						double width = extent.End.VirtualSpaces * textView.FormattedLineSource.ColumnWidth;
						textBounds.Add(new TextBounds(line.TextRight, line.Top, width, line.Height, line.TextTop, line.TextHeight));
					}
					else
						textBounds = line.GetNormalizedTextBounds(extent.SnapshotSpan);
					AddGeometries(textView, textBounds, isMultiLine, clipToViewport, padding, SystemParameters.CaretWidth, ref geo, ref createOutlinedPath);
				}

				if (line.IsLastDocumentLine())
					break;
				pos = new VirtualSnapshotPoint(line.EndIncludingLineBreak);
				lines++;
			}
			if (createOutlinedPath)
				geo = geo.GetOutlinedPathGeometry();
			if (geo != null && geo.CanFreeze)
				geo.Freeze();
			return geo;
		}

		public static void AddGeometries(IWpfTextView textView, Collection<TextBounds> textBounds, bool isLineGeometry, bool clipToViewport, Thickness padding, double minWidth, ref PathGeometry geo, ref bool createOutlinedPath) {
			foreach (var bounds in textBounds) {
				double left = bounds.Left - padding.Left;
				double right = bounds.Right + padding.Right;
				double top, bottom;
				if (isLineGeometry) {
					top = bounds.Top - padding.Top;
					bottom = bounds.Bottom + padding.Bottom;
					// This is needed to prevent a 1px-sized non-selected line between the selected
					// text lines when you've zoomed in to a certain zoom level (eg. 110%).
					bottom += 0.5;
				}
				else {
					top = bounds.TextTop - padding.Top;
					bottom = bounds.TextBottom + padding.Bottom;
				}
				if (right - left < minWidth)
					right = left + minWidth;
				if (clipToViewport) {
					left = Math.Max(left, textView.ViewportLeft);
					right = Math.Min(right, textView.ViewportRight);
				}
				if (right <= left || bottom <= top)
					continue;
				const double MAX_HEIGHT = 1000000;
				const double MAX_WIDTH = 1000000;
				double width = Math.Min(right - left, MAX_WIDTH);
				double height = Math.Min(bottom - top, MAX_HEIGHT);

				if (geo == null)
					geo = new PathGeometry { FillRule = FillRule.Nonzero };
				else
					createOutlinedPath = true;
				geo.AddGeometry(new RectangleGeometry(new Rect(left, top, width, height)));
			}
		}
	}
}
