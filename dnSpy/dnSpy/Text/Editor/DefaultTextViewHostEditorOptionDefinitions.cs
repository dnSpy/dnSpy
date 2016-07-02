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

namespace dnSpy.Text.Editor {
	[Export(typeof(EditorOptionDefinition))]
	sealed class HorizontalScrollBarEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewHostOptions.HorizontalScrollBarId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class VerticalScrollBarEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewHostOptions.VerticalScrollBarId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class LineNumberMarginEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewHostOptions.LineNumberMarginId;
		public override bool Default => false;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class SelectionMarginEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewHostOptions.SelectionMarginId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class GlyphMarginEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewHostOptions.GlyphMarginId;
		public override bool Default => true;
	}
}
