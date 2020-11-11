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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Input;
using dnSpy.Contracts.MVVM;

namespace dnSpy.AsmEditor.Commands {
	class IndexObservableCollection<T> : ObservableCollection<T> where T : class, IIndexedItem {
		public ICommand AddItemBeforeCommand => new RelayCommand(a => AddItemBefore((T[])a!), a => AddItemBeforeCanExecute((T[])a!));
		public ICommand AddItemAfterCommand => new RelayCommand(a => AddItemAfter((T[])a!), a => AddItemAfterCanExecute((T[])a!));
		public ICommand AppendItemCommand => new RelayCommand(a => AppendItem((T[])a!), a => AppendItemCanExecute((T[])a!));
		public ICommand ItemMoveUpCommand => new RelayCommand(a => ItemMoveUp((T[])a!), a => ItemMoveUpCanExecute((T[])a!));
		public ICommand ItemMoveDownCommand => new RelayCommand(a => ItemMoveDown((T[])a!), a => ItemMoveDownCanExecute((T[])a!));
		public ICommand RemoveItemCommand => new RelayCommand(a => RemoveItem((T[])a!), a => RemoveItemCanExecute((T[])a!));
		public ICommand RemoveAllItemsCommand => new RelayCommand(a => RemoveAllItems((T[])a!), a => RemoveAllItemsCanExecute((T[])a!));
		public bool DisableAutoUpdateProps { get; set; }
		public Action<int>? UpdateIndexesDelegate { get; set; }
		public bool CanCreateNewItems => createNewItem is not null;
		public bool CanRemoveItems => true;
		public bool CanMoveItems => true;

		readonly Func<T>? createNewItem;

		public IndexObservableCollection()
			: this(null) {
		}

		public IndexObservableCollection(Func<T>? createNewItem) => this.createNewItem = createNewItem;

		protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
			if (!DisableAutoUpdateProps) {
				switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					UpdateIndexes(e.NewStartingIndex);
					break;

				case NotifyCollectionChangedAction.Remove:
				case NotifyCollectionChangedAction.Replace:
					UpdateIndexes(e.OldStartingIndex);
					break;

				case NotifyCollectionChangedAction.Move:
					UpdateIndexes(Math.Min(e.NewStartingIndex, e.OldStartingIndex));
					break;

				case NotifyCollectionChangedAction.Reset:
					UpdateIndexes(0);
					break;

				default:
					throw new InvalidOperationException();
				}
			}

			base.OnCollectionChanged(e);
		}

		protected override void ClearItems() {
			// Must do this before calling base method since some listeners
			// validate that they don't reference removed items.
			foreach (var item in Items)
				item.Index = -1;
			base.ClearItems();
		}

		protected override void RemoveItem(int index) {
			// Must do this before calling base method since some listeners
			// validate that they don't reference removed items.
			Items[index].Index = -1;
			base.RemoveItem(index);
		}

		public void UpdateIndexes(int index) {
			if (UpdateIndexesDelegate is not null)
				UpdateIndexesDelegate(index);
			else
				DefaultUpdateIndexes(index);
		}

		public void DefaultUpdateIndexes(int index) {
			for (; index < Count; index++)
				this[index].Index = index;
		}

		void AddNewItem(T[] items, int indexDisp) {
			if (items.Length == 0)
				return;
			int index = IndexOf(items[0]);
			Debug.Assert(index >= 0);
			if (index < 0)
				throw new InvalidOperationException();
			AddNewItem(index + indexDisp);
		}

		void AddNewItem(int index) {
			if (createNewItem is null)
				throw new InvalidOperationException();
			Insert(index, createNewItem());
		}

		void AddItemBefore(T[] items) => AddNewItem(items, 0);
		bool AddItemBeforeCanExecute(T[] items) => CanCreateNewItems && items.Length == 1;
		void AddItemAfter(T[] items) => AddNewItem(items, 1);
		bool AddItemAfterCanExecute(T[] items) => CanCreateNewItems && items.Length == 1;
		void AppendItem(T[] items) => AddNewItem(Count);
		bool AppendItemCanExecute(T[] items) => CanCreateNewItems;

		void ItemMoveUp(T[] items) {
			if (items.Length == 0)
				return;

			Array.Sort(items, (a, b) => a.Index.CompareTo(b.Index));

			var old = DisableAutoUpdateProps;
			try {
				DisableAutoUpdateProps = true;

				int index = items[0].Index - 1;
				if (index < 0)
					index = 0;

				foreach (var instr in items) {
					if (index != instr.Index)
						Move(instr.Index, index);
					index++;
				}
			}
			finally {
				DisableAutoUpdateProps = old;
			}
			UpdateIndexes(0);
		}

		bool ItemMoveUpCanExecute(T[] items) => CanMoveItems && items.Length > 0;

		void ItemMoveDown(T[] items) {
			if (items.Length == 0)
				return;

			Array.Sort(items, (a, b) => a.Index.CompareTo(b.Index));

			var old = DisableAutoUpdateProps;
			try {
				DisableAutoUpdateProps = true;

				int index = items[items.Length - 1].Index + 1;
				if (index >= Count)
					index = Count - 1;

				for (int i = 0; i < items.Length; i++) {
					var item = items[i];
					int currIndex = item.Index - i;
					Debug.Assert(currIndex >= 0 && this[currIndex] == item);
					if (index != currIndex)
						Move(currIndex, index);
				}
			}
			finally {
				DisableAutoUpdateProps = old;
			}
			UpdateIndexes(0);
		}

		bool ItemMoveDownCanExecute(T[] items) => CanMoveItems && items.Length > 0;

		void RemoveItem(T[] items) {
			Array.Sort(items, (a, b) => b.Index.CompareTo(a.Index));

			var old = DisableAutoUpdateProps;
			try {
				DisableAutoUpdateProps = true;

				foreach (var item in items) {
					bool b = item.Index < Count && item == this[item.Index];
					Debug.Assert(b);
					if (!b)
						throw new InvalidOperationException();
					RemoveAt(item.Index);
				}
			}
			finally {
				DisableAutoUpdateProps = old;
			}
			UpdateIndexes(0);
		}

		bool RemoveItemCanExecute(T[] items) => CanRemoveItems && items.Length > 0;
		void RemoveAllItems(T[] items) => Clear();
		bool RemoveAllItemsCanExecute(T[] items) => CanRemoveItems && Count > 0;
	}
}
