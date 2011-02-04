// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.CodeCompletion
{
	/// <summary>
	/// The list box used inside the CompletionList.
	/// </summary>
	public class CompletionListBox : ListBox
	{
		internal ScrollViewer scrollViewer;
		
		/// <inheritdoc/>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			
			// Find the scroll viewer:
			scrollViewer = null;
			if (this.VisualChildrenCount > 0) {
				Border border = this.GetVisualChild(0) as Border;
				if (border != null)
					scrollViewer = border.Child as ScrollViewer;
			}
		}
		
		/// <summary>
		/// Gets the number of the first visible item.
		/// </summary>
		public int FirstVisibleItem {
			get {
				if (scrollViewer == null || scrollViewer.ExtentHeight == 0) {
					return 0;
				} else {
					return (int)(this.Items.Count * scrollViewer.VerticalOffset / scrollViewer.ExtentHeight);
				}
			}
			set {
				value = value.CoerceValue(0, this.Items.Count - this.VisibleItemCount);
				if (scrollViewer != null) {
					scrollViewer.ScrollToVerticalOffset((double)value / this.Items.Count * scrollViewer.ExtentHeight);
				}
			}
		}
		
		/// <summary>
		/// Gets the number of visible items.
		/// </summary>
		public int VisibleItemCount {
			get {
				if (scrollViewer == null || scrollViewer.ExtentHeight == 0) {
					return 10;
				} else {
					return Math.Max(
						3,
						(int)Math.Ceiling(this.Items.Count * scrollViewer.ViewportHeight
						                  / scrollViewer.ExtentHeight));
				}
			}
		}
		
		/// <summary>
		/// Removes the selection.
		/// </summary>
		public void ClearSelection()
		{
			this.SelectedIndex = -1;
		}
		
		/// <summary>
		/// Selects the item with the specified index and scrolls it into view.
		/// </summary>
		public void SelectIndex(int index)
		{
			if (index >= this.Items.Count)
				index = this.Items.Count - 1;
			if (index < 0)
				index = 0;
			this.SelectedIndex = index;
			this.ScrollIntoView(this.SelectedItem);
		}
		
		/// <summary>
		/// Centers the view on the item with the specified index.
		/// </summary>
		public void CenterViewOn(int index)
		{
			this.FirstVisibleItem = index - VisibleItemCount / 2;
		}
	}
}
