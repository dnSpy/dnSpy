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
using dnSpy.Shared.Menus;

namespace dnSpy.MainApp {
	sealed class ToolWindowGroupContext {
		public readonly IMainToolWindowManager MainToolWindowManager;
		public readonly IToolWindowGroupManager ToolWindowGroupManager;
		public readonly IToolWindowGroup ToolWindowGroup;

		public ToolWindowGroupContext(IMainToolWindowManager mainToolWindowManager, IToolWindowGroup toolWindowGroup) {
			this.MainToolWindowManager = mainToolWindowManager;
			this.ToolWindowGroupManager = toolWindowGroup.ToolWindowGroupManager;
			this.ToolWindowGroup = toolWindowGroup;
		}
	}

	abstract class CtxMenuToolWindowGroupCommand : MenuItemBase<ToolWindowGroupContext> {
		protected sealed override object CachedContextKey {
			get { return ContextKey; }
		}
		static readonly object ContextKey = new object();

		protected sealed override ToolWindowGroupContext CreateContext(IMenuItemContext context) {
			return CreateContextInternal(context);
		}

		readonly IMainToolWindowManager mainToolWindowManager;

		protected CtxMenuToolWindowGroupCommand(IMainToolWindowManager mainToolWindowManager) {
			this.mainToolWindowManager = mainToolWindowManager;
		}

