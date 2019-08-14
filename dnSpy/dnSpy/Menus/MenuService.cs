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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Resources;
using dnSpy.Images;

namespace dnSpy.Menus {
	sealed class MenuItemMD {
		readonly Lazy<IMenuItem, IMenuItemMetadata> lazy;

		public IMenuItem MenuItem => lazy.Value;
		public IMenuItemMetadata Metadata => lazy.Metadata;

		public MenuItemMD(Lazy<IMenuItem, IMenuItemMetadata> lazy) => this.lazy = lazy;
	}

	sealed class MenuItemGroupMD {
		public readonly double Order;
		public readonly List<MenuItemMD> Items;

		public MenuItemGroupMD(double order) {
			Order = order;
			Items = new List<MenuItemMD>();
		}
	}

	sealed class MenuMD {
		readonly Lazy<IMenu, IMenuMetadata> lazy;

		public IMenu Menu => lazy.Value;
		public IMenuMetadata Metadata => lazy.Metadata;

		public MenuMD(Lazy<IMenu, IMenuMetadata> lazy) => this.lazy = lazy;
	}

	[Export(typeof(IWpfFocusChecker))]
	sealed class MenuWpfFocusChecker : IWpfFocusChecker {
		public bool CanFocus => !InputManager.Current.IsInMenuMode;

		readonly MenuService menuService;

		[ImportingConstructor]
		MenuWpfFocusChecker(MenuService menuService) => this.menuService = menuService;
	}

	[Export, Export(typeof(IMenuService))]
	sealed class MenuService : IMenuService {
		[ImportingConstructor]
		MenuService([ImportMany] IEnumerable<Lazy<IMenu, IMenuMetadata>> mefMenus, [ImportMany] IEnumerable<Lazy<IMenuItem, IMenuItemMetadata>> mefMenuItems) {
			guidToGroups = null;
			this.mefMenus = mefMenus;
			this.mefMenuItems = mefMenuItems;
		}

		public IContextMenuProvider InitializeContextMenu(FrameworkElement elem, Guid guid, IGuidObjectsProvider? provider, IContextMenuInitializer? initCtxMenu, Guid? ctxMenuGuid) {
			Debug.Assert(guid != Guid.Empty);
			return new ContextMenuProvider(this, elem, guid, provider, initCtxMenu, ctxMenuGuid);
		}

		public IContextMenuProvider InitializeContextMenu(FrameworkElement elem, string guid, IGuidObjectsProvider? provider, IContextMenuInitializer? initCtxMenu, string? ctxMenuGuid) =>
			InitializeContextMenu(elem, new Guid(guid), provider, initCtxMenu, ctxMenuGuid is null ? (Guid?)null : new Guid(ctxMenuGuid));

		void InitializeMenuItemObjects() {
			if (!(guidToGroups is null))
				return;

			InitializeMenus();
			Debug2.Assert(!(guidToMenu is null));
			InitializeMenuItems();
			Debug2.Assert(!(guidToGroups is null));
		}

		void InitializeMenus() {
			guidToMenu = new Dictionary<Guid, List<MenuMD>>();
			foreach (var item in mefMenus) {
				string ownerGuidString = item.Metadata.OwnerGuid ?? MenuConstants.APP_MENU_GUID;
				bool b = Guid.TryParse(ownerGuidString, out var ownerGuid);
				Debug.Assert(b, $"Menu: Couldn't parse OwnerGuid property: '{ownerGuidString}'");
				if (!b)
					continue;

				var guidString = item.Metadata.Guid;
				b = Guid.TryParse(guidString, out var guid);
				Debug.Assert(b, $"Menu: Couldn't parse Guid property: '{guidString}'");
				if (!b)
					continue;

				var header = item.Metadata.Header;
				b = !string.IsNullOrEmpty(header);
				Debug2.Assert(b, "Menu: Header is null or empty");
				if (!b)
					continue;

				if (!guidToMenu.TryGetValue(ownerGuid, out var list))
					guidToMenu.Add(ownerGuid, list = new List<MenuMD>());
				list.Add(new MenuMD(item));
			}

			foreach (var list in guidToMenu.Values) {
				var hash = new HashSet<Guid>();
				var origList = new List<MenuMD>(list);
				list.Clear();
				foreach (var menu in origList) {
					var guid = new Guid(menu.Metadata.Guid!);
					if (hash.Contains(guid))
						continue;
					hash.Add(guid);
					list.Add(menu);
				}
				list.Sort((a, b) => a.Metadata.Order.CompareTo(b.Metadata.Order));
			}
		}
		readonly IEnumerable<Lazy<IMenu, IMenuMetadata>> mefMenus;
		Dictionary<Guid, List<MenuMD>>? guidToMenu;

