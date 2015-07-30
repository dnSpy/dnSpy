/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Windows.Controls;
using System.Windows.Media;

namespace dnSpy.HexEditor {
	sealed class SelectionLayer : Control, IHexLayer {
		public static readonly double DEFAULT_ORDER = 1000;

		readonly HexBox hexBox;

		public double Order {
			get { return DEFAULT_ORDER; }
		}

		public SelectionLayer(HexBox hexBox) {
			this.hexBox = hexBox;
		}

		void Redraw() {
			InvalidateVisual();
		}

		public void SelectionChanged() {
			Redraw();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			base.OnPropertyChanged(e);
			if (e.Property == BackgroundProperty)
				Redraw();
		}

		protected override void OnRender(DrawingContext drawingContext) {
			base.OnRender(drawingContext);
			if (hexBox.Selection == null || hexBox.VisibleBytesPerLine < 1)
				return;

			ulong selStart = hexBox.Selection.Value.StartOffset;
			ulong selEnd = hexBox.Selection.Value.EndOffset;
			int lines = hexBox.VisibleLinesPerPage;
			ulong bpl = (ulong)hexBox.VisibleBytesPerLine;
			ulong visibleStart = hexBox.TopOffset;
			ulong visibleEnd = NumberUtils.AddUInt64(NumberUtils.AddUInt64(visibleStart, NumberUtils.MulUInt64(bpl, NumberUtils.SubUInt64((ulong)lines, 1))), NumberUtils.SubUInt64(bpl, 1));
			if (selStart > visibleEnd || selEnd < visibleStart)
				return;

			ulong offset = Math.Max(selStart, visibleStart);
			ulong endOffset = Math.Min(selEnd, visibleEnd);
			double x = -hexBox.CharacterWidth * hexBox.LeftColumn;
			double y = (offset - hexBox.TopOffset) / bpl * hexBox.CharacterHeight;
			var path = new PathGeometry();
			double hexByteX = hexBox.GetHexByteColumnIndex() * hexBox.CharacterWidth;
			double asciiX = hexBox.GetAsciiColumnIndex() * hexBox.CharacterWidth;
			while (offset <= endOffset) {
				ulong byteIndex = hexBox.GetLineByteIndex(offset);
				ulong count = Math.Min(bpl - byteIndex, endOffset - offset + 1);

				double dx = byteIndex * hexBox.CharacterWidth;
				var rectGeo = new RectangleGeometry(new Rect(x + dx * 3 + hexByteX + hexBox.CharacterWidth, y, count * hexBox.CharacterWidth * 3 - hexBox.CharacterWidth, hexBox.CharacterHeight));
				rectGeo.Freeze();
				path.AddGeometry(rectGeo);

				if (hexBox.PrintAscii) {
					rectGeo = new RectangleGeometry(new Rect(x + dx + asciiX, y, count * hexBox.CharacterWidth, hexBox.CharacterHeight));
					rectGeo.Freeze();
					path.AddGeometry(rectGeo);
				}

				if (offset + bpl - byteIndex < offset)
					break;
				offset += bpl - byteIndex;
				y += hexBox.CharacterHeight;
			}

			path.Freeze();
			drawingContext.DrawGeometry(Background, null, path);
		}
	}
}
