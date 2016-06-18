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
	sealed class TextCaretLayer {
		public bool OverwriteMode { get; set; }
		public double Left => left;
		public double Right => Left + Width;
		public double Top => top;
		public double Bottom => Top + Height;
		public double Width => width;
		public double Height => height;
		double left, top, width, height;

		public bool IsHidden {
			get { return isHidden; }
			set {
				if (isHidden == value)
					return;
				isHidden = value;
				if (isHidden)
					layer.RemoveAllAdornments();
				else {
					//TODO: Add it back
				}
			}
		}
		bool isHidden;

		readonly TextCaret textCaret;
		readonly IAdornmentLayer layer;

		public TextCaretLayer(TextCaret textCaret, IAdornmentLayer layer) {
			if (textCaret == null)
				throw new ArgumentNullException(nameof(textCaret));
			if (layer == null)
				throw new ArgumentNullException(nameof(layer));
			this.textCaret = textCaret;
			this.layer = layer;
			left = top = width = height = 0;
		}

		public void Dispose() {
		}
	}
}
