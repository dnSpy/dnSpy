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

using System.Text;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Text;
using ICSharpCode.AvalonEdit.Utils;

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// Creates <see cref="FrameworkElement"/>s that are optionally colorized
	/// </summary>
	public struct ColorizedTextElementCreator {
		TextBlockColorOutput colorizer;
		StringBuilderTextColorOutput output;

		/// <summary>
		/// true if nothing has been written
		/// </summary>
		public bool IsEmpty => colorizer != null ? colorizer.IsEmpty : output.IsEmpty;

		/// <summary>
		/// Gets the text
		/// </summary>
		public string Text => colorizer != null ? colorizer.Text : output.Text;

		/// <summary>
		/// Gets the output writer
		/// </summary>
		public IOutputColorWriter Output => (IOutputColorWriter)output ?? colorizer;

		/// <summary>
		/// Creates a new instance
		/// </summary>
		/// <param name="colorize">true to colorize the text</param>
		/// <returns></returns>
		public static ColorizedTextElementCreator Create(bool colorize) => new ColorizedTextElementCreator(colorize);

		ColorizedTextElementCreator(bool colorize) {
			if (colorize) {
				this.colorizer = new TextBlockColorOutput();
				this.output = null;
			}
			else {
				this.colorizer = null;
				this.output = new StringBuilderTextColorOutput();
			}
		}

		static string ToString(string s, bool filterOutNewLines) {
			if (!filterOutNewLines)
				return s;
			var sb = new StringBuilder(s.Length);
			foreach (var c in s) {
				if (c == '\r' || c == '\n' || c == '\u0085' || c == '\u2028' || c == '\u2029')
					continue;
				sb.Append(c);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Creates a <see cref="FrameworkElement"/> containing the resulting text
		/// </summary>
		/// <param name="useEllipsis">true to add <see cref="TextTrimming.CharacterEllipsis"/> to the <see cref="TextBlock"/></param>
		/// <param name="filterOutNewLines">true to filter out newline characters</param>
		/// <returns></returns>
		public FrameworkElement CreateResult(bool useEllipsis = false, bool filterOutNewLines = true) =>
			CreateResultNewFormatter(false, useEllipsis, filterOutNewLines);

		/// <summary>
		/// Creates a <see cref="FrameworkElement"/> containing the resulting text
		/// </summary>
		/// <param name="newFormatter">true to use the new faster formatter, which has its limitations (doesn't support all characters, no word wrap support)</param>
		/// <param name="useEllipsis">true to add <see cref="TextTrimming.CharacterEllipsis"/> to the <see cref="TextBlock"/></param>
		/// <param name="filterOutNewLines">true to filter out newline characters</param>
		/// <returns></returns>
		public FrameworkElement CreateResultNewFormatter(bool newFormatter, bool useEllipsis = false, bool filterOutNewLines = true) {
			var provider = newFormatter ? TextFormatterProvider.GlyphRunFormatter : TextFormatterProvider.BuiltIn;
			if (colorizer != null)
				return colorizer.Create(provider, useEllipsis, filterOutNewLines);

			if (!useEllipsis && filterOutNewLines) {
				return new FastTextBlock(provider) {
					Text = ToString(output.ToString(), filterOutNewLines)
				};
			}
			else {
				return new TextBlock {
					Text = ToString(output.ToString(), filterOutNewLines),
					TextTrimming = TextTrimming.CharacterEllipsis
				};
			}
		}
	}
}
