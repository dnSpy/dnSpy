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
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Language.Intellisense {
	sealed class CompletionPresenter : ICompletionPresenter {
		UIElement IPopupContent.UIElement => control;

		readonly IImageManager imageManager;
		readonly ICompletionSession session;
		readonly CompletionPresenterControl control;

		const double defaultMaxHeight = 200;

		public CompletionPresenter(IImageManager imageManager, ICompletionSession session) {
			if (imageManager == null)
				throw new ArgumentNullException(nameof(imageManager));
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			this.imageManager = imageManager;
			this.session = session;
			this.control = new CompletionPresenterControl { DataContext = this };
			this.control.completionsListBox.MaxHeight = defaultMaxHeight;
			session.SelectedCompletionCollectionChanged += CompletionSession_SelectedCompletionCollectionChanged;
			session.Dismissed += CompletionSession_Dismissed;
			session.TextView.LostAggregateFocus += TextView_LostAggregateFocus;
			session.TextView.TextBuffer.ChangedLowPriority += TextBuffer_ChangedLowPriority;
			control.completionsListBox.SelectionChanged += CompletionsListBox_SelectionChanged;
			UpdateSelectedCompletion();
		}

		void TextBuffer_ChangedLowPriority(object sender, TextContentChangedEventArgs e) {
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
		void CompletionSession_SelectedCompletionCollectionChanged(object sender, SelectedCompletionCollectionEventArgs e) =>
			UpdateSelectedCompletion();

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
			session.SelectedCompletionCollectionChanged -= CompletionSession_SelectedCompletionCollectionChanged;
			session.Dismissed -= CompletionSession_Dismissed;
			session.TextView.LostAggregateFocus -= TextView_LostAggregateFocus;
			session.TextView.TextBuffer.ChangedLowPriority -= TextBuffer_ChangedLowPriority;
			control.completionsListBox.SelectionChanged -= CompletionsListBox_SelectionChanged;
		}

		public ImageSource GetImageSource(Completion completion) {
			var image = completion.Image;
			if (image.Assembly == null)
				return null;
			return imageManager.GetImage(image, BackgroundType.ListBoxItem);
		}

		public object GetDisplayText(Completion completion) {
			return new TextBlock {
				Text = completion.DisplayText,
			};
		}
	}
}
