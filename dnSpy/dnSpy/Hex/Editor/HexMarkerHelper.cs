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
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Editor {
	static class HexMarkerHelper {
		public static readonly Thickness LineMarkerPadding = new Thickness();
		public static readonly Thickness TextMarkerPadding = new Thickness(0, 0, 0, 1);

		public static bool IsMultiLineSpan(HexView hexView, HexBufferSpan bufferSpan) {
			if (bufferSpan.Length == 0)
				return false;
			if (bufferSpan.End == hexView.BufferLines.BufferEnd) {
				var lineNum = hexView.BufferLines.GetLineNumberFromPosition(bufferSpan.Start);
				return lineNum + 1 != hexView.BufferLines.LineCount;
			}
			var lineNum1 = hexView.BufferLines.GetLineNumberFromPosition(bufferSpan.Start);
			var lineNum2 = hexView.BufferLines.GetLineNumberFromPosition(bufferSpan.End);
			return lineNum1 != lineNum2;
		}

		public static void AddGeometries(WpfHexView hexView, Collection<VSTF.TextBounds> textBounds, bool isLineGeometry, bool clipToViewport, Thickness padding, double minWidth, ref PathGeometry geo, ref bool createOutlinedPath) {
			foreach (var bounds in textBounds) {
				double left = bounds.Left - padding.Left;
				double right = bounds.Right + padding.Right;
				double top, bottom;
				if (isLineGeometry) {
					top = bounds.Top - padding.Top;
					bottom = bounds.Bottom + padding.Bottom;
				}
				else {
					top = bounds.TextTop - padding.Top;
					bottom = bounds.TextBottom + padding.Bottom;
				}
				if (right - left < minWidth)
					right = left + minWidth;
				if (clipToViewport) {
					left = Math.Max(left, hexView.ViewportLeft);
					right = Math.Min(right, hexView.ViewportRight);
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
