/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Windows.Threading;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Contracts.Language.Intellisense.Classification;
using dnSpy.Controls;
using dnSpy.Properties;
using dnSpy.Text;
using dnSpy.Text.Editor;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionPresenter : IPopupIntellisensePresenter, IIntellisenseCommandTarget, IMouseProcessor, INotifyPropertyChanged {
		UIElement IPopupIntellisensePresenter.SurfaceElement => control;
		PopupStyles IPopupIntellisensePresenter.PopupStyles => PopupStyles.None;
		string IPopupIntellisensePresenter.SpaceReservationManagerName => IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName;
		IIntellisenseSession IIntellisensePresenter.Session => session;
		event EventHandler IPopupIntellisensePresenter.SurfaceElementChanged { add { } remove { } }
		event EventHandler<ValueChangedEventArgs<PopupStyles>> IPopupIntellisensePresenter.PopupStylesChanged { add { } remove { } }
		public event EventHandler PresentationSpanChanged;

		public ITrackingSpan PresentationSpan {
			get { return presentationSpan; }
			private set {
				if (!TrackingSpanHelpers.IsSameTrackingSpan(presentationSpan, value)) {
					presentationSpan = value;
					PresentationSpanChanged?.Invoke(this, EventArgs.Empty);
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

		readonly IImageMonikerService imageMonikerService;
		readonly ICompletionSession session;
		readonly ICompletionTextElementProvider completionTextElementProvider;
		readonly Lazy<IUIElementProvider<Completion, ICompletionSession>, IOrderableContentTypeMetadata>[] completionUIElementProviders;
		readonly CompletionPresenterControl control;
		readonly List<FilterVM> filters;
		readonly IWpfTextView wpfTextView;
		readonly DispatcherTimer toolTipTimer;
		ToolTip toolTip;
		CompletionVM toolTipCompletionVM;
		double oldZoomLevel = double.NaN;

		public object Filters => filters;
		public bool HasFilters => filters.Count > 1;
		public event PropertyChangedEventHandler PropertyChanged;

		const double defaultMaxHeight = 200;
		const double defaultMinWidth = 150;
		const double toolTipDelayMilliSeconds = 250;

		public CompletionPresenter(IImageMonikerService imageMonikerService, ICompletionSession session, ICompletionTextElementProvider completionTextElementProvider, Lazy<IUIElementProvider<Completion, ICompletionSession>, IOrderableContentTypeMetadata>[] completionUIElementProviders) {
			this.imageMonikerService = imageMonikerService ?? throw new ArgumentNullException(nameof(imageMonikerService));
			this.session = session ?? throw new ArgumentNullException(nameof(session));
			this.completionTextElementProvider = completionTextElementProvider ?? throw new ArgumentNullException(nameof(completionTextElementProvider));
			this.completionUIElementProviders = completionUIElementProviders ?? throw new ArgumentNullException(nameof(completionUIElementProviders));
			control = new CompletionPresenterControl { DataContext = this };
			filters = new List<FilterVM>();
			control.MinWidth = defaultMinWidth;
			control.completionsListBox.MaxHeight = defaultMaxHeight;
			session.SelectedCompletionSetChanged += CompletionSession_SelectedCompletionSetChanged;
			session.Dismissed += CompletionSession_Dismissed;
			session.TextView.LostAggregateFocus += TextView_LostAggregateFocus;
			session.TextView.TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
			wpfTextView = session.TextView as IWpfTextView;
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
			var item = control.completionsListBox.SelectedItem as CompletionVM;
			if (item == null)
				return;
			var listboxItem = control.completionsListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
			if (listboxItem == null || !listboxItem.IsVisible)
				return;
			if (!listboxItem.IsMouseOver)
				return;
			if (item.Completion != session.SelectedCompletionSet?.SelectionStatus.Completion)
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
			var completionVM = control.completionsListBox.SelectedItem as CompletionVM;
			if (completionVM == toolTipCompletionVM)
				return;
			HideToolTip();
			if (completionVM == null)
				return;
			var container = control.completionsListBox.ItemContainerGenerator.ContainerFromItem(completionVM) as ListBoxItem;
			if (container == null || !container.IsVisible)
				return;
			var toolTipElem = TryGetToolTipUIElement(completionVM);
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
			toolTipCompletionVM = completionVM;
			toolTip.IsOpen = true;
		}

		UIElement TryGetToolTipUIElement(CompletionVM completionVM) {
			if (completionVM == null)
				return null;

			var description = completionVM.Completion.Description;
			if (string.IsNullOrEmpty(description))
				return null;

			var contentType = session.TextView.TextDataModel.ContentType;
			foreach (var provider in completionUIElementProviders) {
				if (!contentType.IsOfAnyType(provider.Metadata.ContentTypes))
					continue;
				var elem = provider.Value.GetUIElement(completionVM.Completion, session, UIElementType.Tooltip);
				if (elem != null)
					return elem;
			}

			return CreateDefaultToolTipUIElement(description);
		}

		UIElement CreateDefaultToolTipUIElement(string description) {
			Debug.Assert(!string.IsNullOrEmpty(description));
			if (string.IsNullOrEmpty(description))
				return null;

			var screen = new Screen(wpfTextView?.VisualElement);
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
			toolTipCompletionVM = null;
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
			var completionSet = session.SelectedCompletionSet;
			if (completionSet == null)
				return;
			var newCompletion = e.AddedItems[0] as CompletionVM;
			Debug.Assert(newCompletion != null);
			if (newCompletion == null)
				return;
			if (completionSet.SelectionStatus.Completion == newCompletion.Completion)
				return;
			bool validItem = completionSet.Completions.Contains(newCompletion.Completion);
			Debug.Assert(validItem);
			if (!validItem)
				return;
			completionSet.SelectionStatus = new CompletionSelectionStatus(newCompletion.Completion, isSelected: true, isUnique: true);
		}

		bool IIntellisenseCommandTarget.ExecuteKeyboardCommand(IntellisenseKeyboardCommand command) {
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
				if (session.SelectedCompletionSet?.SelectionStatus.IsSelected == true) {
					session.Commit();
					return true;
				}
				return false;

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
			var coll = session.SelectedCompletionSet?.Completions;
			if (coll == null)
				return;
			int index = coll.IndexOf(session.SelectedCompletionSet.SelectionStatus.Completion);
			if (index < 0)
				index = 0;
			if (!mustBeSelected || session.SelectedCompletionSet.SelectionStatus.IsSelected)
				index = index + count;
			if (index < 0)
				index = 0;
			else if (index >= coll.Count)
				index = coll.Count - 1;
			var newItem = (uint)index >= (uint)coll.Count ? null : coll[index];

			ignoreScrollIntoView = true;
			try {
				control.completionsListBox.SelectedItem = GetExistingCompletionVM(newItem);
			}
			finally {
				ignoreScrollIntoView = false;
			}

			session.SelectedCompletionSet.SelectionStatus = new CompletionSelectionStatus(newItem, isSelected: true, isUnique: true);
			ScrollSelectedItemIntoView(false);
		}

		bool ignoreScrollIntoView;
		void ScrollSelectedItemIntoView(bool center) {
			if (!ignoreScrollIntoView)
				WpfUtils.ScrollSelectedItemIntoView(control.completionsListBox, center);
		}

		void TextView_LostAggregateFocus(object sender, EventArgs e) => session.Dismiss();
		void CompletionSession_SelectedCompletionSetChanged(object sender, ValueChangedEventArgs<CompletionSet> e) {
			UpdateSelectedCompletion();
			UpdateFilterCollection();
		}

		void UpdateFilterCollection() {
			var filterCompletionSet = session.SelectedCompletionSet as CompletionSet2;
			DisposeFilters();
			if (filterCompletionSet != null) {
				var completionSetFilters = filterCompletionSet.Filters;
				if (completionSetFilters != null) {
					foreach (var filter in completionSetFilters)
						filters.Add(new FilterVM(filter, this, imageMonikerService.ToImageReference(filter.Moniker)));
				}
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
			var completionSet = session.SelectedCompletionSet;
			RegisterCompletionSetEvents(completionSet);
			control.completionsListBox.ItemsSource = RecreateCompletionCollectionVM(completionSet?.Completions);
			UpdateSelectedItem();
		}

		CompletionCollectionVM RecreateCompletionCollectionVM(IList<Completion> completions) {
			completionCollectionVM?.Dispose();
			completionCollectionVM = null;
			if (completions == null)
				return null;
			completionCollectionVM = new CompletionCollectionVM(completions, imageMonikerService);
			return completionCollectionVM;
		}
		CompletionCollectionVM completionCollectionVM;

		CompletionVM GetExistingCompletionVM(Completion completion) {
			if (completion == null)
				return null;
			var vm = CompletionVM.TryGet(completion);
			Debug.Assert(vm != null);
			return vm;
		}

		void UpdateSelectedItem(bool? forceCenter = null) {
			PresentationSpan = currentCompletionSet?.ApplicableTo;
			if (currentCompletionSet == null)
				control.completionsListBox.SelectedItem = null;
			else {
				control.completionsListBox.SelectedItem = GetExistingCompletionVM(currentCompletionSet.SelectionStatus.Completion);
				ScrollSelectedItemIntoView(forceCenter ?? !control.IsKeyboardFocusWithin);
				if (!currentCompletionSet.SelectionStatus.IsSelected)
					control.completionsListBox.SelectedItem = null;
			}
			DelayShowToolTip();
		}

		void CompletionSet_SelectionStatusChanged(object sender, ValueChangedEventArgs<CompletionSelectionStatus> e) {
			Debug.Assert(currentCompletionSet == sender);
			UpdateSelectedItem();
		}

		CompletionSet currentCompletionSet;
		void RegisterCompletionSetEvents(CompletionSet completionSet) {
			UnregisterCompletionSetEvents();
			Debug.Assert(currentCompletionSet == null);
			currentCompletionSet = completionSet;
			if (completionSet != null)
				completionSet.SelectionStatusChanged += CompletionSet_SelectionStatusChanged;
		}

		void UnregisterCompletionSetEvents() {
			if (currentCompletionSet != null) {
				currentCompletionSet.SelectionStatusChanged -= CompletionSet_SelectionStatusChanged;
				currentCompletionSet = null;
			}
		}

		void CompletionSession_Dismissed(object sender, EventArgs e) {
			UnregisterCompletionSetEvents();
			DisposeFilters();
			toolTipTimer.Stop();
			toolTipTimer.Tick -= ToolTipTimer_Tick;
			HideToolTip();
			session.SelectedCompletionSetChanged -= CompletionSession_SelectedCompletionSetChanged;
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
			control.completionsListBox.ItemsSource = null;
			completionCollectionVM?.Dispose();
			completionCollectionVM = null;
		}

		public FrameworkElement GetDisplayText(CompletionVM vm) => CreateFrameworkElement(vm.Completion, CompletionClassifierKind.DisplayText);

		public FrameworkElement GetSuffix(CompletionVM vm) {
			var completion = vm.Completion;
			if (string.IsNullOrEmpty((completion as Completion4)?.Suffix))
				return null;
			var elem = CreateFrameworkElement(completion, CompletionClassifierKind.Suffix);
			elem.Margin = new Thickness(5, 0, 2, 0);
			return elem;
		}

		FrameworkElement CreateFrameworkElement(Completion completion, CompletionClassifierKind kind) {
			if (completion == null)
				throw new ArgumentNullException(nameof(completion));
			if (session.IsDismissed)
				return null;
			var completionSet = session.SelectedCompletionSet;
			if (completionSet == null)
				return null;
			Debug.Assert(completionSet.Completions.Contains(completion));
			const bool colorize = true;
			return completionTextElementProvider.Create(completionSet, completion, kind, colorize);
		}

		public string GetToolTip(FilterVM filterVM) {
			if (filterVM == null)
				throw new ArgumentNullException(nameof(filterVM));
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
