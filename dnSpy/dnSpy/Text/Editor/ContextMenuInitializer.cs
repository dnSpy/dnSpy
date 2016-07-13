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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using dnSpy.Contracts.Menus;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class ContextMenuInitializer : IContextMenuInitializer {
		readonly IWpfTextView textView;
		readonly FrameworkElement ctrl;

		public ContextMenuInitializer(IWpfTextView textView)
			: this(textView, textView.VisualElement) {
		}

		public ContextMenuInitializer(IWpfTextView textView, FrameworkElement ctrl) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (ctrl == null)
				throw new ArgumentNullException(nameof(ctrl));
			this.textView = textView;
			this.ctrl = ctrl;
		}

		public void Initialize(IMenuItemContext context, ContextMenu menu) {
			if (!TrySetPlacement(context, menu))
				ClearPlacementProperties();
		}

		bool TrySetPlacement(IMenuItemContext context, ContextMenu menu) {
			if (!context.OpenedFromKeyboard)
				return false;

			var line = textView.Caret.ContainingTextViewLine;
			menu.HorizontalOffset = Math.Min(Math.Max(0, textView.Caret.Right - textView.ViewportLeft), textView.ViewportWidth);
			menu.VerticalOffset = Math.Min(Math.Max(0, line.TextBottom - textView.ViewportTop), textView.ViewportHeight);
			ContextMenuService.SetPlacement(ctrl, PlacementMode.Relative);
			ContextMenuService.SetPlacementTarget(ctrl, textView.VisualElement);
			menu.Closed += (s, e2) => ClearPlacementProperties();
			return true;
		}

		void ClearPlacementProperties() {
			ctrl.ClearValue(ContextMenuService.PlacementProperty);
			ctrl.ClearValue(ContextMenuService.PlacementTargetProperty);
		}
	}
}
