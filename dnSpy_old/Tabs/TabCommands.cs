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
using System.Windows.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Shared.UI.Menus;
using dnSpy.TreeNodes;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.TreeView;

namespace dnSpy.Tabs {
	sealed class TabGroupContext {
		public readonly TabControl TabControl;

		public TabGroupContext(TabControl tabControl) {
			this.TabControl = tabControl;
		}
	}

	abstract class CtxMenuTabGroupCommand : MenuItemBase<TabGroupContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override TabGroupContext CreateContext(IMenuItemContext context) {
			return CreateContextInternal(context);
		}

		static TabGroupContext CreateContextInternal(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TABCONTROL_GUID))
				return null;
			var tabControl = context.CreatorObject.Object as TabControl;
			if (!MainWindow.Instance.IsDecompilerTabControl(tabControl))
				return null;
			return new TabGroupContext(tabControl);
		}
	}

	[ExportMenuItem(Header = "_Close", InputGestureText = "Ctrl+W", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 10)]
	sealed class CloseTabCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.CloseActiveTabCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.CloseActiveTab();
		}
	}

	[ExportMenuItem(Header = "C_lose All Tabs", Icon = "CloseDocuments", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 20)]
	sealed class CloseAllTabsCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.CloseAllTabsCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.CloseAllTabs();
		}
	}

	[ExportMenuItem(Header = "Close _All But This", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 30)]
	sealed class CloseAllTabsButThisCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.ActiveTabState != null;
		}

		public override bool IsEnabled(TabGroupContext context) {
			return MainWindow.Instance.CloseAllButActiveTabCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.CloseAllButActiveTab();
		}
	}

	[ExportMenuItem(Header = "New _Tab", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 40)]
	sealed class NewTabCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.CloneActiveTabCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.CloneActiveTab();
		}
	}

	[ExportMenuItem(Header = "New Hori_zontal Tab Group", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 0)]
	sealed class NewHorizontalTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.NewHorizontalTabGroupCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.NewHorizontalTabGroup();
		}
	}

	[ExportMenuItem(Header = "New _Vertical Tab Group", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 10)]
	sealed class NewVerticalTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.NewVerticalTabGroupCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.NewVerticalTabGroup();
		}
	}

	[ExportMenuItem(Header = "Move to Ne_xt Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 20)]
	sealed class MoveToNextTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.MoveToNextTabGroupCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.MoveToNextTabGroup();
		}
	}

	[ExportMenuItem(Header = "Move All to Next Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 30)]
	sealed class MoveAllToNextTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.MoveAllToNextTabGroupCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.MoveAllToNextTabGroup();
		}
	}

	[ExportMenuItem(Header = "Move to P_revious Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 40)]
	sealed class MoveToPreviousTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.MoveToPreviousTabGroupCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.MoveToPreviousTabGroup();
		}
	}

	[ExportMenuItem(Header = "Move All to Previous Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 50)]
	sealed class MoveAllToPreviousTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.MoveAllToPreviousTabGroupCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.MoveAllToPreviousTabGroup();
		}
	}

	[ExportMenuItem(Header = "Close Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 0)]
	sealed class CloseTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.CloseTabGroupCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.CloseTabGroup();
		}
	}

	[ExportMenuItem(Header = "Close All Tab Groups But This", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 10)]
	sealed class CloseAllTabGroupsButThisCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.CloseAllTabGroupsButThisCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.CloseAllTabGroupsButThis();
		}
	}

	[ExportMenuItem(Header = "Move Tab Group After Next Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 20)]
	sealed class MoveTabGroupAfterNextTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.MoveTabGroupAfterNextTabGroupCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.MoveTabGroupAfterNextTabGroup();
		}
	}

	[ExportMenuItem(Header = "Move Tab Group Before Previous Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 30)]
	sealed class MoveTabGroupBeforePreviousTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.MoveTabGroupBeforePreviousTabGroupCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.MoveTabGroupBeforePreviousTabGroup();
		}
	}

	[ExportMenuItem(Header = "Merge All Tab Groups", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 40)]
	sealed class MergeAllTabGroupsCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.MergeAllTabGroupsCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.MergeAllTabGroups();
		}
	}

	[ExportMenuItem(Header = "Use Vertical Tab Groups", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_CTX_TABS_GROUPSVERT, Order = 0)]
	sealed class UseVerticalTabGroupsCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.UseVerticalTabGroupsCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.UseVerticalTabGroups();
		}
	}

	[ExportMenuItem(Header = "Use Horizontal Tab Groups", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_CTX_TABS_GROUPSVERT, Order = 10)]
	sealed class UseHorizontalTabGroupsCtxMenuCommand : CtxMenuTabGroupCommand {
		public override bool IsVisible(TabGroupContext context) {
			return MainWindow.Instance.UseHorizontalTabGroupsCanExecute();
		}

		public override void Execute(TabGroupContext context) {
			MainWindow.Instance.UseHorizontalTabGroups();
		}
	}

	[ExportMenuItem(Header = "Open in New _Tab", InputGestureText = "Ctrl+T", Group = MenuConstants.GROUP_CTX_FILES_TABS, Order = 0)]
	sealed class OpenInNewTabCtxMenuCommand : MenuItemBase {
		public override bool IsVisible(IMenuItemContext context) {
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID) &&
				context.FindArrayOrDefaultByType<SharpTreeNode>().Length > 0;
		}

		public override void Execute(IMenuItemContext context) {
			MainWindow.Instance.OpenNewTab();
		}
	}

	[ExportMenuItem(Header = "Open in New _Tab", Group = MenuConstants.GROUP_CTX_CODE_TABS, Order = 0)]
	sealed class OpenReferenceInNewTabCtxMenuCommand : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			var @ref = GetReference(context);
			if (@ref != null)
				MainWindow.Instance.OpenReferenceInNewTab(context.CreatorObject.Object as DecompilerTextView, @ref);
		}

		public override bool IsVisible(IMenuItemContext context) {
			return GetReference(context) != null;
		}

		static ReferenceSegment GetReference(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DECOMPILED_CODE_GUID))
				return null;
			return context.FindByType<ReferenceSegment>();
		}

		public override string GetInputGestureText(IMenuItemContext context) {
			return context.OpenedFromKeyboard ? "Ctrl+F12" : "Ctrl+Click";
		}
	}

	static class OpenReferenceCtxMenuCommand {
		internal static void ExecuteInternal(object @ref, bool newTab) {
			if (@ref == null)
				return;
			if (newTab)
				MainWindow.Instance.OpenNewEmptyTab();
			var textView = MainWindow.Instance.SafeActiveTextView;
			MainWindow.Instance.JumpToReference(textView, @ref);
			MainWindow.Instance.SetTextEditorFocus(textView);
		}

		[ExportMenuItem(Header = "Go to Reference", InputGestureText = "Dbl Click", Group = MenuConstants.GROUP_CTX_SEARCH_TABS, Order = 0)]
		sealed class SearchCommand : MenuItemBase {
			public override void Execute(IMenuItemContext context) {
				ExecuteInternal(GetReference(context), false);
			}

			public override bool IsVisible(IMenuItemContext context) {
				return GetReference(context) != null;
			}

			internal static object GetReference(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_SEARCH_GUID))
					return null;
				var @ref = context.FindByType<CodeReferenceSegment>();
				return @ref == null ? null : @ref.Reference;
			}
		}

		[ExportMenuItem(Header = "Open in New _Tab", InputGestureText = "Shift+Dbl Click", Group = MenuConstants.GROUP_CTX_SEARCH_TABS, Order = 10)]
		sealed class NewTabSearchCommand : MenuItemBase {
			public override void Execute(IMenuItemContext context) {
				ExecuteInternal(SearchCommand.GetReference(context), true);
			}

			public override bool IsVisible(IMenuItemContext context) {
				return SearchCommand.GetReference(context) != null;
			}
		}

		[ExportMenuItem(Header = "Go to Reference", InputGestureText = "Dbl Click", Group = MenuConstants.GROUP_CTX_ANALYZER_TABS, Order = 0)]
		sealed class AnalyzerCommand : MenuItemBase {
			public override void Execute(IMenuItemContext context) {
				ExecuteInternal(GetReference(context), false);
			}

			public override bool IsVisible(IMenuItemContext context) {
				return GetReference(context) != null;
			}

			internal static object GetReference(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_ANALYZER_GUID))
					return null;

				var nodes = context.FindByType<SharpTreeNode[]>();
				if (nodes == null || nodes.Length != 1)
					return null;

				var tokenNode = nodes[0] as ITokenTreeNode;
				if (tokenNode != null && tokenNode.MDTokenProvider != null)
					return tokenNode.MDTokenProvider;

				return null;
			}
		}

		[ExportMenuItem(Header = "Open in New _Tab", InputGestureText = "Shift+Dbl Click", Group = MenuConstants.GROUP_CTX_ANALYZER_TABS, Order = 10)]
		sealed class NewTabAnalyzerCommand : MenuItemBase {
			public override void Execute(IMenuItemContext context) {
				ExecuteInternal(AnalyzerCommand.GetReference(context), true);
			}

			public override bool IsVisible(IMenuItemContext context) {
				return AnalyzerCommand.GetReference(context) != null;
			}
		}
	}

	sealed class MenuTabGroupContext {
	}

	abstract class MenuTabGroupCommand : MenuItemBase<MenuTabGroupContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override MenuTabGroupContext CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.APP_MENU_WINDOW_GUID))
				return null;
			return new MenuTabGroupContext();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "_New Window", Icon = "NewWindow", Group = MenuConstants.GROUP_APP_MENU_WINDOW_WINDOW, Order = 0)]
	sealed class NewWindowCommand : MenuTabGroupCommand {
		public override bool IsEnabled(MenuTabGroupContext context) {
			return MainWindow.Instance.CloneActiveTabCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.CloneActiveTab();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "_Close", InputGestureText = "Ctrl+W", Group = MenuConstants.GROUP_APP_MENU_WINDOW_WINDOW, Order = 10)]
	sealed class CloseTabCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.CloseActiveTabCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.CloseActiveTab();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "New Hori_zontal Tab Group", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 0)]
	sealed class NewHorizontalTabGroupCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.NewHorizontalTabGroupCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.NewHorizontalTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "New _Vertical Tab Group", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 10)]
	sealed class NewVerticalTabGroupCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.NewVerticalTabGroupCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.NewVerticalTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move to Ne_xt Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 20)]
	sealed class MoveToNextTabGroupCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.MoveToNextTabGroupCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.MoveToNextTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move All to Next Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 30)]
	sealed class MoveAllToNextTabGroupCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.MoveAllToNextTabGroupCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.MoveAllToNextTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move to P_revious Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 40)]
	sealed class MoveToPreviousTabGroupCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.MoveToPreviousTabGroupCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.MoveToPreviousTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move All to Previous Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 50)]
	sealed class MoveAllToPreviousTabGroupCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.MoveAllToPreviousTabGroupCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.MoveAllToPreviousTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "C_lose All Tabs", Icon = "CloseDocuments", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 60)]
	sealed class CloseAllTabsCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return true;
		}

		public override bool IsEnabled(MenuTabGroupContext context) {
			return MainWindow.Instance.CloseAllTabsCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.CloseAllTabs();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Close Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 0)]
	sealed class CloseTabGroupCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.CloseTabGroupCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.CloseTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Close All Tab Groups But This", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 10)]
	sealed class CloseAllTabGroupsButThisCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.CloseAllTabGroupsButThisCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.CloseAllTabGroupsButThis();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move Tab Group After Next Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 20)]
	sealed class MoveTabGroupAfterNextTabGroupCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.MoveTabGroupAfterNextTabGroupCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.MoveTabGroupAfterNextTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move Tab Group Before Previous Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 30)]
	sealed class MoveTabGroupBeforePreviousTabGroupCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.MoveTabGroupBeforePreviousTabGroupCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.MoveTabGroupBeforePreviousTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Merge All Tab Groups", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 40)]
	sealed class MergeAllTabGroupsCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.MergeAllTabGroupsCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.MergeAllTabGroups();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Use Vertical Tab Groups", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSVERT, Order = 0)]
	sealed class UseVerticalTabGroupsCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.UseVerticalTabGroupsCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.UseVerticalTabGroups();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Use Horizontal Tab Groups", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSVERT, Order = 10)]
	sealed class UseHorizontalTabGroupsCommand : MenuTabGroupCommand {
		public override bool IsVisible(MenuTabGroupContext context) {
			return MainWindow.Instance.UseHorizontalTabGroupsCanExecute();
		}

		public override void Execute(MenuTabGroupContext context) {
			MainWindow.Instance.UseHorizontalTabGroups();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Group = MenuConstants.GROUP_APP_MENU_WINDOW_ALLWINDOWS, Order = 0)]
	sealed class DecompilerWindowsCommand : MenuTabGroupCommand, IMenuItemCreator {
		public override void Execute(MenuTabGroupContext context) {
		}

		sealed class MyMenuItem : MenuItemBase {
			readonly Action<IMenuItemContext> action;
			readonly bool isChecked;

			public MyMenuItem(Action<IMenuItemContext> action, bool isChecked) {
				this.action = action;
				this.isChecked = isChecked;
			}

			public override void Execute(IMenuItemContext context) {
				action(context);
			}

			public override bool IsChecked(IMenuItemContext context) {
				return isChecked;
			}
		}

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			const int MAX_TABS = 10;
			int index = 0;
			foreach (var tabState in MainWindow.Instance.GetTabStateInOrder()) {
				var header = GetHeader(index + 1, tabState);
				var attr = new ExportMenuItemAttribute { Header = header };
				var tabStateTmp = tabState;
				var item = new MyMenuItem(ctx => MainWindow.Instance.SetActiveTab(tabStateTmp), index == 0);

				yield return new CreatedMenuItem(attr, item);

				if (++index >= MAX_TABS)
					break;
			}

			var attr2 = new ExportMenuItemAttribute { Header = "_Windows..." };
			var item2 = new MyMenuItem(ctx => MainWindow.Instance.ShowDecompilerTabsWindow(), false);
			yield return new CreatedMenuItem(attr2, item2);
		}

		static string GetHeader(int i, TabState tabState) {
			string s;
			if (i == 10)
				s = "1_0";
			else if (i > 10)
				s = i.ToString();
			else
				s = string.Format("_{0}", i);
			return string.Format("{0} {1}", s, UIUtils.EscapeMenuItemHeader(tabState.ShortHeader));
		}
	}
}
