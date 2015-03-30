using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace ICSharpCode.ILSpy
{
	[ExportContextMenuEntry(Header = "Open in New _Tab", Order = 130, InputGestureText = "Ctrl+T", Category = "Tabs")]
	class OpenInNewTabContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Length > 0 &&
				context.TreeView == MainWindow.Instance.treeView;
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.OpenNewTab();
		}
	}

	[ExportContextMenuEntry(Header = "_Close", Order = 100, InputGestureText = "Ctrl+W", Category = "Tabs")]
	class CloseTabContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				MainWindow.Instance.CloseActiveTabCanExecute();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.CloseActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "C_lose All Tabs", Order = 110, Category = "Tabs")]
	class CloseAllTabsContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				MainWindow.Instance.CloseAllTabsCanExecute();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.CloseAllTabs();
		}
	}

	[ExportContextMenuEntry(Header = "Close _All But This", Order = 120, Category = "Tabs")]
	class CloseAllTabsButThisContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				MainWindow.Instance.ActiveTabState != null;
		}

		public bool IsEnabled(TextViewContext context)
		{
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				MainWindow.Instance.CloseAllButActiveTabCanExecute();
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.CloseAllButActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "New _Tab", Order = 130, Category = "Tabs")]
	class NewTabContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				MainWindow.Instance.CloneActiveTabCanExecute();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.CloneActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "Open in New _Tab", Order = 140, Category = "Tabs")]
	class OpenReferenceInNewTabContextMenuEntry : IContextMenuEntry2
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.TextView != null &&
				context.Reference != null;
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.OpenReferenceInNewTab(context.TextView, context.Reference);
		}

		public void Initialize(TextViewContext context, MenuItem menuItem)
		{
			menuItem.InputGestureText = context.OpenedFromKeyboard ? "Ctrl+F12" : "Ctrl+Click";
		}
	}

	abstract class TabGroupCommand : ICommand, IMainMenuCommand
	{
		bool? cachedCanExecuteState;
		bool? cachedIsVisibleState;

		protected TabGroupCommand()
		{
			cachedCanExecuteState = CanExecuteInternal();
			cachedIsVisibleState = IsVisibleInternal();
			MainWindow.Instance.OnActiveDecompilerTextViewChanged += (s, e) => UpdateState();
			MainWindow.Instance.OnDecompilerTextViewChanged += (s, e) => UpdateState();
			MainWindow.Instance.OnDecompilerTextViewAdded += (s, e) => UpdateState();
			MainWindow.Instance.OnDecompilerTextViewRemoved += (s, e) => UpdateState();
			MainWindow.Instance.OnTabGroupAdded += (s, e) => UpdateState();
			MainWindow.Instance.OnTabGroupRemoved += (s, e) => UpdateState();
			MainWindow.Instance.OnTabGroupSelected += (s, e) => UpdateState();
			MainWindow.Instance.OnTabGroupSwapped += (s, e) => UpdateState();
		}

		public bool IsVisible {
			get { return cachedIsVisibleState.Value; }
		}

		public void Execute(object parameter)
		{
			ExecuteInternal();
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter)
		{
			return cachedCanExecuteState.Value;
		}

		protected void UpdateState()
		{
			bool newState = CanExecuteInternal();
			var oldState = cachedCanExecuteState;
			if (oldState.Value != newState) {
				cachedCanExecuteState = newState;

				if (CanExecuteChanged != null)
					CanExecuteChanged(this, EventArgs.Empty);
			}

			newState = IsVisibleInternal();
			oldState = cachedIsVisibleState;
			if (oldState.Value != newState) {
				cachedIsVisibleState = newState;

				MainWindow.Instance.UpdateMainSubMenu("_Window");
			}
		}

		protected abstract bool CanExecuteInternal();
		protected abstract void ExecuteInternal();

		protected virtual bool IsVisibleInternal()
		{
			return CanExecuteInternal();
		}

		protected virtual bool IsEnabledInternal()
		{
			return true;
		}
	}

	abstract class TabGroupContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				IsVisibleInternal();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				IsEnabledInternal();
		}

		public void Execute(TextViewContext context)
		{
			ExecuteInternal();
		}

		protected virtual bool IsVisibleInternal()
		{
			return true;
		}

		protected virtual bool IsEnabledInternal()
		{
			return IsVisibleInternal();
		}

		protected abstract void ExecuteInternal();
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "Window", Header = "_New Window", MenuOrder = 9000)]
	sealed class NewWindowCommand : TabGroupCommand
	{
		protected override bool IsVisibleInternal()
		{
			return true;
		}

		protected override bool CanExecuteInternal()
		{
			return MainWindow.Instance.CloneActiveTabCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.CloneActiveTab();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", Header = "New Hori_zontal Tab Group", MenuOrder = 9200)]
	sealed class NewHorizontalTabGroupCommand : TabGroupCommand
	{
		protected override bool CanExecuteInternal()
		{
			return MainWindow.Instance.NewHorizontalTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.NewHorizontalTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "New Hori_zontal Tab Group", Order = 140, Category = "TabGroups")]
	sealed class NewHorizontalTabContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.NewHorizontalTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.NewHorizontalTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", Header = "New _Vertical Tab Group", MenuOrder = 9210)]
	sealed class NewVerticalTabGroupCommand : TabGroupCommand
	{
		protected override bool CanExecuteInternal()
		{
			return MainWindow.Instance.NewVerticalTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.NewVerticalTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "New _Vertical Tab Group", Order = 150, Category = "TabGroups")]
	sealed class NewVerticalTabContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.NewVerticalTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.NewVerticalTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", Header = "Move to Ne_xt Tab Group", MenuOrder = 9220)]
	sealed class MoveToNextTabGroupCommand : TabGroupCommand
	{
		protected override bool CanExecuteInternal()
		{
			return MainWindow.Instance.MoveToNextTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.MoveToNextTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Move to Ne_xt Tab Group", Order = 160, Category = "TabGroups")]
	sealed class MoveToNextTabGroupContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.MoveToNextTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.MoveToNextTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Move All to Next Tab Group", Order = 170, Category = "TabGroups")]
	sealed class MoveAllToNextTabGroupContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.MoveAllToNextTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.MoveAllToNextTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", Header = "Move to P_revious Tab Group", MenuOrder = 9230)]
	sealed class MoveToPreviousTabGroupCommand : TabGroupCommand
	{
		protected override bool CanExecuteInternal()
		{
			return MainWindow.Instance.MoveToPreviousTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.MoveToPreviousTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Move to P_revious Tab Group", Order = 180, Category = "TabGroups")]
	sealed class MoveToPreviousTabGroupContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.MoveToPreviousTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.MoveToPreviousTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Move All to Previous Tab Group", Order = 190, Category = "TabGroups")]
	sealed class MoveAllToPreviousTabGroupContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.MoveAllToPreviousTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.MoveAllToPreviousTabGroup();
		}
	}

	[ExportMainMenuCommand(Menu = "_Window", MenuCategory = "TabGroups", Header = "C_lose All Tabs", MenuOrder = 9240)]
	sealed class CloseAllTabsCommand : TabGroupCommand
	{
		protected override bool IsVisibleInternal()
		{
			return true;
		}

		protected override bool CanExecuteInternal()
		{
			return MainWindow.Instance.CloseAllTabsCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.CloseAllTabs();
		}
	}

	[ExportContextMenuEntry(Header = "Merge All Tab Groups", Order = 200, Category = "TabGroupsMisc")]
	sealed class MergeAllTabGroupsContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.MergeAllTabGroupsCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.MergeAllTabGroups();
		}
	}

	[ExportContextMenuEntry(Header = "Switch to Vertical Tab Groups", Order = 210, Category = "TabGroupsMisc")]
	sealed class SwitchToVerticalTabGroupsContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.SwitchToVerticalTabGroupsCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.SwitchToVerticalTabGroups();
		}
	}

	[ExportContextMenuEntry(Header = "Switch to Horizontal Tab Groups", Order = 220, Category = "TabGroupsMisc")]
	sealed class SwitchToHorizontalTabGroupsContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.SwitchToHorizontalTabGroupsCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.SwitchToHorizontalTabGroups();
		}
	}

	[ExportContextMenuEntry(Header = "Close Tab Group", Order = 230, Category = "TabGroupsMisc")]
	sealed class CloseTabGroupContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.CloseTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.CloseTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Close All Tab Groups But This", Order = 240, Category = "TabGroupsMisc")]
	sealed class CloseAllTabGroupsButThisContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.CloseAllTabGroupsButThisCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.CloseAllTabGroupsButThis();
		}
	}

	[ExportContextMenuEntry(Header = "Move Tab Group After Next Tab Group", Order = 250, Category = "TabGroupsMisc")]
	sealed class MoveTabGroupAfterNextTabGroupContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.MoveTabGroupAfterNextTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.MoveTabGroupAfterNextTabGroup();
		}
	}

	[ExportContextMenuEntry(Header = "Move Tab Group Before Previous Tab Group", Order = 250, Category = "TabGroupsMisc")]
	sealed class MoveTabGroupBeforePreviousTabGroupContextMenuEntry : TabGroupContextMenuEntry
	{
		protected override bool IsVisibleInternal()
		{
			return MainWindow.Instance.MoveTabGroupBeforePreviousTabGroupCanExecute();
		}

		protected override void ExecuteInternal()
		{
			MainWindow.Instance.MoveTabGroupBeforePreviousTabGroup();
		}
	}
}
