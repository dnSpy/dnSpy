// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Debugger.Models.TreeModel;
using ICSharpCode.ILSpy.Debugger.Services;

namespace ICSharpCode.ILSpy.Debugger.Tooltips
{
	/// <summary>
	/// Default Control used as content of SharpDevelop debugger tooltips.
	/// </summary>
	internal partial class DebuggerTooltipControl : UserControl, ITooltip
	{
		private const double ChildPopupOpenXOffet = 16;
		private const double ChildPopupOpenYOffet = 15;
		private const int InitialItemsCount = 12;
		private const int VisibleItemsCount = 11;
		
		private bool showPins;
		private LazyItemsControl<ITreeNode> lazyGrid;
		private IEnumerable<ITreeNode> itemsSource;
		readonly TextLocation logicalPosition;
		
		public DebuggerTooltipControl(TextLocation logicalPosition)
		{
			this.logicalPosition = logicalPosition;
			InitializeComponent();
			
			Loaded += new RoutedEventHandler(OnLoaded);
		}

		public DebuggerTooltipControl(TextLocation logicalPosition, ITreeNode node)
			: this(logicalPosition, new ITreeNode[] { node })
		{
			
		}

		public DebuggerTooltipControl(TextLocation logicalPosition, IEnumerable<ITreeNode> nodes)
			: this(logicalPosition)
		{
			this.itemsSource = nodes;
		}

		public DebuggerTooltipControl(DebuggerTooltipControl parentControl, TextLocation logicalPosition, bool showPins = false)
			: this(logicalPosition)
		{
			this.parentControl = parentControl;
			this.showPins = showPins;
		}
		
		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (!showPins) {
				dataGrid.Columns[4].Visibility = Visibility.Collapsed;
			}
			
			SetItemsSource(this.itemsSource);
		}

		public event RoutedEventHandler Closed;
		protected void OnClosed()
		{
			if (this.Closed != null) {
				this.Closed(this, new RoutedEventArgs());
			}
		}
		
		public IEnumerable<ITreeNode> ItemsSource {
			get { return this.itemsSource; }
		}
		
		public void SetItemsSource(IEnumerable<ITreeNode> value) {
			
			if (value == null)
				return;
			
			this.itemsSource = value;
			this.lazyGrid = new LazyItemsControl<ITreeNode>(this.dataGrid, InitialItemsCount);
			
//			// HACK for updating the pins in tooltip
//			var observable = new List<ITreeNode>();
//			this.itemsSource.ForEach(item => observable.Add(item));
//			
//			// verify if at the line of the root there's a pin bookmark
//			ITextEditorProvider provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
//			var editor = provider.TextEditor;
//			if (editor != null) {
//				var pin = BookmarkManager.Bookmarks.Find(
//					b => b is PinBookmark &&
//					b.Location.Line == logicalPosition.Line &&
//					b.FileName == editor.FileName) as PinBookmark;
//				
//				if (pin != null) {
//					observable.ForEach(item => { // TODO: find a way not to use "observable"
//					                   	if (pin.ContainsNode(item))
//					                   		item.IsPinned = true;
//					                   });
//				}
//			}
			
			var source = new VirtualizingIEnumerable<ITreeNode>(value);
			lazyGrid.ItemsSource = source;
			this.dataGrid.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(handleScroll));