		void InitializeMenuItems() {
			var dict = new Dictionary<Guid, Dictionary<string, MenuItemGroupMD>>();
			foreach (var item in mefMenuItems) {
				string ownerGuidString = item.Metadata.OwnerGuid ?? MenuConstants.CTX_MENU_GUID;
				bool b = Guid.TryParse(ownerGuidString, out var ownerGuid);
				Debug.Assert(b, $"MenuItem: Couldn't parse OwnerGuid property: '{ownerGuidString}'");
				if (!b)
					continue;

				var guidString = item.Metadata.Guid;
				if (!(guidString is null)) {
					b = Guid.TryParse(guidString, out var guid);
					Debug.Assert(b, $"MenuItem: Couldn't parse Guid property: '{guidString}'");
					if (!b)
						continue;
				}

				b = !string.IsNullOrEmpty(item.Metadata.Group);
				Debug.Assert(b, "MenuItem: Group property is empty or null");
				if (!b)
					continue;
				b = ParseGroup(item.Metadata.Group!, out double groupOrder, out string groupName);
				Debug.Assert(b, "MenuItem: Group property must be of the format \"<order>,<name>\" where <order> is a System.Double");
				if (!b)
					continue;

				if (!dict.TryGetValue(ownerGuid, out var groupDict))
					dict.Add(ownerGuid, groupDict = new Dictionary<string, MenuItemGroupMD>());
				if (!groupDict.TryGetValue(groupName, out var mdGroup))
					groupDict.Add(groupName, mdGroup = new MenuItemGroupMD(groupOrder));
				Debug.Assert(mdGroup.Order == groupOrder, $"MenuItem: Group order is different: {mdGroup.Order} vs {groupOrder}");
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
		readonly IEnumerable<Lazy<IMenuItem, IMenuItemMetadata>> mefMenuItems;
		Dictionary<Guid, List<MenuItemGroupMD>>? guidToGroups;

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
		internal bool? ShowContextMenu(object evArgs, FrameworkElement ctxMenuElem, Guid topLevelMenuGuid, Guid ownerMenuGuid, GuidObject creatorObject, IGuidObjectsProvider? provider, IContextMenuInitializer? initCtxMenu, bool openedFromKeyboard) {
			InitializeMenuItemObjects();
			Debug2.Assert(!(guidToGroups is null));

			// There could be nested context menu handler calls, eg. first text editor followed by
			// the TabControl. We don't wan't the TabControl to disable the text editor's ctx menu.
			if (prevEventArgs.Target == evArgs)
				return null;

			var ctx = new MenuItemContext(topLevelMenuGuid, openedFromKeyboard, creatorObject, provider?.GetGuidObjects(new GuidObjectsProviderArgs(creatorObject, openedFromKeyboard)));

			bool b = guidToGroups.TryGetValue(ownerMenuGuid, out var groups);
			if (!b)
				return false;
			Debug2.Assert(!(groups is null));

			var menu = new ContextMenu();
			BindBackgroundBrush(menu, isCtxMenu: true);

			// The owner control could be zoomed (eg. text editor) but the context menu isn't, so make
			// sure we use 100% zoom here (same as the context menu).
			double defaultZoom = 1.0;
			DsImage.SetZoom(menu, defaultZoom);
			// Also make sure we use Display mode. Let MetroWindow handle it since it has some extra checks
			var window = Window.GetWindow(ctxMenuElem) as MetroWindow;
			window?.SetScaleTransform(menu, defaultZoom);

			var allItems = CreateMenuItems(ctx, groups, null, null, true);
			if (allItems.Count == 0)
				return false;
			foreach (var i in allItems)
				menu.Items.Add(i);

			menu.Closed += (s, e) => {
				ctx.Dispose();
				ctxMenuElem.ContextMenu = new ContextMenu();
			};
			if (!(initCtxMenu is null))
				initCtxMenu.Initialize(ctx, menu);
			ctxMenuElem.ContextMenu = menu;
			prevEventArgs.Target = evArgs;
			return true;
		}

		static void BindBackgroundBrush(Control elem, bool isCtxMenu) =>
			elem.SetResourceReference(DsImage.BackgroundBrushProperty, isCtxMenu ? "ContextMenuRectangleFill" : "ToolBarIconVerticalBackground");

		List<object> CreateMenuItems(MenuItemContext ctx, List<MenuItemGroupMD> groups, IInputElement? commandTarget, MenuItem? firstMenuItem, bool isCtxMenu) {
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
					if (item.MenuItem is IMenuItemProvider itemProvider) {
						foreach (var createdItem in itemProvider.Create(ctx)) {
							var menuItem = Create(createdItem.MenuItem, createdItem.Metadata, ctx, commandTarget, firstMenuItem, isCtxMenu);
							firstMenuItem = null;
							allItems.Add(menuItem);
						}
					}
					else {
						var menuItem = Create(item.MenuItem, item.Metadata, ctx, commandTarget, firstMenuItem, isCtxMenu);
						firstMenuItem = null;
						allItems.Add(menuItem);
					}
				}
			}

			return allItems;
		}

