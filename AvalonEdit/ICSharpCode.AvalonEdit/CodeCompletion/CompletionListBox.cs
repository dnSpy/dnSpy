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
