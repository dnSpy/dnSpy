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

using System.ComponentModel.Composition;
using System.Text;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ShowOffsetColumnEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.ShowOffsetColumnId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ShowValuesColumnEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.ShowValuesColumnId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ShowAsciiColumnEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.ShowAsciiColumnId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class StartPositionEditorOptionDefinition : HexViewOptionDefinition<HexPosition> {
		public override VSTE.EditorOptionKey<HexPosition> Key => DefaultHexViewOptions.StartPositionId;
		public override HexPosition Default => 0;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class EndPositionEditorOptionDefinition : HexViewOptionDefinition<HexPosition> {
		public override VSTE.EditorOptionKey<HexPosition> Key => DefaultHexViewOptions.EndPositionId;
		public override HexPosition Default => HexPosition.MaxEndPosition;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class BasePositionEditorOptionDefinition : HexViewOptionDefinition<HexPosition> {
		public override VSTE.EditorOptionKey<HexPosition> Key => DefaultHexViewOptions.BasePositionId;
		public override HexPosition Default => 0;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class UseRelativePositionsEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.UseRelativePositionsId;
		public override bool Default => false;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class OffsetBitSizeEditorOptionDefinition : HexViewOptionDefinition<int> {
		public override VSTE.EditorOptionKey<int> Key => DefaultHexViewOptions.OffsetBitSizeId;
		public override int Default => 0;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class HexValuesDisplayFormatEditorOptionDefinition : HexViewOptionDefinition<HexValuesDisplayFormat> {
		public override VSTE.EditorOptionKey<HexValuesDisplayFormat> Key => DefaultHexViewOptions.HexValuesDisplayFormatId;
		public override HexValuesDisplayFormat Default => HexValuesDisplayFormat.HexByte;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class HexOffsetFormatEditorOptionDefinition : HexViewOptionDefinition<HexOffsetFormat> {
		public override VSTE.EditorOptionKey<HexOffsetFormat> Key => DefaultHexViewOptions.HexOffsetFormatId;
		public override HexOffsetFormat Default => HexOffsetFormat.Hex;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ValuesLowerCaseHexEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.ValuesLowerCaseHexId;
		public override bool Default => false;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class OffsetLowerCaseHexEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.OffsetLowerCaseHexId;
		public override bool Default => false;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class BytesPerLineEditorOptionDefinition : HexViewOptionDefinition<int> {
		public override VSTE.EditorOptionKey<int> Key => DefaultHexViewOptions.BytesPerLineId;
		public override int Default => 0;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class GroupSizeInBytesEditorOptionDefinition : HexViewOptionDefinition<int> {
		public override VSTE.EditorOptionKey<int> Key => DefaultHexViewOptions.GroupSizeInBytesId;
		public override int Default => 0;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class EnableColorizationEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.EnableColorizationId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ViewProhibitUserInputEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.ViewProhibitUserInputId;
		public override bool Default => false;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class RefreshScreenOnChangeEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.RefreshScreenOnChangeId;
		public override bool Default => false;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class RefreshScreenOnChangeWaitMilliSecondsEditorOptionDefinition : HexViewOptionDefinition<int> {
		public override VSTE.EditorOptionKey<int> Key => DefaultHexViewOptions.RefreshScreenOnChangeWaitMilliSecondsId;
		public override int Default => DefaultHexViewOptions.DefaultRefreshScreenOnChangeWaitMilliSeconds;
		public override bool IsValid(ref int proposedValue) => proposedValue >= 0;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class RemoveExtraTextLineVerticalPixelsEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.RemoveExtraTextLineVerticalPixelsId;
		public override bool Default => false;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ShowColumnLinesEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.ShowColumnLinesId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ColumnLine0EditorOptionDefinition : HexViewOptionDefinition<HexColumnLineKind> {
		public override VSTE.EditorOptionKey<HexColumnLineKind> Key => DefaultHexViewOptions.ColumnLine0Id;
		public override HexColumnLineKind Default => HexColumnLineKind.Solid;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ColumnLine1EditorOptionDefinition : HexViewOptionDefinition<HexColumnLineKind> {
		public override VSTE.EditorOptionKey<HexColumnLineKind> Key => DefaultHexViewOptions.ColumnLine1Id;
		public override HexColumnLineKind Default => HexColumnLineKind.Solid;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ColumnGroupLine0EditorOptionDefinition : HexViewOptionDefinition<HexColumnLineKind> {
		public override VSTE.EditorOptionKey<HexColumnLineKind> Key => DefaultHexViewOptions.ColumnGroupLine0Id;
		public override HexColumnLineKind Default => HexColumnLineKind.Dashed_3_3;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ColumnGroupLine1EditorOptionDefinition : HexViewOptionDefinition<HexColumnLineKind> {
		public override VSTE.EditorOptionKey<HexColumnLineKind> Key => DefaultHexViewOptions.ColumnGroupLine1Id;
		public override HexColumnLineKind Default => HexColumnLineKind.Dashed_3_3;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class HighlightActiveColumnEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.HighlightActiveColumnId;
		public override bool Default => false;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class HighlightCurrentValueEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override VSTE.EditorOptionKey<bool> Key => DefaultHexViewOptions.HighlightCurrentValueId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class EncodingCodePageEditorOptionDefinition : HexViewOptionDefinition<int> {
		public override VSTE.EditorOptionKey<int> Key => DefaultHexViewOptions.EncodingCodePageId;
		public override int Default => Encoding.UTF8.CodePage;
	}
}
