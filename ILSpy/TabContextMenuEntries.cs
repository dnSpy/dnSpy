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
using System.Windows.Input;
using ICSharpCode.ILSpy;

namespace dnSpy.Tabs {
	abstract class TabGroupContextMenuEntry : IContextMenuEntry {
		public bool IsVisible(TextViewContext context) {
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				IsVisibleInternal();
		}

		public bool IsEnabled(TextViewContext context) {
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				IsEnabledInternal();
		}

		public void Execute(TextViewContext context) {
			ExecuteInternal();
		}

		protected virtual bool IsVisibleInternal() {
			return true;
		}

		protected virtual bool IsEnabledInternal() {
			return IsVisibleInternal();
		}

		protected abstract void ExecuteInternal();
	}

	[ExportContextMenuEntry(Header = "_Close", Order = 101, InputGestureText = "Ctrl+W", Category = "Tabs")]
	class CloseTabContextMenuEntry : IContextMenuEntry {
		public bool IsVisible(TextViewContext context) {
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				MainWindow.Instance.CloseActiveTabCanExecute();
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public void Execute(TextViewContext context) {
			MainWindow.Instance.CloseActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "C_lose All Tabs", Order = 110, Category = "Tabs", Icon = "CloseDocuments")]
	class CloseAllTabsContextMenuEntry : IContextMenuEntry {
		public bool IsVisible(TextViewContext context) {
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				MainWindow.Instance.CloseAllTabsCanExecute();
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public void Execute(TextViewContext context) {
			MainWindow.Instance.CloseAllTabs();
		}
	}

	[ExportContextMenuEntry(Header = "Close _All But This", Order = 120, Category = "Tabs")]
	class CloseAllTabsButThisContextMenuEntry : IContextMenuEntry {
		public bool IsVisible(TextViewContext context) {
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				MainWindow.Instance.ActiveTabState != null;
		}

		public bool IsEnabled(TextViewContext context) {
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				MainWindow.Instance.CloseAllButActiveTabCanExecute();
		}

		public void Execute(TextViewContext context) {
			MainWindow.Instance.CloseAllButActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "Open in New _Tab", Order = 130, InputGestureText = "Ctrl+T", Category = "Tabs")]
	class OpenInNewTabContextMenuEntry : IContextMenuEntry {
		public bool IsVisible(TextViewContext context) {
			return context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Length > 0 &&
				context.TreeView == MainWindow.Instance.treeView;
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public void Execute(TextViewContext context) {
			MainWindow.Instance.OpenNewTab();
		}
	}

	[ExportContextMenuEntry(Header = "New _Tab", Order = 140, Category = "Tabs")]
	class NewTabContextMenuEntry : IContextMenuEntry {
		public bool IsVisible(TextViewContext context) {
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				MainWindow.Instance.CloneActiveTabCanExecute();
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public void Execute(TextViewContext context) {
			MainWindow.Instance.CloneActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "Open in New _Tab", Order = 150, Category = "Tabs")]
	class OpenReferenceInNewTabContextMenuEntry : IContextMenuEntry2 {
		public bool IsVisible(TextViewContext context) {
			return context.TextView != null &&
				context.Reference != null;
		}

		public bool IsEnabled(TextViewContext context) {
			return true;
		}

		public void Execute(TextViewContext context) {
			MainWindow.Instance.OpenReferenceInNewTab(context.TextView, context.Reference);
		}

		public void Initialize(TextViewContext context, MenuItem menuItem) {
			menuItem.InputGestureText = context.OpenedFromKeyboard ? "Ctrl+F12" : "Ctrl+Click";
		}
	}

	[ExportContextMenuEntry(Header = "New Hori_zontal Tab Group", Order = 160, Category = "TabGroups", Icon = "HorizontalTabGroup")]
	sealed class NewHorizontalTabGroupContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.NewHorizontalTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.NewHorizontalTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "New _Vertical Tab Group", Order = 170, Category = "TabGroups", Icon = "VerticalTabGroup")]
	sealed class NewVerticalTabGroupContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.NewVerticalTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.NewVerticalTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Move to Ne_xt Tab Group", Order = 180, Category = "TabGroups")]
	sealed class MoveToNextTabGroupContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.MoveToNextTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveToNextTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Move All to Next Tab Group", Order = 190, Category = "TabGroups")]
	sealed class MoveAllToNextTabGroupContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.MoveAllToNextTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveAllToNextTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Move to P_revious Tab Group", Order = 200, Category = "TabGroups")]
	sealed class MoveToPreviousTabGroupContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.MoveToPreviousTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveToPreviousTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Move All to Previous Tab Group", Order = 210, Category = "TabGroups")]
	sealed class MoveAllToPreviousTabGroupContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.MoveAllToPreviousTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveAllToPreviousTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Close Tab Group", Order = 220, Category = "TabGroupsMisc")]
	sealed class CloseTabGroupContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.CloseTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.CloseTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Close All Tab Groups But This", Order = 230, Category = "TabGroupsMisc")]
	sealed class CloseAllTabGroupsButThisContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.CloseAllTabGroupsButThisCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.CloseAllTabGroupsButThis();
		}
	}

