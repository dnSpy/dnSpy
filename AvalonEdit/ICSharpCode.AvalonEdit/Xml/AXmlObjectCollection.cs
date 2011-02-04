// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace ICSharpCode.AvalonEdit.Xml
{
	/// <summary>
	/// Collection that is publicly read-only and has support 
	/// for adding/removing multiple items at a time.
	/// </summary>
	public class AXmlObjectCollection<T>: Collection<T>, INotifyCollectionChanged
	{
		/// <summary> Occurs when the collection is changed </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;
		
		/// <summary> Raises <see cref="CollectionChanged"/> event </summary>
		// Do not inherit - it is not called if event is null
		void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (CollectionChanged != null) {
				CollectionChanged(this, e);
			}
		}
		
		/// <inheritdoc/>
		protected override void ClearItems()
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		protected override void InsertItem(int index, T item)
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		protected override void RemoveItem(int index)
		{
			throw new NotSupportedException();
		}
		
		/// <inheritdoc/>
		protected override void SetItem(int index, T item)
		{
			throw new NotSupportedException();
		}
		
		internal void InsertItemAt(int index, T item)
		{
			base.InsertItem(index, item);
			if (CollectionChanged != null)
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new T[] { item }.ToList(), index));
		}
		
		internal void RemoveItemAt(int index)
		{
			T removed = this[index];
			base.RemoveItem(index);
			if (CollectionChanged != null)
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new T[] { removed }.ToList(), index));
		}
		
		internal void InsertItemsAt(int index, IList<T> items)
		{
			for(int i = 0; i < items.Count; i++) {
				base.InsertItem(index + i, items[i]);
			}
			if (CollectionChanged != null)
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)items, index));
		}
		
		internal void RemoveItemsAt(int index, int count)
		{
			List<T> removed = new List<T>();
			for(int i = 0; i < count; i++) {
				removed.Add(this[index]);
				base.RemoveItem(index);
			}
			if (CollectionChanged != null)
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (IList)removed, index));
		}
	}
}
