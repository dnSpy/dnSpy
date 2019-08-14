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
using System.Windows;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.ToolWindows;

namespace dnSpy.ToolWindows {
	sealed class ToolWindowService : IToolWindowService {
		readonly ITabService tabService;

		public ToolWindowService(ITabService tabService) => this.tabService = tabService;

		public IToolWindowGroupService Create(ToolWindowGroupServiceOptions options) {
			var newOptions = Convert(options);
			var ctxMenuHelper = new InitializeContextMenuHelper(newOptions.TabGroupGuid);
			if (newOptions.TabGroupGuid != Guid.Empty)
				newOptions.InitializeContextMenu = ctxMenuHelper.InitializeContextMenu;
			var mgr = new ToolWindowGroupService(tabService.Create(newOptions));
			ctxMenuHelper.ToolWindowGroupService = mgr;
			return mgr;
		}

		TabGroupServiceOptions Convert(ToolWindowGroupServiceOptions options) {
			if (options is null)
				options = new ToolWindowGroupServiceOptions();
			return new TabGroupServiceOptions {
				TabControlStyle = options.TabControlStyle ?? "ToolWindowGroupTabControlStyle",
				TabItemStyle = options.TabItemStyle ?? "ToolWindowGroupTabItemStyle",
				TabGroupGuid = options.ToolWindowGroupGuid,
			};
		}

		sealed class InitializeContextMenuHelper {
			readonly Guid tabGroupGuid;

			public ToolWindowGroupService? ToolWindowGroupService { get; set; }

			public InitializeContextMenuHelper(Guid tabGroupGuid) => this.tabGroupGuid = tabGroupGuid;

			public IContextMenuProvider InitializeContextMenu(IMenuService menuService, ITabGroup tabGroup, FrameworkElement elem) => menuService.InitializeContextMenu(elem, tabGroupGuid, new GuidObjectsProvider(this, tabGroup));

			sealed class GuidObjectsProvider : IGuidObjectsProvider {
				readonly InitializeContextMenuHelper owner;
				readonly ITabGroup tabGroup;

				public GuidObjectsProvider(InitializeContextMenuHelper owner, ITabGroup tabGroup) {
					this.owner = owner;
					this.tabGroup = tabGroup;
				}

				public IEnumerable<GuidObject> GetGuidObjects(GuidObjectsProviderArgs args) {
					Debug2.Assert(!(owner.ToolWindowGroupService is null));
					if (!(owner.ToolWindowGroupService is null)) {
						var twg = owner.ToolWindowGroupService.GetToolWindowGroup(tabGroup);
						Debug2.Assert(!(twg is null));
						if (!(twg is null))
							yield return new GuidObject(MenuConstants.GUIDOBJ_TOOLWINDOWGROUP_GUID, twg);
					}
				}
			}
		}
	}
}
