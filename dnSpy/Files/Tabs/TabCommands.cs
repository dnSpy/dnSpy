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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Plugin;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.TreeView;
using dnSpy.Files.Tabs.Dialogs;
using dnSpy.Shared.UI.Menus;

namespace dnSpy.Files.Tabs {
	[ExportAutoLoaded]
	sealed class InstallTabCommands : IAutoLoaded {
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		InstallTabCommands(IAppWindow appWindow, IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
			var cmds = appWindow.MainWindowCommands;
			cmds.Add(new RoutedCommand("OpenNewTab", typeof(InstallTabCommands)), (s, e) => OpenNewTab(), (s, e) => e.CanExecute = CanOpenNewTab, ModifierKeys.Control, Key.T);
			cmds.Add(new RoutedCommand("CloseActiveTab", typeof(InstallTabCommands)), (s, e) => CloseActiveTab(), (s, e) => e.CanExecute = CanCloseActiveTab, ModifierKeys.Control, Key.W, ModifierKeys.Control, Key.F4);
			cmds.Add(new RoutedCommand("SelectNextTab", typeof(InstallTabCommands)), (s, e) => SelectNextTab(), (s, e) => e.CanExecute = CanSelectNextTab, ModifierKeys.Control, Key.Tab);
			cmds.Add(new RoutedCommand("SelectPrevTab", typeof(InstallTabCommands)), (s, e) => SelectPrevTab(), (s, e) => e.CanExecute = CanSelectPrevTab, ModifierKeys.Control | ModifierKeys.Shift, Key.Tab);
		}

		internal static bool CanOpenNewTabInternal(IFileTabManager fileTabManager) {
			return fileTabManager.ActiveTab != null;
		}

		internal static void OpenNewTabInternal(IFileTabManager fileTabManager, bool clone = true) {
			var activeTab = fileTabManager.ActiveTab;
			if (activeTab == null)
				return;
			var newTab = fileTabManager.OpenEmptyTab();
			if (clone) {
				newTab.Show(activeTab.Content.Clone(), activeTab.UIContext.Serialize(), null);
				fileTabManager.SetFocus(newTab);
			}
		}

		bool CanOpenNewTab {
			get { return CanOpenNewTabInternal(fileTabManager); }
		}

		void OpenNewTab() {
			OpenNewTabInternal(fileTabManager);
		}

		bool CanCloseActiveTab {
			get { return fileTabManager.TabGroupManager.ActiveTabGroup != null && fileTabManager.TabGroupManager.ActiveTabGroup.CloseActiveTabCanExecute; }
		}

		void CloseActiveTab() {
			if (fileTabManager.TabGroupManager.ActiveTabGroup != null)
				fileTabManager.TabGroupManager.ActiveTabGroup.CloseActiveTab();
		}

		bool CanSelectNextTab {
			get { return fileTabManager.TabGroupManager.ActiveTabGroup != null && fileTabManager.TabGroupManager.ActiveTabGroup.SelectNextTabCanExecute; }
		}

		void SelectNextTab() {
			if (fileTabManager.TabGroupManager.ActiveTabGroup != null)
				fileTabManager.TabGroupManager.ActiveTabGroup.SelectNextTab();
		}

		bool CanSelectPrevTab {
			get { return fileTabManager.TabGroupManager.ActiveTabGroup != null && fileTabManager.TabGroupManager.ActiveTabGroup.SelectPreviousTabCanExecute; }
		}

		void SelectPrevTab() {
			if (fileTabManager.TabGroupManager.ActiveTabGroup != null)
				fileTabManager.TabGroupManager.ActiveTabGroup.SelectPreviousTab();
		}
	}

	sealed class TabGroupContext {
		public readonly ITabGroupManager TabGroupManager;
		public readonly ITabGroup TabGroup;

