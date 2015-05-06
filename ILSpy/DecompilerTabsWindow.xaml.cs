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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Interaction logic for DecompilerTabsWindow.xaml
	/// </summary>
	public partial class DecompilerTabsWindow : Window
	{
		readonly ObservableCollection<TabInfo> allTabs;
		internal TabStateDecompile LastActivatedTabState;

		class TabInfo
		{
			public TabStateDecompile TabState { get; set; }
			public string FirstModuleName { get; set; }
			public string FirstModuleFullName { get; set; }
		}

		public DecompilerTabsWindow()
		{
			InitializeComponent();
			UpdateButtonState();
			allTabs = CreateCollection();
			listView.ItemsSource = allTabs;

			var info = allTabs.FirstOrDefault(a => a.TabState == MainWindow.Instance.ActiveTabState);
			if (info != null) {
				listView.SelectedItem = info;
				listView.Focus();
				// We must wait a little bit for it to create the ListViewItem
				Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => {
					if (listView.SelectedItem != info)
						return;
					var item = listView.ItemContainerGenerator.ContainerFromItem(info) as ListViewItem;
					if (item != null) {
						item.Focus();
						listView.ScrollIntoView(item);
					}
				}));
			}
		}

		ObservableCollection<TabInfo> CreateCollection()
		{
			var list = new List<TabInfo>();
			foreach (var tabState in MainWindow.Instance.GetTabStateInOrder()) {
				var info = new TabInfo();
				info.TabState = tabState;
				var module = ILSpyTreeNode.GetModule(tabState.DecompiledNodes);
				if (module != null) {
					info.FirstModuleName = module.Name;
					info.FirstModuleFullName = module.Location;
				}
				else {
					info.FirstModuleName = string.Empty;
					info.FirstModuleFullName = string.Empty;
				}
				list.Add(info);
			}
			list.Sort((a, b) => StringComparer.InvariantCultureIgnoreCase.Compare(a.TabState.ShortHeader, b.TabState.ShortHeader));
			return new ObservableCollection<TabInfo>(list);
		}

		TabInfo[] GetSelectedItems()
		{
			var list = new TabInfo[listView.SelectedItems.Count];
			for (int i = 0; i < list.Length; i++)
				list[i] = (TabInfo)listView.SelectedItems[i];
			return list;
		}

		void ActivateWindow(TabInfo info)
		{
			LastActivatedTabState = info.TabState;
			MainWindow.Instance.SetActiveTab(info.TabState);
		}

		void CloseWindows(IList<TabInfo> tabs)
		{
			int newIndex = listView.SelectedIndex;
			foreach (var info in tabs) {
				MainWindow.Instance.CloseTab(info.TabState);
				int removedIndex = allTabs.IndexOf(info);
				allTabs.RemoveAt(removedIndex);
				if (newIndex == removedIndex) {
					if (newIndex >= allTabs.Count)
						newIndex--;
				}
				else if (newIndex > removedIndex)
					newIndex--;
			}
			if (newIndex < 0 || newIndex >= allTabs.Count)
				newIndex = 0;
			if (allTabs.Count == 0)
				listView.SelectedIndex = -1;
			else {
				listView.SelectedIndex = newIndex;
				Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => {
					if (listView.SelectedIndex != newIndex)
						return;
					var item = listView.ItemContainerGenerator.ContainerFromIndex(newIndex) as ListViewItem;
					if (item != null) {
						item.Focus();
						listView.ScrollIntoView(item);
					}
				}));
			}
		}

		private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!UIUtils.IsLeftDoubleClick<ListViewItem>(listView, e))
				return;
			var tabs = GetSelectedItems();
			if (tabs.Length > 0) {
				ActivateWindow(tabs[0]);
				this.DialogResult = true;
				e.Handled = true;
				return;
			}
		}

		private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateButtonState();
		}

		void UpdateButtonState()
		{
			closeWindowsButton.Content = listView.SelectedItems.Count <= 1 ? "_Close Window" : "_Close Windows";
			activateButton.IsEnabled = listView.SelectedItems.Count == 1;
			closeWindowsButton.IsEnabled = listView.SelectedItems.Count != 0;
			var tabs = GetSelectedItems();
			saveCodeButton.IsEnabled = tabs.Length == 1 && tabs[0].TabState.DecompiledNodes.Length > 0;
		}

		private void activateButton_Click(object sender, RoutedEventArgs e)
		{
			var tabs = GetSelectedItems();
			if (tabs.Length > 0)
				ActivateWindow(tabs[0]);
		}

		private void saveCodeButton_Click(object sender, RoutedEventArgs e)
		{
			var tabs = GetSelectedItems();
			if (tabs.Length > 0)
				MainWindow.Instance.Save(tabs[0].TabState);
		}

		private void closeWindowsButton_Click(object sender, RoutedEventArgs e)
		{
			CloseWindows(GetSelectedItems());
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			var tabs = GetSelectedItems();
			if (tabs.Length > 0)
				ActivateWindow(tabs[tabs.Length - 1]);
			this.DialogResult = true;
			e.Handled = true;
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Escape) {
				this.DialogResult = false;
				e.Handled = true;
				return;
			}
		}

		private void listView_KeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Return) {
				okButton_Click(sender, e);
				return;
			}
		}
	}
}
