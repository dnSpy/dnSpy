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
using dnSpy.TextView;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.Options;

namespace dnSpy.TreeNodes {
	public struct UISyntaxHighlighter {
		SimpleHighlighter simpleHighlighter;
		PlainTextOutput output;

		public bool IsSyntaxHighlighted {
			get { return simpleHighlighter != null; }
		}

		public ITextOutput TextOutput {
			get { return output ?? simpleHighlighter.TextOutput; }
		}

		public static UISyntaxHighlighter CreateTreeView() {
			return new UISyntaxHighlighter(DisplaySettingsPanel.CurrentDisplaySettings.SyntaxHighlightTreeViewUI);
		}

		public static UISyntaxHighlighter CreateAnalyzerTreeView() {
			return new UISyntaxHighlighter(DisplaySettingsPanel.CurrentDisplaySettings.SyntaxHighlightAnalyzerTreeViewUI);
		}

		public static UISyntaxHighlighter CreateSearchList() {
			return new UISyntaxHighlighter(DisplaySettingsPanel.CurrentDisplaySettings.SyntaxHighlightSearchListUI);
		}

		public static UISyntaxHighlighter Create(bool highlight) {
			return new UISyntaxHighlighter(highlight);
		}

		UISyntaxHighlighter(bool highlight) {
			if (highlight) {
				this.simpleHighlighter = new SimpleHighlighter();
				this.output = null;
			}
			else {
				this.simpleHighlighter = null;
				this.output = new PlainTextOutput();
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
		/// it's just a <see cref="string"/>. See also <see cref="CreateTextBlock()"/>
		/// </summary>
		/// <param name="useEllipsis">true to add <see cref="TextTrimming.CharacterEllipsis"/> to the <see cref="TextBlock"/></param>
		/// <param name="filterOutNewLines">true to filter out newline characters</param>
		/// <returns></returns>
		public object CreateObject(bool useEllipsis = false, bool filterOutNewLines = true) {
			if (simpleHighlighter != null)
				return simpleHighlighter.Create(useEllipsis, filterOutNewLines);

			return ToString(output.ToString(), filterOutNewLines);
		}

		/// <summary>
		/// Creates a <see cref="TextBlock"/> containing the resulting text
		/// </summary>
		/// <param name="useEllipsis">true to add <see cref="TextTrimming.CharacterEllipsis"/> to the <see cref="TextBlock"/></param>
		/// <param name="filterOutNewLines">true to filter out newline characters</param>
		/// <returns></returns>
		public TextBlock CreateTextBlock(bool useEllipsis = false, bool filterOutNewLines = true) {
			if (simpleHighlighter != null)
				return simpleHighlighter.Create(useEllipsis, filterOutNewLines);

			var tb = new TextBlock {
				Text = ToString(output.ToString(), filterOutNewLines),
			};
			if (useEllipsis)
				tb.TextTrimming = TextTrimming.CharacterEllipsis;
			return tb;
		}
	}
}
