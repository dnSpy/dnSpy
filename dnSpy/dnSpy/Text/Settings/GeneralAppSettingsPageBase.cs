/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Settings {
	abstract class GeneralAppSettingsPageBase : AppSettingsPage, INotifyPropertyChanged {
		public sealed override string Title => dnSpy_Resources.GeneralSettings;
		public sealed override object UIObject => this;

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		public bool UseVirtualSpaceEnabled => UseVirtualSpace || !WordWrap;
		public bool WordWrapEnabled => WordWrap || !UseVirtualSpace;

		public bool UseVirtualSpace {
			get { return useVirtualSpace; }
			set {
				if (useVirtualSpace != value) {
					useVirtualSpace = value;
					OnPropertyChanged(nameof(UseVirtualSpace));
					OnPropertyChanged(nameof(UseVirtualSpaceEnabled));
					OnPropertyChanged(nameof(WordWrapEnabled));
				}
			}
		}
		bool useVirtualSpace;

		public bool WordWrap {
			get { return wordWrap; }
			set {
				if (wordWrap != value) {
					wordWrap = value;
					OnPropertyChanged(nameof(WordWrap));
					OnPropertyChanged(nameof(UseVirtualSpaceEnabled));
					OnPropertyChanged(nameof(WordWrapEnabled));
				}
			}
		}
		bool wordWrap;

		public bool WordWrapVisualGlyphs {
			get { return wordWrapVisualGlyphs; }
			set {
				if (wordWrapVisualGlyphs != value) {
					wordWrapVisualGlyphs = value;
					OnPropertyChanged(nameof(WordWrapVisualGlyphs));
				}
			}
		}
		bool wordWrapVisualGlyphs;

		public bool ShowLineNumbers {
			get { return showLineNumbers; }
			set {
				if (showLineNumbers != value) {
					showLineNumbers = value;
					OnPropertyChanged(nameof(ShowLineNumbers));
				}
			}
		}
		bool showLineNumbers;

		public bool HighlightCurrentLine {
			get { return highlightCurrentLine; }
			set {
				if (highlightCurrentLine != value) {
					highlightCurrentLine = value;
					OnPropertyChanged(nameof(HighlightCurrentLine));
				}
			}
		}
		bool highlightCurrentLine;

		readonly ICommonEditorOptions options;

		protected GeneralAppSettingsPageBase(ICommonEditorOptions options) {
			this.options = options ?? throw new ArgumentNullException(nameof(options));
			UseVirtualSpace = options.UseVirtualSpace;
			ShowLineNumbers = options.LineNumberMargin;
			WordWrap = (options.WordWrapStyle & WordWrapStyles.WordWrap) != 0;
			WordWrapVisualGlyphs = (options.WordWrapStyle & WordWrapStyles.VisibleGlyphs) != 0;
			HighlightCurrentLine = options.EnableHighlightCurrentLine;
		}

		public override void OnApply() {
			options.UseVirtualSpace = UseVirtualSpace;
			options.LineNumberMargin = ShowLineNumbers;
			options.EnableHighlightCurrentLine = HighlightCurrentLine;

			var newStyle = options.WordWrapStyle;
			if (WordWrap)
				newStyle |= WordWrapStyles.WordWrap;
			else
				newStyle &= ~WordWrapStyles.WordWrap;
			if (WordWrapVisualGlyphs)
				newStyle |= WordWrapStyles.VisibleGlyphs;
			else
				newStyle &= ~WordWrapStyles.VisibleGlyphs;
			options.WordWrapStyle = newStyle;
		}
	}
}
