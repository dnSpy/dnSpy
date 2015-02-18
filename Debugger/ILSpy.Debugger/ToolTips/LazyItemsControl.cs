// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Windows.Controls;

using ICSharpCode.ILSpy.Debugger.Services;

namespace ICSharpCode.ILSpy.Debugger.Tooltips
{
	/// <summary>
	/// ItemsControl wrapper that takes VirtualizingIEnumerable as source,
	/// and adds additional items from the source to underlying ItemsControl when scrolled to bottom.
	/// </summary>
	internal class LazyItemsControl<T>
	{
		private ItemsControl itemsControl;
		private int initialItemsCount;

		/// <summary>
		/// Creates new instance of LazyItemsControl.
		/// </summary>
		/// <param name="wrappedItemsControl">ItemsControl to wrap and add items to it when scrolled to bottom.</param>
		/// <param name="initialItemsCount">Number of items to be initially displayed in wrapped ItemsControl.</param>
		public LazyItemsControl(ItemsControl wrappedItemsControl, int initialItemsCount)
		{
			if (wrappedItemsControl == null)
				throw new ArgumentNullException("wrappedItemsControl");

			this.initialItemsCount = initialItemsCount;
			this.itemsControl = wrappedItemsControl;
			this.itemsControl.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(handleScroll));
		}

		private ScrollViewer scrollViewerCached;
		public ScrollViewer ScrollViewer
		{
			get
			{
				if (this.scrollViewerCached == null)
					this.scrollViewerCached = this.itemsControl.GetScrollViewer();
				return this.scrollViewerCached;
			}
		}

		public bool IsScrolledToStart
		{
			get
			{
				if (ScrollViewer == null)		// Visual tree not initialized yet
					return false;
				return ScrollViewer.VerticalOffset == 0;
			}
		}

		public bool IsScrolledToEnd
		{
			get
			{
				if (itemsSourceTotalCount == null) {
					// not scrolled to end of IEnumerable yet
					return false;
				}
				// already scrolled to end of IEnumerable
				int totalItems = itemsSourceTotalCount.Value;
				return (ScrollViewer.VerticalOffset >= totalItems - ScrollViewer.ViewportHeight);
			}
		}

		private int? itemsSourceTotalCount = null;
		/// <summary> Items count of underlying IEnumerable. Null until scrolled to the end of IEnumerable. </summary>
		public int? ItemsSourceTotalCount
		{
			get
			{
				return this.itemsSourceTotalCount;
			}
		}

		private VirtualizingIEnumerable<T> itemsSource;
		/// <summary> The collection that underlying ItemsControl sees. </summary>
		public VirtualizingIEnumerable<T> ItemsSource
		{
			get { return itemsSource; }
			set
			{
				this.itemsSource = value;
				addNextItems(this.itemsSource, initialItemsCount);
				this.itemsControl.ItemsSource = value;
			}
		}

		private void addNextItems(VirtualizingIEnumerable<T> sourceToAdd, int nItems)
		{
			sourceToAdd.AddNextItems(nItems);
			if (!sourceToAdd.HasNext) {
				// all items from IEnumerable have been added
				this.itemsSourceTotalCount = sourceToAdd.Count;
			}
		}

		private void handleScroll(object sender, ScrollChangedEventArgs e)
		{
			if (e.VerticalChange > 0) {
				// scrolled to bottom
				if (e.VerticalOffset >= this.itemsSource.Count - e.ViewportHeight) {
					addNextItems(this.itemsSource, (int)e.VerticalChange);
				}
			}
		}
	}
}
