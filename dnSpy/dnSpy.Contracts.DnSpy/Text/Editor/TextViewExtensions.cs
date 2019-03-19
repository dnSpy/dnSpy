/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Text view extensions
	/// </summary>
	public static class TextViewExtensions {
		/// <summary>
		/// Scrolls the view to make the caret visible. If it's not visible, it's centered.
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <param name="center">true to center the caret</param>
		public static void EnsureCaretVisible(this ITextView textView, bool center = false) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			var position = textView.Caret.Position.VirtualBufferPosition;
			var options = EnsureSpanVisibleOptions.ShowStart;
			if (center)
				options |= EnsureSpanVisibleOptions.AlwaysCenter;
			textView.ViewScroller.EnsureSpanVisible(new VirtualSnapshotSpan(position, position), options);
		}
	}
}
