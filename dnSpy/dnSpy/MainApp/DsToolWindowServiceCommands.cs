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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.ToolWindows.App;

namespace dnSpy.MainApp {
	sealed class ToolWindowGroupContext {
		public readonly IDsToolWindowService DsToolWindowService;
		public readonly IToolWindowGroupService ToolWindowGroupService;
		public readonly IToolWindowGroup ToolWindowGroup;

		public ToolWindowGroupContext(IDsToolWindowService toolWindowService, IToolWindowGroup toolWindowGroup) {
			this.DsToolWindowService = toolWindowService;
			this.ToolWindowGroupService = toolWindowGroup.ToolWindowGroupService;
			this.ToolWindowGroup = toolWindowGroup;
		}
	}

	abstract class CtxMenuToolWindowGroupCommand : MenuItemBase<ToolWindowGroupContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override ToolWindowGroupContext CreateContext(IMenuItemContext context) => CreateContextInternal(context);

		readonly IDsToolWindowService toolWindowService;

		protected CtxMenuToolWindowGroupCommand(IDsToolWindowService toolWindowService) {
			this.toolWindowService = toolWindowService;
		}

		protected ToolWindowGroupContext CreateContextInternal(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TOOLWINDOW_TABCONTROL_GUID))
				return null;
			var twg = context.Find<IToolWindowGroup>();
			if (twg == null || !toolWindowService.Owns(twg))
				return null;
			return new ToolWindowGroupContext(toolWindowService, twg);
		}
	}

	[ExportMenuItem(Header = "res:HideToolWindowCommand", InputGestureText = "res:ShortCutKeyShiftEsc", Icon = "tableviewnameonly", Group = MenuConstants.GROUP_CTX_TOOLWINS_CLOSE, Order = 10)]
	sealed class HideTWCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		HideTWCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroup.CloseActiveTabCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroup.CloseActiveTab();
	}

	[ExportMenuItem(Header = "res:HideAllToolWindowsCommand", Icon = "CloseDocuments", Group = MenuConstants.GROUP_CTX_TOOLWINS_CLOSE, Order = 20)]
	sealed class CloseAllTabsTWCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		CloseAllTabsTWCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.CloseAllTabsCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.CloseAllTabs();
	}

	static class Constants {
		public const string MOVE_CONTENT_GUID = "D54D52CB-A6FC-408C-9A52-EA0D53AEEC3A";
		public const string GROUP_MOVE_CONTENT = "0,92C51A9F-DE4B-4D7F-B1DC-AAA482936B5C";
		public const string MOVE_GROUP_GUID = "047ECD64-82EF-4774-9C0A-330A61989432";
		public const string GROUP_MOVE_GROUP = "0,174B60EE-279F-4DA4-9F07-44FFD03E4421";
	}

	[ExportMenuItem(Header = "res:MoveToolWindowCommand", Guid = Constants.MOVE_CONTENT_GUID, Group = MenuConstants.GROUP_CTX_TOOLWINS_CLOSE, Order = 30)]
	sealed class MoveTWCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTWCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override void Execute(ToolWindowGroupContext context) => Debug.Fail("Shouldn't be here");
	}

	[ExportMenuItem(Header = "res:MoveToolWindowGroupCommand", Guid = Constants.MOVE_GROUP_GUID, Group = MenuConstants.GROUP_CTX_TOOLWINS_CLOSE, Order = 40)]
	sealed class MoveGroupTWCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveGroupTWCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroup.TabContents.Count() > 1;
		public override void Execute(ToolWindowGroupContext context) => Debug.Fail("Shouldn't be here");
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_CONTENT_GUID, Header = "res:MoveTopCommand", Icon = "toolstrippaneltop", Group = Constants.GROUP_MOVE_CONTENT, Order = 0)]
	sealed class MoveTWTopCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTWTopCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) => context.DsToolWindowService.CanMove(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Top);
		public override void Execute(ToolWindowGroupContext context) => context.DsToolWindowService.Move(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Top);
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_CONTENT_GUID, Header = "res:MoveLeftCommand", Icon = "toolstrippanelleft", Group = Constants.GROUP_MOVE_CONTENT, Order = 10)]
	sealed class MoveTWLeftCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTWLeftCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) => context.DsToolWindowService.CanMove(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Left);
		public override void Execute(ToolWindowGroupContext context) => context.DsToolWindowService.Move(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Left);
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_CONTENT_GUID, Header = "res:MoveRightCommand", Icon = "toolstrippanelright", Group = Constants.GROUP_MOVE_CONTENT, Order = 20)]
	sealed class MoveTWRightCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTWRightCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) => context.DsToolWindowService.CanMove(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Right);
		public override void Execute(ToolWindowGroupContext context) => context.DsToolWindowService.Move(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Right);
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_CONTENT_GUID, Header = "res:MoveBottomCommand", Icon = "toolstrippanelbottom", Group = Constants.GROUP_MOVE_CONTENT, Order = 30)]
	sealed class MoveTWBottomCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTWBottomCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) => context.DsToolWindowService.CanMove(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Bottom);
		public override void Execute(ToolWindowGroupContext context) => context.DsToolWindowService.Move(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Bottom);
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_GROUP_GUID, Header = "res:MoveTopCommand", Icon = "toolstrippaneltop", Group = Constants.GROUP_MOVE_GROUP, Order = 0)]
	sealed class MoveGroupTWTopCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveGroupTWTopCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) => context.DsToolWindowService.CanMove(context.ToolWindowGroup, AppToolWindowLocation.Top);
		public override void Execute(ToolWindowGroupContext context) => context.DsToolWindowService.Move(context.ToolWindowGroup, AppToolWindowLocation.Top);
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_GROUP_GUID, Header = "res:MoveLeftCommand", Icon = "toolstrippanelleft", Group = Constants.GROUP_MOVE_GROUP, Order = 10)]
	sealed class MoveGroupTWLeftCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveGroupTWLeftCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) => context.DsToolWindowService.CanMove(context.ToolWindowGroup, AppToolWindowLocation.Left);
		public override void Execute(ToolWindowGroupContext context) => context.DsToolWindowService.Move(context.ToolWindowGroup, AppToolWindowLocation.Left);
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_GROUP_GUID, Header = "res:MoveRightCommand", Icon = "toolstrippanelright", Group = Constants.GROUP_MOVE_GROUP, Order = 20)]
	sealed class MoveGroupTWRightCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveGroupTWRightCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) => context.DsToolWindowService.CanMove(context.ToolWindowGroup, AppToolWindowLocation.Right);
		public override void Execute(ToolWindowGroupContext context) => context.DsToolWindowService.Move(context.ToolWindowGroup, AppToolWindowLocation.Right);
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_GROUP_GUID, Header = "res:MoveBottomCommand", Icon = "toolstrippanelbottom", Group = Constants.GROUP_MOVE_GROUP, Order = 30)]
	sealed class MoveGroupTWBottomCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveGroupTWBottomCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) => context.DsToolWindowService.CanMove(context.ToolWindowGroup, AppToolWindowLocation.Bottom);
		public override void Execute(ToolWindowGroupContext context) => context.DsToolWindowService.Move(context.ToolWindowGroup, AppToolWindowLocation.Bottom);
	}

	[ExportMenuItem(Header = "res:NewHorizontalTabGroupCommand", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 0)]
	sealed class NewHorizontalTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		NewHorizontalTabGroupCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.NewHorizontalTabGroupCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.NewHorizontalTabGroup();
	}

	[ExportMenuItem(Header = "res:NewVerticalTabGroupCommand", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 10)]
	sealed class NewVerticalTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		NewVerticalTabGroupCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.NewVerticalTabGroupCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.NewVerticalTabGroup();
	}

	[ExportMenuItem(Header = "res:MoveToNextTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 20)]
	sealed class MoveToNextTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveToNextTabGroupCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveToNextTabGroupCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveToNextTabGroup();
	}

	[ExportMenuItem(Header = "res:MoveAllTabsToNextTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 30)]
	sealed class MoveAllToNextTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveAllToNextTabGroupCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveAllToNextTabGroupCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveAllToNextTabGroup();
	}

	[ExportMenuItem(Header = "res:MoveToPreviousTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 40)]
	sealed class MoveToPreviousTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveToPreviousTabGroupCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveToPreviousTabGroupCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveToPreviousTabGroup();
	}

	[ExportMenuItem(Header = "res:MoveAllToPreviousTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 50)]
	sealed class MoveAllToPreviousTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveAllToPreviousTabGroupCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveAllToPreviousTabGroupCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveAllToPreviousTabGroup();
	}

	[ExportMenuItem(Header = "res:CloseTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSCLOSE, Order = 0)]
	sealed class CloseTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		CloseTabGroupCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.CloseTabGroupCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.CloseTabGroup();
	}

	[ExportMenuItem(Header = "res:CloseAllTabGroupsButThisCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSCLOSE, Order = 10)]
	sealed class CloseAllTabGroupsButThisCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		CloseAllTabGroupsButThisCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.CloseAllTabGroupsButThisCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.CloseAllTabGroupsButThis();
	}

	[ExportMenuItem(Header = "res:MoveTabGroupAfterNextTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSCLOSE, Order = 20)]
	sealed class MoveTabGroupAfterNextTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTabGroupAfterNextTabGroupCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveTabGroupAfterNextTabGroupCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveTabGroupAfterNextTabGroup();
	}

	[ExportMenuItem(Header = "res:MoveTabGroupBeforePreviousTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSCLOSE, Order = 30)]
	sealed class MoveTabGroupBeforePreviousTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTabGroupBeforePreviousTabGroupCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveTabGroupBeforePreviousTabGroupCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.MoveTabGroupBeforePreviousTabGroup();
	}

	[ExportMenuItem(Header = "res:MergeAllTabGroupsCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSCLOSE, Order = 40)]
	sealed class MergeAllTabGroupsCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MergeAllTabGroupsCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.MergeAllTabGroupsCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.MergeAllTabGroups();
	}

	[ExportMenuItem(Header = "res:UseVerticalTabGroupsCommand", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSVERT, Order = 0)]
	sealed class UseVerticalTabGroupsCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		UseVerticalTabGroupsCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.UseVerticalTabGroupsCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.UseVerticalTabGroups();
	}

	[ExportMenuItem(Header = "res:UseHorizontalTabGroupsCommand", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSVERT, Order = 10)]
	sealed class UseHorizontalTabGroupsCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		UseHorizontalTabGroupsCtxMenuCommand(IDsToolWindowService toolWindowService)
			: base(toolWindowService) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) => context.ToolWindowGroupService.UseHorizontalTabGroupsCanExecute;
		public override void Execute(ToolWindowGroupContext context) => context.ToolWindowGroupService.UseHorizontalTabGroups();
	}
}
