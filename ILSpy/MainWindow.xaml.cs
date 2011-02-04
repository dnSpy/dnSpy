// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Windows;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// The main window of the application.
	/// </summary>
	public partial class MainWindow : Window
	{
		SharpTreeNodeCollection assemblies;
		
		public MainWindow()
		{
			InitializeComponent();
			
			treeView.Root = new SharpTreeNode();
			assemblies = treeView.Root.Children;
			
			assemblies.Add(new AssemblyTreeNode(AssemblyDefinition.ReadAssembly(typeof(object).Assembly.Location)));
		}
		
		void Window_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			Process.Start(e.Uri.ToString());
		}
	}
}