		MenuItem Create(IMenuItem item, IMenuItemMetadata metadata, MenuItemContext ctx, IInputElement? commandTarget, MenuItem? menuItem, bool isCtxMenu) {
			Debug2.Assert(!(guidToGroups is null));
			if (menuItem is null)
				menuItem = new MenuItem();
			menuItem.CommandTarget = commandTarget;

			var header = ResourceHelper.GetStringOrNull(item, metadata.Header);
			var inputGestureText = ResourceHelper.GetStringOrNull(item, metadata.InputGestureText);
			var iconImgRef = ImageReferenceHelper.GetImageReference(item, metadata.Icon);

			header = item.GetHeader(ctx) ?? header;
			inputGestureText = item.GetInputGestureText(ctx) ?? inputGestureText;
			iconImgRef = item.GetIcon(ctx) ?? iconImgRef;
			menuItem.IsChecked = item.IsChecked(ctx);

			menuItem.Header = header;
			menuItem.InputGestureText = inputGestureText;

			var cmdHolder = item as ICommandHolder;
			bool lastIsEnabledCallValue = false;
			if (!(iconImgRef is null)) {
				if (cmdHolder is null)
					lastIsEnabledCallValue = item.IsEnabled(ctx);
				else {
					var routedCommand = cmdHolder.Command as RoutedCommand;
					lastIsEnabledCallValue = commandTarget is null || routedCommand is null || routedCommand.CanExecute(ctx, commandTarget);
				}
				Add16x16Image(menuItem, iconImgRef.Value, lastIsEnabledCallValue);
			}

			if (!(metadata.Guid is null)) {
				var itemGuid = Guid.Parse(metadata.Guid);
				if (guidToGroups.ContainsKey(itemGuid)) {
					menuItem.Items.Add(new MenuItem());
					menuItem.SubmenuOpened += (s, e) => {
						if (e.Source == menuItem)
							InitializeSubMenu(menuItem, ctx, itemGuid, commandTarget, isCtxMenu);
					};
					menuItem.SubmenuClosed += (s, e) => {
						if (e.Source == menuItem) {
							menuItem.Items.Clear();
							menuItem.Items.Add(new MenuItem());
						}
					};
				}
			}

			ctx.OnDisposed += (_, __) => {
				// Buggy automation peers could hold a reference to us, so clear the captured variables (we can't clear the captured 'this')
				menuItem = null;
				ctx = null!;
				commandTarget = null;
				item = null!;
				iconImgRef = null;
			};

			menuItem.Command = !(cmdHolder is null) ? cmdHolder.Command : new RelayCommand(a => {
				Debug.Assert(!ctx.IsDisposed);
				if (ctx?.IsDisposed == false) {
					item.Execute(ctx);
					ctx?.Dispose();
				}
			}, a => {
				if (ctx?.IsDisposed != false)
					return false;
				bool b = item.IsEnabled(ctx);
				if (lastIsEnabledCallValue != b && !(iconImgRef is null))
					Add16x16Image(menuItem, iconImgRef.Value, lastIsEnabledCallValue = b);
				return b;
			});

			return menuItem;
		}

