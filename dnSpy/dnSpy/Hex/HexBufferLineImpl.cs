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
using System.Collections.ObjectModel;
using dnSpy.Contracts.Hex;
using VST = Microsoft.VisualStudio.Text;

namespace dnSpy.Hex {
	sealed class HexBufferLineImpl : HexBufferLine {
		public override HexBufferLineFormatter LineProvider { get; }
		public override HexPosition LineNumber { get; }
		public override ReadOnlyCollection<HexColumnType> ColumnOrder { get; }
		public override HexBufferSpan BufferSpan { get; }
		public override HexBytes HexBytes { get; }
		public override string Text { get; }
		public override bool IsOffsetColumnPresent { get; }
		public override bool IsValuesColumnPresent { get; }
		public override bool IsAsciiColumnPresent { get; }
		public override HexPosition LogicalOffset { get; }
		public override HexCellCollection ValueCells { get; }
		public override HexCellCollection AsciiCells { get; }

		readonly VST.Span offsetSpan;
		readonly VST.Span fullValuesSpan;
		readonly VST.Span visibleValuesSpan;
		readonly VST.Span fullAsciiSpan;
		readonly VST.Span visibleAsciiSpan;

		public HexBufferLineImpl(HexBufferLineFormatter hexBufferLineFormatter, HexPosition lineNumber, ReadOnlyCollection<HexColumnType> columnOrder, HexBufferSpan bufferSpan, HexBytes hexBytes, string text, bool isOffsetColumnPresent, bool isValuesColumnPresent, bool isAsciiColumnPresent, HexPosition logicalOffset, HexCellCollection valueCells, HexCellCollection asciiCells, VST.Span offsetSpan, VST.Span fullValuesSpan, VST.Span visibleValuesSpan, VST.Span fullAsciiSpan, VST.Span visibleAsciiSpan) {
			if (bufferSpan.IsDefault)
				throw new ArgumentException();
			if (hexBytes.IsDefault)
				throw new ArgumentException();
			if (valueCells.IsDefault)
				throw new ArgumentNullException(nameof(valueCells));
			if (asciiCells.IsDefault)
				throw new ArgumentNullException(nameof(asciiCells));
			LineProvider = hexBufferLineFormatter ?? throw new ArgumentNullException(nameof(hexBufferLineFormatter));
			LineNumber = lineNumber;
			ColumnOrder = columnOrder ?? throw new ArgumentNullException(nameof(columnOrder));
			BufferSpan = bufferSpan;
			HexBytes = hexBytes;
			Text = text ?? throw new ArgumentNullException(nameof(text));
			IsOffsetColumnPresent = isOffsetColumnPresent;
			IsValuesColumnPresent = isValuesColumnPresent;
			IsAsciiColumnPresent = isAsciiColumnPresent;
			LogicalOffset = logicalOffset;
			ValueCells = valueCells;
			AsciiCells = asciiCells;
			this.offsetSpan = offsetSpan;
			this.fullValuesSpan = fullValuesSpan;
			this.visibleValuesSpan = visibleValuesSpan;
			this.fullAsciiSpan = fullAsciiSpan;
			this.visibleAsciiSpan = visibleAsciiSpan;
		}

		public override VST.Span GetOffsetSpan() => offsetSpan;
		public override VST.Span GetValuesSpan(bool onlyVisibleCells) => onlyVisibleCells ? visibleValuesSpan : fullValuesSpan;
		public override VST.Span GetAsciiSpan(bool onlyVisibleCells) => onlyVisibleCells ? visibleAsciiSpan : fullAsciiSpan;
	}
}