		protected ToolWindowGroupContext CreateContextInternal(IMenuItemContext context) {
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_TOOLWINDOW_TABCONTROL_GUID))
				return null;
			var twg = context.Find<IToolWindowGroup>();
			if (twg == null || !mainToolWindowManager.Owns(twg))
				return null;
			return new ToolWindowGroupContext(mainToolWindowManager, twg);
		}
	}

	[ExportMenuItem(Header = "res:HideToolWindowCommand", Icon = "tableviewnameonly", Group = MenuConstants.GROUP_CTX_TOOLWINS_CLOSE, Order = 10)]
	sealed class HideTWCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		HideTWCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroup.CloseActiveTabCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroup.CloseActiveTab();
		}
	}

	[ExportMenuItem(Header = "res:HideAllToolWindowsCommand", Icon = "CloseDocuments", Group = MenuConstants.GROUP_CTX_TOOLWINS_CLOSE, Order = 20)]
	sealed class CloseAllTabsTWCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		CloseAllTabsTWCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.CloseAllTabsCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.CloseAllTabs();
		}
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
		MoveTWCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override void Execute(ToolWindowGroupContext context) {
			Debug.Fail("Shouldn't be here");
		}
	}

	[ExportMenuItem(Header = "res:MoveToolWindowGroupCommand", Guid = Constants.MOVE_GROUP_GUID, Group = MenuConstants.GROUP_CTX_TOOLWINS_CLOSE, Order = 40)]
	sealed class MoveGroupTWCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveGroupTWCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroup.TabContents.Count() > 1;
		}

		public override void Execute(ToolWindowGroupContext context) {
			Debug.Fail("Shouldn't be here");
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_CONTENT_GUID, Header = "res:MoveTopCommand", Icon = "toolstrippaneltop", Group = Constants.GROUP_MOVE_CONTENT, Order = 0)]
	sealed class MoveTWTopCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTWTopCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) {
			return context.MainToolWindowManager.CanMove(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Top);
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.MainToolWindowManager.Move(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Top);
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_CONTENT_GUID, Header = "res:MoveLeftCommand", Icon = "toolstrippanelleft", Group = Constants.GROUP_MOVE_CONTENT, Order = 10)]
	sealed class MoveTWLeftCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTWLeftCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) {
			return context.MainToolWindowManager.CanMove(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Left);
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.MainToolWindowManager.Move(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Left);
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_CONTENT_GUID, Header = "res:MoveRightCommand", Icon = "toolstrippanelright", Group = Constants.GROUP_MOVE_CONTENT, Order = 20)]
	sealed class MoveTWRightCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTWRightCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) {
			return context.MainToolWindowManager.CanMove(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Right);
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.MainToolWindowManager.Move(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Right);
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_CONTENT_GUID, Header = "res:MoveBottomCommand", Icon = "toolstrippanelbottom", Group = Constants.GROUP_MOVE_CONTENT, Order = 30)]
	sealed class MoveTWBottomCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTWBottomCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) {
			return context.MainToolWindowManager.CanMove(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Bottom);
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.MainToolWindowManager.Move(context.ToolWindowGroup.ActiveTabContent, AppToolWindowLocation.Bottom);
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_GROUP_GUID, Header = "res:MoveTopCommand", Icon = "toolstrippaneltop", Group = Constants.GROUP_MOVE_GROUP, Order = 0)]
	sealed class MoveGroupTWTopCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveGroupTWTopCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) {
			return context.MainToolWindowManager.CanMove(context.ToolWindowGroup, AppToolWindowLocation.Top);
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.MainToolWindowManager.Move(context.ToolWindowGroup, AppToolWindowLocation.Top);
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_GROUP_GUID, Header = "res:MoveLeftCommand", Icon = "toolstrippanelleft", Group = Constants.GROUP_MOVE_GROUP, Order = 10)]
	sealed class MoveGroupTWLeftCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveGroupTWLeftCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) {
			return context.MainToolWindowManager.CanMove(context.ToolWindowGroup, AppToolWindowLocation.Left);
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.MainToolWindowManager.Move(context.ToolWindowGroup, AppToolWindowLocation.Left);
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_GROUP_GUID, Header = "res:MoveRightCommand", Icon = "toolstrippanelright", Group = Constants.GROUP_MOVE_GROUP, Order = 20)]
	sealed class MoveGroupTWRightCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveGroupTWRightCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) {
			return context.MainToolWindowManager.CanMove(context.ToolWindowGroup, AppToolWindowLocation.Right);
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.MainToolWindowManager.Move(context.ToolWindowGroup, AppToolWindowLocation.Right);
		}
	}

	[ExportMenuItem(OwnerGuid = Constants.MOVE_GROUP_GUID, Header = "res:MoveBottomCommand", Icon = "toolstrippanelbottom", Group = Constants.GROUP_MOVE_GROUP, Order = 30)]
	sealed class MoveGroupTWBottomCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveGroupTWBottomCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsEnabled(ToolWindowGroupContext context) {
			return context.MainToolWindowManager.CanMove(context.ToolWindowGroup, AppToolWindowLocation.Bottom);
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.MainToolWindowManager.Move(context.ToolWindowGroup, AppToolWindowLocation.Bottom);
		}
	}

	[ExportMenuItem(Header = "res:NewHorizontalTabGroupCommand", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 0)]
	sealed class NewHorizontalTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		NewHorizontalTabGroupCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.NewHorizontalTabGroupCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.NewHorizontalTabGroup();
		}
	}

	[ExportMenuItem(Header = "res:NewVerticalTabGroupCommand", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 10)]
	sealed class NewVerticalTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		NewVerticalTabGroupCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.NewVerticalTabGroupCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.NewVerticalTabGroup();
		}
	}

	[ExportMenuItem(Header = "res:MoveToNextTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 20)]
	sealed class MoveToNextTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveToNextTabGroupCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.MoveToNextTabGroupCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.MoveToNextTabGroup();
		}
	}

	[ExportMenuItem(Header = "res:MoveAllTabsToNextTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 30)]
	sealed class MoveAllToNextTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveAllToNextTabGroupCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.MoveAllToNextTabGroupCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.MoveAllToNextTabGroup();
		}
	}

	[ExportMenuItem(Header = "res:MoveToPreviousTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 40)]
	sealed class MoveToPreviousTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveToPreviousTabGroupCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.MoveToPreviousTabGroupCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.MoveToPreviousTabGroup();
		}
	}

	[ExportMenuItem(Header = "res:MoveAllToPreviousTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPS, Order = 50)]
	sealed class MoveAllToPreviousTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveAllToPreviousTabGroupCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.MoveAllToPreviousTabGroupCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.MoveAllToPreviousTabGroup();
		}
	}

	[ExportMenuItem(Header = "res:CloseTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSCLOSE, Order = 0)]
	sealed class CloseTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		CloseTabGroupCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.CloseTabGroupCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.CloseTabGroup();
		}
	}

	[ExportMenuItem(Header = "res:CloseAllTabGroupsButThisCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSCLOSE, Order = 10)]
	sealed class CloseAllTabGroupsButThisCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		CloseAllTabGroupsButThisCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.CloseAllTabGroupsButThisCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.CloseAllTabGroupsButThis();
		}
	}

	[ExportMenuItem(Header = "res:MoveTabGroupAfterNextTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSCLOSE, Order = 20)]
	sealed class MoveTabGroupAfterNextTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTabGroupAfterNextTabGroupCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.MoveTabGroupAfterNextTabGroupCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.MoveTabGroupAfterNextTabGroup();
		}
	}

	[ExportMenuItem(Header = "res:MoveTabGroupBeforePreviousTabGroupCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSCLOSE, Order = 30)]
	sealed class MoveTabGroupBeforePreviousTabGroupCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MoveTabGroupBeforePreviousTabGroupCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.MoveTabGroupBeforePreviousTabGroupCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.MoveTabGroupBeforePreviousTabGroup();
		}
	}

	[ExportMenuItem(Header = "res:MergeAllTabGroupsCommand", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSCLOSE, Order = 40)]
	sealed class MergeAllTabGroupsCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		MergeAllTabGroupsCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.MergeAllTabGroupsCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.MergeAllTabGroups();
		}
	}

	[ExportMenuItem(Header = "res:UseVerticalTabGroupsCommand", Icon = "VerticalTabGroup", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSVERT, Order = 0)]
	sealed class UseVerticalTabGroupsCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		UseVerticalTabGroupsCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.UseVerticalTabGroupsCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.UseVerticalTabGroups();
		}
	}

	[ExportMenuItem(Header = "res:UseHorizontalTabGroupsCommand", Icon = "HorizontalTabGroup", Group = MenuConstants.GROUP_CTX_TOOLWINS_GROUPSVERT, Order = 10)]
	sealed class UseHorizontalTabGroupsCtxMenuCommand : CtxMenuToolWindowGroupCommand {
		[ImportingConstructor]
		UseHorizontalTabGroupsCtxMenuCommand(IMainToolWindowManager mainToolWindowManager)
			: base(mainToolWindowManager) {
		}

		public override bool IsVisible(ToolWindowGroupContext context) {
			return context.ToolWindowGroupManager.UseHorizontalTabGroupsCanExecute;
		}

		public override void Execute(ToolWindowGroupContext context) {
			context.ToolWindowGroupManager.UseHorizontalTabGroups();
		}
	}
}
