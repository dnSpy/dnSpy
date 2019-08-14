/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Formatting;
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
		CommandTargetStatus ExecuteSearchControl(Guid group, int cmdId, object? args, ref object? result);
		IEnumerable<SnapshotSpan> GetSpans(NormalizedSnapshotSpanCollection spans);
		void RegisterTextMarkerListener(ITextMarkerListener listener);
	}

	interface ITextMarkerListener {
		void RaiseTagsChanged(SnapshotSpan span);
	}

	sealed class SearchService : ViewModelBase, ISearchService {
#pragma warning disable CS0169
		[Export(typeof(AdornmentLayerDefinition))]
		[Name(PredefinedDsAdornmentLayers.Search)]
		[LayerKind(LayerKind.Overlay)]
		static AdornmentLayerDefinition? searchServiceAdornmentLayerDefinition;
#pragma warning restore CS0169

		const int MAX_SEARCH_RESULTS = 5000;

		enum SearchKind {
			None,
			Find,
			Replace,
			IncrementalSearchForward,
			IncrementalSearchBackward,
		}

		enum SearchControlPosition {
			TopRight,
			BottomRight,

			Default = TopRight,
		}

		public bool FoundMatch => foundSomething;

		public string SearchString {
			get => searchString;
			set => SetSearchString(value);
		}
		string searchString;

		void SetSearchString(string newSearchString, bool canSearch = true) {
			bool restartSearch = false;
			bool updateMarkers = false;
			if (searchString != newSearchString) {
				searchString = newSearchString ?? string.Empty;
				SaveSettings();
				OnPropertyChanged(nameof(SearchString));
				restartSearch = true;
				updateMarkers = true;
			}
			if (updateMarkers)
				UpdateTextMarkerSearch();
			if (restartSearch && canSearch)
				RestartSearch();
		}

		public string ToggleReplaceModeToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Search_ToggleReplaceModeToolTip, null);
		public string FindPreviousToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Search_FindPreviousToolTip, dnSpy_Resources.ShortCutKeyShiftF3);
		public string FindNextToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Search_FindNextToolTip, dnSpy_Resources.ShortCutKeyF3);
		public string CloseToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Search_CloseToolTip, dnSpy_Resources.ShortCutKeyEsc);
		public string ReplaceNextToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Search_ReplaceNextToolTip, dnSpy_Resources.ShortCutKeyAltR);
		public string ReplaceAllToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Search_ReplaceAllToolTip, dnSpy_Resources.ShortCutKeyAltA);
		public string MatchCaseToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Search_MatchCaseToolTip, dnSpy_Resources.ShortCutKeyAltC);
		public string MatchWholeWordToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Search_MatchWholeWordToolTip, dnSpy_Resources.ShortCutKeyAltW);
		public string UseRegularExpressionsToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Search_UseRegularExpressionsToolTip, dnSpy_Resources.ShortCutKeyAltE);

		public ICommand CloseSearchUICommand => new RelayCommand(a => CloseSearchControl());
		public ICommand FindNextCommand => new RelayCommand(a => FindNext(true));
		public ICommand FindPreviousCommand => new RelayCommand(a => FindNext(false));
		public ICommand ReplaceNextCommand => new RelayCommand(a => ReplaceNext(), a => CanReplaceNext);
		public ICommand ReplaceAllCommand => new RelayCommand(a => ReplaceAll(), a => CanReplaceAll);
		public ICommand ToggleFindReplaceCommand => new RelayCommand(a => ToggleFindReplace(), a => CanToggleFindReplace);

		public string ReplaceString {
			get => replaceString;
			set {
				if (replaceString != value) {
					replaceString = value ?? string.Empty;
					SaveSettings();
					OnPropertyChanged(nameof(ReplaceString));
				}
			}
		}
		string replaceString;

		public bool MatchCase {
			get => matchCase;
			set {
				if (matchCase != value) {
					matchCase = value;
					SaveSettings();
					OnPropertyChanged(nameof(MatchCase));
					RestartSearchAndUpdateMarkers();
				}
			}
		}
		bool matchCase;

		public bool MatchWholeWords {
			get => matchWholeWords;
			set {
				if (matchWholeWords != value) {
					matchWholeWords = value;
					SaveSettings();
					OnPropertyChanged(nameof(MatchWholeWords));
					RestartSearchAndUpdateMarkers();
				}
			}
		}
		bool matchWholeWords;

		public bool UseRegularExpressions {
			get => useRegularExpressions;
			set {
				if (useRegularExpressions != value) {
					useRegularExpressions = value;
					SaveSettings();
					OnPropertyChanged(nameof(UseRegularExpressions));
					RestartSearchAndUpdateMarkers();
				}
			}
		}
		bool useRegularExpressions;

		readonly IWpfTextView wpfTextView;
		readonly IEditorOperations editorOperations;
		readonly ITextSearchService2 textSearchService2;
		readonly ISearchSettings searchSettings;
		readonly IMessageBoxService messageBoxService;
		readonly ITextStructureNavigator textStructureNavigator;
		readonly Lazy<IReplaceListenerProvider>[] replaceListenerProviders;
		readonly List<ITextMarkerListener> listeners;
		SearchControl? searchControl;
		SearchControlPosition searchControlPosition;
		IAdornmentLayer? layer;
		NormalizedSnapshotSpanCollection? findResultCollection;
		IReplaceListener[]? replaceListeners;

		public SearchService(IWpfTextView wpfTextView, ITextSearchService2 textSearchService2, ISearchSettings searchSettings, IMessageBoxService messageBoxService, ITextStructureNavigator textStructureNavigator, Lazy<IReplaceListenerProvider>[] replaceListenerProviders, IEditorOperationsFactoryService editorOperationsFactoryService) {
			if (editorOperationsFactoryService is null)
				throw new ArgumentNullException(nameof(editorOperationsFactoryService));
			this.wpfTextView = wpfTextView ?? throw new ArgumentNullException(nameof(wpfTextView));
			editorOperations = editorOperationsFactoryService.GetEditorOperations(wpfTextView);
			this.textSearchService2 = textSearchService2 ?? throw new ArgumentNullException(nameof(textSearchService2));
			this.searchSettings = searchSettings ?? throw new ArgumentNullException(nameof(searchSettings));
			this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
			this.textStructureNavigator = textStructureNavigator ?? throw new ArgumentNullException(nameof(textStructureNavigator));
			this.replaceListenerProviders = replaceListenerProviders ?? throw new ArgumentNullException(nameof(replaceListenerProviders));
			listeners = new List<ITextMarkerListener>();
			searchString = string.Empty;
			replaceString = string.Empty;
			searchKind = SearchKind.None;
			searchControlPosition = SearchControlPosition.Default;
			wpfTextView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (s, e) => ShowFind()));
			wpfTextView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, (s, e) => ShowReplace()));
			wpfTextView.Closed += WpfTextView_Closed;
			UseGlobalSettings(true);
		}

		public CommandTargetStatus CanExecuteSearchControl(Guid group, int cmdId) {
			if (wpfTextView.IsClosed)
				return CommandTargetStatus.NotHandled;
			if (!IsSearchControlVisible)
				return CommandTargetStatus.NotHandled;
			Debug2.Assert(!(searchControl is null));

			if (inIncrementalSearch) {
				if (group == CommandConstants.TextEditorGroup) {
					switch ((TextEditorIds)cmdId) {
					case TextEditorIds.BACKSPACE:
					case TextEditorIds.TYPECHAR:
					case TextEditorIds.TAB:
					case TextEditorIds.RETURN:
					case TextEditorIds.SCROLLUP:
					case TextEditorIds.SCROLLDN:
					case TextEditorIds.SCROLLPAGEUP:
					case TextEditorIds.SCROLLPAGEDN:
					case TextEditorIds.SCROLLLEFT:
					case TextEditorIds.SCROLLRIGHT:
					case TextEditorIds.SCROLLBOTTOM:
					case TextEditorIds.SCROLLCENTER:
					case TextEditorIds.SCROLLTOP:
						return CommandTargetStatus.Handled;
					}
				}
			}
			if (group == CommandConstants.TextEditorGroup && cmdId == (int)TextEditorIds.CANCEL)
				return CommandTargetStatus.Handled;

			if (!searchControl.IsKeyboardFocusWithin)
				return CommandTargetStatus.NotHandled;
			// Make sure the WPF controls work as expected by ignoring all other text editor commands
			return CommandTargetStatus.LetWpfHandleCommand;
		}

		public CommandTargetStatus ExecuteSearchControl(Guid group, int cmdId, object? args, ref object? result) {
			if (wpfTextView.IsClosed)
				return CommandTargetStatus.NotHandled;
			if (!IsSearchControlVisible)
				return CommandTargetStatus.NotHandled;
			Debug2.Assert(!(searchControl is null));

			if (group == CommandConstants.TextEditorGroup && cmdId == (int)TextEditorIds.CANCEL) {
				if (inIncrementalSearch)
					wpfTextView.Selection.Clear();
				CloseSearchControl();
				return CommandTargetStatus.Handled;
			}

			if (inIncrementalSearch) {
				if (group == CommandConstants.TextEditorGroup) {
					switch ((TextEditorIds)cmdId) {
					case TextEditorIds.BACKSPACE:
						if (SearchString.Length != 0)
							SetIncrementalSearchString(SearchString.Substring(0, SearchString.Length - 1));
						return CommandTargetStatus.Handled;

					case TextEditorIds.TYPECHAR:
						var s = args as string;
						if (!(s is null) && s.IndexOfAny(LineConstants.newLineChars) < 0)
							SetIncrementalSearchString(SearchString + s);
						else
							CancelIncrementalSearch();
						return CommandTargetStatus.Handled;

					case TextEditorIds.TAB:
						SetIncrementalSearchString(SearchString + "\t");
						return CommandTargetStatus.Handled;

					case TextEditorIds.RETURN:
						CancelIncrementalSearch();
						return CommandTargetStatus.Handled;

					case TextEditorIds.SCROLLUP:
					case TextEditorIds.SCROLLDN:
					case TextEditorIds.SCROLLPAGEUP:
					case TextEditorIds.SCROLLPAGEDN:
					case TextEditorIds.SCROLLLEFT:
					case TextEditorIds.SCROLLRIGHT:
					case TextEditorIds.SCROLLBOTTOM:
					case TextEditorIds.SCROLLCENTER:
					case TextEditorIds.SCROLLTOP:
						// Allow scrolling by pressing eg. Ctrl+Up
						return CommandTargetStatus.NotHandled;
					}
				}
				else if (group == CommandConstants.TextReferenceGroup && (cmdId == (int)TextReferenceIds.FollowReference || cmdId == (int)TextReferenceIds.MoveToNextReference)) {
					// HACK: This search service shouldn't know about these commands but there's no way for
					// the text ref command handler to know that we're in incremental search mode either.
					CancelIncrementalSearch();
					return CommandTargetStatus.Handled;
				}
				else if (group == CommandConstants.StandardGroup) {
					switch ((StandardIds)cmdId) {
					case StandardIds.IncrementalSearchForward:
					case StandardIds.IncrementalSearchBackward:
						// Make sure that our other handler (with less priority) handles these commands
						return CommandTargetStatus.NotHandled;
					}
				}
				CancelIncrementalSearch();
			}

			if (!searchControl.IsKeyboardFocusWithin)
				return CommandTargetStatus.NotHandled;
			return CommandTargetStatus.LetWpfHandleCommand;
		}

		void SetIncrementalSearchString(string newSearchString) {
			isIncrementalSearchCaretMove = true;
			try {
				SetSearchString(newSearchString, false);
				RestartSearch();
			}
			finally {
				isIncrementalSearchCaretMove = false;
			}
		}
		bool isIncrementalSearchCaretMove;

		bool IsSearchControlVisible => !(layer is null) && !layer.IsEmpty;

		void UseGlobalSettingsIfUiIsHidden(bool canOverwriteSearchString) {
			if (!IsSearchControlVisible)
				UseGlobalSettings(canOverwriteSearchString);
		}

		void UseGlobalSettings(bool canOverwriteSearchString) {
			if (!disableSaveSettings) {
				disableSaveSettings = true;
				if (canOverwriteSearchString)
					SearchString = searchSettings.SearchString;
				ReplaceString = searchSettings.ReplaceString;
				MatchCase = searchSettings.MatchCase;
				MatchWholeWords = searchSettings.MatchWholeWords;
				UseRegularExpressions = searchSettings.UseRegularExpressions;
				disableSaveSettings = false;
			}
		}

		bool disableSaveSettings;
		void SaveSettings() {
			if (!disableSaveSettings)
				searchSettings.SaveSettings(SearchString, ReplaceString, MatchCase, MatchWholeWords, UseRegularExpressions);
		}

		void ShowSearchControl(SearchKind searchKind, bool canOverwriteSearchString) {
			UseGlobalSettingsIfUiIsHidden(canOverwriteSearchString);
			bool wasShown = IsSearchControlVisible;
			if (searchControl is null) {
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
				searchControl.SizeChanged += SearchControl_SizeChanged;
				searchControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			}
			if (layer is null)
				layer = wpfTextView.GetAdornmentLayer(PredefinedDsAdornmentLayers.Search);
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

		public bool HasSearchControlFocus => !(searchControl is null) && searchControl.IsKeyboardFocusWithin;
		void SearchControl_LostKeyboardFocus(object? sender, KeyboardFocusChangedEventArgs e) => OnPropertyChanged(nameof(HasSearchControlFocus));
		void SearchControl_GotKeyboardFocus(object? sender, KeyboardFocusChangedEventArgs e) {
			CloseSearchControlIfIncrementalSearch();
			OnPropertyChanged(nameof(HasSearchControlFocus));
		}

		void SearchControl_MouseDown(object? sender, MouseButtonEventArgs e) => CloseSearchControlIfIncrementalSearch();
		void CloseSearchControlIfIncrementalSearch() {
			if (wpfTextView.IsClosed)
				return;
			if (inIncrementalSearch)
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
			OnPropertyChanged(nameof(CanReplace));
		}
		SearchKind searchKind;
		bool inIncrementalSearch;

		void CancelIncrementalSearch() {
			if (!inIncrementalSearch)
				return;
			CloseSearchControl();
		}

		void CleanUpIncrementalSearch() {
			if (!inIncrementalSearch)
				return;
			wpfTextView.Caret.PositionChanged -= Caret_PositionChanged;
			inIncrementalSearch = false;
			incrementalStartPosition = null;
		}

		void Caret_PositionChanged(object? sender, CaretPositionChangedEventArgs e) {
			if (!isIncrementalSearchCaretMove)
				CancelIncrementalSearch();
		}

		void FocusSearchStringTextBox() {
			Debug2.Assert(!(searchControl is null));
			Action? callback = null;
			// If it hasn't been loaded yet, it has no binding and we must select it in its Loaded event
			if (searchControl.searchStringTextBox.Text.Length == 0 && SearchString.Length != 0)
				callback = () => searchControl.searchStringTextBox.SelectAll();
			else
				searchControl.searchStringTextBox.SelectAll();
			UIUtilities.Focus(searchControl.searchStringTextBox, callback);
		}

		void FocusReplaceStringTextBox() {
			Debug2.Assert(!(searchControl is null));
			Action? callback = null;
			// If it hasn't been loaded yet, it has no binding and we must select it in its Loaded event
			if (searchControl.replaceStringTextBox.Text.Length == 0 && ReplaceString.Length != 0)
				callback = () => searchControl.replaceStringTextBox.SelectAll();
			else
				searchControl.replaceStringTextBox.SelectAll();
			UIUtilities.Focus(searchControl.replaceStringTextBox, callback);
		}

		void RepositionControl(bool recalcSize = false) {
			Debug2.Assert(!(searchControl is null));
			if (recalcSize)
				searchControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			PositionSearchControl(SearchControlPosition.Default);
		}

		Rect TopRightRect => new Rect(wpfTextView.ViewportWidth - searchControl!.DesiredSize.Width, 0, searchControl.DesiredSize.Width, searchControl.DesiredSize.Height);
		Rect BottomRightRect => new Rect(wpfTextView.ViewportWidth - searchControl!.DesiredSize.Width, wpfTextView.ViewportHeight - searchControl.DesiredSize.Height, searchControl.DesiredSize.Width, searchControl.DesiredSize.Height);

		void PositionSearchControl(Rect rect) => PositionSearchControl(rect.Left, rect.Top);
		void PositionSearchControl(double left, double top) {
			if (Canvas.GetLeft(searchControl) == left && Canvas.GetTop(searchControl) == top)
				return;
			Canvas.SetLeft(searchControl, left);
			Canvas.SetTop(searchControl, top);
		}

		void PositionWithoutCoveringSpan(SnapshotSpan span) =>
			PositionSearchControl(GetsearchControlPosition(span));

		void PositionSearchControl(SearchControlPosition position) {
			switch (position) {
			case SearchControlPosition.TopRight:
				searchControlPosition = position;
				PositionSearchControl(TopRightRect);
				break;

			case SearchControlPosition.BottomRight:
				searchControlPosition = position;
				PositionSearchControl(BottomRightRect);
				break;

			default:
				throw new InvalidOperationException();
			}
		}

		void SearchControl_SizeChanged(object? sender, SizeChangedEventArgs e) =>
			PositionSearchControl(searchControlPosition);

		sealed class PositionInfo {
			public SearchControlPosition Position { get; }
			public Rect Rect { get; }
			public bool IntersectsSpan { get; set; }
			public PositionInfo(SearchControlPosition position, Rect rect) {
				Position = position;
				Rect = rect;
				IntersectsSpan = false;
			}
		}

		SearchControlPosition GetsearchControlPosition(SnapshotSpan span) {
			if (!IsSearchControlVisible)
				return SearchControlPosition.Default;

			var infos = new PositionInfo[] {
				// Sorted on preferred priority
				new PositionInfo(SearchControlPosition.TopRight, TopRightRect),
				new PositionInfo(SearchControlPosition.BottomRight, BottomRightRect),
			};
			Debug.Assert(infos.Length != 0 && infos[0].Position == SearchControlPosition.Default);

			foreach (var line in wpfTextView.TextViewLines.GetTextViewLinesIntersectingSpan(span)) {
				foreach (var info in infos) {
					if (Intersects(span, line, info.Rect))
						info.IntersectsSpan = true;
				}
			}
			var info2 = infos.FirstOrDefault(a => !a.IntersectsSpan) ?? infos.First(a => a.Position == SearchControlPosition.Default);
			return info2.Position;
		}

		bool Intersects(SnapshotSpan fullSpan, ITextViewLine line, Rect rect) {
			var span = fullSpan.Intersection(line.ExtentIncludingLineBreak);
			if (span is null || span.Value.Length == 0)
				return false;
			var start = line.GetExtendedCharacterBounds(span.Value.Start);
			var end = line.GetExtendedCharacterBounds(span.Value.End - 1);
			double left = Math.Min(start.Left, end.Left) - wpfTextView.ViewportLeft;
			double top = Math.Min(start.Top, end.Top) - wpfTextView.ViewportTop;
			double right = Math.Max(start.Right, end.Right) - wpfTextView.ViewportLeft;
			double bottom = Math.Max(start.Bottom, end.Bottom) - wpfTextView.ViewportTop;
			bool b = left <= right && top <= bottom;
			Debug.Assert(b);
			if (!b)
				return false;
			var r = new Rect(left, top, right - left, bottom - top);
			return r.IntersectsWith(rect);
		}

		void CloseSearchControl() {
			if (layer is null || layer.IsEmpty) {
				Debug.Assert(searchKind == SearchKind.None && searchControlPosition == SearchControlPosition.Default);
				return;
			}
			CleanUpIncrementalSearch();
			layer.RemoveAllAdornments();
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
			findResultCollection = null;
			RefreshAllTags();
			wpfTextView.VisualElement.Focus();
			searchKind = SearchKind.None;
			searchControlPosition = SearchControlPosition.Default;
			SaveSettings();
		}
		SnapshotPoint? incrementalStartPosition;

		string? TryGetSearchStringAtPoint(VirtualSnapshotPoint point) {
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

		string? TryGetSearchStringFromSelection() {
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

		string? TryGetSearchStringAtCaret() {
			string? s;
			if (!wpfTextView.Selection.IsEmpty)
				s = TryGetSearchStringFromSelection();
			else
				s = TryGetSearchStringAtPoint(wpfTextView.Caret.Position.VirtualBufferPosition);
			if (string2.IsNullOrEmpty(s) || s.IndexOfAny(LineConstants.newLineChars) >= 0)
				return null;
			return s;
		}

		void UpdateSearchStringFromCaretPosition(bool canSearch) {
			var newSearchString = TryGetSearchStringAtCaret();
			if (!(newSearchString is null))
				SetSearchString(newSearchString, canSearch);
		}

		public void ShowFind() {
			if (IsSearchControlVisible && searchControl!.IsKeyboardFocusWithin) {
				SetSearchKind(SearchKind.Find);
				FocusSearchStringTextBox();
				return;
			}

			UpdateSearchStringFromCaretPosition(canSearch: false);
			ShowSearchControl(SearchKind.Find, canOverwriteSearchString: false);
			FocusSearchStringTextBox();
		}

		public void ShowReplace() {
			if (IsSearchControlVisible && searchControl!.IsKeyboardFocusWithin) {
				SetSearchKind(SearchKind.Replace);
				FocusSearchStringTextBox();
				return;
			}

			UpdateSearchStringFromCaretPosition(canSearch: false);
			ShowSearchControl(SearchKind.Replace, canOverwriteSearchString: false);
			FocusSearchStringTextBox();
		}

		public void ShowIncrementalSearch(bool forward) {
			var searchKind = forward ? SearchKind.IncrementalSearchForward : SearchKind.IncrementalSearchBackward;
			if (IsSearchControlVisible && inIncrementalSearch && !wpfTextView.Selection.IsEmpty) {
				var options = GetFindOptions(searchKind, forward);
				var startingPosition = GetNextSearchPosition(wpfTextView.Selection.StreamSelectionSpan.SnapshotSpan, forward);
				incrementalStartPosition = startingPosition;
				ShowSearchControl(searchKind, canOverwriteSearchString: false);

				isIncrementalSearchCaretMove = true;
				try {
					FindNextCore(options, startingPosition);
				}
				finally {
					isIncrementalSearchCaretMove = false;
				}
				return;
			}

			SearchString = string.Empty;
			wpfTextView.VisualElement.Focus();
			incrementalStartPosition = wpfTextView.Caret.Position.BufferPosition;
			ShowSearchControl(searchKind, canOverwriteSearchString: false);
		}

		SnapshotPoint GetNextSearchPosition(SnapshotSpan span, bool forward) {
			var snapshot = span.Snapshot;
			if (forward) {
				if (span.Start.Position == snapshot.Length)
					return new SnapshotPoint(snapshot, 0);
				return span.Start + 1;
			}
			else {
				if (span.End.Position == 0)
					return new SnapshotPoint(snapshot, snapshot.Length);
				return span.End - 1;
			}
		}

		public bool CanReplace => IsReplaceMode && !wpfTextView.Options.DoesViewProhibitUserInput();
		bool CanReplaceNext => CanReplace && SearchString.Length > 0;
		void ReplaceNext() {
			if (!CanReplaceNext)
				return;

			var res = ReplaceFindNextCore(out var expandedReplacePattern);
			if (res is null)
				return;
			Debug2.Assert(!(expandedReplacePattern is null));

			var vres = new VirtualSnapshotSpan(res.Value);
			if (!wpfTextView.Selection.IsEmpty && wpfTextView.Selection.StreamSelectionSpan == vres) {
				if (CanReplaceSpan(res.Value, expandedReplacePattern)) {
					using (var ed = wpfTextView.TextBuffer.CreateEdit()) {
						if (ed.Replace(res.Value.Span, expandedReplacePattern))
							ed.Apply();
					}
				}
				wpfTextView.Selection.Clear();
				var newPos = res.Value.End.TranslateTo(wpfTextView.TextSnapshot, PointTrackingMode.Positive);
				wpfTextView.Caret.MoveTo(newPos);

				res = ReplaceFindNextCore(out expandedReplacePattern);
				if (res is null)
					return;
				Debug2.Assert(!(expandedReplacePattern is null));
				ShowSearchResult(res.Value);
			}
			else
				ShowSearchResult(res.Value);
		}

		static string Unescape(string s, FindOptions options) {
			if ((options & FindOptions.UseRegularExpressions) == 0)
				return s;
			if (s.IndexOf('\\') < 0)
				return s;
			var sb = new StringBuilder(s.Length);
			for (int i = 0; i < s.Length; i++) {
				var c = s[i];
				if (c == '\\' && i + 1 < s.Length) {
					i++;
					c = s[i];
					switch (c) {
					case 't': sb.Append('\t'); break;
					case 'n': sb.Append('\n'); break;
					case 'r': sb.Append('\r'); break;
					default:
						sb.Append('\\');
						sb.Append(c);
						break;
					}
				}
				else
					sb.Append(c);
			}
			return sb.ToString();
		}

		SnapshotSpan? ReplaceFindNextCore(out string? expandedReplacePattern) {
			if (SearchString.Length == 0) {
				expandedReplacePattern = null;
				return null;
			}
			var snapshot = wpfTextView.TextSnapshot;
			var options = GetFindOptions(SearchKind.Replace, true);
			var startingPosition = GetStartingPosition(SearchKind.Replace, options, restart: true);
			if (startingPosition is null) {
				expandedReplacePattern = null;
				return null;
			}
			startingPosition = startingPosition.Value.TranslateTo(snapshot, PointTrackingMode.Negative);
			try {
				return textSearchService2.FindForReplace(startingPosition.Value, SearchString, Unescape(ReplaceString, options), options, out expandedReplacePattern);
			}
			catch (ArgumentException) when ((options & FindOptions.UseRegularExpressions) != 0) {
				// Invalid regex string
				expandedReplacePattern = null;
				return null;
			}
		}

		bool CanReplaceSpan(SnapshotSpan span, string newText) {
			if (replaceListeners is null) {
				var list = new List<IReplaceListener>(replaceListenerProviders.Length);
				foreach (var provider in replaceListenerProviders) {
					var listener = provider.Value.Create(wpfTextView);
					if (!(listener is null))
						list.Add(listener);
				}
				replaceListeners = list.Count == 0 ? Array.Empty<IReplaceListener>() : list.ToArray();
			}
			foreach (var listener in replaceListeners) {
				if (!listener.CanReplace(span, newText))
					return false;
			}
			return true;
		}

		bool CanReplaceAll => CanReplace && SearchString.Length > 0;
		void ReplaceAll() {
			if (!CanReplaceAll)
				return;

			var oldSnapshot = wpfTextView.TextSnapshot;
			try {
				using (var ed = wpfTextView.TextBuffer.CreateEdit()) {
					foreach (var res in GetAllResultsForReplaceAll()) {
						if (CanReplaceSpan(res.span, res.expandedReplacePattern)) {
							// Ignore errors due to read-only regions
							ed.Replace(res.span.Span, res.expandedReplacePattern);
						}
					}
					ed.Apply();
					if (ed.Canceled)
						return;
				}
			}
			catch (OutOfMemoryException) {
				messageBoxService.Show("Out of memory");
				return;
			}
			if (oldSnapshot != wpfTextView.TextSnapshot)
				wpfTextView.Selection.Clear();
			wpfTextView.Caret.EnsureVisible();
		}

		// Finds all results but makes sure that all replacements never overlap another one,
		// eg. if SearchString is aaa and text is aaaaaaaa, it returns two results, starting
		// at offsets 0 and 3. The last two aa's aren't touched. Normal FindNext finds matches
		// at offsets 0, 1, 2, 3, 4, 5.
		IEnumerable<(SnapshotSpan span, string expandedReplacePattern)> GetAllResultsForReplaceAll() {
			var snapshot = wpfTextView.TextSnapshot;
			var options = GetFindOptions(SearchKind.Replace, true) & ~FindOptions.Wrap;
			var startingPosition = new SnapshotPoint(snapshot, 0);
			var searchString = SearchString;
			var replaceString = Unescape(ReplaceString, options);
			for (;;) {
				string? expandedReplacePattern;
				SnapshotSpan? res;
				try {
					res = textSearchService2.FindForReplace(startingPosition, searchString, replaceString, options, out expandedReplacePattern);
				}
				catch (ArgumentException) when ((options & FindOptions.UseRegularExpressions) != 0) {
					// Invalid regex string
					res = null;
					expandedReplacePattern = null;
				}
				if (res is null)
					break;
				Debug2.Assert(!(expandedReplacePattern is null));
				yield return (res.Value, expandedReplacePattern);
				if (startingPosition.Position == snapshot.Length)
					break;
				if (res.Value.Length != 0)
					startingPosition = res.Value.End;
				else
					startingPosition = res.Value.End + 1;
			}
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
				if (forward is null)
					forward = searchKind == SearchKind.IncrementalSearchForward;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(searchKind));
			}
			if (forward == false)
				options |= FindOptions.SearchReverse;
			if ((options & FindOptions.UseRegularExpressions) != 0) {
				if (IsMultiLineRegexPattern(SearchString))
					options |= FindOptions.Multiline;
				else
					options |= FindOptions.SingleLine;
			}
			options |= FindOptions.Wrap | FindOptions.OrdinalComparison;
			return options;
		}

		static bool IsMultiLineRegexPattern(string s) => s.Contains(@"\r") || s.Contains(@"\n") || s.Contains("$");

		SnapshotPoint? GetStartingPosition(SearchKind searchKind, FindOptions options, bool restart) {
			Debug.Assert(searchKind != SearchKind.None);
			switch (searchKind) {
			case SearchKind.Find:
			case SearchKind.Replace:
				return GetStartingPosition(options, restart);

			case SearchKind.IncrementalSearchBackward:
			case SearchKind.IncrementalSearchForward:
				return incrementalStartPosition;

			default:
				throw new ArgumentOutOfRangeException(nameof(searchKind));
			}
		}

		SnapshotPoint? GetStartingPosition(FindOptions options, bool restart) {
			if (wpfTextView.Selection.IsEmpty)
				return wpfTextView.Caret.Position.BufferPosition;
			if (restart) {
				if ((options & FindOptions.SearchReverse) == 0)
					return wpfTextView.Selection.Start.Position;
				return wpfTextView.Selection.End.Position;
			}
			if ((options & FindOptions.SearchReverse) != 0) {
				if (wpfTextView.Selection.End.Position.Position > 0)
					return wpfTextView.Selection.End.Position - 1;
				if ((options & FindOptions.Wrap) != 0)
					return new SnapshotPoint(wpfTextView.TextSnapshot, wpfTextView.TextSnapshot.Length);
				return null;
			}
			if (wpfTextView.Selection.Start.Position.Position != wpfTextView.Selection.Start.Position.Snapshot.Length)
				return wpfTextView.Selection.Start.Position + 1;
			if ((options & FindOptions.Wrap) != 0)
				return new SnapshotPoint(wpfTextView.TextSnapshot, 0);
			return null;
		}

		public void FindNext(bool forward) {
			UseGlobalSettingsIfUiIsHidden(true);
			var options = GetFindOptions(SearchKind.Find, forward);
			var startingPosition = GetStartingPosition(SearchKind.Find, options, restart: false);
			FindNextCore(options, startingPosition);
		}

		void FindNextCore(FindOptions options, SnapshotPoint? startingPosition) {
			if (startingPosition is null)
				return;
			var res = FindNextResultCore(options, startingPosition.Value);
			if (res is null)
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
			editorOperations.SelectAndMoveCaret(new VirtualSnapshotPoint(span.Start), new VirtualSnapshotPoint(span.End));
			if (IsSearchControlVisible)
				PositionWithoutCoveringSpan(span);
		}

		void SetFoundResult(bool found) {
			if (foundSomething == found)
				return;
			foundSomething = found;
			OnPropertyChanged(nameof(FoundMatch));
			OnPropertyChanged(nameof(SearchString));
		}
		bool foundSomething;
		public void FindNextSelected(bool forward) => FindNextSelectedCore(forward, false);

		void FindNextSelectedCore(bool forward, bool restart) {
			var newSearchString = TryGetSearchStringAtCaret();
			if (newSearchString is null)
				return;

			ShowSearchControl(SearchKind.Find, canOverwriteSearchString: false);
			// Don't focus the search control. Whoever has focus (most likely text editor)
			// should keep the focus.

			// This search doesn't use the options from the search control
			var options = FindOptions.Wrap | FindOptions.MatchCase | FindOptions.OrdinalComparison;
			if (!forward)
				options |= FindOptions.SearchReverse;
			var startingPosition = GetStartingPosition(SearchKind.Find, options, restart);
			SetSearchString(newSearchString, canSearch: false);
			FindNextCore(options, startingPosition);
		}

		public IEnumerable<SnapshotSpan> GetSpans(NormalizedSnapshotSpanCollection spans) {
			if (!IsSearchControlVisible)
				yield break;
			if (findResultCollection is null)
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
					if (resSpan.Start > snapshotSpan.End)
						break;
					yield return resSpan;
				}
			}
		}

		int GetFindResultStartIndex(int position) {
			if (findResultCollection is null)
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
				else {
					if (index > 0 && array[index - 1].End == position)
						return index - 1;
					return index;
				}
			}
			if ((uint)hi < (uint)array.Count && array[hi].End == position)
				return hi;
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
				if (!(oldColl is null) && oldColl.Count != 0)
					RefreshAllTags();
				SetFoundResult(true);
				return;
			}

			var snapshot = wpfTextView.TextSnapshot;
			var searchRange = new SnapshotSpan(snapshot, 0, snapshot.Length);
			var options = GetFindOptions(searchKind, true);
			int count = 0;
			try {
				var list = new List<SnapshotSpan>();
				foreach (var res in textSearchService2.FindAll(searchRange, SearchString, options)) {
					if (res.Length != 0)
						list.Add(res);
					count++;
					if (count == MAX_SEARCH_RESULTS)
						break;
				}
				findResultCollection = new NormalizedSnapshotSpanCollection(list);
			}
			catch (ArgumentException) when ((options & FindOptions.UseRegularExpressions) != 0) {
				// Invalid regex string
				findResultCollection = NormalizedSnapshotSpanCollection.Empty;
				count = 0;
			}
			RefreshAllTags();
			SetFoundResult(count != 0);
		}

		void RefreshAllTags() {
			var snapshot = wpfTextView.TextSnapshot;
			var span = new SnapshotSpan(snapshot, 0, snapshot.Length);
			foreach (var listener in listeners)
				listener.RaiseTagsChanged(span);
		}

		void WpfTextView_LayoutChanged(object? sender, TextViewLayoutChangedEventArgs e) {
			Debug.Assert(IsSearchControlVisible);
			if (!IsSearchControlVisible)
				return;
			if (e.OldViewState.ViewportWidth != e.NewViewState.ViewportWidth)
				RepositionControl(true);
			else if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				RepositionControl(true);
			if (e.OldSnapshot != e.NewSnapshot) {
				CancelIncrementalSearch();
				UpdateTextMarkerSearch();
			}
		}

		void WpfTextView_Closed(object? sender, EventArgs e) {
			CloseSearchControl();
			wpfTextView.Closed -= WpfTextView_Closed;
			wpfTextView.LayoutChanged -= WpfTextView_LayoutChanged;
		}
	}
}
