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
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy;
using dnSpy.AsmEditor;
using dnSpy.AvalonEdit;
using dnSpy.Contracts;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Remove;
using dnSpy.Contracts.Themes;
using dnSpy.Contracts.ToolBars;
using dnSpy.Decompiler;
using dnSpy.Files;
using dnSpy.Files.WPF;
using dnSpy.Hex;
using dnSpy.NRefactory;
using dnSpy.Options;
using dnSpy.Search;
using dnSpy.Shared.UI.Controls;
using dnSpy.Tabs;
using dnSpy.TextView;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.AvalonEdit;
using ICSharpCode.ILSpy.Controls;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.NRefactory;
using ICSharpCode.TreeView;
using Microsoft.Win32;

namespace ICSharpCode.ILSpy {
	/// <summary>
	/// The main window of the application.
	/// </summary>
	partial class MainWindow : MetroWindow {
		internal readonly SessionSettings sessionSettings;

		public DnSpyFileListManager DnSpyFileListManager {
			get { return dnSpyFileListManager; }
		}
		internal DnSpyFileListManager dnSpyFileListManager;
		DnSpyFileList dnspyFileList;
		DnSpyFileListTreeNode dnSpyFileListTreeNode;
		internal readonly Menu mainMenu;
		internal readonly ComboBox languageComboBox;

		[ImportMany]
		IEnumerable<IPlugin> plugins = null;

		[ImportMany]
		IEnumerable<IPaneCreator> paneCreators = null;

		[ImportMany(typeof(ITextEditorListener))]
		public IEnumerable<ITextEditorListener> textEditorListeners = null;

		static MainWindow instance;

		public IHexDocumentManager HexDocumentManager { get; set; }

		public static MainWindow Instance {
			get { return instance; }
		}

		public SharpTreeView TreeView {
			get { return treeView; }
		}

		public ImageSource BigLoadingImage { get; private set; }

		public SessionSettings SessionSettings {
			get { return sessionSettings; }
		}

		readonly TabGroupsManager<TabState> tabGroupsManager;

		// Returns null if the active tab is not a DecompileTabState. Doesn't try to return a
		// non-active DecompileTabState
		public DecompileTabState GetActiveDecompileTabState() {
			return tabGroupsManager.ActiveTabGroup.ActiveTabState as DecompileTabState;
		}

		DecompileTabState GetOrCreateActiveDecompileTabState() {
			bool wasMadeActive;
			return GetOrCreateActiveDecompileTabState(out wasMadeActive);
		}

		DecompileTabState GetOrCreateActiveDecompileTabState(out bool wasMadeActive) {
			wasMadeActive = false;
			var tabState = GetActiveDecompileTabState();
			if (tabState != null)
				return tabState;

			// If another tab group has an active DecompileTabState, use it instead of
			// creating a new one
			foreach (var ts in AllVisibleTabStates) {
				var dts = ts as DecompileTabState;
				if (dts != null) {
					wasMadeActive = true;
					SetActiveTab(dts);
					return dts;
				}
			}

			tabState = CreateEmptyDecompileTabState();

			var old = IgnoreSelectionChanged_HACK(tabState);
			try {
				tabGroupsManager.ActiveTabGroup.SetSelectedTab(tabState);
			}
			finally {
				RestoreIgnoreSelectionChanged_HACK(tabState, old);
			}

			return tabState;
		}

		public TabState ActiveTabState {
			get { return tabGroupsManager.ActiveTabGroup.ActiveTabState; }
		}

		public IEnumerable<TabState> AllTabStates {
			get {
				// Return the visible tabs first
				foreach (var tabManager in tabGroupsManager.AllTabGroups) {
					var tabState = tabManager.ActiveTabState;
					if (tabState != null)
						yield return tabState;
				}
				foreach (var tabManager in tabGroupsManager.AllTabGroups) {
					var active = tabManager.ActiveTabState;
					foreach (var tabState in tabManager.AllTabStates) {
						if (tabState != active)
							yield return tabState;
					}
				}
			}
		}

		IEnumerable<DecompileTabState> AllDecompileTabStates {
			get {
				foreach (var tabState in AllTabStates) {
					var dts = tabState as DecompileTabState;
					if (dts != null)
						yield return dts;
				}
			}
		}

		IEnumerable<TabState> AllVisibleTabStates {
			get {
				foreach (var tabManager in tabGroupsManager.AllTabGroups) {
					var tabState = tabManager.ActiveTabState;
					if (tabState != null)
						yield return tabState;
				}
			}
		}

		public IEnumerable<DecompileTabState> AllVisibleDecompileTabStates {
			get {
				foreach (var tabState in AllVisibleTabStates) {
					var dts = tabState as DecompileTabState;
					if (dts != null)
						yield return dts;
				}
			}
		}

		public DecompilerTextView SafeActiveTextView {
			get { return GetOrCreateActiveDecompileTabState().TextView; }
		}

		public DecompilerTextView ActiveTextView {
			get {
				var tabState = GetActiveDecompileTabState();
				return tabState == null ? null : tabState.TextView;
			}
		}

		public IEnumerable<DecompilerTextView> AllVisibleTextViews {
			get {
				foreach (var tabState in AllVisibleDecompileTabStates)
					yield return tabState.TextView;
			}
		}

		public IEnumerable<DecompilerTextView> AllTextViews {
			get {
				foreach (var tabState in AllDecompileTabStates)
					yield return tabState.TextView;
			}
		}

		public MainWindow() {
			instance = this;
			this.sessionSettings = new SessionSettings();
			this.sessionSettings.PropertyChanged += sessionSettings_PropertyChanged;
			var listOptions = new DnSpyFileListOptionsImpl(this.Dispatcher);
			this.dnSpyFileListManager = new DnSpyFileListManager(listOptions);
			DnSpy.App.ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;
			Options.DisplaySettingsPanel.CurrentDisplaySettings.PropertyChanged += CurrentDisplaySettings_PropertyChanged;
			OtherSettings.Instance.PropertyChanged += OtherSettings_PropertyChanged;
			InitializeTextEditorFontResource();

			languageComboBox = new ComboBox() {
				DisplayMemberPath = "NameUI",
				Width = 100,
				ItemsSource = Languages.AllLanguages,
			};
			languageComboBox.SetBinding(ComboBox.SelectedItemProperty, new Binding("FilterSettings.Language") {
				Source = sessionSettings,
			});

			InitializeComponent();
			AddTitleInfo(IntPtr.Size == 4 ? "x86" : "x64");
			DnSpy.App.CompositionContainer.ComposeParts(this);
			foreach (var plugin in plugins)
				plugin.EarlyInit();

			if (sessionSettings.LeftColumnWidth > 0)
				leftColumn.Width = new GridLength(sessionSettings.LeftColumnWidth, GridUnitType.Pixel);
			sessionSettings.FilterSettings.PropertyChanged += filterSettings_PropertyChanged;

			InstallCommands();

			tabGroupsManager = new TabGroupsManager<TabState>(tabGroupsContentPresenter, tabManager_OnSelectionChanged, tabManager_OnAddRemoveTabState);
			tabGroupsManager.OnTabGroupSelected += tabGroupsManager_OnTabGroupSelected;
			TempHack.HackRemove.InitializeThemes(sessionSettings.ThemeName);
			InitializeAssemblyTreeView(treeView);

			mainMenu = DnSpy.App.MenuManager.CreateMenu(new Guid(MenuConstants.APP_MENU_GUID), this);
			UpdateToolbar();
			loadingImage.Source = DnSpy.App.ImageManager.GetImage(GetType().Assembly, "dnSpy-Big", (DnSpy.App.ThemeManager.Theme.GetColor(ColorType.EnvironmentBackground).Background as SolidColorBrush).Color);

			this.Activated += (s, e) => UpdateSystemMenuImage();
			this.Deactivated += (s, e) => UpdateSystemMenuImage();
			this.ContentRendered += MainWindow_ContentRendered;
			this.IsEnabled = false;
		}

		protected override void OnKeyDown(KeyEventArgs e) {
			if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Escape) {
				var tabState = ActiveTabState;
				if (tabState != null)
					tabState.FocusContent();
				e.Handled = true;
				return;
			}

			base.OnKeyDown(e);
		}

