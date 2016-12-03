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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor.HexGroups {
	sealed class LocalGroupOptions {
		public bool ShowOffsetColumn { get; } = true;
		public bool ShowValuesColumn { get; } = true;
		public bool ShowAsciiColumn { get; } = true;
		public HexPosition StartPosition { get; }
		public HexPosition EndPosition { get; }
		public HexPosition BasePosition { get; } = HexPosition.Zero;
		public bool UseRelativePositions { get; } = false;
		public int OffsetBitSize { get; } = 0;
		public HexValuesDisplayFormat HexValuesDisplayFormat { get; } = HexValuesDisplayFormat.HexByte;
		public int BytesPerLine { get; } = 0;

		public LocalGroupOptions(HexBuffer buffer) {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			StartPosition = buffer.Span.Start;
			EndPosition = buffer.Span.End;
		}

		public void WriteTo(HexView hexView) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			hexView.Options.SetOptionValue(DefaultHexViewOptions.ShowOffsetColumnId, ShowOffsetColumn);
			hexView.Options.SetOptionValue(DefaultHexViewOptions.ShowValuesColumnId, ShowValuesColumn);
			hexView.Options.SetOptionValue(DefaultHexViewOptions.ShowAsciiColumnId, ShowAsciiColumn);
			hexView.Options.SetOptionValue(DefaultHexViewOptions.StartPositionId, StartPosition);
			hexView.Options.SetOptionValue(DefaultHexViewOptions.EndPositionId, EndPosition);
			hexView.Options.SetOptionValue(DefaultHexViewOptions.BasePositionId, BasePosition);
			hexView.Options.SetOptionValue(DefaultHexViewOptions.UseRelativePositionsId, UseRelativePositions);
			hexView.Options.SetOptionValue(DefaultHexViewOptions.OffsetBitSizeId, OffsetBitSize);
			hexView.Options.SetOptionValue(DefaultHexViewOptions.HexValuesDisplayFormatId, HexValuesDisplayFormat);
			hexView.Options.SetOptionValue(DefaultHexViewOptions.BytesPerLineId, BytesPerLine);
		}
	}
}
