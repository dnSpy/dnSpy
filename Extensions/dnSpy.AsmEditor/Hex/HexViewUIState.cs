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

using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.AsmEditor.Hex {
	sealed class HexViewUIState {
		public HexViewUIState() { }

		public HexViewUIState(HexView hexView) {
			var pos = hexView.Caret.Position.Position;
			ActiveColumn = pos.ActiveColumn;
			ValuesPosition = pos.ValuePosition.BufferPosition.Position;
			ValuesCellPosition = pos.ValuePosition.CellPosition;
			AsciiPosition = pos.AsciiPosition.BufferPosition.Position;
			ViewportLeft = hexView.ViewportLeft;
			var line = hexView.HexViewLines.FirstVisibleLine;
			TopLinePosition = line.BufferStart.Position;
			TopLineVerticalDistance = line.Top - hexView.ViewportTop;
			AnchorPoint = hexView.Selection.AnchorPoint;
			ActivePoint = hexView.Selection.ActivePoint;
		}

		public bool ShowOffsetColumn { get; set; }
		public bool ShowValuesColumn { get; set; }
		public bool ShowAsciiColumn { get; set; }
		public HexPosition StartPosition { get; set; }
		public HexPosition EndPosition { get; set; }
		public HexPosition BasePosition { get; set; }
		public bool UseRelativePositions { get; set; }
		public int OffsetBitSize { get; set; }
		public HexValuesDisplayFormat HexValuesDisplayFormat { get; set; }
		public int BytesPerLine { get; set; }

		public HexColumnType ActiveColumn { get; set; }
		public HexPosition ValuesPosition { get; set; }
		public int ValuesCellPosition { get; set; }
		public HexPosition AsciiPosition { get; set; }
		public double ViewportLeft { get; set; }
		public HexPosition TopLinePosition { get; set; }
		public double TopLineVerticalDistance { get; set; }
		public HexPosition AnchorPoint { get; set; }
		public HexPosition ActivePoint { get; set; }
	}
}
