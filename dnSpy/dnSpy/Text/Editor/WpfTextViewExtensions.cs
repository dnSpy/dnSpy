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
using System.Windows.Input;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	static class WpfTextViewExtensions {
		public static TextEditorLocation? GetTextEditorLocation(this IWpfTextView textView, bool openedFromKeyboard) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (openedFromKeyboard)
				return GetTextEditorLocationFromCaret(textView);
			return GetTextEditorLocationFromMouse(textView);
		}

		static TextEditorLocation? GetTextEditorLocation(VirtualSnapshotPoint point) {
			var line = point.Position.GetContainingLine();
			return new TextEditorLocation(line.LineNumber, point.Position - line.Start + point.VirtualSpaces);
		}

		static TextEditorLocation? GetTextEditorLocationFromCaret(IWpfTextView textView) =>
			GetTextEditorLocation(textView.Caret.Position.VirtualBufferPosition);

		static TextEditorLocation? GetTextEditorLocationFromMouse(IWpfTextView textView) {
			if (!textView.VisualElement.IsVisible)
				return null;
			var loc = MouseLocation.Create(textView, new MouseEventArgs(Mouse.PrimaryDevice, 0));
			if (loc.Point.Y < textView.ViewportTop || loc.Point.Y >= textView.ViewportBottom)
				return null;
			if (loc.Point.X < textView.ViewportLeft || loc.Point.X >= textView.ViewportRight)
				return null;
			return GetTextEditorLocation(loc.Position);
		}
	}
}
