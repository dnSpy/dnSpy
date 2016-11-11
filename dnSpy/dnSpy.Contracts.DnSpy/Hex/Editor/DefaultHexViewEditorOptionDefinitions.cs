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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	[Export(typeof(HexEditorOptionDefinition))]
	sealed class HexBytesDisplayFormatEditorOptionDefinition : HexViewOptionDefinition<HexBytesDisplayFormat> {
		public override EditorOptionKey<HexBytesDisplayFormat> Key => DefaultHexViewOptions.HexBytesDisplayFormatId;
		public override HexBytesDisplayFormat Default => HexBytesDisplayFormat.HexByte;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class HexOffsetFormatEditorOptionDefinition : HexViewOptionDefinition<HexOffsetFormat> {
		public override EditorOptionKey<HexOffsetFormat> Key => DefaultHexViewOptions.HexOffsetFormatId;
		public override HexOffsetFormat Default => HexOffsetFormat.None;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class LowerCaseHexEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultHexViewOptions.LowerCaseHexId;
		public override bool Default => false;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class BytesPerLineEditorOptionDefinition : HexViewOptionDefinition<int> {
		public override EditorOptionKey<int> Key => DefaultHexViewOptions.BytesPerLineId;
		public override int Default => 0;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class EnableColorizationEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultHexViewOptions.EnableColorizationId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ViewProhibitUserInputEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultHexViewOptions.ViewProhibitUserInputId;
		public override bool Default => false;
	}
}
