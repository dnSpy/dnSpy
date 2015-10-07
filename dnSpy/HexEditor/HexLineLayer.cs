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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace dnSpy.HexEditor {
	sealed class HexLineLayer : UIElement, IHexLayer {
		public static readonly double DEFAULT_ORDER = 2000;

		public double Order {
			get { return DEFAULT_ORDER; }
		}

		public Vector LineStart {
			get { return lineStart; }
			set {
				if (lineStart != value) {
					lineStart = value;
					InvalidateArrange();
				}
			}
		}
		Vector lineStart;

		public void Initialize(List<HexLine> newLines) {
			var currentLinesHash = new HashSet<HexLine>(hexLines);
			var newLinesHash = new HashSet<HexLine>(newLines);
			foreach (var line in hexLines) {
				if (!newLines.Contains(line))
					RemoveVisualChild(line.GetOrCreateDrawingVisual());
			}
			hexLines.Clear();
			foreach (var line in newLines) {
				hexLines.Add(line);
				if (!currentLinesHash.Contains(line))
					AddVisualChild(line.GetOrCreateDrawingVisual());
			}

			InvalidateArrange();
		}
		List<HexLine> hexLines = new List<HexLine>();

		protected override int VisualChildrenCount {
			get { return hexLines.Count; }
		}

		protected override Visual GetVisualChild(int index) {
			return hexLines[index].GetOrCreateDrawingVisual();
		}

		protected override void ArrangeCore(Rect finalRect) {
			double x = lineStart.X, y = lineStart.Y;
			foreach (var line in hexLines) {
				var visual = line.GetOrCreateDrawingVisual();
				var t = new TranslateTransform(x, y);
				t.Freeze();
				visual.Transform = t;
				y += line.Height;
			}
		}
	}
}
