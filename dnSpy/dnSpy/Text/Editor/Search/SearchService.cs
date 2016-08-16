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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor.Search {
	interface ISearchService {
		void ShowFind();
		void ShowReplace();
		void ShowIncrementalSearch(bool forward);
		void FindNext(bool forward);
		void FindNextSelected(bool forward);
		CommandTargetStatus CanExecuteSearchControl(Guid group, int cmdId);
		CommandTargetStatus ExecuteSearchControl(Guid group, int cmdId, object args, ref object result);
		IEnumerable<SnapshotSpan> GetSpans(NormalizedSnapshotSpanCollection spans);
		void RegisterTextMarkerListener(ITextMarkerListener listener);
	}

	interface ITextMarkerListener {
		void RaiseTagsChanged(SnapshotSpan span);
	}

	sealed class SearchService : ViewModelBase, ISearchService {
#pragma warning disable 0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedDnSpyAdornmentLayers.Search)]
		[LayerKind(LayerKind.Overlay)]
		static AdornmentLayerDefinition searchServiceAdornmentLayerDefinition;
#pragma warning restore 0169

		const int MAX_SEARCH_RESULTS = 5000;

		enum SearchKind {
			None,
			Find,
			Replace,
			IncrementalSearchForward,
			IncrementalSearchBackward,
		}

		public string SearchString {
			get { return searchString; }
			set { SetSearchString(value); }
		}
		string searchString;

		void SetSearchString(string newSearchString, bool canSearch = true) {
			bool restartSearch = false;
			bool updateMarkers = false;
			if (searchString != newSearchString) {
				searchString = newSearchString ?? string.Empty;
				OnPropertyChanged(nameof(SearchString));
				restartSearch = true;
				updateMarkers = true;
			}
			if (updateMarkers)
				UpdateTextMarkerSearch();
			if (restartSearch && canSearch)
				RestartSearch();
		}

		public ICommand CloseSearchUICommand => new RelayCommand(a => CloseSearchControl());
		public ICommand FindNextCommand => new RelayCommand(a => FindNext(true));
		public ICommand FindPreviousCommand => new RelayCommand(a => FindNext(false));
		public ICommand ReplaceNextCommand => new RelayCommand(a => ReplaceNext(), a => CanReplaceNext);
		public ICommand ReplaceAllCommand => new RelayCommand(a => ReplaceAll(), a => CanReplaceAll);
		public ICommand ToggleFindReplaceCommand => new RelayCommand(a => ToggleFindReplace(), a => CanToggleFindReplace);

		public string ReplaceString {
			get { return replaceString; }
			set {
				if (replaceString != value) {
					replaceString = value ?? string.Empty;
					OnPropertyChanged(nameof(ReplaceString));
				}
			}
		}
		string replaceString;

		public bool MatchCase {
			get { return matchCase; }
			set {
				if (matchCase != value) {
					matchCase = value;
					OnPropertyChanged(nameof(MatchCase));
					RestartSearchAndUpdateMarkers();
				}
			}
		}
		bool matchCase;

		public bool MatchWholeWords {
			get { return matchWholeWords; }
			set {
				if (matchWholeWords != value) {
					matchWholeWords = value;
					OnPropertyChanged(nameof(MatchWholeWords));
					RestartSearchAndUpdateMarkers();
				}
			}
		}
		bool matchWholeWords;

		public bool UseRegularExpressions {
			get { return useRegularExpressions; }
			set {
				if (useRegularExpressions != value) {
					useRegularExpressions = value;
					OnPropertyChanged(nameof(UseRegularExpressions));
					RestartSearchAndUpdateMarkers();
				}
			}
		}
		bool useRegularExpressions;

		readonly IWpfTextView wpfTextView;
		readonly ITextSearchService2 textSearchService2;
		readonly ITextStructureNavigator textStructureNavigator;
		readonly List<ITextMarkerListener> listeners;
		SearchControl searchControl;
		IAdornmentLayer layer;
		NormalizedSnapshotSpanCollection findResultCollection;

		public SearchService(IWpfTextView wpfTextView, ITextSearchService2 textSearchService2, ITextStructureNavigator textStructureNavigator) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			if (textSearchService2 == null)
				throw new ArgumentNullException(nameof(textSearchService2));
			if (textStructureNavigator == null)
				throw new ArgumentNullException(nameof(textStructureNavigator));
			this.wpfTextView = wpfTextView;
			this.textSearchService2 = textSearchService2;
			this.textStructureNavigator = textStructureNavigator;
			this.listeners = new List<ITextMarkerListener>();
			this.searchString = string.Empty;
			this.replaceString = string.Empty;
			this.searchKind = SearchKind.None;
			wpfTextView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (s, e) => ShowFind()));
			wpfTextView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, (s, e) => ShowReplace()));
			wpfTextView.Closed += WpfTextView_Closed;
		}

		public CommandTargetStatus CanExecuteSearchControl(Guid group, int cmdId) {
			if (wpfTextView.IsClosed)
				return CommandTargetStatus.NotHandled;
			if (!IsSearchControlVisible)
				return CommandTargetStatus.NotHandled;

			if (searchKind == SearchKind.IncrementalSearchForward || searchKind == SearchKind.IncrementalSearchBackward) {
				if (group == CommandConstants.TextEditorGroup) {
					switch ((TextEditorIds)cmdId) {
					case TextEditorIds.BACKSPACE:
					case TextEditorIds.TYPECHAR:
						return CommandTargetStatus.Handled;
					}
				}
			}
			if (group == CommandConstants.TextEditorGroup && cmdId == (int)TextEditorIds.CANCEL)
				return CommandTargetStatus.Handled;

			if (!searchControl.IsKeyboardFocusWithin)
				return CommandTargetStatus.NotHandled;
			// Make sure the WPF controls work as expected by ignoring all other text editor commands
			return CommandTargetStatus.NotHandledDontCallNextHandler;
		}

		public CommandTargetStatus ExecuteSearchControl(Guid group, int cmdId, object args, ref object result) {
			if (wpfTextView.IsClosed)
				return CommandTargetStatus.NotHandled;
			if (!IsSearchControlVisible)
				return CommandTargetStatus.NotHandled;

			if (searchKind == SearchKind.IncrementalSearchForward || searchKind == SearchKind.IncrementalSearchBackward) {
				if (group == CommandConstants.TextEditorGroup) {
					switch ((TextEditorIds)cmdId) {
					case TextEditorIds.BACKSPACE:
						if (SearchString.Length != 0) {
							SearchString = SearchString.Substring(0, SearchString.Length - 1);
							RestartSearch();
						}
						return CommandTargetStatus.Handled;

					case TextEditorIds.TYPECHAR:
						var s = args as string;
						if (s != null) {
							SearchString += s;
							RestartSearch();
						}
						return CommandTargetStatus.Handled;

					default:
						CancelIncrementalSearch();
						break;
					}
				}
			}
			if (group == CommandConstants.TextEditorGroup && cmdId == (int)TextEditorIds.CANCEL) {
				CloseSearchControl();
				return CommandTargetStatus.Handled;
			}

			if (!searchControl.IsKeyboardFocusWithin)
				return CommandTargetStatus.NotHandled;
			return CommandTargetStatus.NotHandledDontCallNextHandler;
		}

		bool IsSearchControlVisible => layer != null && !layer.IsEmpty;

		void ShowSearchControl(SearchKind searchKind) {
			bool wasShown = IsSearchControlVisible;
			if (searchControl == null) {
				searchControl = new SearchControl { DataContext = this };
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => CloseSearchControl()), new KeyGesture(Key.Escape, ModifierKeys.None)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => ShowFind()), new KeyGesture(Key.F, ModifierKeys.Control)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => ShowReplace()), new KeyGesture(Key.H, ModifierKeys.Control)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => FindNext(true)), new KeyGesture(Key.F, ModifierKeys.Alt)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => FindNext(true)), new KeyGesture(Key.Enter, ModifierKeys.None)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => FindNext(false)), new KeyGesture(Key.Enter, ModifierKeys.Shift)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => FindNext(true)), new KeyGesture(Key.F3, ModifierKeys.None)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => FindNext(false)), new KeyGesture(Key.F3, ModifierKeys.Shift)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => FindNextSelected(true)), new KeyGesture(Key.F3, ModifierKeys.Control)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => FindNextSelected(false)), new KeyGesture(Key.F3, ModifierKeys.Control | ModifierKeys.Shift)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => FocusSearchStringTextBox()), new KeyGesture(Key.N, ModifierKeys.Alt)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => FocusReplaceStringTextBox(), a => IsReplaceMode), new KeyGesture(Key.P, ModifierKeys.Alt)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => MatchCase = !MatchCase), new KeyGesture(Key.C, ModifierKeys.Alt)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => MatchWholeWords = !MatchWholeWords), new KeyGesture(Key.W, ModifierKeys.Alt)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => UseRegularExpressions = !UseRegularExpressions), new KeyGesture(Key.E, ModifierKeys.Alt)));
				searchControl.InputBindings.Add(new KeyBinding(ReplaceNextCommand, new KeyGesture(Key.R, ModifierKeys.Alt)));
				searchControl.InputBindings.Add(new KeyBinding(ReplaceAllCommand, new KeyGesture(Key.A, ModifierKeys.Alt)));
				searchControl.AddHandler(UIElement.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(SearchControl_GotKeyboardFocus), true);
				searchControl.AddHandler(UIElement.LostKeyboardFocusEvent, new KeyboardFocusChangedEventHandler(SearchControl_LostKeyboardFocus), true);
				searchControl.AddHandler(UIElement.MouseDownEvent, new MouseButtonEventHandler(SearchControl_MouseDown), true);
				SelectAllWhenFocused(searchControl.searchStringTextBox);
				SelectAllWhenFocused(searchControl.replaceStringTextBox);
			}
			if (layer == null)
				layer = wpfTextView.GetAdornmentLayer(PredefinedDnSpyAdornmentLayers.Search);
			if (layer.IsEmpty) {
				layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled, null, null, searchControl, null);
				wpfTextView.LayoutChanged += WpfTextView_LayoutChanged;
			}

			SetSearchKind(searchKind);
			RepositionControl();

			if (!wasShown)
				UpdateTextMarkerSearch();
		}

		static void SelectAllWhenFocused(TextBox textBox) =>
			textBox.GotKeyboardFocus += (s, e) => textBox.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => textBox.SelectAll()));

		public bool HasSearchControlFocus => searchControl != null && searchControl.IsKeyboardFocusWithin;
		void SearchControl_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => OnPropertyChanged(nameof(HasSearchControlFocus));
		void SearchControl_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			CloseSearchControlIfIncrementalSearch();
			OnPropertyChanged(nameof(HasSearchControlFocus));
		}

		void SearchControl_MouseDown(object sender, MouseButtonEventArgs e) => CloseSearchControlIfIncrementalSearch();
		void CloseSearchControlIfIncrementalSearch() {
			if (wpfTextView.IsClosed)
				return;
			if (searchKind == SearchKind.IncrementalSearchForward || searchKind == SearchKind.IncrementalSearchBackward)
				CloseSearchControl();
		}

		public bool ShowReplaceRow => searchKind == SearchKind.Replace;
		public bool ShowOptionsRow => searchKind == SearchKind.Find || searchKind == SearchKind.Replace;
		public bool IsReplaceMode => searchKind == SearchKind.Replace;
		void SetSearchKind(SearchKind value) {
			Debug.Assert(value != SearchKind.None);
			if (IsSearchControlVisible && searchKind == value)
				return;
			searchKind = value;
			if (searchKind == SearchKind.IncrementalSearchForward || searchKind == SearchKind.IncrementalSearchBackward) {
				wpfTextView.VisualElement.Focus();
				if (!inIncrementalSearch)
					wpfTextView.Caret.PositionChanged += Caret_PositionChanged;
				inIncrementalSearch = true;
			}
			else
				CleanUpIncrementalSearch();
			OnPropertyChanged(nameof(ShowReplaceRow));
			OnPropertyChanged(nameof(ShowOptionsRow));
			OnPropertyChanged(nameof(IsReplaceMode));
		}
		SearchKind searchKind;
		bool inIncrementalSearch;

		void CancelIncrementalSearch() {
			if (!inIncrementalSearch)
				return;
			CleanUpIncrementalSearch();
			CloseSearchControl();
		}

		void CleanUpIncrementalSearch() {
			if (!inIncrementalSearch)
				return;
			wpfTextView.Caret.PositionChanged -= Caret_PositionChanged;
			inIncrementalSearch = false;
			incrementalStartPosition = null;
		}

		void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e) {
			if (!isOurCaretMove)
				CancelIncrementalSearch();
		}

		void FocusSearchStringTextBox() {
			Action action = null;
			// If it hasn't been loaded yet, it has no binding and we must select it in its Loaded event
			if (searchControl.searchStringTextBox.Text.Length == 0 && SearchString.Length != 0)
				action = () => searchControl.searchStringTextBox.SelectAll();
			else
				searchControl.searchStringTextBox.SelectAll();
			UIUtilities.Focus(searchControl.searchStringTextBox, action);
		}

		void FocusReplaceStringTextBox() {
			Action action = null;
			// If it hasn't been loaded yet, it has no binding and we must select it in its Loaded event
			if (searchControl.replaceStringTextBox.Text.Length == 0 && ReplaceString.Length != 0)
				action = () => searchControl.replaceStringTextBox.SelectAll();
			else
				searchControl.replaceStringTextBox.SelectAll();
			UIUtilities.Focus(searchControl.replaceStringTextBox, action);
		}

		void RepositionControl() {
			Debug.Assert(searchControl != null);
			searchControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			Canvas.SetLeft(searchControl, wpfTextView.ViewportWidth - searchControl.DesiredSize.Width);
		}

		void CloseSearchControl() {
			if (layer == null || layer.IsEmpty) {
				Debug.Assert(searchKind == SearchKind.None);
				return;
			}
			CleanUpIncrementalSearch();
			layer.RemoveAllAdornments();
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
			findResultCollection = null;
			RefreshAllTags();
			wpfTextView.VisualElement.Focus();
			searchKind = SearchKind.None;
		}
		SnapshotPoint? incrementalStartPosition;

		string TryGetSearchStringAtPoint(VirtualSnapshotPoint point) {
			if (point.IsInVirtualSpace)
				return null;

			var extent = textStructureNavigator.GetExtentOfWord(point.Position);
			if (extent.IsSignificant)
				return extent.Span.GetText();

			var line = point.Position.GetContainingLine();
			if (line.Start == point.Position)
				return null;

			extent = textStructureNavigator.GetExtentOfWord(point.Position - 1);
			if (extent.IsSignificant)
				return extent.Span.GetText();

			return null;
		}

		string TryGetSearchStringFromSelection() {
			if (wpfTextView.Selection.IsEmpty)
				return null;
			if (wpfTextView.Selection.Start.IsInVirtualSpace)
				return null;

			var start = wpfTextView.Selection.Start.Position;
			var line = start.GetContainingLine();
			var end = wpfTextView.Selection.End.Position;
			if (end > line.EndIncludingLineBreak)
				return null;
			if (end > line.End)
				end = line.End;
			if (start >= end)
				return null;
			return new SnapshotSpan(start, end).GetText();
		}

		string TryGetSearchStringAtCaret() {
			string s;
			if (!wpfTextView.Selection.IsEmpty)
				s = TryGetSearchStringFromSelection();
			else
				s = TryGetSearchStringAtPoint(wpfTextView.Caret.Position.VirtualBufferPosition);
			if (string.IsNullOrEmpty(s) || s.IndexOfAny(newLineChars) >= 0)
				return null;
			return s;
		}
		static readonly char[] newLineChars = new char[] { '\r', '\n', '\u0085', '\u2028', '\u2029' };

		void UpdateSearchStringFromCaretPosition() {
			var newSearchString = TryGetSearchStringAtCaret();
			if (newSearchString != null)
				SetSearchString(newSearchString);
		}

		public void ShowFind() {
			if (IsSearchControlVisible && searchControl.IsKeyboardFocusWithin) {
				SetSearchKind(SearchKind.Find);
				FocusSearchStringTextBox();
				return;
			}

			UpdateSearchStringFromCaretPosition();
			ShowSearchControl(SearchKind.Find);
			FocusSearchStringTextBox();
		}

		public void ShowReplace() {
			if (IsSearchControlVisible && searchControl.IsKeyboardFocusWithin) {
				SetSearchKind(SearchKind.Replace);
				FocusSearchStringTextBox();
				return;
			}

			UpdateSearchStringFromCaretPosition();
			ShowSearchControl(SearchKind.Replace);
			FocusSearchStringTextBox();
		}

		public void ShowIncrementalSearch(bool forward) {
			SearchString = string.Empty;
			wpfTextView.VisualElement.Focus();
			incrementalStartPosition = wpfTextView.Caret.Position.BufferPosition;
			ShowSearchControl(forward ? SearchKind.IncrementalSearchForward : SearchKind.IncrementalSearchBackward);
		}

		public bool CanReplace => !wpfTextView.Options.DoesViewProhibitUserInput();
		bool CanReplaceNext => CanReplace && SearchString.Length > 0;
		void ReplaceNext() {
			if (!CanReplaceNext)
				return;

			string expandedReplacePattern;
			var res = ReplaceFindNextCore(out expandedReplacePattern);
			if (res == null)
				return;

			var vres = new VirtualSnapshotSpan(res.Value);
			if (!wpfTextView.Selection.IsEmpty && wpfTextView.Selection.StreamSelectionSpan == vres) {
				using (var ed = wpfTextView.TextBuffer.CreateEdit()) {
					if (!ed.Replace(res.Value.Span, expandedReplacePattern))
						return;
					ed.Apply();
					if (ed.Canceled)
						return;
				}
				wpfTextView.Selection.Clear();
				var newPos = res.Value.End.TranslateTo(wpfTextView.TextSnapshot, PointTrackingMode.Positive);
				wpfTextView.Caret.MoveTo(newPos);

				res = ReplaceFindNextCore(out expandedReplacePattern);
				if (res == null)
					return;
				ShowSearchResult(res.Value);
			}
			else
				ShowSearchResult(res.Value);
		}

		SnapshotSpan? ReplaceFindNextCore(out string expandedReplacePattern) {
			if (SearchString.Length == 0) {
				expandedReplacePattern = null;
				return null;
			}
			var snapshot = wpfTextView.TextSnapshot;
			var options = GetFindOptions(SearchKind.Replace, true);
			var startingPosition = GetStartingPosition(SearchKind.Replace, options, restart: true);
			startingPosition = startingPosition.TranslateTo(snapshot, PointTrackingMode.Negative);
			try {
				return textSearchService2.FindForReplace(startingPosition, SearchString, ReplaceString, options, out expandedReplacePattern);
			}
			catch (ArgumentException) when ((options & FindOptions.UseRegularExpressions) != 0) {
				// Invalid regex string
				expandedReplacePattern = null;
				return null;
			}
		}

		bool CanReplaceAll => CanReplace && SearchString.Length > 0;
		void ReplaceAll() {
			if (!CanReplaceAll)
				return;

			var snapshot = wpfTextView.TextSnapshot;
			var options = GetFindOptions(SearchKind.Replace, true);
			var searchRange = new SnapshotSpan(snapshot, 0, snapshot.Length);
			Tuple<SnapshotSpan, string>[] result;
			try {
				result = textSearchService2.FindAllForReplace(searchRange, SearchString, ReplaceString, options).ToArray();
			}
			catch (ArgumentException) when ((options & FindOptions.UseRegularExpressions) != 0) {
				// Invalid regex string
				return;
			}
			using (var ed = wpfTextView.TextBuffer.CreateEdit()) {
				foreach (var res in result) {
					// Ignore errors due to read-only regions
					ed.Replace(res.Item1.Span, res.Item2);
				}
				ed.Apply();
				if (ed.Canceled)
					return;
			}
			wpfTextView.Selection.Clear();
			wpfTextView.Caret.EnsureVisible();
		}

		bool CanToggleFindReplace => true;
		void ToggleFindReplace() {
			if (searchKind != SearchKind.Replace)
				SetSearchKind(SearchKind.Replace);
			else
				SetSearchKind(SearchKind.Find);
		}

		FindOptions GetFindOptions(SearchKind searchKind, bool? forward) {
			Debug.Assert(searchKind != SearchKind.None);
			var options = FindOptions.None;
			switch (searchKind) {
			case SearchKind.Find:
			case SearchKind.Replace:
				if (MatchCase)
					options |= FindOptions.MatchCase;
				if (UseRegularExpressions)
					options |= FindOptions.UseRegularExpressions;
				if (MatchWholeWords)
					options |= FindOptions.WholeWord;
				break;

			case SearchKind.IncrementalSearchBackward:
			case SearchKind.IncrementalSearchForward:
				if (SearchString.Any(c => char.IsUpper(c)))
					options |= FindOptions.MatchCase;
				if (forward == null)
					forward = searchKind == SearchKind.IncrementalSearchForward;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(searchKind));
			}
			if (forward == false)
				options |= FindOptions.SearchReverse;
			options |= FindOptions.Wrap;
			return options;
		}

		SnapshotPoint GetStartingPosition(SearchKind searchKind, FindOptions options, bool restart) {
			Debug.Assert(searchKind != SearchKind.None);
			switch (searchKind) {
			case SearchKind.Find:
			case SearchKind.Replace:
				return GetStartingPosition(options, restart);

			case SearchKind.IncrementalSearchBackward:
			case SearchKind.IncrementalSearchForward:
				Debug.Assert(incrementalStartPosition != null);
				if (incrementalStartPosition == null)
					return GetStartingPosition(options, restart);
				return incrementalStartPosition.Value;

			default:
				throw new ArgumentOutOfRangeException(nameof(searchKind));
			}
		}

		SnapshotPoint GetStartingPosition(FindOptions options, bool restart) {
			if (wpfTextView.Selection.IsEmpty)
				return wpfTextView.Caret.Position.BufferPosition;
			if (restart) {
				if ((options & FindOptions.SearchReverse) == 0)
					return wpfTextView.Selection.Start.Position;
				return wpfTextView.Selection.End.Position;
			}
			if ((options & FindOptions.SearchReverse) != 0)
				return wpfTextView.Selection.Start.Position;
			return wpfTextView.Selection.End.Position;
		}

		public void FindNext(bool forward) {
			var options = GetFindOptions(SearchKind.Find, forward);
			var startingPosition = GetStartingPosition(SearchKind.Find, options, restart: false);
			FindNextCore(options, startingPosition);
		}

		void FindNextCore(FindOptions options, SnapshotPoint startingPosition) {
			var res = FindNextResultCore(options, startingPosition);
			if (res == null)
				return;
			ShowSearchResult(res.Value);
		}

		SnapshotSpan? FindNextResultCore(FindOptions options, SnapshotPoint startingPosition) {
			if (SearchString.Length == 0)
				return null;
			var snapshot = wpfTextView.TextSnapshot;
			startingPosition = startingPosition.TranslateTo(snapshot, PointTrackingMode.Negative);
			var searchRange = new SnapshotSpan(snapshot, 0, snapshot.Length);
			try {
				return textSearchService2.Find(searchRange, startingPosition, SearchString, options);
			}
			catch (ArgumentException) when ((options & FindOptions.UseRegularExpressions) != 0) {
				// Invalid regex string
				return null;
			}
		}

		void ShowSearchResult(SnapshotSpan span) {
			try {
				isOurCaretMove = true;
				//TODO: Use editorOperations.SelectAndMoveCaret(new VirtualSnapshotPoint(res.Value.Start), new VirtualSnapshotPoint(res.Value.End));
				wpfTextView.Selection.Mode = TextSelectionMode.Stream;
				wpfTextView.Selection.Select(new VirtualSnapshotPoint(span.Start), new VirtualSnapshotPoint(span.End));
				wpfTextView.Caret.MoveTo(span.End);
				wpfTextView.Caret.EnsureVisible();
			}
			finally {
				isOurCaretMove = false;
			}
		}
		bool isOurCaretMove;

		void SetFoundResult(bool found) {
			if (foundSomething == found)
				return;
			foundSomething = found;
			HasErrorUpdated();
			OnPropertyChanged(nameof(SearchString));
		}
		bool foundSomething;

		protected override string Verify(string columnName) {
			if (columnName == nameof(SearchString))
				return foundSomething ? null : dnSpy_Resources.Search_NothingFound;
			return null;
		}

		public override bool HasError => !string.IsNullOrEmpty(Verify(nameof(SearchString)));

		public void FindNextSelected(bool forward) => FindNextSelectedCore(forward, false);

		void FindNextSelectedCore(bool forward, bool restart) {
			var newSearchString = TryGetSearchStringAtCaret();
			if (newSearchString == null)
				return;

			ShowSearchControl(SearchKind.Find);
			// Don't focus the search control. Whoever has focus (most likely text editor)
			// should keep the focus.

			// This search doesn't use the options from the search control
			var options = FindOptions.Wrap | FindOptions.MatchCase;
			if (!forward)
				options |= FindOptions.SearchReverse;
			var startingPosition = GetStartingPosition(SearchKind.Find, options, restart);
			SetSearchString(newSearchString, canSearch: false);
			FindNextCore(options, startingPosition);
		}

		public IEnumerable<SnapshotSpan> GetSpans(NormalizedSnapshotSpanCollection spans) {
			if (!IsSearchControlVisible)
				yield break;
			if (findResultCollection == null)
				yield break;
			if (findResultCollection.Count == 0)
				yield break;
			if (spans.Count == 0)
				yield break;
			// If they're not identical, we'll soon invalidate all spans so just ignore this one for now
			if (findResultCollection[0].Snapshot != spans[0].Snapshot)
				yield break;

			foreach (var snapshotSpan in spans) {
				int index = GetFindResultStartIndex(snapshotSpan.Span.Start);
				if (index < 0)
					continue;
				for (int i = index; i < findResultCollection.Count; i++) {
					var resSpan = findResultCollection[i];
					if (resSpan.Start >= snapshotSpan.End)
						break;
					yield return resSpan;
				}
			}
		}

		int GetFindResultStartIndex(int position) {
			if (findResultCollection == null)
				return -1;
			var array = findResultCollection;
			int lo = 0, hi = array.Count - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				var span = array[index];
				if (position < span.Span.Start)
					hi = index - 1;
				else if (position >= span.Span.End)
					lo = index + 1;
				else
					return index;
			}
			return lo < array.Count ? lo : -1;
		}

		public void RegisterTextMarkerListener(ITextMarkerListener listener) => listeners.Add(listener);

		void RestartSearchAndUpdateMarkers() {
			if (!IsSearchControlVisible)
				return;

			UpdateTextMarkerSearch();
			RestartSearch();
		}

		void RestartSearch() {
			if (!IsSearchControlVisible)
				return;
			if (SearchString.Length == 0)
				return;

			var options = GetFindOptions(searchKind, null);
			var startingPosition = GetStartingPosition(searchKind, options, restart: true);
			FindNextCore(options, startingPosition);
		}

		void UpdateTextMarkerSearch() {
			if (!IsSearchControlVisible)
				return;

			if (SearchString.Length == 0) {
				var oldColl = findResultCollection;
				findResultCollection = NormalizedSnapshotSpanCollection.Empty;
				if (oldColl != null && oldColl.Count != 0)
					RefreshAllTags();
				SetFoundResult(true);
				return;
			}

			var snapshot = wpfTextView.TextSnapshot;
			var searchRange = new SnapshotSpan(snapshot, 0, snapshot.Length);
			var options = GetFindOptions(searchKind, true);
			try {
				findResultCollection = new NormalizedSnapshotSpanCollection(textSearchService2.FindAll(searchRange, SearchString, options).Take(MAX_SEARCH_RESULTS));
			}
			catch (ArgumentException) when ((options & FindOptions.UseRegularExpressions) != 0) {
				// Invalid regex string
				findResultCollection = NormalizedSnapshotSpanCollection.Empty;
			}
			RefreshAllTags();
			SetFoundResult(findResultCollection.Count != 0);
		}

		void RefreshAllTags() {
			var snapshot = wpfTextView.TextSnapshot;
			var span = new SnapshotSpan(snapshot, 0, snapshot.Length);
			foreach (var listener in listeners)
				listener.RaiseTagsChanged(span);
		}

		void WpfTextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
			Debug.Assert(IsSearchControlVisible);
			if (!IsSearchControlVisible)
				return;
			if (e.OldViewState.ViewportWidth != e.NewViewState.ViewportWidth)
				RepositionControl();
			else if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				RepositionControl();
			if (e.OldSnapshot != e.NewSnapshot) {
				CancelIncrementalSearch();
				UpdateTextMarkerSearch();
			}
		}

		void WpfTextView_Closed(object sender, EventArgs e) {
			CloseSearchControl();
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
		}
	}
}
