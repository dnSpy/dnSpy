// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

using ICSharpCode.TreeView;

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
				assemblies.Add(new AssemblyTreeNode(asm.Location));
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