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

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using dnSpy.Contracts.Menus;

namespace dnSpy.Menus {
	sealed class ContextMenuCreator {
		readonly MenuManager menuManager;
		readonly FrameworkElement elem;
		readonly Guid guid;
		readonly IGuidObjectsCreator creator;
		readonly IContextMenuInitializer initCtxMenu;

		public ContextMenuCreator(MenuManager menuManager, FrameworkElement elem, Guid guid, IGuidObjectsCreator creator, IContextMenuInitializer initCtxMenu) {
			this.menuManager = menuManager;
			this.elem = elem;
			this.guid = guid;
			this.creator = creator;
			this.initCtxMenu = initCtxMenu;
			elem.ContextMenu = new ContextMenu();
			elem.ContextMenuOpening += FrameworkElement_ContextMenuOpening;
		}

		static DependencyObject GetParent(DependencyObject depo) {
			if (depo is Visual || depo is Visual3D)
				return VisualTreeHelper.GetParent(depo);
			else if (depo is FrameworkContentElement)
				return ((FrameworkContentElement)depo).Parent;
			return null;
		}

		bool IsIgnored(object sender, ContextMenuEventArgs e) {
			if (!(elem is ListBox))
				return false;

			var o = e.OriginalSource as DependencyObject;
			while (o != null) {
				if (o == elem)
					return false;

				if (o is ScrollBar)
					return true;	// Don't set e.Handled

				o = GetParent(o);
			}

			e.Handled = true;
			return true;
		}

		void FrameworkElement_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
			if (IsIgnored(sender, e))
				return;

			bool? b = menuManager.ShowContextMenu(e, elem, new Guid(MenuConstants.CTX_MENU_GUID), new Guid(MenuConstants.CTX_MENU_GUID), new GuidObject(guid, elem), creator, initCtxMenu, e.CursorLeft == -1 && e.CursorTop == -1);
			if (b == null)
				return;
			if (!b.Value)
				e.Handled = true;
		}
	}
}
