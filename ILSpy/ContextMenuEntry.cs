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
using System.Windows.Media;

using ICSharpCode.AvalonEdit;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.TreeView;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy
{
	public interface IContextMenuEntry<TContext>
	{
		/// <summary>
		/// Returns true if it should be visible in the context menu
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		bool IsVisible(TContext context);

		/// <summary>
		/// Returns true if it's enabled in the context menu. Only called if <see cref="IsVisible"/>
		/// returns true.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		bool IsEnabled(TContext context);

		/// <summary>
		/// Called when the menu item has been clicked
		/// </summary>
		/// <param name="context"></param>
		void Execute(TContext context);
	}

	public interface IContextMenuEntry2<TContext> : IContextMenuEntry<TContext>
	{
		/// <summary>
		/// Called before the context menu is shown. Can be used to update the menu item before it's
		/// shown.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="menuItem"></param>
		void Initialize(TContext context, MenuItem menuItem);
	}
	public interface IContextMenuEntry : IContextMenuEntry<TextViewContext>
	{
	}
	public interface IContextMenuEntry2 : IContextMenuEntry, IContextMenuEntry2<TextViewContext>
	{
	}
	
	public class TextViewContext
	{
		/// <summary>
		/// Returns the selected nodes in the tree view.
		/// Returns null, if context menu does not belong to a tree view.
		/// </summary>
		public ILSpyTreeNode[] SelectedTreeNodes { get; private set; }
		
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
		/// Returns the tab control the context menu is assigned to.
		/// Returns null, if context menu is not assigned to a tab control.
		/// </summary>
		public TabControl TabControl { get; private set; }
		
		/// <summary>
		/// Returns the reference the mouse cursor is currently hovering above.
		/// Returns null, if there was no reference found.
		/// </summary>
		public ReferenceSegment Reference { get; private set; }
		
		/// <summary>
		/// Returns the position in TextView the mouse cursor is currently hovering above or if
		/// the context menu was opened from the keyboard, the current caret location.
		/// Returns null, if TextView returns null;
		/// </summary>
		public TextViewPosition? Position { get; private set; }

		/// <summary>
		/// true if the context menu was opened from the keyboard instead of from the mouse
		/// </summary>
		public bool OpenedFromKeyboard { get; private set; }
		
		public static TextViewContext Create(SharpTreeView treeView = null, DecompilerTextView textView = null, ListBox listBox = null, TabControl tabControl = null, bool openedFromKeyboard = false)
		{
			TextViewPosition? position = null;
			if (textView != null)
				position = openedFromKeyboard ? textView.TextEditor.TextArea.Caret.Position : textView.GetPositionFromMousePosition();
			ReferenceSegment reference;
			if (textView != null)
				reference = textView.GetReferenceSegmentAt(position);
			else if (listBox != null && listBox.SelectedItem != null)
				reference = new ReferenceSegment { Reference = ((SearchResult)listBox.SelectedItem).MDTokenProvider };
			else
				reference = null;
			var selectedTreeNodes = treeView != null ? treeView.GetTopLevelSelection().OfType<ILSpyTreeNode>().ToArray() : null;
			return new TextViewContext {
				TreeView = treeView,
				SelectedTreeNodes = selectedTreeNodes,
				TextView = textView,
				TabControl = tabControl,
				ListBox = listBox,
				Reference = reference,
				Position = position,
				OpenedFromKeyboard = openedFromKeyboard,
			};
		}
	}
	
	public interface IContextMenuEntryMetadata
	{
		string Icon { get; }
		string Header { get; }
		string Category { get; }
		double Order { get; }
		string InputGestureText { get; }
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
		public string InputGestureText { get; set; }
	}
	
	internal class ContextMenuProvider : IDisposable
	{
		/// <summary>
		/// Enables extensible context menu support for the specified tree view.
		/// </summary>
		public static ContextMenuProvider Add(SharpTreeView treeView)
		{
			var provider = new ContextMenuProvider(treeView);
			treeView.ContextMenuOpening += provider.treeView_ContextMenuOpening;
			// Context menu is shown only when the ContextMenu property is not null before the
			// ContextMenuOpening event handler is called.
			treeView.ContextMenu = new ContextMenu();
			return provider;
		}

		/// <summary>
		/// Enables extensible context menu support for the specified text view.
		/// </summary>
		public static ContextMenuProvider Add(DecompilerTextView textView)
		{
			var provider = new ContextMenuProvider(textView);
			textView.ContextMenuOpening += provider.textView_ContextMenuOpening;
			// Context menu is shown only when the ContextMenu property is not null before the
			// ContextMenuOpening event handler is called.
			textView.ContextMenu = new ContextMenu();
			return provider;
		}

		public static ContextMenuProvider Add(ListBox listBox)
		{
			var provider = new ContextMenuProvider(listBox);
			listBox.ContextMenuOpening += provider.listBox_ContextMenuOpening;
			listBox.ContextMenu = new ContextMenu();
			return provider;
		}

		public static ContextMenuProvider Add(TabControl tabControl)
		{
			var provider = new ContextMenuProvider(tabControl);
			tabControl.ContextMenuOpening += provider.tabControl_ContextMenuOpening;
			tabControl.ContextMenu = new ContextMenu();
			return provider;
		}

		public void Dispose()
		{
			if (treeView != null)
				treeView.ContextMenuOpening -= this.treeView_ContextMenuOpening;
			if (textView != null)
				textView.ContextMenuOpening -= this.textView_ContextMenuOpening;
			if (listBox != null)
				listBox.ContextMenuOpening -= listBox_ContextMenuOpening;
		}
		
		readonly SharpTreeView treeView;
		readonly DecompilerTextView textView;
		readonly ListBox listBox;
		readonly TabControl tabControl;

		// Prevent big memory leaks (text editor) because the data is put into some MEF data structure.
		// All created instances in this class are shared so this one can be shared as well.
		class MefState
		{
			public static readonly MefState Instance = new MefState();

			MefState()
			{
				App.CompositionContainer.ComposeParts(this);
			}

			[ImportMany(typeof(IContextMenuEntry))]
			public Lazy<IContextMenuEntry, IContextMenuEntryMetadata>[] entries = null;
		}
		
		ContextMenuProvider(SharpTreeView treeView)
		{
			this.treeView = treeView;
			this.textView = null;
		}

		ContextMenuProvider(DecompilerTextView textView)
		{
			this.treeView = null;
			this.textView = textView;
		}
		
		ContextMenuProvider(ListBox listBox)
		{
			this.listBox = listBox;
		}

		ContextMenuProvider(TabControl tabControl)
		{
			this.tabControl = tabControl;
		}
		
		void treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			TextViewContext context = TextViewContext.Create(treeView);
			ContextMenu menu;
			if (ShowContextMenu(context, out menu))
				treeView.ContextMenu = menu;
			else
				// hide the context menu.
				e.Handled = true;
		}
		
		void textView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			bool openedFromKeyboard = e.CursorLeft == -1 && e.CursorTop == -1;
			TextViewContext context = TextViewContext.Create(textView: textView, openedFromKeyboard: openedFromKeyboard);
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
		
		void tabControl_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			TextViewContext context = TextViewContext.Create(tabControl: tabControl);
			ContextMenu menu;
			if (ShowContextMenu(context, out menu))
				tabControl.ContextMenu = menu;
			else
				// hide the context menu.
				e.Handled = true;
		}

		bool ShowContextMenu(TextViewContext context, out ContextMenu menu)
		{
			menu = new ContextMenu();
			foreach (var category in MefState.Instance.entries.OrderBy(c => c.Metadata.Order).GroupBy(c => c.Metadata.Category)) {
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
						menuItem.InputGestureText = entryPair.Metadata.InputGestureText ?? string.Empty;
						var entry2 = entry as IContextMenuEntry2;
						if (entry2 != null)
							entry2.Initialize(context, menuItem);
						menu.Items.Add(menuItem);
					}
				}
			}
			menu.Background = Brushes.White;
			return menu.Items.Count > 0;
		}
	}
}
