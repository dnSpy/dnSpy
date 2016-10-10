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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	[Export(typeof(EditorOptionDefinition))]
	sealed class CanChangeOverwriteModeEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.CanChangeOverwriteModeId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class CanChangeUseVisibleWhitespaceEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.CanChangeUseVisibleWhitespaceId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class CanChangeWordWrapStyleEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.CanChangeWordWrapStyleId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class AllowBoxSelectionEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.AllowBoxSelectionId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class RefreshScreenOnChangeEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.RefreshScreenOnChangeId;
		public override bool Default => false;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class RefreshScreenOnChangeWaitMilliSecondsEditorOptionDefinition : ViewOptionDefinition<int> {
		public override EditorOptionKey<int> Key => DefaultDsTextViewOptions.RefreshScreenOnChangeWaitMilliSecondsId;
		public override int Default => DefaultDsTextViewOptions.DefaultRefreshScreenOnChangeWaitMilliSeconds;
		public override bool IsValid(ref int proposedValue) => proposedValue >= 0;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class EnableColorizationEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.EnableColorizationId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class ReferenceHighlightingEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.ReferenceHighlightingId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class BraceMatchingEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.BraceMatchingId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class LineSeparatorEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.LineSeparatorsId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class HighlightRelatedKeywordsEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.HighlightRelatedKeywordsId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class CompressEmptyOrWhitespaceLinesEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.CompressEmptyOrWhitespaceLinesId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class CompressNonLetterLinesEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.CompressNonLetterLinesId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class ShowStructureLinesEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.ShowStructureLinesId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class RemoveExtraTextLineVerticalPixelsEditorOptionDefinition : ViewOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultDsTextViewOptions.RemoveExtraTextLineVerticalPixelsId;
		public override bool Default => false;
	}
}
