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
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class TextSelectionLayer {
		readonly TextSelection textSelection;
		readonly IAdornmentLayer layer;

		public TextSelectionLayer(TextSelection textSelection, IAdornmentLayer layer) {
			if (textSelection == null)
				throw new ArgumentNullException(nameof(textSelection));
			if (layer == null)
				throw new ArgumentNullException(nameof(layer));
			this.textSelection = textSelection;
			this.layer = layer;
			textSelection.SelectionChanged += TextSelection_SelectionChanged;
		}

		public void OnModeUpdated() => Repaint();

		void TextSelection_SelectionChanged(object sender, EventArgs e) {
			//TODO:
		}

		void Repaint() {
			//TODO: Use textSelection.GetSelectionOnTextViewLine()
		}

		public void Dispose() {
			textSelection.SelectionChanged -= TextSelection_SelectionChanged;
		}
	}
}
