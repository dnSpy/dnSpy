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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using ICSharpCode.TreeView;
using Microsoft.Win32;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// The main window of the application.
	/// </summary>
	public partial class MainWindow : Window
	{
		AssemblyList assemblyList = new AssemblyList();
		FilterSettings filterSettings = new FilterSettings();
		
		static readonly Assembly[] initialAssemblies = {
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
			typeof(MainWindow).Assembly
		};
		
		public MainWindow()
		{
			this.DataContext = filterSettings;
			InitializeComponent();
			
			textEditor.Text = "Welcome to ILSpy!";
			AssemblyListTreeNode assemblyListTreeNode = new AssemblyListTreeNode(assemblyList);
			assemblyListTreeNode.FilterSettings = filterSettings.Clone();
			filterSettings.PropertyChanged += delegate {
				// filterSettings is mutable; but the ILSpyTreeNode filtering assumes that filter settings are immutable.
				// Thus, the main window will use one mutable instance (for data-binding), and assign a new clone to the ILSpyTreeNodes whenever the main
				// mutable instance changes.
				assemblyListTreeNode.FilterSettings = filterSettings.Clone();
			};
			treeView.Root = assemblyListTreeNode;
			assemblyListTreeNode.Select = delegate(SharpTreeNode obj) {
				if (obj != null) {
					foreach (SharpTreeNode node in obj.Ancestors())
						node.IsExpanded = true;
					
					treeView.SelectedItem = obj;
					treeView.ScrollIntoView(obj);
				}
			};
			
			foreach (Assembly asm in initialAssemblies)
				assemblyList.OpenAssembly(asm.Location);
			string[] args = Environment.GetCommandLineArgs();
			for (int i = 1; i < args.Length; i++) {
				assemblyList.OpenAssembly(args[i]);
			}
		}
		
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
			try {
				textEditor.SyntaxHighlighting = ILSpy.Language.Current.SyntaxHighlighting;
				SmartTextOutput textOutput = new SmartTextOutput();
				foreach (var node in treeView.SelectedItems.OfType<ILSpyTreeNode>()) {
					node.Decompile(ILSpy.Language.Current, textOutput);
				}
				textEditor.Text = textOutput.ToString();
			} catch (Exception ex) {
				textEditor.SyntaxHighlighting = null;
				textEditor.Text = ex.ToString();
			}
		}
	}
}