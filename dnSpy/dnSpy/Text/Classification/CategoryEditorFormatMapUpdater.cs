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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Themes;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Text.Classification {
	sealed class CategoryEditorFormatMapUpdater {
		readonly IThemeService themeService;
		readonly ITextEditorFontSettings textEditorFontSettings;
		readonly IEditorFormatDefinitionService editorFormatDefinitionService;
		readonly IEditorFormatMap editorFormatMap;

		public CategoryEditorFormatMapUpdater(IThemeService themeService, ITextEditorFontSettings textEditorFontSettings, IEditorFormatDefinitionService editorFormatDefinitionService, IEditorFormatMap editorFormatMap) {
			if (themeService == null)
				throw new ArgumentNullException(nameof(themeService));
			if (textEditorFontSettings == null)
				throw new ArgumentNullException(nameof(textEditorFontSettings));
			if (editorFormatDefinitionService == null)
				throw new ArgumentNullException(nameof(editorFormatDefinitionService));
			if (editorFormatMap == null)
				throw new ArgumentNullException(nameof(editorFormatMap));
			this.themeService = themeService;
			this.textEditorFontSettings = textEditorFontSettings;
			this.editorFormatDefinitionService = editorFormatDefinitionService;
			this.editorFormatMap = editorFormatMap;

			themeService.ThemeChangedHighPriority += ThemeService_ThemeChangedHighPriority;
			textEditorFontSettings.SettingsChanged += TextEditorFontSettings_SettingsChanged;
			InitializeAll();
		}

		void TextEditorFontSettings_SettingsChanged(object sender, EventArgs e) => InitializeAll();
		void ThemeService_ThemeChangedHighPriority(object sender, ThemeChangedEventArgs e) => InitializeAll();

		void InitializeAll() {
			bool callBeginEndUpdate = !editorFormatMap.IsInBatchUpdate;
			if (callBeginEndUpdate)
				editorFormatMap.BeginBatchUpdate();

			var theme = themeService.Theme;
			var textProps = textEditorFontSettings.CreateResourceDictionary(theme);
			var winbg = textProps[EditorFormatMapConstants.TextViewBackgroundId] as Brush ?? SystemColors.WindowBrush;
			var winbgRes = editorFormatMap.GetProperties(EditorFormatMapConstants.TextViewBackgroundId);
			if (winbgRes[EditorFormatDefinition.BackgroundBrushId] != winbg) {
				winbgRes[EditorFormatDefinition.BackgroundBrushId] = winbg;
				editorFormatMap.SetProperties(EditorFormatMapConstants.TextViewBackgroundId, winbgRes);
			}

			foreach (var t in GetEditorFormatDefinitions()) {
				var key = t.Item1.Name;
				var props = t.Item2.CreateThemeResourceDictionary(theme);
				editorFormatMap.SetProperties(key, props);
			}

			var ptDict = CreatePlainTextDictionary(textProps);
			editorFormatMap.SetProperties(EditorFormatMapConstants.PlainText, ptDict);

			if (callBeginEndUpdate)
				editorFormatMap.EndBatchUpdate();
		}

		ResourceDictionary CreatePlainTextDictionary(ResourceDictionary textProps) =>
			ClassificationFontUtils.CreateResourceDictionary(textProps, plainTextPropertiesToCopy);
		static readonly string[] plainTextPropertiesToCopy = new string[] {
			// The background props aren't copied
			EditorFormatDefinition.ForegroundBrushId,
			EditorFormatDefinition.ForegroundColorId,
			ClassificationFormatDefinition.CultureInfoId,
			ClassificationFormatDefinition.FontHintingSizeId,
			ClassificationFormatDefinition.FontRenderingSizeId,
			ClassificationFormatDefinition.ForegroundOpacityId,
			ClassificationFormatDefinition.IsBoldId,
			ClassificationFormatDefinition.IsItalicId,
			ClassificationFormatDefinition.TextDecorationsId,
			ClassificationFormatDefinition.TextEffectsId,
			ClassificationFormatDefinition.TypefaceId,
		};

		IEnumerable<Tuple<IEditorFormatMetadata, EditorFormatDefinition>> GetEditorFormatDefinitions() {
			foreach (var lazy in editorFormatDefinitionService.EditorFormatDefinitions)
				yield return Tuple.Create(lazy.Metadata, lazy.Value);
		}
	}
}
