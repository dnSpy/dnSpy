// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.ILSpy.Bookmarks;

namespace ICSharpCode.ILSpy.AvalonEdit
{
	#region Context menu extensibility
	public interface IBookmarkContextMenuEntry
	{
		bool IsVisible(IBookmark[] bookmarks);
		bool IsEnabled(IBookmark[] bookmarks);
		void Execute(IBookmark[] bookmarks);
	}
	
	public interface IBookmarkContextMenuEntryMetadata
	{
		string Icon { get; }
		string Header { get; }
		string Category { get; }
		
		double Order { get; }
	}
	
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
	public class ExportBookmarkContextMenuEntryAttribute : ExportAttribute, IBookmarkContextMenuEntryMetadata
	{
		public ExportBookmarkContextMenuEntryAttribute()
			: base(typeof(IBookmarkContextMenuEntry))
		{
		}
		
		public string Icon { get; set; }
		public string Header { get; set; }
		public string Category { get; set; }
		public double Order { get; set; }
	}
	#endregion
	
	#region Actions (simple clicks) - this will be used for creating bookmarks (e.g. Breakpoint bookmarks)
	
	public interface IBookmarkActionEntry
	{
		bool IsEnabled();
		void Execute(int line);
	}
	
	public interface IBookmarkActionMetadata
	{
		string Category { get; }
		
		double Order { get; }
	}
	
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
	public class ExportBookmarkActionEntryAttribute : ExportAttribute, IBookmarkActionMetadata
	{
		public ExportBookmarkActionEntryAttribute()
			: base(typeof(IBookmarkActionEntry))
		{
		}
		
		public string Icon { get; set; }
		public string Header { get; set; }
		public string Category { get; set; }
		public double Order { get; set; }
	}
	
	#endregion
	
	internal class IconMarginActionsProvider
	{
		/// <summary>
		/// Enables extensible context menu support for the specified icon margin.
		/// </summary>
		public static void Add(IconBarMargin margin)
		{
			var provider = new IconMarginActionsProvider(margin);
			margin.MouseUp += provider.HandleMouseEvent;
			margin.ContextMenu = new ContextMenu();
		}
		
		readonly IconBarMargin margin;
		
		[ImportMany(typeof(IBookmarkContextMenuEntry))]
		Lazy<IBookmarkContextMenuEntry, IBookmarkContextMenuEntryMetadata>[] contextEntries = null;
		
		[ImportMany(typeof(IBookmarkActionEntry))]
		Lazy<IBookmarkActionEntry, IBookmarkActionMetadata>[] actionEntries = null;
		
		private IconMarginActionsProvider(IconBarMargin margin)
		{
			this.margin = margin;
			App.CompositionContainer.ComposeParts(this);
		}
		
		void HandleMouseEvent(object sender, MouseButtonEventArgs e)
		{
			int line = margin.GetLineFromMousePosition(e);
			
			if (e.ChangedButton == MouseButton.Left) {
				foreach (var category in actionEntries.OrderBy(c => c.Metadata.Order).GroupBy(c => c.Metadata.Category)) {
					foreach (var entryPair in category) {
						IBookmarkActionEntry entry = entryPair.Value;

						if (entryPair.Value.IsEnabled()) {
							entry.Execute(line);
						} 
					}
				}
			}
			
			// context menu entries
			var bookmarks = margin.Manager.Bookmarks.ToArray();
			if (bookmarks.Length == 0) {
				// don't show the menu
				e.Handled = true;
				this.margin.ContextMenu = null;
				return;
			}
			
			if (e.ChangedButton == MouseButton.Right) {
				// check if we are on a Member				
				var bookmark = bookmarks.FirstOrDefault(b => b.LineNumber == line);
				if (bookmark == null) {
					// don't show the menu
					e.Handled = true;
					this.margin.ContextMenu = null;
					return;
				}
				
				var marks = new[] { bookmark };
				ContextMenu menu = new ContextMenu();
				foreach (var category in contextEntries.OrderBy(c => c.Metadata.Order).GroupBy(c => c.Metadata.Category)) {
					if (menu.Items.Count > 0) {
						menu.Items.Add(new Separator());
					}
					foreach (var entryPair in category) {
						IBookmarkContextMenuEntry entry = entryPair.Value;
						if (entry.IsVisible(marks)) {
							MenuItem menuItem = new MenuItem();
							menuItem.Header = entryPair.Metadata.Header;
							if (!string.IsNullOrEmpty(entryPair.Metadata.Icon)) {
								menuItem.Icon = new Image {
									Width = 16,
									Height = 16,
									Source = Images.LoadImage(entry, entryPair.Metadata.Icon)
								};
							}
							if (entryPair.Value.IsEnabled(marks)) {
								menuItem.Click += delegate { entry.Execute(marks); };
							} else
								menuItem.IsEnabled = false;
							menu.Items.Add(menuItem);
						}
					}
				}
				if (menu.Items.Count > 0)
					margin.ContextMenu = menu;
				else
					// hide the context menu.
					e.Handled = true;
			}
		}
	}
}