		public TabGroupContext(ITabGroup tabGroup) {
			this.TabGroup = tabGroup;
			this.TabGroupManager = tabGroup.TabGroupManager;
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

		protected readonly IFileTabManager fileTabManager;

		protected CtxMenuTabGroupCommand(IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
		}

		TabGroupContext CreateContextInternal(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TABCONTROL_GUID))
				return null;
			var tabGroup = context.Find<ITabGroup>();
			if (tabGroup == null || !fileTabManager.Owns(tabGroup))
				return null;
			return new TabGroupContext(tabGroup);
		}
	}

	[ExportMenuItem(Header = "_Close", InputGestureText = "Ctrl+W", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 10)]
	sealed class CloseTabCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		CloseTabCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroup.CloseActiveTabCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroup.CloseActiveTab();
		}
	}

	[ExportMenuItem(Header = "C_lose All Tabs", Icon = "CloseDocuments", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 20)]
	sealed class CloseAllTabsCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		CloseAllTabsCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.CloseAllTabsCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.CloseAllTabs();
		}
	}

	[ExportMenuItem(Header = "Close _All But This", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 30)]
	sealed class CloseAllTabsButThisCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		CloseAllTabsButThisCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroup.ActiveTabContent != null;
		}

		public override bool IsEnabled(TabGroupContext context) {
			return context.TabGroup.CloseAllButActiveTabCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroup.CloseAllButActiveTab();
		}
	}

	[ExportMenuItem(Header = "New _Tab", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 40)]
	sealed class NewTabCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		NewTabCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return InstallTabCommands.CanOpenNewTabInternal(fileTabManager);
		}

		public override void Execute(TabGroupContext context) {
			InstallTabCommands.OpenNewTabInternal(fileTabManager);
		}
	}

	[ExportMenuItem(Header = "New Hori_zontal Tab Group", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 0)]
	sealed class NewHorizontalTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		NewHorizontalTabGroupCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.NewHorizontalTabGroupCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.NewHorizontalTabGroup();
		}
	}

	[ExportMenuItem(Header = "New _Vertical Tab Group", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 10)]
	sealed class NewVerticalTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		NewVerticalTabGroupCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.NewVerticalTabGroupCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.NewVerticalTabGroup();
		}
	}

	[ExportMenuItem(Header = "Move to Ne_xt Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 20)]
	sealed class MoveToNextTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveToNextTabGroupCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.MoveToNextTabGroupCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.MoveToNextTabGroup();
		}
	}

	[ExportMenuItem(Header = "Move All to Next Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 30)]
	sealed class MoveAllToNextTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveAllToNextTabGroupCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.MoveAllToNextTabGroupCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.MoveAllToNextTabGroup();
		}
	}

	[ExportMenuItem(Header = "Move to P_revious Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 40)]
	sealed class MoveToPreviousTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveToPreviousTabGroupCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.MoveToPreviousTabGroupCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.MoveToPreviousTabGroup();
		}
	}

	[ExportMenuItem(Header = "Move All to Previous Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 50)]
	sealed class MoveAllToPreviousTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveAllToPreviousTabGroupCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.MoveAllToPreviousTabGroupCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.MoveAllToPreviousTabGroup();
		}
	}

	[ExportMenuItem(Header = "Close Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 0)]
	sealed class CloseTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		CloseTabGroupCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.CloseTabGroupCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.CloseTabGroup();
		}
	}

	[ExportMenuItem(Header = "Close All Tab Groups But This", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 10)]
	sealed class CloseAllTabGroupsButThisCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		CloseAllTabGroupsButThisCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.CloseAllTabGroupsButThisCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.CloseAllTabGroupsButThis();
		}
	}

	[ExportMenuItem(Header = "Move Tab Group After Next Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 20)]
	sealed class MoveTabGroupAfterNextTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveTabGroupAfterNextTabGroupCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.MoveTabGroupAfterNextTabGroupCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.MoveTabGroupAfterNextTabGroup();
		}
	}

	[ExportMenuItem(Header = "Move Tab Group Before Previous Tab Group", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 30)]
	sealed class MoveTabGroupBeforePreviousTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveTabGroupBeforePreviousTabGroupCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.MoveTabGroupBeforePreviousTabGroupCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.MoveTabGroupBeforePreviousTabGroup();
		}
	}

	[ExportMenuItem(Header = "Merge All Tab Groups", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 40)]
	sealed class MergeAllTabGroupsCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MergeAllTabGroupsCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.MergeAllTabGroupsCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.MergeAllTabGroups();
		}
	}

	[ExportMenuItem(Header = "Use Vertical Tab Groups", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_CTX_TABS_GROUPSVERT, Order = 0)]
	sealed class UseVerticalTabGroupsCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		UseVerticalTabGroupsCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.UseVerticalTabGroupsCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.UseVerticalTabGroups();
		}
	}

	[ExportMenuItem(Header = "Use Horizontal Tab Groups", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_CTX_TABS_GROUPSVERT, Order = 10)]
	sealed class UseHorizontalTabGroupsCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		UseHorizontalTabGroupsCtxMenuCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(TabGroupContext context) {
			return context.TabGroupManager.UseHorizontalTabGroupsCanExecute;
		}

		public override void Execute(TabGroupContext context) {
			context.TabGroupManager.UseHorizontalTabGroups();
		}
	}

	[ExportMenuItem(Header = "Open in New _Tab", InputGestureText = "Ctrl+T", Group = MenuConstants.GROUP_CTX_FILES_TABS, Order = 0)]
	sealed class OpenInNewTabCtxMenuCommand : MenuItemBase {
		readonly IFileTabManager fileTabManager;

		[ImportingConstructor]
		OpenInNewTabCtxMenuCommand(IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
		}

		public override bool IsVisible(IMenuItemContext context) {
			return context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID) &&
				InstallTabCommands.CanOpenNewTabInternal(fileTabManager) &&
				(context.Find<ITreeNodeData[]>() ?? emptyArray).Length > 0;
		}
		static readonly ITreeNodeData[] emptyArray = new ITreeNodeData[0];

		public override void Execute(IMenuItemContext context) {
			InstallTabCommands.OpenNewTabInternal(fileTabManager);
		}
	}

	[ExportMenuItem(Header = "Open in New _Tab", Group = MenuConstants.GROUP_CTX_CODE_TABS, Order = 0)]
	sealed class OpenReferenceInNewTabCtxMenuCommand : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			ITextEditorUIContext uiContext;
			var @ref = GetReference(context, out uiContext);
			if (@ref != null)
				uiContext.FileTab.FollowReferenceNewTab(@ref);
		}

		public override bool IsVisible(IMenuItemContext context) {
			ITextEditorUIContext uiContext;
			return GetReference(context, out uiContext) != null;
		}

		static object GetReference(IMenuItemContext context, out ITextEditorUIContext uiContext) {
			uiContext = null;
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID))
				return null;
			uiContext = context.Find<ITextEditorUIContext>();
			if (uiContext == null)
				return null;
			return context.Find<CodeReference>();
		}

		public override string GetInputGestureText(IMenuItemContext context) {
			return context.OpenedFromKeyboard ? "Ctrl+F12" : "Ctrl+Click";
		}
	}

	sealed class MenuTabGroupContext {
		public readonly ITabGroupManager TabGroupManager;
		public readonly ITabGroup TabGroup;

		public MenuTabGroupContext(ITabGroupManager tabGroupManager) {
			this.TabGroup = tabGroupManager.ActiveTabGroup;
			this.TabGroupManager = tabGroupManager;
		}
	}

	abstract class MenuTabGroupCommand : MenuItemBase<MenuTabGroupContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override MenuTabGroupContext CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.APP_MENU_WINDOW_GUID))
				return null;

			return new MenuTabGroupContext(fileTabManager.TabGroupManager);
		}

		protected readonly IFileTabManager fileTabManager;

		protected MenuTabGroupCommand(IFileTabManager fileTabManager) {
			this.fileTabManager = fileTabManager;
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "_New Window", Icon = "NewWindow", Group = MenuConstants.GROUP_APP_MENU_WINDOW_WINDOW, Order = 0)]
	sealed class NewWindowCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		NewWindowCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsEnabled(MenuTabGroupContext context) {
			return InstallTabCommands.CanOpenNewTabInternal(fileTabManager);
		}

		public override void Execute(MenuTabGroupContext context) {
			InstallTabCommands.OpenNewTabInternal(fileTabManager);
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "_Close", InputGestureText = "Ctrl+W", Group = MenuConstants.GROUP_APP_MENU_WINDOW_WINDOW, Order = 10)]
	sealed class CloseTabCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		CloseTabCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroup != null && context.TabGroup.CloseActiveTabCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			if (context.TabGroup != null)
				context.TabGroup.CloseActiveTab();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "New Hori_zontal Tab Group", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 0)]
	sealed class NewHorizontalTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		NewHorizontalTabGroupCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.NewHorizontalTabGroupCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.NewHorizontalTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "New _Vertical Tab Group", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 10)]
	sealed class NewVerticalTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		NewVerticalTabGroupCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.NewVerticalTabGroupCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.NewVerticalTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move to Ne_xt Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 20)]
	sealed class MoveToNextTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveToNextTabGroupCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.MoveToNextTabGroupCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.MoveToNextTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move All to Next Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 30)]
	sealed class MoveAllToNextTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveAllToNextTabGroupCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.MoveAllToNextTabGroupCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.MoveAllToNextTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move to P_revious Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 40)]
	sealed class MoveToPreviousTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveToPreviousTabGroupCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.MoveToPreviousTabGroupCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.MoveToPreviousTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move All to Previous Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 50)]
	sealed class MoveAllToPreviousTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveAllToPreviousTabGroupCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.MoveAllToPreviousTabGroupCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.MoveAllToPreviousTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "C_lose All Tabs", Icon = "CloseDocuments", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 60)]
	sealed class CloseAllTabsCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		CloseAllTabsCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return true;
		}

		public override bool IsEnabled(MenuTabGroupContext context) {
			return context.TabGroupManager.CloseAllTabsCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.CloseAllTabs();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Close Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 0)]
	sealed class CloseTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		CloseTabGroupCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.CloseTabGroupCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.CloseTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Close All Tab Groups But This", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 10)]
	sealed class CloseAllTabGroupsButThisCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		CloseAllTabGroupsButThisCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.CloseAllTabGroupsButThisCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.CloseAllTabGroupsButThis();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move Tab Group After Next Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 20)]
	sealed class MoveTabGroupAfterNextTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveTabGroupAfterNextTabGroupCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.MoveTabGroupAfterNextTabGroupCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.MoveTabGroupAfterNextTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Move Tab Group Before Previous Tab Group", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 30)]
	sealed class MoveTabGroupBeforePreviousTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveTabGroupBeforePreviousTabGroupCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.MoveTabGroupBeforePreviousTabGroupCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.MoveTabGroupBeforePreviousTabGroup();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Merge All Tab Groups", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 40)]
	sealed class MergeAllTabGroupsCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MergeAllTabGroupsCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.MergeAllTabGroupsCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.MergeAllTabGroups();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Use Vertical Tab Groups", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSVERT, Order = 0)]
	sealed class UseVerticalTabGroupsCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		UseVerticalTabGroupsCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.UseVerticalTabGroupsCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.UseVerticalTabGroups();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "Use Horizontal Tab Groups", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSVERT, Order = 10)]
	sealed class UseHorizontalTabGroupsCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		UseHorizontalTabGroupsCommand(IFileTabManager fileTabManager)
			: base(fileTabManager) {
		}

		public override bool IsVisible(MenuTabGroupContext context) {
			return context.TabGroupManager.UseHorizontalTabGroupsCanExecute;
		}

		public override void Execute(MenuTabGroupContext context) {
			context.TabGroupManager.UseHorizontalTabGroups();
		}
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Group = MenuConstants.GROUP_APP_MENU_WINDOW_ALLWINDOWS, Order = 0)]
	sealed class AllTabsMenuItemCommand : MenuTabGroupCommand, IMenuItemCreator {
		readonly ISaveManager saveManager;
		readonly ITabsVMSettings tabsVMSettings;
		readonly IAppWindow appWindow;

		[ImportingConstructor]
		AllTabsMenuItemCommand(IFileTabManager fileTabManager, ISaveManager saveManager, ITabsVMSettings tabsVMSettings, IAppWindow appWindow)
			: base(fileTabManager) {
			this.saveManager = saveManager;
			this.tabsVMSettings = tabsVMSettings;
			this.appWindow = appWindow;
		}

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
			foreach (var tab in fileTabManager.SortedTabs) {
				var header = GetHeader(index + 1, tab);
				var attr = new ExportMenuItemAttribute { Header = header };
				var tabTmp = tab;
				var item = new MyMenuItem(ctx => fileTabManager.ActiveTab = tabTmp, index == 0);

				yield return new CreatedMenuItem(attr, item);

				if (++index >= MAX_TABS)
					break;
			}

			var attr2 = new ExportMenuItemAttribute { Header = "_Windows..." };
			var item2 = new MyMenuItem(ctx => ShowTabsDlg(), false);
			yield return new CreatedMenuItem(attr2, item2);
		}

		static string GetShortMenuItemHeader(string s) {
			Debug.Assert(s != null);
			if (s == null)
				s = string.Empty;
			const int MAX_LEN = 40;
			if (s.Length > MAX_LEN)
				s = s.Substring(0, MAX_LEN) + "...";
			return MenuUtils.EscapeMenuItemHeader(s);
		}

		static string GetHeader(int i, IFileTab tab) {
			string s;
			if (i == 10)
				s = "1_0";
			else if (i > 10)
				s = i.ToString();
			else
				s = string.Format("_{0}", i);
			return string.Format("{0} {1}", s, GetShortMenuItemHeader(tab.Content.Title));
		}

		void ShowTabsDlg() {
			var win = new TabsDlg();
			var vm = new TabsVM(fileTabManager, saveManager, tabsVMSettings);
			win.DataContext = vm;
			win.Owner = appWindow.MainWindow;
			win.ShowDialog();

			// The original tab group gets back its keyboard focus by ShowDialog(). Make sure that
			// the correct tab is activated.
			if (vm.LastActivated != null)
				vm.LastActivated.Tab.FileTabManager.SetFocus(vm.LastActivated.Tab);
		}
	}
}
