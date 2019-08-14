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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Command;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Contracts.Hex.Operations;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;
using dnSpy.Properties;
using CT = dnSpy.Contracts.Text;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor.Search {
	abstract class HexViewSearchService {
		public abstract void ShowFind();
		public abstract void ShowReplace();
		public abstract void ShowIncrementalSearch(bool forward);
		public abstract void FindNext(bool forward);
		public abstract void FindNextSelected(bool forward);
		public abstract CommandTargetStatus CanExecuteSearchControl(Guid group, int cmdId);
		public abstract CommandTargetStatus ExecuteSearchControl(Guid group, int cmdId, object? args, ref object? result);
		public abstract IEnumerable<HexBufferSpan> GetSpans(NormalizedHexBufferSpanCollection spans);
		public abstract void RegisterHexMarkerListener(IHexMarkerListener listener);
	}

	interface IHexMarkerListener {
		void RaiseTagsChanged(HexBufferSpan span);
	}

	sealed class DataKindVM : ViewModelBase {
		public HexDataKind DataKind { get; }
		public string DisplayName { get; }
		public string InputGestureText { get; }

		public DataKindVM(HexDataKind dataKind, string displayName, string? inputGestureText = null) {
			DataKind = dataKind;
			DisplayName = displayName;
			InputGestureText = inputGestureText is null ? string.Empty : "(" + inputGestureText + ")";
		}
	}

	sealed class HexViewSearchServiceImpl : HexViewSearchService, INotifyPropertyChanged {
		public event PropertyChangedEventHandler? PropertyChanged;
		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

#pragma warning disable CS0169
		[Export(typeof(HexAdornmentLayerDefinition))]
		[VSUTIL.Name(PredefinedHexAdornmentLayers.Search)]
		[HexLayerKind(HexLayerKind.Overlay)]
		static HexAdornmentLayerDefinition? searchServiceAdornmentLayerDefinition;
#pragma warning restore CS0169

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

		[Flags]
		enum OurFindOptions {
			None				= 0,
			SearchReverse		= 0x00000001,
			Wrap				= 0x00000002,
			NoOverlaps			= 0x40000000,
			MatchCase			= int.MinValue,
		}

		public bool FoundMatch {
			get => foundMatch;
			set {
				if (foundMatch != value) {
					foundMatch = value;
					OnPropertyChanged(nameof(FoundMatch));
				}
			}
		}
		bool foundMatch;

		public bool Searching {
			get => searching;
			set {
				if (searching != value) {
					searching = value;
					OnPropertyChanged(nameof(Searching));
				}
			}
		}
		bool searching;

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
				UpdateHexMarkerSearch();
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
		public string BigEndianToolTip => ToolTipHelper.AddKeyboardShortcut(dnSpy_Resources.Search_BigEndianToolTip, dnSpy_Resources.ShortCutKeyAltB);

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

		public bool IsBigEndian {
			get => isBigEndian;
			set {
				if (isBigEndian != value) {
					isBigEndian = value;
					SaveSettings();
					OnPropertyChanged(nameof(IsBigEndian));
					RestartSearchAndUpdateMarkers();
				}
			}
		}
		bool isBigEndian;

		public HexDataKind DataKind {
			get => selectedDataKindVM.DataKind;
			set => SelectedDataKindVM = dataKinds.First(a => a.DataKind == value);
		}

		public System.Collections.IList DataKinds => dataKinds;
		readonly ObservableCollection<DataKindVM> dataKinds;
		public object SelectedDataKindVM {
			get => selectedDataKindVM;
			set {
				if (selectedDataKindVM != value) {
					selectedDataKindVM = (DataKindVM)value;
					SaveSettings();
					OnPropertyChanged(nameof(SelectedDataKindVM));
					RestartSearchAndUpdateMarkers();
				}
			}
		}
		DataKindVM selectedDataKindVM;

		static readonly DataKindVM[] dataKindVMList = new DataKindVM[] {
			new DataKindVM(HexDataKind.Bytes, "Hex", dnSpy_Resources.ShortCutKeyAltH),
			new DataKindVM(HexDataKind.Utf8String, GetStringDataKind("UTF-8"), dnSpy_Resources.ShortCutKeyAlt8),
			new DataKindVM(HexDataKind.Utf16String, GetStringDataKind("Unicode"), dnSpy_Resources.ShortCutKeyAltU),
			new DataKindVM(HexDataKind.Byte, "Byte"),
			new DataKindVM(HexDataKind.SByte, "SByte"),
			new DataKindVM(HexDataKind.Int16, "Int16"),
			new DataKindVM(HexDataKind.UInt16, "UInt16"),
			new DataKindVM(HexDataKind.Int32, "Int32"),
			new DataKindVM(HexDataKind.UInt32, "UInt32"),
			new DataKindVM(HexDataKind.Int64, "Int64"),
			new DataKindVM(HexDataKind.UInt64, "UInt64"),
			new DataKindVM(HexDataKind.Single, "Single"),
			new DataKindVM(HexDataKind.Double, "Double"),
		};
		static string GetStringDataKind(string encodingName) => $"String ({encodingName})";

		readonly WpfHexView wpfHexView;
		readonly HexEditorOperations editorOperations;
		readonly HexSearchServiceFactory hexSearchServiceFactory;
		readonly SearchSettings searchSettings;
		readonly IMessageBoxService messageBoxService;
		readonly List<IHexMarkerListener> listeners;
		SearchControl? searchControl;
		SearchControlPosition searchControlPosition;
		HexAdornmentLayer? layer;

		public HexViewSearchServiceImpl(WpfHexView wpfHexView, HexSearchServiceFactory hexSearchServiceFactory, SearchSettings searchSettings, IMessageBoxService messageBoxService, HexEditorOperationsFactoryService editorOperationsFactoryService) {
			if (editorOperationsFactoryService is null)
				throw new ArgumentNullException(nameof(editorOperationsFactoryService));
			dataKinds = new ObservableCollection<DataKindVM>(dataKindVMList);
			selectedDataKindVM = dataKinds.First();
			this.wpfHexView = wpfHexView ?? throw new ArgumentNullException(nameof(wpfHexView));
			editorOperations = editorOperationsFactoryService.GetEditorOperations(wpfHexView);
			this.hexSearchServiceFactory = hexSearchServiceFactory ?? throw new ArgumentNullException(nameof(hexSearchServiceFactory));
			this.searchSettings = searchSettings ?? throw new ArgumentNullException(nameof(searchSettings));
			this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
			listeners = new List<IHexMarkerListener>();
			searchString = string.Empty;
			replaceString = string.Empty;
			searchKind = SearchKind.None;
			searchControlPosition = SearchControlPosition.Default;
			wpfHexView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, (s, e) => ShowFind()));
			wpfHexView.VisualElement.CommandBindings.Add(new CommandBinding(ApplicationCommands.Replace, (s, e) => ShowReplace()));
			wpfHexView.Closed += WpfHexView_Closed;
			UseGlobalSettings(true);
		}

		public override CommandTargetStatus CanExecuteSearchControl(Guid group, int cmdId) {
			if (wpfHexView.IsClosed)
				return CommandTargetStatus.NotHandled;
			if (!IsSearchControlVisible)
				return CommandTargetStatus.NotHandled;
			Debug2.Assert(!(searchControl is null));

			if (inIncrementalSearch) {
				if (group == CommandConstants.HexEditorGroup) {
					switch ((HexEditorIds)cmdId) {
					case HexEditorIds.BACKSPACE:
					case HexEditorIds.TYPECHAR:
					case HexEditorIds.TAB:
					case HexEditorIds.RETURN:
					case HexEditorIds.SCROLLUP:
					case HexEditorIds.SCROLLDN:
					case HexEditorIds.SCROLLPAGEUP:
					case HexEditorIds.SCROLLPAGEDN:
					case HexEditorIds.SCROLLLEFT:
					case HexEditorIds.SCROLLRIGHT:
					case HexEditorIds.SCROLLBOTTOM:
					case HexEditorIds.SCROLLCENTER:
					case HexEditorIds.SCROLLTOP:
						return CommandTargetStatus.Handled;
					}
				}
			}
			if (group == CommandConstants.HexEditorGroup && cmdId == (int)HexEditorIds.CANCEL)
				return CommandTargetStatus.Handled;

			if (!searchControl.IsKeyboardFocusWithin)
				return CommandTargetStatus.NotHandled;
			// Make sure the WPF controls work as expected by ignoring all other hex editor commands
			return CommandTargetStatus.LetWpfHandleCommand;
		}

		public override CommandTargetStatus ExecuteSearchControl(Guid group, int cmdId, object? args, ref object? result) {
			if (wpfHexView.IsClosed)
				return CommandTargetStatus.NotHandled;
			if (!IsSearchControlVisible)
				return CommandTargetStatus.NotHandled;
			Debug2.Assert(!(searchControl is null));

			if (group == CommandConstants.HexEditorGroup && cmdId == (int)HexEditorIds.CANCEL) {
				if (inIncrementalSearch)
					wpfHexView.Selection.Clear();
				CloseSearchControl();
				return CommandTargetStatus.Handled;
			}

			if (inIncrementalSearch) {
				if (group == CommandConstants.HexEditorGroup) {
					switch ((HexEditorIds)cmdId) {
					case HexEditorIds.BACKSPACE:
						if (SearchString.Length != 0)
							SetIncrementalSearchString(SearchString.Substring(0, SearchString.Length - 1));
						return CommandTargetStatus.Handled;

					case HexEditorIds.TYPECHAR:
						var s = args as string;
						if (!(s is null) && s.IndexOfAny(CT.LineConstants.newLineChars) < 0)
							SetIncrementalSearchString(SearchString + s);
						else
							CancelIncrementalSearch();
						return CommandTargetStatus.Handled;

					case HexEditorIds.TAB:
						SetIncrementalSearchString(SearchString + "\t");
						return CommandTargetStatus.Handled;

					case HexEditorIds.RETURN:
						CancelIncrementalSearch();
						return CommandTargetStatus.Handled;

					case HexEditorIds.SCROLLUP:
					case HexEditorIds.SCROLLDN:
					case HexEditorIds.SCROLLPAGEUP:
					case HexEditorIds.SCROLLPAGEDN:
					case HexEditorIds.SCROLLLEFT:
					case HexEditorIds.SCROLLRIGHT:
					case HexEditorIds.SCROLLBOTTOM:
					case HexEditorIds.SCROLLCENTER:
					case HexEditorIds.SCROLLTOP:
						// Allow scrolling by pressing eg. Ctrl+Up
						return CommandTargetStatus.NotHandled;
					}
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
				IsBigEndian = searchSettings.BigEndian;
				DataKind = searchSettings.DataKind;
				disableSaveSettings = false;
			}
		}

		bool disableSaveSettings;
		void SaveSettings() {
			if (!disableSaveSettings)
				searchSettings.SaveSettings(SearchString, ReplaceString, MatchCase, IsBigEndian, DataKind);
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
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => IsBigEndian = !IsBigEndian), new KeyGesture(Key.B, ModifierKeys.Alt)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => DataKind = HexDataKind.Bytes), new KeyGesture(Key.H, ModifierKeys.Alt)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => DataKind = HexDataKind.Utf16String), new KeyGesture(Key.U, ModifierKeys.Alt)));
				searchControl.InputBindings.Add(new KeyBinding(new RelayCommand(a => DataKind = HexDataKind.Utf8String), new KeyGesture(Key.D8, ModifierKeys.Alt)));
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
				layer = wpfHexView.GetAdornmentLayer(PredefinedHexAdornmentLayers.Search);
			if (layer.IsEmpty) {
				layer.AddAdornment(VSTE.AdornmentPositioningBehavior.OwnerControlled, (HexBufferSpan?)null, null, searchControl, null);
				wpfHexView.LayoutChanged += WpfHexView_LayoutChanged;
				wpfHexView.BufferLinesChanged += WpfHexView_BufferLinesChanged;
				wpfHexView.Buffer.BufferSpanInvalidated += Buffer_BufferSpanInvalidated;
			}

			SetSearchKind(searchKind);
			RepositionControl();

			if (!wasShown)
				UpdateHexMarkerSearch();
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
			if (wpfHexView.IsClosed)
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
				wpfHexView.VisualElement.Focus();
				if (!inIncrementalSearch)
					wpfHexView.Caret.PositionChanged += Caret_PositionChanged;
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
			wpfHexView.Caret.PositionChanged -= Caret_PositionChanged;
			inIncrementalSearch = false;
			incrementalStartPosition = null;
		}

		void Caret_PositionChanged(object? sender, HexCaretPositionChangedEventArgs e) {
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

		Rect TopRightRect => new Rect(wpfHexView.ViewportWidth - searchControl!.DesiredSize.Width, 0, searchControl.DesiredSize.Width, searchControl.DesiredSize.Height);
		Rect BottomRightRect => new Rect(wpfHexView.ViewportWidth - searchControl!.DesiredSize.Width, wpfHexView.ViewportHeight - searchControl.DesiredSize.Height, searchControl.DesiredSize.Width, searchControl.DesiredSize.Height);

		void PositionSearchControl(Rect rect) => PositionSearchControl(rect.Left, rect.Top);
		void PositionSearchControl(double left, double top) {
			if (Canvas.GetLeft(searchControl) == left && Canvas.GetTop(searchControl) == top)
				return;
			Canvas.SetLeft(searchControl, left);
			Canvas.SetTop(searchControl, top);
		}

		void PositionWithoutCoveringSpan(HexBufferSpan span) =>
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

		SearchControlPosition GetsearchControlPosition(HexBufferSpan span) {
			if (!IsSearchControlVisible)
				return SearchControlPosition.Default;

			var infos = new PositionInfo[] {
				// Sorted on preferred priority
				new PositionInfo(SearchControlPosition.TopRight, TopRightRect),
				new PositionInfo(SearchControlPosition.BottomRight, BottomRightRect),
			};
			Debug.Assert(infos.Length != 0 && infos[0].Position == SearchControlPosition.Default);

			foreach (var line in wpfHexView.HexViewLines.GetHexViewLinesIntersectingSpan(span)) {
				foreach (var info in infos) {
					if (Intersects(span, line, info.Rect))
						info.IntersectsSpan = true;
				}
			}
			var info2 = infos.FirstOrDefault(a => !a.IntersectsSpan) ?? infos.First(a => a.Position == SearchControlPosition.Default);
			return info2.Position;
		}

		bool Intersects(HexBufferSpan fullSpan, HexViewLine line, Rect rect) {
			var span = fullSpan.Intersection(line.BufferSpan);
			if (span is null || span.Value.Length == 0)
				return false;
			var allBounds = line.GetNormalizedTextBounds(span.Value, HexSpanSelectionFlags.Selection);
			if (allBounds.Count == 0)
				return false;
			double left = double.MaxValue, right = double.MinValue, top = double.MaxValue, bottom = double.MinValue;
			foreach (var bounds in allBounds) {
				left = Math.Min(left, bounds.Left);
				right = Math.Max(right, bounds.Right);
				top = Math.Min(top, bounds.TextTop);
				bottom = Math.Max(bottom, bounds.TextBottom);
			}
			left -= wpfHexView.ViewportLeft;
			top -= wpfHexView.ViewportTop;
			right -= wpfHexView.ViewportLeft;
			bottom -= wpfHexView.ViewportTop;
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
			CancelAsyncSearch();
			CleanUpIncrementalSearch();
			layer.RemoveAllAdornments();
			wpfHexView.LayoutChanged -= WpfHexView_LayoutChanged;
			wpfHexView.BufferLinesChanged -= WpfHexView_BufferLinesChanged;
			wpfHexView.Buffer.BufferSpanInvalidated -= Buffer_BufferSpanInvalidated;
			hexMarkerSearchService = null;
			RefreshAllTags();
			wpfHexView.VisualElement.Focus();
			searchKind = SearchKind.None;
			searchControlPosition = SearchControlPosition.Default;
			SaveSettings();
		}
		HexBufferPoint? incrementalStartPosition;

		string? TryGetSearchStringAtPoint(HexBufferPoint point) =>
			// The text editor can find the current word, but there's not much we can do
			// so return null.
			null;

		string? TryGetSearchStringFromSelection() {
			if (wpfHexView.Selection.IsEmpty)
				return null;

			// The TextBox doesn't allow too long strings
			const int MAX_BYTES = 1024;
			var span = wpfHexView.Selection.StreamSelectionSpan;
			var start = span.Start.Position;
			var end = HexPosition.Min(start + MAX_BYTES, span.End.Position);
			int byteCount = (int)(end - start).ToUInt64();
			var chars = new char[byteCount * 2];
			var buffer = span.Buffer;
			var pos = start;
			const bool upper = true;
			for (int i = 0, j = 0; i < byteCount; i++) {
				byte b = buffer.ReadByte(pos);
				chars[j++] = ToHexChar(b >> 4, upper);
				chars[j++] = ToHexChar(b & 0x0F, upper);
				pos++;
			}
			return new string(chars);
		}

		static char ToHexChar(int val, bool upper) {
			if (0 <= val && val <= 9)
				return (char)(val + (int)'0');
			return (char)(val - 10 + (upper ? (int)'A' : (int)'a'));
		}

		string? TryGetSearchStringAtCaret() {
			string? s;
			if (!wpfHexView.Selection.IsEmpty)
				s = TryGetSearchStringFromSelection();
			else
				s = TryGetSearchStringAtPoint(wpfHexView.Caret.Position.Position.ActivePosition.BufferPosition);
			if (string2.IsNullOrEmpty(s) || s.IndexOfAny(CT.LineConstants.newLineChars) >= 0)
				return null;
			return s;
		}

		void UpdateSearchStringFromCaretPosition(bool canSearch) {
			var newSearchString = TryGetSearchStringAtCaret();
			if (!(newSearchString is null))
				SetSearchString(newSearchString, canSearch);
		}

		public override void ShowFind() {
			if (IsSearchControlVisible && searchControl!.IsKeyboardFocusWithin) {
				SetSearchKind(SearchKind.Find);
				FocusSearchStringTextBox();
				return;
			}

			UpdateSearchStringFromCaretPosition(canSearch: false);
			ShowSearchControl(SearchKind.Find, canOverwriteSearchString: false);
			FocusSearchStringTextBox();
		}

		public override void ShowReplace() {
			if (IsSearchControlVisible && searchControl!.IsKeyboardFocusWithin) {
				SetSearchKind(SearchKind.Replace);
				FocusSearchStringTextBox();
				return;
			}

			UpdateSearchStringFromCaretPosition(canSearch: false);
			ShowSearchControl(SearchKind.Replace, canOverwriteSearchString: false);
			FocusSearchStringTextBox();
		}

		public override void ShowIncrementalSearch(bool forward) {
			var searchKind = forward ? SearchKind.IncrementalSearchForward : SearchKind.IncrementalSearchBackward;
			if (IsSearchControlVisible && inIncrementalSearch && !wpfHexView.Selection.IsEmpty) {
				var options = GetFindOptions(searchKind, forward);
				var startingPosition = GetNextSearchPosition(wpfHexView.Selection.StreamSelectionSpan, forward);
				incrementalStartPosition = startingPosition;
				ShowSearchControl(searchKind, canOverwriteSearchString: false);

				FindNextCore(options, startingPosition, isIncrementalSearch: true);
				return;
			}

			SearchString = string.Empty;
			wpfHexView.VisualElement.Focus();
			incrementalStartPosition = wpfHexView.Caret.Position.Position.ActivePosition.BufferPosition;
			ShowSearchControl(searchKind, canOverwriteSearchString: false);
		}

		HexBufferPoint GetNextSearchPosition(HexBufferSpan span, bool forward) {
			var validSpan = wpfHexView.BufferLines.BufferSpan;
			if (validSpan.IsEmpty)
				return validSpan.Start;
			if (forward) {
				if (span.Start >= validSpan.End)
					return validSpan.Start;
				return span.Start + 1;
			}
			else {
				var end = span.End == HexPosition.Zero ? span.End : span.End - 1;
				if (end <= validSpan.Start)
					return validSpan.End;
				return end - 1;
			}
		}

		bool IsReplaceStringValid() => !(DataParser.TryParseData(ReplaceString, DataKind, IsBigEndian) is null);

		byte[]? TryGetReplaceStringData(HexBufferSpan replaceSpan) {
			var data = DataParser.TryParseData(ReplaceString, DataKind, IsBigEndian);
			if (data is null)
				return null;
			if (data.LongLength == replaceSpan.Length)
				return data;
			var newData = new byte[replaceSpan.Length >= ulong.MaxValue ? ulong.MaxValue : replaceSpan.Length.ToUInt64()];
			Array.Copy(data, 0, newData, 0, Math.Min(data.LongLength, newData.LongLength));
			return newData;
		}

		public bool CanReplace => IsReplaceMode && !(wpfHexView.Buffer.IsReadOnly || wpfHexView.Options.DoesViewProhibitUserInput());
		bool CanReplaceNext => CanReplace &&
			hexSearchServiceFactory.IsSearchDataValid(DataKind, SearchString, (GetFindOptions(SearchKind.Replace, true) & OurFindOptions.MatchCase) != 0, IsBigEndian) &&
			IsReplaceStringValid();
		void ReplaceNext() {
			if (!CanReplaceNext)
				return;

			var res = ReplaceFindNextCore();
			if (res is null)
				return;

			var vres = res.Value;
			if (!wpfHexView.Selection.IsEmpty && wpfHexView.Selection.StreamSelectionSpan == vres) {
				try {
					var newData = TryGetReplaceStringData(res.Value);
					Debug2.Assert(!(newData is null) && newData.Length == res.Value.Length);
					if (newData is null || newData.Length != res.Value.Length)
						return;

					using (var ed = wpfHexView.Buffer.CreateEdit()) {
						if (ed.Replace(res.Value.Span.Start, newData))
							ed.Apply();
					}
				}
				catch (ArithmeticException) {
					messageBoxService.Show("Out of memory");
					return;
				}
				catch (OutOfMemoryException) {
					messageBoxService.Show("Out of memory");
					return;
				}
				wpfHexView.Selection.Clear();
				wpfHexView.Caret.MoveTo(res.Value.IsEmpty ? res.Value.Start : res.Value.End - 1);

				res = ReplaceFindNextCore();
				if (res is null)
					return;
				ShowSearchResult(res.Value);
			}
			else
				ShowSearchResult(res.Value);
		}

		HexBufferSpan? ReplaceFindNextCore() {
			if (SearchString.Length == 0)
				return null;
			var options = GetFindOptions(SearchKind.Replace, true);
			var hexSearchService = hexSearchServiceFactory.TryCreateHexSearchService(DataKind, SearchString, (options & OurFindOptions.MatchCase) != 0, IsBigEndian);
			if (hexSearchService is null)
				return null;
			var startingPosition = GetStartingPosition(SearchKind.Replace, options, restart: true);
			if (startingPosition is null)
				return null;
			var searchRange = wpfHexView.BufferLines.BufferSpan;
			return hexSearchService.Find(searchRange, startingPosition.Value, ToHexFindOptions(options), CancellationToken.None);
		}

		static HexFindOptions ToHexFindOptions(OurFindOptions options) {
			var res = HexFindOptions.None;
			if ((options & OurFindOptions.SearchReverse) != 0)
				res |= HexFindOptions.SearchReverse;
			if ((options & OurFindOptions.Wrap) != 0)
				res |= HexFindOptions.Wrap;
			if ((options & OurFindOptions.NoOverlaps) != 0)
				res |= HexFindOptions.NoOverlaps;
			return res;
		}

		bool CanReplaceAll => CanReplace &&
			hexSearchServiceFactory.IsSearchDataValid(DataKind, SearchString, (GetFindOptions(SearchKind.Replace, true) & OurFindOptions.MatchCase) != 0, IsBigEndian) &&
			IsReplaceStringValid();
		void ReplaceAll() {
			if (!CanReplaceAll)
				return;

			var oldVersion = wpfHexView.Buffer.Version;
			try {
				byte[]? newData = null;
				using (var ed = wpfHexView.Buffer.CreateEdit()) {
					foreach (var res in GetAllResultsForReplaceAll()) {
						if (newData is null)
							newData = TryGetReplaceStringData(res);
						Debug2.Assert(!(newData is null) && newData.Length == res.Length);
						if (newData is null || newData.Length != res.Length)
							return;
						// Ignore errors due to read-only regions
						ed.Replace(res.Span.Start, newData);
					}
					ed.Apply();
					if (ed.Canceled)
						return;
				}
			}
			catch (ArithmeticException) {
				messageBoxService.Show("Out of memory");
				return;
			}
			catch (OutOfMemoryException) {
				messageBoxService.Show("Out of memory");
				return;
			}
			if (oldVersion != wpfHexView.Buffer.Version)
				wpfHexView.Selection.Clear();
			wpfHexView.Caret.EnsureVisible();
		}

		// Finds all results but makes sure that all replacements never overlap another one,
		// eg. if SearchString is aaa and text is aaaaaaaa, it returns two results, starting
		// at offsets 0 and 3. The last two aa's aren't touched. Normal FindNext finds matches
		// at offsets 0, 1, 2, 3, 4, 5.
		IEnumerable<HexBufferSpan> GetAllResultsForReplaceAll() {
			var searchRange = wpfHexView.BufferLines.BufferSpan;
			var options = GetFindOptions(SearchKind.Replace, true) & ~OurFindOptions.Wrap;
			options |= OurFindOptions.NoOverlaps;
			var startingPosition = searchRange.Start;
			var hexSearchService = hexSearchServiceFactory.TryCreateHexSearchService(DataKind, SearchString, (options & OurFindOptions.MatchCase) != 0, IsBigEndian);
			if (hexSearchService is null)
				return Array.Empty<HexBufferSpan>();
			return hexSearchService.FindAll(searchRange, startingPosition, ToHexFindOptions(options), CancellationToken.None);
		}

		bool CanToggleFindReplace => true;
		void ToggleFindReplace() {
			if (searchKind != SearchKind.Replace)
				SetSearchKind(SearchKind.Replace);
			else
				SetSearchKind(SearchKind.Find);
		}

		OurFindOptions GetFindOptions(SearchKind searchKind, bool? forward) {
			Debug.Assert(searchKind != SearchKind.None);
			var options = OurFindOptions.None;
			switch (searchKind) {
			case SearchKind.Find:
			case SearchKind.Replace:
				if (MatchCase)
					options |= OurFindOptions.MatchCase;
				break;

			case SearchKind.IncrementalSearchBackward:
			case SearchKind.IncrementalSearchForward:
				if (SearchString.Any(c => char.IsUpper(c)))
					options |= OurFindOptions.MatchCase;
				if (forward is null)
					forward = searchKind == SearchKind.IncrementalSearchForward;
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(searchKind));
			}
			if (forward == false)
				options |= OurFindOptions.SearchReverse;
			options |= OurFindOptions.Wrap;
			return options;
		}

		static bool IsMultiLineRegexPattern(string s) => s.Contains(@"\r") || s.Contains(@"\n") || s.Contains("$");

		HexBufferPoint? GetStartingPosition(SearchKind searchKind, OurFindOptions options, bool restart) {
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

		HexBufferPoint? GetStartingPosition(OurFindOptions options, bool restart) {
			if (wpfHexView.Selection.IsEmpty)
				return wpfHexView.Caret.Position.Position.ActivePosition.BufferPosition;
			if (restart) {
				if ((options & OurFindOptions.SearchReverse) == 0)
					return wpfHexView.Selection.Start;
				return wpfHexView.Selection.End;
			}
			var validSpan = wpfHexView.BufferLines.BufferSpan;
			if ((options & OurFindOptions.SearchReverse) != 0) {
				if (wpfHexView.Selection.End.Position >= validSpan.Start.Position + 2)
					return wpfHexView.Selection.End - 2;
				if ((options & OurFindOptions.Wrap) != 0)
					return validSpan.End;
				return null;
			}
			if (wpfHexView.Selection.Start < validSpan.End)
				return wpfHexView.Selection.Start + 1;
			if ((options & OurFindOptions.Wrap) != 0)
				return validSpan.Start;
			return null;
		}

		public override void FindNext(bool forward) {
			UseGlobalSettingsIfUiIsHidden(true);
			var options = GetFindOptions(SearchKind.Find, forward);
			var startingPosition = GetStartingPosition(SearchKind.Find, options, restart: false);
			FindNextCore(options, startingPosition, isIncrementalSearch: false);
		}

		void FindNextCore(OurFindOptions options, HexBufferPoint? startingPosition, bool isIncrementalSearch) {
			CancelFindAsyncSearcher();
			if (startingPosition is null)
				return;

			var searchOptions = new SearchOptions(wpfHexView.BufferLines.BufferSpan, startingPosition.Value, DataKind, SearchString, options, IsBigEndian);
			IAsyncSearcher? findAsyncSearcherTmp = null;
			findAsyncSearcherTmp = FindAsync(searchOptions, (result, foundSpan) => {
				if (findAsyncSearcher != findAsyncSearcherTmp)
					return;
				CancelFindAsyncSearcher();
				if (!(foundSpan is null)) {
					try {
						isIncrementalSearchCaretMove = isIncrementalSearch;
						ShowSearchResult(foundSpan.Value);
					}
					finally {
						isIncrementalSearchCaretMove = false;
					}
				}
			});
			findAsyncSearcher = findAsyncSearcherTmp;
		}
		IAsyncSearcher? findAsyncSearcher;

		void CancelFindAsyncSearcher() {
			findAsyncSearcher?.CancelAndDispose();
			findAsyncSearcher = null;
		}

		enum FindAsyncResult {
			InvalidSearchOptions,
			HasResult,
			Other,
		}

		IAsyncSearcher? FindAsync(SearchOptions searchOptions, Action<FindAsyncResult, HexBufferSpan?> onCompleted) {
			var hexSearchService = hexSearchServiceFactory.TryCreateHexSearchService(searchOptions.DataKind, searchOptions.SearchString, (searchOptions.FindOptions & OurFindOptions.MatchCase) != 0, searchOptions.IsBigEndian);
			if (hexSearchService is null) {
				onCompleted(FindAsyncResult.InvalidSearchOptions, null);
				return null;
			}
			var searcher = new AsyncSearcher(hexSearchService, searchOptions);
			searcher.OnCompleted += onCompleted;
			asyncSearchers.Add(searcher);
			Searching = asyncSearchers.Count != 0;
			StartSearchAsync(searcher).ContinueWith(t => {
				var searcherWasCanceled = searcher.Canceled;
				searcher.CancelAndDispose();
				bool wasInList = asyncSearchers.Remove(searcher);
				Searching = asyncSearchers.Count != 0;
				var ex = t.Exception;
				Debug2.Assert(ex is null);
				if (wasInList && !searcherWasCanceled && !t.IsCanceled && !t.IsFaulted)
					searcher.RaiseCompleted(FindAsyncResult.HasResult, t.Result);
				else
					searcher.RaiseCompleted(FindAsyncResult.Other, null);
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			return searcher;
		}
		readonly List<AsyncSearcher> asyncSearchers = new List<AsyncSearcher>();

		Task<HexBufferSpan?> StartSearchAsync(AsyncSearcher asyncSearcher) =>
			Task.Run(() => asyncSearcher.Find(), asyncSearcher.CancellationToken);

		void CancelAllAsyncSearches() {
			foreach (var searcher in asyncSearchers)
				searcher.CancelAndDispose();
			asyncSearchers.Clear();
			Searching = asyncSearchers.Count != 0;
		}

		sealed class SearchOptions {
			public HexBufferSpan SearchRange { get; }
			public HexBufferPoint StartingPosition { get; }
			public HexDataKind DataKind { get; }
			public string SearchString { get; }
			public OurFindOptions FindOptions { get; }
			public bool IsBigEndian { get; }

			public SearchOptions(HexBufferSpan searchRange, HexBufferPoint startingPosition, HexDataKind dataKind, string searchString, OurFindOptions findOptions, bool isBigEndian) {
				SearchRange = searchRange;
				StartingPosition = startingPosition;
				DataKind = dataKind;
				SearchString = searchString;
				FindOptions = findOptions;
				IsBigEndian = isBigEndian;
			}
		}

		interface IAsyncSearcher {
			HexSearchService HexSearchService { get; }
			void CancelAndDispose();
		}

		sealed class AsyncSearcher : IAsyncSearcher {
			public HexSearchService HexSearchService { get; }
			public SearchOptions SearchOptions { get; }
			readonly CancellationTokenSource cancellationTokenSource;
			public CancellationToken CancellationToken { get; }
			bool disposed;

			public bool Canceled { get; private set; }
			public event Action<FindAsyncResult, HexBufferSpan?>? OnCompleted;

			public AsyncSearcher(HexSearchService hexSearchService, SearchOptions searchOptions) {
				HexSearchService = hexSearchService;
				SearchOptions = searchOptions;
				cancellationTokenSource = new CancellationTokenSource();
				CancellationToken = cancellationTokenSource.Token;
			}

			public HexBufferSpan? Find() =>
				HexSearchService.Find(SearchOptions.SearchRange, SearchOptions.StartingPosition, ToHexFindOptions(SearchOptions.FindOptions), CancellationToken);

			public void RaiseCompleted(FindAsyncResult result, HexBufferSpan? span) => OnCompleted?.Invoke(result, span);

			public void CancelAndDispose() {
				Canceled = true;
				Cancel();
				Dispose();
			}

			void Cancel() {
				if (!disposed)
					cancellationTokenSource.Cancel();
			}

			void Dispose() {
				disposed = true;
				cancellationTokenSource.Dispose();
			}
		}

		void ShowSearchResult(HexBufferSpan span) {
			editorOperations.SelectAndMoveCaret(wpfHexView.Caret.Position.Position.ActiveColumn, span.Start, span.End, alignPoints: false);
			if (IsSearchControlVisible)
				PositionWithoutCoveringSpan(span);
		}

		public override void FindNextSelected(bool forward) => FindNextSelectedCore(forward, false);

		void FindNextSelectedCore(bool forward, bool restart) {
			var newSearchString = TryGetSearchStringAtCaret();
			if (newSearchString is null)
				return;

			ShowSearchControl(SearchKind.Find, canOverwriteSearchString: false);
			// Don't focus the search control. Whoever has focus (most likely hex editor)
			// should keep the focus.

			// This search doesn't use the options from the search control
			var options = OurFindOptions.Wrap | OurFindOptions.MatchCase;
			if (!forward)
				options |= OurFindOptions.SearchReverse;
			var startingPosition = GetStartingPosition(SearchKind.Find, options, restart);
			SetSearchString(newSearchString, canSearch: false);
			FindNextCore(options, startingPosition, isIncrementalSearch: false);
		}

		public override IEnumerable<HexBufferSpan> GetSpans(NormalizedHexBufferSpanCollection spans) {
			var searchService = hexMarkerSearchService;
			if (searchService is null)
				yield break;
			var validSpan = wpfHexView.BufferLines.BufferSpan;
			int lengthLessOne = searchService.ByteCount - 1;
			foreach (var span in spans) {
				var overlap = validSpan.Overlap(span);
				if (overlap is null)
					continue;
				var start = validSpan.Start.Position + lengthLessOne <= overlap.Value.Start.Position ? overlap.Value.Start - lengthLessOne : validSpan.Start;
				var end = new HexBufferPoint(validSpan.Buffer, HexPosition.Min(overlap.Value.End.Position + lengthLessOne, validSpan.End));
				foreach (var res in searchService.FindAll(HexBufferSpan.FromBounds(start, end), start, HexFindOptions.None, CancellationToken.None))
					yield return res;
			}
		}

		public override void RegisterHexMarkerListener(IHexMarkerListener listener) => listeners.Add(listener);

		void RestartSearchAndUpdateMarkers() {
			if (!IsSearchControlVisible)
				return;

			UpdateHexMarkerSearch();
			RestartSearch();
		}

		void RestartSearch() {
			if (!IsSearchControlVisible)
				return;
			if (SearchString.Length == 0)
				return;

			var options = GetFindOptions(searchKind, null);
			var startingPosition = GetStartingPosition(searchKind, options, restart: true);
			FindNextCore(options, startingPosition, isIncrementalSearch: searchKind == SearchKind.IncrementalSearchBackward || searchKind == SearchKind.IncrementalSearchForward);
		}

		void UpdateHexMarkerSearch() {
			CancelAsyncSearch();
			if (!IsSearchControlVisible)
				return;

			var options = GetFindOptions(SearchKind.Find, true);
			var searchRange = wpfHexView.BufferLines.BufferSpan;
			var startingPosition = searchRange.Start;
			// Continue from the last match, instead of from the beginning of the range, if possible.
			if (searchRange.Span.Contains(lastMatch)) {
				startingPosition = new HexBufferPoint(searchRange.Buffer, lastMatch);
				options |= OurFindOptions.Wrap;
			}
			var searchOptions = new SearchOptions(searchRange, startingPosition, DataKind, SearchString, options, IsBigEndian);

			IAsyncSearcher? searcher = null;
			searcher = FindAsync(searchOptions, (result, foundSpan) => {
				if (result == FindAsyncResult.InvalidSearchOptions) {
					bool refresh = !(hexMarkerSearchService is null);
					hexMarkerSearchService = null;
					if (refresh)
						RefreshAllTags();
					// We could be here if the input string is invalid, in which case we tell the user there was nothing found
					FoundMatch = SearchString.Length == 0;
				}
				else if (result == FindAsyncResult.HasResult && !(searcher is null) && searcher.HexSearchService == hexMarkerSearchService) {
					FoundMatch = !(foundSpan is null);
					lastMatch = foundSpan is null ? HexPosition.Zero : foundSpan.Value.Span.Start;
				}
			});
			hexMarkerSearchService = searcher?.HexSearchService;
			RefreshAllTags();
		}
		HexPosition lastMatch;
		HexSearchService? hexMarkerSearchService;
		IAsyncSearcher? hexMarkerAsyncSearcher;

		void CancelAsyncSearch() {
			hexMarkerAsyncSearcher?.CancelAndDispose();
			hexMarkerAsyncSearcher = null;
			hexMarkerSearchService = null;
		}

		void RefreshAllTags() {
			var span = new HexBufferSpan(wpfHexView.Buffer, HexSpan.FromBounds(HexPosition.Zero, HexPosition.MaxEndPosition));
			foreach (var listener in listeners)
				listener.RaiseTagsChanged(span);
		}

		void WpfHexView_LayoutChanged(object? sender, HexViewLayoutChangedEventArgs e) {
			Debug.Assert(IsSearchControlVisible);
			if (!IsSearchControlVisible)
				return;
			if (e.OldViewState.ViewportWidth != e.NewViewState.ViewportWidth)
				RepositionControl(true);
			else if (e.OldViewState.ViewportHeight != e.NewViewState.ViewportHeight)
				RepositionControl(true);
			if (e.OldVersion != e.NewVersion)
				CancelIncrementalSearchAndUpdateMarkers();
		}

		void WpfHexView_BufferLinesChanged(object? sender, BufferLinesChangedEventArgs e) => CancelIncrementalSearchAndUpdateMarkers();
		void Buffer_BufferSpanInvalidated(object? sender, HexBufferSpanInvalidatedEventArgs e) => CancelIncrementalSearchAndUpdateMarkers();

		void CancelIncrementalSearchAndUpdateMarkers() {
			CancelIncrementalSearch();
			UpdateHexMarkerSearch();
		}

		void WpfHexView_Closed(object? sender, EventArgs e) {
			CloseSearchControl();
			CancelAllAsyncSearches();
			wpfHexView.Closed -= WpfHexView_Closed;
			wpfHexView.LayoutChanged -= WpfHexView_LayoutChanged;
			wpfHexView.BufferLinesChanged -= WpfHexView_BufferLinesChanged;
			wpfHexView.Buffer.BufferSpanInvalidated -= Buffer_BufferSpanInvalidated;
		}
	}
}
