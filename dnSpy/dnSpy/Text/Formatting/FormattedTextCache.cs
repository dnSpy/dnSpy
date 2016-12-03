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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Text.Formatting {
	sealed class FormattedTextCache {
		readonly TextFormattingMode textFormattingMode;
		readonly Dictionary<TextFormattingRunProperties, Info> dict;

		sealed class Info {
			readonly FormattedText formattedText;

			public double ColumnWidth => formattedText.WidthIncludingTrailingWhitespace - formattedText.Width;
			public double LineHeight => formattedText.Height;
			public double TextHeightAboveBaseline => formattedText.Baseline;
			public double TextHeightBelowBaseline => formattedText.Height - formattedText.Baseline;

			public Info(FormattedText formattedText) {
				this.formattedText = formattedText;
			}
		}

		public FormattedTextCache(bool useDisplayMode) {
			textFormattingMode = useDisplayMode ? TextFormattingMode.Display : TextFormattingMode.Ideal;
			dict = new Dictionary<TextFormattingRunProperties, Info>();
		}

		Info GetInfo(TextFormattingRunProperties props) {
			Info info;
			if (dict.TryGetValue(props, out info))
				return info;
			var ft = new FormattedText("Xg ", props.CultureInfo, FlowDirection.LeftToRight, props.Typeface, props.FontRenderingEmSize, props.ForegroundBrush, null, textFormattingMode);
			info = new Info(ft);
			dict.Add(props, info);
			return info;
		}

		public double GetColumnWidth(TextFormattingRunProperties props) => GetInfo(props).ColumnWidth;
		public double GetLineHeight(TextFormattingRunProperties props) => GetInfo(props).LineHeight;
		public double GetTextHeightAboveBaseline(TextFormattingRunProperties props) => GetInfo(props).TextHeightAboveBaseline;
		public double GetTextHeightBelowBaseline(TextFormattingRunProperties props) => GetInfo(props).TextHeightBelowBaseline;
	}
}
