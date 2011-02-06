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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.FlowAnalysis;
using ICSharpCode.TreeView;
using Microsoft.Win32;
using Mono.Cecil.Rocks;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// The main window of the application.
	/// </summary>
	public partial class MainWindow : Window
	{
		SessionSettings sessionSettings;
		AssemblyListManager assemblyListManager;
		AssemblyList assemblyList;
		AssemblyListTreeNode assemblyListTreeNode;
		
		public MainWindow()
		{
			ILSpySettings spySettings = ILSpySettings.Load();
			this.sessionSettings = new SessionSettings(spySettings);
			this.assemblyListManager = new AssemblyListManager(spySettings);
			this.assemblyList = assemblyListManager.LoadList(spySettings, sessionSettings.ActiveAssemblyList);
			
			this.DataContext = sessionSettings;
			this.Left = sessionSettings.WindowBounds.Left;
			this.Top = sessionSettings.WindowBounds.Top;
			this.Width = sessionSettings.WindowBounds.Width;
			this.Height = sessionSettings.WindowBounds.Height;
			// TODO: validate bounds (maybe a screen was removed...)
			this.WindowState = sessionSettings.WindowState;
			
			InitializeComponent();
			decompilerTextView.mainWindow = this;
			
			assemblyListTreeNode = new AssemblyListTreeNode(assemblyList);
			assemblyListTreeNode.FilterSettings = sessionSettings.FilterSettings.Clone();
			sessionSettings.FilterSettings.PropertyChanged += new PropertyChangedEventHandler(filterSettings_PropertyChanged);
			treeView.Root = assemblyListTreeNode;
			assemblyListTreeNode.Select = SelectNode;
			
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 1; i < args.Length; i++) {
				assemblyList.OpenAssembly(args[i]);
			}
			if (assemblyList.Assemblies.Count == 0)
				LoadInitialAssemblies();
			
			SelectNode(FindNodeByPath(sessionSettings.ActiveTreeViewPath));
			
			#if DEBUG
			AddDebugItemsToToolbar();
			#endif
		}
		
		void LoadInitialAssemblies()
		{
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
			assemblyListTreeNode.FilterSettings = sessionSettings.FilterSettings.Clone();
			if (e.PropertyName == "Language") {
				TreeView_SelectionChanged(null, null);
			}
		}
		
		internal AssemblyList AssemblyList {
			get { return assemblyList; }
		}
		
		internal void SelectNode(SharpTreeNode obj)
		{
			if (obj != null) {
				treeView.FocusNode(obj);
				treeView.SelectedItem = obj;
			}
		}
		
		SharpTreeNode FindNodeByPath(string[] path)
		{
			if (path == null)
				return null;
			SharpTreeNode node = treeView.Root;
			foreach (var element in path) {
				if (node == null)
					break;
				node.EnsureLazyChildren();
				node = node.Children.FirstOrDefault(c => c.ToString() == element);
			}
			return node;
		}
		
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
				treeView.ScrollIntoView(lastNode);
		}
		
		void ExitClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
		
		void AboutClick(object sender, RoutedEventArgs e)
		{
			AboutDialog dlg = new AboutDialog();
			dlg.Owner = this;
			dlg.ShowDialog();
		}
		
		void OpenFromGac_Click(object sender, RoutedEventArgs e)
		{
			OpenFromGacDialog dlg = new OpenFromGacDialog();
			dlg.Owner = this;
			if (dlg.ShowDialog() == true) {
				OpenFiles(dlg.SelectedFileNames);
			}
		}
		
		void TreeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			decompilerTextView.Decompile(sessionSettings.FilterSettings.Language, treeView.SelectedItems.OfType<ILSpyTreeNodeBase>());
		}
		
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
			sessionSettings.Save();
		}
	}
}