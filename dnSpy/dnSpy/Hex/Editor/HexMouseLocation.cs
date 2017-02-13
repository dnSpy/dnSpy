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
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;

namespace dnSpy.Hex.Editor {
	sealed class HexMouseLocation {
		public HexViewLine HexViewLine { get; }
		public int Position { get; }
		public Point Point { get; }

		HexMouseLocation(HexViewLine hexViewLine, int position, Point point) {
			HexViewLine = hexViewLine ?? throw new ArgumentNullException(nameof(hexViewLine));
			Position = position;
			Point = point;
		}

		static Point GetTextPoint(WpfHexView wpfHexView, MouseEventArgs e) {
			var pos = e.GetPosition(wpfHexView.VisualElement);
			return new Point(wpfHexView.ViewportLeft + pos.X, wpfHexView.ViewportTop + pos.Y);
		}

		public static HexMouseLocation Create(WpfHexView wpfHexView, MouseEventArgs e, bool insertionPosition) {
			HexViewLine hexViewLine;
			int position;

			var point = GetTextPoint(wpfHexView, e);
			var line = wpfHexView.HexViewLines.GetHexViewLineContainingYCoordinate(point.Y);
			if (line != null)
				hexViewLine = line;
			else if (point.Y <= wpfHexView.ViewportTop)
				hexViewLine = wpfHexView.HexViewLines.FirstVisibleLine;
			else
				hexViewLine = wpfHexView.HexViewLines.LastVisibleLine;
			if (insertionPosition)
				position = hexViewLine.GetInsertionLinePositionFromXCoordinate(point.X);
			else
				position = hexViewLine.GetVirtualLinePositionFromXCoordinate(point.X);

			return new HexMouseLocation(hexViewLine, position, point);
		}

		public static HexMouseLocation TryCreateTextOnly(WpfHexView wpfHexView, MouseEventArgs e, bool fullLineHeight) {
			var point = GetTextPoint(wpfHexView, e);
			var line = wpfHexView.HexViewLines.GetHexViewLineContainingYCoordinate(point.Y);
			if (line == null)
				return null;
			if (fullLineHeight) {
				if (!(line.Top <= point.Y && point.Y < line.Bottom))
					return null;
				if (!(line.Left <= point.X && point.X < line.Right))
					return null;
			}
			else {
				if (!(line.TextTop <= point.Y && point.Y < line.TextBottom))
					return null;
				if (!(line.TextLeft <= point.X && point.X < line.TextRight))
					return null;
			}
			var position = line.GetLinePositionFromXCoordinate(point.X, true);
			if (position == null)
				return null;

			return new HexMouseLocation(line, position.Value, point);
		}

		public override string ToString() {
			var line = HexViewLine.BufferLine;
			int col = Position;
			return $"({line.LineNumber + 1},{col + 1}) {Position} {HexViewLine.BufferSpan}";
		}
	}
}
