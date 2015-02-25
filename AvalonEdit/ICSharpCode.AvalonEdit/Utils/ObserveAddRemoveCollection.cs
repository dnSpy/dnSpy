// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
		
		/// <summary>
		/// Creates a new ObserveAddRemoveCollection using the specified callbacks.
		/// </summary>
		public ObserveAddRemoveCollection(Action<T> onAdd, Action<T> onRemove)
		{
			if (onAdd == null)
				throw new ArgumentNullException("onAdd");
			if (onRemove == null)
				throw new ArgumentNullException("onRemove");
			this.onAdd = onAdd;
			this.onRemove = onRemove;
		}
		
		/// <inheritdoc/>
		protected override void ClearItems()
		{
			if (onRemove != null) {
				foreach (T val in this)
					onRemove(val);
			}
			base.ClearItems();
		}
		
		/// <inheritdoc/>
		protected override void InsertItem(int index, T item)
		{
			if (onAdd != null)
				onAdd(item);
			base.InsertItem(index, item);
		}
		
		/// <inheritdoc/>
		protected override void RemoveItem(int index)
		{
			if (onRemove != null)
				onRemove(this[index]);
			base.RemoveItem(index);
		}
		
		/// <inheritdoc/>
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