		static void Add16x16Image(MenuItem menuItem, ImageReference imageReference, bool? enable = null) {
			var image = new DsImage { ImageReference = imageReference };
			menuItem.Icon = image;
			if (enable == false)
				image.Opacity = 0.3;
		}

		void Reinitialize(MenuItem menuItem) {
			// To trigger this condition: Open the menu, then hold down LEFT or RIGHT for a few secs
			if (menuItem.Items.Count != 1 || !(menuItem.Items[0] is MenuItem first) || !(first.Header is null)) {
				menuItem.Items.Clear();
				menuItem.Items.Add(new MenuItem());
			}
		}

		void InitializeSubMenu(MenuItem menuItem, MenuItemContext ctx, Guid ownerMenuGuid, IInputElement? commandTarget, bool isCtxMenu) {
			Reinitialize(menuItem);
			Debug2.Assert(!(guidToGroups is null));

			bool b = guidToGroups.TryGetValue(ownerMenuGuid, out var groups);
			Debug.Assert(b);
			if (b) {
				Debug2.Assert(!(groups is null));
				BindBackgroundBrush(menuItem, isCtxMenu);
				var firstMenuItem = menuItem.Items.Count == 1 ? menuItem.Items[0] as MenuItem : null;
				var allItems = CreateMenuItems(ctx, groups, commandTarget, firstMenuItem, isCtxMenu);
				foreach (var i in allItems) {
					if (firstMenuItem != i)
						menuItem.Items.Add(i);
				}
			}
		}

		MenuItemContext? InitializeMainSubMenu(MenuItem menuItem, MenuMD md, IInputElement? commandTarget) {
			Reinitialize(menuItem);
			Debug2.Assert(!(guidToGroups is null));

			var guid = new Guid(md.Metadata.Guid!);
			bool b = guidToGroups.TryGetValue(guid, out var groups);
			Debug.Assert(b);
			if (b) {
				Debug2.Assert(!(groups is null));
				BindBackgroundBrush(menuItem, isCtxMenu: false);
				var ctx = new MenuItemContext(guid, true, new GuidObject(guid, null), null);
				var firstMenuItem = menuItem.Items.Count == 1 ? menuItem.Items[0] as MenuItem : null;
				var allItems = CreateMenuItems(ctx, groups, commandTarget, firstMenuItem, false);
				foreach (var i in allItems) {
					if (firstMenuItem != i)
						menuItem.Items.Add(i);
				}
				return ctx;
			}

			return null;
		}

		public Menu CreateMenu(Guid menuGuid, IInputElement? commandTarget) {
			InitializeMenuItemObjects();
			Debug2.Assert(!(guidToGroups is null));
			Debug2.Assert(!(guidToMenu is null));

			var menu = new Menu();

			if (!guidToMenu.TryGetValue(menuGuid, out var list))
				return menu;

			foreach (var md in list) {
				var guid = new Guid(md.Metadata.Guid!);
				if (!guidToGroups.TryGetValue(guid, out var itemGroups))
					continue;

				var topMenuItem = new MenuItem() { Header = ResourceHelper.GetStringOrNull(md.Menu, md.Metadata.Header) };
				topMenuItem.Items.Add(new MenuItem());
				var mdTmp = md;
				MenuItemContext? ctxTmp = null;
				topMenuItem.SubmenuOpened += (s, e) => {
					if (e.Source == topMenuItem) {
						ctxTmp?.Dispose();
						ctxTmp = InitializeMainSubMenu(topMenuItem, mdTmp, commandTarget);
					}
				};
				topMenuItem.SubmenuClosed += (s, e) => {
					if (e.Source == topMenuItem) {
						Debug2.Assert(!(ctxTmp is null));
						ctxTmp?.Dispose();
						ctxTmp = null;

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
