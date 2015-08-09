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

using System.Windows.Controls;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.Options;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy.TreeNodes {
	struct UISyntaxHighlighter {
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

		/// <summary>
		/// Creates the object. If it's syntax highlighted, it's a <see cref="TextBlock"/>, else
		/// it's just a <see cref="string"/>. See also <see cref="CreateTextBlock()"/>
		/// </summary>
		/// <returns></returns>
		public object CreateObject() {
			if (simpleHighlighter != null)
				return simpleHighlighter.Create();

			return output.ToString();
		}

		/// <summary>
		/// Creates a <see cref="TextBlock"/> containing the resulting text
		/// </summary>
		/// <returns></returns>
		public TextBlock CreateTextBlock() {
			if (simpleHighlighter != null)
				return simpleHighlighter.Create();

			return new TextBlock {
				Text = output.ToString(),
			};
		}
	}
}
