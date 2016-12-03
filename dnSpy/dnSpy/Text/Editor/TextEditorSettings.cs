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
using System.Windows.Media;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	class TextEditorSettings : ViewModelBase, ITextEditorSettings {
		protected virtual void OnModified() { }

		public FontFamily FontFamily {
			get { return fontFamily; }
			set {
				if (fontFamily.Source != value.Source) {
					fontFamily = value;
					OnPropertyChanged(nameof(FontFamily));
					OnModified();
				}
			}
		}
		FontFamily fontFamily = new FontFamily(FontUtilities.GetDefaultTextEditorFont());

		public double FontSize {
			get { return fontSize; }
			set {
				if (fontSize != value) {
					fontSize = FontUtilities.FilterFontSize(value);
					OnPropertyChanged(nameof(FontSize));
					OnModified();
				}
			}
		}
		double fontSize = FontUtilities.DEFAULT_FONT_SIZE;
	}

	[Export, Export(typeof(ITextEditorSettings))]
	sealed class TextEditorSettingsImpl : TextEditorSettings {
		static readonly Guid SETTINGS_GUID = new Guid("9D40E1AD-5922-4BBA-B386-E6BABE5D185D");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		TextEditorSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			FontFamily = new FontFamily(sect.Attribute<string>(nameof(FontFamily)) ?? FontUtilities.GetDefaultTextEditorFont());
			FontSize = sect.Attribute<double?>(nameof(FontSize)) ?? FontSize;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(FontFamily), FontFamily.Source);
			sect.Attribute(nameof(FontSize), FontSize);
		}
	}
}
