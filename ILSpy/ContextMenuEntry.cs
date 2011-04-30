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

using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	public interface IContextMenuEntry
	{
		bool IsVisible(SharpTreeNode[] selectedNodes);
		bool IsEnabled(SharpTreeNode[] selectedNodes);
		void Execute(SharpTreeNode[] selectedNodes);
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
		public static void Add(SharpTreeView treeView)
		{
			var provider = new ContextMenuProvider(treeView);
			treeView.ContextMenuOpening += provider.treeView_ContextMenuOpening;
			// Context menu is shown only when the ContextMenu property is not null before the
			// ContextMenuOpening event handler is called.
			treeView.ContextMenu = new ContextMenu();
		}
		
		readonly SharpTreeView treeView;
		
		[ImportMany(typeof(IContextMenuEntry))]
		Lazy<IContextMenuEntry, IContextMenuEntryMetadata>[] entries = null;
		
		private ContextMenuProvider(SharpTreeView treeView)
		{
			this.treeView = treeView;
			App.CompositionContainer.ComposeParts(this);
		}
		
		void treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			SharpTreeNode[] selectedNodes = treeView.GetTopLevelSelection().ToArray();
			if (selectedNodes.Length == 0) {
				e.Handled = true; // don't show the menu
				return;
			}
			ContextMenu menu = new ContextMenu();
			foreach (var category in entries.OrderBy(c => c.Metadata.Order).GroupBy(c => c.Metadata.Category)) {
				if (menu.Items.Count > 0) {
					menu.Items.Add(new Separator());
				}
				foreach (var entryPair in category) {
					IContextMenuEntry entry = entryPair.Value;
					if (entry.IsVisible(selectedNodes)) {
						MenuItem menuItem = new MenuItem();
						menuItem.Header = entryPair.Metadata.Header;
						if (!string.IsNullOrEmpty(entryPair.Metadata.Icon)) {
							menuItem.Icon = new Image {
								Width = 16,
								Height = 16,
								Source = Images.LoadImage(entry, entryPair.Metadata.Icon)
							};
						}
						if (entryPair.Value.IsEnabled(selectedNodes)) {
							menuItem.Click += delegate
							{
								entry.Execute(selectedNodes);
							};
						} else
							menuItem.IsEnabled = false;
						menu.Items.Add(menuItem);
					}
				}
			}
			if (menu.Items.Count > 0)
				treeView.ContextMenu = menu;
			else
				// hide the context menu.
				e.Handled = true;
		}
	}
}
