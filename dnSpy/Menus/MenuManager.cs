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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.Images;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Menus {
	sealed class MenuItemMD {
		readonly Lazy<IMenuItem, IMenuItemMetadata> lazy;

		public IMenuItem MenuItem {
			get { return lazy.Value; }
		}

		public IMenuItemMetadata Metadata {
			get { return lazy.Metadata; }
		}

		public MenuItemMD(Lazy<IMenuItem, IMenuItemMetadata> lazy) {
			this.lazy = lazy;
		}
	}

	sealed class MenuItemGroupMD {
		public readonly double Order;
		public readonly List<MenuItemMD> Items;

		public MenuItemGroupMD(double order) {
			this.Order = order;
			this.Items = new List<MenuItemMD>();
		}
	}

	sealed class MenuMD {
		readonly Lazy<IMenu, IMenuMetadata> lazy;

		public IMenu Menu {
			get { return lazy.Value; }
		}

		public IMenuMetadata Metadata {
			get { return lazy.Metadata; }
		}

		public MenuMD(Lazy<IMenu, IMenuMetadata> lazy) {
			this.lazy = lazy;
		}
	}

	sealed class MenuManager : IMenuManager {
		readonly IApp app;

		public bool IsMenuOpened {
			get { return menuOpenedCounter != 0; }
		}
		int menuOpenedCounter;

		void MenuOpened() {
			Debug.Assert(menuOpenedCounter >= 0);
			// Can be 2 when switching from one menu to another one
			menuOpenedCounter++;
		}

		void MenuClosed() {
			Debug.Assert(menuOpenedCounter >= 1);
			menuOpenedCounter--;
		}

		public MenuManager(IApp app) {
			this.app = app;
			this.guidToGroups = null;
		}

		public void InitializeContextMenu(FrameworkElement elem, Guid guid, IGuidObjectsCreator creator, IContextMenuInitializer initCtxMenu) {
			Debug.Assert(guid != Guid.Empty);
			new ContextMenuCreator(this, elem, guid, creator, initCtxMenu);
		}

		void InitializeMenuItemObjects() {
			if (guidToGroups != null)
				return;

			InitializeMenus();
			Debug.Assert(guidToMenu != null);
			InitializeMenuItems();
			Debug.Assert(guidToGroups != null);
		}

		void InitializeMenus() {
			guidToMenu = new Dictionary<Guid, List<MenuMD>>();
			foreach (var item in app.CompositionContainer.GetExports<IMenu, IMenuMetadata>()) {
				string ownerGuidString = item.Metadata.OwnerGuid ?? MenuConstants.APP_MENU_GUID;
				Guid ownerGuid;
				bool b = Guid.TryParse(ownerGuidString, out ownerGuid);
				Debug.Assert(b, string.Format("Menu: Couldn't parse OwnerGuid property: '{0}'", ownerGuidString));
				if (!b)
					continue;

				string guidString = item.Metadata.Guid;
				Guid guid;
				b = Guid.TryParse(guidString, out guid);
				Debug.Assert(b, string.Format("Menu: Couldn't parse Guid property: '{0}'", guidString));
				if (!b)
					continue;

				string header = item.Metadata.Header;
				b = !string.IsNullOrEmpty(header);
				Debug.Assert(b, "Menu: Header is null or empty");
				if (!b)
					continue;

				List<MenuMD> list;
				if (!guidToMenu.TryGetValue(ownerGuid, out list))
					guidToMenu.Add(ownerGuid, list = new List<MenuMD>());
				list.Add(new MenuMD(item));
			}

			foreach (var list in guidToMenu.Values) {
				var hash = new HashSet<Guid>();
				var origList = new List<MenuMD>(list);
				list.Clear();
				foreach (var menu in origList) {
					var guid = new Guid(menu.Metadata.Guid);
					if (hash.Contains(guid))
						continue;
					hash.Add(guid);
					list.Add(menu);
				}
				list.Sort((a, b) => a.Metadata.Order.CompareTo(b.Metadata.Order));
			}
		}
		Dictionary<Guid, List<MenuMD>> guidToMenu;

		void InitializeMenuItems() {
			var dict = new Dictionary<Guid, Dictionary<string, MenuItemGroupMD>>();
			foreach (var item in app.CompositionContainer.GetExports<IMenuItem, IMenuItemMetadata>()) {
				string ownerGuidString = item.Metadata.OwnerGuid ?? MenuConstants.CTX_MENU_GUID;
				Guid ownerGuid;
				bool b = Guid.TryParse(ownerGuidString, out ownerGuid);
				Debug.Assert(b, string.Format("MenuItem: Couldn't parse OwnerGuid property: '{0}'", ownerGuidString));
				if (!b)
					continue;

				string guidString = item.Metadata.Guid;
				if (guidString != null) {
					Guid guid;
					b = Guid.TryParse(guidString, out guid);
					Debug.Assert(b, string.Format("MenuItem: Couldn't parse Guid property: '{0}'", guidString));
					if (!b)
						continue;
				}

				b = !string.IsNullOrEmpty(item.Metadata.Group);
				Debug.Assert(b, "MenuItem: Group property is empty or null");
				if (!b)
					continue;
				double groupOrder;
				string groupName;
				b = ParseGroup(item.Metadata.Group, out groupOrder, out groupName);
				Debug.Assert(b, "MenuItem: Group property must be of the format \"<order>,<name>\" where <order> is a System.Double");
				if (!b)
					continue;

				Dictionary<string, MenuItemGroupMD> groupDict;
				if (!dict.TryGetValue(ownerGuid, out groupDict))
					dict.Add(ownerGuid, groupDict = new Dictionary<string, MenuItemGroupMD>());
				MenuItemGroupMD mdGroup;
				if (!groupDict.TryGetValue(groupName, out mdGroup))
					groupDict.Add(groupName, mdGroup = new MenuItemGroupMD(groupOrder));
				Debug.Assert(mdGroup.Order == groupOrder, string.Format("MenuItem: Group order is different: {0} vs {1}", mdGroup.Order, groupOrder));
				mdGroup.Items.Add(new MenuItemMD(item));
			}

			guidToGroups = new Dictionary<Guid, List<MenuItemGroupMD>>();
			foreach (var kv in dict) {
				var groups = new List<MenuItemGroupMD>(kv.Value.Select(a => a.Value).OrderBy(a => a.Order));
				foreach (var g in groups)
					g.Items.Sort((a, b) => a.Metadata.Order.CompareTo(b.Metadata.Order));
				guidToGroups.Add(kv.Key, groups);
			}
		}
		Dictionary<Guid, List<MenuItemGroupMD>> guidToGroups;

		internal static bool ParseGroup(string s, out double order, out string name) {
			order = 0;
			name = string.Empty;
			int index = s.IndexOf(',');
			if (index < 0)
				return false;
			if (!double.TryParse(s.Substring(0, index), out order))
				return false;
			name = s.Substring(index + 1).Trim();
			return name != string.Empty;
		}

		WeakReference prevEventArgs = new WeakReference(null);
		internal bool? ShowContextMenu(object evArgs, FrameworkElement ctxMenuElem, Guid topLevelMenuGuid, Guid ownerMenuGuid, GuidObject creatorObject, IGuidObjectsCreator creator, IContextMenuInitializer initCtxMenu, bool openedFromKeyboard) {
			InitializeMenuItemObjects();

			// There could be nested contex menu handler calls, eg. first text editor followed by
			// the TabControl. We don't wan't the TabControl to disable the text editor's ctx menu.
			if (prevEventArgs.Target == evArgs)
				return null;

			var ctx = new MenuItemContext(topLevelMenuGuid, openedFromKeyboard, creatorObject, creator == null ? null : creator.GetGuidObjects(creatorObject, openedFromKeyboard));

			List<MenuItemGroupMD> groups;
			bool b = guidToGroups.TryGetValue(ownerMenuGuid, out groups);
			Debug.Assert(b);
			if (!b)
				return false;

			var menu = new ContextMenu();
			var allItems = CreateMenuItems(ctx, groups, null, null);
			if (allItems.Count == 0)
				return false;
			foreach (var i in allItems)
				menu.Items.Add(i);

			menu.Opened += (s, e) => MenuOpened();
			menu.Closed += (s, e) => { MenuClosed(); ctxMenuElem.ContextMenu = new ContextMenu(); };
			if (initCtxMenu != null)
				initCtxMenu.Initialize(ctx, menu);
			ctxMenuElem.ContextMenu = menu;
			prevEventArgs.Target = evArgs;
			return true;
		}

		List<object> CreateMenuItems(IMenuItemContext ctx, List<MenuItemGroupMD> groups, IInputElement commandTarget, MenuItem firstMenuItem) {
			var allItems = new List<object>();

			var items = new List<MenuItemMD>();
			bool needSeparator = false;
			foreach (var group in groups) {
				items.Clear();
				foreach (var item in group.Items) {
					if (item.MenuItem.IsVisible(ctx))
						items.Add(item);
				}
				if (items.Count == 0)
					continue;
				if (needSeparator)
					allItems.Add(new Separator());
				needSeparator = true;

				foreach (var item in items) {
					var itemCreator = item.MenuItem as IMenuItemCreator;
					if (itemCreator != null) {
						foreach (var createdItem in itemCreator.Create(ctx)) {
							var menuItem = Create(createdItem.MenuItem, createdItem.Metadata, ctx, commandTarget, firstMenuItem);
							firstMenuItem = null;
							allItems.Add(menuItem);
						}
					}
					else {
						var menuItem = Create(item.MenuItem, item.Metadata, ctx, commandTarget, firstMenuItem);
						firstMenuItem = null;
						allItems.Add(menuItem);
					}
				}
			}

			return allItems;
		}

		MenuItem Create(IMenuItem item, IMenuItemMetadata metadata, IMenuItemContext ctx, IInputElement commandTarget, MenuItem menuItem) {
			if (menuItem == null)
				menuItem = new MenuItem();
			menuItem.CommandTarget = commandTarget;

			string header = metadata.Header;
			string inputGestureText = metadata.InputGestureText;
			string iconName = metadata.Icon;

			var mi2 = item as IMenuItem2;
			if (mi2 != null) {
				header = mi2.GetHeader(ctx) ?? header;
				inputGestureText = mi2.GetInputGestureText(ctx) ?? inputGestureText;
				iconName = mi2.GetIcon(ctx) ?? iconName;
				menuItem.IsChecked = mi2.IsChecked(ctx);
			}

			menuItem.Header = header;
			menuItem.InputGestureText = inputGestureText;

			bool isCtxMenu = ctx.MenuGuid == new Guid(MenuConstants.CTX_MENU_GUID);
			var cmdHolder = item as ICommandHolder;
			bool lastIsEnabledCallValue = false;
			if (!string.IsNullOrEmpty(iconName)) {
				if (cmdHolder == null)
					lastIsEnabledCallValue = item.IsEnabled(ctx);
				else {
					var routedCommand = cmdHolder.Command as RoutedCommand;
					lastIsEnabledCallValue = commandTarget == null || routedCommand == null || routedCommand.CanExecute(ctx, commandTarget);
				}
				app.ImageManager.Add16x16Image(menuItem, item.GetType().Assembly, iconName, isCtxMenu, lastIsEnabledCallValue);
			}

			if (metadata.Guid != null) {
				var itemGuid = Guid.Parse(metadata.Guid);
				List<MenuItemGroupMD> list;
				if (guidToGroups.TryGetValue(itemGuid, out list)) {
					menuItem.Items.Add(new MenuItem());
					menuItem.SubmenuOpened += (s, e) => {
						if (e.Source == menuItem)
							InitializeSubMenu(menuItem, ctx, itemGuid, commandTarget);
					};
					menuItem.SubmenuClosed += (s, e) => {
						if (e.Source == menuItem) {
							menuItem.Items.Clear();
							menuItem.Items.Add(new MenuItem());
						}
					};
				}
			}

			menuItem.Command = cmdHolder != null ? cmdHolder.Command : new RelayCommand(a => item.Execute(ctx), a => {
				bool b = item.IsEnabled(ctx);
				if (lastIsEnabledCallValue != b && !string.IsNullOrEmpty(iconName))
					app.ImageManager.Add16x16Image(menuItem, item.GetType().Assembly, iconName, isCtxMenu, lastIsEnabledCallValue = b);
				return b;
			});

			return menuItem;
		}

		void Reinitialize(MenuItem menuItem) {
			// To trigger this condition: Open the menu, then hold down LEFT or RIGHT for a few secs
			MenuItem first;
			if (menuItem.Items.Count != 1 || (first = menuItem.Items[0] as MenuItem) == null || first.Header != null) {
				menuItem.Items.Clear();
				menuItem.Items.Add(new MenuItem());
			}
		}

		void InitializeSubMenu(MenuItem menuItem, IMenuItemContext ctx, Guid ownerMenuGuid, IInputElement commandTarget) {
			Reinitialize(menuItem);

			List<MenuItemGroupMD> groups;
			bool b = guidToGroups.TryGetValue(ownerMenuGuid, out groups);
			Debug.Assert(b);
			if (b) {
				var firstMenuItem = menuItem.Items.Count == 1 ? menuItem.Items[0] as MenuItem : null;
				var allItems = CreateMenuItems(ctx, groups, commandTarget, firstMenuItem);
				foreach (var i in allItems) {
					if (firstMenuItem != i)
						menuItem.Items.Add(i);
				}
			}
		}

		void InitializeMainSubMenu(MenuItem menuItem, MenuMD md, IInputElement commandTarget) {
			Reinitialize(menuItem);

			List<MenuItemGroupMD> groups;
			var guid = new Guid(md.Metadata.Guid);
			bool b = guidToGroups.TryGetValue(guid, out groups);
			Debug.Assert(b);
			if (b) {
				var ctx = new MenuItemContext(guid, true, new GuidObject(guid, null), null);
				var firstMenuItem = menuItem.Items.Count == 1 ? menuItem.Items[0] as MenuItem : null;
				var allItems = CreateMenuItems(ctx, groups, commandTarget, firstMenuItem);
				foreach (var i in allItems) {
					if (firstMenuItem != i)
						menuItem.Items.Add(i);
				}
			}
		}

		public Menu CreateMenu(Guid menuGuid, IInputElement commandTarget) {
			InitializeMenuItemObjects();

			var menu = new Menu();

			List<MenuMD> list;
			if (!guidToMenu.TryGetValue(menuGuid, out list))
				return menu;

			foreach (var md in list) {
				var guid = new Guid(md.Metadata.Guid);
				List<MenuItemGroupMD> itemGroups;
				if (!guidToGroups.TryGetValue(guid, out itemGroups))
					continue;

				var topMenuItem = new MenuItem() { Header = md.Metadata.Header };
				topMenuItem.Items.Add(new MenuItem());
				topMenuItem.SubmenuOpened += (s, e) => {
					if (e.Source == topMenuItem) {
						MenuOpened();
						InitializeMainSubMenu(topMenuItem, md, commandTarget);
					}
				};
				topMenuItem.SubmenuClosed += (s, e) => {
					if (e.Source == topMenuItem) {
						MenuClosed();
						// There must always be exactly one MenuItem in the list when it's not shown.
						// We must re-use the first one or the first menu item won't be highlighted
						// when the menu is opened from the keyboard, eg. Alt+F.
						topMenuItem.Items.Clear();
						topMenuItem.Items.Add(new MenuItem());
					}
				};
				menu.Items.Add(topMenuItem);
			}

			return menu;
		}
	}
}
