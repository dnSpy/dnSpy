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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.ILSpy.Bookmarks;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy.AvalonEdit
{
	#region Context menu extensibility
	public interface IBookmarkContextMenuEntry : IContextMenuEntry<IBookmark>
	{
	}
	public interface IBookmarkContextMenuEntry2 : IBookmarkContextMenuEntry, IContextMenuEntry2<IBookmark>
	{
	}
	
	public interface IBookmarkContextMenuEntryMetadata
	{
		string Icon { get; }
		string Header { get; }
		string Category { get; }
		string InputGestureText { get; }
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
		public string InputGestureText { get; set; }
		public double Order { get; set; }
	}
	#endregion
	
	#region Actions (simple clicks) - this will be used for creating bookmarks (e.g. Breakpoint bookmarks)
	
	public interface IBookmarkActionEntry
	{
		bool IsEnabled(DecompilerTextView textView);
		void Execute(DecompilerTextView textView, int line);
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
		public static void Add(IconBarMargin margin, DecompilerTextView textView)
		{
			var provider = new IconMarginActionsProvider(margin, textView);
			margin.MouseUp += provider.HandleMouseEvent;
			margin.ContextMenu = new ContextMenu();
		}
		
		readonly IconBarMargin margin;
		readonly DecompilerTextView textView;

		// Prevent big memory leaks (text editor) because the data is put into some MEF data structure.
		// All created instances in this class are shared so this one can be shared as well.
		class MefState
		{
			public static readonly MefState Instance = new MefState();

			MefState()
			{
				App.CompositionContainer.ComposeParts(this);
			}

			[ImportMany(typeof(IBookmarkContextMenuEntry))]
			public Lazy<IBookmarkContextMenuEntry, IBookmarkContextMenuEntryMetadata>[] contextEntries = null;

			[ImportMany(typeof(IBookmarkActionEntry))]
			public Lazy<IBookmarkActionEntry, IBookmarkActionMetadata>[] actionEntries = null;
		}
		
		private IconMarginActionsProvider(IconBarMargin margin, DecompilerTextView textView)
		{
			this.margin = margin;
			this.textView = textView;
		}

		List<IBookmark> GetBookmark(IList<IBookmark> bookmarks, int line)
		{
			var list = new List<IBookmark>();
			foreach (var b in bookmarks) {
				if (BookmarkBase.GetLineNumber(b, textView) != line)
					continue;
				if (!b.HasImage)
					continue;
				list.Add(b);
			}
			list.Sort((a, b) => b.ZOrder.CompareTo(a.ZOrder));
			return list;
		}
		
		void HandleMouseEvent(object sender, MouseButtonEventArgs e)
		{
			int line = margin.GetLineFromMousePosition(e);
			
			if (e.ChangedButton == MouseButton.Left) {
				foreach (var category in MefState.Instance.actionEntries.OrderBy(c => c.Metadata.Order).GroupBy(c => c.Metadata.Category)) {
					foreach (var entryPair in category) {
						IBookmarkActionEntry entry = entryPair.Value;

						if (entryPair.Value.IsEnabled(textView)) {
							entry.Execute(textView, line);
						}
					}
				}
			}
			
			// context menu entries
			var bookmarks = new List<IBookmark>(margin.Manager.Bookmarks);
			bookmarks.AddRange(BookmarkManager.Bookmarks);
			if (bookmarks.Count == 0) {
				// don't show the menu
				e.Handled = true;
				this.margin.ContextMenu = null;
				return;
			}
			
			if (e.ChangedButton == MouseButton.Right) {
				// check if we are on a Member
				var bms = GetBookmark(bookmarks, line);
				if (bms.Count == 0) {
					// don't show the menu
					e.Handled = true;
					this.margin.ContextMenu = null;
					return;
				}

				foreach (var bookmark in bms) {
					ContextMenu menu = new ContextMenu();
					foreach (var category in MefState.Instance.contextEntries.OrderBy(c => c.Metadata.Order).GroupBy(c => c.Metadata.Category)) {
						bool hasAddedSep = menu.Items.Count == 0;
						foreach (var entryPair in category) {
							IBookmarkContextMenuEntry entry = entryPair.Value;
							if (entry.IsVisible(bookmark)) {
								if (!hasAddedSep) {
									menu.Items.Add(new Separator());
									hasAddedSep = true;
								}

								MenuItem menuItem = new MenuItem();
								menuItem.Header = entryPair.Metadata.Header;
								bool isEnabled;
								if (entryPair.Value.IsEnabled(bookmark)) {
									menuItem.Click += delegate { entry.Execute(bookmark); };
									isEnabled = true;
								} else {
									menuItem.IsEnabled = false;
									isEnabled = false;
								}
								if (!string.IsNullOrEmpty(entryPair.Metadata.Icon))
									MainWindow.CreateMenuItemImage(menuItem, entry, entryPair.Metadata.Icon, BackgroundType.ContextMenuItem, isEnabled);
								menuItem.InputGestureText = entryPair.Metadata.InputGestureText ?? string.Empty;
								var entry2 = entry as IBookmarkContextMenuEntry2;
								if (entry2 != null)
									entry2.Initialize(bookmark, menuItem);
								menu.Items.Add(menuItem);
							}
						}
					}
					if (menu.Items.Count > 0) {
						margin.ContextMenu = menu;
						return;
					}
				}
				// hide the context menu.
				e.Handled = true;
			}
		}
	}
}
