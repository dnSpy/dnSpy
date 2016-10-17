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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Output.Settings {
	sealed class GeneralAppSettingsPage : AppSettingsPage, INotifyPropertyChanged {
		public override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_OUTPUT);
		public override Guid Guid => new Guid("FB352B1B-A310-480E-B0D6-C3F78CAA2B32");
		public override double Order => AppSettingsConstants.ORDER_OUTPUT_DEFAULT_GENERAL;
		public override string Title => dnSpy_Resources.GeneralSettings;
		public override ImageReference Icon => ImageReference.None;
		public override object UIObject => this;

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

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

		public bool ShowTimestamps {
			get { return showTimestamps; }
			set {
				if (showTimestamps != value) {
					showTimestamps = value;
					OnPropertyChanged(nameof(ShowTimestamps));
				}
			}
		}
		bool showTimestamps;

		readonly IOutputWindowOptions options;

		public GeneralAppSettingsPage(IOutputWindowOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			this.options = options;
			UseVirtualSpace = options.UseVirtualSpace;
			ShowLineNumbers = options.LineNumberMargin;
			WordWrap = (options.WordWrapStyle & WordWrapStyles.WordWrap) != 0;
			WordWrapVisualGlyphs = (options.WordWrapStyle & WordWrapStyles.VisibleGlyphs) != 0;
			HighlightCurrentLine = options.EnableHighlightCurrentLine;
			ShowTimestamps = options.ShowTimestamps;
		}

		public override void OnApply() {
			options.UseVirtualSpace = UseVirtualSpace;
			options.LineNumberMargin = ShowLineNumbers;
			options.EnableHighlightCurrentLine = HighlightCurrentLine;
			options.ShowTimestamps = ShowTimestamps;

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

		public override object GetDataTemplateObject() => UIObject;
	}
}
