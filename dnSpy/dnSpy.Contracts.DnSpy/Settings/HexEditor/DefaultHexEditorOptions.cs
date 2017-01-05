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

using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Contracts.Settings.HexEditor {
	/// <summary>
	/// Default hex editor options
	/// </summary>
	public static class DefaultHexEditorOptions {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public const HexOffsetFormat HexOffsetFormat = Hex.HexOffsetFormat.Hex;
		public const bool ValuesLowerCaseHex = false;
		public const bool OffsetLowerCaseHex = false;
		public const int GroupSizeInBytes = 0;
		public const bool EnableColorization = true;
		public const bool RemoveExtraTextLineVerticalPixels = false;
		public const bool ShowColumnLines = true;
		public const HexColumnLineKind ColumnLine0 = HexColumnLineKind.Solid;
		public const HexColumnLineKind ColumnLine1 = HexColumnLineKind.Solid;
		public const HexColumnLineKind ColumnGroupLine0 = HexColumnLineKind.Dashed_3_3;
		public const HexColumnLineKind ColumnGroupLine1 = HexColumnLineKind.Dashed_3_3;
		public const bool HighlightActiveColumn = false;
		public const bool HighlightCurrentValue = true;
		public const int HighlightCurrentValueDelayMilliSeconds = DefaultHexViewOptions.DefaultHighlightCurrentValueDelayMilliSeconds;
		public const int EncodingCodePage = 65001;// System.Text.Encoding.UTF8.CodePage
		public const bool HighlightStructureUnderMouseCursor = true;
		public const bool EnableHighlightCurrentLine = true;
		public const bool EnableMouseWheelZoom = true;
		public const double ZoomLevel = 100;
		public const bool HorizontalScrollBar = true;
		public const bool VerticalScrollBar = true;
		public const bool SelectionMargin = true;
		public const bool ZoomControl = true;
		public const bool GlyphMargin = true;
		public const bool ForceClearTypeIfNeeded = true;
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
