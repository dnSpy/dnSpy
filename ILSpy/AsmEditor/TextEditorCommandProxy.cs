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

using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor {
	sealed class TextEditorCommandProxy : ContextMenuEntryCommandProxy {
		public TextEditorCommandProxy(IContextMenuEntry cmd)
			: base(cmd) {
		}

		protected override ContextMenuEntryContext CreateContext() {
			var textView = MainWindow.Instance.ActiveTextView;
			if (textView != null && textView.IsKeyboardFocusWithin)
				return ContextMenuEntryContext.Create(MainWindow.Instance.ActiveTextView, true);
			return null;
		}
	}
}
