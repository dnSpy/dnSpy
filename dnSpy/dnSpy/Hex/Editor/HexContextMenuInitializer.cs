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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Menus;

namespace dnSpy.Hex.Editor {
	sealed class HexContextMenuInitializer : IContextMenuInitializer {
		readonly WpfHexView hexView;
		readonly FrameworkElement ctrl;

		public HexContextMenuInitializer(WpfHexView hexView)
			: this(hexView, hexView.VisualElement) {
		}

		public HexContextMenuInitializer(WpfHexView hexView, FrameworkElement ctrl) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			if (ctrl == null)
				throw new ArgumentNullException(nameof(ctrl));
			this.hexView = hexView;
			this.ctrl = ctrl;
		}

		public void Initialize(IMenuItemContext context, ContextMenu menu) {
			if (!TrySetPlacement(context, menu))
				ClearPlacementProperties();
		}

		bool TrySetPlacement(IMenuItemContext context, ContextMenu menu) {
			if (!context.OpenedFromKeyboard)
				return false;

			double caretRight;
			if (hexView.Caret.IsValuesCaretPresent && hexView.Caret.Position.Position.ActiveColumn == HexColumnType.Values)
				caretRight = hexView.Caret.ValuesRight;
			else if (hexView.Caret.IsAsciiCaretPresent && hexView.Caret.Position.Position.ActiveColumn == HexColumnType.Ascii)
				caretRight = hexView.Caret.AsciiRight;
			else
				return false;

			var line = hexView.Caret.ContainingHexViewLine;
			menu.HorizontalOffset = Math.Min(Math.Max(0, caretRight - hexView.ViewportLeft), hexView.ViewportWidth);
			menu.VerticalOffset = Math.Min(Math.Max(0, line.TextBottom - hexView.ViewportTop), hexView.ViewportHeight);
			ContextMenuService.SetPlacement(ctrl, PlacementMode.Relative);
			ContextMenuService.SetPlacementTarget(ctrl, hexView.VisualElement);
			menu.Closed += (s, e2) => ClearPlacementProperties();
			return true;
		}

		void ClearPlacementProperties() {
			ctrl.ClearValue(ContextMenuService.PlacementProperty);
			ctrl.ClearValue(ContextMenuService.PlacementTargetProperty);
		}
	}
}
