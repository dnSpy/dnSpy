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

using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Default <see cref="HexView"/> options
	/// </summary>
	public static class DefaultHexViewOptions {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public const string ShowOffsetColumnName = "HexView/ShowOffsetColumn";
		public static readonly VSTE.EditorOptionKey<bool> ShowOffsetColumnId = new VSTE.EditorOptionKey<bool>(ShowOffsetColumnName);
		public const string ShowValuesColumnName = "HexView/ShowValuesColumn";
		public static readonly VSTE.EditorOptionKey<bool> ShowValuesColumnId = new VSTE.EditorOptionKey<bool>(ShowValuesColumnName);
		public const string ShowAsciiColumnName = "HexView/ShowAsciiColumn";
		public static readonly VSTE.EditorOptionKey<bool> ShowAsciiColumnId = new VSTE.EditorOptionKey<bool>(ShowAsciiColumnName);
		public const string StartPositionName = "HexView/StartPosition";
		public static readonly VSTE.EditorOptionKey<HexPosition> StartPositionId = new VSTE.EditorOptionKey<HexPosition>(StartPositionName);
		public const string EndPositionName = "HexView/EndPosition";
		public static readonly VSTE.EditorOptionKey<HexPosition> EndPositionId = new VSTE.EditorOptionKey<HexPosition>(EndPositionName);
		public const string BasePositionName = "HexView/BasePosition";
		public static readonly VSTE.EditorOptionKey<HexPosition> BasePositionId = new VSTE.EditorOptionKey<HexPosition>(BasePositionName);
		public const string UseRelativePositionsName = "HexView/UseRelativePositions";
		public static readonly VSTE.EditorOptionKey<bool> UseRelativePositionsId = new VSTE.EditorOptionKey<bool>(UseRelativePositionsName);
		public const string OffsetBitSizeName = "HexView/OffsetBitSize";
		public static readonly VSTE.EditorOptionKey<int> OffsetBitSizeId = new VSTE.EditorOptionKey<int>(OffsetBitSizeName);
		public const string HexValuesDisplayFormatName = "HexView/HexValuesDisplayFormat";
		public static readonly VSTE.EditorOptionKey<HexValuesDisplayFormat> HexValuesDisplayFormatId = new VSTE.EditorOptionKey<HexValuesDisplayFormat>(HexValuesDisplayFormatName);
		public const string HexOffsetFormatName = "HexView/HexOffsetFormat";
		public static readonly VSTE.EditorOptionKey<HexOffsetFormat> HexOffsetFormatId = new VSTE.EditorOptionKey<HexOffsetFormat>(HexOffsetFormatName);
		public const string ValuesLowerCaseHexName = "HexView/ValuesLowerCaseHex";
		public static readonly VSTE.EditorOptionKey<bool> ValuesLowerCaseHexId = new VSTE.EditorOptionKey<bool>(ValuesLowerCaseHexName);
		public const string OffsetLowerCaseHexName = "HexView/OffsetLowerCaseHex";
		public static readonly VSTE.EditorOptionKey<bool> OffsetLowerCaseHexId = new VSTE.EditorOptionKey<bool>(OffsetLowerCaseHexName);
		public const string BytesPerLineName = "HexView/BytesPerLine";
		public static readonly VSTE.EditorOptionKey<int> BytesPerLineId = new VSTE.EditorOptionKey<int>(BytesPerLineName);
		public const string GroupSizeInBytesName = "HexView/GroupSizeInBytes";
		public static readonly VSTE.EditorOptionKey<int> GroupSizeInBytesId = new VSTE.EditorOptionKey<int>(GroupSizeInBytesName);
		public const string EnableColorizationName = "HexView/EnableColorization";
		public static readonly VSTE.EditorOptionKey<bool> EnableColorizationId = new VSTE.EditorOptionKey<bool>(EnableColorizationName);
		public const string ViewProhibitUserInputName = "HexView/ProhibitUserInput";
		public static readonly VSTE.EditorOptionKey<bool> ViewProhibitUserInputId = new VSTE.EditorOptionKey<bool>(ViewProhibitUserInputName);
		public const string RefreshScreenOnChangeName = "HexView/RefreshScreenOnChange";
		public static readonly VSTE.EditorOptionKey<bool> RefreshScreenOnChangeId = new VSTE.EditorOptionKey<bool>(RefreshScreenOnChangeName);
		public const string RefreshScreenOnChangeWaitMilliSecondsName = "HexView/RefreshScreenOnChangeWaitMilliSeconds";
		public static readonly VSTE.EditorOptionKey<int> RefreshScreenOnChangeWaitMilliSecondsId = new VSTE.EditorOptionKey<int>(RefreshScreenOnChangeWaitMilliSecondsName);
		public const int DefaultRefreshScreenOnChangeWaitMilliSeconds = 150;
		public const string RemoveExtraTextLineVerticalPixelsName = "HexView/RemoveExtraTextLineVerticalPixels";
		public static readonly VSTE.EditorOptionKey<bool> RemoveExtraTextLineVerticalPixelsId = new VSTE.EditorOptionKey<bool>(RemoveExtraTextLineVerticalPixelsName);
		public const string ShowColumnLinesName = "HexView/ShowColumnLines";
		public static readonly VSTE.EditorOptionKey<bool> ShowColumnLinesId = new VSTE.EditorOptionKey<bool>(ShowColumnLinesName);
		public const string ColumnLine0Name = "HexView/ColumnLine0";
		public static readonly VSTE.EditorOptionKey<HexColumnLineKind> ColumnLine0Id = new VSTE.EditorOptionKey<HexColumnLineKind>(ColumnLine0Name);
		public const string ColumnLine1Name = "HexView/ColumnLine1";
		public static readonly VSTE.EditorOptionKey<HexColumnLineKind> ColumnLine1Id = new VSTE.EditorOptionKey<HexColumnLineKind>(ColumnLine1Name);
		public const string ColumnGroupLine0Name = "HexView/ColumnGroupLine0";
		public static readonly VSTE.EditorOptionKey<HexColumnLineKind> ColumnGroupLine0Id = new VSTE.EditorOptionKey<HexColumnLineKind>(ColumnGroupLine0Name);
		public const string ColumnGroupLine1Name = "HexView/ColumnGroupLine1";
		public static readonly VSTE.EditorOptionKey<HexColumnLineKind> ColumnGroupLine1Id = new VSTE.EditorOptionKey<HexColumnLineKind>(ColumnGroupLine1Name);
		public const string HighlightActiveColumnName = "HexView/HighlightActiveColumn";
		public static readonly VSTE.EditorOptionKey<bool> HighlightActiveColumnId = new VSTE.EditorOptionKey<bool>(HighlightActiveColumnName);
		public const string HighlightCurrentValueName = "HexView/HighlightCurrentValue";
		public static readonly VSTE.EditorOptionKey<bool> HighlightCurrentValueId = new VSTE.EditorOptionKey<bool>(HighlightCurrentValueName);
		public const string HighlightCurrentValueDelayMilliSecondsName = "HexView/HighlightCurrentValueDelayMilliSeconds";
		public static readonly VSTE.EditorOptionKey<int> HighlightCurrentValueDelayMilliSecondsId = new VSTE.EditorOptionKey<int>(HighlightCurrentValueDelayMilliSecondsName);
		public const int DefaultHighlightCurrentValueDelayMilliSeconds = 100;
		public const string EncodingCodePageName = "HexView/EncodingCodePage";
		public static readonly VSTE.EditorOptionKey<int> EncodingCodePageId = new VSTE.EditorOptionKey<int>(EncodingCodePageName);
		public const string HighlightStructureUnderMouseCursorName = "HexView/HighlightStructureUnderMouseCursor";
		public static readonly VSTE.EditorOptionKey<bool> HighlightStructureUnderMouseCursorId = new VSTE.EditorOptionKey<bool>(HighlightStructureUnderMouseCursorName);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
