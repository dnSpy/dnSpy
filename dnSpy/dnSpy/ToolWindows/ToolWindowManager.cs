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
using System.Windows;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.ToolWindows;

namespace dnSpy.ToolWindows {
	sealed class ToolWindowManager : IToolWindowManager {
		readonly ITabManager tabManager;

		public ToolWindowManager(ITabManager tabManager) {
			this.tabManager = tabManager;
		}

		public IToolWindowGroupManager Create(ToolWindowGroupManagerOptions options) {
			var newOptions = Convert(options);
			var ctxMenuHelper = new InitializeContextMenuHelper(newOptions.TabGroupGuid);
			if (newOptions.TabGroupGuid != Guid.Empty)
				newOptions.InitializeContextMenu = ctxMenuHelper.InitializeContextMenu;
			var mgr = new ToolWindowGroupManager(tabManager.Create(newOptions));
			ctxMenuHelper.ToolWindowGroupManager = mgr;
			return mgr;
		}

		TabGroupManagerOptions Convert(ToolWindowGroupManagerOptions options) {
			if (options == null)
				options = new ToolWindowGroupManagerOptions();
			return new TabGroupManagerOptions {
				TabControlStyle = options.TabControlStyle ?? "ToolWindowGroupTabControlStyle",
				TabItemStyle = options.TabItemStyle ?? "ToolWindowGroupTabItemStyle",
				TabGroupGuid = options.ToolWindowGroupGuid,
			};
		}

		sealed class InitializeContextMenuHelper {
			readonly Guid tabGroupGuid;

			public ToolWindowGroupManager ToolWindowGroupManager { get; set; }

			public InitializeContextMenuHelper(Guid tabGroupGuid) {
				this.tabGroupGuid = tabGroupGuid;
			}

			public IContextMenuProvider InitializeContextMenu(IMenuManager menuManager, ITabGroup tabGroup, FrameworkElement elem) => menuManager.InitializeContextMenu(elem, tabGroupGuid, new GuidObjectsProvider(this, tabGroup));

			sealed class GuidObjectsProvider : IGuidObjectsProvider {
				readonly InitializeContextMenuHelper owner;
				readonly ITabGroup tabGroup;

				public GuidObjectsProvider(InitializeContextMenuHelper owner, ITabGroup tabGroup) {
					this.owner = owner;
					this.tabGroup = tabGroup;
				}

				public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
					Debug.Assert(owner.ToolWindowGroupManager != null);
					if (owner.ToolWindowGroupManager != null) {
						var twg = owner.ToolWindowGroupManager.GetToolWindowGroup(tabGroup);
						Debug.Assert(twg != null);
						if (twg != null)
							yield return new GuidObject(MenuConstants.GUIDOBJ_TOOLWINDOWGROUP_GUID, twg);
					}
				}
			}
		}
	}
}
