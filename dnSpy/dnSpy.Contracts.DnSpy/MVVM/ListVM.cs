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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace dnSpy.Contracts.MVVM {
	/// <summary>
	/// List of items
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ListVM<T> : INotifyPropertyChanged, IDataErrorInfo {
		/// <summary>The list</summary>
		protected ObservableCollection<T> list;
		readonly Action<int, int>? onChanged;
		int index;

		/// <summary>
		/// Gets the index
		/// </summary>
		protected int Index => index;

		/// <summary>
		/// Gets the items
		/// </summary>
		public IList<T> Items => list;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="newValues"></param>
		/// <param name="addDefault"></param>
		/// <param name="defaultValue"></param>
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
				OnPropertyChanged(nameof(SelectedIndex));
				OnPropertyChanged(nameof(SelectedItem));
				OnPropertyChanged(nameof(Items));
			}
			finally {
				index = newIndex;
				OnPropertyChanged(nameof(SelectedIndex));
				OnPropertyChanged(nameof(SelectedItem));
			}
		}

		/// <summary>
		/// Gets/sets the selected index
		/// </summary>
		public int SelectedIndex {
			get => index;
			set {
				if (index != value) {
					int oldIndex = index;
					Debug.Assert(value >= 0 && value < list.Count);
					index = value;
					OnPropertyChanged(nameof(SelectedIndex));
					OnPropertyChanged(nameof(SelectedItem));
					onChanged?.Invoke(oldIndex, index);
				}
			}
		}

		/// <summary>
		/// Gets/sets the selected item
		/// </summary>
		public T SelectedItem {
			get {
				if (index < 0 || index >= list.Count)
					return default!;
				return list[index];
			}
			set {
				if (index < 0 || !object.Equals(value, SelectedItem))
					SelectedIndex = GetIndex(value);
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public ListVM()
			: this((Action<int, int>?)null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onChanged">Called when the selected item gets changed</param>
		public ListVM(Action<int, int>? onChanged) {
			list = new ObservableCollection<T>();
			index = -1;
			this.onChanged = onChanged;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="list">Initial value</param>
		public ListVM(IList<T> list)
			: this(list, null) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="list">Initial value</param>
		/// <param name="onChanged">Called when the selected item gets changed</param>
		public ListVM(IEnumerable<T> list, Action<int, int>? onChanged) {
			this.list = new ObservableCollection<T>(list);
			index = this.list.Count == 0 ? -1 : 0;
			this.onChanged = onChanged;
		}

		int GetIndex(T value) {
			int index = list.IndexOf(value);
			if (index >= 0)
				return index;

			list.Add(value);
			return list.Count - 1;
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		string IDataErrorInfo.Error { get { throw new NotImplementedException(); } }

		string IDataErrorInfo.this[string columnName] {
			get {
				if (columnName == nameof(SelectedIndex)) {
					if (!(DataErrorInfoDelegate is null))
						return DataErrorInfoDelegate(this);
				}
				return string.Empty;
			}
		}

		/// <summary>
		/// Can be set to validate the list
		/// </summary>
		public Func<ListVM<T>, string>? DataErrorInfoDelegate;
	}
}
