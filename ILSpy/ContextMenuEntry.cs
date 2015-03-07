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
using System.Windows;
using System.Windows.Controls;

using ICSharpCode.AvalonEdit;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	public interface IContextMenuEntry
	{
		bool IsVisible(TextViewContext context);
		bool IsEnabled(TextViewContext context);
		void Execute(TextViewContext context);
	}
	
	public class TextViewContext
	{
		/// <summary>
		/// Returns the selected nodes in the tree view.
		/// Returns null, if context menu does not belong to a tree view.
		/// </summary>
		public SharpTreeNode[] SelectedTreeNodes { get; private set; }
		
		/// <summary>
		/// Returns the tree view the context menu is assigned to.
		/// Returns null, if context menu is not assigned to a tree view.
		/// </summary>
		public SharpTreeView TreeView { get; private set; }
		
		/// <summary>
		/// Returns the text view the context menu is assigned to.
		/// Returns null, if context menu is not assigned to a text view.
		/// </summary>
		public DecompilerTextView TextView { get; private set; }
		
		/// <summary>
		/// Returns the list box the context menu is assigned to.
		/// Returns null, if context menu is not assigned to a list box.
		/// </summary>
		public ListBox ListBox { get; private set; }
		
		/// <summary>
		/// Returns the reference the mouse cursor is currently hovering above.
		/// Returns null, if there was no reference found.
		/// </summary>
		public ReferenceSegment Reference { get; private set; }
		
		/// <summary>
		/// Returns the position in TextView the mouse cursor is currently hovering above.
		/// Returns null, if TextView returns null;
		/// </summary>
		public TextViewPosition? Position { get; private set; }
		
		public static TextViewContext Create(SharpTreeView treeView = null, DecompilerTextView textView = null, ListBox listBox = null)
		{
			ReferenceSegment reference;
			if (textView != null)
				reference = textView.GetReferenceSegmentAtMousePosition();
			else if (listBox != null)
				reference = new ReferenceSegment { Reference = ((SearchResult)listBox.SelectedItem).Member };
			else
				reference = null;
			var position = textView != null ? textView.GetPositionFromMousePosition() : null;
			var selectedTreeNodes = treeView != null ? treeView.GetTopLevelSelection().ToArray() : null;
			return new TextViewContext {
				TreeView = treeView,
				SelectedTreeNodes = selectedTreeNodes,
				TextView = textView,
				Reference = reference,
				Position = position
			};
		}
	}
	
	public interface IContextMenuEntryMetadata
	{
		string Icon { get; }
		string Header { get; }
		string Category { get; }
		
		double Order { get; }
	}
	
	[MetadataAttribute]
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
	public class ExportContextMenuEntryAttribute : ExportAttribute, IContextMenuEntryMetadata
	{
		public ExportContextMenuEntryAttribute()
			: base(typeof(IContextMenuEntry))
		{
		}
		
		public string Icon { get; set; }
		public string Header { get; set; }
		public string Category { get; set; }
		public double Order { get; set; }
	}
	
	internal class ContextMenuProvider
	{
		/// <summary>
		/// Enables extensible context menu support for the specified tree view.
		/// </summary>
		public static void Add(SharpTreeView treeView, DecompilerTextView textView = null)
		{
			var provider = new ContextMenuProvider(treeView, textView);
			treeView.ContextMenuOpening += provider.treeView_ContextMenuOpening;
			// Context menu is shown only when the ContextMenu property is not null before the
			// ContextMenuOpening event handler is called.
			treeView.ContextMenu = new ContextMenu();
			if (textView != null) {
				textView.ContextMenuOpening += provider.textView_ContextMenuOpening;
				// Context menu is shown only when the ContextMenu property is not null before the
				// ContextMenuOpening event handler is called.
				textView.ContextMenu = new ContextMenu();
			}
		}
		
		public static void Add(ListBox listBox)
		{
			var provider = new ContextMenuProvider(listBox);
			listBox.ContextMenuOpening += provider.listBox_ContextMenuOpening;
			listBox.ContextMenu = new ContextMenu();
		}
		
		readonly SharpTreeView treeView;
		readonly DecompilerTextView textView;
		readonly ListBox listBox;
		
		[ImportMany(typeof(IContextMenuEntry))]
		Lazy<IContextMenuEntry, IContextMenuEntryMetadata>[] entries = null;
		
		ContextMenuProvider(SharpTreeView treeView, DecompilerTextView textView = null)
		{
			this.treeView = treeView;
			this.textView = textView;
			App.CompositionContainer.ComposeParts(this);
		}
		
		ContextMenuProvider(ListBox listBox)
		{
			this.listBox = listBox;
			App.CompositionContainer.ComposeParts(this);
		}
		
		void treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			TextViewContext context = TextViewContext.Create(treeView);
			if (context.SelectedTreeNodes.Length == 0) {
				e.Handled = true; // don't show the menu
				return;
			}
			ContextMenu menu;
			if (ShowContextMenu(context, out menu))
				treeView.ContextMenu = menu;
			else
				// hide the context menu.
				e.Handled = true;
		}
		
		void textView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			TextViewContext context = TextViewContext.Create(textView: textView);
			ContextMenu menu;
			if (ShowContextMenu(context, out menu))
				textView.ContextMenu = menu;
			else
				// hide the context menu.
				e.Handled = true;
		}

		void listBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			TextViewContext context = TextViewContext.Create(listBox: listBox);
			ContextMenu menu;
			if (ShowContextMenu(context, out menu))
				listBox.ContextMenu = menu;
			else
				// hide the context menu.
				e.Handled = true;
		}
		
		bool ShowContextMenu(TextViewContext context, out ContextMenu menu)
		{
			menu = new ContextMenu();
			foreach (var category in entries.OrderBy(c => c.Metadata.Order).GroupBy(c => c.Metadata.Category)) {
				bool needSeparatorForCategory = menu.Items.Count > 0;
				foreach (var entryPair in category) {
					IContextMenuEntry entry = entryPair.Value;
					if (entry.IsVisible(context)) {
						if (needSeparatorForCategory) {
							menu.Items.Add(new Separator());
							needSeparatorForCategory = false;
						}
						MenuItem menuItem = new MenuItem();
						menuItem.Header = entryPair.Metadata.Header;
						if (!string.IsNullOrEmpty(entryPair.Metadata.Icon)) {
							menuItem.Icon = new Image {
								Width = 16,
								Height = 16,
								Source = Images.LoadImage(entry, entryPair.Metadata.Icon)
							};
						}
						if (entryPair.Value.IsEnabled(context)) {
							menuItem.Click += delegate { entry.Execute(context); };
						} else
							menuItem.IsEnabled = false;
						menu.Items.Add(menuItem);
					}
				}
			}
			return menu.Items.Count > 0;
		}
	}
}
