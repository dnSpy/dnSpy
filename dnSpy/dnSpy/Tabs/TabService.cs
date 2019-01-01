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
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Tabs;

namespace dnSpy.Tabs {
	sealed class TabService : ITabService {
		public IEnumerable<ITabGroupService> TabGroupServices => tabGroupServices;
		readonly List<TabGroupService> tabGroupServices;

		public ITabGroupService ActiveTabGroupService => selectedIndex < 0 ? null : tabGroupServices[selectedIndex];

		public int SelectedIndex => selectedIndex;
		int selectedIndex;

		readonly IMenuService menuService;
		readonly IWpfFocusService wpfFocusService;

		public TabService(IMenuService menuService, IWpfFocusService wpfFocusService) {
			this.menuService = menuService;
			this.wpfFocusService = wpfFocusService;
			tabGroupServices = new List<TabGroupService>();
			selectedIndex = -1;
		}

		public ITabGroupService Create(TabGroupServiceOptions options = null) {
			var mgr = new TabGroupService(this, menuService, wpfFocusService, options);
			tabGroupServices.Add(mgr);
			if (selectedIndex < 0)
				selectedIndex = 0;
			return mgr;
		}

		public void Remove(ITabGroupService mgr) {
			if (mgr == null)
				throw new ArgumentNullException(nameof(mgr));
			int index = tabGroupServices.IndexOf((TabGroupService)mgr);
			Debug.Assert(index >= 0);
			if (index >= 0) {
				tabGroupServices.RemoveAt(index);
				if (selectedIndex >= tabGroupServices.Count)
					selectedIndex = tabGroupServices.Count - 1;
			}
		}

		internal void SetActive(TabGroupService tabGroupService) {
			int index = tabGroupServices.IndexOf(tabGroupService);
			Debug.Assert(index >= 0);
			if (index >= 0)
				selectedIndex = index;
		}
	}
}
