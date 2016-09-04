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
using System.Windows.Input;
using System.Windows.Media;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionPresenter : ICompletionPresenter, INotifyPropertyChanged {
		UIElement IPopupContent.UIElement => control;

		readonly IImageManager imageManager;
		readonly ICompletionSession session;
		readonly ICompletionTextElementProvider completionTextElementProvider;
		readonly CompletionPresenterControl control;
		readonly List<FilterVM> filters;
		readonly IWpfTextView wpfTextView;

		public object Filters => filters;
		public bool HasFilters => filters.Count > 0;
		public event PropertyChangedEventHandler PropertyChanged;

		const double defaultMaxHeight = 200;

		public CompletionPresenter(IImageManager imageManager, ICompletionSession session, ICompletionTextElementProvider completionTextElementProvider) {
			if (imageManager == null)
				throw new ArgumentNullException(nameof(imageManager));
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			if (completionTextElementProvider == null)
				throw new ArgumentNullException(nameof(completionTextElementProvider));
			this.imageManager = imageManager;
			this.session = session;
			this.completionTextElementProvider = completionTextElementProvider;
			this.control = new CompletionPresenterControl { DataContext = this };
			this.filters = new List<FilterVM>();
			this.control.completionsListBox.MaxHeight = defaultMaxHeight;
			session.SelectedCompletionCollectionChanged += CompletionSession_SelectedCompletionCollectionChanged;
			session.Dismissed += CompletionSession_Dismissed;
			session.TextView.LostAggregateFocus += TextView_LostAggregateFocus;
			session.TextView.TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
			this.wpfTextView = session.TextView as IWpfTextView;
			Debug.Assert(wpfTextView != null);
			if (wpfTextView != null)
				wpfTextView.VisualElement.PreviewKeyDown += VisualElement_PreviewKeyDown;
			control.completionsListBox.SelectionChanged += CompletionsListBox_SelectionChanged;
			control.SizeChanged += Control_SizeChanged;
			UpdateSelectedCompletion();
			UpdateFilterCollection();
		}

		void Control_SizeChanged(object sender, SizeChangedEventArgs e) {
			// Prevent the control from getting thinner when pressing PageUp/Down
			if (control.MinWidth != e.NewSize.Width)
				control.MinWidth = e.NewSize.Width;
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
			coll.CurrentCompletion = new CurrentCompletion(newCompletion, true, true);
		}

		bool ICompletionPresenter.HandleCommand(PresenterCommandTargetCommand command) {
			switch (command) {
			case PresenterCommandTargetCommand.Up:
				MoveUpDown(true);
				return true;

			case PresenterCommandTargetCommand.Down:
				MoveUpDown(false);
				return true;

			case PresenterCommandTargetCommand.PageUp:
				PageUpDown(true);
				return true;

			case PresenterCommandTargetCommand.PageDown:
				PageUpDown(false);
				return true;

			case PresenterCommandTargetCommand.Home:
			case PresenterCommandTargetCommand.End:
			case PresenterCommandTargetCommand.TopLine:
			case PresenterCommandTargetCommand.BottomLine:
			case PresenterCommandTargetCommand.Escape:
			case PresenterCommandTargetCommand.Enter:
			case PresenterCommandTargetCommand.IncreaseFilterLevel:
			case PresenterCommandTargetCommand.DecreaseFilterLevel:
			default:
				return false;
			}
		}

		void MoveUpDown(bool up) => MoveUpDown(up ? -1 : 1);

		void PageUpDown(bool up) {
			const int defaultValue = 9;
			var items = WpfUtils.GetItemsPerPage(control.completionsListBox, defaultValue);
			MoveUpDown(up ? -items : items);
		}

		void MoveUpDown(int count) {
			var coll = session.SelectedCompletionCollection.FilteredCollection;
			int index = coll.IndexOf(session.SelectedCompletionCollection.CurrentCompletion.Completion);
			if (index < 0)
				index = 0;
			index = index + count;
			if (index < 0)
				index = 0;
			else if (index >= coll.Count)
				index = coll.Count - 1;
			var newItem = (uint)index >= (uint)coll.Count ? null : coll[index];
			control.completionsListBox.SelectedItem = newItem;
			session.SelectedCompletionCollection.CurrentCompletion = new CurrentCompletion(newItem, true, true);
			ScrollSelectedItemIntoView();
		}

		void ScrollSelectedItemIntoView() => WpfUtils.ScrollSelectedItemIntoView(control.completionsListBox);
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

		void UpdateSelectedItem() {
			if (currentCompletionCollection == null)
				control.completionsListBox.SelectedItem = null;
			else {
				control.completionsListBox.SelectedItem = currentCompletionCollection.CurrentCompletion.Completion;
				ScrollSelectedItemIntoView();
			}
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
			session.SelectedCompletionCollectionChanged -= CompletionSession_SelectedCompletionCollectionChanged;
			session.Dismissed -= CompletionSession_Dismissed;
			session.TextView.LostAggregateFocus -= TextView_LostAggregateFocus;
			session.TextView.TextBuffer.ChangedLowPriority -= TextBuffer_ChangedLowPriority;
			control.completionsListBox.SelectionChanged -= CompletionsListBox_SelectionChanged;
			if (wpfTextView != null)
				wpfTextView.VisualElement.PreviewKeyDown -= VisualElement_PreviewKeyDown;
			completionTextElementProvider.Dispose();
		}

		public ImageSource GetImageSource(Completion completion) => GetImageSource(completion.Image);
		public ImageSource GetImageSource(FilterVM filterVM) => GetImageSource(filterVM.Image);
		ImageSource GetImageSource(ImageReference imageReference) {
			if (session.IsDismissed)
				return null;
			if (imageReference.Assembly == null)
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
	}
}
