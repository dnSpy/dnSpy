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
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.Themes;

namespace dnSpy.Tabs {
	sealed class TabManager : ITabManager {
		public IEnumerable<ITabGroupManager> TabGroupManagers => tabGroupManagers;
		readonly List<TabGroupManager> tabGroupManagers;

		public ITabGroupManager ActiveTabGroupManager => selectedIndex < 0 ? null : tabGroupManagers[selectedIndex];

		public int SelectedIndex => selectedIndex;
		int selectedIndex;

		readonly IMenuManager menuManager;
		readonly IWpfFocusManager wpfFocusManager;

		public TabManager(IThemeManager themeManager, IMenuManager menuManager, IWpfFocusManager wpfFocusManager) {
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
			this.menuManager = menuManager;
			this.wpfFocusManager = wpfFocusManager;
			this.tabGroupManagers = new List<TabGroupManager>();
			this.selectedIndex = -1;
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			foreach (var mgr in tabGroupManagers)
				mgr.OnThemeChanged();
		}

		public ITabGroupManager Create(TabGroupManagerOptions options = null) {
			var mgr = new TabGroupManager(this, menuManager, wpfFocusManager, options);
			tabGroupManagers.Add(mgr);
			if (selectedIndex < 0)
				selectedIndex = 0;
			return mgr;
		}

		public void Remove(ITabGroupManager mgr) {
			if (mgr == null)
				throw new ArgumentNullException();
			int index = tabGroupManagers.IndexOf((TabGroupManager)mgr);
			Debug.Assert(index >= 0);
			if (index >= 0) {
				tabGroupManagers.RemoveAt(index);
				if (selectedIndex >= tabGroupManagers.Count)
					selectedIndex = tabGroupManagers.Count - 1;
			}
		}

		internal void SetActive(TabGroupManager tabGroupManager) {
			int index = tabGroupManagers.IndexOf(tabGroupManager);
			Debug.Assert(index >= 0);
			if (index >= 0)
				selectedIndex = index;
		}
	}
}
