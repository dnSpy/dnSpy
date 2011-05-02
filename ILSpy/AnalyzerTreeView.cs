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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using ICSharpCode.ILSpy.TreeNodes.Analyzer;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Analyzer tree view.
	/// </summary>
	public partial class AnalyzerTreeView : SharpTreeView, IPane
	{
		static AnalyzerTreeView instance;
		
		public static AnalyzerTreeView Instance {
			get {
				if (instance == null) {
					App.Current.VerifyAccess();
					instance = new AnalyzerTreeView();
				}
				return instance;
			}
		}
		
		private AnalyzerTreeView()
		{
			this.ShowRoot = false;
			this.Root = new AnalyzerTreeNode { Language = MainWindow.Instance.CurrentLanguage };
			ContextMenuProvider.Add(this);
		}
		
		public void Show()
		{
			if (!IsVisible)
				MainWindow.Instance.ShowInBottomPane("Analyzer", this);
		}
		
		public void Show(AnalyzerTreeNode node)
		{
			Show();
			
			node.IsExpanded = true;
			this.Root.Children.Add(node);
			this.SelectedItem = node;
			this.FocusNode(node);
		}
		
		void IPane.Closed()
		{
			this.Root.Children.Clear();
		}
	}
	
	[ExportMainMenuCommand(Menu = "_View", Header = "_Analyzer", MenuCategory = "ShowPane", MenuOrder = 100)]
	sealed class ShowAnalyzerCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			AnalyzerTreeView.Instance.Show();
		}
	}
}