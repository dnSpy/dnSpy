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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using dnSpy.Contracts.Menus;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.TextEditor {
	sealed class ContextMenuInitializer : IContextMenuInitializer {
		readonly Control ctrl;
		readonly NewTextEditor textEditor;

		public ContextMenuInitializer(Control ctrl, NewTextEditor textEditor) {
			this.ctrl = ctrl;
			this.textEditor = textEditor;
		}

		public void Initialize(IMenuItemContext context, ContextMenu menu) {
			if (context.OpenedFromKeyboard) {
				IScrollInfo scrollInfo = textEditor.TextArea.TextView;
				var pos = textEditor.TextArea.TextView.GetVisualPosition(textEditor.TextArea.Caret.Position, VisualYPosition.TextBottom);
				pos = new Point(pos.X - scrollInfo.HorizontalOffset, pos.Y - scrollInfo.VerticalOffset);

				menu.HorizontalOffset = pos.X;
				menu.VerticalOffset = pos.Y;
				ContextMenuService.SetPlacement(ctrl, PlacementMode.Relative);
				ContextMenuService.SetPlacementTarget(ctrl, textEditor.TextArea.TextView);
				menu.Closed += (s, e2) => {
					ctrl.ClearValue(ContextMenuService.PlacementProperty);
					ctrl.ClearValue(ContextMenuService.PlacementTargetProperty);
				};
			}
			else {
				ctrl.ClearValue(ContextMenuService.PlacementProperty);
				ctrl.ClearValue(ContextMenuService.PlacementTargetProperty);
			}
		}
	}
}
