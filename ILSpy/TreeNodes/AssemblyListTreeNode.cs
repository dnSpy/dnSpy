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
using System.Linq;
using System.Windows;

using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Represents a list of assemblies.
	/// This is used as (invisible) root node of the tree view.
	/// </summary>
	sealed class AssemblyListTreeNode : ILSpyTreeNode<AssemblyTreeNode>
	{
		readonly AssemblyList assemblyList;
		
		public AssemblyList AssemblyList {
			get { return assemblyList; }
		}
		
		public AssemblyListTreeNode(AssemblyList assemblyList)
			: base(assemblyList.assemblies)
		{
			if (assemblyList == null)
				throw new ArgumentNullException("assemblyList");
			this.assemblyList = assemblyList;
		}
		
		public override object Text {
			get { return assemblyList.ListName; }
		}
		
		/*
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
				lock (assemblyList.assemblies) {
					var nodes = (from file in files
					             where file != null
					             select assemblyList.OpenAssembly(file) into node
					             where node != null
					             select node).Distinct().ToList();
					foreach (AssemblyTreeNode node in nodes) {
						int nodeIndex = assemblyList.assemblies.IndexOf(node);
						if (nodeIndex < index)
							index--;
						assemblyList.assemblies.RemoveAt(nodeIndex);
					}
					nodes.Reverse();
					foreach (AssemblyTreeNode node in nodes) {
						assemblyList.assemblies.Insert(index, node);
					}
				}
			}
		}*/
		
		public Action<SharpTreeNode> Select = delegate {};
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, "List: " + assemblyList.ListName);
			output.WriteLine();
			foreach (AssemblyTreeNode asm in assemblyList.GetAssemblies()) {
				language.WriteCommentLine(output, new string('-', 60));
				output.WriteLine();
				asm.Decompile(language, output, options);
			}
		}
	}
}
