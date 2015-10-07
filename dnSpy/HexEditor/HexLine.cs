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
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace dnSpy.HexEditor {
	[DebuggerDisplay("{Text}")]
	sealed class HexLine : IDisposable {
		public readonly HexLinePart[] LineParts;
		public readonly string Text;
		public readonly ulong StartOffset;
		public readonly ulong EndOffset;
		public double Height;
		public double Width;

		public TextLine[] TextLines {
			get { return textLines; }
			set {
				textLines = value;
				Height = 0;
				Width = 0;
				foreach (var line in value) {
					Height += line.Height;
					Width = Math.Max(Width, line.Width);
				}
			}
		}
		TextLine[] textLines;

		public int Length {
			get {
				if (LineParts.Length == 0)
					return 0;
				var last = LineParts[LineParts.Length - 1];
				return last.Offset + last.Length;
			}
		}

		public HexLine(ulong offset, ulong end, string text, HexLinePart[] parts) {
			this.StartOffset = offset;
			this.EndOffset = end;
			this.Text = text;
			this.LineParts = parts;
		}

		public DrawingVisual GetOrCreateDrawingVisual() {
			if (drawingVisual == null) {
				drawingVisual = new DrawingVisual();
				double y = 0;
				var dc = drawingVisual.RenderOpen();
				foreach (var line in textLines) {
					line.Draw(dc, new Point(0, y), InvertAxes.None);
					y += line.Height;
				}
				dc.Close();
			}
			return drawingVisual;
		}
		DrawingVisual drawingVisual;

		public void Dispose() {
			foreach (var textLine in textLines)
				textLine.Dispose();
			textLines = null;
		}
	}
}
