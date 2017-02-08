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

namespace dnSpy.Contracts.Hex.Editor.HexGroups {
	/// <summary>
	/// Local group options
	/// </summary>
	public sealed class LocalGroupOptions : IEquatable<LocalGroupOptions> {
		/// <summary><see cref="DefaultHexViewOptions.ShowOffsetColumnId"/></summary>
		public bool ShowOffsetColumn { get; set; }
		/// <summary><see cref="DefaultHexViewOptions.ShowValuesColumnId"/></summary>
		public bool ShowValuesColumn { get; set; }
		/// <summary><see cref="DefaultHexViewOptions.ShowAsciiColumnId"/></summary>
		public bool ShowAsciiColumn { get; set; }
		/// <summary><see cref="DefaultHexViewOptions.StartPositionId"/></summary>
		public HexPosition StartPosition { get; set; }
		/// <summary><see cref="DefaultHexViewOptions.EndPositionId"/></summary>
		public HexPosition EndPosition { get; set; }
		/// <summary><see cref="DefaultHexViewOptions.BasePositionId"/></summary>
		public HexPosition BasePosition { get; set; }
		/// <summary><see cref="DefaultHexViewOptions.UseRelativePositionsId"/></summary>
		public bool UseRelativePositions { get; set; }
		/// <summary><see cref="DefaultHexViewOptions.OffsetBitSizeId"/></summary>
		public int OffsetBitSize { get; set; }
		/// <summary><see cref="DefaultHexViewOptions.HexValuesDisplayFormatId"/></summary>
		public HexValuesDisplayFormat HexValuesDisplayFormat { get; set; }
		/// <summary><see cref="DefaultHexViewOptions.BytesPerLineId"/></summary>
		public int BytesPerLine { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public LocalGroupOptions() { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="hexView">Hex view</param>
		public LocalGroupOptions(HexView hexView) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			InitializeFrom(hexView);
		}

		/// <summary>
		/// Initializes this instance
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <returns></returns>
		public LocalGroupOptions InitializeFrom(HexView hexView) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			ShowOffsetColumn = hexView.Options.GetOptionValue(DefaultHexViewOptions.ShowOffsetColumnId);
			ShowValuesColumn = hexView.Options.GetOptionValue(DefaultHexViewOptions.ShowValuesColumnId);
			ShowAsciiColumn = hexView.Options.GetOptionValue(DefaultHexViewOptions.ShowAsciiColumnId);
			StartPosition = hexView.Options.GetOptionValue(DefaultHexViewOptions.StartPositionId);
			EndPosition = hexView.Options.GetOptionValue(DefaultHexViewOptions.EndPositionId);
			BasePosition = hexView.Options.GetOptionValue(DefaultHexViewOptions.BasePositionId);
			UseRelativePositions = hexView.Options.GetOptionValue(DefaultHexViewOptions.UseRelativePositionsId);
			OffsetBitSize = hexView.Options.GetOptionValue(DefaultHexViewOptions.OffsetBitSizeId);
			HexValuesDisplayFormat = hexView.Options.GetOptionValue(DefaultHexViewOptions.HexValuesDisplayFormatId);
			BytesPerLine = hexView.Options.GetOptionValue(DefaultHexViewOptions.BytesPerLineId);
			return this;
		}

		/// <summary>
		/// Writes all options to <paramref name="hexView"/>
		/// </summary>
		/// <param name="hexView">Hex view</param>
		public LocalGroupOptions WriteTo(HexView hexView) {
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
			return this;
		}

		/// <summary>
		/// Copies this instance to <paramref name="destination"/> and returns <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">Destination</param>
		/// <returns></returns>
		public LocalGroupOptions CopyTo(LocalGroupOptions destination) {
			destination.ShowOffsetColumn = ShowOffsetColumn;
			destination.ShowValuesColumn = ShowValuesColumn;
			destination.ShowAsciiColumn = ShowAsciiColumn;
			destination.StartPosition = StartPosition;
			destination.EndPosition = EndPosition;
			destination.BasePosition = BasePosition;
			destination.UseRelativePositions = UseRelativePositions;
			destination.OffsetBitSize = OffsetBitSize;
			destination.HexValuesDisplayFormat = HexValuesDisplayFormat;
			destination.BytesPerLine = BytesPerLine;
			return destination;
		}

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public LocalGroupOptions Clone() => CopyTo(new LocalGroupOptions());

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(LocalGroupOptions other) =>
			other != null &&
			ShowOffsetColumn == other.ShowOffsetColumn &&
			ShowValuesColumn == other.ShowValuesColumn &&
			ShowAsciiColumn == other.ShowAsciiColumn &&
			StartPosition == other.StartPosition &&
			EndPosition == other.EndPosition &&
			BasePosition == other.BasePosition &&
			UseRelativePositions == other.UseRelativePositions &&
			OffsetBitSize == other.OffsetBitSize &&
			HexValuesDisplayFormat == other.HexValuesDisplayFormat &&
			BytesPerLine == other.BytesPerLine;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is LocalGroupOptions && Equals((LocalGroupOptions)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() =>
			(ShowOffsetColumn ? int.MinValue : 0) ^
			(ShowValuesColumn ? 0x40000000 : 0) ^
			(ShowAsciiColumn ? 0x20000000 : 0) ^
			StartPosition.GetHashCode() ^
			EndPosition.GetHashCode() ^
			BasePosition.GetHashCode() ^
			(UseRelativePositions ? 0x10000000 : 0) ^
			OffsetBitSize ^
			(int)HexValuesDisplayFormat ^
			BytesPerLine;
	}
}
