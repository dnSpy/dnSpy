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
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;
using dnSpy.Text.MEF;

namespace dnSpy.Text.Classification {
	sealed class TextEditorFontSettings : ITextEditorFontSettings {
		readonly Lazy<TextEditorFormatDefinition, ITextEditorFormatDefinitionMetadata> textEditorFormatDefinition;
		readonly TextEditorFontSettings baseType;
		ResourceDictionary resourceDictionary;
		Brush windowBackgroundBrush;

		public TextEditorFontSettings(ITextEditorSettings textEditorSettings, Lazy<TextEditorFormatDefinition, ITextEditorFormatDefinitionMetadata> textEditorFormatDefinition, TextEditorFontSettings baseType) {
			this.textEditorFormatDefinition = textEditorFormatDefinition;
			this.baseType = baseType;
			if (baseType != null)
				baseType.SettingsChanged += BaseType_SettingsChanged;
			//TODO: Don't use the global settings
			textEditorSettings.PropertyChanged += TextEditorSettings_PropertyChanged;
		}

		void BaseType_SettingsChanged(object sender, EventArgs e) {
			//TODO: Only raise the event if we inherit the changed settings
			RaiseSettingsChanged();
		}

		void TextEditorSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var settings = (ITextEditorSettings)sender;
			if (e.PropertyName == nameof(settings.FontFamily) || e.PropertyName == nameof(settings.FontSize))
				RaiseSettingsChanged();
		}

		public void ClearCache() {
			resourceDictionary = null;
			windowBackgroundBrush = null;
		}

		public void OnThemeChanged() => RaiseSettingsChanged();

		void RaiseSettingsChanged() {
			ClearCache();
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler SettingsChanged;

		public ResourceDictionary CreateResourceDictionary(ITheme theme) {
			if (resourceDictionary == null) {
				resourceDictionary = textEditorFormatDefinition.Value.CreateResourceDictionary(theme);
				if (baseType != null)
					ClassificationFontUtils.Merge(baseType.CreateResourceDictionary(theme), resourceDictionary);
			}

			var bg = GetWindowBackground(theme);
			Debug.Assert(bg != null);
			resourceDictionary[EditorFormatMapConstants.TextViewBackgroundId] = bg;

			return resourceDictionary;
		}

		Brush GetWindowBackground(ITheme theme) {
			if (windowBackgroundBrush == null) {
				windowBackgroundBrush = textEditorFormatDefinition.Value.GetWindowBackground(theme);
				if (windowBackgroundBrush == null && baseType != null)
					windowBackgroundBrush = baseType.GetWindowBackground(theme);
				// Default settings instance should've returned a color
				Debug.Assert(windowBackgroundBrush != null);
				if (windowBackgroundBrush == null) {
					windowBackgroundBrush = theme.GetColor(ColorType.DefaultText).Background;
					Debug.Assert(windowBackgroundBrush != null);
					if (windowBackgroundBrush == null)
						windowBackgroundBrush = Brushes.White;
				}
			}
			return windowBackgroundBrush;
		}
	}
}
