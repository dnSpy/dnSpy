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
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Menus {
	sealed class ContextMenuProvider : IContextMenuProvider {
		readonly MenuService menuService;
		readonly FrameworkElement element;
		readonly Guid guid;
		readonly IGuidObjectsProvider provider;
		readonly IContextMenuInitializer initCtxMenu;
		readonly Guid ctxMenuGuid;

		public ContextMenuProvider(MenuService menuService, FrameworkElement elem, Guid guid, IGuidObjectsProvider provider, IContextMenuInitializer initCtxMenu, Guid? ctxMenuGuid) {
			this.menuService = menuService;
			element = elem;
			this.guid = guid;
			this.provider = provider;
			this.initCtxMenu = initCtxMenu;
			this.ctxMenuGuid = ctxMenuGuid ?? new Guid(MenuConstants.CTX_MENU_GUID);
			elem.ContextMenu = new ContextMenu();
			elem.ContextMenuOpening += FrameworkElement_ContextMenuOpening;
		}

		bool IsIgnored(object sender, ContextMenuEventArgs e) {
			if (!(element is ListBox))
				return false;

			var o = e.OriginalSource as DependencyObject;
			while (o != null) {
				if (o == element)
					return false;

				if (o is ScrollBar)
					return true;	// Don't set e.Handled

				o = UIUtilities.GetParent(o);
			}

			e.Handled = true;
			return true;
		}

		void FrameworkElement_ContextMenuOpening(object sender, ContextMenuEventArgs e) {
			if (IsIgnored(sender, e))
				return;

			bool? b = menuService.ShowContextMenu(e, element, ctxMenuGuid, ctxMenuGuid, new GuidObject(guid, element), provider, initCtxMenu, e.CursorLeft == -1 && e.CursorTop == -1);
			if (b == null)
				return;
			if (!b.Value)
				e.Handled = true;
		}

		public void Show(FrameworkElement elem) {
			bool? b = menuService.ShowContextMenu(0, elem, ctxMenuGuid, ctxMenuGuid, new GuidObject(guid, element), provider, initCtxMenu, false);
			if (b == true) {
				elem.ContextMenu.Placement = PlacementMode.Bottom;
				elem.ContextMenu.PlacementTarget = elem;
				elem.ContextMenu.IsOpen = true;
			}
		}
	}
}
