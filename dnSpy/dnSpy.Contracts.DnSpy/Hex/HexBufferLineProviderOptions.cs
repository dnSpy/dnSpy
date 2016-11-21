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

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Options passed to <see cref="HexBufferLineProviderFactoryService.Create(HexBuffer, HexBufferLineProviderOptions)"/>
	/// </summary>
	public sealed class HexBufferLineProviderOptions : IEquatable<HexBufferLineProviderOptions> {
		/// <summary>
		/// Number of visible characters per line
		/// </summary>
		public int CharsPerLine { get; set; }

		/// <summary>
		/// Number of bytes per line or 0 to fit to width
		/// </summary>
		public int BytesPerLine { get; set; }

		/// <summary>
		/// Size of a group in bytes or 0 to use the default value
		/// </summary>
		public int GroupSizeInBytes { get; set; }

		/// <summary>
		/// true to show the offset
		/// </summary>
		public bool ShowOffset { get; set; }

		/// <summary>
		/// true to use lower case hex
		/// </summary>
		public bool OffsetLowerCaseHex { get; set; }

		/// <summary>
		/// Offset format
		/// </summary>
		public HexOffsetFormat OffsetFormat { get; set; }

		/// <summary>
		/// First position to show
		/// </summary>
		public HexPosition StartPosition { get; set; }

		/// <summary>
		/// End position
		/// </summary>
		public HexPosition EndPosition { get; set; }

		/// <summary>
		/// Base position. The real position is added to this value which is then
		/// shown in the offset column.
		/// </summary>
		public HexPosition BasePosition { get; set; }

		/// <summary>
		/// true if all visible positions are relative to <see cref="StartPosition"/>
		/// </summary>
		public bool UseRelativePositions { get; set; }

		/// <summary>
		/// true to show the values
		/// </summary>
		public bool ShowValues { get; set; }

		/// <summary>
		/// true to use lower case hex
		/// </summary>
		public bool ValuesLowerCaseHex { get; set; }

		/// <summary>
		/// Number of bits of the offset to show. Must be a multiple of 4. If it's 0, the default
		/// value is used.
		/// </summary>
		public int OffsetBitSize { get; set; }

		/// <summary>
		/// Values format
		/// </summary>
		public HexValuesDisplayFormat ValuesFormat { get; set; }

		/// <summary>
		/// true to show ASCII chars
		/// </summary>
		public bool ShowAscii { get; set; }

		/// <summary>
		/// Column order or null to use the default order. All columns must be present in this
		/// array even if they're not shown.
		/// </summary>
		public HexColumnType[] ColumnOrder { get; set; }

		/// <summary>
		/// Minimum <see cref="OffsetBitSize"/> value
		/// </summary>
		public static readonly int MinOffsetBitSize = 0;

		/// <summary>
		/// Maximum <see cref="OffsetBitSize"/> value
		/// </summary>
		public static readonly int MaxOffsetBitSize = 64;

		/// <summary>
		/// Max bytes per line
		/// </summary>
		public static readonly int MaxBytesPerLine = 1024;

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(HexBufferLineProviderOptions other) =>
			CharsPerLine == other.CharsPerLine &&
			BytesPerLine == other.BytesPerLine &&
			GroupSizeInBytes == other.GroupSizeInBytes &&
			ShowOffset == other.ShowOffset &&
			OffsetLowerCaseHex == other.OffsetLowerCaseHex &&
			OffsetFormat == other.OffsetFormat &&
			StartPosition == other.StartPosition &&
			EndPosition == other.EndPosition &&
			BasePosition == other.BasePosition &&
			UseRelativePositions == other.UseRelativePositions &&
			ShowValues == other.ShowValues &&
			ValuesLowerCaseHex == other.ValuesLowerCaseHex &&
			OffsetBitSize == other.OffsetBitSize &&
			ValuesFormat == other.ValuesFormat &&
			ShowAscii == other.ShowAscii &&
			HexColumnTypeArraysEquals(ColumnOrder, other.ColumnOrder);

		static bool HexColumnTypeArraysEquals(HexColumnType[] a, HexColumnType[] b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Other object</param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is HexBufferLineProviderOptions && Equals((HexBufferLineProviderOptions)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() =>
			CharsPerLine.GetHashCode() ^
			BytesPerLine.GetHashCode() ^
			GroupSizeInBytes.GetHashCode() ^
			ShowOffset.GetHashCode() ^
			OffsetLowerCaseHex.GetHashCode() ^
			(int)OffsetFormat ^
			StartPosition.GetHashCode() ^
			EndPosition.GetHashCode() ^
			BasePosition.GetHashCode() ^
			UseRelativePositions.GetHashCode() ^
			ShowValues.GetHashCode() ^
			ValuesLowerCaseHex.GetHashCode() ^
			OffsetBitSize.GetHashCode() ^
			(int)ValuesFormat ^
			ShowAscii.GetHashCode() ^
			GetHexColumnTypeArrayHashCode(ColumnOrder);

		int GetHexColumnTypeArrayHashCode(HexColumnType[] a) {
			if (a == null)
				return 0;
			int hc = 0;
			foreach (var t in a)
				hc ^= (int)t;
			return hc;
		}
	}
}
