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
using System.Linq;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Editor {
	sealed class TextLayer : UIElement {
		readonly List<LineInfo> lines;

		readonly struct LineInfo {
			public HexFormattedLine Line { get; }
			public Visual Visual { get; }
			public LineInfo(HexFormattedLine line) {
				Line = line;
				Visual = line.GetOrCreateVisual();
			}
		}

		public TextLayer(HexAdornmentLayer adornmentLayer) {
			if (adornmentLayer is null)
				throw new ArgumentNullException(nameof(adornmentLayer));
			lines = new List<LineInfo>();
			adornmentLayer.AddAdornment(VSTE.AdornmentPositioningBehavior.OwnerControlled, (HexBufferSpan?)null, null, this, null);
		}

		protected override int VisualChildrenCount => lines.Count;
		protected override Visual GetVisualChild(int index) => lines[index].Visual;

		public void AddVisibleLines(List<WpfHexViewLine> allVisibleLines) {
			var currentLinesHash = new HashSet<HexFormattedLine>();
			foreach (var info in lines)
				currentLinesHash.Add(info.Line);
			var newLinesHash = new HashSet<HexFormattedLine>(allVisibleLines.Cast<HexFormattedLine>());
			foreach (var info in lines) {
				if (!newLinesHash.Contains(info.Line)) {
					RemoveVisualChild(info.Visual);
					if (info.Line.IsValid)
						info.Line.RemoveVisual();
				}
			}
			lines.Clear();
			foreach (HexFormattedLine line in allVisibleLines) {
				lines.Add(new LineInfo(line));
				if (!currentLinesHash.Contains(line))
					AddVisualChild(line.GetOrCreateVisual());
			}
		}

		public void Dispose() { }
	}
}
