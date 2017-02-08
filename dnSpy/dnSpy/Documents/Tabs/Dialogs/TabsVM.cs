/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;

namespace dnSpy.Documents.Tabs.Dialogs {
	sealed class TabsVM : ViewModelBase {
		public ICommand SaveCommand => new RelayCommand(a => Save(), a => CanSave);
		public ICommand CloseTabCommand => new RelayCommand(a => CloseTab(), a => CanCloseTab);
		public ObservableCollection<TabVM> Collection => tabsList;
		public ITabsVMSettings Settings { get; }

		readonly IDocumentTabService documentTabService;
		readonly ObservableCollection<TabVM> tabsList;
		readonly ISaveService saveService;

		public TabVM[] SelectedItems {
			get { return selectedItems; }
			set {
				selectedItems = value ?? Array.Empty<TabVM>();
				InitializeSaveText();
			}
		}
		TabVM[] selectedItems = Array.Empty<TabVM>();

		public object SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem != value) {
					selectedItem = value;
					OnPropertyChanged(nameof(SelectedItem));
					InitializeSaveText();
				}
			}
		}
		object selectedItem;

		public string SaveText {
			get { return saveText; }
			set {
				if (saveText != value) {
					saveText = value;
					OnPropertyChanged(nameof(SaveText));
				}
			}
		}
		string saveText;

		public IClassificationFormatMap ClassificationFormatMap { get; }
		public ITextElementProvider TextElementProvider { get; }

		public TabsVM(IDocumentTabService documentTabService, ISaveService saveService, ITabsVMSettings tabsVMSettings, IClassificationFormatMap classificationFormatMap, ITextElementProvider textElementProvider) {
			this.documentTabService = documentTabService;
			this.saveService = saveService;
			ClassificationFormatMap = classificationFormatMap;
			TextElementProvider = textElementProvider;
			Settings = tabsVMSettings;
			tabsList = new ObservableCollection<TabVM>(documentTabService.SortedTabs.Select(a => new TabVM(this, a)));
			SelectedItem = tabsList.Count == 0 ? null : tabsList[0];
			InitializeSaveText();
		}

		void InitializeSaveText() {
			var vm = selectedItems.Length == 0 ? null : selectedItems[0];
			SaveText = saveService.GetMenuHeader(vm?.Tab);
		}

		bool CanSave => SelectedItems.Length == 1 && saveService.CanSave(SelectedItems[0].Tab);

		void Save() {
			if (!CanSave)
				return;
			saveService.Save(SelectedItems[0].Tab);
		}

		bool CanCloseTab => SelectedItems.Length > 0;

		void CloseTab() {
			var oldSelItem = SelectedItem;
			bool resetSelItem = false;
			foreach (var vm in SelectedItems.ToArray()) {
				resetSelItem |= oldSelItem == vm;
				if (lastActivated == vm)
					lastActivated = null;
				documentTabService.Close(vm.Tab);
				Collection.Remove(vm);
			}
			if (resetSelItem)
				SelectedItem = tabsList.Count == 0 ? null : tabsList[0];
		}

		public void Activate(TabVM vm) {
			if (vm == null)
				return;
			LastActivated = vm;
			documentTabService.SetFocus(vm.Tab);
		}

		public TabVM LastActivated {
			get { return lastActivated; }
			set { lastActivated = value; }
		}
		TabVM lastActivated;
	}
}
