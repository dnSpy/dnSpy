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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace dnSpy.Contracts.Language.Intellisense {
	/// <summary>
	/// A read-only filtered <see cref="ObservableCollection{T}"/>. Its content changes when the list gets filtered.
	/// </summary>
	sealed class FilteredCompletionCollection : IFilteredCompletionCollection
		// NOTE: IList is required for WPF ItemsControl since it ignores IList<T> ifaces
		, IList {

		/// <summary>
		/// Raised when the collection has changed
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary>
		/// Gets the item at the specified index
		/// </summary>
		/// <param name="index">Index of item</param>
		/// <returns></returns>
		public Completion this[int index] {
			get { return items[index]; }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Gets the number of items in the collection
		/// </summary>
		public int Count => items.Count;

		readonly List<Completion> items;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="items">Items</param>
		internal FilteredCompletionCollection(IEnumerable<Completion> items) {
			this.items = items.ToList();
		}

		/// <summary>
		/// Should be called when the list has been filtered
		/// </summary>
		/// <param name="newItems">New items</param>
		internal void SetNewFilteredCollection(IEnumerable<Completion> newItems) {
			items.Clear();
			items.AddRange(newItems);
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <summary>
		/// Checks whether <paramref name="item"/> is present in the collection
		/// </summary>
		/// <param name="item">Item</param>
		/// <returns></returns>
		public bool Contains(Completion item) => items.Contains(item);

		/// <summary>
		/// Returns the index of <paramref name="item"/> in the collection or -1 if it's not present
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int IndexOf(Completion item) => items.IndexOf(item);

		/// <summary>
		/// Copies the collection to <paramref name="array"/>
		/// </summary>
		/// <param name="array">Destination array</param>
		/// <param name="arrayIndex">Index</param>
		public void CopyTo(Completion[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

		bool IList.Contains(object value) => ((IList)items).Contains(value);
		int IList.IndexOf(object value) => ((IList)items).IndexOf(value);
		void ICollection.CopyTo(Array array, int index) => ((ICollection)items).CopyTo(array, index);

		/// <summary>
		/// Gets the enumerator
		/// </summary>
		/// <returns></returns>
		public IEnumerator<Completion> GetEnumerator() => items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		bool ICollection<Completion>.IsReadOnly => true;
		bool IList.IsReadOnly => true;
		bool IList.IsFixedSize => false;
		object ICollection.SyncRoot => ((IList)items).SyncRoot;
		bool ICollection.IsSynchronized => false;

		object IList.this[int index] {
			get { return this[index]; }
			set { throw new NotSupportedException(); }
		}

		void ICollection<Completion>.Add(Completion item) { throw new NotSupportedException(); }
		void ICollection<Completion>.Clear() { throw new NotSupportedException(); }
		void IList<Completion>.Insert(int index, Completion item) { throw new NotSupportedException(); }
		bool ICollection<Completion>.Remove(Completion item) { throw new NotSupportedException(); }
		void IList<Completion>.RemoveAt(int index) { throw new NotSupportedException(); }
		int IList.Add(object value) { throw new NotSupportedException(); }
		void IList.Clear() { throw new NotSupportedException(); }
		void IList.Insert(int index, object value) { throw new NotSupportedException(); }
		void IList.Remove(object value) { throw new NotSupportedException(); }
		void IList.RemoveAt(int index) { throw new NotSupportedException(); }
	}
}
