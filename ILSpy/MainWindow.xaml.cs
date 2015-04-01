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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Controls;
using ICSharpCode.ILSpy.Debugger.Services;
using ICSharpCode.ILSpy.dntheme;
using ICSharpCode.ILSpy.Debugger;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.NRefactory;
using ICSharpCode.TreeView;
using Microsoft.Win32;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// The main window of the application.
	/// </summary>
	partial class MainWindow : Window
	{
		ILSpySettings spySettings;
		internal SessionSettings sessionSettings;
		
		internal AssemblyListManager assemblyListManager;
		AssemblyList assemblyList;
		AssemblyListTreeNode assemblyListTreeNode;

		[ImportMany]
		IEnumerable<IPlugin> plugins = null;

		[ImportMany]
		IEnumerable<IPaneCreator> paneCreators = null;

		[ImportMany(typeof(ITextEditorListener))]
		public IEnumerable<ITextEditorListener> textEditorListeners = null;
		
		static MainWindow instance;
		
		public static MainWindow Instance {
			get { return instance; }
		}
		
		public SessionSettings SessionSettings {
			get { return sessionSettings; }
		}

		readonly TabGroupsManager<TabStateDecompile> tabGroupsManager;

		internal TabStateDecompile SafeActiveTabState {
			get {
				var tabState = ActiveTabState;
				if (tabState != null)
					return tabState;

				tabState = CreateEmptyTabState();

				var old = IgnoreSelectionChanged_HACK(tabState);
				try {
					tabGroupsManager.ActiveTabGroup.SetSelectedTab(tabState);
				}
				finally {
					RestoreIgnoreSelectionChanged_HACK(tabState, old);
				}

				return tabState;
			}
		}

		internal TabStateDecompile ActiveTabState {
			get { return tabGroupsManager.ActiveTabGroup.ActiveTabState; }
		}

		IEnumerable<TabStateDecompile> AllTabStates {
			get { return tabGroupsManager.AllTabStates; }
		}

		IEnumerable<TabStateDecompile> AllVisibleTabStates {
			get {
				foreach (var tabManager in tabGroupsManager.AllTabGroups) {
					var tabState = tabManager.ActiveTabState;
					if (tabState != null)
						yield return tabState;
				}
			}
		}

		public DecompilerTextView SafeActiveTextView {
			get { return SafeActiveTabState.TextView; }
		}

		public DecompilerTextView ActiveTextView {
			get {
				var tabState = ActiveTabState;
				return tabState == null ? null : tabState.TextView;
			}
		}

		public IEnumerable<DecompilerTextView> AllVisibleTextViews {
			get {
				foreach (var tabState in AllVisibleTabStates)
					yield return tabState.TextView;
			}
		}

		public IEnumerable<DecompilerTextView> AllTextViews {
			get {
				foreach (var tabState in AllTabStates)
					yield return tabState.TextView;
			}
		}
		
		public MainWindow()
		{
			instance = this;
			spySettings = ILSpySettings.Load();
			this.sessionSettings = new SessionSettings(spySettings);
			this.sessionSettings.PropertyChanged += sessionSettings_PropertyChanged;
			this.assemblyListManager = new AssemblyListManager(spySettings);
			Themes.ThemeChanged += Themes_ThemeChanged;
			
			this.Icon = new BitmapImage(new Uri("pack://application:,,,/dnSpy;component/images/ILSpy.ico"));
			
			this.DataContext = sessionSettings;
			
			InitializeComponent();
			App.CompositionContainer.ComposeParts(this);
			
			if (sessionSettings.LeftColumnWidth > 0)
				leftColumn.Width = new GridLength(sessionSettings.LeftColumnWidth, GridUnitType.Pixel);
			sessionSettings.FilterSettings.PropertyChanged += filterSettings_PropertyChanged;
			
			tabGroupsManager = new TabGroupsManager<TabStateDecompile>(tabGroupsContentPresenter, tabManager_OnSelectionChanged, tabManager_OnAddRemoveTabState);
			tabGroupsManager.OnTabGroupSelected += tabGroupsManager_OnTabGroupSelected;
			InitMainMenu();
			InitToolbar();
			
			this.Loaded += MainWindow_Loaded;
		}

		internal bool IsDecompilerTabControl(TabControl tabControl)
		{
			return tabGroupsManager.IsTabGroup(tabControl);
		}

		void sessionSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "WordWrap" || e.PropertyName == "HighlightCurrentLine") {
				foreach (var textView in AllTextViews) {
					textView.TextEditor.WordWrap = sessionSettings.WordWrap;
					textView.TextEditor.Options.HighlightCurrentLine = sessionSettings.HighlightCurrentLine;
				}
			}
		}

		static void RemoveCommands(DecompilerTextView view)
		{
			var handler = view.TextEditor.TextArea.DefaultInputHandler;

			RemoveCommands(handler.Editing);
			RemoveCommands(handler.CaretNavigation);
		}

		static void RemoveCommands(ICSharpCode.AvalonEdit.Editing.TextAreaInputHandler handler)
		{
			var commands = new HashSet<ICommand>();
			var inputList = (IList<InputBinding>)handler.InputBindings;
			for (int i = inputList.Count - 1; i >= 0; i--) {
				var kb = inputList[i] as KeyBinding;
				if (kb == null)
					continue;
				if ((kb.Modifiers == ModifierKeys.None && kb.Key == Key.Back) ||
					(kb.Modifiers == ModifierKeys.None && kb.Key == Key.Enter) ||
					(kb.Modifiers == ModifierKeys.Control && kb.Key == Key.Enter)) {
					inputList.RemoveAt(i);
					commands.Add(kb.Command);
				}
			}

			var bindingList = (IList<CommandBinding>)handler.CommandBindings;
			for (int i = bindingList.Count - 1; i >= 0; i--) {
				var binding = bindingList[i];
				// Ctrl+D: GoToToken
				if (binding.Command == ICSharpCode.AvalonEdit.AvalonEditCommands.DeleteLine)
					bindingList.RemoveAt(i);
				else if (commands.Contains(binding.Command))
					bindingList.RemoveAt(i);
			}
		}

		void tabManager_OnAddRemoveTabState(TabManager<TabStateDecompile> tabManager, TabManagerAddType addType, TabStateDecompile tabState)
		{
			var view = tabState.TextView;
			if (addType == TabManagerAddType.Add) {
				RemoveCommands(view);
				view.TextEditor.TextArea.MouseRightButtonDown += delegate { view.GoToMousePosition(); };
				view.TextEditor.WordWrap = sessionSettings.WordWrap;
				view.TextEditor.Options.HighlightCurrentLine = sessionSettings.HighlightCurrentLine;

				if (OnDecompilerTextViewAdded != null)
					OnDecompilerTextViewAdded(this, new DecompilerTextViewEventArgs(view));
			}
			else if (addType == TabManagerAddType.Remove) {
				if (OnDecompilerTextViewRemoved != null)
					OnDecompilerTextViewRemoved(this, new DecompilerTextViewEventArgs(view));
			}
			else if (addType == TabManagerAddType.Attach) {
				if (OnDecompilerTextViewAttached != null)
					OnDecompilerTextViewAttached(this, new DecompilerTextViewEventArgs(view));
			}
			else if (addType == TabManagerAddType.Detach) {
				if (OnDecompilerTextViewDetached != null)
					OnDecompilerTextViewDetached(this, new DecompilerTextViewEventArgs(view));
			}
			else
				throw new InvalidOperationException();
		}

		public event EventHandler<DecompilerTextViewEventArgs> OnDecompilerTextViewAdded;
		public event EventHandler<DecompilerTextViewEventArgs> OnDecompilerTextViewRemoved;
		public event EventHandler<DecompilerTextViewEventArgs> OnDecompilerTextViewAttached;
		public event EventHandler<DecompilerTextViewEventArgs> OnDecompilerTextViewDetached;
		public class DecompilerTextViewEventArgs : EventArgs
		{
			public readonly DecompilerTextView DecompilerTextView;

			public DecompilerTextViewEventArgs(DecompilerTextView decompilerTextView)
			{
				this.DecompilerTextView = decompilerTextView;
			}
		}

		public event EventHandler<TabGroupEventArgs> OnTabGroupAdded {
			add {
				tabGroupsManager.OnTabGroupAdded += value;
			}
			remove {
				tabGroupsManager.OnTabGroupAdded -= value;
			}
		}

		public event EventHandler<TabGroupEventArgs> OnTabGroupRemoved {
			add {
				tabGroupsManager.OnTabGroupRemoved += value;
			}
			remove {
				tabGroupsManager.OnTabGroupRemoved -= value;
			}
		}

		public event EventHandler<TabGroupSelectedEventArgs> OnTabGroupSelected {
			add {
				tabGroupsManager.OnTabGroupSelected += value;
			}
			remove {
				tabGroupsManager.OnTabGroupSelected -= value;
			}
		}

		public event EventHandler<TabGroupSwappedEventArgs> OnTabGroupSwapped {
			add {
				tabGroupsManager.OnTabGroupSwapped += value;
			}
			remove {
				tabGroupsManager.OnTabGroupSwapped -= value;
			}
		}

		public event EventHandler<TabGroupEventArgs> OnTabGroupsOrientationChanged {
			add {
				tabGroupsManager.OnOrientationChanged += value;
			}
			remove {
				tabGroupsManager.OnOrientationChanged -= value;
			}
		}

		bool IsActiveTab(TabStateDecompile tabState)
		{
			return tabGroupsManager.ActiveTabGroup.ActiveTabState == tabState;
		}

		void SelectTreeViewNodes(TabStateDecompile tabState, ILSpyTreeNode[] nodes)
		{
			if (!IsActiveTab(tabState))
				return;
			var old = tabState.ignoreDecompilationRequests;
			try {
				tabState.ignoreDecompilationRequests = true;
				treeView.SelectedItems.Clear();
				if (nodes.Length > 0) {
					treeView.FocusNode(nodes[0]);
					// This can happen when pressing Ctrl+Shift+Tab when the treeview has keyboard focus
					if (treeView.SelectedItems.Count != 0)
						treeView.SelectedItems.Clear();
					treeView.SelectedItem = nodes[0];
				}
				foreach (var node in nodes)
					treeView.SelectedItems.Add(node);
			}
			finally {
				tabState.ignoreDecompilationRequests = old;
			}
		}

		//TODO: HACK alert
		Dictionary<TabManagerBase, bool> tabManager_dontSelectHack = new Dictionary<TabManagerBase, bool>();
		internal bool IgnoreSelectionChanged_HACK(TabState tabState)
		{
			var tabManager = tabState.Owner;
			bool value;
			tabManager_dontSelectHack.TryGetValue(tabManager, out value);
			tabManager_dontSelectHack[tabManager] = true;
			return value;
		}
		internal void RestoreIgnoreSelectionChanged_HACK(TabState tabState, bool oldValue)
		{
			var tabManager = tabState.Owner;
			if (!oldValue)
				tabManager_dontSelectHack.Remove(tabManager);
			else
				tabManager_dontSelectHack[tabManager] = oldValue;
		}

		int debug_CommandBindings_Count;
		void InitializeActiveTab(TabStateDecompile tabState, bool forceIsInActiveTabGroup)
		{
			var tabManager = tabState == null ? null : tabState.Owner as TabManager<TabStateDecompile>;
			bool isInActiveTabGroup = tabGroupsManager.ActiveTabGroup == tabManager || forceIsInActiveTabGroup;

			var newView = tabState == null ? null : tabState.TextView;

			if (newView != null) {
				if (isInActiveTabGroup) {
					Debug.Assert(debug_CommandBindings_Count == this.CommandBindings.Count);
					this.CommandBindings.AddRange(newView.CommandBindings);
					SetLanguage(tabState.Language);
				}
			}

			bool dontSelect;
			if (tabManager != null && tabManager_dontSelectHack.TryGetValue(tabManager, out dontSelect) && dontSelect) {
			}
			else if (tabState == null) {
				if ((tabGroupsManager.AllTabGroups.Count == 1 && tabGroupsManager.ActiveTabGroup.ActiveTabState == null)) {
					var old = TreeView_SelectionChanged_ignore;
					try {
						TreeView_SelectionChanged_ignore = true;
						treeView.SelectedItems.Clear();
					}
					finally {
						TreeView_SelectionChanged_ignore = old;
					}
				}
				else if (isInActiveTabGroup)
					treeView.SelectedItems.Clear();
			}
			else
				SelectTreeViewNodes(tabState, tabState.DecompiledNodes);

			if (isInActiveTabGroup)
				ClosePopups();
			if (newView != null && isInActiveTabGroup)
				InstallTextEditorListeners(newView);
		}

		void UninitializeActiveTab(TabStateDecompile tabState, bool forceIsInActiveTabGroup)
		{
			var tabManager = tabState == null ? null : tabState.Owner as TabManager<TabStateDecompile>;
			bool isInActiveTabGroup = tabGroupsManager.ActiveTabGroup == tabManager || forceIsInActiveTabGroup;

			var oldView = tabState == null ? null : tabState.TextView;

			if (oldView != null && isInActiveTabGroup) {
				Debug.Assert(debug_CommandBindings_Count + oldView.CommandBindings.Count == this.CommandBindings.Count);
				foreach (CommandBinding binding in oldView.CommandBindings)
					this.CommandBindings.Remove(binding);
				Debug.Assert(debug_CommandBindings_Count == this.CommandBindings.Count);
				UninstallTextEditorListeners(oldView);
			}
		}

		internal void tabManager_OnSelectionChanged(TabManager<TabStateDecompile> tabManager, TabStateDecompile oldState, TabStateDecompile newState)
		{
			var oldView = oldState == null ? null : oldState.TextView;
			var newView = newState == null ? null : newState.TextView;

			UninitializeActiveTab(oldState, false);
			InitializeActiveTab(newState, false);

			if (IsActiveTab(newState))
				SetTextEditorFocus(newView);

			if (OnDecompilerTextViewChanged != null)
				OnDecompilerTextViewChanged(this, new DecompilerTextViewChangedEventArgs(oldView, newView));
		}

		void tabGroupsManager_OnTabGroupSelected(object sender, TabGroupSelectedEventArgs e)
		{
			var oldTabManager = tabGroupsManager.AllTabGroups[e.OldIndex];
			var newTabManager = tabGroupsManager.AllTabGroups[e.NewIndex];

			UninitializeActiveTab(oldTabManager.ActiveTabState, true);
			InitializeActiveTab(newTabManager.ActiveTabState, true);

			var activeTabState = newTabManager.ActiveTabState;
			if (activeTabState != null)
				SetTextEditorFocus(activeTabState.TextView);

			if (OnActiveDecompilerTextViewChanged != null) {
				var oldView = oldTabManager.ActiveTabState == null ? null : oldTabManager.ActiveTabState.TextView;
				var newView = newTabManager.ActiveTabState == null ? null : newTabManager.ActiveTabState.TextView;
				OnActiveDecompilerTextViewChanged(this, new DecompilerTextViewChangedEventArgs(oldView, newView));
			}
		}

		public void SetTextEditorFocus(DecompilerTextView textView)
		{
			var tabState = TabStateDecompile.GetTabStateDecompile(textView);
			if (tabState == null)
				return;
			if (!IsActiveTab(tabState))
				return;

			if (textView.waitAdornerButton.IsVisible) {
				textView.waitAdornerButton.Focus();
				return;
			}

			// Set focus to the text area whenever the view is selected
			var textArea = textView.TextEditor.TextArea;
			if (!textArea.IsVisible) {
				new SetFocusWhenVisible(tabState);
			}
			else
				textArea.Focus();
		}

		class SetFocusWhenVisible
		{
			readonly TabStateDecompile tabState;

			public SetFocusWhenVisible(TabStateDecompile tabState)
			{
				this.tabState = tabState;
				tabState.TextView.TextEditor.TextArea.IsVisibleChanged += textArea_IsVisibleChanged;
			}

			void textArea_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
			{
				var textArea = tabState.TextView.TextEditor.TextArea;
				textArea.IsVisibleChanged -= textArea_IsVisibleChanged;
				if (MainWindow.Instance.IsActiveTab(tabState))
					textArea.Focus();
			}
		}

		public event EventHandler<DecompilerTextViewChangedEventArgs> OnDecompilerTextViewChanged;
		public event EventHandler<DecompilerTextViewChangedEventArgs> OnActiveDecompilerTextViewChanged;
		public class DecompilerTextViewChangedEventArgs : EventArgs
		{
			/// <summary>
			/// Old view. Can be null
			/// </summary>
			public readonly DecompilerTextView OldView;

			/// <summary>
			/// New view. Can be null
			/// </summary>
			public readonly DecompilerTextView NewView;

			public DecompilerTextViewChangedEventArgs(DecompilerTextView oldView, DecompilerTextView newView)
			{
				this.OldView = oldView;
				this.NewView = newView;
			}
		}

		void InstallTextEditorListeners(DecompilerTextView textView)
		{
			if (textEditorListeners == null)
				return;
			foreach (var listener in textEditorListeners) {
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHover.AddListener(textView.TextEditor, listener);
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHoverStopped.AddListener(textView.TextEditor, listener);
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseDown.AddListener(textView.TextEditor, listener);
			}
		}

		void UninstallTextEditorListeners(DecompilerTextView textView)
		{
			if (textEditorListeners == null)
				return;
			foreach (var listener in textEditorListeners) {
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHover.RemoveListener(textView.TextEditor, listener);
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHoverStopped.RemoveListener(textView.TextEditor, listener);
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseDown.RemoveListener(textView.TextEditor, listener);
			}
		}

		internal void SetTitle(DecompilerTextView textView, string title)
		{
			var tabState = TabStateDecompile.GetTabStateDecompile(textView);
			tabState.Title = title;
			InitializeHeader(tabState);
		}

		void InitializeHeader(TabStateDecompile tabState)
		{
			tabState.InitializeHeader();
			if (OnTabHeaderChanged != null)
				OnTabHeaderChanged(null, EventArgs.Empty);
		}
		public event EventHandler OnTabHeaderChanged;

		internal void ClosePopups()
		{
			if (textEditorListeners == null)
				return;
			foreach (var listener in textEditorListeners)
				listener.ClosePopup();
		}

		void Themes_ThemeChanged(object sender, EventArgs e)
		{
			foreach (var view in AllTextViews)
				view.OnThemeUpdated();
		}
		
		void SetWindowBounds(Rect bounds)
		{
			this.Left = bounds.Left;
			this.Top = bounds.Top;
			this.Width = bounds.Width;
			this.Height = bounds.Height;
		}
		
		#region Toolbar extensibility
		[ImportMany("ToolbarCommand", typeof(ICommand))]
		Lazy<ICommand, IToolbarCommandMetadata>[] toolbarCommands = null;
		
		/// <summary>
		/// Call this when a toolbar button's visibility has changed. The toolbar items will be
		/// re-created.
		/// </summary>
		public void UpdateToolbar()
		{
			toolBar.Items.Clear();
			foreach (var o in mtbState.OriginalToolbarItems)
				toolBar.Items.Add(o);
			int navigationPos = 1;
			int openPos = 2;
			foreach (var commandGroup in mtbState.Groupings) {
				if (commandGroup.Key == "Navigation") {
					foreach (var command in commandGroup) {
						toolBar.Items.Insert(navigationPos++, MakeToolbarItem(command));
						openPos++;
					}
				} else if (commandGroup.Key == "Open") {
					foreach (var command in commandGroup) {
						toolBar.Items.Insert(openPos++, MakeToolbarItem(command));
					}
				} else {
					var items = new List<Button>();
					foreach (var command in commandGroup) {
						var tbarCmd = command.Value as IToolbarCommand;
						if (tbarCmd == null || tbarCmd.IsVisible)
							items.Add(MakeToolbarItem(command));
					}

					if (items.Count > 0) {
						toolBar.Items.Add(new Separator());
						foreach (var item in items)
							toolBar.Items.Add(item);
					}
				}
			}

			// We must tell it to re-check all commands again. If we don't, some of the toolbar
			// buttons will be disabled until the user clicks with the mouse or presses a key.
			// This happens when debugging and you press F5 and the program exits.
			CommandManager.InvalidateRequerySuggested();
		}

		class MainToolbarState
		{
			public object[] OriginalToolbarItems;
			public IGrouping<string, Lazy<ICommand, IToolbarCommandMetadata>>[] Groupings;
		}
		MainToolbarState mtbState = new MainToolbarState();
		void InitToolbar()
		{
			mtbState.OriginalToolbarItems = new object[toolBar.Items.Count];
			for (int i = 0; i < toolBar.Items.Count; i++)
				mtbState.OriginalToolbarItems[i] = toolBar.Items[i];
			mtbState.Groupings = toolbarCommands.OrderBy(c => c.Metadata.ToolbarOrder).GroupBy(c => c.Metadata.ToolbarCategory).ToArray();
			UpdateToolbar();
		}
		
		Button MakeToolbarItem(Lazy<ICommand, IToolbarCommandMetadata> command)
		{
			var button = new Button {
				Command = CommandWrapper.Unwrap(command.Value),
				ToolTip = command.Metadata.ToolTip,
				Tag = command.Metadata.Tag,
				Content = new Image {
					Width = 16,
					Height = 16,
					Source = Images.LoadImage(command.Value, command.Metadata.ToolbarIcon)
				}
			};
			ToolTipService.SetShowOnDisabled(button, true);
			return button;
		}
		#endregion
		
		#region Main Menu extensibility
		[ImportMany("MainMenuCommand", typeof(ICommand))]
		Lazy<ICommand, IMainMenuCommandMetadata>[] mainMenuCommands = null;
		
		class MainSubMenuState
		{
			public MenuItem TopLevelMenuItem;
			public IGrouping<string, Lazy<ICommand, IMainMenuCommandMetadata>>[] Groupings;
			public List<object> OriginalItems = new List<object>();
		}
		readonly Dictionary<string, MainSubMenuState> subMenusDict = new Dictionary<string, MainSubMenuState>();
		void InitMainMenu()
		{
			foreach (var topLevelMenu in mainMenuCommands.OrderBy(c => c.Metadata.MenuOrder).GroupBy(c => c.Metadata.Menu)) {
				var topLevelMenuItem = mainMenu.Items.OfType<MenuItem>().FirstOrDefault(m => (m.Header as string) == topLevelMenu.Key);
				var state = new MainSubMenuState();
				if (topLevelMenuItem == null) {
					topLevelMenuItem = new MenuItem();
					topLevelMenuItem.Header = topLevelMenu.Key;
					mainMenu.Items.Add(topLevelMenuItem);
				}
				subMenusDict.Add((string)topLevelMenuItem.Header, state);
				state.TopLevelMenuItem = topLevelMenuItem;
				state.Groupings = topLevelMenu.GroupBy(c => c.Metadata.MenuCategory).ToArray();
				foreach (object o in state.TopLevelMenuItem.Items)
					state.OriginalItems.Add(o);
				InitializeMainSubMenu(state);
			}
		}

		/// <summary>
		/// If a menu item gets hidden, this method should be called to re-create the sub menu.
		/// </summary>
		/// <param name="menuHeader">The exact display name of the sub menu (eg. "_Debug")</param>
		public void UpdateMainSubMenu(string menuHeader)
		{
			InitializeMainSubMenu(subMenusDict[menuHeader]);
		}

		static void InitializeMainSubMenu(MainSubMenuState state)
		{
			//TODO: This code generates errors at runtime, except the first time it's called
			//		System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=HorizontalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'HorizontalContentAlignment' (type 'HorizontalAlignment')
			//		System.Windows.Data Error: 4 : Cannot find source for binding with reference 'RelativeSource FindAncestor, AncestorType='System.Windows.Controls.ItemsControl', AncestorLevel='1''. BindingExpression:Path=VerticalContentAlignment; DataItem=null; target element is 'MenuItem' (Name=''); target property is 'VerticalContentAlignment' (type 'VerticalAlignment')
			// Seems to be harmless, the menu items seem to work as expected.
			var topLevelMenuItem = state.TopLevelMenuItem;
			topLevelMenuItem.Items.Clear();
			foreach (var o in state.OriginalItems)
				state.TopLevelMenuItem.Items.Add(o);
			foreach (var category in state.Groupings) {
				var items = new List<object>();
				foreach (var entry in category) {
					var menuCmd = entry.Value as IMainMenuCommand;
					if (menuCmd != null && !menuCmd.IsVisible)
						continue;
					var provider = entry.Value as IMenuItemProvider;
					if (provider != null)
						items.AddRange(provider.CreateMenuItems());
					else {
						MenuItem menuItem = new MenuItem();
						menuItem.Command = CommandWrapper.Unwrap(entry.Value);
						// We must initialize CommandTarget or the menu items for the standard commands
						// (Ctrl+C, Ctrl+O etc) will be disabled after we stop debugging. We didn't
						// need to do this when the menu wasn't in the toolbar.
						menuItem.CommandTarget = MainWindow.Instance;
						if (!string.IsNullOrEmpty(entry.Metadata.Header))
							menuItem.Header = entry.Metadata.Header;
						if (!string.IsNullOrEmpty(entry.Metadata.MenuIcon)) {
							menuItem.Icon = new Image {
								Width = 16,
								Height = 16,
								Source = Images.LoadImage(entry.Value, entry.Metadata.MenuIcon)
							};
						}

						menuItem.IsEnabled = entry.Metadata.IsEnabled;
						menuItem.InputGestureText = entry.Metadata.InputGestureText;
						items.Add(menuItem);
					}
				}
				if (items.Count > 0) {
					if (topLevelMenuItem.Items.Count > 0)
						topLevelMenuItem.Items.Add(new Separator());
					foreach (var item in items)
						topLevelMenuItem.Items.Add(item);
				}
			}
		}
		#endregion
		
		#region Message Hook
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			PresentationSource source = PresentationSource.FromVisual(this);
			HwndSource hwndSource = source as HwndSource;
			if (hwndSource != null) {
				hwndSource.AddHook(WndProc);
			}
			// Validate and Set Window Bounds
			Rect bounds = Rect.Transform(sessionSettings.WindowBounds, source.CompositionTarget.TransformToDevice);
			var boundsRect = new System.Drawing.Rectangle((int)bounds.Left, (int)bounds.Top, (int)bounds.Width, (int)bounds.Height);
			bool boundsOK = false;
			foreach (var screen in System.Windows.Forms.Screen.AllScreens) {
				var intersection = System.Drawing.Rectangle.Intersect(boundsRect, screen.WorkingArea);
				if (intersection.Width > 10 && intersection.Height > 10)
					boundsOK = true;
			}
			if (boundsOK)
				SetWindowBounds(sessionSettings.WindowBounds);
			else
				SetWindowBounds(SessionSettings.DefaultWindowBounds);
			
			this.WindowState = sessionSettings.WindowState;
		}
		
		unsafe IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == NativeMethods.WM_COPYDATA) {
				CopyDataStruct* copyData = (CopyDataStruct*)lParam;
				string data = new string((char*)copyData->Buffer, 0, copyData->Size / sizeof(char));
				if (data.StartsWith("dnSpy:\r\n", StringComparison.Ordinal)) {
					data = data.Substring(8);
					List<string> lines = new List<string>();
					using (StringReader r = new StringReader(data)) {
						string line;
						while ((line = r.ReadLine()) != null)
							lines.Add(line);
					}
					var args = new CommandLineArguments(lines);
					if (HandleCommandLineArguments(args)) {
						if (!args.NoActivate && WindowState == WindowState.Minimized)
							WindowState = WindowState.Normal;
						HandleCommandLineArgumentsAfterShowList(args);
						handled = true;
						return (IntPtr)1;
					}
				}
			}
			return IntPtr.Zero;
		}
		#endregion
		
		public AssemblyList CurrentAssemblyList {
			get { return assemblyList; }
		}
		
		public event NotifyCollectionChangedEventHandler CurrentAssemblyListChanged;
		
		List<LoadedAssembly> commandLineLoadedAssemblies = new List<LoadedAssembly>();
		
		bool HandleCommandLineArguments(CommandLineArguments args)
		{
			foreach (string file in args.AssembliesToLoad) {
				commandLineLoadedAssemblies.Add(assemblyList.OpenAssembly(file));
			}
			if (args.Language != null)
				sessionSettings.FilterSettings.Language = Languages.GetLanguage(args.Language);
			return true;
		}
		
		void HandleCommandLineArgumentsAfterShowList(CommandLineArguments args)
		{
			if (args.NavigateTo != null) {
				bool found = false;
				if (args.NavigateTo.StartsWith("N:", StringComparison.Ordinal)) {
					string namespaceName = args.NavigateTo.Substring(2);
					foreach (LoadedAssembly asm in commandLineLoadedAssemblies) {
						AssemblyTreeNode asmNode = assemblyListTreeNode.FindAssemblyNode(asm);
						if (asmNode != null) {
							NamespaceTreeNode nsNode = asmNode.FindNamespaceNode(namespaceName);
							if (nsNode != null) {
								found = true;
								SelectNode(nsNode);
								break;
							}
						}
					}
				} else {
					foreach (LoadedAssembly asm in commandLineLoadedAssemblies) {
						ModuleDef def = asm.ModuleDefinition;
						if (def != null) {
							IMemberRef mr = XmlDocKeyProvider.FindMemberByKey(def, args.NavigateTo);
							if (mr != null) {
								found = true;
								JumpToReference(mr);
								break;
							}
						}
					}
				}
				if (!found) {
					AvalonEditTextOutput output = new AvalonEditTextOutput();
					output.Write(string.Format("Cannot find '{0}' in command line specified assemblies.", args.NavigateTo), TextTokenType.Text);
					SafeActiveTextView.ShowText(output);
				}
			} else if (commandLineLoadedAssemblies.Count == 1) {
				// NavigateTo == null and an assembly was given on the command-line:
				// Select the newly loaded assembly
				JumpToReference(commandLineLoadedAssemblies[0].ModuleDefinition);
			}
			if (args.Search != null)
			{
				SearchPane.Instance.SearchTerm = args.Search;
				SearchPane.Instance.Show();
			}
			if (!string.IsNullOrEmpty(args.SaveDirectory)) {
				foreach (var x in commandLineLoadedAssemblies) {
					x.ContinueWhenLoaded( (Task<ModuleDef> moduleTask) => {
						OnExportAssembly(moduleTask, args.SaveDirectory);
					}, TaskScheduler.FromCurrentSynchronizationContext());
				}
			}
			commandLineLoadedAssemblies.Clear(); // clear references once we don't need them anymore
		}
		
		void OnExportAssembly(Task<ModuleDef> moduleTask, string path)
		{
			var textView = ActiveTextView;
			if (textView == null)
				return;
			AssemblyTreeNode asmNode = assemblyListTreeNode.FindModuleNode(moduleTask.Result);
			if (asmNode != null) {
				string file = DecompilerTextView.CleanUpName(asmNode.LoadedAssembly.ShortName);
				Language language = sessionSettings.FilterSettings.Language;
				DecompilationOptions options = new DecompilationOptions();
				options.FullDecompilation = true;
				options.SaveAsProjectDirectory = Path.Combine(App.CommandLineArguments.SaveDirectory, file);
				if (!Directory.Exists(options.SaveAsProjectDirectory)) {
					Directory.CreateDirectory(options.SaveAsProjectDirectory);
				}
				string fullFile = Path.Combine(options.SaveAsProjectDirectory, file + language.ProjectFileExtension);
				textView.SaveToDisk(language, new[] { asmNode }, options, fullFile);
			}
		}

		public void ExecuteWhenLoaded(Action func)
		{
			if (callWhenLoaded == null)
				func();
			else
				callWhenLoaded.Add(func);
		}
		List<Action> callWhenLoaded = new List<Action>();

		void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			debug_CommandBindings_Count = this.CommandBindings.Count;
			ContextMenuProvider.Add(treeView);
			Themes.Theme = Themes.GetThemeOrDefault(sessionSettings.ThemeName);

			ILSpySettings spySettings = this.spySettings;
			this.spySettings = null;
			
			// Load AssemblyList only in Loaded event so that WPF is initialized before we start the CPU-heavy stuff.
			// This makes the UI come up a bit faster.
			this.assemblyList = assemblyListManager.LoadList(spySettings, sessionSettings.ActiveAssemblyList);
			
			HandleCommandLineArguments(App.CommandLineArguments);
			
			if (assemblyList.GetAssemblies().Length == 0
				&& assemblyList.ListName == AssemblyListManager.DefaultListName)
			{
				LoadInitialAssemblies();
			}
			
			ShowAssemblyList(this.assemblyList);
			
			HandleCommandLineArgumentsAfterShowList(App.CommandLineArguments);
			if (App.CommandLineArguments.NavigateTo == null && App.CommandLineArguments.AssembliesToLoad.Count != 1) {
				if (ICSharpCode.ILSpy.Options.DisplaySettingsPanel.CurrentDisplaySettings.RestoreTabsAtStartup) {
					RestoreTabGroups(sessionSettings.SavedTabGroupsState);
					if (!sessionSettings.TabsFound)
						AboutPage.Display(SafeActiveTextView);
				}
				else {
					AboutPage.Display(SafeActiveTextView);
				}
			}
			
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			if (FormatExceptions(App.StartupExceptions.ToArray(), output))
				SafeActiveTextView.ShowText(output);

			if (topPane.Content == null) {
				var pane = GetPane(topPane, sessionSettings.TopPaneSettings.Name);
				if (pane != null)
					ShowInTopPane(pane.PaneTitle, pane);
			}
			if (bottomPane.Content == null) {
				var pane = GetPane(bottomPane, sessionSettings.BottomPaneSettings.Name);
				if (pane != null)
					ShowInBottomPane(pane.PaneTitle, pane);
			}

			foreach (var plugin in plugins)
				plugin.OnLoaded();

			var list = callWhenLoaded;
			callWhenLoaded = null;
			foreach (var func in list)
				func();
		}

		IPane GetPane(DockedPane dockedPane, string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;
			foreach (var creator in paneCreators) {
				var pane = creator.Create(name);
				if (pane != null)
					return pane;
			}
			return null;
		}
		
		bool FormatExceptions(App.ExceptionData[] exceptions, ITextOutput output)
		{
			if (exceptions.Length == 0) return false;
			bool first = true;
			
			foreach (var item in exceptions) {
				if (first)
					first = false;
				else
					output.WriteLine("-------------------------------------------------", TextTokenType.Text);
				output.WriteLine("Error(s) loading plugin: " + item.PluginName, TextTokenType.Text);
				if (item.Exception is System.Reflection.ReflectionTypeLoadException) {
					var e = (System.Reflection.ReflectionTypeLoadException)item.Exception;
					foreach (var ex in e.LoaderExceptions) {
						output.WriteLine(ex.ToString(), TextTokenType.Text);
						output.WriteLine();
					}
				} else
					output.WriteLine(item.Exception.ToString(), TextTokenType.Text);
			}
			
			return true;
		}
		
		#region Update Check
		string updateAvailableDownloadUrl;
		
		void ShowMessageIfUpdatesAvailableAsync(ILSpySettings spySettings)
		{
			AboutPage.CheckForUpdatesIfEnabledAsync(spySettings).ContinueWith(
				delegate (Task<string> task) {
					if (task.Result != null) {
						updateAvailableDownloadUrl = task.Result;
						updateAvailablePanel.Visibility = Visibility.Visible;
					}
				},
				TaskScheduler.FromCurrentSynchronizationContext()
			);
		}
		
		void updateAvailablePanelCloseButtonClick(object sender, RoutedEventArgs e)
		{
			updateAvailablePanel.Visibility = Visibility.Collapsed;
		}
		
		void downloadUpdateButtonClick(object sender, RoutedEventArgs e)
		{
			Process.Start(updateAvailableDownloadUrl);
		}
		#endregion
		
		public void ShowAssemblyList(string name)
		{
			ILSpySettings settings = this.spySettings;
			if (settings == null)
			{
				settings = ILSpySettings.Load();
			}
			AssemblyList list = this.assemblyListManager.LoadList(settings, name);
			//Only load a new list when it is a different one
			if (list.ListName != CurrentAssemblyList.ListName)
			{
				ShowAssemblyList(list);
			}
		}
		
		void ShowAssemblyList(AssemblyList assemblyList)
		{
			// Clear the cache since the keys contain tree nodes which get recreated now. The keys
			// will never match again so shouldn't be in the cache.
			DecompileCache.Instance.ClearAll();

			foreach (var tabManager in tabGroupsManager.AllTabGroups.ToArray())
				tabManager.RemoveAllTabStates();
			this.assemblyList = assemblyList;

			// Make sure memory usage doesn't increase out of control. This method allocates lots of
			// new stuff, but the GC doesn't bother to reclaim that memory for a long time.
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			assemblyList.CollectionChanged += assemblyList_Assemblies_CollectionChanged;
			
			assemblyListTreeNode = new AssemblyListTreeNode(assemblyList);
			assemblyListTreeNode.FilterSettings = sessionSettings.FilterSettings.Clone();
			assemblyListTreeNode.Select = SelectNode;
			treeView.Root = assemblyListTreeNode;
			
			if (assemblyList.ListName == AssemblyListManager.DefaultListName)
				this.Title = string.Format("dnSpy ({0})", GetCpuType());
			else
				this.Title = string.Format("dnSpy ({0}) - {1}", GetCpuType(), assemblyList.ListName);
		}

		static string GetCpuType()
		{
			return IntPtr.Size == 4 ? "x86" : "x64";
		}
		
		void assemblyList_Assemblies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Reset) {
				foreach (var tabManager in tabGroupsManager.AllTabGroups)
					tabManager.RemoveAllTabStates();
			}
			if (e.OldItems != null) {
				var oldAssemblies = new HashSet<LoadedAssembly>(e.OldItems.Cast<LoadedAssembly>());
				foreach (var tabState in AllTabStates.ToArray()) {
					tabState.History.RemoveAll(n => n.TreeNodes.Any(
						nd => nd.AncestorsAndSelf().OfType<AssemblyTreeNode>().Any(
							a => oldAssemblies.Contains(a.LoadedAssembly))));

					foreach (var node in tabState.DecompiledNodes) {
						var asmNode = GetAssemblyTreeNode(node);
						if (asmNode != null && oldAssemblies.Contains(asmNode.LoadedAssembly)) {
							var tabManager = (TabManager<TabStateDecompile>)tabState.Owner;
							tabManager.RemoveTabState(tabState);
							break;
						}
					}
				}
				DecompileCache.Instance.Clear(oldAssemblies);
			}
			if (CurrentAssemblyListChanged != null)
				CurrentAssemblyListChanged(this, e);
		}
		
		void LoadInitialAssemblies()
		{
			// Called when loading an empty assembly list; so that
			// the user can see something initially.
			System.Reflection.Assembly[] initialAssemblies = {
				typeof(object).Assembly,
				typeof(Uri).Assembly,
				typeof(System.Linq.Enumerable).Assembly,
				typeof(System.Xml.XmlDocument).Assembly,
				typeof(System.Windows.Markup.MarkupExtension).Assembly,
				typeof(System.Windows.Rect).Assembly,
				typeof(System.Windows.UIElement).Assembly,
				typeof(System.Windows.FrameworkElement).Assembly,
				typeof(ICSharpCode.TreeView.SharpTreeView).Assembly,
				typeof(dnlib.DotNet.ModuleDefMD).Assembly,
				typeof(ICSharpCode.AvalonEdit.TextEditor).Assembly,
				typeof(ICSharpCode.Decompiler.Ast.AstBuilder).Assembly,
				typeof(MainWindow).Assembly
			};
			foreach (System.Reflection.Assembly asm in initialAssemblies)
				assemblyList.OpenAssembly(asm.Location);
		}

		void filterSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			RefreshTreeViewFilter();
			if (e.PropertyName == "Language") {
				var tabState = ActiveTabState;
				if (tabState != null)
					DecompileNodes(tabState, null, false, sessionSettings.FilterSettings.Language, tabState.DecompiledNodes);
			}
		}

		void SetLanguage(Language language)
		{
			languageComboBox.SelectedItem = language;
		}
		
		public void RefreshTreeViewFilter()
		{
			// filterSettings is mutable; but the ILSpyTreeNode filtering assumes that filter settings are immutable.
			// Thus, the main window will use one mutable instance (for data-binding), and assign a new clone to the ILSpyTreeNodes whenever the main
			// mutable instance changes.
			if (assemblyListTreeNode != null)
				assemblyListTreeNode.FilterSettings = sessionSettings.FilterSettings.Clone();
		}
		
		internal AssemblyListTreeNode AssemblyListTreeNode {
			get { return assemblyListTreeNode; }
		}
		
		#region Node Selection

		public void SelectNode(SharpTreeNode obj)
		{
			if (obj != null) {
				if (!obj.AncestorsAndSelf().Any(node => node.IsHidden)) {
					// Set both the selection and focus to ensure that keyboard navigation works as expected.
					treeView.FocusNode(obj);
					treeView.SelectedItem = obj;
				} else {
					MessageBox.Show("Navigation failed because the target is hidden or a compiler-generated class.\n" +
						"Please disable all filters that might hide the item (i.e. activate " +
						"\"View > Show Internal Types and Members\") and try again.",
						"dnSpy", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				}
			}
		}
		
		/// <summary>
		/// Retrieves a node using the NodePathName property of its ancestors.
		/// </summary>
		public ILSpyTreeNode FindNodeByPath(FullNodePathName fullPath)
		{
			var node = (ILSpyTreeNode)treeView.Root;
			foreach (var name in fullPath.Names) {
				if (node == null)
					break;
				node.EnsureChildrenFiltered();
				node = (ILSpyTreeNode)node.Children.FirstOrDefault(c => ((ILSpyTreeNode)c).NodePathName == name);
			}
			return node == treeView.Root ? null : node;
		}
		
		/// <summary>
		/// Gets full node path name of the node's ancestors.
		/// </summary>
		public static FullNodePathName GetPathForNode(ILSpyTreeNode node)
		{
			var fullPath = new FullNodePathName();
			if (node == null)
				return fullPath;
			while (node.Parent != null) {
				fullPath.Names.Add(node.NodePathName);
				node = (ILSpyTreeNode)node.Parent;
			}
			fullPath.Names.Reverse();
			return fullPath;
		}
		
		public ILSpyTreeNode FindTreeNode(object reference)
		{
			if (reference is ITypeDefOrRef) {
				return assemblyListTreeNode.FindTypeNode(((ITypeDefOrRef)reference).ResolveTypeDef());
			} else if (reference is IMethod && ((IMethod)reference).MethodSig != null) {
				return assemblyListTreeNode.FindMethodNode(((IMethod)reference).Resolve());
			} else if (reference is IField) {
				return assemblyListTreeNode.FindFieldNode(((IField)reference).Resolve());
			} else if (reference is PropertyDef) {
				return assemblyListTreeNode.FindPropertyNode((PropertyDef)reference);
			} else if (reference is EventDef) {
				return assemblyListTreeNode.FindEventNode((EventDef)reference);
			} else if (reference is AssemblyDef) {
				return assemblyListTreeNode.FindAssemblyNode((AssemblyDef)reference);
			} else if (reference is ModuleDef) {
				return assemblyListTreeNode.FindModuleNode((ModuleDef)reference);
			} else {
				return null;
			}
		}

		internal static IMemberDef ResolveReference(object reference) {
			if (reference is ITypeDefOrRef)
				return ((ITypeDefOrRef)reference).ResolveTypeDef();
			else if (reference is IMethod && ((IMethod)reference).MethodSig != null)
				return ((IMethod)reference).Resolve();
			else if (reference is IField)
				return ((IField)reference).Resolve();
			else if (reference is PropertyDef)
				return (PropertyDef)reference;
			else if (reference is EventDef)
				return (EventDef)reference;
			return null;
		}

		object FixReference(object reference)
		{
			IMemberDef member = ResolveReference(reference);
			if (member != null && ICSharpCode.ILSpy.Options.DisplaySettingsPanel.CurrentDisplaySettings.DecompileFullType) {
				var type = member.DeclaringType;
				if (type == null)
					reference = member;
				else {
					for (int i = 0; i < 100; i++) {
						var declType = type.DeclaringType;
						if (declType == null)
							break;
						type = declType;
					}
					reference = type;
				}
			}

			return reference;
		}

		public bool JumpToReference(object reference, bool canRecordHistory = true)
		{
			var tabState = SafeActiveTabState;
			if (canRecordHistory)
				RecordHistory(tabState);
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), (success, hasMovedCaret) => GoToLocation(tabState.TextView, success, hasMovedCaret, ResolveReference(reference)));
		}

		public bool JumpToReference(DecompilerTextView textView, object reference, bool canRecordHistory = true)
		{
			var tabState = TabStateDecompile.GetTabStateDecompile(textView);
			if (canRecordHistory)
				RecordHistory(tabState);
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), (success, hasMovedCaret) => GoToLocation(tabState.TextView, success, hasMovedCaret, ResolveReference(reference)));
		}

		public bool JumpToReference(DecompilerTextView textView, object reference, Func<TextLocation> getLocation, bool canRecordHistory = true)
		{
			var tabState = TabStateDecompile.GetTabStateDecompile(textView);
			if (canRecordHistory)
				RecordHistory(tabState);
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), (success, hasMovedCaret) => GoToLocation(tabState.TextView, success, hasMovedCaret, getLocation()));
		}

		public bool JumpToReference(DecompilerTextView textView, object reference, Func<bool, bool, bool> onDecompileFinished, bool canRecordHistory = true)
		{
			var tabState = TabStateDecompile.GetTabStateDecompile(textView);
			if (canRecordHistory)
				RecordHistory(tabState);
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), onDecompileFinished);
		}

		bool GoToLocation(DecompilerTextView decompilerTextView, bool success, bool hasMovedCaret, object destLoc)
		{
			if (!success || destLoc == null)
				return false;
			return decompilerTextView.GoToLocation(destLoc);
		}

		sealed class OnShowOutputHelper
		{
			DecompilerTextView decompilerTextView;
			readonly Func<bool, bool, bool> onDecompileFinished;
			readonly ILSpyTreeNode[] nodes;
			public OnShowOutputHelper(DecompilerTextView decompilerTextView, Func<bool, bool, bool> onDecompileFinished, ILSpyTreeNode node)
				: this(decompilerTextView, onDecompileFinished, new[] { node })
			{
			}

			public OnShowOutputHelper(DecompilerTextView decompilerTextView, Func<bool, bool, bool> onDecompileFinished, ILSpyTreeNode[] nodes)
			{
				this.decompilerTextView = decompilerTextView;
				this.onDecompileFinished = onDecompileFinished;
				this.nodes = nodes;
				decompilerTextView.OnShowOutput += OnShowOutput;
			}

			public void OnShowOutput(object sender, DecompilerTextView.ShowOutputEventArgs e)
			{
				decompilerTextView.OnShowOutput -= OnShowOutput;
				bool success = Equals(e.Nodes, nodes);
				if (onDecompileFinished != null)
					e.HasMovedCaret |= onDecompileFinished(success, e.HasMovedCaret);
			}

			static bool Equals(ILSpyTreeNode[] a, ILSpyTreeNode[] b)
			{
				if (a == b)
					return true;
				if (a == null || b == null)
					return false;
				if (a.Length != b.Length)
					return false;
				for (int i = 0; i < a.Length; i++) {
					if (a[i] != b[i])
						return false;
				}
				return true;
			}

			public void Abort()
			{
				decompilerTextView.OnShowOutput -= OnShowOutput;
			}
		}

		// Returns true if we could decompile the reference
		bool JumpToReferenceAsyncInternal(TabStateDecompile tabState, bool canLoad, object reference, Func<bool, bool, bool> onDecompileFinished)
		{
			ILSpyTreeNode treeNode = FindTreeNode(reference);
			if (treeNode != null) {
				var helper = new OnShowOutputHelper(tabState.TextView, onDecompileFinished, treeNode);
				var nodes = new[] { treeNode };
				bool? decompiled = DecompileNodes(tabState, null, false, tabState.Language, nodes);
				if (decompiled == false) {
					helper.Abort();
					onDecompileFinished(true, false);
				}
				SelectTreeViewNodes(tabState, nodes);
				return true;
			} else if (reference is dnlib.DotNet.Emit.OpCode) {
				string link = "http://msdn.microsoft.com/library/system.reflection.emit.opcodes." + ((dnlib.DotNet.Emit.OpCode)reference).Code.ToString().ToLowerInvariant() + ".aspx";
				try {
					Process.Start(link);
				} catch {
					
				}
				return true;
			} else if (canLoad && reference is IMemberDef) {
				// Here if the module was removed. It's possible that the user has re-added it.

				var member = (IMemberDef)reference;
				var module = member.Module;
				var mainModule = module;
				if (module.Assembly != null)
					mainModule = module.Assembly.ManifestModule;
				// Check if the module was removed and then added again
				foreach (var m in assemblyList.GetAllModules()) {
					if (mainModule.Location.Equals(m.Location, StringComparison.OrdinalIgnoreCase)) {
						foreach (var asmMod in GetAssemblyModules(m)) {
							if (!module.Location.Equals(asmMod.Location, StringComparison.OrdinalIgnoreCase))
								continue;

							// Found the module
							var modDef = asmMod as ModuleDefMD;
							if (modDef != null) {
								member = modDef.ResolveToken(member.MDToken) as IMemberDef;
								if (member != null) // should never fail
									return JumpToReferenceAsyncInternal(tabState, false, member, onDecompileFinished);
							}

							break;
						}

						return false;
					}
				}

				// The module has been removed. Add it again
				var loadedAsm = new LoadedAssembly(assemblyList, mainModule);
				loadedAsm.IsAutoLoaded = true;
				assemblyList.AddAssembly(loadedAsm, true, false);
				return JumpToReferenceAsyncInternal(tabState, false, reference, onDecompileFinished);
			}
			else
				return false;
		}

		IEnumerable<ModuleDef> GetAssemblyModules(ModuleDef module)
		{
			if (module == null)
				yield break;
			var asm = module.Assembly;
			if (asm == null)
				yield return module;
			else {
				foreach (var mod in asm.Modules)
					yield return mod;
			}
		}
		#endregion
		
		#region Open/Refresh
		void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			e.Handled = true;
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = ".NET assemblies|*.dll;*.exe;*.winmd|All files|*.*";
			dlg.Multiselect = true;
			dlg.RestoreDirectory = true;
			if (dlg.ShowDialog() == true) {
				OpenFiles(dlg.FileNames);
			}
		}
		
		public void OpenFiles(string[] fileNames, bool focusNode = true)
		{
			if (fileNames == null)
				throw new ArgumentNullException("fileNames");
			
			if (focusNode)
				treeView.UnselectAll();
			
			SharpTreeNode lastNode = null;
			foreach (string file in fileNames) {
				var asm = assemblyList.OpenAssembly(file);
				if (asm != null) {
					var node = assemblyListTreeNode.FindAssemblyNode(asm);
					if (node != null && focusNode) {
						treeView.SelectedItems.Add(node);
						lastNode = node;
					}
				}
				if (lastNode != null && focusNode)
					treeView.FocusNode(lastNode);
			}
		}
		
		void RefreshCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var savedState = CreateSavedTabGroupsState();

			try {
				TreeView_SelectionChanged_ignore = true;
				ShowAssemblyList(assemblyListManager.LoadList(ILSpySettings.Load(), assemblyList.ListName));
			}
			finally {
				TreeView_SelectionChanged_ignore = false;
			}

			RestoreTabGroups(savedState);
		}

		private void RefreshCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			var cd = DebuggerService.CurrentDebugger;
			e.CanExecute = cd == null || !cd.IsDebugging;
		}
		
		void SearchCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			SearchPane.Instance.Show();
		}
		#endregion
		
		#region Decompile (TreeView_SelectionChanged)
		//TODO: HACK alert
		bool TreeView_SelectionChanged_ignore = false;
		void TreeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TreeView_SelectionChanged_ignore)
				return;
			var tabState = SafeActiveTabState;
			DecompileNodes(tabState, null, true, tabState.Language, this.SelectedNodes.ToArray());

			if (SelectionChanged != null)
				SelectionChanged(sender, e);

			if (ICSharpCode.ILSpy.Options.DisplaySettingsPanel.CurrentDisplaySettings.AutoFocusTextView)
				SetTextEditorFocus(tabState.TextView);
		}
		
		bool? DecompileNodes(TabStateDecompile tabState, DecompilerTextViewState state, bool recordHistory, Language language, ILSpyTreeNode[] nodes, bool forceDecompile = false)
		{
			if (tabState.ignoreDecompilationRequests)
				return null;
			if (tabState.HasDecompiled && !forceDecompile && tabState.Equals(nodes, language)) {
				if (state != null)
					tabState.TextView.EditorPositionState = state.EditorPositionState;
				return false;
			}
			tabState.HasDecompiled = true;
			tabState.Language = language;
			tabState.DecompiledNodes = nodes ?? new ILSpyTreeNode[0];
			tabState.Title = null;
			InitializeHeader(tabState);
			
			if (recordHistory)
				RecordHistory(tabState);

			if (nodes.Length == 1 && nodes[0].View(tabState.TextView)) {
				tabState.TextView.CancelDecompileAsync();
				return true;
			}
			tabState.TextView.DecompileAsync(language, nodes, new DecompilationOptions() { TextViewState = state, DecompilerTextView = tabState.TextView });
			return true;
		}

		internal void RecordHistory(DecompilerTextView textView)
		{
			RecordHistory(TabStateDecompile.GetTabStateDecompile(textView));
		}

		void RecordHistory(TabStateDecompile tabState)
		{
			if (tabState == null)
				return;
			var dtState = tabState.TextView.GetState();
			if (dtState != null)
				tabState.History.UpdateCurrent(new NavigationState(dtState, tabState.Language));
			tabState.History.Record(new NavigationState(tabState.DecompiledNodes, tabState.Language));
		}
		
		void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			Save(ActiveTabState);
		}

		internal void Save(TabStateDecompile tabState)
		{
			if (tabState == null)
				return;
			var textView = tabState.TextView;
			if (tabState.DecompiledNodes.Length == 1) {
				if (tabState.DecompiledNodes[0].Save(textView))
					return;
			}
			textView.SaveToDisk(tabState.Language,
				tabState.DecompiledNodes,
				new DecompilationOptions() { FullDecompilation = true });
		}
		
		public Language CurrentLanguage {
			get {
				var tabState = ActiveTabState;
				if (tabState != null)
					return tabState.Language;
				return sessionSettings.FilterSettings.Language;
			}
		}

		public event SelectionChangedEventHandler SelectionChanged;

		IEnumerable<ILSpyTreeNode> SelectedNodes {
			get {
				return treeView.GetTopLevelSelection().OfType<ILSpyTreeNode>();
			}
		}
		#endregion
		
		#region Back/Forward navigation
		void BackCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			var tabState = ActiveTabState;
			e.Handled = true;
			e.CanExecute = tabState != null && tabState.History.CanNavigateBack;
		}
		
		void BackCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (BackCommand(ActiveTabState))
				e.Handled = true;
		}

		internal void BackCommand(DecompilerTextView textView)
		{
			BackCommand(TabStateDecompile.GetTabStateDecompile(textView));
		}

		bool BackCommand(TabStateDecompile tabState)
		{
			if (tabState == null)
				return false;
			if (tabState.History.CanNavigateBack) {
				NavigateHistory(tabState, false);
				return true;
			}
			return false;
		}
		
		void ForwardCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			var tabState = ActiveTabState;
			e.Handled = true;
			e.CanExecute = tabState != null && tabState.History.CanNavigateForward;
		}
		
		void ForwardCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var tabState = ActiveTabState;
			if (tabState == null)
				return;
			if (tabState.History.CanNavigateForward) {
				e.Handled = true;
				NavigateHistory(tabState, true);
			}
		}
		
		void NavigateHistory(TabStateDecompile tabState, bool forward)
		{
			var dtState = tabState.TextView.GetState();
			if(dtState != null)
				tabState.History.UpdateCurrent(new NavigationState(dtState, tabState.Language));
			var newState = forward ? tabState.History.GoForward() : tabState.History.GoBack();
			var nodes = newState.TreeNodes.Cast<ILSpyTreeNode>().ToArray();
			SelectTreeViewNodes(tabState, nodes);
			DecompileNodes(tabState, newState.ViewState, false, newState.Language, nodes);
			SetLanguage(newState.Language);
		}
		
		#endregion
		
		protected override void OnStateChanged(EventArgs e)
		{
			base.OnStateChanged(e);
			// store window state in settings only if it's not minimized
			if (this.WindowState != System.Windows.WindowState.Minimized)
				sessionSettings.WindowState = this.WindowState;
		}
		
		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			sessionSettings.ThemeName = Themes.Theme.Name;
			sessionSettings.ActiveAssemblyList = assemblyList.ListName;
			sessionSettings.WindowBounds = this.RestoreBounds;
			sessionSettings.LeftColumnWidth = leftColumn.Width.Value;
			sessionSettings.TopPaneSettings = GetPaneSettings(topPane, topPaneRow);
			sessionSettings.BottomPaneSettings = GetPaneSettings(bottomPane, bottomPaneRow);
			sessionSettings.SavedTabGroupsState = CreateSavedTabGroupsState();
			sessionSettings.Save();
		}

		void RestoreTabGroups(SavedTabGroupsState savedGroups)
		{
			Debug.Assert(tabGroupsManager.AllTabGroups.Count == 1);
			bool first = true;
			foreach (var savedGroupState in savedGroups.Groups) {
				var tabManager = first ? tabGroupsManager.ActiveTabGroup : tabGroupsManager.CreateTabGroup(savedGroups.IsHorizontal);
				first = false;
				foreach (var savedTabState in savedGroupState.Tabs) {
					var tabState = CreateNewTabState(tabManager, Languages.GetLanguage(savedTabState.Language));
					CreateTabState(tabState, savedTabState);
				}

				tabManager.SetSelectedIndex(savedGroupState.Index);
			}

			tabGroupsManager.SetSelectedIndex(savedGroups.Index);
		}

		SavedTabGroupsState CreateSavedTabGroupsState()
		{
			var state = new SavedTabGroupsState();
			state.IsHorizontal = tabGroupsManager.IsHorizontal;
			state.Index = tabGroupsManager.ActiveIndex;
			foreach (var tabManager in tabGroupsManager.AllTabGroups)
				state.Groups.Add(CreateSavedTabGroupState(tabManager));
			return state;
		}

		static SavedTabGroupState CreateSavedTabGroupState(TabManager<TabStateDecompile> tabManager)
		{
			var savedState = new SavedTabGroupState();

			savedState.Index = tabManager.ActiveIndex;

			foreach (var tabState in tabManager.AllTabStates)
				savedState.Tabs.Add(CreateSavedTabState(tabState));

			return savedState;
		}

		static SavedTabState CreateSavedTabState(TabStateDecompile tabState)
		{
			var savedState = new SavedTabState();
			savedState.Language = tabState.Language.Name;
			savedState.Paths = new List<FullNodePathName>();
			savedState.ActiveAutoLoadedAssemblies = new List<string>();
			foreach (var node in tabState.DecompiledNodes) {
				savedState.Paths.Add(GetPathForNode(node));
				var autoAsm = GetAutoLoadedAssemblyNode(node);
				if (!string.IsNullOrEmpty(autoAsm))
					savedState.ActiveAutoLoadedAssemblies.Add(autoAsm);
			}
			savedState.EditorPositionState = tabState.TextView.EditorPositionState;
			return savedState;
		}

		TabStateDecompile CreateNewTabState(Language language = null)
		{
			return CreateNewTabState(tabGroupsManager.ActiveTabGroup, language);
		}

		TabStateDecompile CreateEmptyTabState(Language language = null)
		{
			var tabState = CreateNewTabState(tabGroupsManager.ActiveTabGroup, language);
			DecompileNodes(tabState, null, true, tabState.Language, new ILSpyTreeNode[0]);
			return tabState;
		}

		TabStateDecompile CreateNewTabState(TabManager<TabStateDecompile> tabManager, Language language = null)
		{
			var tabState = new TabStateDecompile(language ?? sessionSettings.FilterSettings.Language);
			return tabManager.AddNewTabState(tabState);
		}

		TabStateDecompile CreateTabState(SavedTabState savedState, IList<ILSpyTreeNode> newNodes = null, bool decompile = true)
		{
			var tabState = CreateNewTabState(Languages.GetLanguage(savedState.Language));
			return CreateTabState(tabState, savedState, newNodes, decompile);
		}

		TabStateDecompile CreateTabState(TabStateDecompile tabState, SavedTabState savedState, IList<ILSpyTreeNode> newNodes = null, bool decompile = true)
		{
			var nodes = new List<ILSpyTreeNode>(savedState.Paths.Count);
			if (newNodes != null)
				nodes.AddRange(newNodes);
			else {
				foreach (var asm in savedState.ActiveAutoLoadedAssemblies)
					this.assemblyList.OpenAssembly(asm, true);
				foreach (var path in savedState.Paths) {
					var node = FindNodeByPath(path);
					if (node == null) {
						nodes = null;
						break;
					}
					nodes.Add(node);
				}
			}
			if (decompile) {
				if (nodes != null) {
					var tmpNodes = nodes.ToArray();
					var helper = new OnShowOutputHelper(tabState.TextView, (success, hasMovedCaret) => decompilerTextView_OnShowOutput(success, hasMovedCaret, tabState.TextView, savedState), tmpNodes);
					DecompileNodes(tabState, null, true, tabState.Language, tmpNodes);
				}
				else
					AboutPage.Display(tabState.TextView);
			}

			return tabState;
		}

		bool decompilerTextView_OnShowOutput(bool success, bool hasMovedCaret, DecompilerTextView textView, SavedTabState savedState)
		{
			if (!success)
				return false;

			if (IsValid(textView, savedState.EditorPositionState)) {
				textView.EditorPositionState = savedState.EditorPositionState;
				return true;
			}

			return false;
		}

		bool IsValid(DecompilerTextView decompilerTextView, EditorPositionState state)
		{
			if (state.VerticalOffset < 0 || state.HorizontalOffset < 0)
				return false;
			if (state.DesiredXPos < 0)
				return false;
			if (state.TextViewPosition.Line < 1 || state.TextViewPosition.Column < 1)
				return false;
			if (state.TextViewPosition.VisualColumn < -1)
				return false;

			if (state.TextViewPosition.Line > decompilerTextView.TextEditor.LineCount)
				return false;

			return true;
		}

		static SessionSettings.PaneSettings GetPaneSettings(DockedPane dockedPane, RowDefinition row)
		{
			var settings = new SessionSettings.PaneSettings();

			if (dockedPane.Visibility == Visibility.Visible)
				settings.Height = row.Height.Value;
			else
				settings.Height = dockedPane.Height;
			if (double.IsNaN(settings.Height))
				settings.Height = 250;

			settings.Name = GetPaneName(dockedPane);

			return settings;
		}

		static string GetPaneName(DockedPane dockedPane)
		{
			var pane = dockedPane.Content as IPane;
			return pane == null ? string.Empty : pane.PaneName;
		}

		internal static AssemblyTreeNode GetAssemblyTreeNode(SharpTreeNode node)
		{
			if (node == null)
				return null;
			while (!(node is TreeNodes.AssemblyTreeNode) && node.Parent != null) {
				node = node.Parent;
			}
			if (node.Parent is AssemblyTreeNode)
				node = node.Parent;
			return node as AssemblyTreeNode;
		}

		static string GetAutoLoadedAssemblyNode(SharpTreeNode node)
		{
			var assyNode = GetAssemblyTreeNode(node);
			if (assyNode == null)
				return null;
			var loadedAssy = assyNode.LoadedAssembly;
			if (!(loadedAssy.IsLoaded && loadedAssy.IsAutoLoaded))
				return null;

			return loadedAssy.FileName;
		}
		
		#region Top/Bottom Pane management
		public void ShowInTopPane(string title, object content)
		{
			topPaneRow.MinHeight = 100;
			if (sessionSettings.TopPaneSettings.Height > 0)
				topPaneRow.Height = new GridLength(sessionSettings.TopPaneSettings.Height, GridUnitType.Pixel);
			topPane.Title = title;
			if (topPane.Content != content) {
				IPane pane = topPane.Content as IPane;
				if (pane != null)
					pane.Closed();
				topPane.Content = content;
			}
			topPane.Visibility = Visibility.Visible;
			if (content is IPane)
				((IPane)content).Opened();
		}
		
		void TopPane_CloseButtonClicked(object sender, EventArgs e)
		{
			CloseTopPane();
		}

		public void CloseTopPane()
		{
			sessionSettings.TopPaneSettings.Height = topPaneRow.Height.Value;
			topPaneRow.MinHeight = 0;
			topPaneRow.Height = new GridLength(0);
			topPane.Visibility = Visibility.Collapsed;
			
			IPane pane = topPane.Content as IPane;
			topPane.Content = null;
			if (pane != null)
				pane.Closed();
		}

		public object TopPaneContent {
			get { return topPane.Content; }
		}
		
		public void ShowInBottomPane(string title, object content)
		{
			bottomPaneRow.MinHeight = 100;
			if (sessionSettings.BottomPaneSettings.Height > 0)
				bottomPaneRow.Height = new GridLength(sessionSettings.BottomPaneSettings.Height, GridUnitType.Pixel);
			bottomPane.Title = title;
			if (bottomPane.Content != content) {
				IPane pane = bottomPane.Content as IPane;
				if (pane != null)
					pane.Closed();
				bottomPane.Content = content;
			}
			bottomPane.Visibility = Visibility.Visible;
			if (content is IPane)
				((IPane)content).Opened();
		}
		
		void BottomPane_CloseButtonClicked(object sender, EventArgs e)
		{
			CloseBottomPane();
		}

		public void CloseBottomPane()
		{
			sessionSettings.BottomPaneSettings.Height = bottomPaneRow.Height.Value;
			bottomPaneRow.MinHeight = 0;
			bottomPaneRow.Height = new GridLength(0);
			bottomPane.Visibility = Visibility.Collapsed;
			
			IPane pane = bottomPane.Content as IPane;
			bottomPane.Content = null;
			if (pane != null)
				pane.Closed();
		}

		public object BottomPaneContent {
			get { return bottomPane.Content; }
		}
		#endregion
		
		public void UnselectAll()
		{
			treeView.UnselectAll();
		}
		
		public void SetStatus(string status, Brush foreground)
		{
			if (this.statusBar.Visibility == Visibility.Collapsed)
				this.statusBar.Visibility = Visibility.Visible;
			this.StatusLabel.Foreground = foreground;
			this.StatusLabel.Text = status;
		}

		public void HideStatus()
		{
			this.statusBar.Visibility = Visibility.Collapsed;
		}
		
		public ItemCollection GetMainMenuItems()
		{
			return mainMenu.Items;
		}
		
		public ItemCollection GetToolBarItems()
		{
			return toolBar.Items;
		}

		public LoadedAssembly LoadAssembly(string asmFilename, string moduleFilename)
		{
			lock (assemblyList.GetLockObj()) {
				// Get or create the assembly
				var loadedAsm = assemblyList.OpenAssemblyDelay(asmFilename, true);

				// Common case is a one-file assembly or first module of a multifile assembly
				if (asmFilename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
					return loadedAsm;

				var loadedMod = assemblyListTreeNode.FindModule(loadedAsm, moduleFilename);
				if (loadedMod != null)
					return loadedMod;

				Debug.Fail("Shouldn't be here.");
				return assemblyList.OpenAssemblyDelay(moduleFilename, true);
			}
		}

		private void GoToTokenExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			GoToTokenContextMenuEntry.Execute();
		}

		private void GoToTokenCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = GoToTokenContextMenuEntry.CanExecute();
		}

		private void GoToLineExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var decompilerTextView = ActiveTextView;
			if (decompilerTextView == null)
				return;

			var ask = new AskForInput();
			ask.Owner = this;
			ask.Title = "Go to Line";
			ask.textBlock.Text = "Line [, column]";
			ask.textBox.Text = "";
			ask.ToolTip = "Enter a line and/or column\n10 => line 10, column 1\n,5 => column 5\n10,5 => line 10, column 5";
			ask.ShowDialog();
			if (ask.DialogResult != true)
				return;

			string lineText = ask.textBox.Text;
			int? line = null, column = null;
			Match match;
			if ((match = goToLineRegex1.Match(lineText)) != null && match.Groups.Count == 4) {
				line = TryParse(match.Groups[1].Value);
				column = match.Groups[3].Value != string.Empty ? TryParse(match.Groups[3].Value) : 1;
			}
			else if ((match = goToLineRegex2.Match(lineText)) != null && match.Groups.Count == 2) {
				line = decompilerTextView.TextEditor.TextArea.Caret.Line;
				column = TryParse(match.Groups[1].Value);
			}
			if (line == null || column == null) {
				MessageBox.Show(this, string.Format("Invalid line: {0}", lineText));
				return;
			}
			decompilerTextView.ScrollAndMoveCaretTo(line.Value, column.Value);
		}
		static readonly Regex goToLineRegex1 = new Regex(@"^\s*(\d+)\s*(,\s*(\d+))?\s*$");
		static readonly Regex goToLineRegex2 = new Regex(@"^\s*,\s*(\d+)\s*$");

		static int? TryParse(string valText)
		{
			int val;
			return int.TryParse(valText, out val) ? (int?)val : null;
		}

		internal Language GetLanguage(DecompilerTextView textView)
		{
			return TabStateDecompile.GetTabStateDecompile(textView).Language;
		}

		private void OpenNewTabExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			OpenNewTab();
		}

		private void CloseActiveTabExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			CloseActiveTab();
		}

		private void CloseActiveTabCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = CloseActiveTabCanExecute();
		}

		private void SelectNextTabExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			tabGroupsManager.ActiveTabGroup.SelectNextTab();
		}

		private void SelectNextTabCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = tabGroupsManager.ActiveTabGroup.SelectNextTabCanExecute();
		}

		private void SelectPrevTabExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			tabGroupsManager.ActiveTabGroup.SelectPreviousTab();
		}

		private void SelectPrevTabCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = tabGroupsManager.ActiveTabGroup.SelectPreviousTabCanExecute();
		}

		internal TabStateDecompile CloneTab(TabStateDecompile tabState, bool decompile = true)
		{
			if (tabState == null)
				return null;
			return CreateTabState(CreateSavedTabState(tabState), tabState.DecompiledNodes, decompile);
		}

		internal TabStateDecompile CloneTabMakeActive(TabStateDecompile tabState, bool decompile = true)
		{
			var clonedTabState = CloneTab(tabState, decompile);
			if (clonedTabState != null)
				tabGroupsManager.ActiveTabGroup.SetSelectedTab(clonedTabState);
			return clonedTabState;
		}

		internal void OpenNewTab()
		{
			TabStateDecompile tabState;
			var currenTabState = ActiveTabState;
			if (currenTabState != null && !ICSharpCode.ILSpy.Options.DisplaySettingsPanel.CurrentDisplaySettings.NewEmptyTabs)
				tabState = CloneTab(currenTabState);
			else
				tabState = CreateEmptyTabState();

			tabGroupsManager.ActiveTabGroup.SetSelectedTab(tabState);
		}

		internal void OpenReferenceInNewTab(DecompilerTextView textView, ReferenceSegment reference)
		{
			if (textView == null || reference == null)
				return;
			var tabState = TabStateDecompile.GetTabStateDecompile(textView);
			var clonedTabState = CloneTabMakeActive(tabState, true);
			clonedTabState.History.Clear();
			clonedTabState.TextView.GoToTarget(reference, true, false);
		}

		internal void CloseActiveTab()
		{
			tabGroupsManager.ActiveTabGroup.CloseActiveTab();
		}

		internal bool CloseActiveTabCanExecute()
		{
			return tabGroupsManager.ActiveTabGroup.CloseActiveTabCanExecute();
		}

		internal void CloseAllButActiveTab()
		{
			tabGroupsManager.ActiveTabGroup.CloseAllButActiveTab();
		}

		internal bool CloseAllButActiveTabCanExecute()
		{
			return tabGroupsManager.ActiveTabGroup.CloseAllButActiveTabCanExecute();
		}

		internal bool CloseAllTabsCanExecute()
		{
			return tabGroupsManager.CloseAllTabsCanExecute();
		}

		internal void CloseAllTabs()
		{
			tabGroupsManager.CloseAllTabs();
		}

		internal void CloneActiveTab()
		{
			CloneTabMakeActive(ActiveTabState);
		}

		internal bool CloneActiveTabCanExecute()
		{
			return ActiveTabState != null;
		}

		internal void RefreshCodeCSharp(bool disassembleIL, bool decompileILAst, bool decompileCSharp, bool decompileVB)
		{
			if (decompileILAst)
				decompileCSharp = decompileVB = true;
			if (decompileCSharp)
				decompileVB = true;
			if (disassembleIL)
				RefreshCodeIL();
			if (decompileILAst)
				RefreshCodeILAst();
			if (decompileCSharp)
				RefreshCodeCSharp();
			if (decompileVB)
				RefreshCodeVB();
		}

		void RefreshCodeIL()
		{
			foreach (var tabState in AllTabStates) {
				if (tabState.Language.Name == "IL")
					ForceDecompile(tabState);
			}
		}

		void RefreshCodeILAst()
		{
			foreach (var tabState in AllTabStates) {
				if (tabState.Language.Name.StartsWith("ILAst (") && tabState.Language.Name.EndsWith(")"))
					ForceDecompile(tabState);
			}
		}

		void RefreshCodeCSharp()
		{
			foreach (var tabState in AllTabStates) {
				if (tabState.Language.Name == "C#" || tabState.Language.Name.StartsWith("C# - "))
					ForceDecompile(tabState);
			}
		}

		void RefreshCodeVB()
		{
			foreach (var tabState in AllTabStates) {
				if (tabState.Language.Name == "VB")
					ForceDecompile(tabState);
			}
		}

		void ForceDecompile(TabStateDecompile tabState)
		{
			DecompileNodes(tabState, null, false, tabState.Language, tabState.DecompiledNodes, true);
		}

		internal void RefreshTreeViewNodeNames()
		{
			RefreshTreeViewFilter();
			foreach (var tabState in AllTabStates)
				InitializeHeader(tabState);
		}

		internal bool NewHorizontalTabGroupCanExecute()
		{
			return tabGroupsManager.NewHorizontalTabGroupCanExecute();
		}

		internal void NewHorizontalTabGroup()
		{
			tabGroupsManager.NewHorizontalTabGroup();
		}

		internal bool NewVerticalTabGroupCanExecute()
		{
			return tabGroupsManager.NewVerticalTabGroupCanExecute();
		}

		internal void NewVerticalTabGroup()
		{
			tabGroupsManager.NewVerticalTabGroup();
		}

		internal bool MoveToNextTabGroupCanExecute()
		{
			return tabGroupsManager.MoveToNextTabGroupCanExecute();
		}

		internal void MoveToNextTabGroup()
		{
			tabGroupsManager.MoveToNextTabGroup();
		}

		internal bool MoveToPreviousTabGroupCanExecute()
		{
			return tabGroupsManager.MoveToPreviousTabGroupCanExecute();
		}

		internal void MoveToPreviousTabGroup()
		{
			tabGroupsManager.MoveToPreviousTabGroup();
		}

		internal bool MoveAllToNextTabGroupCanExecute()
		{
			return tabGroupsManager.MoveAllToNextTabGroupCanExecute();
		}

		internal void MoveAllToNextTabGroup()
		{
			tabGroupsManager.MoveAllToNextTabGroup();
		}

		internal bool MoveAllToPreviousTabGroupCanExecute()
		{
			return tabGroupsManager.MoveAllToPreviousTabGroupCanExecute();
		}

		internal void MoveAllToPreviousTabGroup()
		{
			tabGroupsManager.MoveAllToPreviousTabGroup();
		}

		internal bool MergeAllTabGroupsCanExecute()
		{
			return tabGroupsManager.MergeAllTabGroupsCanExecute();
		}

		internal void MergeAllTabGroups()
		{
			tabGroupsManager.MergeAllTabGroups();
		}

		internal bool UseVerticalTabGroupsCanExecute()
		{
			return tabGroupsManager.UseVerticalTabGroupsCanExecute();
		}

		internal void UseVerticalTabGroups()
		{
			tabGroupsManager.UseVerticalTabGroups();
		}

		internal bool UseHorizontalTabGroupsCanExecute()
		{
			return tabGroupsManager.UseHorizontalTabGroupsCanExecute();
		}

		internal void UseHorizontalTabGroups()
		{
			tabGroupsManager.UseHorizontalTabGroups();
		}

		internal bool CloseTabGroupCanExecute()
		{
			return tabGroupsManager.CloseTabGroupCanExecute();
		}

		internal void CloseTabGroup()
		{
			tabGroupsManager.CloseTabGroup();
		}

		internal bool CloseAllTabGroupsButThisCanExecute()
		{
			return tabGroupsManager.CloseAllTabGroupsButThisCanExecute();
		}

		internal void CloseAllTabGroupsButThis()
		{
			tabGroupsManager.CloseAllTabGroupsButThis();
		}

		internal bool MoveTabGroupAfterNextTabGroupCanExecute()
		{
			return tabGroupsManager.MoveTabGroupAfterNextTabGroupCanExecute();
		}

		internal void MoveTabGroupAfterNextTabGroup()
		{
			tabGroupsManager.MoveTabGroupAfterNextTabGroup();
		}

		internal bool MoveTabGroupBeforePreviousTabGroupCanExecute()
		{
			return tabGroupsManager.MoveTabGroupBeforePreviousTabGroupCanExecute();
		}

		internal void MoveTabGroupBeforePreviousTabGroup()
		{
			tabGroupsManager.MoveTabGroupBeforePreviousTabGroup();
		}

		internal IEnumerable<TabStateDecompile> GetTabStateInOrder()
		{
			var tabGroups = tabGroupsManager.AllTabGroups.ToArray();
			int active = tabGroupsManager.ActiveIndex;
			for (int i = 0; i < tabGroups.Length; i++) {
				var tabGroup = tabGroups[(i + active) % tabGroups.Length];

				var tabStates = tabGroup.AllTabStates.ToArray();
				int activeTabIndex = tabGroup.ActiveIndex;
				if (activeTabIndex < 0)
					activeTabIndex = 0;
				for (int j = 0; j < tabStates.Length; j++)
					yield return tabStates[(j + activeTabIndex) % tabStates.Length];
			}
		}

		internal bool SetActiveTab(TabStateDecompile tabState)
		{
			if (tabGroupsManager.SetActiveTab(tabState)) {
				SetTextEditorFocus(tabState.TextView);
				return true;
			}
			return false;
		}

		internal void CloseTab(TabStateDecompile tabState)
		{
			var tabManager = (TabManager<TabStateDecompile>)tabState.Owner;
			tabManager.CloseTab(tabState);
		}

		internal void ShowDecompilerTabsWindow()
		{
			var win = new DecompilerTabsWindow();
			win.Owner = this;
			win.LastActivatedTabState = ActiveTabState;
			win.ShowDialog();

			// The original tab group gets back its keyboard focus by ShowDialog(). Make sure that
			// the correct tab is activated.
			if (win.LastActivatedTabState != null) {
				if (!SetActiveTab(win.LastActivatedTabState)) {
					// Last activated window was deleted
					SetTextEditorFocus(ActiveTextView);
				}
			}
		}

		public MsgBoxButton? ShowIgnorableMessageBox(string id, string msg, MessageBoxButton buttons)
		{
			if (sessionSettings.IgnoredWarnings.Contains(id))
				return null;

			var msgBox = new MsgBox();
			msgBox.textBlock.Text = msg;
			msgBox.Owner = this;

			switch (buttons) {
			case MessageBoxButton.OK:
				msgBox.noButton.Visibility = Visibility.Collapsed;
				msgBox.cancelButton.Visibility = Visibility.Collapsed;
				break;

			case MessageBoxButton.OKCancel:
				msgBox.noButton.Visibility = Visibility.Collapsed;
				break;

			case MessageBoxButton.YesNoCancel:
				msgBox.okButton.Content = "_Yes";
				break;

			case MessageBoxButton.YesNo:
				msgBox.okButton.Content = "_Yes";
				msgBox.cancelButton.Visibility = Visibility.Collapsed;
				break;

			default:
				throw new ArgumentException("Invalid buttons arg", "buttons");
			}

			msgBox.ShowDialog();
			if (msgBox.ButtonClicked != MsgBoxButton.None && msgBox.dontShowCheckBox.IsChecked == true)
				sessionSettings.IgnoredWarnings.Add(id);

			return msgBox.ButtonClicked;
		}
	}
}
