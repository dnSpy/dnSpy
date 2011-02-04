// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.ObjectModel;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// A collection where adding and removing items causes a callback.
	/// It is valid for the onAdd callback to throw an exception - this will prevent the new item from
	/// being added to the collection.
	/// </summary>
	sealed class ObserveAddRemoveCollection<T> : Collection<T>
	{
		readonly Action<T> onAdd, onRemove;
		
		public ObserveAddRemoveCollection(Action<T> onAdd, Action<T> onRemove)
		{
			this.onAdd = onAdd;
			this.onRemove = onRemove;
		}
		
		protected override void ClearItems()
		{
			if (onRemove != null) {
				foreach (T val in this)
					onRemove(val);
			}
			base.ClearItems();
		}
		
		protected override void InsertItem(int index, T item)
		{
			if (onAdd != null)
				onAdd(item);
			base.InsertItem(index, item);
		}
		
		protected override void RemoveItem(int index)
		{
			if (onRemove != null)
				onRemove(this[index]);
			base.RemoveItem(index);
		}
		
		protected override void SetItem(int index, T item)
		{
			if (onRemove != null)
				onRemove(this[index]);
			try {
				if (onAdd != null)
					onAdd(item);
			} catch {
				// When adding the new item fails, just remove the old one
				// (we cannot keep the old item since we already successfully called onRemove for it)
				base.RemoveAt(index);
				throw;
			}
			base.SetItem(index, item);
		}
	}
}
