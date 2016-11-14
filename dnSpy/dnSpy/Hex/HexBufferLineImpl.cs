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
using dnSpy.Contracts.Hex;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Hex {
	sealed class HexBufferLineImpl : HexBufferLine {
		public override HexBufferLineProvider LineProvider { get; }
		public override HexPosition LineNumber { get; }
		public override ReadOnlyCollection<HexColumnType> ColumnOrder { get; }
		public override HexBufferSpan LineSpan { get; }
		public override HexBufferSpan VisibleBytesSpan { get; }
		public override HexBytes VisibleHexBytes { get; }
		public override string Text { get; }
		public override bool IsOffsetColumnPresent { get; }
		public override bool IsValuesColumnPresent { get; }
		public override bool IsAsciiColumnPresent { get; }
		public override HexPosition LogicalOffset { get; }
		public override HexCellInformationCollection ValueCells { get; }
		public override HexCellInformationCollection AsciiCells { get; }

		readonly Span offsetSpan;
		readonly Span fullValuesSpan;
		readonly Span visibleValuesSpan;
		readonly Span fullAsciiSpan;
		readonly Span visibleAsciiSpan;

		public HexBufferLineImpl(HexBufferLineProvider hexBufferLineProvider, HexPosition lineNumber, ReadOnlyCollection<HexColumnType> columnOrder, HexBufferSpan lineSpan, HexBufferSpan visibleBytesSpan, HexBytes visibleHexBytes, string text, bool isOffsetColumnPresent, bool isValuesColumnPresent, bool isAsciiColumnPresent, HexPosition logicalOffset, HexCellInformationCollection valueCells, HexCellInformationCollection asciiCells, Span offsetSpan, Span fullValuesSpan, Span visibleValuesSpan, Span fullAsciiSpan, Span visibleAsciiSpan) {
			if (hexBufferLineProvider == null)
				throw new ArgumentNullException(nameof(hexBufferLineProvider));
			if (columnOrder == null)
				throw new ArgumentNullException(nameof(columnOrder));
			if (lineSpan.IsDefault)
				throw new ArgumentException();
			if (visibleBytesSpan.IsDefault)
				throw new ArgumentException();
			if (visibleHexBytes.IsDefault)
				throw new ArgumentException();
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (valueCells.IsDefault)
				throw new ArgumentNullException(nameof(valueCells));
			if (asciiCells.IsDefault)
				throw new ArgumentNullException(nameof(asciiCells));
			LineProvider = hexBufferLineProvider;
			LineNumber = lineNumber;
			ColumnOrder = columnOrder;
			LineSpan = lineSpan;
			VisibleBytesSpan = visibleBytesSpan;
			VisibleHexBytes = visibleHexBytes;
			Text = text;
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

		public override Span GetOffsetSpan() => offsetSpan;
		public override Span GetValuesSpan(bool onlyVisibleCells) => onlyVisibleCells ? visibleValuesSpan : fullValuesSpan;
		public override Span GetAsciiSpan(bool onlyVisibleCells) => onlyVisibleCells ? visibleAsciiSpan : fullAsciiSpan;
	}
}
