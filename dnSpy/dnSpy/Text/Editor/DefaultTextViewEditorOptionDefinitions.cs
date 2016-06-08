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
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	[Export(typeof(EditorOptionDefinition))]
	sealed class CutOrCopyBlankLineIfNoSelectionEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class DisplayUrlsAsHyperlinksEditorOptionDefinition : EditorOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.DisplayUrlsAsHyperlinksId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class DragDropEditingEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.DragDropEditingId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class CanChangeOverwriteModeEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.CanChangeOverwriteModeId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class OverwriteModeEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.OverwriteModeId;
		public override bool Default => false;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class UseVirtualSpaceEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.UseVirtualSpaceId;
		public override bool Default => false;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class CanChangeUseVisibleWhitespaceEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.CanChangeUseVisibleWhitespaceId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class UseVisibleWhitespaceEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.UseVisibleWhitespaceId;
		public override bool Default => false;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class ViewProhibitUserInputEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.ViewProhibitUserInputId;
		public override bool Default => false;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class CanChangeWordWrapStyleEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.CanChangeWordWrapStyleId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class WordWrapStyleEditorOptionDefinition : ViewOptionDefinition<WordWrapStyles> {
		public override EditorOptionKey<WordWrapStyles> Key => DefaultTextViewOptions.WordWrapStyleId;
		public override WordWrapStyles Default => WordWrapStyles.None;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class ScrollBelowDocumentEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.ScrollBelowDocumentId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class RectangularSelectionEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.RectangularSelectionId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class HideCaretWhileTypingEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.HideCaretWhileTypingId;
		public override bool Default => false;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class ShowColumnRulerEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultTextViewOptions.ShowColumnRulerId;
		public override bool Default => false;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class ColumnRulerPositionEditorOptionDefinition : ViewOptionDefinition<int> {
		public override EditorOptionKey<int> Key => DefaultTextViewOptions.ColumnRulerPositionId;
		public override int Default => 80;
		public override bool IsValid(ref int proposedValue) => 0 < proposedValue && proposedValue <= 500;
	}
}
