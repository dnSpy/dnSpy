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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace dnSpy.Shared.UI.MVVM {
	public class ListVM<T> : INotifyPropertyChanged, IDataErrorInfo {
		protected ObservableCollection<T> list;
		readonly Action<int, int> onChanged;
		int index;

		protected int Index {
			get { return index; }
		}

		public IList<T> Items {
			get { return list; }
		}

		public void InvalidateSelected(IEnumerable<T> newValues, bool addDefault, T defaultValue) {
			//TODO: Optimize callers. This method is slow.
			var newList = new ObservableCollection<T>();
			if (addDefault)
				newList.Add(defaultValue);
			foreach (var v in newValues)
				newList.Add(v);
			T selectedItem = SelectedItem;
			if (index < 0)
				selectedItem = defaultValue;
			int newIndex = index >= 0 && index < newList.Count &&
						object.Equals(newList[index], selectedItem) ?
						index : newList.IndexOf(selectedItem);
			if (newIndex < 0) {
				newList.Add(selectedItem);
				newIndex = newList.Count - 1;
			}
			try {
				list = newList;
				index = -1;
				OnPropertyChanged("SelectedIndex");
				OnPropertyChanged("SelectedItem");
				OnPropertyChanged("Items");
			}
			finally {
				index = newIndex;
				OnPropertyChanged("SelectedIndex");
				OnPropertyChanged("SelectedItem");
			}
		}

		public int SelectedIndex {
			get { return index; }
			set {
				if (index != value) {
					int oldIndex = index;
					Debug.Assert(value >= 0 && value < list.Count);
					index = value;
					OnPropertyChanged("SelectedIndex");
					OnPropertyChanged("SelectedItem");
					if (onChanged != null)
						onChanged(oldIndex, index);
				}
			}
		}

		public T SelectedItem {
			get {
				if (index < 0 || index >= list.Count)
					return default(T);
				return list[index];
			}
			set {
				if (index < 0 || !object.Equals(value, SelectedItem))
					SelectedIndex = GetIndex(value);
			}
		}

		public ListVM()
			: this((Action<int, int>)null) {
		}

		public ListVM(Action<int, int> onChanged) {
			this.list = new ObservableCollection<T>();
			this.index = -1;
			this.onChanged = onChanged;
		}

		public ListVM(IList<T> list)
			: this(list, null) {
		}

		public ListVM(IEnumerable<T> list, Action<int, int> onChanged) {
			this.list = new ObservableCollection<T>(list);
			this.index = this.list.Count == 0 ? -1 : 0;
			this.onChanged = onChanged;
		}

		int GetIndex(T value) {
			int index = list.IndexOf(value);
			if (index >= 0)
				return index;

			list.Add(value);
			return list.Count - 1;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		void OnPropertyChanged(string propName) {
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		public string Error {
			get { throw new NotImplementedException(); }
		}

		public string this[string columnName] {
			get {
				if (columnName == "SelectedIndex") {
					if (DataErrorInfoDelegate != null)
						return DataErrorInfoDelegate(this);
				}
				return string.Empty;
			}
		}

		public Func<ListVM<T>, string> DataErrorInfoDelegate;
	}
}
