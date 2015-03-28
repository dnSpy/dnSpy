
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy
{
	sealed class TabManager<TState> where TState : TabState
	{
		readonly TabControl tabControl;

		internal TState ActiveTabState {
			get {
				int index = tabControl.SelectedIndex == -1 ? 0 : tabControl.SelectedIndex;
				if (index >= tabControl.Items.Count)
					return null;
				var item = tabControl.Items[index] as TabItem;
				return item == null ? null : (TState)item.Tag;
			}
		}

		public IEnumerable<TState> AllTabStates {
			get {
				foreach (var item in tabControl.Items) {
					var tabItem = item as TabItem;
					if (tabItem == null)
						continue;
					Debug.Assert(tabItem.Tag is TState);
					yield return (TState)tabItem.Tag;
				}
			}
		}

		Action<TState, TState> OnSelectionChanged;
		Action<TState> OnCreateNewTabState;
		Action<TState> OnRemoveTabState;

		public TabManager(TabControl tabControl, Action<TState, TState> onSelectionChanged, Action<TState> onCreateNewTabState, Action<TState> onRemoveTabState)
		{
			this.tabControl = tabControl;
			this.tabControl.SelectionChanged += tabControl_SelectionChanged;
			this.OnSelectionChanged = onSelectionChanged;
			this.OnCreateNewTabState = onCreateNewTabState;
			this.OnRemoveTabState = onRemoveTabState;
		}

		void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender != tabControl || e.Source != tabControl)
				return;
			Debug.Assert(e.RemovedItems.Count <= 1);
			Debug.Assert(e.AddedItems.Count <= 1);

			var oldState = e.RemovedItems.Count >= 1 ? (TState)((TabItem)e.RemovedItems[0]).Tag : null;
			var newState = e.AddedItems.Count >= 1 ? (TState)((TabItem)e.AddedItems[0]).Tag : null;

			OnSelectionChanged(oldState, newState);
		}

		internal TState AddNewTabState(TState tabState)
		{
			tabControl.Items.Add(tabState.TabItem);
			OnCreateNewTabState(tabState);
			return tabState;
		}

		// This method is only executed when the text editor does NOT have keyboard focus
		void SelectTab(int index)
		{
			if (tabControl.Items.Count == 0)
				return;
			if (index < 0)
				index += tabControl.Items.Count;
			index = index % tabControl.Items.Count;
			tabControl.SelectedIndex = index;
		}

		public void SetSelectedIndex(int index)
		{
			int selectedIndex = unchecked((uint)index) < (uint)tabControl.Items.Count ?
						index : tabControl.Items.Count == 0 ? -1 : 0;
			tabControl.SelectedIndex = selectedIndex;
		}

		public int GetSelectedIndex()
		{
			return tabControl.SelectedIndex;
		}

		public void SetSelectedTab(TabState tabState)
		{
			tabControl.SelectedItem = tabState.TabItem;
		}

		public void SelectNextTab()
		{
			SelectTab(tabControl.SelectedIndex + 1);
		}

		public bool SelectNextTabPossible()
		{
			return tabControl.Items.Count > 1;
		}

		public void SelectPreviousTab()
		{
			SelectTab(tabControl.SelectedIndex - 1);
		}

		public bool SelectPreviousTabPossible()
		{
			return tabControl.Items.Count > 1;
		}

		public void CloseActiveTab()
		{
			RemoveTabState(ActiveTabState);
		}

		public bool CloseActiveTabPossible()
		{
			return ActiveTabState != null;
		}

		public void CloseAllButActiveTab()
		{
			var activeTab = ActiveTabState;
			if (activeTab == null)
				return;
			foreach (var tabState in AllTabStates.ToArray()) {
				if (tabState != activeTab)
					RemoveTabState(tabState);
			}
		}

		public bool CloseAllButActiveTabPossible()
		{
			return tabControl.Items.Count > 1;
		}

		public void RemoveAllTabStates()
		{
			var allTabStates = AllTabStates.ToArray();
			tabControl.Items.Clear();
			foreach (var tabState in allTabStates)
				RemoveTabStateInternal(tabState);
		}

		public void RemoveTabState(TState tabState)
		{
			if (tabState == null)
				return;
			int index = tabControl.Items.IndexOf(tabState.TabItem);
			Debug.Assert(index >= 0);
			if (index < 0)
				return;

			tabControl.SelectedIndex = index - 1;
			tabControl.Items.RemoveAt(index);

			RemoveTabStateInternal(tabState);
		}

		void RemoveTabStateInternal(TState tabState)
		{
			OnRemoveTabState(tabState);
			tabState.Dispose();
		}
	}
}