	[ExportContextMenuEntry(Header = "Move Tab Group After Next Tab Group", Order = 240, Category = "TabGroupsMisc")]
	sealed class MoveTabGroupAfterNextTabGroupContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.MoveTabGroupAfterNextTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveTabGroupAfterNextTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Move Tab Group Before Previous Tab Group", Order = 250, Category = "TabGroupsMisc")]
	sealed class MoveTabGroupBeforePreviousTabGroupContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.MoveTabGroupBeforePreviousTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveTabGroupBeforePreviousTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Merge All Tab Groups", Order = 260, Category = "TabGroupsMisc")]
	sealed class MergeAllTabGroupsContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.MergeAllTabGroupsCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MergeAllTabGroups();
		}
	}

	[ExportContextMenuEntry(Header = "Use Vertical Tab Groups", Order = 300, Category = "TabGroupsMisc2", Icon = "VerticalTabGroup")]
	sealed class UseVerticalTabGroupsContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.UseVerticalTabGroupsCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.UseVerticalTabGroups();
		}
	}

	[ExportContextMenuEntry(Header = "Use Horizontal Tab Groups", Order = 310, Category = "TabGroupsMisc2", Icon = "HorizontalTabGroup")]
	sealed class UseHorizontalTabGroupsContextMenuEntry : TabGroupContextMenuEntry {
		protected override bool IsVisibleInternal() {
			return MainWindow.Instance.UseHorizontalTabGroupsCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.UseHorizontalTabGroups();
		}
	}

	abstract class TabGroupCommand : ICommand, IMainMenuCommand {
		bool? cachedCanExecuteState;
		bool? cachedIsVisibleState;

		static void Register(TabGroupCommand cmd) {
			commands.Add(cmd);
		}
		static readonly List<TabGroupCommand> commands = new List<TabGroupCommand>();

		static TabGroupCommand() {
			MainWindow.Instance.OnActiveTabStateChanged += (s, e) => UpdateState();
			MainWindow.Instance.OnTabStateChanged += (s, e) => UpdateState();
			MainWindow.Instance.OnTabStateAdded += (s, e) => UpdateState();
			MainWindow.Instance.OnTabStateRemoved += (s, e) => UpdateState();
			MainWindow.Instance.OnTabStateAttached += (s, e) => UpdateState();
			MainWindow.Instance.OnTabStateDetached += (s, e) => UpdateState();
			MainWindow.Instance.OnTabGroupAdded += (s, e) => UpdateState();
			MainWindow.Instance.OnTabGroupRemoved += (s, e) => UpdateState();
			MainWindow.Instance.OnTabGroupSelected += (s, e) => UpdateState();
			MainWindow.Instance.OnTabGroupSwapped += (s, e) => UpdateState();
			MainWindow.Instance.OnTabGroupsOrientationChanged += (s, e) => UpdateState();
		}

		protected TabGroupCommand() {
			cachedCanExecuteState = CanExecuteInternal();
			cachedIsVisibleState = IsVisibleInternal();
			Register(this);
		}

		public bool IsVisible {
			get { return cachedIsVisibleState.Value; }
		}

		public void Execute(object parameter) {
			ExecuteInternal();
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter) {
			return cachedCanExecuteState.Value;
		}

		static void UpdateState() {
			bool updateWindowsMenu = false;
			foreach (var cmd in commands) {
				bool newState = cmd.CanExecuteInternal();
				var oldState = cmd.cachedCanExecuteState;
				if (oldState.Value != newState) {
					cmd.cachedCanExecuteState = newState;

					if (cmd.CanExecuteChanged != null)
						cmd.CanExecuteChanged(cmd, EventArgs.Empty);
				}

				newState = cmd.IsVisibleInternal();
				oldState = cmd.cachedIsVisibleState;
				if (oldState.Value != newState) {
					cmd.cachedIsVisibleState = newState;
					updateWindowsMenu = true;
				}

				if (cmd.MustUpdateWindowsMenu())
					updateWindowsMenu = true;
			}

			if (updateWindowsMenu)
				MainWindow.Instance.UpdateMainSubMenu("_Window");
		}

		protected abstract bool CanExecuteInternal();
		protected abstract void ExecuteInternal();

		protected virtual bool IsVisibleInternal() {
			return CanExecuteInternal();
		}

		protected virtual bool IsEnabledInternal() {
			return true;
		}

		protected virtual bool MustUpdateWindowsMenu() {
			return false;
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "Window", MenuHeader = "_New Window", MenuOrder = 9000, MenuIcon = "NewWindow")]
	sealed class NewWindowCommand : TabGroupCommand {
		protected override bool IsVisibleInternal() {
			return true;
		}

		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.CloneActiveTabCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.CloneActiveTab();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "Window", MenuHeader = "_Close", MenuOrder = 9010, MenuInputGestureText = "Ctrl+W")]
	class CloseTabCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.CloseActiveTabCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.CloseActiveTab();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", MenuHeader = "New Hori_zontal Tab Group", MenuOrder = 9200, MenuIcon = "HorizontalTabGroup")]
	sealed class NewHorizontalTabGroupCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.NewHorizontalTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.NewHorizontalTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", MenuHeader = "New _Vertical Tab Group", MenuOrder = 9210, MenuIcon = "VerticalTabGroup")]
	sealed class NewVerticalTabGroupCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.NewVerticalTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.NewVerticalTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", MenuHeader = "Move to Ne_xt Tab Group", MenuOrder = 9220)]
	sealed class MoveToNextTabGroupCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.MoveToNextTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveToNextTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", MenuHeader = "Move All to Next Tab Group", MenuOrder = 9230)]
	sealed class MoveAllToNextTabGroupCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.MoveAllToNextTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveAllToNextTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", MenuHeader = "Move to P_revious Tab Group", MenuOrder = 9240)]
	sealed class MoveToPreviousTabGroupCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.MoveToPreviousTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveToPreviousTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", MenuHeader = "Move All to Previous Tab Group", MenuOrder = 9250)]
	sealed class MoveAllToPreviousTabGroupCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.MoveAllToPreviousTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveAllToPreviousTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", MenuHeader = "C_lose All Tabs", MenuOrder = 9260, MenuIcon = "CloseDocuments")]
	sealed class CloseAllTabsCommand : TabGroupCommand {
		protected override bool IsVisibleInternal() {
			return true;
		}

		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.CloseAllTabsCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.CloseAllTabs();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroupsMisc", MenuHeader = "Close Tab Group", MenuOrder = 9270)]
	sealed class CloseTabGroupCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.CloseTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.CloseTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroupsMisc", MenuHeader = "Close All Tab Groups But This", MenuOrder = 9280)]
	sealed class CloseAllTabGroupsButThisCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.CloseAllTabGroupsButThisCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.CloseAllTabGroupsButThis();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroupsMisc", MenuHeader = "Move Tab Group After Next Tab Group", MenuOrder = 9290)]
	sealed class MoveTabGroupAfterNextTabGroupCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.MoveTabGroupAfterNextTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveTabGroupAfterNextTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroupsMisc", MenuHeader = "Move Tab Group Before Previous Tab Group", MenuOrder = 9300)]
	sealed class MoveTabGroupBeforePreviousTabGroupCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.MoveTabGroupBeforePreviousTabGroupCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MoveTabGroupBeforePreviousTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroupsMisc", MenuHeader = "Merge All Tab Groups", MenuOrder = 9310)]
	sealed class MergeAllTabGroupsCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.MergeAllTabGroupsCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.MergeAllTabGroups();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroupsMisc2", MenuHeader = "Use Vertical Tab Groups", MenuOrder = 9400, MenuIcon = "VerticalTabGroup")]
	sealed class UseVerticalTabGroupsCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.UseVerticalTabGroupsCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.UseVerticalTabGroups();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroupsMisc2", MenuHeader = "Use Horizontal Tab Groups", MenuOrder = 9410, MenuIcon = "HorizontalTabGroup")]
	sealed class UseHorizontalTabGroupsCommand : TabGroupCommand {
		protected override bool CanExecuteInternal() {
			return MainWindow.Instance.UseHorizontalTabGroupsCanExecute();
		}

		protected override void ExecuteInternal() {
			MainWindow.Instance.UseHorizontalTabGroups();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroupsWindows", MenuOrder = 9500)]
	sealed class DecompilerWindowsCommand : TabGroupCommand, IMenuItemProvider {
		readonly MenuItem cachedLastMenuItem;
		readonly MenuItem[] cachedMenuItems = new MenuItem[10];

		public DecompilerWindowsCommand() {
			MainWindow.Instance.OnTabHeaderChanged += (s, e) => MainWindow.Instance.UpdateMainSubMenu("_Window");

			cachedLastMenuItem = new MenuItem();
			cachedLastMenuItem.Header = "_Windows…";
			cachedLastMenuItem.Click += (s, e) => MainWindow.Instance.ShowDecompilerTabsWindow();

			for (int i = 0; i < cachedMenuItems.Length; i++)
				cachedMenuItems[i] = new MenuItem();
		}

		protected override bool CanExecuteInternal() {
			return true;
		}

		protected override void ExecuteInternal() {
		}

		protected override bool MustUpdateWindowsMenu() {
			return true;
		}

		public IEnumerable<MenuItem> CreateMenuItems(MenuItem cachedMenuItem) {
			MenuItem menuItem;
			const int MAX_TABS = 10;
			int index = 0;
			foreach (var tabState in MainWindow.Instance.GetTabStateInOrder()) {
				menuItem = cachedMenuItems[index];
				menuItem.IsChecked = index == 0;
				menuItem.Header = string.Format("{0} {1}", index + 1 == 10 ? "1_0" : string.Format("_{0}", index + 1), UIUtils.EscapeMenuItemHeader(tabState.ShortHeader));

				var tabStateTmp = tabState;
				menuItem.Click += (s, e) => MainWindow.Instance.SetActiveTab(tabStateTmp);
				yield return menuItem;

				if (++index >= MAX_TABS)
					break;
			}

			yield return cachedLastMenuItem;
		}
	}
}
