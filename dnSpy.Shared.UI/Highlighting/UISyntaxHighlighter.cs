/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Highlighting;
using dnSpy.Shared.UI.Controls;

namespace dnSpy.Shared.UI.Highlighting {
	public struct UISyntaxHighlighter {
		SyntaxHighlighter syntaxHighlighter;
		NoSyntaxHighlightOutput output;

		public bool IsEmpty {
			get {
				if (syntaxHighlighter != null)
					return syntaxHighlighter.IsEmpty;
				return output.IsEmpty;
			}
		}

		public string Text {
			get {
				if (syntaxHighlighter != null)
					return syntaxHighlighter.Text;
				return output.Text;
			}
		}

		public ISyntaxHighlightOutput Output {
			get { return (ISyntaxHighlightOutput)output ?? syntaxHighlighter; }
		}

		public static UISyntaxHighlighter Create(bool syntaxHighlight) {
			return new UISyntaxHighlighter(syntaxHighlight);
		}

		UISyntaxHighlighter(bool syntaxHighlight) {
			if (syntaxHighlight) {
				this.syntaxHighlighter = new SyntaxHighlighter();
				this.output = null;
			}
			else {
				this.syntaxHighlighter = null;
				this.output = new NoSyntaxHighlightOutput();
			}
		}

		static string ToString(string s, bool filterOutNewLines) {
			if (!filterOutNewLines)
				return s;
			var sb = new StringBuilder(s.Length);
			foreach (var c in s) {
				if (c == '\r' || c == '\n')
					continue;
				sb.Append(c);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Creates the object. If it's syntax highlighted, it's a <see cref="TextBlock"/>, else
		/// it's just a <see cref="string"/>. See also <see cref="CreateTextBlock(bool,bool)"/>
		/// </summary>
		/// <param name="useEllipsis">true to add <see cref="TextTrimming.CharacterEllipsis"/> to the <see cref="TextBlock"/></param>
		/// <param name="filterOutNewLines">true to filter out newline characters</param>
		/// <returns></returns>
		public object CreateObject(bool useEllipsis = false, bool filterOutNewLines = true) {
			if (syntaxHighlighter != null)
				return syntaxHighlighter.Create(useEllipsis, filterOutNewLines);

			return ToString(output.ToString(), filterOutNewLines);
		}

		/// <summary>
		/// Creates a <see cref="FrameworkElement"/> containing the resulting text
		/// </summary>
		/// <param name="useEllipsis">true to add <see cref="TextTrimming.CharacterEllipsis"/> to the <see cref="TextBlock"/></param>
		/// <param name="filterOutNewLines">true to filter out newline characters</param>
		/// <returns></returns>
		public FrameworkElement CreateTextBlock(bool useEllipsis = false, bool filterOutNewLines = true) {
			if (syntaxHighlighter != null)
				return syntaxHighlighter.Create(useEllipsis, filterOutNewLines);

			if (!useEllipsis) {
				return new FastTextBlock {
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
