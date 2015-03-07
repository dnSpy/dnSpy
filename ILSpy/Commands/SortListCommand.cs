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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy
{
	[ExportMainMenuCommand(Menu = "_View", Header = "_Sort Assemblies", MenuIcon = "Images/Sort.png", MenuCategory = "List", MenuOrder = 3)]
	sealed class SortListCommand : SimpleCommand
	{
		public override void Execute(object parameter)
		{
			//cache a (copied) list of the currently selected nodes
			var selectedCache = MainWindow.Instance.SelectedNodes.Where(n => n is AssemblyTreeNode).Select(n => ((AssemblyTreeNode)n).LoadedAssembly).ToList();
			MainWindow.Instance.CurrentAssemblyList.Sort(new LoadedAssembly.NameComparer());
			MainWindow.Instance.treeView.SetSelectedNodes(GetNewSelectedNodes(selectedCache));
		}

		private IEnumerable<ILSpyTreeNode> GetNewSelectedNodes(IEnumerable<LoadedAssembly> cache)
		{
			foreach (var assy in cache) {
				yield return MainWindow.Instance.AssemblyListTreeNode.FindAssemblyNode(assy);
			}
		}
	}
}
