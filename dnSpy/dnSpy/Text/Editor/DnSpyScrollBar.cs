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

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace dnSpy.Text.Editor {
	abstract class DnSpyScrollBar : ScrollBar {
		protected DnSpyScrollBar() {
			Scroll += DnSpyScrollBar_Scroll;
		}

		void DnSpyScrollBar_Scroll(object sender, ScrollEventArgs e) => OnScroll(e);

		// The ScrollBar class doesn't send a scroll event when the user Shift+Clicks
		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
			var old = isShiftClick;
			isShiftClick = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
			try {
				base.OnPreviewMouseLeftButtonDown(e);
			}
			finally {
				isShiftClick = old;
			}
		}
		bool isShiftClick;

		protected override void OnValueChanged(double oldValue, double newValue) {
			base.OnValueChanged(oldValue, newValue);
			if (isShiftClick)
				OnScroll(new ScrollEventArgs(ScrollEventType.ThumbPosition, newValue));
		}

		protected abstract void OnScroll(ScrollEventArgs e);

		// TODO: Hack so the correct context menu is shown in the text view
		protected override void OnContextMenuOpening(ContextMenuEventArgs e) {
			ClearValue(ContextMenuProperty);
			base.OnContextMenuOpening(e);
			var ctxMenu = ContextMenu;
			if (ctxMenu != null) {
				if (IsEnabled) {
					ctxMenu.PlacementTarget = this;
					ctxMenu.IsOpen = true;
				}
				e.Handled = true;
			}
		}
	}
}
