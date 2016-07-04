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
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Text.Editor {
	interface IGlobalEditorOptions {
		void Initialize(IEditorOptions options);
	}

	[Export(typeof(IGlobalEditorOptions))]
	sealed class GlobalEditorOptions : IGlobalEditorOptions {
		readonly TextEditorSettingsImpl textEditorSettings;
		IEditorOptions globalOptions;

		[ImportingConstructor]
		GlobalEditorOptions(TextEditorSettingsImpl textEditorSettings) {
			this.textEditorSettings = textEditorSettings;
		}

		public void Initialize(IEditorOptions globalOptions) {
			if (this.globalOptions != null)
				throw new InvalidOperationException();
			this.globalOptions = globalOptions;
			globalOptions.OptionChanged += EditorOptions_OptionChanged;
			globalOptions.SetOptionValue(DefaultWpfViewOptions.EnableHighlightCurrentLineId, textEditorSettings.HighlightCurrentLine);
			globalOptions.SetOptionValue(DefaultDnSpyWpfViewOptions.ForceClearTypeIfNeededId, textEditorSettings.ForceClearTypeIfNeeded);
			globalOptions.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, textEditorSettings.ShowLineNumbers);
			globalOptions.SetOptionValue(DefaultTextViewOptions.WordWrapStyleId, textEditorSettings.WordWrapStyle);
			globalOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, textEditorSettings.ConvertTabsToSpaces);
		}

		void EditorOptions_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultWpfViewOptions.EnableHighlightCurrentLineId.Name)
				textEditorSettings.HighlightCurrentLine = globalOptions.IsHighlightCurrentLineEnabled();
			else if (e.OptionId == DefaultDnSpyWpfViewOptions.ForceClearTypeIfNeededId.Name)
				textEditorSettings.ForceClearTypeIfNeeded = globalOptions.IsForceClearTypeIfNeededEnabled();
			else if (e.OptionId == DefaultTextViewHostOptions.LineNumberMarginId.Name)
				textEditorSettings.ShowLineNumbers = globalOptions.IsLineNumberMarginEnabled();
			else if (e.OptionId == DefaultTextViewOptions.WordWrapStyleId.Name)
				textEditorSettings.WordWrapStyle = globalOptions.WordWrapStyle();
			else if (e.OptionId == DefaultOptions.ConvertTabsToSpacesOptionId.Name)
				textEditorSettings.ConvertTabsToSpaces = globalOptions.IsConvertTabsToSpacesEnabled();
		}
	}
}
