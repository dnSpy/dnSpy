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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.FlowAnalysis;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;
using ILSpy.Debugger.AvalonEdit;
using ILSpy.Debugger.Services;
using ILSpy.Debugger.UI;
using Microsoft.Win32;
using Mono.Cecil.Rocks;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// The main window of the application.
	/// </summary>
	partial class MainWindow : Window
	{
		NavigationHistory history = new NavigationHistory();
		ILSpySettings spySettings;
		SessionSettings sessionSettings;
		AssemblyListManager assemblyListManager;
		AssemblyList assemblyList;
		AssemblyListTreeNode assemblyListTreeNode;
		
		public MainWindow()
		{
			spySettings = ILSpySettings.Load();
			this.sessionSettings = new SessionSettings(spySettings);
			this.assemblyListManager = new AssemblyListManager(spySettings);
			
			this.DataContext = sessionSettings;
			this.Left = sessionSettings.WindowBounds.Left;
			this.Top = sessionSettings.WindowBounds.Top;
			this.Width = sessionSettings.WindowBounds.Width;
			this.Height = sessionSettings.WindowBounds.Height;
			// TODO: validate bounds (maybe a monitor was removed...)
			this.WindowState = sessionSettings.WindowState;
			
			InitializeComponent();
			decompilerTextView.mainWindow = this;
			
			if (sessionSettings.SplitterPosition > 0 && sessionSettings.SplitterPosition < 1) {
				leftColumn.Width = new GridLength(sessionSettings.SplitterPosition, GridUnitType.Star);
				rightColumn.Width = new GridLength(1 - sessionSettings.SplitterPosition, GridUnitType.Star);
			}
			sessionSettings.FilterSettings.PropertyChanged += filterSettings_PropertyChanged;
			
			#if DEBUG
			AddDebugItemsToToolbar();
			#endif
			this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
		}

		void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			ILSpySettings spySettings = this.spySettings;
			this.spySettings = null;
			
			// Load AssemblyList only in Loaded event so that WPF is initialized before we start the CPU-heavy stuff.
			// This makes the UI come up a bit faster.
			this.assemblyList = assemblyListManager.LoadList(spySettings, sessionSettings.ActiveAssemblyList);
			
			ShowAssemblyList(this.assemblyList);
			
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 1; i < args.Length; i++) {
				assemblyList.OpenAssembly(args[i]);
			}
			if (assemblyList.GetAssemblies().Length == 0)
				LoadInitialAssemblies();
			
			SharpTreeNode node = FindNodeByPath(sessionSettings.ActiveTreeViewPath, true);
			if (node != null) {
				SelectNode(node);
				
				// only if not showing the about page, perform the update check:
				ShowMessageIfUpdatesAvailableAsync(spySettings);
			} else {
				AboutPage.Display(decompilerTextView);
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
				foreach (AssemblyTreeNode node in e.OldItems)
					history.RemoveAll(n => n.AncestorsAndSelf().Contains(node));
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
				typeof(ICSharpCode.Decompiler.GraphVizGraph).Assembly,
				typeof(MainWindow).Assembly
			};
			foreach (System.Reflection.Assembly asm in initialAssemblies)
				assemblyList.OpenAssembly(asm.Location);
		}

		void filterSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			// filterSettings is mutable; but the ILSpyTreeNode filtering assumes that filter settings are immutable.
			// Thus, the main window will use one mutable instance (for data-binding), and assign a new clone to the ILSpyTreeNodes whenever the main
			// mutable instance changes.
			if (assemblyListTreeNode != null)
				assemblyListTreeNode.FilterSettings = sessionSettings.FilterSettings.Clone();
			if (e.PropertyName == "Language") {
				TreeView_SelectionChanged(null, null);
			}
		}
		
		internal AssemblyList AssemblyList {
			get { return assemblyList; }
		}
		
		#region Node Selection
		internal void SelectNode(SharpTreeNode obj, bool recordNavigationInHistory = true)
		{
			if (obj != null) {
				SharpTreeNode oldNode = treeView.SelectedItem as SharpTreeNode;
				if (oldNode != null && recordNavigationInHistory)
					history.Record(oldNode);
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
		#endregion
		
		#region Debugging CFG
		#if DEBUG
		void AddDebugItemsToToolbar()
		{
			toolBar.Items.Add(new Separator());
			
			Button cfg = new Button() { Content = "CFG" };
			cfg.Click += new RoutedEventHandler(cfg_Click);
			toolBar.Items.Add(cfg);
			
			Button ssa = new Button() { Content = "SSA" };
			ssa.Click += new RoutedEventHandler(ssa_Click);
			toolBar.Items.Add(ssa);
			
			Button varGraph = new Button() { Content = "Var" };
			varGraph.Click += new RoutedEventHandler(varGraph_Click);
			toolBar.Items.Add(varGraph);
		}
		
		void cfg_Click(object sender, RoutedEventArgs e)
		{
			MethodTreeNode node = treeView.SelectedItem as MethodTreeNode;
			if (node != null && node.MethodDefinition.HasBody) {
				var cfg = ControlFlowGraphBuilder.Build(node.MethodDefinition.Body);
				cfg.ComputeDominance();
				cfg.ComputeDominanceFrontier();
				ShowGraph(node.MethodDefinition.Name + "-cfg", cfg.ExportGraph());
			}
		}
		
		void ssa_Click(object sender, RoutedEventArgs e)
		{
			MethodTreeNode node = treeView.SelectedItem as MethodTreeNode;
			if (node != null && node.MethodDefinition.HasBody) {
				node.MethodDefinition.Body.SimplifyMacros();
				ShowGraph(node.MethodDefinition.Name + "-ssa", SsaFormBuilder.Build(node.MethodDefinition).ExportBlockGraph());
			}
		}
		
		void varGraph_Click(object sender, RoutedEventArgs e)
		{
			MethodTreeNode node = treeView.SelectedItem as MethodTreeNode;
			if (node != null && node.MethodDefinition.HasBody) {
				node.MethodDefinition.Body.SimplifyMacros();
				ShowGraph(node.MethodDefinition.Name + "-var", SsaFormBuilder.Build(node.MethodDefinition).ExportVariableGraph());
			}
		}
		
		void ShowGraph(string name, GraphVizGraph graph)
		{
			foreach (char c in Path.GetInvalidFileNameChars())
				name = name.Replace(c, '-');
			string fileName = Path.Combine(Path.GetTempPath(), name);
			graph.Save(fileName + ".gv");
			Process.Start("dot", "\"" + fileName + ".gv\" -Tpng -o \"" + fileName + ".png\"").WaitForExit();
			Process.Start(fileName + ".png");
		}
		#endif
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
		
		void OpenFiles(string[] fileNames)
		{
			treeView.UnselectAll();
			SharpTreeNode lastNode = null;
			foreach (string file in fileNames) {
				var asm = assemblyList.OpenAssembly(file);
				if (asm != null) {
					treeView.SelectedItems.Add(asm);
					lastNode = asm;
				}
			}
			if (lastNode != null)
				treeView.FocusNode(lastNode);
		}
		
		void OpenFromGac_Click(object sender, RoutedEventArgs e)
		{
			OpenFromGacDialog dlg = new OpenFromGacDialog();
			dlg.Owner = this;
			if (dlg.ShowDialog() == true) {
				OpenFiles(dlg.SelectedFileNames);
			}
		}
		
		#endregion
		
		#region Debugger commands
		
		void RefreshCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			e.Handled = true;
			var path = GetPathForNode(treeView.SelectedItem as SharpTreeNode);
			ShowAssemblyList(assemblyListManager.LoadList(ILSpySettings.Load(), assemblyList.ListName));
			SelectNode(FindNodeByPath(path, true));
		}
		
		void AttachToProcessExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (!DebuggerService.CurrentDebugger.IsDebugging) {
				var window = new AttachToProcessWindow();
				window.Owner = this;
				if (window.ShowDialog() == true)
				{
					AttachMenuItem.IsEnabled = AttachButton.IsEnabled = false;
					ContinueDebuggingMenuItem.IsEnabled =
						StepIntoMenuItem.IsEnabled =
						StepOverMenuItem.IsEnabled =
						StepOutMenuItem.IsEnabled =
						DetachMenuItem.IsEnabled = true;
				}
			}
		}
		
		void DetachFromProcessExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (DebuggerService.CurrentDebugger.IsDebugging){
				DebuggerService.CurrentDebugger.Detach();
				
				AttachMenuItem.IsEnabled = AttachButton.IsEnabled = true;
				ContinueDebuggingMenuItem.IsEnabled =
					StepIntoMenuItem.IsEnabled =
					StepOverMenuItem.IsEnabled =
					StepOutMenuItem.IsEnabled =
					DetachMenuItem.IsEnabled = false;
			}
		}
		
		void ContinueDebuggingExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (DebuggerService.CurrentDebugger.IsDebugging)
				DebuggerService.CurrentDebugger.Continue();
		}
		
		void StepIntoExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (DebuggerService.CurrentDebugger.IsDebugging)
				DebuggerService.CurrentDebugger.StepInto();
		}
		
		void StepOverExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (DebuggerService.CurrentDebugger.IsDebugging)
				DebuggerService.CurrentDebugger.StepOver();
		}
		
		void StepOutExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			if (DebuggerService.CurrentDebugger.IsDebugging)
				DebuggerService.CurrentDebugger.StepOut();
		}
		
		protected override void OnKeyUp(KeyEventArgs e)
		{
			switch (e.Key) {
				case Key.F5:
					ContinueDebuggingExecuted(null, null);
					e.Handled = true;
					break;
				case Key.System:
					StepOverExecuted(null, null);
					e.Handled = true;
					break;
				case Key.F11:
					StepIntoExecuted(null, null);
					e.Handled = true;
					break;
				default:
					// do nothing
					break;
			}
			
			base.OnKeyUp(e);
		}
		
		#endregion
		
		#region Exit/About
		void ExitClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
		
		void AboutClick(object sender, RoutedEventArgs e)
		{
			AboutPage.Display(decompilerTextView);
		}
		#endregion
		
		#region Decompile / Save
		void TreeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (treeView.SelectedItems.Count == 1) {
				ILSpyTreeNode node = treeView.SelectedItem as ILSpyTreeNode;
				if (node != null && node.View(decompilerTextView))
					return;
			}
			decompilerTextView.Decompile(sessionSettings.FilterSettings.Language,
			                             treeView.GetTopLevelSelection().OfType<ILSpyTreeNode>(),
			                             new DecompilationOptions());
		}
		
		void saveCode_Click(object sender, RoutedEventArgs e)
		{
			if (treeView.SelectedItems.Count == 1) {
				ILSpyTreeNode node = treeView.SelectedItem as ILSpyTreeNode;
				if (node != null && node.Save())
					return;
			}
			decompilerTextView.SaveToDisk(sessionSettings.FilterSettings.Language,
			                              treeView.GetTopLevelSelection().OfType<ILSpyTreeNode>(),
			                              new DecompilationOptions());
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
				SelectNode(history.GoBack(treeView.SelectedItem as SharpTreeNode), false);
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
				SelectNode(history.GoForward(treeView.SelectedItem as SharpTreeNode), false);
			}
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
			sessionSettings.Save();
		}
		
		void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			DebuggerService.CurrentDebugger.Language = 
				sessionSettings.FilterSettings.Language.Name == "IL" ? DecompiledLanguages.IL : DecompiledLanguages.CSharp;
		}
	}
}