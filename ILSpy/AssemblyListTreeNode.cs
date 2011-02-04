// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
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
	sealed class AssemblyListTreeNode : SharpTreeNode, IAssemblyResolver
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
			else
				return DropEffect.None;
		}
		
		public override void Drop(IDataObject data, int index, DropEffect finalEffect)
		{
			string[] files = data.GetData(AssemblyTreeNode.DataFormat) as string[];
			if (files != null) {
				var nodes = (from file in files
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
		
		AssemblyTreeNode OpenGacAssembly(string fullName)
		{
			App.Current.Dispatcher.VerifyAccess();
			
			string file = GacInterop.FindAssemblyInNetGac(AssemblyNameReference.Parse(fullName));
			if (file != null) {
				return OpenAssembly(file);
			} else {
				return null;
			}
		}
		
		AssemblyDefinition IAssemblyResolver.Resolve(AssemblyNameReference name)
		{
			var node = OpenGacAssembly(name.FullName);
			return node != null ? node.AssemblyDefinition : null;
		}
		
		AssemblyDefinition IAssemblyResolver.Resolve(AssemblyNameReference name, ReaderParameters parameters)
		{
			var node = OpenGacAssembly(name.FullName);
			return node != null ? node.AssemblyDefinition : null;
		}
		
		AssemblyDefinition IAssemblyResolver.Resolve(string fullName)
		{
			var node = OpenGacAssembly(fullName);
			return node != null ? node.AssemblyDefinition : null;
		}
		
		AssemblyDefinition IAssemblyResolver.Resolve(string fullName, ReaderParameters parameters)
		{
			var node = OpenGacAssembly(fullName);
			return node != null ? node.AssemblyDefinition : null;
		}
	}
}