			if (this.lazyGrid.ItemsSourceTotalCount != null) {
				// hide up/down buttons if too few items
				btnUp.Visibility = btnDown.Visibility =
					this.lazyGrid.ItemsSourceTotalCount.Value <= VisibleItemsCount ? Visibility.Collapsed : Visibility.Visible;
			}
		}
		
		//public Location LogicalPosition { get; set; }

		/// <inheritdoc/>
		public bool ShowAsPopup
		{
			get
			{
				return true;
			}
		}

		/// <inheritdoc/>
		public bool Close(bool mouseClick)
		{
			if (mouseClick || (!mouseClick && !isChildExpanded)) {
				CloseChildPopups();
				return true;
			} else {
				return false;
			}
		}

		private DebuggerPopup childPopup { get; set; }
		private DebuggerTooltipControl parentControl { get; set; }
		internal DebuggerPopup containingPopup { get; set; }

		bool isChildExpanded
		{
			get
			{
				return this.childPopup != null && this.childPopup.IsOpen;
			}
		}

		private ToggleButton expandedButton;

		/// <summary>
		/// Closes the child popup of this control, if it exists.
		/// </summary>
		public void CloseChildPopups()
		{
			if (this.expandedButton != null) {
				this.expandedButton.IsChecked = false;
				this.expandedButton = null;
				// nice simple example of indirect recursion
				this.childPopup.CloseSelfAndChildren();
			}
		}

		public void CloseOnLostFocus()
		{
			// when we close, parent becomes leaf
			if (this.containingPopup != null) {
				this.containingPopup.IsLeaf = true;
			}
			if (!this.IsMouseOver) {
				if (this.containingPopup != null) {
					this.containingPopup.IsOpen = false;
					this.containingPopup.IsLeaf = false;
				}
				if (this.parentControl != null) {
					this.parentControl.CloseOnLostFocus();
				}
				OnClosed();
			} else {
				// leaf closed because of click inside this control - stop the closing chain
				if (this.expandedButton != null && !this.expandedButton.IsMouseOver) {
					this.expandedButton.IsChecked = false;
					this.expandedButton = null;
				}
			}
		}

		private void btnExpander_Click(object sender, RoutedEventArgs e)
		{
			var clickedButton = (ToggleButton)e.OriginalSource;
			var clickedNode = (ITreeNode)clickedButton.DataContext;
			// use device independent units, because child popup Left/Top are in independent units
			Point buttonPos = clickedButton.PointToScreen(new Point(0, 0)).TransformFromDevice(clickedButton);

			if (clickedButton.IsChecked.GetValueOrDefault(false)) {
				CloseChildPopups();
				this.expandedButton = clickedButton;

				// open child Popup
				if (this.childPopup == null) {
					this.childPopup = new DebuggerPopup(this, logicalPosition, false);
					this.childPopup.Placement = PlacementMode.Absolute;
				}
				if (this.containingPopup != null) {
					this.containingPopup.IsLeaf = false;
				}
				this.childPopup.IsLeaf = true;
				this.childPopup.HorizontalOffset = buttonPos.X + ChildPopupOpenXOffet;
				this.childPopup.VerticalOffset = buttonPos.Y + ChildPopupOpenYOffet;
				this.childPopup.ItemsSource = clickedNode.ChildNodes;
				this.childPopup.Open();
			} else {
				CloseChildPopups();
			}
		}

		private void handleScroll(object sender, ScrollChangedEventArgs e)
		{
			if (this.lazyGrid == null)
				return;
			
			btnUp.IsEnabled = !this.lazyGrid.IsScrolledToStart;
			btnDown.IsEnabled = !this.lazyGrid.IsScrolledToEnd;
		}

		void BtnUp_Click(object sender, RoutedEventArgs e)
		{
			if (this.lazyGrid == null)
				return;
			
			this.lazyGrid.ScrollViewer.ScrollUp(1);
		}

		void BtnDown_Click(object sender, RoutedEventArgs e)
		{
			if (this.lazyGrid == null)
				return;
			
			this.lazyGrid.ScrollViewer.ScrollDown(1);
		}
		
		#region Edit value in tooltip
		
		void TextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape) {
				dataGrid.Focus();
				return;
			}

			if (e.Key == Key.Enter) {
				dataGrid.Focus();
				// set new value
				var textBox = (TextBox)e.OriginalSource;
				var newValue = textBox.Text;
				var node = ((FrameworkElement)sender).DataContext as ITreeNode;
				SaveNewValue(node, textBox.Text);
			}
		}
		
		void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			var textBox = (TextBox)e.OriginalSource;
			var newValue = textBox.Text;
			var node = ((FrameworkElement)sender).DataContext as ITreeNode;
			SaveNewValue(node, textBox.Text);
		}
		
		void SaveNewValue(ITreeNode node, string newValue)
		{
			if(node != null && node.SetText(newValue)) {
				// show adorner
				var adornerLayer = AdornerLayer.GetAdornerLayer(dataGrid);
				var adorners = adornerLayer.GetAdorners(dataGrid);
				if (adorners != null && adorners.Length != 0)
					adornerLayer.Remove(adorners[0]);
				SavedAdorner adorner = new SavedAdorner(dataGrid);
				adornerLayer.Add(adorner);
			}
		}
		
		#endregion
		
		#region Pining checked/unchecked
		
		void PinButton_Checked(object sender, RoutedEventArgs e)
		{
//			ITextEditorProvider provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
//			var editor = provider.TextEditor;
//			if (editor == null) return;
//			var node = (ITreeNode)(((ToggleButton)(e.OriginalSource)).DataContext);
//			
//			if (!string.IsNullOrEmpty(editor.FileName)) {
//				
//				// verify if at the line of the root there's a pin bookmark
//				var pin = BookmarkManager.Bookmarks.Find(
//					b => b is PinBookmark &&
//					b.LineNumber == logicalPosition.Line &&
//					b.FileName == editor.FileName) as PinBookmark;
//				
//				if (pin == null) {
//					pin = new PinBookmark(editor.FileName, logicalPosition);
//					// show pinned DebuggerPopup
//					if (pin.Popup == null) {
//						pin.Popup = new PinDebuggerControl();
//						pin.Popup.Mark = pin;
//						Rect rect = new Rect(this.DesiredSize);
//						var point = this.PointToScreen(rect.TopRight);
//						pin.Popup.Location = new Point { X = 500, Y = point.Y - 150 };
//						pin.Nodes.Add(node);
//						pin.Popup.ItemsSource = pin.Nodes;
//					}
//					
//					// do actions
//					pin.Popup.Open();
//					BookmarkManager.AddMark(pin);
//				}
//				else
//				{
//					if (!pin.ContainsNode(node)) {
//						pin.Nodes.Add(node);
//						pin.Popup.ItemsSource = pin.Nodes;
//					}
//				}
//			}
		}
		
		void PinButton_Unchecked(object sender, RoutedEventArgs e)
		{
//			ITextEditorProvider provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
//			var editor = provider.TextEditor;
//			if (editor == null) return;
//			
//			if (!string.IsNullOrEmpty(editor.FileName)) {
//				// remove from pinned DebuggerPopup
//				var pin = BookmarkManager.Bookmarks.Find(
//					b => b is PinBookmark &&
//					b.LineNumber == logicalPosition.Line &&
//					b.FileName == editor.FileName) as PinBookmark;
//				if (pin == null) return;
//				
//				ToggleButton button = (ToggleButton)e.OriginalSource;
//				pin.RemoveNode((ITreeNode)button.DataContext);
//				pin.Popup.ItemsSource = pin.Nodes;
//				// remove if no more data pins are available
//				if (pin.Nodes.Count == 0) {
//					pin.Popup.Close();
//					
//					BookmarkManager.RemoveMark(pin);
//				}
//			}
		}
		
		#endregion
		
		#region Saved Adorner
		
		class SavedAdorner : Adorner
		{
			public SavedAdorner(UIElement adornedElement) : base(adornedElement)
			{
				Loaded += delegate { Show(); };
			}
			
			protected override void OnRender(DrawingContext drawingContext)
			{
				Rect adornedElementRect = new Rect(this.AdornedElement.DesiredSize);
				
				// Some arbitrary drawing implements.
				var formatedText = new FormattedText("Saved",
				                                     CultureInfo.CurrentCulture,
				                                     FlowDirection.LeftToRight,
				                                     new Typeface(new FontFamily("Arial"),
				                                                  FontStyles.Normal,
				                                                  FontWeights.Black,
				                                                  FontStretches.Expanded),
				                                     8d,
				                                     Brushes.Black);
				
				
				drawingContext.DrawText(formatedText,
				                        new Point(adornedElementRect.TopRight.X - formatedText.Width - 2,
				                                  adornedElementRect.TopRight.Y));
			}
			
			private void Show()
			{
				DoubleAnimation animation = new DoubleAnimation();
				animation.From = 1;
				animation.To = 0;
				
				animation.Duration = new Duration(TimeSpan.FromSeconds(2));
				animation.SetValue(Storyboard.TargetProperty, this);
				animation.SetValue(Storyboard.TargetPropertyProperty, new PropertyPath(Rectangle.OpacityProperty));
				
				Storyboard board = new Storyboard();
				board.Children.Add(animation);
				
				board.Begin(this);
			}
		}
		
		#endregion
	}
}