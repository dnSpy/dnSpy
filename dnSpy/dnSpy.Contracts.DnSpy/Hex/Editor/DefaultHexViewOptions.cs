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

using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Default <see cref="HexView"/> options
	/// </summary>
	public static class DefaultHexViewOptions {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public const string ShowOffsetColumnName = "HexView/ShowOffsetColumn";
		public static readonly EditorOptionKey<bool> ShowOffsetColumnId = new EditorOptionKey<bool>(ShowOffsetColumnName);
		public const string ShowValuesColumnName = "HexView/ShowValuesColumn";
		public static readonly EditorOptionKey<bool> ShowValuesColumnId = new EditorOptionKey<bool>(ShowValuesColumnName);
		public const string ShowAsciiColumnName = "HexView/ShowAsciiColumn";
		public static readonly EditorOptionKey<bool> ShowAsciiColumnId = new EditorOptionKey<bool>(ShowAsciiColumnName);
		public const string StartPositionName = "HexView/StartPosition";
		public static readonly EditorOptionKey<HexPosition> StartPositionId = new EditorOptionKey<HexPosition>(StartPositionName);
		public const string EndPositionName = "HexView/EndPosition";
		public static readonly EditorOptionKey<HexPosition> EndPositionId = new EditorOptionKey<HexPosition>(EndPositionName);
		public const string BasePositionName = "HexView/BasePosition";
		public static readonly EditorOptionKey<HexPosition> BasePositionId = new EditorOptionKey<HexPosition>(BasePositionName);
		public const string UseRelativePositionsName = "HexView/UseRelativePositions";
		public static readonly EditorOptionKey<bool> UseRelativePositionsId = new EditorOptionKey<bool>(UseRelativePositionsName);
		public const string OffsetBitSizeName = "HexView/OffsetBitSize";
		public static readonly EditorOptionKey<int> OffsetBitSizeId = new EditorOptionKey<int>(OffsetBitSizeName);
		public const string HexValuesDisplayFormatName = "HexView/HexValuesDisplayFormat";
		public static readonly EditorOptionKey<HexValuesDisplayFormat> HexValuesDisplayFormatId = new EditorOptionKey<HexValuesDisplayFormat>(HexValuesDisplayFormatName);
		public const string HexOffsetFormatName = "HexView/HexOffsetFormat";
		public static readonly EditorOptionKey<HexOffsetFormat> HexOffsetFormatId = new EditorOptionKey<HexOffsetFormat>(HexOffsetFormatName);
		public const string ValuesLowerCaseHexName = "HexView/ValuesLowerCaseHex";
		public static readonly EditorOptionKey<bool> ValuesLowerCaseHexId = new EditorOptionKey<bool>(ValuesLowerCaseHexName);
		public const string OffsetLowerCaseHexName = "HexView/OffsetLowerCaseHex";
		public static readonly EditorOptionKey<bool> OffsetLowerCaseHexId = new EditorOptionKey<bool>(OffsetLowerCaseHexName);
		public const string BytesPerLineName = "HexView/BytesPerLine";
		public static readonly EditorOptionKey<int> BytesPerLineId = new EditorOptionKey<int>(BytesPerLineName);
		public const string EnableColorizationName = "HexView/EnableColorization";
		public static readonly EditorOptionKey<bool> EnableColorizationId = new EditorOptionKey<bool>(EnableColorizationName);
		public const string ViewProhibitUserInputName = "HexView/ProhibitUserInput";
		public static readonly EditorOptionKey<bool> ViewProhibitUserInputId = new EditorOptionKey<bool>(ViewProhibitUserInputName);
		public const string RefreshScreenOnChangeName = "HexView/RefreshScreenOnChange";
		public static readonly EditorOptionKey<bool> RefreshScreenOnChangeId = new EditorOptionKey<bool>(RefreshScreenOnChangeName);
		public const string RefreshScreenOnChangeWaitMilliSecondsName = "HexView/RefreshScreenOnChangeWaitMilliSeconds";
		public static readonly EditorOptionKey<int> RefreshScreenOnChangeWaitMilliSecondsId = new EditorOptionKey<int>(RefreshScreenOnChangeWaitMilliSecondsName);
		public const int DefaultRefreshScreenOnChangeWaitMilliSeconds = 150;
		public const string RemoveExtraTextLineVerticalPixelsName = "HexView/RemoveExtraTextLineVerticalPixels";
		public static readonly EditorOptionKey<bool> RemoveExtraTextLineVerticalPixelsId = new EditorOptionKey<bool>(RemoveExtraTextLineVerticalPixelsName);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
