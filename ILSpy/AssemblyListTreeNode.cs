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
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Represents a list of assemblies.
	/// </summary>
	sealed class AssemblyListTreeNode : ILSpyTreeNode<AssemblyTreeNode>
	{
		readonly AssemblyList assemblyList;
		
		public AssemblyList AssemblyList {
			get { return assemblyList; }
		}
		
		public AssemblyListTreeNode(AssemblyList assemblyList)
			: base(assemblyList.Assemblies)
		{
			if (assemblyList == null)
				throw new ArgumentNullException("assemblyList");
			this.assemblyList = assemblyList;
		}
		
		public override bool CanDelete(SharpTreeNode[] nodes)
		{
			return nodes.All(n => n is AssemblyTreeNode);
		}
		
		public override void Delete(SharpTreeNode[] nodes)
		{
			DeleteCore(nodes);
		}
		
		public override void DeleteCore(SharpTreeNode[] nodes)
		{
			foreach (AssemblyTreeNode node in nodes)
				assemblyList.Assemblies.Remove(node);
		}
		
		public override DropEffect CanDrop(IDataObject data, DropEffect requestedEffect)
		{
			if (data.GetDataPresent(AssemblyTreeNode.DataFormat))
				return DropEffect.Move;
			else if (data.GetDataPresent(DataFormats.FileDrop))
				return DropEffect.Move;
			else
				return DropEffect.None;
		}
		
		public override void Drop(IDataObject data, int index, DropEffect finalEffect)
		{
			string[] files = data.GetData(AssemblyTreeNode.DataFormat) as string[];
			if (files == null)
				files = data.GetData(DataFormats.FileDrop) as string[];
			if (files != null) {
				var nodes = (from file in files
				             where file != null
				             select assemblyList.OpenAssembly(file) into node
				             where node != null
				             select node).Distinct().ToList();
				foreach (AssemblyTreeNode node in nodes) {
					int nodeIndex = this.Children.IndexOf(node);
					if (nodeIndex < index)
						index--;
					this.Children.RemoveAt(nodeIndex);
				}
				nodes.Reverse();
				foreach (AssemblyTreeNode node in nodes) {
					this.Children.Insert(index, node);
				}
			}
		}
		
		public Action<SharpTreeNode> Select = delegate {};
	}
}
