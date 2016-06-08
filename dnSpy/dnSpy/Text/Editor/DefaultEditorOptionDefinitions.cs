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

using System;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	[Export(typeof(EditorOptionDefinition))]
	sealed class TabSizeEditorOptionDefinition : EditorOptionDefinition<int> {
		public override EditorOptionKey<int> Key => DefaultOptions.TabSizeOptionId;
		public override int Default => 4;
		public override bool IsValid(ref int proposedValue) => 0 < proposedValue && proposedValue < 100;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class IndentSizeEditorOptionDefinition : EditorOptionDefinition<int> {
		public override EditorOptionKey<int> Key => DefaultOptions.IndentSizeOptionId;
		public override int Default => 4;
		public override bool IsValid(ref int proposedValue) => 0 < proposedValue && proposedValue < 100;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class IndentStyleEditorOptionDefinition : EditorOptionDefinition<IndentStyle> {
		public override EditorOptionKey<IndentStyle> Key => DefaultOptions.IndentStyleOptionId;
		public override IndentStyle Default => IndentStyle.Smart;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class NewLineCharacterEditorOptionDefinition : EditorOptionDefinition<string> {
		public override EditorOptionKey<string> Key => DefaultOptions.NewLineCharacterOptionId;
		public override string Default => Environment.NewLine;
		public override bool IsValid(ref string proposedValue) => proposedValue != null && 0 < proposedValue.Length && proposedValue.Length < 10;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class ReplicateNewLineCharacterEditorOptionDefinition : EditorOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultOptions.ReplicateNewLineCharacterOptionId;
		public override bool Default => true;
	}

	[Export(typeof(EditorOptionDefinition))]
	sealed class ConvertTabsToSpacesEditorOptionDefinition : EditorOptionDefinition<bool> {
		public override EditorOptionKey<bool> Key => DefaultOptions.ConvertTabsToSpacesOptionId;
		public override bool Default => true;
	}
}
