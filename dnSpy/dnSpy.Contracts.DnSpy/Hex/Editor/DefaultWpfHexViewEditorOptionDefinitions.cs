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
using dnSpy.Contracts.Hex.Classification;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Hex.Editor {
	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ForceClearTypeIfNeededEditorOptionDefinition : WpfHexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultWpfHexViewOptions.ForceClearTypeIfNeededId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class AppearanceCategoryEditorOptionDefinition : WpfHexViewOptionDefinition<string> {
		public override EditorOptionKey<string> Key => DefaultWpfHexViewOptions.AppearanceCategoryId;
		public override string Default => HexAppearanceCategoryConstants.HexEditor;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class EnableHighlightCurrentLineEditorOptionDefinition : WpfHexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultWpfHexViewOptions.EnableHighlightCurrentLineId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class EnableMouseWheelZoomEditorOptionDefinition : WpfHexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultWpfHexViewOptions.EnableMouseWheelZoomId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class EnableSimpleGraphicsEditorOptionDefinition : WpfHexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultWpfHexViewOptions.EnableSimpleGraphicsId;
		public override bool Default => false;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class UseReducedOpacityForHighContrastOptionEditorOptionDefinition : WpfHexViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultWpfHexViewOptions.UseReducedOpacityForHighContrastOptionId;
		public override bool Default => true;
	}

	[Export(typeof(HexEditorOptionDefinition))]
	sealed class ZoomLevelEditorOptionDefinition : WpfHexViewOptionDefinition<double> {
		public override EditorOptionKey<double> Key => DefaultWpfHexViewOptions.ZoomLevelId;
		public override double Default => 100;
	}
}
