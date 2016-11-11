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
	sealed class HorizontalScrollBarEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultHexViewHostOptions.HorizontalScrollBarId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class VerticalScrollBarEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultHexViewHostOptions.VerticalScrollBarId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class SelectionMarginEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultHexViewHostOptions.SelectionMarginId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ZoomControlEditorOptionDefinition : HexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultHexViewHostOptions.ZoomControlId;
		public override bool Default => true;
	}
}
