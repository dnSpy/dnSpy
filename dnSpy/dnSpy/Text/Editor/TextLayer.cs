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
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Editor {
	sealed class TextLayer : UIElement {
		readonly List<LineInfo> lines;

		struct LineInfo {
			public IFormattedLine Line { get; }
			public Visual Visual { get; }
			public LineInfo(IFormattedLine line) {
				Line = line;
				Visual = line.GetOrCreateVisual();
			}
		}

		public TextLayer(IAdornmentLayer adornmentLayer) {
			if (adornmentLayer == null)
				throw new ArgumentNullException(nameof(adornmentLayer));
			lines = new List<LineInfo>();
			adornmentLayer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, this, null);
		}

		protected override int VisualChildrenCount => lines.Count;
		protected override Visual GetVisualChild(int index) => lines[index].Visual;

		public void AddVisibleLines(List<IWpfTextViewLine> allVisibleLines) {
			var currentLinesHash = new HashSet<IFormattedLine>();
			foreach (var info in lines)
				currentLinesHash.Add(info.Line);
			var newLinesHash = new HashSet<IFormattedLine>(allVisibleLines.Cast<IFormattedLine>());
			foreach (var info in lines) {
				if (!newLinesHash.Contains(info.Line)) {
					RemoveVisualChild(info.Visual);
					if (info.Line.IsValid)
						info.Line.RemoveVisual();
				}
			}
			lines.Clear();
			foreach (IFormattedLine line in allVisibleLines) {
				lines.Add(new LineInfo(line));
				if (!currentLinesHash.Contains(line))
					AddVisualChild(line.GetOrCreateVisual());
			}
		}

		public void Dispose() { }
	}
}
