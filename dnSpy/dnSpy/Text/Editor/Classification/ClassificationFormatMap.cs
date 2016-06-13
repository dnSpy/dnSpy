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
using System.Collections.ObjectModel;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.Classification;
using dnSpy.Contracts.Text.Formatting;
using dnSpy.Contracts.Themes;
using dnSpy.Themes;

namespace dnSpy.Text.Editor.Classification {
	sealed class ClassificationFormatMap : IClassificationFormatMap {
		public TextFormattingRunProperties DefaultTextProperties {
			get {
				throw new NotImplementedException();//TODO:
			}
		}

		public event EventHandler<EventArgs> ClassificationFormatMappingChanged;
		readonly ITextView textView;

		public ClassificationFormatMap(IThemeManager themeManager, ITextView textView) {
			if (themeManager == null)
				throw new ArgumentNullException(nameof(themeManager));
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			this.textView = textView;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			var themeManager = (ThemeManager)sender;
			//TODO: Update colors
			ClassificationFormatMappingChanged?.Invoke(this, EventArgs.Empty);
		}

		public TextFormattingRunProperties GetExplicitTextProperties(IClassificationType classificationType) {
			throw new NotImplementedException();//TODO:
		}

		public TextFormattingRunProperties GetTextProperties(IClassificationType classificationType) {
			throw new NotImplementedException();//TODO:
		}
	}
}
