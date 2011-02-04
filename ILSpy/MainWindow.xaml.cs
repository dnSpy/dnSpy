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
		AssemblyListTreeNode assemblyList = new AssemblyListTreeNode();
		
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
			
			textEditor.Text = "// Welcome to ILSpy!";
			treeView.Root = assemblyList;
			
			foreach (Assembly asm in initialAssemblies)
				assemblyList.OpenAssembly(asm.Location);
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
					var asm = assemblyList.OpenAssembly(file);
					if (asm != null)
						treeView.SelectedItems.Add(asm);
				}
			}
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
				treeView.UnselectAll();
				foreach (string fullName in dlg.SelectedFullNames) {
					var asm = assemblyList.OpenGacAssembly(fullName);
					if (asm != null)
						treeView.SelectedItems.Add(asm);
				}
			}
		}
	}
}