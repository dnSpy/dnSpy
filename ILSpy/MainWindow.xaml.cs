// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
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
		SharpTreeNodeCollection assemblies;
		
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
			InitializeComponent();
			
			treeView.Root = new AssemblyListTreeNode();
			assemblies = treeView.Root.Children;
			
			foreach (Assembly asm in initialAssemblies)
				OpenAssembly(asm.Location);
		}
		
		void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			e.Handled = true;
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = ".NET assemblies|*.dll;*.exe|All files|*.*";
			dlg.Multiselect = true;
			dlg.RestoreDirectory = true;
			if (dlg.ShowDialog() == true) {
				treeView.UnselectAll();
				foreach (string file in dlg.FileNames) {
					treeView.SelectedItems.Add(OpenAssembly(file));
				}
			}
		}
		
		AssemblyTreeNode OpenAssembly(string file)
		{
			file = Path.GetFullPath(file);
			
			var node = assemblies.OfType<AssemblyTreeNode>().FirstOrDefault(a => file.Equals(a.FileName, StringComparison.OrdinalIgnoreCase));
			if (node == null) {
				node = new AssemblyTreeNode(file);
				assemblies.Add(node);
			}
			return node;
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
	}
}