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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.ILSpy.TreeNodes.Analyzer;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.TreeView;
using Microsoft.Win32;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// The main window of the application.
	/// </summary>
	partial class MainWindow : Window
	{
		NavigationHistory<NavigationState> history = new NavigationHistory<NavigationState>();
		ILSpySettings spySettings;
		SessionSettings sessionSettings;
		AssemblyListManager assemblyListManager;
		AssemblyList assemblyList;
		AssemblyListTreeNode assemblyListTreeNode;
		
		[Import]
		DecompilerTextView decompilerTextView = null;
		
		static MainWindow instance;
		
		public static MainWindow Instance {
			get { return instance; }
		}
		
		public MainWindow()
		{
			instance = this;
			spySettings = ILSpySettings.Load();
			this.sessionSettings = new SessionSettings(spySettings);
			this.assemblyListManager = new AssemblyListManager(spySettings);
			
			this.Icon = new BitmapImage(new Uri("pack://application:,,,/ILSpy;component/images/ILSpy.ico"));
			
			this.DataContext = sessionSettings;
			this.Left = sessionSettings.WindowBounds.Left;
			this.Top = sessionSettings.WindowBounds.Top;
			this.Width = sessionSettings.WindowBounds.Width;
			this.Height = sessionSettings.WindowBounds.Height;
			// TODO: validate bounds (maybe a monitor was removed...)
			this.WindowState = sessionSettings.WindowState;
			
			InitializeComponent();
			App.CompositionContainer.ComposeParts(this);
			mainPane.Content = decompilerTextView;
			
			if (sessionSettings.SplitterPosition > 0 && sessionSettings.SplitterPosition < 1) {
				leftColumn.Width = new GridLength(sessionSettings.SplitterPosition, GridUnitType.Star);
				rightColumn.Width = new GridLength(1 - sessionSettings.SplitterPosition, GridUnitType.Star);
			}
			sessionSettings.FilterSettings.PropertyChanged += filterSettings_PropertyChanged;
			
			InitMainMenu();
			InitToolbar();
			ContextMenuProvider.Add(treeView);
			
			this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
		}
		
		#region Toolbar extensibility
		[ImportMany("ToolbarCommand", typeof(ICommand))]
		Lazy<ICommand, IToolbarCommandMetadata>[] toolbarCommands = null;
		
		void InitToolbar()
		{
			int navigationPos = 0;
			int openPos = 1;
			foreach (var commandGroup in toolbarCommands.OrderBy(c => c.Metadata.ToolbarOrder).GroupBy(c => c.Metadata.ToolbarCategory)) {
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
					toolBar.Items.Add(new Separator());
					foreach (var command in commandGroup) {
						toolBar.Items.Add(MakeToolbarItem(command));
					}
				}
			}
			
		}
		
		Button MakeToolbarItem(Lazy<ICommand, IToolbarCommandMetadata> command)
		{
			return new Button {
				Command = CommandWrapper.Unwrap(command.Value),
				ToolTip = command.Metadata.ToolTip,
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
		
		void InitMainMenu()
		{
			foreach (var topLevelMenu in mainMenuCommands.OrderBy(c => c.Metadata.MenuOrder).GroupBy(c => c.Metadata.Menu)) {
				var topLevelMenuItem = mainMenu.Items.OfType<MenuItem>().FirstOrDefault(m => (m.Header as string) == topLevelMenu.Key);
				foreach (var category in topLevelMenu.GroupBy(c => c.Metadata.MenuCategory)) {
					if (topLevelMenuItem == null) {
						topLevelMenuItem = new MenuItem();
						topLevelMenuItem.Header = topLevelMenu.Key;
						mainMenu.Items.Add(topLevelMenuItem);
					} else if (topLevelMenuItem.Items.Count > 0) {
						topLevelMenuItem.Items.Add(new Separator());
					}
					foreach (var entry in category) {
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
						topLevelMenuItem.Items.Add(menuItem);
					}
				}
			}
		}
		#endregion
		
		#region Message Hook
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			HwndSource source = PresentationSource.FromVisual(this) as HwndSource;;
			if (source != null) {
				source.AddHook(WndProc);
			}
		}
		
		unsafe IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == NativeMethods.WM_COPYDATA) {
				CopyDataStruct* copyData = (CopyDataStruct*)lParam;
				string data = new string((char*)copyData->Buffer, 0, copyData->Size / sizeof(char));
				if (data.StartsWith("ILSpy:\r\n", StringComparison.Ordinal)) {
					data = data.Substring(8);
					List<string> lines = new List<string>();
					using (StringReader r = new StringReader(data)) {
						string line;
						while ((line = r.ReadLine()) != null)
							lines.Add(line);
					}
					var args = new CommandLineArguments(lines);
					if (HandleCommandLineArguments(args)) {
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
				foreach (LoadedAssembly asm in commandLineLoadedAssemblies) {
					AssemblyDefinition def = asm.AssemblyDefinition;
					if (def != null) {
						MemberReference mr = XmlDocKeyProvider.FindMemberByKey(def.MainModule, args.NavigateTo);
						if (mr != null) {
							found = true;
							JumpToReference(mr);
							break;
						}
					}
				}
				if (!found) {
					AvalonEditTextOutput output = new AvalonEditTextOutput();
					output.Write("Cannot find " + args.NavigateTo);
					decompilerTextView.Show(output);
				}
			}
			commandLineLoadedAssemblies.Clear(); // clear references once we don't need them anymore
		}
		
		void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
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
			
			if (App.CommandLineArguments.NavigateTo == null) {
				SharpTreeNode node = FindNodeByPath(sessionSettings.ActiveTreeViewPath, true);
				if (node != null) {
					SelectNode(node);
					
					// only if not showing the about page, perform the update check:
					ShowMessageIfUpdatesAvailableAsync(spySettings);
				} else {
					AboutPage.Display(decompilerTextView);
				}
			}
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
		
		void ShowAssemblyList(AssemblyList assemblyList)
		{
			history.Clear();
			this.assemblyList = assemblyList;
			
			assemblyList.assemblies.CollectionChanged += assemblyList_Assemblies_CollectionChanged;
			
			assemblyListTreeNode = new AssemblyListTreeNode(assemblyList);
			assemblyListTreeNode.FilterSettings = sessionSettings.FilterSettings.Clone();
			assemblyListTreeNode.Select = node => SelectNode(node);
			treeView.Root = assemblyListTreeNode;
			
			if (assemblyList.ListName == AssemblyListManager.DefaultListName)
				this.Title = "ILSpy";
			else
				this.Title = "ILSpy - " + assemblyList.ListName;
		}

		void assemblyList_Assemblies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
				foreach (LoadedAssembly asm in e.OldItems)
					history.RemoveAll(n => n.TreeNodes.Any(nd => nd.AncestorsAndSelf().OfType<AssemblyTreeNode>().Any(a => a.LoadedAssembly == asm)));
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
				typeof(Mono.Cecil.AssemblyDefinition).Assembly,
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
				DecompileSelectedNodes();
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
		internal void SelectNode(SharpTreeNode obj)
		{
			if (obj != null) {
				// Set both the selection and focus to ensure that keyboard navigation works as expected.
				treeView.FocusNode(obj);
				treeView.SelectedItem = obj;
			}
		}
		
		/// <summary>
		/// Retrieves a node using the .ToString() representations of its ancestors.
		/// </summary>
		SharpTreeNode FindNodeByPath(string[] path, bool returnBestMatch)
		{
			if (path == null)
				return null;
			SharpTreeNode node = treeView.Root;
			SharpTreeNode bestMatch = node;
			foreach (var element in path) {
				if (node == null)
					break;
				bestMatch = node;
				node.EnsureLazyChildren();
				node = node.Children.FirstOrDefault(c => c.ToString() == element);
			}
			if (returnBestMatch)
				return node ?? bestMatch;
			else
				return node;
		}
		
		/// <summary>
		/// Gets the .ToString() representation of the node's ancestors.
		/// </summary>
		string[] GetPathForNode(SharpTreeNode node)
		{
			if (node == null)
				return null;
			List<string> path = new List<string>();
			while (node.Parent != null) {
				path.Add(node.ToString());
				node = node.Parent;
			}
			path.Reverse();
			return path.ToArray();
		}
		
		public void JumpToReference(object reference)
		{
			if (reference is TypeReference) {
				SelectNode(assemblyListTreeNode.FindTypeNode(((TypeReference)reference).Resolve()));
			} else if (reference is MethodReference) {
				SelectNode(assemblyListTreeNode.FindMethodNode(((MethodReference)reference).Resolve()));
			} else if (reference is FieldReference) {
				SelectNode(assemblyListTreeNode.FindFieldNode(((FieldReference)reference).Resolve()));
			} else if (reference is PropertyReference) {
				SelectNode(assemblyListTreeNode.FindPropertyNode(((PropertyReference)reference).Resolve()));
			} else if (reference is EventReference) {
				SelectNode(assemblyListTreeNode.FindEventNode(((EventReference)reference).Resolve()));
			} else if (reference is AssemblyDefinition) {
				SelectNode(assemblyListTreeNode.FindAssemblyNode((AssemblyDefinition)reference));
			} else if (reference is Mono.Cecil.Cil.OpCode) {
				string link = "http://msdn.microsoft.com/library/system.reflection.emit.opcodes." + ((Mono.Cecil.Cil.OpCode)reference).Code.ToString().ToLowerInvariant() + ".aspx";
				try {
					Process.Start(link);
				} catch {
					
				}
			}
		}
		#endregion
		
		#region Open/Refresh
		void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			e.Handled = true;
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = ".NET assemblies|*.dll;*.exe|All files|*.*";
			dlg.Multiselect = true;
			dlg.RestoreDirectory = true;
			if (dlg.ShowDialog() == true) {
				OpenFiles(dlg.FileNames);
			}
		}
		
		public void OpenFiles(string[] fileNames)
		{
			if (fileNames == null)
				throw new ArgumentNullException("fileNames");
			treeView.UnselectAll();
			SharpTreeNode lastNode = null;
			foreach (string file in fileNames) {
				var asm = assemblyList.OpenAssembly(file);
				if (asm != null) {
					var node = assemblyListTreeNode.FindAssemblyNode(asm);
					if (node != null) {
						treeView.SelectedItems.Add(node);
						lastNode = node;
					}
				}
			}
			if (lastNode != null)
				treeView.FocusNode(lastNode);
		}
		
		void RefreshCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			e.Handled = true;
			var path = GetPathForNode(treeView.SelectedItem as SharpTreeNode);
			ShowAssemblyList(assemblyListManager.LoadList(ILSpySettings.Load(), assemblyList.ListName));
			SelectNode(FindNodeByPath(path, true));
		}
		
		void SearchCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			SearchPane.Instance.Show();
		}
		#endregion
		
		#region Decompile (TreeView_SelectionChanged)
		void TreeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			DecompileSelectedNodes();
		}

		private bool ignoreDecompilationRequests;

		private void DecompileSelectedNodes(DecompilerTextViewState state = null, bool recordHistory = true)
		{
			if (ignoreDecompilationRequests)
				return;

			if (recordHistory)
				history.Record(new NavigationState(treeView.SelectedItems.OfType<SharpTreeNode>(), decompilerTextView.GetState()));

			if (treeView.SelectedItems.Count == 1) {
				ILSpyTreeNode node = treeView.SelectedItem as ILSpyTreeNode;
				if (node != null && node.View(decompilerTextView))
					return;
			}
			decompilerTextView.Decompile(this.CurrentLanguage, this.SelectedNodes, new DecompilationOptions() { TextViewState = state });
		}
		
		void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (this.SelectedNodes.Count() == 1) {
				if (this.SelectedNodes.Single().Save(this.TextView))
					return;
			}
			this.TextView.SaveToDisk(this.CurrentLanguage,
			                         this.SelectedNodes,
			                         new DecompilationOptions() { FullDecompilation = true });
		}
		
		public void RefreshDecompiledView()
		{
			DecompileSelectedNodes();
		}
		
		public DecompilerTextView TextView {
			get { return decompilerTextView; }
		}
		
		public Language CurrentLanguage {
			get {
				return sessionSettings.FilterSettings.Language;
			}
		}
		
		public IEnumerable<ILSpyTreeNode> SelectedNodes {
			get {
				return treeView.GetTopLevelSelection().OfType<ILSpyTreeNode>();
			}
		}
		#endregion
		
		#region Back/Forward navigation
		void BackCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.Handled = true;
			e.CanExecute = history.CanNavigateBack;
		}
		
		void BackCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (history.CanNavigateBack) {
				e.Handled = true;
				NavigateHistory(false);
			}
		}
		
		void ForwardCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.Handled = true;
			e.CanExecute = history.CanNavigateForward;
		}
		
		void ForwardCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (history.CanNavigateForward) {
				e.Handled = true;
				NavigateHistory(true);
			}
		}

		void NavigateHistory(bool forward)
		{
			var combinedState = new NavigationState(treeView.SelectedItems.OfType<SharpTreeNode>(), decompilerTextView.GetState());
			history.Record(combinedState, replace: true, clearForward: false);
			var newState = forward ? history.GoForward() : history.GoBack();

			ignoreDecompilationRequests = true;
			treeView.SelectedItems.Clear();
			foreach (var node in newState.TreeNodes)
			{
				treeView.SelectedItems.Add(node);
			}
			if (newState.TreeNodes.Any())
				treeView.FocusNode(newState.TreeNodes.First());
			ignoreDecompilationRequests = false;
			DecompileSelectedNodes(newState.ViewState, false);
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
			sessionSettings.ActiveAssemblyList = assemblyList.ListName;
			sessionSettings.ActiveTreeViewPath = GetPathForNode(treeView.SelectedItem as SharpTreeNode);
			sessionSettings.WindowBounds = this.RestoreBounds;
			sessionSettings.SplitterPosition = leftColumn.Width.Value / (leftColumn.Width.Value + rightColumn.Width.Value);
			if (topPane.Visibility == Visibility.Visible)
				sessionSettings.BottomPaneSplitterPosition = topPaneRow.Height.Value / (topPaneRow.Height.Value + textViewRow.Height.Value);
			if (bottomPane.Visibility == Visibility.Visible)
				sessionSettings.BottomPaneSplitterPosition = bottomPaneRow.Height.Value / (bottomPaneRow.Height.Value + textViewRow.Height.Value);
			sessionSettings.Save();
		}
		
		#region Top/Bottom Pane management
		public void ShowInTopPane(string title, object content)
		{
			topPaneRow.MinHeight = 100;
			if (sessionSettings.TopPaneSplitterPosition > 0 && sessionSettings.TopPaneSplitterPosition < 1) {
				textViewRow.Height = new GridLength(1 - sessionSettings.TopPaneSplitterPosition, GridUnitType.Star);
				topPaneRow.Height = new GridLength(sessionSettings.TopPaneSplitterPosition, GridUnitType.Star);
			}
			topPane.Title = title;
			topPane.Content = content;
			topPane.Visibility = Visibility.Visible;
		}
		
		void TopPane_CloseButtonClicked(object sender, EventArgs e)
		{
			sessionSettings.TopPaneSplitterPosition = topPaneRow.Height.Value / (topPaneRow.Height.Value + textViewRow.Height.Value);
			topPaneRow.MinHeight = 0;
			topPaneRow.Height = new GridLength(0);
			topPane.Visibility = Visibility.Collapsed;
			
			IPane pane = topPane.Content as IPane;
			topPane.Content = null;
			if (pane != null)
				pane.Closed();
		}
		
		public void ShowInBottomPane(string title, object content)
		{
			bottomPaneRow.MinHeight = 100;
			if (sessionSettings.BottomPaneSplitterPosition > 0 && sessionSettings.BottomPaneSplitterPosition < 1) {
				textViewRow.Height = new GridLength(1 - sessionSettings.BottomPaneSplitterPosition, GridUnitType.Star);
				bottomPaneRow.Height = new GridLength(sessionSettings.BottomPaneSplitterPosition, GridUnitType.Star);
			}
			bottomPane.Title = title;
			bottomPane.Content = content;
			bottomPane.Visibility = Visibility.Visible;
		}
		
		void BottomPane_CloseButtonClicked(object sender, EventArgs e)
		{
			sessionSettings.BottomPaneSplitterPosition = bottomPaneRow.Height.Value / (bottomPaneRow.Height.Value + textViewRow.Height.Value);
			bottomPaneRow.MinHeight = 0;
			bottomPaneRow.Height = new GridLength(0);
			bottomPane.Visibility = Visibility.Collapsed;
			
			IPane pane = bottomPane.Content as IPane;
			bottomPane.Content = null;
			if (pane != null)
				pane.Closed();
		}
		#endregion
	}
}