		void OtherSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "DeserializeResources") {
				if (OtherSettings.Instance.DeserializeResources)
					OnDeserializeResources();
			}
		}

		void OnDeserializeResources() {
			var modifiedResourceNodes = new HashSet<ILSpyTreeNode>();
			foreach (var node in this.dnSpyFileListTreeNode.Descendants().OfType<SerializedResourceElementTreeNode>()) {
				if (node.DeserializeCanExecute()) {
					node.Deserialize();
					modifiedResourceNodes.Add(node);
				}
			}

			RefreshResources(modifiedResourceNodes);
		}

		void RefreshResources(HashSet<ILSpyTreeNode> modifiedResourceNodes) {
			if (modifiedResourceNodes.Count == 0)
				return;

			var ownerNodes = new HashSet<ResourceListTreeNode>();
			foreach (var node in modifiedResourceNodes) {
				var owner = ILSpyTreeNode.GetNode<ResourceListTreeNode>(node);
				if (owner != null)
					ownerNodes.Add(owner);
			}
			if (ownerNodes.Count == 0)
				return;

			DecompileCache.Instance.Clear(new HashSet<IDnSpyFile>(ownerNodes.Select(a => ILSpyTreeNode.GetNode<AssemblyTreeNode>(a).DnSpyFile)));

			foreach (var tabState in AllDecompileTabStates) {
				bool refresh = tabState.DecompiledNodes.Any(a => ownerNodes.Contains(ILSpyTreeNode.GetNode<ResourceListTreeNode>(a)));
				if (refresh)
					ForceDecompile(tabState);
			}
		}

		void CurrentDisplaySettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SelectedFont")
				InitializeTextEditorFontResource();
			else if (e.PropertyName == "SyntaxHighlightTreeViewUI")
				OnSyntaxHighlightTreeViewUIChanged();
		}

		void OnSyntaxHighlightTreeViewUIChanged() {
			RefreshTreeViewFilter();
		}

		void InitializeTextEditorFontResource() {
			App.Current.Resources["TextEditorFontFamily"] = Options.DisplaySettingsPanel.CurrentDisplaySettings.SelectedFont;
		}

		public static void InitializeTreeView(SharpTreeView treeView, bool isGridView = false) {
			if (isGridView)
				treeView.ItemContainerStyle = (Style)App.Current.TryFindResource(SharpGridView.ItemContainerStyleKey);
			else {
				// Clear the value set by the constructor. This is required or our style won't be used.
				treeView.ClearValue(ItemsControl.ItemContainerStyleProperty);
			}

			treeView.GetPreviewInsideTextBackground = () => DnSpy.App.ThemeManager.Theme.GetColor(ColorType.SystemColorsHighlight).Background;
			treeView.GetPreviewInsideForeground = () => DnSpy.App.ThemeManager.Theme.GetColor(ColorType.SystemColorsHighlightText).Foreground;
		}

		public static void InitializeAssemblyTreeView(SharpTreeView treeView) {
			InitializeTreeView(treeView);

			VirtualizingStackPanel.SetIsVirtualizing(treeView, true);
			// VirtualizationMode.Recycling results in slower scrolling but less memory usage.
			// In my simple test, 225MB vs 280MB (165 loaded assemblies, selected method was
			// [mscorlib]System.String::Format(string, object), scroll up and down, release, then
			// keep doing it a number of times).
			// VirtualizationMode.Standard: all created items are freed once the scrolling stops
			// which can sometimes result in the UI not responding to input. Doesn't seem to be a
			// problem with the treeview, though. More of a problem with the CIL editor which has
			// to use Recycling.
			// SharpTreeView defaults to Recycling, so we must explicitly set it to Standard.
			VirtualizingStackPanel.SetVirtualizationMode(treeView, VirtualizationMode.Standard);
		}

		internal bool IsDecompilerTabControl(TabControl tabControl) {
			return tabGroupsManager.IsTabGroup(tabControl);
		}

		void sessionSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "HighlightCurrentLine") {
				foreach (var textView in AllTextViews)
					textView.TextEditor.Options.HighlightCurrentLine = sessionSettings.HighlightCurrentLine;
			}
		}

		static void RemoveCommands(DecompilerTextView view) {
			var handler = view.TextEditor.TextArea.DefaultInputHandler;

			RemoveCommands(handler.Editing);
			RemoveCommands(handler.CaretNavigation);
			RemoveCommands(handler.CommandBindings);
		}

		static void RemoveCommands(ICSharpCode.AvalonEdit.Editing.TextAreaInputHandler handler) {
			var commands = new HashSet<ICommand>();
			var inputList = (IList<InputBinding>)handler.InputBindings;
			for (int i = inputList.Count - 1; i >= 0; i--) {
				var kb = inputList[i] as KeyBinding;
				if (kb == null)
					continue;
				if ((kb.Modifiers == ModifierKeys.None && kb.Key == Key.Back) ||
					(kb.Modifiers == ModifierKeys.None && kb.Key == Key.Enter) ||
					(kb.Modifiers == ModifierKeys.None && kb.Key == Key.Tab) ||
					(kb.Modifiers == ModifierKeys.Shift && kb.Key == Key.Tab) ||
					(kb.Modifiers == ModifierKeys.Control && kb.Key == Key.Enter) ||
					(kb.Modifiers == ModifierKeys.None && kb.Key == Key.Delete)) {
					inputList.RemoveAt(i);
					commands.Add(kb.Command);
				}
			}
			RemoveCommands(handler.CommandBindings);
			var bindingList = (IList<CommandBinding>)handler.CommandBindings;
			for (int i = bindingList.Count - 1; i >= 0; i--) {
				var binding = bindingList[i];
				if (commands.Contains(binding.Command))
					bindingList.RemoveAt(i);
			}
		}

		static void RemoveCommands(ICollection<CommandBinding> commandBindings) {
			var bindingList = (IList<CommandBinding>)commandBindings;
			for (int i = bindingList.Count - 1; i >= 0; i--) {
				var binding = bindingList[i];
				// Ctrl+D: GoToToken
				if (binding.Command == ICSharpCode.AvalonEdit.AvalonEditCommands.DeleteLine ||
					binding.Command == ApplicationCommands.Undo ||
					binding.Command == ApplicationCommands.Redo ||
					binding.Command == ApplicationCommands.Cut ||
					binding.Command == ApplicationCommands.Delete ||
					binding.Command == EditingCommands.Delete)
					bindingList.RemoveAt(i);
			}
		}

		void tabManager_OnAddRemoveTabState(TabManager<TabState> tabManager, TabManagerAddType addType, TabState tabState) {
			if (addType == TabManagerAddType.Add) {
				tabState.PropertyChanged += tabState_PropertyChanged;

				var dts = tabState as DecompileTabState;
				if (dts != null) {
					var view = dts.TextView;
					RemoveCommands(view);
					view.TextEditor.TextArea.MouseRightButtonDown += (s, e) => view.GoToMousePosition();
					view.TextEditor.Options.EnableRectangularSelection = false;
					view.TextEditor.SetBinding(ICSharpCode.AvalonEdit.TextEditor.WordWrapProperty, new Binding("WordWrap") { Source = sessionSettings });
				}

				if (OnTabStateAdded != null)
					OnTabStateAdded(this, new TabStateEventArgs(tabState));
			}
			else if (addType == TabManagerAddType.Remove) {
				tabState.PropertyChanged -= tabState_PropertyChanged;
				if (OnTabStateRemoved != null)
					OnTabStateRemoved(this, new TabStateEventArgs(tabState));
			}
			else if (addType == TabManagerAddType.Attach) {
				if (OnTabStateAttached != null)
					OnTabStateAttached(this, new TabStateEventArgs(tabState));
			}
			else if (addType == TabManagerAddType.Detach) {
				if (OnTabStateDetached != null)
					OnTabStateDetached(this, new TabStateEventArgs(tabState));
			}
			else
				throw new InvalidOperationException();
		}

		void tabState_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "Header") {
				if (OnTabHeaderChanged != null)
					OnTabHeaderChanged(sender, e);
			}
		}
		public event EventHandler OnTabHeaderChanged;

		public event EventHandler<TabStateEventArgs> OnTabStateAdded;
		public event EventHandler<TabStateEventArgs> OnTabStateRemoved;
		public event EventHandler<TabStateEventArgs> OnTabStateAttached;
		public event EventHandler<TabStateEventArgs> OnTabStateDetached;
		public class TabStateEventArgs : EventArgs {
			public readonly TabState TabState;

			public TabStateEventArgs(TabState tabState) {
				this.TabState = tabState;
			}
		}

		public event EventHandler<TabGroupEventArgs> OnTabGroupAdded {
			add { tabGroupsManager.OnTabGroupAdded += value; }
			remove { tabGroupsManager.OnTabGroupAdded -= value; }
		}

		public event EventHandler<TabGroupEventArgs> OnTabGroupRemoved {
			add { tabGroupsManager.OnTabGroupRemoved += value; }
			remove { tabGroupsManager.OnTabGroupRemoved -= value; }
		}

		public event EventHandler<TabGroupSelectedEventArgs> OnTabGroupSelected {
			add { tabGroupsManager.OnTabGroupSelected += value; }
			remove { tabGroupsManager.OnTabGroupSelected -= value; }
		}

		public event EventHandler<TabGroupSwappedEventArgs> OnTabGroupSwapped {
			add { tabGroupsManager.OnTabGroupSwapped += value; }
			remove { tabGroupsManager.OnTabGroupSwapped -= value; }
		}

		public event EventHandler<TabGroupEventArgs> OnTabGroupsOrientationChanged {
			add { tabGroupsManager.OnOrientationChanged += value; }
			remove { tabGroupsManager.OnOrientationChanged -= value; }
		}

		bool IsActiveTab(TabState tabState) {
			return tabGroupsManager.ActiveTabGroup.ActiveTabState == tabState;
		}

		void SelectTreeViewNodes(DecompileTabState tabState, ILSpyTreeNode[] nodes) {
			if (!IsActiveTab(tabState))
				return;

			// This isn't perfect, but let's assume the TE has focus if the treeview doesn't.
			// We should normally check for:
			//	Keyboard.FocusedElement == tabState.TextView.TextEditor.TextArea
			// but a menu could be open in the text editor, and then the above expression fails.
			bool hasKeyboardFocus = !(Keyboard.FocusedElement is SharpTreeViewItem);
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

					// FocusNode() should already call ScrollIntoView() but for some reason,
					// the ScrollIntoView() does nothing so add another call.
					// Background priority won't work, we need ContextIdle prio
					this.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(delegate {
						var item = treeView.SelectedItem as SharpTreeNode;
						if (item != null)
							treeView.ScrollIntoView(item);
					}));
				}
				foreach (var node in nodes)
					treeView.SelectedItems.Add(node);
			}
			finally {
				tabState.ignoreDecompilationRequests = old;
			}
			// The treeview stole the focus; get it back
			if (hasKeyboardFocus)
				DelaySetFocus(tabState);
		}

		//TODO: HACK alert
		Dictionary<TabManagerBase, bool> tabManager_dontSelectHack = new Dictionary<TabManagerBase, bool>();
		internal bool IgnoreSelectionChanged_HACK(TabState tabState) {
			var tabManager = tabState.Owner;
			bool value;
			tabManager_dontSelectHack.TryGetValue(tabManager, out value);
			tabManager_dontSelectHack[tabManager] = true;
			return value;
		}
		internal void RestoreIgnoreSelectionChanged_HACK(TabState tabState, bool oldValue) {
			var tabManager = tabState.Owner;
			if (!oldValue)
				tabManager_dontSelectHack.Remove(tabManager);
			else
				tabManager_dontSelectHack[tabManager] = oldValue;
		}

		void InitializeActiveTab(TabState tabState, bool forceIsInActiveTabGroup) {
			var tabManager = tabState == null ? null : tabState.Owner as TabManager<TabState>;
			bool isInActiveTabGroup = tabGroupsManager.ActiveTabGroup == tabManager || forceIsInActiveTabGroup;

			var dts = tabState as DecompileTabState;
			var newView = dts == null ? null : dts.TextView;

			if (isInActiveTabGroup) {
				InstallTabCommandBindings(tabState);
				if (dts != null)
					SetLanguage(dts.Language);
			}

			bool dontSelect;
			if (tabManager != null && tabManager_dontSelectHack.TryGetValue(tabManager, out dontSelect) && dontSelect) {
			}
			else if (tabState == null || dts == null) {
				if ((isInActiveTabGroup && tabState != null) || (tabGroupsManager.AllTabGroups.Count == 1 && tabGroupsManager.ActiveTabGroup.ActiveTabState == null)) {
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
				SelectTreeViewNodes(dts, dts.DecompiledNodes);

			if (isInActiveTabGroup)
				ClosePopups();
			if (newView != null && isInActiveTabGroup)
				InstallTextEditorListeners(newView);
		}

		void UninitializeActiveTab(TabState tabState, bool forceIsInActiveTabGroup) {
			var tabManager = tabState == null ? null : tabState.Owner as TabManager<TabState>;
			bool isInActiveTabGroup = tabGroupsManager.ActiveTabGroup == tabManager || forceIsInActiveTabGroup;

			if (isInActiveTabGroup) {
				UninstallTabCommandBindings(tabState);
				var dts = tabState as DecompileTabState;
				if (dts != null)
					UninstallTextEditorListeners(dts.TextView);
			}
		}

		void InstallTabCommandBindings(TabState tabState) {
			if (tabState == null)
				return;
			switch (tabState.Type) {
			case TabStateType.DecompiledCode:
				AddCommandBindings(CodeBindings, ((DecompileTabState)tabState).TextView);
				break;

			case TabStateType.HexEditor:
				AddCommandBindings(HexBindings, ((HexTabState)tabState).HexBox);
				break;

			default:
				throw new InvalidOperationException();
			}
		}

		void UninstallTabCommandBindings(TabState tabState) {
			if (tabState == null)
				return;

			switch (tabState.Type) {
			case TabStateType.DecompiledCode:
				RemoveCommandBindings(CodeBindings, ((DecompileTabState)tabState).TextView);
				break;

			case TabStateType.HexEditor:
				RemoveCommandBindings(HexBindings, ((HexTabState)tabState).HexBox);
				break;

			default:
				throw new InvalidOperationException();
			}
		}

		// These command and input bindings are added whenever a new tab gets active
		public readonly TabBindings CodeBindings = new TabBindings();
		public readonly TabBindings HexBindings = new TabBindings();
		public class TabBindings {
			public readonly List<CommandBinding> CommandBindings = new List<CommandBinding>();
			public readonly List<InputBinding> InputBindings = new List<InputBinding>();

			public void Install(UIElement target) {
				target.CommandBindings.AddRange(CommandBindings);
				target.InputBindings.AddRange(InputBindings);
			}

			public void Uninstall(UIElement target) {
				foreach (var binding in CommandBindings)
					target.CommandBindings.Remove(binding);
				foreach (var binding in InputBindings)
					target.InputBindings.Remove(binding);
			}

			public void Add(ICommand command, ICommand realCommand, ModifierKeys modifiers1, Key key1, ModifierKeys modifiers2 = ModifierKeys.None, Key key2 = Key.None, ModifierKeys modifiers3 = ModifierKeys.None, Key key3 = Key.None) {
				Add(command, (s, e) => realCommand.Execute(e.Parameter), (s, e) => e.CanExecute = realCommand.CanExecute(e.Parameter), modifiers1, key1, modifiers2, key2, modifiers3, key3);
			}

			public void Add(ICommand command, ExecutedRoutedEventHandler exec, CanExecuteRoutedEventHandler canExec, ModifierKeys modifiers1, Key key1, ModifierKeys modifiers2 = ModifierKeys.None, Key key2 = Key.None, ModifierKeys modifiers3 = ModifierKeys.None, Key key3 = Key.None) {
				this.CommandBindings.Add(new CommandBinding(command, exec, canExec));
				this.InputBindings.Add(new KeyBinding(command, key1, modifiers1));
				if (key2 != Key.None)
					this.InputBindings.Add(new KeyBinding(command, key2, modifiers2));
				if (key3 != Key.None)
					this.InputBindings.Add(new KeyBinding(command, key3, modifiers3));
			}
		}

		void InstallCommands() {
			CodeBindings.Add(new RoutedCommand("GoToLine", typeof(MainWindow)), GoToLineExecuted, GoToLineExecutedCanExecute, ModifierKeys.Control, Key.G);

			var bindings = new TabBindings();
			bindings.Add(new RoutedCommand("OpenNewTab", typeof(MainWindow)), OpenNewTabExecuted, null, ModifierKeys.Control, Key.T);
			bindings.Add(new RoutedCommand("CloseActiveTab", typeof(MainWindow)), CloseActiveTabExecuted, CloseActiveTabCanExecute, ModifierKeys.Control, Key.W, ModifierKeys.Control, Key.F4);
			bindings.Add(new RoutedCommand("SelectNextTab", typeof(MainWindow)), SelectNextTabExecuted, SelectNextTabCanExecute, ModifierKeys.Control, Key.Tab);
			bindings.Add(new RoutedCommand("SelectPrevTab", typeof(MainWindow)), SelectPrevTabExecuted, SelectPrevTabCanExecute, ModifierKeys.Control | ModifierKeys.Shift, Key.Tab);
			bindings.Add(new RoutedCommand("ZoomIncrease", typeof(MainWindow)), ZoomIncreaseExecuted, ZoomIncreaseCanExecute, ModifierKeys.Control, Key.OemPlus, ModifierKeys.Control, Key.Add);
			bindings.Add(new RoutedCommand("ZoomDecrease", typeof(MainWindow)), ZoomDecreaseExecuted, ZoomDecreaseCanExecute, ModifierKeys.Control, Key.OemMinus, ModifierKeys.Control, Key.Subtract);
			bindings.Add(new RoutedCommand("ZoomReset", typeof(MainWindow)), ZoomResetExecuted, ZoomResetCanExecute, ModifierKeys.Control, Key.D0, ModifierKeys.Control, Key.NumPad0);
			bindings.Add(new RoutedCommand("FocusCode", typeof(MainWindow)), FocusCodeExecuted, FocusCodeCanExecute, ModifierKeys.None, Key.F7, ModifierKeys.Control | ModifierKeys.Alt, Key.D0, ModifierKeys.Control | ModifierKeys.Alt, Key.NumPad0);
			bindings.Add(new RoutedCommand("FocusTreeView", typeof(MainWindow)), FocusTreeViewExecuted, FocusTreeViewCanExecute, ModifierKeys.Control | ModifierKeys.Alt, Key.L);
			bindings.Add(NavigationCommands.BrowseBack, BackCommandExecuted, BackCommandCanExecute, ModifierKeys.None, Key.Back);
			bindings.CommandBindings.Add(new CommandBinding(NavigationCommands.BrowseForward, ForwardCommandExecuted, ForwardCommandCanExecute));
			bindings.Add(new RoutedCommand("FullScreen", typeof(MainWindow)), FullScreenExecuted, FullScreenCanExecute, ModifierKeys.Shift | ModifierKeys.Alt, Key.Enter);
			bindings.CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenCommandExecuted));
			bindings.CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, SaveCommandExecuted, SaveCommandCanExecute));
			bindings.CommandBindings.Add(new CommandBinding(NavigationCommands.Search, SearchCommandExecuted));
			bindings.Add(new RoutedCommand("WordWrap", typeof(MainWindow)), WordWrapExecuted, WordWrapCanExecute, ModifierKeys.Control | ModifierKeys.Alt, Key.W);
			bindings.Install(this);
		}

		void AddCommandBindings(TabBindings bindings, UIElement elem) {
			bindings.Install(this);
			this.CommandBindings.AddRange(elem.CommandBindings);
		}

		void RemoveCommandBindings(TabBindings bindings, UIElement elem) {
			bindings.Uninstall(this);
			foreach (CommandBinding binding in elem.CommandBindings)
				this.CommandBindings.Remove(binding);
		}

		internal void tabManager_OnSelectionChanged(TabManager<TabState> tabManager, TabState oldState, TabState newState) {
			UninitializeActiveTab(oldState, false);
			InitializeActiveTab(newState, false);

			if (IsActiveTab(newState))
				SetTabFocus(newState);

			if (OnTabStateChanged != null)
				OnTabStateChanged(this, new TabStateChangedEventArgs(oldState, newState));
		}

		void tabGroupsManager_OnTabGroupSelected(object sender, TabGroupSelectedEventArgs e) {
			var oldTabManager = tabGroupsManager.AllTabGroups[e.OldIndex];
			var newTabManager = tabGroupsManager.AllTabGroups[e.NewIndex];

			UninitializeActiveTab(oldTabManager.ActiveTabState, true);
			InitializeActiveTab(newTabManager.ActiveTabState, true);

			var activeTabState = newTabManager.ActiveTabState;
			if (activeTabState != null)
				SetTabFocus(activeTabState);

			if (OnActiveTabStateChanged != null)
				OnActiveTabStateChanged(this, new TabStateChangedEventArgs(oldTabManager.ActiveTabState, newTabManager.ActiveTabState));
		}

		public void SetTextEditorFocus(DecompilerTextView textView) {
			SetTabFocus(DecompileTabState.GetDecompileTabState(textView));
		}

		void SetTabFocus(TabState tabState) {
			if (disable_SetTabFocus)
				return;
			if (tabState == null)
				return;
			if (!IsActiveTab(tabState))
				return;
			if (tabState.TabItem.Content == null)
				return;

			var uiElem = tabState.FocusedElement;
			Debug.Assert(uiElem != null);
			if (uiElem == null)
				return;

			if (!uiElem.IsVisible)
				new SetFocusWhenVisible(tabState, uiElem);
			else
				SetFocusIfNoMenuIsOpened(uiElem);
		}
		bool disable_SetTabFocus = false;

		class SetFocusWhenVisible {
			readonly TabState tabState;
			readonly UIElement uiElem;

			public SetFocusWhenVisible(TabState tabState, UIElement uiElem) {
				this.tabState = tabState;
				this.uiElem = uiElem;
				uiElem.IsVisibleChanged += uiElem_IsVisibleChanged;
			}

			void uiElem_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
				uiElem.IsVisibleChanged -= uiElem_IsVisibleChanged;
				if (MainWindow.Instance.IsActiveTab(tabState))
					SetFocusIfNoMenuIsOpened(uiElem);
			}
		}

		public event EventHandler<TabStateChangedEventArgs> OnTabStateChanged;
		public event EventHandler<TabStateChangedEventArgs> OnActiveTabStateChanged;
		public class TabStateChangedEventArgs : EventArgs {
			/// <summary>
			/// Old tab state. Can be null
			/// </summary>
			public readonly TabState OldTabState;

			/// <summary>
			/// New tab state. Can be null
			/// </summary>
			public readonly TabState NewTabState;

			public TabStateChangedEventArgs(TabState oldTabState, TabState newTabState) {
				this.OldTabState = oldTabState;
				this.NewTabState = newTabState;
			}
		}

		void InstallTextEditorListeners(DecompilerTextView textView) {
			if (textEditorListeners == null)
				return;
			foreach (var listener in textEditorListeners) {
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHover.AddListener(textView.TextEditor, listener);
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHoverStopped.AddListener(textView.TextEditor, listener);
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseDown.AddListener(textView.TextEditor, listener);
			}
		}

		void UninstallTextEditorListeners(DecompilerTextView textView) {
			if (textEditorListeners == null)
				return;
			foreach (var listener in textEditorListeners) {
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHover.RemoveListener(textView.TextEditor, listener);
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseHoverStopped.RemoveListener(textView.TextEditor, listener);
				ICSharpCode.ILSpy.AvalonEdit.TextEditorWeakEventManager.MouseDown.RemoveListener(textView.TextEditor, listener);
			}
		}

		internal void SetTitle(DecompilerTextView textView, string title) {
			var tabState = DecompileTabState.GetDecompileTabState(textView);
			tabState.Title = title;
		}

		internal void ClosePopups() {
			if (textEditorListeners == null)
				return;
			foreach (var listener in textEditorListeners)
				listener.ClosePopup();
		}

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) {
			TempHack.HackRemove.ImageManager_OnThemeChanged();
			UpdateSystemMenuImage();
			DnSpy.App.ThemeManager.Theme.UpdateResources(App.Current.Resources);
			NewTextEditor.OnThemeUpdatedStatic();
			HexBoxThemeHelper.OnThemeUpdatedStatic();
			foreach (var view in AllTextViews)
				view.OnThemeUpdated();
			UpdateToolbar();
			RefreshTreeViewFilter();
		}

		void UpdateSystemMenuImage() {
			if (IsActive)
				SystemMenuImage = DnSpy.App.ImageManager.GetImage(GetType().Assembly, "Assembly", BackgroundType.TitleAreaActive);
			else
				SystemMenuImage = DnSpy.App.ImageManager.GetImage(GetType().Assembly, "Assembly", BackgroundType.TitleAreaInactive);
		}

		void SetWindowBounds(Rect bounds) {
			this.Left = bounds.Left;
			this.Top = bounds.Top;
			this.Width = bounds.Width;
			this.Height = bounds.Height;
		}

		public void UpdateToolbar() {
			DnSpy.App.ToolBarManager.InitializeToolBar(toolBar, new Guid(ToolBarConstants.APP_TB_GUID), this);
		}

		public static void SetFocusIfNoMenuIsOpened(UIElement elem) {
			if (!DnSpy.App.MenuManager.IsMenuOpened)
				elem.Focus();
		}

		#region Message Hook
		protected override void OnSourceInitialized(EventArgs e) {
			base.OnSourceInitialized(e);
			PresentationSource source = PresentationSource.FromVisual(this);
			HwndSource hwndSource = source as HwndSource;
			if (hwndSource != null) {
				hwndSource.AddHook(WndProc);
			}
			if (sessionSettings.WindowBounds != null) {
				// Validate and Set Window Bounds
				bool boundsOK = false;
				Rect bounds = Rect.Transform(sessionSettings.WindowBounds.Value, source.CompositionTarget.TransformToDevice);
				var boundsRect = new System.Drawing.Rectangle((int)bounds.Left, (int)bounds.Top, (int)bounds.Width, (int)bounds.Height);
				foreach (var screen in System.Windows.Forms.Screen.AllScreens) {
					var intersection = System.Drawing.Rectangle.Intersect(boundsRect, screen.WorkingArea);
					if (intersection.Width > 10 && intersection.Height > 10)
						boundsOK = true;
				}
				if (boundsOK)
					SetWindowBounds(sessionSettings.WindowBounds.Value);
				else
					SetWindowBounds(SessionSettings.DefaultWindowBounds);
			}
			else {
				this.Width = SessionSettings.DefaultWindowBounds.Width;
				this.Height = SessionSettings.DefaultWindowBounds.Height;
			}
		}

		unsafe IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
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
							WindowUtils.SetState(this, WindowState.Normal);
						HandleCommandLineArgumentsAfterShowList(args);
						handled = true;
						return (IntPtr)0x2E9A5913;
					}
				}
			}
			return IntPtr.Zero;
		}
		#endregion

		public DnSpyFileList DnSpyFileList {
			get { return dnspyFileList; }
		}

		public event NotifyCollectionChangedEventHandler CurrentAssemblyListChanged;

		List<IDnSpyFile> commandLineLoadedFiles = new List<IDnSpyFile>();

		bool HandleCommandLineArguments(CommandLineArguments args) {
			foreach (string file in args.AssembliesToLoad) {
				commandLineLoadedFiles.Add(dnspyFileList.OpenFile(file));
			}
			if (args.Language != null)
				sessionSettings.FilterSettings.Language = Languages.GetLanguage(args.Language);
			return true;
		}

		void HandleCommandLineArgumentsAfterShowList(CommandLineArguments args) {
			if (args.NavigateTo != null) {
				bool found = false;
				if (args.NavigateTo.StartsWith("N:", StringComparison.Ordinal)) {
					string namespaceName = args.NavigateTo.Substring(2);
					foreach (IDnSpyFile asm in commandLineLoadedFiles) {
						AssemblyTreeNode asmNode = dnSpyFileListTreeNode.FindAssemblyNode(asm);
						if (asmNode != null) {
							NamespaceTreeNode nsNode = asmNode.FindNamespaceNode(namespaceName);
							if (nsNode != null) {
								found = true;
								SelectNode(nsNode);
								break;
							}
						}
					}
				}
				else {
					foreach (IDnSpyFile asm in commandLineLoadedFiles) {
						ModuleDef def = asm.ModuleDef;
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
			}
			else if (commandLineLoadedFiles.Count == 1) {
				// NavigateTo == null and an assembly was given on the command-line:
				// Select the newly loaded assembly
				JumpToReference(commandLineLoadedFiles[0].ModuleDef);
			}
			if (args.Search != null) {
				SearchPane.Instance.SearchTerm = args.Search;
				SearchPane.Instance.Show();
			}
			if (!string.IsNullOrEmpty(args.SaveDirectory)) {
				foreach (var x in commandLineLoadedFiles)
					OnExportAssembly(x, args.SaveDirectory);
			}
			commandLineLoadedFiles.Clear(); // clear references once we don't need them anymore
		}

		void OnExportAssembly(IDnSpyFile dnSpyFile, string path) {
			var textView = ActiveTextView;
			if (textView == null)
				return;
			AssemblyTreeNode asmNode = dnSpyFileListTreeNode.FindModuleNode(dnSpyFile.ModuleDef);
			if (asmNode != null) {
				string file = DecompilerTextView.CleanUpName(asmNode.DnSpyFile.GetShortName());
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

		public void ExecuteWhenLoaded(Action func) {
			if (callWhenLoaded == null)
				func();
			else
				callWhenLoaded.Add(func);
		}
		List<Action> callWhenLoaded = new List<Action>();

		sealed class GuidObjectsCreator : IGuidObjectsCreator {
			public IEnumerable<GuidObject> GetGuidObjects(GuidObject creatorObject, bool openedFromKeyboard) {
				var atv = (SharpTreeView)creatorObject.Object;
				yield return new GuidObject(MenuConstants.GUIDOBJ_TREEVIEW_NODES_ARRAY_GUID, atv.GetTopLevelSelection().ToArray());
			}
		}

		void MainWindow_ContentRendered(object sender, EventArgs e) {
			this.ContentRendered -= MainWindow_ContentRendered;
			if (!sessionSettings.IsFullScreen)
				WindowUtils.SetState(this, sessionSettings.WindowState);
			this.IsFullScreen = sessionSettings.IsFullScreen;
			StartLoadingHandler(0);
		}

		void StartLoadingHandler(int i) {
			this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate {
				LoadingHandler(i);
			}));
		}

		void LoadingHandler(int i) {
			switch (i) {
			case 0:
				this.CommandBindings.Add(new CommandBinding(ILSpyTreeNode.TreeNodeActivatedEvent, TreeNodeActivatedExecuted));

				DnSpy.App.MenuManager.InitializeContextMenu(treeView, MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID, new GuidObjectsCreator());

				this.dnspyFileList = dnSpyFileListManager.LoadList(sessionSettings.ActiveAssemblyList);
				break;

			case 1:
				HandleCommandLineArguments(App.CommandLineArguments);

				if (dnspyFileList.GetDnSpyFiles().Length == 0
					&& dnspyFileList.Name == DnSpyFileListManager.DefaultListName) {
					LoadInitialAssemblies();
				}

				ShowAssemblyListDontAskUser(this.dnspyFileList);
				break;

			case 2:
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
				break;

			case 3:
				AvalonEditTextOutput output = new AvalonEditTextOutput();

				if (topPane.Content == null) {
					var pane = GetPane(topPane, sessionSettings.TopPaneSettings.Name);
					if (pane != null)
						ShowInTopPane(pane);
				}
				if (bottomPane.Content == null) {
					var pane = GetPane(bottomPane, sessionSettings.BottomPaneSettings.Name);
					if (pane != null)
						ShowInBottomPane(pane);
				}
				break;

			case 4:
				foreach (var plugin in plugins)
					plugin.OnLoaded();

				var list = callWhenLoaded;
				callWhenLoaded = null;
				foreach (var func in list)
					func();

				break;

			case 5:
				this.IsEnabled = true;

				// Make sure that when no tabs are created that we have focus. If we don't do this we
				// can't press Ctrl+K and open the asm search.
				this.Focus();

				// Sometimes we get keyboard focus when it's better that the text editor gets the focus instead
				this.GotKeyboardFocus += MainWindow_GotKeyboardFocus;

				loadingControl.Visibility = Visibility.Collapsed;
				mainGrid.Visibility = Visibility.Visible;

				// In case a plugin has added their own bindings
				UninstallTabCommandBindings(ActiveTabState);
				InstallTabCommandBindings(ActiveTabState);

				// Flickering workaround fix. Could reproduce it when using VMWare + WinXP
				loadingProgressBar.IsIndeterminate = false;
				return;
			default:
				return;
			}
			StartLoadingHandler(i + 1);
		}

		void MainWindow_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
			if (e.NewFocus == this) {
				var tabState = ActiveTabState;
				if (tabState != null) {
					tabState.FocusContent();
					e.Handled = true;
					return;
				}
			}
		}

		IPane GetPane(DockedPane dockedPane, string name) {
			if (string.IsNullOrEmpty(name))
				return null;
			foreach (var creator in paneCreators) {
				var pane = creator.Create(name);
				if (pane != null)
					return pane;
			}
			return null;
		}

		public void ShowAssemblyList(string name) {
			DnSpyFileList list = this.dnSpyFileListManager.LoadList(name);
			//Only load a new list when it is a different one
			if (list.Name != DnSpyFileList.Name) {
				if (AskUserReloadAssemblyListIfModified("Are you sure you want to load new assemblies and lose all changes?"))
					ShowAssemblyListDontAskUser(list);
			}
		}

		public Func<string, bool> AskUserReloadAssemblyListIfModified;
		public event EventHandler OnShowNewAssemblyList;

		void ShowAssemblyListDontAskUser(DnSpyFileList dnspyFileList) {
			// Clear the cache since the keys contain tree nodes which get recreated now. The keys
			// will never match again so shouldn't be in the cache.
			DecompileCache.Instance.ClearAll();
			if (OnShowNewAssemblyList != null)
				OnShowNewAssemblyList(this, EventArgs.Empty);

			foreach (var tabManager in tabGroupsManager.AllTabGroups.ToArray())
				tabManager.RemoveAllTabStates();
			this.dnspyFileList = dnspyFileList;

			// Make sure memory usage doesn't increase out of control. This method allocates lots of
			// new stuff, but the GC doesn't bother to reclaim that memory for a long time.
			GC.Collect();
			GC.WaitForPendingFinalizers();

			dnspyFileList.CollectionChanged += assemblyList_Assemblies_CollectionChanged;
			dnSpyFileListTreeNode = new DnSpyFileListTreeNode(dnspyFileList);
			// Make sure CurrentAssemblyListChanged() is called after the treenodes have been created
			dnspyFileList.CollectionChanged += assemblyList_Assemblies_CollectionChanged2;
			dnSpyFileListTreeNode.FilterSettings = sessionSettings.FilterSettings.Clone();
			dnSpyFileListTreeNode.Select = SelectNode;
			dnSpyFileListTreeNode.OwnerTreeView = treeView;
			treeView.Root = dnSpyFileListTreeNode;

			UpdateTitle();
		}

		void UpdateTitle() {
			this.Title = GetDefaultTitle();
		}

		string GetDefaultTitle() {
			// If this string gets updated, App.xaml.cs (SendToPreviousInstance()) needs to be updated too
			var t = string.Format("dnSpy ({0})", string.Join(", ", titleInfos.ToArray()));
			if (dnspyFileList != null && dnspyFileList.Name != DnSpyFileListManager.DefaultListName)
				t = string.Format("{0} - {1}", t, dnspyFileList.Name);
			return t;
		}
		List<string> titleInfos = new List<string>();

		public void AddTitleInfo(string info) {
			if (!titleInfos.Contains(info)) {
				titleInfos.Add(info);
				UpdateTitle();
			}
		}

		public void RemoveTitleInfo(string info) {
			if (titleInfos.Remove(info))
				UpdateTitle();
		}

		const string DebuggingTitleInfo = "Debugging";
		public void SetDebugging() {
			AddTitleInfo(DebuggingTitleInfo);
			App.Current.Resources["IsDebuggingKey"] = true;
		}

		public void ClearDebugging() {
			RemoveTitleInfo(DebuggingTitleInfo);
			App.Current.Resources["IsDebuggingKey"] = false;
		}

		void assemblyList_Assemblies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			if (!dnspyFileList.IsReArranging) {
				if (e.Action == NotifyCollectionChangedAction.Reset) {
					foreach (var tabManager in tabGroupsManager.AllTabGroups)
						tabManager.RemoveAllTabStates();
				}
				if (e.OldItems != null) {
					var oldAssemblies = new HashSet<IDnSpyFile>(e.OldItems.Cast<IDnSpyFile>());
					var newNodes = new List<ILSpyTreeNode>();
					foreach (var tabState in AllTabStates.ToArray()) {
						switch (tabState.Type) {
						case TabStateType.DecompiledCode:
							var dts = (DecompileTabState)tabState;
							dts.History.RemoveAll(n => n.TreeNodes.Any(
								nd => nd.AncestorsAndSelf().OfType<AssemblyTreeNode>().Any(
									a => oldAssemblies.Contains(a.DnSpyFile))));

							newNodes.Clear();
							foreach (var node in dts.DecompiledNodes) {
								var asmNode = GetAssemblyTreeNode(node);
								if (asmNode != null && !oldAssemblies.Contains(asmNode.DnSpyFile))
									newNodes.Add(node);
							}
							if (newNodes.Count == 0 && ActiveTabState != dts) {
								var tabManager = (TabManager<TabState>)dts.Owner;
								tabManager.RemoveTabState(dts);
							}
							else if (!dts.Equals(newNodes.ToArray(), dts.Language)) {
								dts.History.UpdateCurrent(null);
								dts.TextView.CleanUpBeforeReDecompile();
								DecompileRestoreLocation(dts, newNodes.ToArray(), null, true);
							}
							break;

						case TabStateType.HexEditor:
							break;

						default:
							throw new InvalidOperationException();
						}
					}
					var oldModules = new HashSet<IDnSpyFile>(oldAssemblies);
					foreach (var asm in oldAssemblies) {
						var node = dnSpyFileListTreeNode.FindAssemblyNode(asm);
						if (node != null) {
							foreach (var asmNode in node.Children.OfType<AssemblyTreeNode>())
								oldModules.Add(asmNode.DnSpyFile);
						}
					}
					DecompileCache.Instance.Clear(oldModules);
				}
			}
		}

		void assemblyList_Assemblies_CollectionChanged2(object sender, NotifyCollectionChangedEventArgs e) {
			if (CurrentAssemblyListChanged != null)
				CurrentAssemblyListChanged(this, e);
		}

		void LoadInitialAssemblies() {
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
				dnspyFileList.OpenFile(asm.Location);
		}

		void filterSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			RefreshTreeViewFilter();
			if (e.PropertyName == "Language") {
				var tabState = GetActiveDecompileTabState();
				if (tabState != null)
					DecompileRestoreLocation(tabState, tabState.DecompiledNodes, sessionSettings.FilterSettings.Language);
			}
		}

		void SetLanguage(Language language) {
			languageComboBox.SelectedItem = language;
		}

		public void RefreshTreeViewFilter() {
			// filterSettings is mutable; but the ILSpyTreeNode filtering assumes that filter settings are immutable.
			// Thus, the main window will use one mutable instance (for data-binding), and assign a new clone to the ILSpyTreeNodes whenever the main
			// mutable instance changes.
			if (dnSpyFileListTreeNode != null)
				dnSpyFileListTreeNode.FilterSettings = sessionSettings.FilterSettings.Clone();
		}

		public DnSpyFileListTreeNode DnSpyFileListTreeNode {
			get { return dnSpyFileListTreeNode; }
		}

		#region Node Selection

		public void SelectNode(SharpTreeNode obj) {
			if (obj != null) {
				if (!obj.AncestorsAndSelf().Any(node => node.IsHidden)) {
					// Set both the selection and focus to ensure that keyboard navigation works as expected.
					treeView.FocusNode(obj);
					treeView.SelectedItem = obj;
				}
				else {
					MainWindow.Instance.ShowMessageBox("Navigation failed because the target is hidden or a compiler-generated class.\n" +
						"Please disable all filters that might hide the item (i.e. activate " +
						"\"View > Show Internal Types and Members\") and try again.");
				}
			}
		}

		public ILSpyTreeNode FindTreeNode(object reference) {
			return dnSpyFileListTreeNode.FindTreeNode(reference);
		}

		public static IMemberDef ResolveReference(object reference) {
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

		object FixReference(object reference) {
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

		bool JumpToNamespace(DecompilerTextView textView, NamespaceRef nsRef, bool canRecordHistory = true) {
			if (nsRef == null)
				return false;
			return JumpToNamespace(textView, nsRef.Module, nsRef.Namespace, canRecordHistory);
		}

		bool JumpToNamespace(DecompilerTextView textView, IDnSpyFile asm, string ns, bool canRecordHistory = true) {
			if (asm == null || ns == null)
				return false;
			var asmNode = FindTreeNode(asm.ModuleDef) as AssemblyTreeNode;
			if (asmNode == null)
				return false;
			var nsNode = asmNode.FindNamespaceNode(ns);
			if (nsNode == null)
				return false;

			var tabState = textView != null ? DecompileTabState.GetDecompileTabState(textView) : GetOrCreateActiveDecompileTabState();
			if (canRecordHistory)
				RecordHistory(tabState);

			var nodes = new[] { nsNode };
			DecompileNodes(tabState, null, false, tabState.Language, nodes);
			SelectTreeViewNodes(tabState, nodes);
			return true;
		}

		public bool JumpToReference(object reference, bool canRecordHistory = true) {
			var nsRef = reference as NamespaceRef;
			if (nsRef != null)
				return JumpToNamespace(null, nsRef);
			var tabState = GetOrCreateActiveDecompileTabState();
			if (canRecordHistory)
				RecordHistory(tabState);
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), (success, hasMovedCaret) => GoToLocation(tabState.TextView, success, hasMovedCaret, ResolveReference(reference)));
		}

		public bool JumpToReference(DecompilerTextView textView, object reference, bool canRecordHistory = true) {
			var nsRef = reference as NamespaceRef;
			if (nsRef != null)
				return JumpToNamespace(textView, nsRef);
			var tabState = DecompileTabState.GetDecompileTabState(textView);
			if (canRecordHistory)
				RecordHistory(tabState);
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), (success, hasMovedCaret) => GoToLocation(tabState.TextView, success, hasMovedCaret, ResolveReference(reference)));
		}

		public bool JumpToReference(DecompilerTextView textView, object reference, Func<TextLocation> getLocation, bool canRecordHistory = true) {
			var nsRef = reference as NamespaceRef;
			if (nsRef != null)
				return JumpToNamespace(textView, nsRef);
			var tabState = DecompileTabState.GetDecompileTabState(textView);
			if (canRecordHistory)
				RecordHistory(tabState);
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), (success, hasMovedCaret) => GoToLocation(tabState.TextView, success, hasMovedCaret, getLocation()));
		}

		public bool JumpToReference(DecompilerTextView textView, object reference, Func<bool, bool, bool> onDecompileFinished, bool canRecordHistory = true) {
			var nsRef = reference as NamespaceRef;
			if (nsRef != null)
				return JumpToNamespace(textView, nsRef);
			var tabState = DecompileTabState.GetDecompileTabState(textView);
			if (canRecordHistory)
				RecordHistory(tabState);
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), onDecompileFinished);
		}

		bool GoToLocation(DecompilerTextView decompilerTextView, bool success, bool hasMovedCaret, object destLoc) {
			if (!success || destLoc == null)
				return false;
			return decompilerTextView.GoToLocation(destLoc);
		}

		sealed class OnShowOutputHelper {
			DecompilerTextView decompilerTextView;
			readonly Func<bool, bool, bool> onDecompileFinished;
			readonly ILSpyTreeNode[] nodes;
			public OnShowOutputHelper(DecompilerTextView decompilerTextView, Func<bool, bool, bool> onDecompileFinished, ILSpyTreeNode[] nodes) {
				this.decompilerTextView = decompilerTextView;
				this.onDecompileFinished = onDecompileFinished;
				this.nodes = nodes;
				decompilerTextView.OnShowOutput += OnShowOutput;
			}

			public void OnShowOutput(object sender, DecompilerTextView.ShowOutputEventArgs e) {
				decompilerTextView.OnShowOutput -= OnShowOutput;
				bool success = Equals(e.Nodes, nodes);
				if (onDecompileFinished != null)
					e.HasMovedCaret |= onDecompileFinished(success, e.HasMovedCaret);
			}

			static bool Equals(ILSpyTreeNode[] a, ILSpyTreeNode[] b) {
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

			public void Abort() {
				decompilerTextView.OnShowOutput -= OnShowOutput;
			}
		}

		// Returns true if we could decompile the reference
		bool JumpToReferenceAsyncInternal(DecompileTabState tabState, bool canLoad, object reference, Func<bool, bool, bool> onDecompileFinished) {
			ILSpyTreeNode treeNode = FindTreeNode(reference);
			if (treeNode != null) {
				var nodes = new[] { treeNode };
				DecompileNodes(tabState, nodes, false, onDecompileFinished);
				SelectTreeViewNodes(tabState, nodes);
				return true;
			}
			else if (reference is dnlib.DotNet.Emit.OpCode) {
				string link = "http://msdn.microsoft.com/library/system.reflection.emit.opcodes." + ((dnlib.DotNet.Emit.OpCode)reference).Code.ToString().ToLowerInvariant() + ".aspx";
				try {
					Process.Start(link);
				}
				catch {

				}
				return true;
			}
			else if (canLoad && reference is IMemberDef) {
				// Here if the module was removed. It's possible that the user has re-added it.

				var member = (IMemberDef)reference;
				var module = member.Module;
				if (module == null) // Check if it has been deleted
					return false;
				var mainModule = module;
				if (module.Assembly != null)
					mainModule = module.Assembly.ManifestModule;
				if (!string.IsNullOrEmpty(mainModule.Location) && !string.IsNullOrEmpty(module.Location)) {
					// Check if the module was removed and then added again
					foreach (var m in dnspyFileList.GetAllModules()) {
						if (mainModule.Location.Equals(m.Location, StringComparison.OrdinalIgnoreCase)) {
							foreach (var asmMod in GetAssemblyModules(m)) {
								if (!module.Location.Equals(asmMod.Location, StringComparison.OrdinalIgnoreCase))
									continue;

								// Found the module
								member = asmMod.ResolveToken(member.MDToken.Raw) as IMemberDef;
								if (member != null) // should never fail
									return JumpToReferenceAsyncInternal(tabState, false, member, onDecompileFinished);

								break;
							}

							return false;
						}
					}
				}

				// The module has been removed. Add it again
				var dnSpyFile = dnspyFileList.CreateDnSpyFile(mainModule, true);
				dnSpyFile.IsAutoLoaded = true;
				dnspyFileList.AddFile(dnSpyFile, true, false, false);
				return JumpToReferenceAsyncInternal(tabState, false, reference, onDecompileFinished);
			}
			else
				return false;
		}

		IEnumerable<ModuleDef> GetAssemblyModules(ModuleDef module) {
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
		void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
			e.Handled = true;
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = ".NET Executables (*.exe, *.dll, *.netmodule, *.winmd)|*.exe;*.dll;*.netmodule;*.winmd|All files (*.*)|*.*";
			dlg.Multiselect = true;
			dlg.RestoreDirectory = true;
			if (dlg.ShowDialog() == true) {
				OpenFiles(dlg.FileNames);
			}
		}

		public void OpenFiles(string[] fileNames, bool focusNode = true) {
			if (fileNames == null)
				throw new ArgumentNullException("fileNames");

			if (focusNode)
				treeView.UnselectAll();

			SharpTreeNode lastNode = null;
			foreach (string file in fileNames) {
				var asm = dnspyFileList.OpenFile(file);
				if (asm != null) {
					var node = dnSpyFileListTreeNode.FindAssemblyNode(asm);
					if (node != null && focusNode) {
						treeView.SelectedItems.Add(node);
						lastNode = node;
					}
				}
				if (lastNode != null && focusNode)
					treeView.FocusNode(lastNode);
			}
		}

		internal void ReloadList() {
			if (!ReloadListCanExecute())
				return;
			if (!AskUserReloadAssemblyListIfModified("Are you sure you want to reload all assemblies and lose all changes?"))
				return;
			var savedState = CreateSavedTabGroupsState();

			try {
				TreeView_SelectionChanged_ignore = true;
				ShowAssemblyListDontAskUser(dnSpyFileListManager.LoadList(dnspyFileList.Name));
			}
			finally {
				TreeView_SelectionChanged_ignore = false;
			}

			RestoreTabGroups(savedState);
		}

		internal bool ReloadListCanExecute() {
			if (CanExecuteEvent != null) {
				var ea = new CanExecuteEventArgs(CanExecuteType.ReloadList, true);
				CanExecuteEvent(this, ea);
				if (ea.Result is bool)
					return (bool)ea.Result;
			}
			return true;
		}

		public enum CanExecuteType {
			ReloadList,
		}

		public class CanExecuteEventArgs : EventArgs {
			public readonly CanExecuteType Type;
			public object Result;

			public CanExecuteEventArgs(CanExecuteType type, object defaultResult) {
				this.Type = type;
				this.Result = defaultResult;
			}
		}
		public event EventHandler<CanExecuteEventArgs> CanExecuteEvent;

		void SearchCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
			SearchPane.Instance.Show();
		}
		#endregion

		#region Decompile (TreeView_SelectionChanged)
		//TODO: HACK alert
		internal bool TreeView_SelectionChanged_ignore = false;
		void TreeView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (TreeView_SelectionChanged_ignore)
				return;
			// New nodes could be selected so grab the current selected nodes now
			var nodes = this.SelectedNodes;

			DecompileTabState tabState;
			bool old = TreeView_SelectionChanged_ignore, old2 = disable_SetTabFocus;
			try {
				TreeView_SelectionChanged_ignore = true;
				disable_SetTabFocus = true;
				bool wasMadeActive;
				tabState = GetOrCreateActiveDecompileTabState(out wasMadeActive);
				if (wasMadeActive)
					SelectTreeViewNodes(tabState, nodes);
			}
			finally {
				TreeView_SelectionChanged_ignore = old;
				disable_SetTabFocus = old2;
			}

			DecompileNodes(tabState, null, true, tabState.Language, nodes);

			if (SelectionChanged != null)
				SelectionChanged(sender, e);
		}

		static ILSpyTreeNode[] FilterOutDeletedNodes(IEnumerable<ILSpyTreeNode> nodes) {
			return nodes.Where(a => ILSpyTreeNode.GetNode<DnSpyFileListTreeNode>(a) != null).ToArray();
		}

		void DecompileNodes(DecompileTabState tabState, ILSpyTreeNode[] nodes, bool recordHistory, Func<bool, bool, bool> onDecompileFinished) {
			DecompileNodes(tabState, nodes, recordHistory, tabState.Language, onDecompileFinished);
		}

		void DecompileRestoreLocation(DecompileTabState tabState, ILSpyTreeNode[] nodes, Language language = null, bool forceDecompile = false) {
			var pos = tabState.TextView.GetRefPos();
			DecompileNodes(tabState, nodes, false, language ?? tabState.Language, (a, b) => tabState.TextView.GoTo(pos), forceDecompile);
		}

		void DecompileNodes(DecompileTabState tabState, ILSpyTreeNode[] nodes, bool recordHistory, Language language, Func<bool, bool, bool> onDecompileFinished, bool forceDecompile = false) {
			var helper = new OnShowOutputHelper(tabState.TextView, onDecompileFinished, nodes);
			bool? decompiled = DecompileNodes(tabState, null, recordHistory, language, nodes, forceDecompile);
			if (decompiled == false) {
				helper.Abort();
				onDecompileFinished(true, false);
			}
		}

		bool? DecompileNodes(DecompileTabState tabState, DecompilerTextViewState state, bool recordHistory, Language language, ILSpyTreeNode[] nodes, bool forceDecompile = false) {
			if (tabState.ignoreDecompilationRequests)
				return null;

			// Ignore all nodes that have been deleted
			nodes = FilterOutDeletedNodes(nodes);

			if (tabState.HasDecompiled && !forceDecompile && tabState.Equals(nodes, language)) {
				if (state != null)
					tabState.TextView.EditorPositionState = state.EditorPositionState;
				return false;
			}

			if (tabState.HasDecompiled && recordHistory)
				RecordHistory(tabState);

			tabState.HasDecompiled = true;
			tabState.SetDecompileProps(language, nodes);

			if (nodes.Length == 1) {
				var node = nodes[0];

				var viewObject = node.GetViewObject(tabState.TextView);
				if (viewObject != null) {
					tabState.TextView.CancelDecompileAsync();
					tabState.Content = viewObject;
					return true;
				}

				if (node.View(tabState.TextView)) {
					tabState.Content = tabState.TextView;
					tabState.TextView.CancelDecompileAsync();
					return true;
				}
			}

			tabState.Content = tabState.TextView;
			tabState.TextView.DecompileAsync(language, nodes, new DecompilationOptions() { TextViewState = state });
			return true;
		}

		internal void RecordHistory(DecompilerTextView textView) {
			RecordHistory(DecompileTabState.GetDecompileTabState(textView));
		}

		void RecordHistory(DecompileTabState tabState) {
			if (tabState == null)
				return;
			var dtState = tabState.TextView.GetState(tabState.DecompiledNodes);
			if (dtState != null)
				tabState.History.UpdateCurrent(new NavigationState(dtState, tabState.Language));
			tabState.History.Record(new NavigationState(tabState.DecompiledNodes, tabState.Language));
		}

		void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
			Save(ActiveTabState);
		}

		void SaveCommandCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = ActiveTabState != null;
		}

		public event EventHandler<TabStateEventArgs> SaveTabState;

		internal void Save(TabState tabState) {
			var decompileTabState = tabState as DecompileTabState;
			if (decompileTabState != null) {
				SaveCode(decompileTabState);
				return;
			}

			if (SaveTabState != null)
				SaveTabState(this, new TabStateEventArgs(tabState));
		}

		void SaveCode(DecompileTabState tabState) {
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
				var tabState = GetActiveDecompileTabState();
				if (tabState != null)
					return tabState.Language;
				return sessionSettings.FilterSettings.Language;
			}
		}

		public event SelectionChangedEventHandler SelectionChanged;

		public ILSpyTreeNode[] SelectedNodes {
			get { return treeView.GetTopLevelSelection().OfType<ILSpyTreeNode>().ToArray(); }
		}
		#endregion

		#region Back/Forward navigation
		void BackCommandCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			var tabState = GetActiveDecompileTabState();
			e.Handled = true;
			e.CanExecute = tabState != null && tabState.History.CanNavigateBack;
		}

		void BackCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
			if (BackCommand(GetActiveDecompileTabState()))
				e.Handled = true;
		}

		internal void BackCommand(DecompilerTextView textView) {
			BackCommand(DecompileTabState.GetDecompileTabState(textView));
		}

		bool BackCommand(DecompileTabState tabState) {
			if (tabState == null)
				return false;
			if (tabState.History.CanNavigateBack) {
				NavigateHistory(tabState, false);
				return true;
			}
			return false;
		}

		void ForwardCommandCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			var tabState = GetActiveDecompileTabState();
			e.Handled = true;
			e.CanExecute = tabState != null && tabState.History.CanNavigateForward;
		}

		void ForwardCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
			var tabState = GetActiveDecompileTabState();
			if (tabState == null)
				return;
			if (tabState.History.CanNavigateForward) {
				e.Handled = true;
				NavigateHistory(tabState, true);
			}
		}

		void NavigateHistory(DecompileTabState tabState, bool forward) {
			var dtState = tabState.TextView.GetState(tabState.DecompiledNodes);
			if (dtState != null)
				tabState.History.UpdateCurrent(new NavigationState(dtState, tabState.Language));
			var newState = forward ? tabState.History.GoForward() : tabState.History.GoBack();
			var nodes = newState.TreeNodes.Cast<ILSpyTreeNode>().ToArray();
			SelectTreeViewNodes(tabState, nodes);
			DecompileNodes(tabState, newState.ViewState, false, newState.Language, nodes);
			SetLanguage(newState.Language);
		}

		#endregion

		protected override void OnStateChanged(EventArgs e) {
			base.OnStateChanged(e);
			// store window state in settings only if it's not minimized
			if (this.WindowState != System.Windows.WindowState.Minimized) {
				sessionSettings.WindowState = this.WindowState;
				sessionSettings.IsFullScreen = this.IsFullScreen;
			}
		}

		protected override void OnClosing(CancelEventArgs e) {
			base.OnClosing(e);
			if (e.Cancel)
				return;

			sessionSettings.ThemeName = DnSpy.App.ThemeManager.Theme.Name;
			sessionSettings.ActiveAssemblyList = dnspyFileList.Name;
			sessionSettings.WindowBounds = this.RestoreBounds;
			sessionSettings.LeftColumnWidth = leftColumn.Width.Value;
			sessionSettings.TopPaneSettings = GetPaneSettings(topPane, topPaneRow);
			sessionSettings.BottomPaneSettings = GetPaneSettings(bottomPane, bottomPaneRow);
			sessionSettings.SavedTabGroupsState = CreateSavedTabGroupsState();
			sessionSettings.Save();

			foreach (var tabState in AllTabStates)
				tabState.Dispose();

			TempHack.HackRemove.SaveSettings();
		}

		void RestoreTabGroups(SavedTabGroupsState savedGroups) {
			Debug.Assert(tabGroupsManager.AllTabGroups.Count == 1);
			bool first = true;
			foreach (var savedGroupState in savedGroups.Groups) {
				var tabManager = first ? tabGroupsManager.ActiveTabGroup : tabGroupsManager.CreateTabGroup(savedGroups.IsHorizontal);
				first = false;
				foreach (var savedTabState in savedGroupState.Tabs) {
					var savedDecompileTabState = savedTabState as SavedDecompileTabState;
					if (savedDecompileTabState != null) {
						var tabState = CreateNewDecompileTabState(tabManager, Languages.GetLanguage(savedDecompileTabState.Language));
						CreateDecompileTabState(tabState, savedDecompileTabState);
						continue;
					}

					var savedHexTabState = savedTabState as SavedHexTabState;
					if (savedHexTabState != null) {
						if (File.Exists(savedHexTabState.FileName)) {
							var tabState = CreateNewHexTabState(tabManager);
							CreateHexTabState(tabState, savedHexTabState);
						}
						continue;
					}

					Debug.Fail("Unknown saved state");
				}

				tabManager.SetSelectedIndex(savedGroupState.Index);
			}

			tabGroupsManager.SetSelectedIndex(savedGroups.Index);
		}

		SavedTabGroupsState CreateSavedTabGroupsState() {
			var state = new SavedTabGroupsState();
			state.IsHorizontal = tabGroupsManager.IsHorizontal;
			state.Index = tabGroupsManager.ActiveIndex;
			foreach (var tabManager in tabGroupsManager.AllTabGroups)
				state.Groups.Add(CreateSavedTabGroupState(tabManager));
			return state;
		}

		static SavedTabGroupState CreateSavedTabGroupState(TabManager<TabState> tabManager) {
			var savedState = new SavedTabGroupState();

			savedState.Index = tabManager.ActiveIndex;

			foreach (var tabState in tabManager.AllTabStates)
				savedState.Tabs.Add(tabState.CreateSavedTabState());

			return savedState;
		}

		DecompileTabState CreateNewDecompileTabState(Language language = null) {
			return CreateNewDecompileTabState(tabGroupsManager.ActiveTabGroup, language);
		}

		DecompileTabState CreateEmptyDecompileTabState(Language language = null) {
			var tabState = CreateNewDecompileTabState(tabGroupsManager.ActiveTabGroup, language);
			DecompileNodes(tabState, null, false, tabState.Language, new ILSpyTreeNode[0]);
			return tabState;
		}

		DecompileTabState CreateNewDecompileTabState(TabManager<TabState> tabManager, Language language = null) {
			var tabState = new DecompileTabState(language ?? sessionSettings.FilterSettings.Language);
			return (DecompileTabState)tabManager.AddNewTabState(tabState);
		}

		DecompileTabState CreateDecompileTabState(SavedDecompileTabState savedState, IList<ILSpyTreeNode> newNodes = null, bool decompile = true) {
			var tabState = CreateNewDecompileTabState(Languages.GetLanguage(savedState.Language));
			return CreateDecompileTabState(tabState, savedState, newNodes, decompile);
		}

		DecompileTabState CreateDecompileTabState(DecompileTabState tabState, SavedDecompileTabState savedState, IList<ILSpyTreeNode> newNodes = null, bool decompile = true) {
			var nodes = new List<ILSpyTreeNode>(savedState.Paths.Count);
			if (newNodes != null)
				nodes.AddRange(newNodes);
			else {
				foreach (var asm in savedState.ActiveAutoLoadedAssemblies)
					this.dnspyFileList.OpenFile(asm, true);
				foreach (var path in savedState.Paths) {
					var node = dnSpyFileListTreeNode.FindNodeByPath(path);
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
					DecompileNodes(tabState, tmpNodes, false, (success, hasMovedCaret) => decompilerTextView_OnShowOutput(success, hasMovedCaret, tabState.TextView, savedState));
				}
				else
					AboutPage.Display(tabState.TextView);
			}

			return tabState;
		}

		bool decompilerTextView_OnShowOutput(bool success, bool hasMovedCaret, DecompilerTextView textView, SavedDecompileTabState savedState) {
			if (!success)
				return false;

			if (IsValid(textView, savedState.EditorPositionState)) {
				textView.EditorPositionState = savedState.EditorPositionState;
				return true;
			}

			return false;
		}

		HexTabState CreateNewHexTabState(TabManager<TabState> tabManager) {
			var tabState = new HexTabState();
			return (HexTabState)tabManager.AddNewTabState(tabState);
		}

		HexTabState CreateHexTabState(SavedHexTabState savedState) {
			if (!File.Exists(savedState.FileName))
				return null;
			var tabState = CreateNewHexTabState(tabGroupsManager.ActiveTabGroup);
			return CreateHexTabState(tabState, savedState);
		}

		HexTabState CreateHexTabState(HexTabState tabState, SavedHexTabState savedHexTabState) {
			tabState.Restore(savedHexTabState);
			return InitializeHexDocument(tabState, savedHexTabState.FileName);
		}

		HexTabState InitializeHexDocument(HexTabState tabState, string filename) {
			var doc = HexDocumentManager.GetOrCreate(filename);
			tabState.SetDocument(doc);
			if (doc == null)
				ShowIgnorableMessageBox("hex: load doc err", string.Format("Error loading {0}", filename), MessageBoxButton.OK);
			return tabState;
		}

		bool IsValid(DecompilerTextView decompilerTextView, EditorPositionState state) {
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

		static SessionSettings.PaneSettings GetPaneSettings(DockedPane dockedPane, RowDefinition row) {
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

		static string GetPaneName(DockedPane dockedPane) {
			var pane = dockedPane.Content;
			return pane == null ? string.Empty : pane.PaneName;
		}

		internal static AssemblyTreeNode GetAssemblyTreeNode(SharpTreeNode node) {
			if (node == null)
				return null;
			while (!(node is TreeNodes.AssemblyTreeNode) && node.Parent != null) {
				node = node.Parent;
			}
			if (node.Parent is AssemblyTreeNode)
				node = node.Parent;
			return node as AssemblyTreeNode;
		}

		#region Top/Bottom Pane management
		public void ShowInTopPane(IPane content) {
			ShowPane(topPane, content, topPaneRow, sessionSettings.TopPaneSettings);
		}

		static void ShowPane(DockedPane dockedPane, IPane content, RowDefinition paneRow, SessionSettings.PaneSettings paneSettings) {
			paneRow.MinHeight = 100;
			if (paneSettings.Height > 0 && (paneRow.Height == null || paneRow.Height.Value == 0))
				paneRow.Height = new GridLength(paneSettings.Height, GridUnitType.Pixel);
			dockedPane.Title = content.PaneTitle;
			if (dockedPane.Content != content) {
				var pane = dockedPane.Content;
				if (pane != null)
					pane.Closed();
				dockedPane.Content = content;
			}
			dockedPane.Visibility = Visibility.Visible;
			content.Opened();
		}

		static void ClosePane(DockedPane dockedPane, RowDefinition paneRow, ref SessionSettings.PaneSettings paneSettings) {
			paneSettings.Height = paneRow.Height.Value;
			paneRow.MinHeight = 0;
			paneRow.Height = new GridLength(0);
			dockedPane.Visibility = Visibility.Collapsed;

			var pane = dockedPane.Content;
			dockedPane.Content = null;
			if (pane != null)
				pane.Closed();
		}

		void TopPane_CloseButtonClicked(object sender, EventArgs e) {
			CloseTopPane();
		}

		public void CloseTopPane() {
			ClosePane(topPane, topPaneRow, ref sessionSettings.TopPaneSettings);
		}

		public IPane TopPaneContent {
			get { return topPane.Content; }
		}

		public void ShowInBottomPane(IPane content) {
			ShowPane(bottomPane, content, bottomPaneRow, sessionSettings.BottomPaneSettings);
		}

		void BottomPane_CloseButtonClicked(object sender, EventArgs e) {
			CloseBottomPane();
		}

		public void CloseBottomPane() {
			ClosePane(bottomPane, bottomPaneRow, ref sessionSettings.BottomPaneSettings);
		}

		public bool IsTopPaneContent(IPane content) {
			return IsPaneContent(topPane, content);
		}

		public bool IsBottomPaneContent(IPane content) {
			return IsPaneContent(bottomPane, content);
		}

		bool IsPaneContent(DockedPane pane, IPane content) {
			return pane.Content == content;
		}

		public bool IsTopPaneVisible(IPane content) {
			return IsPaneVisible(topPane, content);
		}

		public bool IsBottomPaneVisible(IPane content) {
			return IsPaneVisible(bottomPane, content);
		}

		bool IsPaneVisible(DockedPane pane, IPane content) {
			return pane.IsVisible && IsPaneContent(pane, content);
		}
		#endregion

		public void UnselectAll() {
			treeView.UnselectAll();
		}

		public void SetStatus(string status) {
			if (this.statusBar.Visibility == Visibility.Collapsed)
				this.statusBar.Visibility = Visibility.Visible;
			this.StatusLabel.Text = status;
		}

		public void HideStatus() {
			this.statusBar.Visibility = Visibility.Collapsed;
		}

		private void GoToLineExecutedCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			var tabState = GetActiveDecompileTabState();
			e.CanExecute = tabState != null && tabState.IsTextViewInVisualTree;
		}

		private void GoToLineExecuted(object sender, ExecutedRoutedEventArgs e) {
			var decompilerTextView = ActiveTextView;
			if (decompilerTextView == null)
				return;

			var ask = new AskForInput();
			ask.Owner = this;
			ask.Title = "Go to Line";
			ask.label.Content = "_Line [, column]";
			ask.textBox.Text = "";
			ask.textBox.ToolTip = "Enter a line and/or column\n10 => line 10, column 1\n,5 => column 5\n10,5 => line 10, column 5";
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
				ShowMessageBox(string.Format("Invalid line: {0}", lineText));
				return;
			}
			decompilerTextView.ScrollAndMoveCaretTo(line.Value, column.Value);
		}
		static readonly Regex goToLineRegex1 = new Regex(@"^\s*(\d+)\s*(,\s*(\d+))?\s*$");
		static readonly Regex goToLineRegex2 = new Regex(@"^\s*,\s*(\d+)\s*$");

		static int? TryParse(string valText) {
			int val;
			return int.TryParse(valText, out val) ? (int?)val : null;
		}

		internal Language GetLanguage(DecompilerTextView textView) {
			return DecompileTabState.GetDecompileTabState(textView).Language;
		}

		void TreeNodeActivatedExecuted(object sender, ExecutedRoutedEventArgs e) {
			DelaySetFocus(GetActiveDecompileTabState());
		}

		void DelaySetFocus(TabState tabState) {
			if (tabState != null) {
				// The TreeView steals the focus so we can't just set the focus to the text view
				// right here, we have to wait a little bit.
				// This is ugly, but we must use Normal prio to get rid of flickering (tab getting
				// inactive followed by getting active). However, this doesn't work all the time
				// (test: right-click tab, open new tab), so we must start another one at a lower
				// priority in case the treeview steals the focus.......
				this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate {
					if (ActiveTabState == tabState)
						SetTabFocus(tabState);
				}));
				this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate {
					if (ActiveTabState == tabState)
						SetTabFocus(tabState);
				}));
			}
		}

		private void OpenNewTabExecuted(object sender, ExecutedRoutedEventArgs e) {
			OpenNewTab();
		}

		private void CloseActiveTabExecuted(object sender, ExecutedRoutedEventArgs e) {
			CloseActiveTab();
		}

		private void CloseActiveTabCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = CloseActiveTabCanExecute();
		}

		private void SelectNextTabExecuted(object sender, ExecutedRoutedEventArgs e) {
			tabGroupsManager.ActiveTabGroup.SelectNextTab();
		}

		private void SelectNextTabCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = tabGroupsManager.ActiveTabGroup.SelectNextTabCanExecute();
		}

		private void SelectPrevTabExecuted(object sender, ExecutedRoutedEventArgs e) {
			tabGroupsManager.ActiveTabGroup.SelectPreviousTab();
		}

		private void SelectPrevTabCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = tabGroupsManager.ActiveTabGroup.SelectPreviousTabCanExecute();
		}

		private void WordWrapExecuted(object sender, ExecutedRoutedEventArgs e) {
			sessionSettings.WordWrap = !sessionSettings.WordWrap;
		}

		private void WordWrapCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = true;
		}

		private void FullScreenExecuted(object sender, ExecutedRoutedEventArgs e) {
			IsFullScreen = !IsFullScreen;
		}

		private void FullScreenCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = true;
		}

		private void FocusTreeViewExecuted(object sender, ExecutedRoutedEventArgs e) {
			var node = (SharpTreeNode)treeView.SelectedItem;
			if (node != null)
				treeView.FocusNode(node);
			else if (treeView.Items.Count > 0) {
				node = (SharpTreeNode)treeView.Items[0];
				treeView.FocusNode(node);
				treeView.SelectedItem = node;
			}
		}

		private void FocusTreeViewCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = treeView.HasItems;
		}

		private void FocusCodeExecuted(object sender, ExecutedRoutedEventArgs e) {
			var tabState = ActiveTabState;
			if (tabState != null)
				SetTabFocus(tabState);
		}

		private void FocusCodeCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = ActiveTabState != null;
		}

		private void ZoomIncreaseExecuted(object sender, ExecutedRoutedEventArgs e) {
			ZoomIncrease(ActiveTabState);
		}

		private void ZoomIncreaseCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = ActiveTabState != null;
		}

		private void ZoomDecreaseExecuted(object sender, ExecutedRoutedEventArgs e) {
			ZoomDecrease(ActiveTabState);
		}

		private void ZoomDecreaseCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = ActiveTabState != null;
		}

		private void ZoomResetExecuted(object sender, ExecutedRoutedEventArgs e) {
			ZoomReset(ActiveTabState);
		}

		private void ZoomResetCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = ActiveTabState != null;
		}

		const double MIN_ZOOM = 0.2;
		const double MAX_ZOOM = 4.0;

		void ZoomIncrease(TabState tabState) {
			if (tabState == null)
				return;

			var scale = GetScaleValue(tabState);
			scale += scale / 10;
			SetScaleValue(tabState, scale);
		}

		void ZoomDecrease(TabState tabState) {
			if (tabState == null)
				return;

			var scale = GetScaleValue(tabState);
			scale -= scale / 10;
			SetScaleValue(tabState, scale);
		}

		void ZoomReset(TabState tabState) {
			if (tabState == null)
				return;

			SetScaleValue(tabState, 1);
		}

		double GetScaleValue(TabState tabState) {
			var scaleElem = tabState.ScaleElement;
			if (scaleElem == null)
				return 1;
			var st = scaleElem.LayoutTransform as ScaleTransform;
			if (st != null)
				return st.ScaleX;
			return 1;
		}

		void SetScaleValue(TabState tabState, double scale) {
			var scaleElem = tabState.ScaleElement;
			if (scaleElem == null)
				return;
			if (scale == 1) {
				scaleElem.LayoutTransform = Transform.Identity;
				scaleElem.ClearValue(TextOptions.TextFormattingModeProperty);
			}
			else {
				if (scale < MIN_ZOOM)
					scale = MIN_ZOOM;
				else if (scale > MAX_ZOOM)
					scale = MAX_ZOOM;

				// We must set it to Ideal or the text will be blurry
				TextOptions.SetTextFormattingMode(scaleElem, TextFormattingMode.Ideal);

				var st = new ScaleTransform(scale, scale);
				st.Freeze();
				scaleElem.LayoutTransform = st;
			}
		}

		internal void ZoomMouseWheel(TabState tabState, int delta) {
			if (delta > 0)
				ZoomIncrease(tabState);
			else if (delta < 0)
				ZoomDecrease(tabState);
		}

		internal TabState CloneTab(TabState tabState, bool decompile = true) {
			if (tabState == null)
				return null;

			var savedTabState = tabState.CreateSavedTabState();

			var dts = tabState as DecompileTabState;
			if (dts != null)
				return CreateDecompileTabState((SavedDecompileTabState)savedTabState, dts.DecompiledNodes, decompile);

			var hts = tabState as HexTabState;
			if (hts != null)
				return CreateHexTabState((SavedHexTabState)savedTabState);

			Debug.Fail("Unknown tab state");
			return null;
		}

		internal TabState CloneTabMakeActive(TabState tabState, bool decompile = true) {
			var clonedTabState = CloneTab(tabState, decompile);
			if (clonedTabState != null)
				tabGroupsManager.ActiveTabGroup.SetSelectedTab(clonedTabState);
			return clonedTabState;
		}

		internal void OpenNewTab() {
			TabState tabState;
			var currenTabState = ActiveTabState;

			if (currenTabState != null && !ICSharpCode.ILSpy.Options.DisplaySettingsPanel.CurrentDisplaySettings.NewEmptyTabs)
				tabState = CloneTab(currenTabState);
			else {
				if (currenTabState is DecompileTabState)
					tabState = CreateEmptyDecompileTabState();
				else
					tabState = CloneTab(currenTabState);
			}
			if (tabState == null)
				return;

			tabGroupsManager.ActiveTabGroup.SetSelectedTab(tabState);
		}

		public void OpenNewEmptyTab() {
			tabGroupsManager.ActiveTabGroup.SetSelectedTab(CreateEmptyDecompileTabState());
		}

		internal void OpenReferenceInNewTab(DecompilerTextView textView, ReferenceSegment reference) {
			if (reference == null)
				return;
			if (reference.Reference is AddressReference) {
				GoToAddress((AddressReference)reference.Reference);
				return;
			}
			if (textView == null)
				return;

			var tabState = DecompileTabState.GetDecompileTabState(textView);
			var clonedTabState = (DecompileTabState)CloneTabMakeActive(tabState, false);
			if (clonedTabState == null)
				return;
			clonedTabState.History.Clear();

			// Always open resources in their own window
			if (reference.Reference is IResourceNode) {
				JumpToReference(clonedTabState.TextView, reference.Reference, false);
				return;
			}

			DecompileNodes(clonedTabState, tabState.DecompiledNodes, false, (success, hasMovedCaret) => clonedTabState.TextView.GoToTarget(reference, true, false));
		}

		internal void CloseActiveTab() {
			tabGroupsManager.ActiveTabGroup.CloseActiveTab();
		}

		internal bool CloseActiveTabCanExecute() {
			return tabGroupsManager.ActiveTabGroup.CloseActiveTabCanExecute();
		}

		internal void CloseAllButActiveTab() {
			tabGroupsManager.ActiveTabGroup.CloseAllButActiveTab();
		}

		internal bool CloseAllButActiveTabCanExecute() {
			return tabGroupsManager.ActiveTabGroup.CloseAllButActiveTabCanExecute();
		}

		internal bool CloseAllTabsCanExecute() {
			return tabGroupsManager.CloseAllTabsCanExecute();
		}

		internal void CloseAllTabs() {
			tabGroupsManager.CloseAllTabs();
		}

		internal void CloneActiveTab() {
			CloneTabMakeActive(ActiveTabState);
		}

		internal bool CloneActiveTabCanExecute() {
			return ActiveTabState != null;
		}

		public void ModuleModified(IDnSpyFile mod) {
			DecompileCache.Instance.Clear(mod);

			foreach (var tabState in AllDecompileTabStates) {
				if (MustRefresh(tabState, mod))
					ForceDecompile(tabState);
			}

			if (OnModuleModified != null)
				OnModuleModified(null, new ModuleModifiedEventArgs(mod));
		}

		public event EventHandler<ModuleModifiedEventArgs> OnModuleModified;
		public class ModuleModifiedEventArgs : EventArgs {
			public IDnSpyFile DnSpyFile { get; private set; }

			public ModuleModifiedEventArgs(IDnSpyFile asm) {
				this.DnSpyFile = asm;
			}
		}

		static bool MustRefresh(DecompileTabState tabState, IDnSpyFile mod) {
			var asms = new HashSet<IDnSpyFile>();
			asms.Add(mod);
			return DecompileCache.IsInModifiedModule(asms, tabState.DecompiledNodes) ||
				DecompileCache.IsInModifiedModule(asms, tabState.TextView.References);
		}

		internal void DisableMemoryMappedIO() {
			DisableMemoryMappedIO(GetAllDnSpyFileInstances());
		}

		public void DisableMemoryMappedIO(IEnumerable<IDnSpyFile> files) {
			foreach (var tabState in AllDecompileTabStates) {
				// Make sure that the code doesn't try to reference memory that will be moved.
				tabState.TextView.CancelDecompilation();
			}

			foreach (var file in files) {
				var peImage = file.PEImage;
				if (peImage != null)
					peImage.UnsafeDisableMemoryMappedIO();
			}
		}

		public IEnumerable<IDnSpyFile> GetAllDnSpyFileInstances() {
			if (dnSpyFileListTreeNode == null)
				yield break;
			foreach (AssemblyTreeNode asmNode in dnSpyFileListTreeNode.Children) {
				if (asmNode.Children.Count == 0 || !(asmNode.Children[0] is AssemblyTreeNode))
					yield return asmNode.DnSpyFile;
				else {
					foreach (AssemblyTreeNode child in asmNode.Children)
						yield return child.DnSpyFile;
				}
			}
		}

		internal void RefreshCodeCSharp(bool disassembleIL, bool decompileILAst, bool decompileCSharp, bool decompileVB) {
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

		void RefreshCodeIL() {
			foreach (var tabState in AllDecompileTabStates) {
				if (tabState.Language.NameUI == "IL")
					ForceDecompile(tabState);
			}
		}

		void RefreshCodeILAst() {
			foreach (var tabState in AllDecompileTabStates) {
				if (tabState.Language.NameUI.StartsWith("ILAst (") && tabState.Language.NameUI.EndsWith(")"))
					ForceDecompile(tabState);
			}
		}

		void RefreshCodeCSharp() {
			foreach (var tabState in AllDecompileTabStates) {
				if (tabState.Language.NameUI == "C#" || tabState.Language.NameUI.StartsWith("C# - "))
					ForceDecompile(tabState);
			}
		}

		void RefreshCodeVB() {
			foreach (var tabState in AllDecompileTabStates) {
				if (tabState.Language.NameUI == "VB")
					ForceDecompile(tabState);
			}
		}

		void ForceDecompile(DecompileTabState tabState) {
			DecompileRestoreLocation(tabState, tabState.DecompiledNodes, null, true);
		}

		internal void RefreshTreeViewNodes() {
			RefreshTreeViewFilter();
		}

		internal bool NewHorizontalTabGroupCanExecute() {
			return tabGroupsManager.NewHorizontalTabGroupCanExecute();
		}

		internal void NewHorizontalTabGroup() {
			tabGroupsManager.NewHorizontalTabGroup();
		}

		internal bool NewVerticalTabGroupCanExecute() {
			return tabGroupsManager.NewVerticalTabGroupCanExecute();
		}

		internal void NewVerticalTabGroup() {
			tabGroupsManager.NewVerticalTabGroup();
		}

		internal bool MoveToNextTabGroupCanExecute() {
			return tabGroupsManager.MoveToNextTabGroupCanExecute();
		}

		internal void MoveToNextTabGroup() {
			tabGroupsManager.MoveToNextTabGroup();
		}

		internal bool MoveToPreviousTabGroupCanExecute() {
			return tabGroupsManager.MoveToPreviousTabGroupCanExecute();
		}

		internal void MoveToPreviousTabGroup() {
			tabGroupsManager.MoveToPreviousTabGroup();
		}

		internal bool MoveAllToNextTabGroupCanExecute() {
			return tabGroupsManager.MoveAllToNextTabGroupCanExecute();
		}

		internal void MoveAllToNextTabGroup() {
			tabGroupsManager.MoveAllToNextTabGroup();
		}

		internal bool MoveAllToPreviousTabGroupCanExecute() {
			return tabGroupsManager.MoveAllToPreviousTabGroupCanExecute();
		}

		internal void MoveAllToPreviousTabGroup() {
			tabGroupsManager.MoveAllToPreviousTabGroup();
		}

		internal bool MergeAllTabGroupsCanExecute() {
			return tabGroupsManager.MergeAllTabGroupsCanExecute();
		}

		internal void MergeAllTabGroups() {
			tabGroupsManager.MergeAllTabGroups();
		}

		internal bool UseVerticalTabGroupsCanExecute() {
			return tabGroupsManager.UseVerticalTabGroupsCanExecute();
		}

		internal void UseVerticalTabGroups() {
			tabGroupsManager.UseVerticalTabGroups();
		}

		internal bool UseHorizontalTabGroupsCanExecute() {
			return tabGroupsManager.UseHorizontalTabGroupsCanExecute();
		}

		internal void UseHorizontalTabGroups() {
			tabGroupsManager.UseHorizontalTabGroups();
		}

		internal bool CloseTabGroupCanExecute() {
			return tabGroupsManager.CloseTabGroupCanExecute();
		}

		internal void CloseTabGroup() {
			tabGroupsManager.CloseTabGroup();
		}

		internal bool CloseAllTabGroupsButThisCanExecute() {
			return tabGroupsManager.CloseAllTabGroupsButThisCanExecute();
		}

		internal void CloseAllTabGroupsButThis() {
			tabGroupsManager.CloseAllTabGroupsButThis();
		}

		internal bool MoveTabGroupAfterNextTabGroupCanExecute() {
			return tabGroupsManager.MoveTabGroupAfterNextTabGroupCanExecute();
		}

		internal void MoveTabGroupAfterNextTabGroup() {
			tabGroupsManager.MoveTabGroupAfterNextTabGroup();
		}

		internal bool MoveTabGroupBeforePreviousTabGroupCanExecute() {
			return tabGroupsManager.MoveTabGroupBeforePreviousTabGroupCanExecute();
		}

		internal void MoveTabGroupBeforePreviousTabGroup() {
			tabGroupsManager.MoveTabGroupBeforePreviousTabGroup();
		}

		internal IEnumerable<TabState> GetTabStateInOrder() {
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

		internal bool SetActiveView(DecompilerTextView view) {
			var tabState = TabState.GetTabState(view);
			if (tabState == null)
				return false;

			tabGroupsManager.SetActiveTab(tabState);
			return true;
		}

		internal bool SetActiveTab(TabState tabState) {
			if (tabGroupsManager.SetActiveTab(tabState)) {
				SetTabFocus(tabState);
				return true;
			}
			return false;
		}

		internal void CloseTab(TabState tabState) {
			var tabManager = (TabManager<TabState>)tabState.Owner;
			tabManager.CloseTab(tabState);
		}

		internal void ShowDecompilerTabsWindow() {
			var win = new DecompilerTabsWindow();
			win.Owner = this;
			win.LastActivatedTabState = ActiveTabState;
			win.ShowDialog();

			// The original tab group gets back its keyboard focus by ShowDialog(). Make sure that
			// the correct tab is activated.
			if (win.LastActivatedTabState != null) {
				if (!SetActiveTab(win.LastActivatedTabState)) {
					// Last activated window was deleted
					SetTabFocus(ActiveTabState);
				}
			}
		}

		protected override void OnDeactivated(EventArgs e) {
			ClosePopups();
			base.OnDeactivated(e);
		}

		public MsgBoxButton? ShowIgnorableMessageBox(string id, string msg, MessageBoxButton buttons, Window ownerWindow = null) {
			if (sessionSettings.IgnoredWarnings.Contains(id))
				return null;

			bool? dontShowIsChecked;
			var button = ShowMessageBoxInternal(ownerWindow, msg, buttons, true, out dontShowIsChecked);
			if (button != MsgBoxButton.None && dontShowIsChecked == true)
				sessionSettings.IgnoredWarnings.Add(id);

			return button;
		}

		public MsgBoxButton ShowMessageBox(string msg, MessageBoxButton buttons = MessageBoxButton.OK, Window ownerWindow = null) {
			bool? dontShowIsChecked;
			return ShowMessageBoxInternal(ownerWindow, msg, buttons, false, out dontShowIsChecked);
		}

		MsgBoxButton ShowMessageBoxInternal(Window ownerWindow, string msg, MessageBoxButton buttons, bool showDontShowCheckBox, out bool? dontShowIsChecked) {
			var msgBox = new MsgBox();
			msgBox.textBlock.Text = msg;
			msgBox.Owner = ownerWindow ?? this;

			if (!showDontShowCheckBox)
				msgBox.dontShowCheckBox.Visibility = Visibility.Collapsed;

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
			dontShowIsChecked = msgBox.dontShowCheckBox.IsChecked;
			return msgBox.ButtonClicked;
		}

		public void OpenOrShowHexBox(string filename) {
			var tabState = GetHexTabState(filename);
			if (tabState != null)
				ShowHexBox(filename);
			else
				OpenHexBox(filename);
		}

		void ShowHexBox(string filename) {
			var tabState = GetHexTabState(filename);
			if (tabState != null)
				SetActiveTab(tabState);
		}

		void OpenHexBox(string filename) {
			var tabState = OpenHexBoxInternal(filename);
			if (tabState == null)
				return;
			SetActiveTab(tabState);
		}

		public HexTabState GetHexTabState(AssemblyTreeNode node) {
			if (node == null)
				return null;
			return GetHexTabState(node.DnSpyFile.Filename);
		}

		HexTabState GetHexTabState(string filename) {
			if (string.IsNullOrEmpty(filename))
				return null;
			return GetHexTabStates(filename).FirstOrDefault();
		}

		HexTabState OpenHexBoxInternal(string filename) {
			if (!File.Exists(filename))
				return null;
			var tabState = CreateNewHexTabState(tabGroupsManager.ActiveTabGroup);
			InitializeHexDocument(tabState, filename);
			tabState.HexBox.InitializeStartEndOffsetToDocument();
			return tabState;
		}

		IEnumerable<HexTabState> GetHexTabStates(string filename) {
			if (filename == null)
				yield break;
			foreach (var tabState in AllTabStates) {
				var hex = tabState as HexTabState;
				if (hex != null && filename.Equals(hex.FileName, StringComparison.OrdinalIgnoreCase))
					yield return hex;
			}
		}

		IEnumerable<HexTabState> GetHexTabStates(string filename, ulong offset, ulong? length) {
			ulong? end;
			if (length == null)
				end = null;
			else if (length.Value == 0)
				end = offset;
			else if (offset + length.Value - 1 < offset)
				end = ulong.MaxValue;
			else
				end = offset + length.Value - 1;
			foreach (var tabState in GetHexTabStates(filename)) {
				var hb = tabState.HexBox;
				if (offset < hb.StartOffset || offset > hb.EndOffset)
					continue;
				if (end != null && (end.Value < hb.StartOffset || end.Value > hb.EndOffset))
					continue;
				yield return tabState;
			}
		}

		public void GoToAddress(AddressReference @ref) {
			HexTabState tabState;
			ulong fileOffset;
			if (@ref.IsRVA) {
				var asm = dnspyFileList.Find(@ref.Filename);
				if (asm == null)
					return;
				var pe = asm.PEImage;
				if (pe == null)
					return;
				fileOffset = (ulong)pe.ToFileOffset((RVA)@ref.Address);
				tabState = GetHexTabStates(@ref.Filename, fileOffset, @ref.Length).FirstOrDefault();
			}
			else {
				fileOffset = @ref.Address;
				tabState = GetHexTabStates(@ref.Filename, fileOffset, @ref.Length).FirstOrDefault();
			}

			if (tabState == null)
				tabState = OpenHexBoxInternal(@ref.Filename);
			if (tabState == null)
				return;

			SetActiveTab(tabState);
			tabState.HexBox.SelectAndMoveCaret(fileOffset, @ref.Length);
		}
	}
}
