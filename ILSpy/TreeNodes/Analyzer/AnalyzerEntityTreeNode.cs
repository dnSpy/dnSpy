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
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	/// <summary>
	/// Base class for entity nodes.
	/// </summary>
	public abstract class AnalyzerEntityTreeNode : AnalyzerTreeNode, IMemberTreeNode
	{
		public abstract MemberReference Member { get; }
		
		public override void ActivateItem(System.Windows.RoutedEventArgs e)
		{
			e.Handled = true;
			MainWindow.Instance.JumpToReference(this.Member);
		}
		
		public override bool HandleAssemblyListChanged(ICollection<LoadedAssembly> removedAssemblies, ICollection<LoadedAssembly> addedAssemblies)
		{
			foreach (LoadedAssembly asm in removedAssemblies) {
				if (this.Member.Module == asm.ModuleDefinition)
					return false; // remove this node
			}
			this.Children.RemoveAll(
				delegate(SharpTreeNode n) {
					AnalyzerTreeNode an = n as AnalyzerTreeNode;
					return an == null || !an.HandleAssemblyListChanged(removedAssemblies, addedAssemblies);
				});
			return true;
		}
	}
}
