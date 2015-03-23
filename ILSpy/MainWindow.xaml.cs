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
	/// A plugin can implement this interface to get notified at startup
	/// </summary>
	public interface IPlugin
	{
		/// <summary>
		/// Called when MainWindow has been loaded
		/// </summary>
		void OnLoaded();
	}

	class TabState : IDisposable
	{
		public readonly DecompilerTextView TextView = new DecompilerTextView();
		public readonly NavigationHistory<NavigationState> History = new NavigationHistory<NavigationState>();
		public bool ignoreDecompilationRequests;
		public ILSpyTreeNode[] DecompiledNodes = new ILSpyTreeNode[0];
		public TabItem TabItem;
		public string Title;

		public string Header {
			get {
				var nodes = DecompiledNodes;
				if (nodes == null || nodes.Length == 0)
					return Title ?? "<empty>";

				if (nodes.Length == 1)
					return nodes[0].ToString();

				var sb = new StringBuilder();
				foreach (var node in nodes) {
					if (sb.Length > 0)
						sb.Append(", ");
					sb.Append(node.ToString());
				}
				return sb.ToString();
			}
		}

		const int MAX_HEADER_LENGTH = 40;
		string ShortHeader {
			get {
				var header = Header;
				if (header.Length <= MAX_HEADER_LENGTH)
					return header;
				return header.Substring(0, MAX_HEADER_LENGTH) + "...";
			}
		}

		public static TabState GetTabState(DecompilerTextView textView)
		{
			return (TabState)textView.tabState;
		}

		public TabState(SharpTreeView treeView)
		{
			this.TextView.tabState = this;
			var view = TextView;
			var tabItem = new TabItem();
			tabItem.Content = view;
			tabItem.Tag = this;
			TabItem = tabItem;
			InitializeHeader();
			ContextMenuProvider.Add(view);
			tabItem.MouseRightButtonDown += tabItem_MouseRightButtonDown;
		}

		void tabItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			MainWindow.Instance.tabControl.SelectedItem = TabItem;
		}

		public void Dispose()
		{
			TextView.Dispose();
		}

		public void InitializeHeader()
		{
			var shortHeader = ShortHeader;
			var header = Header;
			TabItem.Header = new TextBlock {
				Text = shortHeader,
				ToolTip = shortHeader == header ? null : header,
			};
		}

		public bool IsSameNodes(ILSpyTreeNode[] nodes)
		{
			if (DecompiledNodes.Length != nodes.Length)
				return false;
			for (int i = 0; i < DecompiledNodes.Length; i++) {
				if (DecompiledNodes[i] != nodes[i])
					return false;
			}
			return true;
		}
	}

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

		internal Theme Theme { get; private set; }

		internal TabState SafeActiveTabState {
			get {
				var tabState = ActiveTabState;
				if (tabState != null)
					return tabState;

				tabState = CreateNewTabState();

				var old = tabControl_SelectionChanged_dont_select;
				try {
					tabControl_SelectionChanged_dont_select = true;
					tabControl.SelectedItem = tabState.TabItem;
				}
				finally {
					tabControl_SelectionChanged_dont_select = old;
				}

				return tabState;
			}
		}

		internal TabState ActiveTabState {
			get {
				int index = tabControl.SelectedIndex == -1 ? 0 : tabControl.SelectedIndex;
				if (index >= tabControl.Items.Count)
					return null;
				var item = tabControl.Items[index] as TabItem;
				return item == null ? null : (TabState)item.Tag;
			}
		}

		IEnumerable<TabState> AllTabStates {
			get {
				foreach (var item in tabControl.Items) {
					var tabItem = item as TabItem;
					if (tabItem == null)
						continue;
					Debug.Assert(tabItem.Tag is TabState);
					yield return (TabState)tabItem.Tag;
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
			Theme = Themes.GetThemeOrDefault(sessionSettings.ThemeName);
			
			this.Icon = new BitmapImage(new Uri("pack://application:,,,/dnSpy;component/images/ILSpy.ico"));
			
			this.DataContext = sessionSettings;
			
			InitializeComponent();
			App.CompositionContainer.ComposeParts(this);
			tabControl.SelectionChanged += tabControl_SelectionChanged;
			
			if (sessionSettings.LeftColumnWidth > 0)
				leftColumn.Width = new GridLength(sessionSettings.LeftColumnWidth, GridUnitType.Pixel);
			sessionSettings.FilterSettings.PropertyChanged += filterSettings_PropertyChanged;
			
			InitMainMenu();
			InitToolbar();
			
			this.Loaded += MainWindow_Loaded;
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
			var list = (IList<CommandBinding>)handler.Editing.CommandBindings;
			for (int i = list.Count - 1; i >= 0; i--) {
				var binding = list[i];
				// Ctrl+D: used by GoToToken
				// Backspace: used by BrowseBack
				if (binding.Command == ICSharpCode.AvalonEdit.AvalonEditCommands.DeleteLine ||
					binding.Command == System.Windows.Documents.EditingCommands.Backspace)
					list.RemoveAt(i);
			}
		}

		TabState CreateNewTabState()
		{
			TabState tabState = new TabState(treeView);
			tabControl.Items.Add(tabState.TabItem);

			var view = tabState.TextView;
			RemoveCommands(view);
			view.TextEditor.TextArea.MouseRightButtonDown += delegate { view.GoToMousePosition(); };
			view.TextEditor.WordWrap = sessionSettings.WordWrap;
			view.TextEditor.Options.HighlightCurrentLine = sessionSettings.HighlightCurrentLine;

			if (OnDecompilerTextViewAdded != null)
				OnDecompilerTextViewAdded(this, new DecompilerTextViewEventArgs(view));

			return tabState;
		}

		void RemoveTabState(TabState tabState)
		{
			if (tabState == null)
				return;
			int index = tabControl.Items.IndexOf(tabState.TabItem);
			Debug.Assert(index >= 0);
			if (index < 0)
				return;

			tabControl.SelectedIndex = index - 1;
			tabControl.Items.RemoveAt(index);

			RemoveTabStateInternal(tabState);
		}

		void RemoveAllTabStates()
		{
			var allTabStates = AllTabStates.ToArray();
			tabControl.Items.Clear();
			foreach (var tabState in allTabStates)
				RemoveTabStateInternal(tabState);
		}

		void RemoveTabStateInternal(TabState tabState)
		{
			if (OnDecompilerTextViewRemoved != null)
				OnDecompilerTextViewRemoved(this, new DecompilerTextViewEventArgs(tabState.TextView));
			tabState.Dispose();
		}

		public event EventHandler<DecompilerTextViewEventArgs> OnDecompilerTextViewAdded;
		public event EventHandler<DecompilerTextViewEventArgs> OnDecompilerTextViewRemoved;
		public class DecompilerTextViewEventArgs : EventArgs
		{
			public readonly DecompilerTextView DecompilerTextView;

			public DecompilerTextViewEventArgs(DecompilerTextView decompilerTextView)
			{
				this.DecompilerTextView = decompilerTextView;
			}
		}

		void SelectTreeViewNodes(TabState tabState, ILSpyTreeNode[] nodes)
		{
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

		bool tabControl_SelectionChanged_dont_select = false;
		void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender != tabControl || e.Source != tabControl)
				return;
			Debug.Assert(e.RemovedItems.Count <= 1);
			Debug.Assert(e.AddedItems.Count <= 1);

			var oldState = e.RemovedItems.Count >= 1 ? (TabState)((TabItem)e.RemovedItems[0]).Tag : null;
			var newState = e.AddedItems.Count >= 1 ? (TabState)((TabItem)e.AddedItems[0]).Tag : null;

			var oldView = oldState == null ? null : oldState.TextView;
			var newView = newState == null ? null : newState.TextView;

			if (oldView != null) {
				foreach (CommandBinding binding in oldView.CommandBindings)
					this.CommandBindings.Remove(binding);
			}
			if (newView != null)
				this.CommandBindings.AddRange(newView.CommandBindings);

			if (tabControl_SelectionChanged_dont_select) {
			}
			else if (newState == null)
				treeView.SelectedItems.Clear();
			else
				SelectTreeViewNodes(newState, newState.DecompiledNodes);

			ClosePopups();
			if (oldView != null)
				UninstallTextEditorListeners(oldView);
			if (newView != null)
				InstallTextEditorListeners(newView);

			SetTextEditorFocus(newView);

			if (OnActiveDecompilerTextViewChanged != null)
				OnActiveDecompilerTextViewChanged(this, new ActiveDecompilerTextViewChangedEventArgs(oldView, newView));
		}

		void SetTextEditorFocus(DecompilerTextView textView) {
			if (textView == null)
				return;

			// Set focus to the text area whenever the view is selected
			var textArea = textView.TextEditor.TextArea;
			if (!textArea.IsVisible)
				textArea.IsVisibleChanged += TextArea_IsVisibleChanged;
			else
				textArea.Focus();
		}

		void TextArea_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var textArea = (ICSharpCode.AvalonEdit.Editing.TextArea)sender;
			textArea.IsVisibleChanged -= TextArea_IsVisibleChanged;
			textArea.Focus();
		}

		public event EventHandler<ActiveDecompilerTextViewChangedEventArgs> OnActiveDecompilerTextViewChanged;
		public class ActiveDecompilerTextViewChangedEventArgs : EventArgs
		{
			/// <summary>
			/// Old view. Can be null
			/// </summary>
			public readonly DecompilerTextView OldView;

			/// <summary>
			/// New view. Can be null
			/// </summary>
			public readonly DecompilerTextView NewView;

			public ActiveDecompilerTextViewChangedEventArgs(DecompilerTextView oldView, DecompilerTextView newView)
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

		public void SetTitle(DecompilerTextView textView, string title)
		{
			var tabState = TabState.GetTabState(textView);
			tabState.Title = title;
			tabState.InitializeHeader();
		}

		internal void ClosePopups()
		{
			if (textEditorListeners == null)
				return;
			foreach (var listener in textEditorListeners)
				listener.ClosePopup();
		}

		void BuildThemeMenu()
		{
			themeMenu.Items.Clear();
			foreach (var theme in Themes.AllThemes.OrderBy(x => x.Sort)) {
				var mi = new MenuItem {
					Header = theme.MenuName,
					Tag = theme,
				};
				if (Theme == theme)
					mi.IsChecked = true;
				mi.Click += ThemeMenuItem_Click;
				themeMenu.Items.Add(mi);
			}
		}

		void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var mi = (MenuItem)sender;
			foreach (MenuItem menuItem in themeMenu.Items)
				menuItem.IsChecked = menuItem == mi;
			SetTheme((Theme)mi.Tag);
		}

		void SetTheme(Theme theme)
		{
			if (theme == null)
				return;
			if (theme == Theme)
				return;
			Theme = theme;
			OnThemeUpdated();
		}

		void OnThemeUpdated()
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
			return new Button {
				Command = CommandWrapper.Unwrap(command.Value),
				ToolTip = command.Metadata.ToolTip,
				Tag = command.Metadata.Tag,
				Content = new Image {
					Width = 16,
					Height = 16,
					Source = Images.LoadImage(command.Value, command.Metadata.ToolbarIcon)
				}
			};
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
			var state = subMenusDict[menuHeader];
			int index = mainMenu.Items.IndexOf(state.TopLevelMenuItem);
			mainMenu.Items.RemoveAt(index);
			var newItem = new MenuItem();
			newItem.Header = state.TopLevelMenuItem.Header;
			newItem.Name = state.TopLevelMenuItem.Name;
			state.TopLevelMenuItem = newItem;
			mainMenu.Items.Insert(index, newItem);
			InitializeMainSubMenu(state);
		}

		static void InitializeMainSubMenu(MainSubMenuState state)
		{
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
					MenuItem menuItem = new MenuItem();
					menuItem.Command = CommandWrapper.Unwrap(entry.Value);
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
			ContextMenuProvider.Add(treeView);
			ContextMenuProvider.Add(tabControl);
			BuildThemeMenu();
			OnThemeUpdated();

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
					foreach (var savedState in sessionSettings.SavedTabStates)
						CreateTabState(savedState);
					if (!sessionSettings.TabsFound)
						AboutPage.Display(SafeActiveTextView);

					int selectedIndex = unchecked((uint)sessionSettings.ActiveTabIndex) < (uint)tabControl.Items.Count ?
								sessionSettings.ActiveTabIndex : tabControl.Items.Count == 0 ? -1 : 0;
					tabControl.SelectedIndex = selectedIndex;
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

			RemoveAllTabStates();
			this.assemblyList = assemblyList;

			// Make sure memory usage doesn't increase out of control. This method allocates lots of
			// new stuff, but the GC doesn't bother to reclaim that memory for a long time.
			GC.Collect();
			GC.WaitForPendingFinalizers();
			
			assemblyList.assemblies.CollectionChanged += assemblyList_Assemblies_CollectionChanged;
			
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
			if (e.Action == NotifyCollectionChangedAction.Reset)
				RemoveAllTabStates();
			if (e.OldItems != null) {
				var oldAssemblies = new HashSet<LoadedAssembly>(e.OldItems.Cast<LoadedAssembly>());
				foreach (var tabState in AllTabStates.ToArray()) {
					tabState.History.RemoveAll(n => n.TreeNodes.Any(
						nd => nd.AncestorsAndSelf().OfType<AssemblyTreeNode>().Any(
							a => oldAssemblies.Contains(a.LoadedAssembly))));

					foreach (var node in tabState.DecompiledNodes) {
						var asmNode = GetAssemblyTreeNode(node);
						if (asmNode != null && oldAssemblies.Contains(asmNode.LoadedAssembly)) {
							RemoveTabState(tabState);
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
				foreach (var tabState in AllTabStates) {
					//TODO: Restore the caret too
					DecompileNodes(tabState, null, false, tabState.DecompiledNodes, true);
				}
			}
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
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), success => GoToLocation(tabState.TextView, success, ResolveReference(reference)));
		}

		public bool JumpToReference(DecompilerTextView textView, object reference, bool canRecordHistory = true)
		{
			var tabState = TabState.GetTabState(textView);
			if (canRecordHistory)
				RecordHistory(tabState);
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), success => GoToLocation(tabState.TextView, success, ResolveReference(reference)));
		}

		public bool JumpToReference(DecompilerTextView textView, object reference, Func<TextLocation> getLocation, bool canRecordHistory = true)
		{
			var tabState = TabState.GetTabState(textView);
			if (canRecordHistory)
				RecordHistory(tabState);
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), success => GoToLocation(tabState.TextView, success, getLocation()));
		}

		public bool JumpToReference(DecompilerTextView textView, object reference, Action<bool> onDecompileFinished, bool canRecordHistory = true)
		{
			var tabState = TabState.GetTabState(textView);
			if (canRecordHistory)
				RecordHistory(tabState);
			return JumpToReferenceAsyncInternal(tabState, true, FixReference(reference), onDecompileFinished);
		}

		void GoToLocation(DecompilerTextView decompilerTextView, bool success, object destLoc)
		{
			if (!success || destLoc == null)
				return;
			decompilerTextView.GoToLocation(destLoc);
		}

		sealed class OnShowOutputHelper
		{
			DecompilerTextView decompilerTextView;
			readonly Action<bool> onDecompileFinished;
			readonly ILSpyTreeNode[] nodes;
			public OnShowOutputHelper(DecompilerTextView decompilerTextView, Action<bool> onDecompileFinished, ILSpyTreeNode node)
				: this(decompilerTextView, onDecompileFinished, new[] { node })
			{
			}

			public OnShowOutputHelper(DecompilerTextView decompilerTextView, Action<bool> onDecompileFinished, ILSpyTreeNode[] nodes)
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
					onDecompileFinished(success);
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
		bool JumpToReferenceAsyncInternal(TabState tabState, bool canLoad, object reference, Action<bool> onDecompileFinished)
		{
			ILSpyTreeNode treeNode = FindTreeNode(reference);
			if (treeNode != null) {
				var helper = new OnShowOutputHelper(tabState.TextView, onDecompileFinished, treeNode);
				var nodes = new[] { treeNode };
				bool? decompiled = DecompileNodes(tabState, null, false, nodes);
				if (decompiled == false) {
					helper.Abort();
					onDecompileFinished(true);
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
				assemblyList.AddAssembly(loadedAsm, true);
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
			int selectedIndex = tabControl.SelectedIndex;

			var allTabsState = new List<SavedTabState>();
			foreach (var tabState in AllTabStates.ToArray())
				allTabsState.Add(CreateSavedTabState(tabState));

			try {
				TreeView_SelectionChanged_ignore = true;
				ShowAssemblyList(assemblyListManager.LoadList(ILSpySettings.Load(), assemblyList.ListName));
			}
			finally {
				TreeView_SelectionChanged_ignore = false;
			}

			foreach (var savedState in allTabsState)
				CreateTabState(savedState);

			tabControl.SelectedIndex = selectedIndex;
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
		bool TreeView_SelectionChanged_ignore = false;
		void TreeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (TreeView_SelectionChanged_ignore)
				return;
			var tabState = SafeActiveTabState;
			DecompileNodes(tabState, null, true, this.SelectedNodes.ToArray());

			if (SelectionChanged != null)
				SelectionChanged(sender, e);

			if (ICSharpCode.ILSpy.Options.DisplaySettingsPanel.CurrentDisplaySettings.AutoFocusTextView)
				SetTextEditorFocus(tabState.TextView);
		}
		
		bool? DecompileNodes(TabState tabState, DecompilerTextViewState state, bool recordHistory, ILSpyTreeNode[] nodes, bool forceDecompile = false)
		{
			if (tabState.ignoreDecompilationRequests)
				return null;
			if (!forceDecompile && tabState.IsSameNodes(nodes)) {
				if (state != null)
					tabState.TextView.EditorPositionState = state.EditorPositionState;
				return false;
			}
			tabState.DecompiledNodes = nodes ?? new ILSpyTreeNode[0];
			tabState.Title = null;
			tabState.InitializeHeader();
			
			if (recordHistory)
				RecordHistory(tabState);

			if (nodes.Length == 1 && nodes[0].View(tabState.TextView)) {
				tabState.TextView.CancelDecompileAsync();
				return true;
			}
			tabState.TextView.DecompileAsync(this.CurrentLanguage, nodes, new DecompilationOptions() { TextViewState = state });
			return true;
		}

		internal void RecordHistory(DecompilerTextView textView)
		{
			RecordHistory(TabState.GetTabState(textView));
		}

		void RecordHistory(TabState tabState)
		{
			if (tabState == null)
				return;
			var dtState = tabState.TextView.GetState();
			if (dtState != null)
				tabState.History.UpdateCurrent(new NavigationState(dtState));
			tabState.History.Record(new NavigationState(tabState.DecompiledNodes));
		}
		
		void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var textView = ActiveTextView;
			if (textView == null)
				return;
			if (this.SelectedNodes.Count() == 1) {
				if (this.SelectedNodes.Single().Save(textView))
					return;
			}
			textView.SaveToDisk(this.CurrentLanguage,
				this.SelectedNodes,
				new DecompilationOptions() { FullDecompilation = true });
		}
		
		public Language CurrentLanguage {
			get {
				return sessionSettings.FilterSettings.Language;
			}
		}

		public event SelectionChangedEventHandler SelectionChanged;

		public IEnumerable<ILSpyTreeNode> SelectedNodes {
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
			BackCommand(TabState.GetTabState(textView));
		}

		bool BackCommand(TabState tabState)
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
		
		void NavigateHistory(TabState tabState, bool forward)
		{
			var dtState = tabState.TextView.GetState();
			if(dtState != null)
				tabState.History.UpdateCurrent(new NavigationState(dtState));
			var newState = forward ? tabState.History.GoForward() : tabState.History.GoBack();
			var nodes = newState.TreeNodes.Cast<ILSpyTreeNode>().ToArray();
			SelectTreeViewNodes(tabState, nodes);
			DecompileNodes(tabState, newState.ViewState, false, nodes);
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
			sessionSettings.ThemeName = Theme.Name;
			sessionSettings.ActiveAssemblyList = assemblyList.ListName;
			sessionSettings.WindowBounds = this.RestoreBounds;
			sessionSettings.LeftColumnWidth = leftColumn.Width.Value;
			sessionSettings.TopPaneSettings = GetPaneSettings(topPane, topPaneRow);
			sessionSettings.BottomPaneSettings = GetPaneSettings(bottomPane, bottomPaneRow);

			var allTabStates = AllTabStates.ToArray();
			sessionSettings.ActiveTabIndex = tabControl.SelectedIndex;
			sessionSettings.SavedTabStates = new SavedTabState[allTabStates.Length];
			for (int i = 0; i < allTabStates.Length; i++)
				sessionSettings.SavedTabStates[i] = CreateSavedTabState(allTabStates[i]);

			sessionSettings.Save();
		}

		static SavedTabState CreateSavedTabState(TabState tabState)
		{
			var savedState = new SavedTabState();
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

		TabState CreateTabState(SavedTabState savedState, IList<ILSpyTreeNode> newNodes = null, bool decompile = true)
		{
			var tabState = CreateNewTabState();
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
					var helper = new OnShowOutputHelper(tabState.TextView, success => decompilerTextView_OnShowOutput(success, tabState.TextView, savedState), tmpNodes);
					DecompileNodes(tabState, null, true, tmpNodes);
				}
				else
					AboutPage.Display(tabState.TextView);
			}

			return tabState;
		}

		void decompilerTextView_OnShowOutput(bool success, DecompilerTextView textView, SavedTabState savedState)
		{
			if (!success)
				return;

			if (IsValid(textView, savedState.EditorPositionState))
				textView.EditorPositionState = savedState.EditorPositionState;
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
			lock (assemblyList.assemblies) {
				// Get or create the assembly
				var loadedAsm = assemblyList.OpenAssembly(asmFilename, true);

				// Common case is a one-file assembly or first module of a multifile assembly
				if (asmFilename.Equals(moduleFilename, StringComparison.OrdinalIgnoreCase))
					return loadedAsm;

				var loadedMod = assemblyListTreeNode.FindModule(loadedAsm, moduleFilename);
				if (loadedMod != null)
					return loadedMod;

				Debug.Fail("Shouldn't be here.");
				return assemblyList.OpenAssembly(moduleFilename, true);
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
			e.CanExecute = CloseActiveTabPossible();
		}

		// This method is only executed when the text editor does NOT have keyboard focus
		void SelectTab(int index)
		{
			if (tabControl.Items.Count == 0)
				return;
			if (index < 0)
				index += tabControl.Items.Count;
			index = index % tabControl.Items.Count;
			tabControl.SelectedIndex = index;
		}

		void SelectNextTab()
		{
			SelectTab(tabControl.SelectedIndex + 1);
		}

		bool SelectNextTabPossible()
		{
			return tabControl.Items.Count > 1;
		}

		void SelectPreviousTab()
		{
			SelectTab(tabControl.SelectedIndex - 1);
		}

		bool SelectPreviousTabPossible()
		{
			return tabControl.Items.Count > 1;
		}

		private void SelectNextTabExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			SelectNextTab();
		}

		private void SelectNextTabCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectNextTabPossible();
		}

		private void SelectPrevTabExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			SelectPreviousTab();
		}

		private void SelectPrevTabCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectPreviousTabPossible();
		}

		internal TabState CloneTab(TabState tabState, bool decompile = true)
		{
			if (tabState == null)
				return null;
			return CreateTabState(CreateSavedTabState(tabState), tabState.DecompiledNodes, decompile);
		}

		internal TabState CloneTabMakeActive(TabState tabState, bool decompile = true)
		{
			var clonedTabState = CloneTab(tabState, decompile);
			if (clonedTabState != null)
				tabControl.SelectedItem = clonedTabState.TabItem;
			return clonedTabState;
		}

		internal void OpenNewTab()
		{
			TabState tabState;
			var currenTabState = ActiveTabState;
			if (currenTabState != null && !ICSharpCode.ILSpy.Options.DisplaySettingsPanel.CurrentDisplaySettings.NewEmptyTabs)
				tabState = CloneTab(currenTabState);
			else
				tabState = CreateNewTabState();

			tabControl.SelectedItem = tabState.TabItem;
		}

		internal void OpenReferenceInNewTab(DecompilerTextView textView, ReferenceSegment reference)
		{
			if (textView == null || reference == null)
				return;
			var tabState = TabState.GetTabState(textView);
			var clonedTabState = CloneTabMakeActive(tabState, true);
			clonedTabState.History.Clear();
			clonedTabState.TextView.GoToTarget(reference, true, false);
		}

		internal void CloseActiveTab()
		{
			RemoveTabState(ActiveTabState);
		}

		internal bool CloseActiveTabPossible()
		{
			return ActiveTabState != null;
		}

		internal void CloseAllButActiveTab()
		{
			var activeTab = ActiveTabState;
			if (activeTab == null)
				return;
			foreach (var tabState in AllTabStates.ToArray()) {
				if (tabState != activeTab)
					RemoveTabState(tabState);
			}
		}

		internal bool CloseAllButActiveTabPossible()
		{
			return tabControl.Items.Count > 1;
		}

		internal void CloneActiveTab()
		{
			CloneTabMakeActive(ActiveTabState);
		}

		internal bool CloneActiveTabPossible()
		{
			return ActiveTabState != null;
		}
	}

	[ExportContextMenuEntry(Header = "Open in New _Tab", Order = 130, InputGestureText = "Ctrl+T", Category = "Tabs")]
	class OpenInNewTabContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Length > 0 &&
				context.TreeView == MainWindow.Instance.treeView;
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.OpenNewTab();
		}
	}

	[ExportContextMenuEntry(Header = "_Close", Order = 100, InputGestureText = "Ctrl+W", Category = "Tabs")]
	class CloseTabContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.TabControl == MainWindow.Instance.tabControl &&
				MainWindow.Instance.CloseActiveTabPossible();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.CloseActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "Close _All But This", Order = 110, Category = "Tabs")]
	class CloseAllTabsButThisContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.TabControl == MainWindow.Instance.tabControl &&
				MainWindow.Instance.CloseAllButActiveTabPossible();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.CloseAllButActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "Clone _Tab", Order = 120, Category = "Tabs")]
	class CloneTabContextMenuEntry : IContextMenuEntry
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.TabControl == MainWindow.Instance.tabControl &&
				MainWindow.Instance.CloneActiveTabPossible();
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.CloneActiveTab();
		}
	}

	[ExportContextMenuEntry(Header = "Open in New _Tab", Order = 130, Category = "Tabs")]
	class OpenReferenceInNewTabContextMenuEntry : IContextMenuEntry2
	{
		public bool IsVisible(TextViewContext context)
		{
			return context.TextView != null &&
				context.Reference != null;
		}

		public bool IsEnabled(TextViewContext context)
		{
			return true;
		}

		public void Execute(TextViewContext context)
		{
			MainWindow.Instance.OpenReferenceInNewTab(context.TextView, context.Reference);
		}

		public void Initialize(TextViewContext context, MenuItem menuItem)
		{
			menuItem.InputGestureText = context.OpenedFromKeyboard ? "Ctrl+F12" : "Ctrl+Click";
		}
	}
}
