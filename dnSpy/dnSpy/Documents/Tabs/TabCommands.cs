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
using System.Windows.Input;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Utilities;
using dnSpy.Documents.Tabs.Dialogs;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Documents.Tabs {
	[ExportAutoLoaded]
	sealed class InstallTabCommands : IAutoLoaded {
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		InstallTabCommands(IAppWindow appWindow, IDocumentTabService documentTabService) {
			this.documentTabService = documentTabService;
			var cmds = appWindow.MainWindowCommands;
			cmds.Add(new RoutedCommand("OpenNewTab", typeof(InstallTabCommands)), (s, e) => OpenNewTab(), (s, e) => e.CanExecute = CanOpenNewTab, ModifierKeys.Control, Key.T);
			cmds.Add(new RoutedCommand("CloseActiveTab", typeof(InstallTabCommands)), (s, e) => CloseActiveTab(), (s, e) => e.CanExecute = CanCloseActiveTab, ModifierKeys.Control, Key.F4);
			cmds.Add(new RoutedCommand("SelectNextTab", typeof(InstallTabCommands)), (s, e) => SelectNextTab(), (s, e) => e.CanExecute = CanSelectNextTab, ModifierKeys.Control, Key.Tab);
			cmds.Add(new RoutedCommand("SelectPrevTab", typeof(InstallTabCommands)), (s, e) => SelectPrevTab(), (s, e) => e.CanExecute = CanSelectPrevTab, ModifierKeys.Control | ModifierKeys.Shift, Key.Tab);
		}

		internal static bool CanOpenNewTabInternal(IDocumentTabService documentTabService) => documentTabService.ActiveTab?.Content.CanClone == true;

		internal static void OpenNewTabInternal(IDocumentTabService documentTabService, bool clone = true) {
			var activeTab = documentTabService.ActiveTab;
			if (activeTab is null)
				return;
			if (clone && !activeTab.Content.CanClone)
				return;
			var newTab = documentTabService.OpenEmptyTab();
			if (clone) {
				newTab.Show(activeTab.Content.Clone(), activeTab.UIContext.CreateUIState(), null);
				documentTabService.SetFocus(newTab);
			}
		}

		bool CanOpenNewTab => CanOpenNewTabInternal(documentTabService);
		void OpenNewTab() => OpenNewTabInternal(documentTabService);

		bool CanCloseActiveTab => !(documentTabService.TabGroupService.ActiveTabGroup is null) && documentTabService.TabGroupService.ActiveTabGroup.CloseActiveTabCanExecute;
		void CloseActiveTab() {
			if (!(documentTabService.TabGroupService.ActiveTabGroup is null))
				documentTabService.TabGroupService.ActiveTabGroup.CloseActiveTab();
		}

		bool CanSelectNextTab => !(documentTabService.TabGroupService.ActiveTabGroup is null) && documentTabService.TabGroupService.ActiveTabGroup.SelectNextTabCanExecute;
		void SelectNextTab() {
			if (!(documentTabService.TabGroupService.ActiveTabGroup is null))
				documentTabService.TabGroupService.ActiveTabGroup.SelectNextTab();
		}

		bool CanSelectPrevTab => !(documentTabService.TabGroupService.ActiveTabGroup is null) && documentTabService.TabGroupService.ActiveTabGroup.SelectPreviousTabCanExecute;
		void SelectPrevTab() {
			if (!(documentTabService.TabGroupService.ActiveTabGroup is null))
				documentTabService.TabGroupService.ActiveTabGroup.SelectPreviousTab();
		}
	}

	sealed class TabGroupContext {
		public readonly ITabGroupService TabGroupService;
		public readonly ITabGroup TabGroup;

		public TabGroupContext(ITabGroup tabGroup) {
			TabGroup = tabGroup;
			TabGroupService = tabGroup.TabGroupService;
		}
	}

	abstract class CtxMenuTabGroupCommand : MenuItemBase<TabGroupContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override TabGroupContext? CreateContext(IMenuItemContext context) => CreateContextInternal(context);

		protected readonly IDocumentTabService documentTabService;

		protected CtxMenuTabGroupCommand(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

		TabGroupContext? CreateContextInternal(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TABCONTROL_GUID))
				return null;
			var tabGroup = context.Find<ITabGroup>();
			if (tabGroup is null || !documentTabService.Owns(tabGroup))
				return null;
			return new TabGroupContext(tabGroup);
		}
	}

	[ExportMenuItem(Header = "res:CloseTabCommand", InputGestureText = "res:ShortCutKeyCtrlF4", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 10)]
	sealed class CloseTabCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		CloseTabCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroup.CloseActiveTabCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroup.CloseActiveTab();
	}

	[ExportMenuItem(Header = "res:CloseAllTabsCommand", Icon = DsImagesAttribute.CloseDocumentGroup, Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 20)]
	sealed class CloseAllTabsCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		CloseAllTabsCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.CloseAllTabsCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.CloseAllTabs();
	}

	[ExportMenuItem(Header = "res:CloseAllTabsButThisCommand", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 30)]
	sealed class CloseAllTabsButThisCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		CloseAllTabsButThisCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => !(context.TabGroup.ActiveTabContent is null);
		public override bool IsEnabled(TabGroupContext context) => context.TabGroup.CloseAllButActiveTabCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroup.CloseAllButActiveTab();
	}

	[ExportMenuItem(Header = "res:NewTabCommand", Group = MenuConstants.GROUP_CTX_TABS_CLOSE, Order = 40)]
	sealed class NewTabCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		NewTabCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => InstallTabCommands.CanOpenNewTabInternal(documentTabService);
		public override void Execute(TabGroupContext context) => InstallTabCommands.OpenNewTabInternal(documentTabService);
	}

	[ExportMenuItem(Header = "res:NewHorizontalTabGroupCommand", Icon = DsImagesAttribute.SplitScreenHorizontally, Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 0)]
	sealed class NewHorizontalTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		NewHorizontalTabGroupCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.NewHorizontalTabGroupCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.NewHorizontalTabGroup();
	}

	[ExportMenuItem(Header = "res:NewVerticalTabGroupCommand", Icon = DsImagesAttribute.SplitScreenVertically, Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 10)]
	sealed class NewVerticalTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		NewVerticalTabGroupCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.NewVerticalTabGroupCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.NewVerticalTabGroup();
	}

	[ExportMenuItem(Header = "res:MoveToNextTabGroupCommand", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 20)]
	sealed class MoveToNextTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveToNextTabGroupCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.MoveToNextTabGroupCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.MoveToNextTabGroup();
	}

	[ExportMenuItem(Header = "res:MoveAllTabsToNextTabGroupCommand", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 30)]
	sealed class MoveAllToNextTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveAllToNextTabGroupCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.MoveAllToNextTabGroupCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.MoveAllToNextTabGroup();
	}

	[ExportMenuItem(Header = "res:MoveToPreviousTabGroupCommand", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 40)]
	sealed class MoveToPreviousTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveToPreviousTabGroupCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.MoveToPreviousTabGroupCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.MoveToPreviousTabGroup();
	}

	[ExportMenuItem(Header = "res:MoveAllToPreviousTabGroupCommand", Group = MenuConstants.GROUP_CTX_TABS_GROUPS, Order = 50)]
	sealed class MoveAllToPreviousTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveAllToPreviousTabGroupCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.MoveAllToPreviousTabGroupCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.MoveAllToPreviousTabGroup();
	}

	[ExportMenuItem(Header = "res:CloseTabGroupCommand", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 0)]
	sealed class CloseTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		CloseTabGroupCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.CloseTabGroupCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.CloseTabGroup();
	}

	[ExportMenuItem(Header = "res:CloseAllTabGroupsButThisCommand", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 10)]
	sealed class CloseAllTabGroupsButThisCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		CloseAllTabGroupsButThisCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.CloseAllTabGroupsButThisCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.CloseAllTabGroupsButThis();
	}

	[ExportMenuItem(Header = "res:MoveTabGroupAfterNextTabGroupCommand", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 20)]
	sealed class MoveTabGroupAfterNextTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveTabGroupAfterNextTabGroupCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.MoveTabGroupAfterNextTabGroupCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.MoveTabGroupAfterNextTabGroup();
	}

	[ExportMenuItem(Header = "res:MoveTabGroupBeforePreviousTabGroupCommand", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 30)]
	sealed class MoveTabGroupBeforePreviousTabGroupCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MoveTabGroupBeforePreviousTabGroupCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.MoveTabGroupBeforePreviousTabGroupCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.MoveTabGroupBeforePreviousTabGroup();
	}

	[ExportMenuItem(Header = "res:MergeAllTabGroupsCommand", Group = MenuConstants.GROUP_CTX_TABS_GROUPSCLOSE, Order = 40)]
	sealed class MergeAllTabGroupsCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		MergeAllTabGroupsCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.MergeAllTabGroupsCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.MergeAllTabGroups();
	}

	[ExportMenuItem(Header = "res:UseVerticalTabGroupsCommand", Icon = DsImagesAttribute.SplitScreenVertically, Group = MenuConstants.GROUP_CTX_TABS_GROUPSVERT, Order = 0)]
	sealed class UseVerticalTabGroupsCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		UseVerticalTabGroupsCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.UseVerticalTabGroupsCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.UseVerticalTabGroups();
	}

	[ExportMenuItem(Header = "res:UseHorizontalTabGroupsCommand", Icon = DsImagesAttribute.SplitScreenHorizontally, Group = MenuConstants.GROUP_CTX_TABS_GROUPSVERT, Order = 10)]
	sealed class UseHorizontalTabGroupsCtxMenuCommand : CtxMenuTabGroupCommand {
		[ImportingConstructor]
		UseHorizontalTabGroupsCtxMenuCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(TabGroupContext context) => context.TabGroupService.UseHorizontalTabGroupsCanExecute;
		public override void Execute(TabGroupContext context) => context.TabGroupService.UseHorizontalTabGroups();
	}

	[ExportMenuItem(Header = "res:OpenInNewTabCommand", InputGestureText = "res:OpenInNewTabKey", Group = MenuConstants.GROUP_CTX_DOCUMENTS_TABS, Order = 0)]
	sealed class OpenInNewTabCtxMenuCommand : MenuItemBase {
		readonly IDocumentTabService documentTabService;

		[ImportingConstructor]
		OpenInNewTabCtxMenuCommand(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;

		public override bool IsVisible(IMenuItemContext context) =>
			context.CreatorObject.Guid == new Guid(MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID) &&
			InstallTabCommands.CanOpenNewTabInternal(documentTabService) &&
			(context.Find<TreeNodeData[]>() ?? Array.Empty<TreeNodeData>()).Length > 0;

		public override void Execute(IMenuItemContext context) => InstallTabCommands.OpenNewTabInternal(documentTabService);
	}

	[ExportMenuItem(Header = "res:OpenInNewTabCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_TABS, Order = 0)]
	sealed class OpenReferenceInNewTabCtxMenuCommand : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			var @ref = GetReference(context, out var documentViewer);
			if (!(@ref is null))
				documentViewer!.DocumentTab?.FollowReferenceNewTab(@ref);
		}

		public override bool IsVisible(IMenuItemContext context) => !(GetReference(context, out var documentViewer) is null);

		static object? GetReference(IMenuItemContext context, out IDocumentViewer? documentViewer) {
			documentViewer = null;
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
				return null;
			documentViewer = context.Find<IDocumentViewer>();
			if (documentViewer is null)
				return null;
			var textRef = context.Find<TextReference>();
			if (textRef is null)
				return null;
			if (textRef.NoFollow)
				return null;
			return textRef;
		}

		public override string? GetInputGestureText(IMenuItemContext context) =>
			context.OpenedFromKeyboard ? dnSpy_Resources.OpenInNewTabKey2 : dnSpy_Resources.OpenInNewTabKey3;
	}

	sealed class MenuTabGroupContext {
		public readonly ITabGroupService TabGroupService;
		public readonly ITabGroup? TabGroup;

		public MenuTabGroupContext(ITabGroupService tabGroupService) {
			TabGroup = tabGroupService.ActiveTabGroup;
			TabGroupService = tabGroupService;
		}
	}

	abstract class MenuTabGroupCommand : MenuItemBase<MenuTabGroupContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override MenuTabGroupContext? CreateContext(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.APP_MENU_WINDOW_GUID))
				return null;

			return new MenuTabGroupContext(documentTabService.TabGroupService);
		}

		protected readonly IDocumentTabService documentTabService;

		protected MenuTabGroupCommand(IDocumentTabService documentTabService) => this.documentTabService = documentTabService;
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:NewWindowCommand", Icon = DsImagesAttribute.NewWindow, Group = MenuConstants.GROUP_APP_MENU_WINDOW_WINDOW, Order = 0)]
	sealed class NewWindowCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		NewWindowCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsEnabled(MenuTabGroupContext context) => InstallTabCommands.CanOpenNewTabInternal(documentTabService);
		public override void Execute(MenuTabGroupContext context) => InstallTabCommands.OpenNewTabInternal(documentTabService);
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:CloseTabCommand", InputGestureText = "res:ShortCutKeyCtrlF4", Group = MenuConstants.GROUP_APP_MENU_WINDOW_WINDOW, Order = 10)]
	sealed class CloseTabCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		CloseTabCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => !(context.TabGroup is null) && context.TabGroup.CloseActiveTabCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroup?.CloseActiveTab();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:NewHorizontalTabGroupCommand", Icon = DsImagesAttribute.SplitScreenHorizontally, Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 0)]
	sealed class NewHorizontalTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		NewHorizontalTabGroupCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.NewHorizontalTabGroupCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.NewHorizontalTabGroup();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:NewVerticalTabGroupCommand", Icon = DsImagesAttribute.SplitScreenVertically, Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 10)]
	sealed class NewVerticalTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		NewVerticalTabGroupCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.NewVerticalTabGroupCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.NewVerticalTabGroup();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:MoveToNextTabGroupCommand", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 20)]
	sealed class MoveToNextTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveToNextTabGroupCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.MoveToNextTabGroupCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.MoveToNextTabGroup();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:MoveAllTabsToNextTabGroupCommand", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 30)]
	sealed class MoveAllToNextTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveAllToNextTabGroupCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.MoveAllToNextTabGroupCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.MoveAllToNextTabGroup();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:MoveToPreviousTabGroupCommand", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 40)]
	sealed class MoveToPreviousTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveToPreviousTabGroupCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.MoveToPreviousTabGroupCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.MoveToPreviousTabGroup();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:MoveAllToPreviousTabGroupCommand", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 50)]
	sealed class MoveAllToPreviousTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveAllToPreviousTabGroupCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.MoveAllToPreviousTabGroupCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.MoveAllToPreviousTabGroup();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:CloseAllTabsCommand", Icon = DsImagesAttribute.CloseDocumentGroup, Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPS, Order = 60)]
	sealed class CloseAllTabsCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		CloseAllTabsCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => true;
		public override bool IsEnabled(MenuTabGroupContext context) => context.TabGroupService.CloseAllTabsCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.CloseAllTabs();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:CloseTabGroupCommand", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 0)]
	sealed class CloseTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		CloseTabGroupCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.CloseTabGroupCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.CloseTabGroup();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:CloseAllTabGroupsButThisCommand", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 10)]
	sealed class CloseAllTabGroupsButThisCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		CloseAllTabGroupsButThisCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.CloseAllTabGroupsButThisCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.CloseAllTabGroupsButThis();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:MoveTabGroupAfterNextTabGroupCommand", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 20)]
	sealed class MoveTabGroupAfterNextTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveTabGroupAfterNextTabGroupCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.MoveTabGroupAfterNextTabGroupCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.MoveTabGroupAfterNextTabGroup();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:MoveTabGroupBeforePreviousTabGroupCommand", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 30)]
	sealed class MoveTabGroupBeforePreviousTabGroupCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MoveTabGroupBeforePreviousTabGroupCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.MoveTabGroupBeforePreviousTabGroupCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.MoveTabGroupBeforePreviousTabGroup();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:MergeAllTabGroupsCommand", Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE, Order = 40)]
	sealed class MergeAllTabGroupsCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		MergeAllTabGroupsCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.MergeAllTabGroupsCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.MergeAllTabGroups();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:UseVerticalTabGroupsCommand", Icon = DsImagesAttribute.SplitScreenVertically, Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSVERT, Order = 0)]
	sealed class UseVerticalTabGroupsCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		UseVerticalTabGroupsCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.UseVerticalTabGroupsCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.UseVerticalTabGroups();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Header = "res:UseHorizontalTabGroupsCommand", Icon = DsImagesAttribute.SplitScreenHorizontally, Group = MenuConstants.GROUP_APP_MENU_WINDOW_TABGROUPSVERT, Order = 10)]
	sealed class UseHorizontalTabGroupsCommand : MenuTabGroupCommand {
		[ImportingConstructor]
		UseHorizontalTabGroupsCommand(IDocumentTabService documentTabService)
			: base(documentTabService) {
		}

		public override bool IsVisible(MenuTabGroupContext context) => context.TabGroupService.UseHorizontalTabGroupsCanExecute;
		public override void Execute(MenuTabGroupContext context) => context.TabGroupService.UseHorizontalTabGroups();
	}

	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_WINDOW_GUID, Group = MenuConstants.GROUP_APP_MENU_WINDOW_ALLWINDOWS, Order = 0)]
	sealed class AllTabsMenuItemCommand : MenuTabGroupCommand, IMenuItemProvider {
		readonly ISaveService saveService;
		readonly ITabsVMSettings tabsVMSettings;
		readonly IAppWindow appWindow;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly ITextElementProvider textElementProvider;

		[ImportingConstructor]
		AllTabsMenuItemCommand(IDocumentTabService documentTabService, ISaveService saveService, ITabsVMSettings tabsVMSettings, IAppWindow appWindow, IClassificationFormatMapService classificationFormatMapService, ITextElementProvider textElementProvider)
			: base(documentTabService) {
			this.saveService = saveService;
			this.tabsVMSettings = tabsVMSettings;
			this.appWindow = appWindow;
			classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap(AppearanceCategoryConstants.UIMisc);
			this.textElementProvider = textElementProvider;
		}

		public override void Execute(MenuTabGroupContext context) { }

		sealed class MyMenuItem : MenuItemBase {
			readonly Action<IMenuItemContext> action;
			readonly bool isChecked;

			public MyMenuItem(Action<IMenuItemContext> action, bool isChecked) {
				this.action = action;
				this.isChecked = isChecked;
			}

			public override void Execute(IMenuItemContext context) => action(context);
			public override bool IsChecked(IMenuItemContext context) => isChecked;
		}

		public IEnumerable<CreatedMenuItem> Create(IMenuItemContext context) {
			const int MAX_TABS = 10;
			int index = 0;
			foreach (var tab in documentTabService.SortedTabs) {
				var header = GetHeader(index + 1, tab);
				var attr = new ExportMenuItemAttribute { Header = header };
				var tabTmp = tab;
				var item = new MyMenuItem(ctx => documentTabService.ActiveTab = tabTmp, index == 0);

				yield return new CreatedMenuItem(attr, item);

				if (++index >= MAX_TABS)
					break;
			}

			var attr2 = new ExportMenuItemAttribute { Header = dnSpy_Resources.WindowsCommand };
			var item2 = new MyMenuItem(ctx => ShowTabsDlg(), false);
			yield return new CreatedMenuItem(attr2, item2);
		}

		static string GetShortMenuItemHeader(string s) {
			Debug2.Assert(!(s is null));
			if (s is null)
				s = string.Empty;
			const int MAX_LEN = 40;
			if (s.Length > MAX_LEN)
				s = s.Substring(0, MAX_LEN) + "...";
			return UIUtilities.EscapeMenuItemHeader(s);
		}

		static string GetHeader(int i, IDocumentTab tab) {
			string s;
			if (i == 10)
				s = "1_0";
			else if (i > 10)
				s = i.ToString();
			else
				s = $"_{i}";
			return $"{s} {GetShortMenuItemHeader(tab.Content.Title)}";
		}

		void ShowTabsDlg() {
			var win = new TabsDlg();
			var vm = new TabsVM(documentTabService, saveService, tabsVMSettings, classificationFormatMap, textElementProvider);
			win.DataContext = vm;
			win.Owner = appWindow.MainWindow;
			win.ShowDialog();

			// The original tab group gets back its keyboard focus by ShowDialog(). Make sure that
			// the correct tab is activated.
			if (!(vm.LastActivated is null))
				vm.LastActivated.Tab.DocumentTabService.SetFocus(vm.LastActivated.Tab);
		}
	}
}
