/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Controls;
using dnSpy.Properties;
using dnSpy.Text;
using dnSpy.Text.Editor;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionPresenter : IPopupIntellisensePresenter, IMouseProcessor {
		UIElement IPopupIntellisensePresenter.SurfaceElement => control;
		PopupStyles IPopupIntellisensePresenter.PopupStyles => PopupStyles.None;
		string IPopupIntellisensePresenter.SpaceReservationManagerName => PredefinedSpaceReservationManagerNames.Completion;
		IIntellisenseSession IIntellisensePresenter.Session => session;

		public ITrackingSpan PresentationSpan {
			get { return presentationSpan; }
			private set {
				if (!TrackingSpanHelpers.IsSameTrackingSpan(presentationSpan, value)) {
					presentationSpan = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PresentationSpan)));
				}
			}
		}
		ITrackingSpan presentationSpan;

		double IPopupIntellisensePresenter.Opacity {
			get { return control.Opacity; }
			set {
				control.Opacity = value;
				if (value != 1)
					HideToolTip();
				else if (!toolTipTimer.IsEnabled)
					DelayShowToolTip();
			}
		}

		readonly IImageManager imageManager;
		readonly ICompletionSession session;
		readonly ICompletionTextElementProvider completionTextElementProvider;
		readonly Lazy<IUIElementProvider<Completion, ICompletionSession>, IOrderableContentTypeMetadata>[] completionUIElementProviders;
		readonly CompletionPresenterControl control;
		readonly List<FilterVM> filters;
		readonly IWpfTextView wpfTextView;
		readonly DispatcherTimer toolTipTimer;
		ToolTip toolTip;
		Completion toolTipCompletion;
		double oldZoomLevel = double.NaN;

		public object Filters => filters;
		public bool HasFilters => filters.Count > 1;
		public event PropertyChangedEventHandler PropertyChanged;

		const double defaultMaxHeight = 200;
		const double defaultMinWidth = 150;
		const double toolTipDelayMilliSeconds = 250;

		public CompletionPresenter(IImageManager imageManager, ICompletionSession session, ICompletionTextElementProvider completionTextElementProvider, Lazy<IUIElementProvider<Completion, ICompletionSession>, IOrderableContentTypeMetadata>[] completionUIElementProviders) {
			if (imageManager == null)
				throw new ArgumentNullException(nameof(imageManager));
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			if (completionTextElementProvider == null)
				throw new ArgumentNullException(nameof(completionTextElementProvider));
			if (completionUIElementProviders == null)
				throw new ArgumentNullException(nameof(completionUIElementProviders));
			this.imageManager = imageManager;
			this.session = session;
			this.completionTextElementProvider = completionTextElementProvider;
			this.completionUIElementProviders = completionUIElementProviders;
			this.control = new CompletionPresenterControl { DataContext = this };
			this.filters = new List<FilterVM>();
			this.control.MinWidth = defaultMinWidth;
			this.control.completionsListBox.MaxHeight = defaultMaxHeight;
			session.SelectedCompletionCollectionChanged += CompletionSession_SelectedCompletionCollectionChanged;
			session.Dismissed += CompletionSession_Dismissed;
			session.TextView.LostAggregateFocus += TextView_LostAggregateFocus;
			session.TextView.TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
			this.wpfTextView = session.TextView as IWpfTextView;
			Debug.Assert(wpfTextView != null);
			if (wpfTextView != null)
				wpfTextView.VisualElement.PreviewKeyDown += VisualElement_PreviewKeyDown;
			session.TextView.LayoutChanged += TextView_LayoutChanged;
			control.completionsListBox.SelectionChanged += CompletionsListBox_SelectionChanged;
			control.completionsListBox.Loaded += CompletionsListBox_Loaded;
			control.completionsListBox.PreviewMouseDown += CompletionsListBox_PreviewMouseDown;
			control.completionsListBox.PreviewMouseUp += CompletionsListBox_PreviewMouseUp;
			control.completionsListBox.MouseLeave += CompletionsListBox_MouseLeave;
			control.completionsListBox.MouseDoubleClick += CompletionsListBox_MouseDoubleClick;
			control.SizeChanged += Control_SizeChanged;
			control.AddHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(Control_GotKeyboardFocus), true);

			toolTipTimer = new DispatcherTimer(DispatcherPriority.Background, control.Dispatcher);
			toolTipTimer.Tick += ToolTipTimer_Tick;
			toolTipTimer.Interval = TimeSpan.FromMilliseconds(toolTipDelayMilliSeconds);

			UpdateSelectedCompletion();
			UpdateFilterCollection();
		}

		void CompletionsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			var item = control.completionsListBox.SelectedItem;
			if (item == null)
				return;
			var listboxItem = control.completionsListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
			if (listboxItem == null || !listboxItem.IsVisible)
				return;
			if (!listboxItem.IsMouseOver)
				return;
			if (item != session.SelectedCompletionCollection?.CurrentCompletion.Completion)
				return;
			session.Commit();
		}

		void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			if (wpfTextView != null && oldZoomLevel != wpfTextView.ZoomLevel) {
				oldZoomLevel = wpfTextView.ZoomLevel;
				HideToolTip();
			}
			if (e.HorizontalTranslation || e.VerticalTranslation)
				HideToolTip();
		}

		void ToolTipTimer_Tick(object sender, EventArgs e) {
			toolTipTimer.Stop();
			if (session.IsDismissed)
				return;
			ShowToolTip();
		}

		void DelayShowToolTip() {
			toolTipTimer.Stop();
			HideToolTip();
			if (session.IsDismissed)
				return;
			toolTipTimer.Start();
		}

		void ShowToolTip() {
			if (session.IsDismissed)
				return;
			var completion = control.completionsListBox.SelectedItem as Completion;
			if (completion == toolTipCompletion)
				return;
			HideToolTip();
			if (completion == null)
				return;
			var container = control.completionsListBox.ItemContainerGenerator.ContainerFromItem(completion) as ListBoxItem;
			if (container == null || !container.IsVisible)
				return;
			var toolTipElem = TryGetToolTipUIElement(completion);
			if (toolTipElem == null)
				return;

			// When the tooltip was reused, it was empty every other time, so always create a new one.
			toolTip = new ToolTip {
				Placement = PlacementMode.Right,
				Visibility = Visibility.Collapsed,
				IsOpen = false,
			};
			toolTip.SetResourceReference(FrameworkElement.StyleProperty, "CompletionToolTipStyle");

			// There's a scrollbar; place the tooltip to the right of the main control and not the ListBoxItem
			var pointRelativeToControl = container.TranslatePoint(new Point(0, 0), control);
			toolTip.VerticalOffset = pointRelativeToControl.Y;
			toolTip.PlacementTarget = control;
			toolTip.Content = toolTipElem;
			toolTip.Visibility = Visibility.Visible;
			Debug.Assert(!toolTip.IsOpen, "Can't set the tool tip's LayoutTransform if it's open");
			PopupHelper.SetScaleTransform(wpfTextView, toolTip);
			toolTipCompletion = completion;
			toolTip.IsOpen = true;
		}

		UIElement TryGetToolTipUIElement(Completion completion) {
			if (completion == null)
				return null;

			var contentType = wpfTextView.TextDataModel.ContentType;
			foreach (var provider in completionUIElementProviders) {
				if (!contentType.IsOfAnyType(provider.Metadata.ContentTypes))
					continue;
				var elem = provider.Value.GetUIElement(completion, session, UIElementType.Tooltip);
				if (elem != null)
					return elem;
			}

			var description = completion.Description;
			if (!string.IsNullOrEmpty(description))
				return CreateDefaultToolTipUIElement(description);

			return null;
		}

		UIElement CreateDefaultToolTipUIElement(string description) {
			Debug.Assert(!string.IsNullOrEmpty(description));
			if (string.IsNullOrEmpty(description))
				return null;

			var screen = new Screen(wpfTextView.VisualElement);
			var screenWidth = screen.IsValid ? screen.DisplayRect.Width : SystemParameters.WorkArea.Width;
			var maxWidth = screenWidth * 0.4;

			return new TextBlock {
				Text = description,
				MaxWidth = maxWidth,
				TextWrapping = TextWrapping.Wrap,
			};
		}

		void HideToolTip() {
			if (toolTip == null)
				return;
			toolTip.IsOpen = false;
			toolTip.Visibility = Visibility.Collapsed;
			toolTip.Content = null;
			toolTip.PlacementTarget = null;
			toolTip = null;
			toolTipCompletion = null;
		}

		// Hack needed so we can give back keyboard focus to the text view as fast as possible. We delay
		// this if the user uses the mouse, else we do it immediately. If we don't do it immediately,
		// we'll miss typed characters if the user types fast (the listbox gets the typed chars).
		bool isMouseSelection;
		void CompletionsListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e) => isMouseSelection = true;
		void CompletionsListBox_PreviewMouseUp(object sender, MouseButtonEventArgs e) => isMouseSelection = false;
		void CompletionsListBox_MouseLeave(object sender, MouseEventArgs e) => isMouseSelection = false;

		void CompletionsListBox_Loaded(object sender, RoutedEventArgs e) {
			control.completionsListBox.Loaded -= CompletionsListBox_Loaded;
			if (control.completionsListBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
				InitializeLoaded();
			else
				control.completionsListBox.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
		}

		void ItemContainerGenerator_StatusChanged(object sender, EventArgs e) {
			if (control.completionsListBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated) {
				control.completionsListBox.ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
				InitializeLoaded();
			}
		}

		void InitializeLoaded() {
			UpdateSelectedItem();
			var item = control.completionsListBox.SelectedItem;
			if (item == null && control.completionsListBox.Items.Count > 0)
				item = control.completionsListBox.Items[0];
			var scrollViewer = WpfUtils.TryGetScrollViewer(control.completionsListBox);
			if (item != null && scrollViewer != null) {
				var lbItem = control.completionsListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
				lbItem.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				var itemHeight = lbItem.DesiredSize.Height;
				double maxHeight = itemHeight * 9;
				var borderThickness = control.completionsListBox.BorderThickness;
				maxHeight += borderThickness.Top + borderThickness.Bottom;
				if (maxHeight > 50)
					control.completionsListBox.MaxHeight = maxHeight;
			}

			if (scrollViewer != null) {
				if (scrollViewer.ViewportHeight != 0)
					UpdateSelectedItem();
				else {
					control.completionsListBox.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
						UpdateSelectedItem(true);
					}));
				}
			}
		}

		// Make sure the text view gets focus again whenever the listbox gets focus
		void Control_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			if (!isMouseSelection)
				(session.TextView as IWpfTextView)?.VisualElement.Focus();
			else
				control.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => (session.TextView as IWpfTextView)?.VisualElement.Focus()));
		}

		void Control_SizeChanged(object sender, SizeChangedEventArgs e) {
			// Prevent the control from getting thinner when pressing PageUp/Down
			if (control.MinWidth != e.NewSize.Width)
				control.MinWidth = e.NewSize.Width;

			HideToolTip();
		}

		void VisualElement_PreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Handled)
				return;
			if (e.KeyboardDevice.Modifiers != ModifierKeys.Alt)
				return;
			var accessKey = GetAccessKey(e.SystemKey);
			if (accessKey == null)
				return;
			foreach (var filter in filters) {
				if (StringComparer.OrdinalIgnoreCase.Equals(filter.AccessKey, accessKey)) {
					filter.IsChecked = !filter.IsChecked;
					e.Handled = true;
					return;
				}
			}
		}

		static string GetAccessKey(Key key) {
			switch (key) {
			case Key.A: return "A";
			case Key.B: return "B";
			case Key.C: return "C";
			case Key.D: return "D";
			case Key.E: return "E";
			case Key.F: return "F";
			case Key.G: return "G";
			case Key.H: return "H";
			case Key.I: return "I";
			case Key.J: return "J";
			case Key.K: return "K";
			case Key.L: return "L";
			case Key.M: return "M";
			case Key.N: return "N";
			case Key.O: return "O";
			case Key.P: return "P";
			case Key.Q: return "Q";
			case Key.R: return "R";
			case Key.S: return "S";
			case Key.T: return "T";
			case Key.U: return "U";
			case Key.V: return "V";
			case Key.W: return "W";
			case Key.X: return "X";
			case Key.Y: return "Y";
			case Key.Z: return "Z";
			default: return null;
			}
		}

		void TextBuffer_ChangedLowPriority(object sender, TextContentChangedEventArgs e) => Refilter();

		void Refilter() {
			if (!session.IsDismissed) {
				session.Filter();
				session.Match();
				// Filter() could've scrolled the selected item out of view (less items are shown but
				// it keeps the old viewport Y offset), and Match() could've selected the same item again
				// (i.e., no CurrentCompletionChanged event) which could result in the item being out of
				// view. Fix that by always making sure it's visible after Filter() + Match().
				UpdateSelectedItem();
			}
		}

		void CompletionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e.AddedItems == null || e.AddedItems.Count < 1)
				return;
			Debug.Assert(e.AddedItems.Count == 1);
			var coll = session.SelectedCompletionCollection;
			if (coll == null)
				return;
			var newCompletion = e.AddedItems[0] as Completion;
			Debug.Assert(newCompletion != null);
			if (newCompletion == null)
				return;
			if (coll.CurrentCompletion.Completion == e.AddedItems[0])
				return;
			bool validItem = coll.FilteredCollection.Contains(newCompletion);
			Debug.Assert(validItem);
			if (!validItem)
				return;
			coll.CurrentCompletion = new CurrentCompletion(newCompletion, isSelected: true, isUnique: true);
		}

		public bool ExecuteKeyboardCommand(IntellisenseKeyboardCommand command) {
			switch (command) {
			case IntellisenseKeyboardCommand.Up:
				MoveUpDown(true);
				return true;

			case IntellisenseKeyboardCommand.Down:
				MoveUpDown(false);
				return true;

			case IntellisenseKeyboardCommand.PageUp:
				PageUpDown(true);
				return true;

			case IntellisenseKeyboardCommand.PageDown:
				PageUpDown(false);
				return true;

			case IntellisenseKeyboardCommand.Escape:
				session.Dismiss();
				return true;

			case IntellisenseKeyboardCommand.Enter:
				session.Commit();
				return true;

			case IntellisenseKeyboardCommand.TopLine:
				WpfUtils.ScrollToTop(control.completionsListBox);
				return true;

			case IntellisenseKeyboardCommand.BottomLine:
				WpfUtils.ScrollToBottom(control.completionsListBox);
				return true;

			case IntellisenseKeyboardCommand.Home:
			case IntellisenseKeyboardCommand.End:
			case IntellisenseKeyboardCommand.IncreaseFilterLevel:
			case IntellisenseKeyboardCommand.DecreaseFilterLevel:
			default:
				return false;
			}
		}

		void MoveUpDown(bool up) => MoveUpDown(up ? -1 : 1, true);

		void PageUpDown(bool up) {
			const int defaultValue = 9;
			var items = WpfUtils.GetItemsPerPage(control.completionsListBox, defaultValue);
			MoveUpDown(up ? -items : items, false);
		}

		void MoveUpDown(int count, bool mustBeSelected) {
			var coll = session.SelectedCompletionCollection.FilteredCollection;
			int index = coll.IndexOf(session.SelectedCompletionCollection.CurrentCompletion.Completion);
			if (index < 0)
				index = 0;
			if (!mustBeSelected || session.SelectedCompletionCollection.CurrentCompletion.IsSelected)
				index = index + count;
			if (index < 0)
				index = 0;
			else if (index >= coll.Count)
				index = coll.Count - 1;
			var newItem = (uint)index >= (uint)coll.Count ? null : coll[index];

			ignoreScrollIntoView = true;
			try {
				control.completionsListBox.SelectedItem = newItem;
			}
			finally {
				ignoreScrollIntoView = false;
			}

			session.SelectedCompletionCollection.CurrentCompletion = new CurrentCompletion(newItem, isSelected: true, isUnique: true);
			ScrollSelectedItemIntoView(false);
		}

		bool ignoreScrollIntoView;
		void ScrollSelectedItemIntoView(bool center) {
			if (!ignoreScrollIntoView)
				WpfUtils.ScrollSelectedItemIntoView(control.completionsListBox, center);
		}

		void TextView_LostAggregateFocus(object sender, EventArgs e) => session.Dismiss();
		void CompletionSession_SelectedCompletionCollectionChanged(object sender, SelectedCompletionCollectionEventArgs e) {
			UpdateSelectedCompletion();
			UpdateFilterCollection();
		}

		void UpdateFilterCollection() {
			var coll = session.SelectedCompletionCollection;
			DisposeFilters();
			if (coll != null) {
				foreach (var filter in coll.Filters)
					filters.Add(new FilterVM(filter, this));
			}
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Filters)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasFilters)));
		}

		void DisposeFilters() {
			foreach (var filter in filters)
				filter.Dispose();
			filters.Clear();
		}

		internal void OnIsCheckedChanged(FilterVM filterVM) {
			// Prevent the control from shrinking in size. That will make clicking with
			// the mouse much more difficult.
			if (filters.Any(a => a.IsChecked))
				control.MinHeight = control.ActualHeight;
			else
				control.MinHeight = 0;
			Refilter();
		}

		void UpdateSelectedCompletion() {
			var coll = session.SelectedCompletionCollection;
			RegisterCompletionCollectionEvents(coll);
			control.completionsListBox.ItemsSource = coll?.FilteredCollection;
			UpdateSelectedItem();
		}

		void UpdateSelectedItem(bool? forceCenter = null) {
			PresentationSpan = currentCompletionCollection?.ApplicableTo;
			if (currentCompletionCollection == null)
				control.completionsListBox.SelectedItem = null;
			else {
				control.completionsListBox.SelectedItem = currentCompletionCollection.CurrentCompletion.Completion;
				ScrollSelectedItemIntoView(forceCenter ?? !control.IsKeyboardFocusWithin);
				if (!currentCompletionCollection.CurrentCompletion.IsSelected)
					control.completionsListBox.SelectedItem = null;
			}
			DelayShowToolTip();
		}

		void CompletionCollection_CurrentCompletionChanged(object sender, EventArgs e) {
			Debug.Assert(currentCompletionCollection == sender);
			UpdateSelectedItem();
		}

		CompletionCollection currentCompletionCollection;
		void RegisterCompletionCollectionEvents(CompletionCollection collection) {
			UnregisterCompletionCollectionEvents();
			Debug.Assert(currentCompletionCollection == null);
			currentCompletionCollection = collection;
			collection.CurrentCompletionChanged += CompletionCollection_CurrentCompletionChanged;
		}

		void UnregisterCompletionCollectionEvents() {
			if (currentCompletionCollection != null) {
				currentCompletionCollection.CurrentCompletionChanged -= CompletionCollection_CurrentCompletionChanged;
				currentCompletionCollection = null;
			}
		}

		void CompletionSession_Dismissed(object sender, EventArgs e) {
			UnregisterCompletionCollectionEvents();
			DisposeFilters();
			toolTipTimer.Stop();
			toolTipTimer.Tick -= ToolTipTimer_Tick;
			HideToolTip();
			session.SelectedCompletionCollectionChanged -= CompletionSession_SelectedCompletionCollectionChanged;
			session.Dismissed -= CompletionSession_Dismissed;
			session.TextView.LostAggregateFocus -= TextView_LostAggregateFocus;
			session.TextView.TextBuffer.ChangedLowPriority -= TextBuffer_ChangedLowPriority;
			control.completionsListBox.SelectionChanged -= CompletionsListBox_SelectionChanged;
			control.completionsListBox.Loaded -= CompletionsListBox_Loaded;
			control.completionsListBox.PreviewMouseDown -= CompletionsListBox_PreviewMouseDown;
			control.completionsListBox.PreviewMouseUp -= CompletionsListBox_PreviewMouseUp;
			control.completionsListBox.MouseLeave -= CompletionsListBox_MouseLeave;
			control.completionsListBox.MouseDoubleClick -= CompletionsListBox_MouseDoubleClick;
			control.completionsListBox.ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;
			control.SizeChanged -= Control_SizeChanged;
			control.GotKeyboardFocus -= Control_GotKeyboardFocus;
			if (wpfTextView != null)
				wpfTextView.VisualElement.PreviewKeyDown -= VisualElement_PreviewKeyDown;
			session.TextView.LayoutChanged -= TextView_LayoutChanged;
			completionTextElementProvider.Dispose();
		}

		public ImageSource GetImageSource(Completion completion) => GetImageSource(completion.Image);
		public ImageSource GetImageSource(FilterVM filterVM) => GetImageSource(filterVM.Image);
		ImageSource GetImageSource(ImageReference imageReference) {
			if (session.IsDismissed)
				return null;
			if (imageReference.IsDefault)
				return null;
			return imageManager.GetImage(imageReference, BackgroundType.ListBoxItem);
		}

		public FrameworkElement GetDisplayText(Completion completion) {
			if (session.IsDismissed)
				return null;
			var collection = session.SelectedCompletionCollection;
			Debug.Assert(collection.FilteredCollection.Contains(completion));
			return completionTextElementProvider.Create(collection, completion);
		}

		public string GetToolTip(FilterVM filterVM) {
			if (session.IsDismissed)
				return null;
			var toolTip = filterVM.ToolTip;
			var accessKey = filterVM.AccessKey;
			if (string.IsNullOrEmpty(toolTip))
				return null;
			if (!string.IsNullOrEmpty(accessKey))
				return string.Format("{0} ({1})", toolTip, string.Format(dnSpy_Resources.ShortCutKeyAltPlusAnyKey, accessKey.ToUpper()));
			return toolTip;
		}

		static int GetScrollWheelLines() {
			if (!SystemParameters.IsMouseWheelPresent)
				return 1;
			return SystemParameters.WheelScrollLines;
		}

		void IMouseProcessor.PostprocessMouseWheel(MouseWheelEventArgs e) {
			if (e.Handled)
				return;
			if (Keyboard.Modifiers != ModifierKeys.None)
				return;
			if (e.Delta == 0)
				return;

			int lines = GetScrollWheelLines();
			if (e.Delta < 0)
				lines = -lines;
			WpfUtils.Scroll(control.completionsListBox, lines);
			e.Handled = true;
		}

		void IMouseProcessor.PreprocessMouseLeftButtonDown(MouseButtonEventArgs e) { }
		void IMouseProcessor.PostprocessMouseLeftButtonDown(MouseButtonEventArgs e) { }
		void IMouseProcessor.PreprocessMouseRightButtonDown(MouseButtonEventArgs e) { }
		void IMouseProcessor.PostprocessMouseRightButtonDown(MouseButtonEventArgs e) { }
		void IMouseProcessor.PreprocessMouseLeftButtonUp(MouseButtonEventArgs e) { }
		void IMouseProcessor.PostprocessMouseLeftButtonUp(MouseButtonEventArgs e) { }
		void IMouseProcessor.PreprocessMouseRightButtonUp(MouseButtonEventArgs e) { }
		void IMouseProcessor.PostprocessMouseRightButtonUp(MouseButtonEventArgs e) { }
		void IMouseProcessor.PreprocessMouseUp(MouseButtonEventArgs e) { }
		void IMouseProcessor.PostprocessMouseUp(MouseButtonEventArgs e) { }
		void IMouseProcessor.PreprocessMouseDown(MouseButtonEventArgs e) { }
		void IMouseProcessor.PostprocessMouseDown(MouseButtonEventArgs e) { }
		void IMouseProcessor.PreprocessMouseMove(MouseEventArgs e) { }
		void IMouseProcessor.PostprocessMouseMove(MouseEventArgs e) { }
		void IMouseProcessor.PreprocessMouseWheel(MouseWheelEventArgs e) { }
		void IMouseProcessor.PreprocessMouseEnter(MouseEventArgs e) { }
		void IMouseProcessor.PostprocessMouseEnter(MouseEventArgs e) { }
		void IMouseProcessor.PreprocessMouseLeave(MouseEventArgs e) { }
		void IMouseProcessor.PostprocessMouseLeave(MouseEventArgs e) { }
		void IMouseProcessor.PreprocessDragLeave(DragEventArgs e) { }
		void IMouseProcessor.PostprocessDragLeave(DragEventArgs e) { }
		void IMouseProcessor.PreprocessDragOver(DragEventArgs e) { }
		void IMouseProcessor.PostprocessDragOver(DragEventArgs e) { }
		void IMouseProcessor.PreprocessDragEnter(DragEventArgs e) { }
		void IMouseProcessor.PostprocessDragEnter(DragEventArgs e) { }
		void IMouseProcessor.PreprocessDrop(DragEventArgs e) { }
		void IMouseProcessor.PostprocessDrop(DragEventArgs e) { }
		void IMouseProcessor.PreprocessQueryContinueDrag(QueryContinueDragEventArgs e) { }
		void IMouseProcessor.PostprocessQueryContinueDrag(QueryContinueDragEventArgs e) { }
		void IMouseProcessor.PreprocessGiveFeedback(GiveFeedbackEventArgs e) { }
		void IMouseProcessor.PostprocessGiveFeedback(GiveFeedbackEventArgs e) { }
	}
}
