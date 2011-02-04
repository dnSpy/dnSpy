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
	sealed class AssemblyListTreeNode : SharpTreeNode
	{
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
			foreach (SharpTreeNode node in nodes)
				this.Children.Remove(node);
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
				             select OpenAssembly(file) into node
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
		
		public AssemblyTreeNode OpenAssembly(string file)
		{
			App.Current.Dispatcher.VerifyAccess();
			
			file = Path.GetFullPath(file);
			
			foreach (AssemblyTreeNode node in this.Children) {
				if (file.Equals(node.FileName, StringComparison.OrdinalIgnoreCase))
					return node;
			}
			
			var newNode = new AssemblyTreeNode(file, this);
			this.Children.Add(newNode);
			return newNode;
		}
		
		public Action<SharpTreeNode> Select = delegate {};
		
		ConcurrentDictionary<TypeDefinition, TypeTreeNode> typeDict = new ConcurrentDictionary<TypeDefinition, TypeTreeNode>();
		
		public void RegisterTypeNode(TypeTreeNode node)
		{
			// called on background loading thread, so we need to use a ConcurrentDictionary
			typeDict[node.TypeDefinition] = node;
		}
		
		public TypeTreeNode FindTypeNode(TypeDefinition def)
		{
			if (def.DeclaringType != null) {
				TypeTreeNode decl = FindTypeNode(def.DeclaringType);
				if (decl != null) {
					decl.EnsureLazyChildren();
					return decl.Children.OfType<TypeTreeNode>().FirstOrDefault(t => t.TypeDefinition == def);
				}
			} else {
				TypeTreeNode node;
				if (typeDict.TryGetValue(def, out node)) {
					// Ensure that the node is connected to the tree
					node.ParentAssemblyNode.EnsureLazyChildren();
					// Validate that the node wasn't removed due to visibility settings:
					if (node.Ancestors().Contains(this))
						return node;
				}
			}
			return null;
		}
	}
}
