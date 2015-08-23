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
using System.Windows.Controls.Primitives;
using dnSpy;
using dnSpy.HexEditor;
using dnSpy.Images;
using ICSharpCode.AvalonEdit;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy {
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

	public interface IContextMenuEntry : IContextMenuEntry<ContextMenuEntryContext>
	{
	}

	public interface IContextMenuEntry2 : IContextMenuEntry, IContextMenuEntry2<ContextMenuEntryContext>
	{
	}
	
	public class ContextMenuEntryContext
	{
		public SharpTreeNode[] SelectedTreeNodes { get; private set; }

		public FrameworkElement Element {
			get { return elem; }
			private set { elem = value; }
		}
		FrameworkElement elem;

		public object DataContext {
			get { return elem == null ? null : elem.DataContext; }
		}

		public ReferenceSegment Reference { get; private set; }
		
		public TextViewPosition? Position { get; private set; }

		public bool OpenedFromKeyboard { get; private set; }
		
		public static ContextMenuEntryContext Create(FrameworkElement elem, bool openedFromKeyboard = false)
		{
			TextViewPosition? position = null;
			var textView = elem as DecompilerTextView;
			var listBox = elem as ListBox;
			var treeView = elem as SharpTreeView;

			if (textView != null)
				position = openedFromKeyboard ? textView.TextEditor.TextArea.Caret.Position : textView.GetPositionFromMousePosition();
			ReferenceSegment reference;
			if (textView != null)
				reference = textView.GetReferenceSegmentAt(position);
			else if (listBox != null && listBox.SelectedItem is SearchResult)
				reference = new ReferenceSegment { Reference = ((SearchResult)listBox.SelectedItem).Reference };
			else
				reference = null;
			var selectedTreeNodes = treeView != null ? treeView.GetTopLevelSelection().ToArray() : null;
			return new ContextMenuEntryContext {
				Element = elem,
				SelectedTreeNodes = selectedTreeNodes,
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
		readonly Predicate<DependencyObject> isIgnored;

		ContextMenuProvider(FrameworkElement elem, Predicate<DependencyObject> isIgnored)
		{
			this.elem = elem;
			this.isIgnored = isIgnored;
			this.elem.ContextMenu = new ContextMenu();
		}

		public static ContextMenuProvider Add(FrameworkElement elem, Predicate<DependencyObject> isIgnored = null)
		{
			var provider = new ContextMenuProvider(elem, isIgnored);
			elem.ContextMenuOpening += provider.elem_ContextMenuOpening;
			return provider;
		}

		public static ContextMenuProvider Add(DecompilerTextView textView, Predicate<DependencyObject> isIgnored = null)
		{
			var provider = new ContextMenuProvider(textView, isIgnored);
			textView.ContextMenuOpening += provider.textView_ContextMenuOpening;
			return provider;
		}

		public static ContextMenuProvider Add(HexBox hexBox, Predicate<DependencyObject> isIgnored = null)
		{
			var provider = new ContextMenuProvider(hexBox, isIgnored);
			hexBox.ContextMenuOpening += provider.hexBox_ContextMenuOpening;
			return provider;
		}

		// Make sure there are no more refs to modules so the GC can collect removed modules
		void ClearReferences()
		{
			elem.ContextMenu = new ContextMenu();
		}

		public void Dispose()
		{
			elem.ContextMenuOpening -= this.elem_ContextMenuOpening;
			elem.ContextMenuOpening -= this.textView_ContextMenuOpening;
			elem.ContextMenuOpening -= this.hexBox_ContextMenuOpening;
		}

		readonly FrameworkElement elem;

		// Prevent big memory leaks (text editor) because the data is put into some MEF data structure.
		// All created instances in this class are shared so this one can be shared as well.
		sealed class MefState
		{
			public static readonly MefState Instance = new MefState();

			MefState()
			{
				App.CompositionContainer.ComposeParts(this);
			}

			[ImportMany(typeof(IContextMenuEntry))]
			public Lazy<IContextMenuEntry, IContextMenuEntryMetadata>[] entries = null;
		}

		ContextMenuEntryContext CreateContext(ContextMenuEventArgs e) {
			return ContextMenuEntryContext.Create(elem, e.CursorLeft == -1 && e.CursorTop == -1);
		}

		bool IsIgnored(object sender, ContextMenuEventArgs e)
		{
			if (isIgnored == null)
				return false;

			var o = e.OriginalSource as DependencyObject;
			while (o != null) {
				if (o == elem)
					return false;

				if (isIgnored(o))
					return true;	// Don't set e.Handled

				o = UIUtils.GetParent(o);
			}

			e.Handled = true;
			return true;
		}

		void elem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			if (IsIgnored(sender, e))
				return;

			var context = CreateContext(e);
			ContextMenu menu;
			if (ShowContextMenu(context, out menu))
				elem.ContextMenu = menu;
			else
				e.Handled = true;
		}
		
		void textView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			if (IsIgnored(sender, e))
				return;

			var textView = (DecompilerTextView)elem;
			var context = CreateContext(e);
			ContextMenu menu;
			if (ShowContextMenu(context, out menu)) {
				if (context.OpenedFromKeyboard) {
					var scrollInfo = (IScrollInfo)textView.TextEditor.TextArea.TextView;
					var pos = textView.TextEditor.TextArea.TextView.GetVisualPosition(textView.TextEditor.TextArea.Caret.Position, ICSharpCode.AvalonEdit.Rendering.VisualYPosition.TextBottom);
					pos = new Point(pos.X - scrollInfo.HorizontalOffset, pos.Y - scrollInfo.VerticalOffset);

					menu.HorizontalOffset = pos.X;
					menu.VerticalOffset = pos.Y;
					ContextMenuService.SetPlacement(textView, PlacementMode.Relative);
					ContextMenuService.SetPlacementTarget(textView, textView.TextEditor.TextArea.TextView);
					menu.Closed += (s, e2) => {
						textView.ClearValue(ContextMenuService.PlacementProperty);
						textView.ClearValue(ContextMenuService.PlacementTargetProperty);
					};
				}
				else {
					textView.ClearValue(ContextMenuService.PlacementProperty);
					textView.ClearValue(ContextMenuService.PlacementTargetProperty);
				}
				textView.ContextMenu = menu;
			}
			else
				e.Handled = true;
		}

		void hexBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		{
			if (IsIgnored(sender, e))
				return;

			var hexBox = (HexBox)elem;
			var context = CreateContext(e);
			ContextMenu menu;
			if (ShowContextMenu(context, out menu)) {
				var rect = hexBox.GetCaretWindowRect();
				if (rect != null && context.OpenedFromKeyboard) {
					var pos = rect.Value.BottomLeft;
					menu.HorizontalOffset = pos.X;
					menu.VerticalOffset = pos.Y;
					ContextMenuService.SetPlacement(hexBox, PlacementMode.Relative);
					ContextMenuService.SetPlacementTarget(hexBox, hexBox);
					menu.Closed += (s, e2) => {
						hexBox.ClearValue(ContextMenuService.PlacementProperty);
						hexBox.ClearValue(ContextMenuService.PlacementTargetProperty);
					};
				}
				else {
					hexBox.ClearValue(ContextMenuService.PlacementProperty);
					hexBox.ClearValue(ContextMenuService.PlacementTargetProperty);
				}
				hexBox.ContextMenu = menu;
			}
			else
				e.Handled = true;
		}

		bool ShowContextMenu(ContextMenuEntryContext context, out ContextMenu menu)
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
						bool isEnabled;
						if (entryPair.Value.IsEnabled(context)) {
							menuItem.Click += (s, e) => {
								// Clear this before executing the command since MainWindow might
								// fail to give focus to new elements if it still thinks the menu
								// is opened.
								isMenuOpened = false;
								entry.Execute(context);
							};
							isEnabled = true;
						} else {
							menuItem.IsEnabled = false;
							isEnabled = false;
						}
						if (!string.IsNullOrEmpty(entryPair.Metadata.Icon))
							MainWindow.CreateMenuItemImage(menuItem, entry, entryPair.Metadata.Icon, BackgroundType.ContextMenuItem, isEnabled);
						menuItem.InputGestureText = entryPair.Metadata.InputGestureText ?? string.Empty;
						var entry2 = entry as IContextMenuEntry2;
						if (entry2 != null)
							entry2.Initialize(context, menuItem);
						menu.Items.Add(menuItem);
					}
				}
			}
			menu.Opened += (s, e) => isMenuOpened = true;
			menu.Closed += (s, e) => { isMenuOpened = false; ClearReferences(); };
			return menu.Items.Count > 0;
		}

		public static bool IsMenuOpened {
			get { return isMenuOpened; }
		}
		static bool isMenuOpened;
	}
}
