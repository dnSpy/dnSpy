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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.Editor {
	static class SelectionUtilities {
		public static HexBufferSpan GetLineAnchorSpan(HexSelection selection) {
			if (selection == null)
				throw new ArgumentNullException(nameof(selection));
			if (selection.IsEmpty)
				return selection.HexView.Caret.ContainingHexViewLine.BufferSpan;
			var anchorExtent = selection.HexView.GetHexViewLineContainingBufferPosition(selection.AnchorPoint).BufferSpan;
			if (selection.AnchorPoint >= selection.ActivePoint) {
				if (anchorExtent.Start == selection.AnchorPoint && selection.AnchorPoint > selection.HexView.BufferLines.BufferStart)
					anchorExtent = selection.HexView.GetHexViewLineContainingBufferPosition(selection.AnchorPoint - 1).BufferSpan;
			}
			return anchorExtent;
		}
	}
}
