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
			
			var newNode = new AssemblyTreeNode(file, null, this);
			this.Children.Add(newNode);
			return newNode;
		}
		
		public AssemblyTreeNode OpenGacAssembly(string fullName)
		{
			App.Current.Dispatcher.VerifyAccess();
			
			foreach (AssemblyTreeNode node in this.Children) {
				if (fullName.Equals(node.FullName, StringComparison.OrdinalIgnoreCase))
					return node;
			}
			
			string file = FindAssemblyInNetGac(AssemblyNameReference.Parse(fullName));
			if (file != null) {
				var newNode = new AssemblyTreeNode(file, fullName, this);
				this.Children.Add(newNode);
				return newNode;
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
		
		#region FindAssemblyInGac
		// This region is taken from Mono.Cecil:
		
		// Author:
		//   Jb Evain (jbevain@gmail.com)
		//
		// Copyright (c) 2008 - 2010 Jb Evain
		//
		// Permission is hereby granted, free of charge, to any person obtaining
		// a copy of this software and associated documentation files (the
		// "Software"), to deal in the Software without restriction, including
		// without limitation the rights to use, copy, modify, merge, publish,
		// distribute, sublicense, and/or sell copies of the Software, and to
		// permit persons to whom the Software is furnished to do so, subject to
		// the following conditions:
		//
		// The above copyright notice and this permission notice shall be
		// included in all copies or substantial portions of the Software.
		//
		// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
		// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
		// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
		// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
		// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
		// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
		// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
		//

		static string FindAssemblyInNetGac (AssemblyNameReference reference)
		{
			string[] gac_paths = { Fusion.GetGacPath(false), Fusion.GetGacPath(true) };
			string[] gacs = { "GAC_MSIL", "GAC_32", "GAC" };
			string[] prefixes = { string.Empty, "v4.0_" };

			for (int i = 0; i < 2; i++) {
				for (int j = 0; j < gacs.Length; j++) {
					var gac = Path.Combine (gac_paths [i], gacs [j]);
					var file = GetAssemblyFile (reference, prefixes [i], gac);
					if (File.Exists (file))
						return file;
				}
			}

			return null;
		}
		
		static string GetAssemblyFile (AssemblyNameReference reference, string prefix, string gac)
		{
			var gac_folder = new StringBuilder ()
				.Append (prefix)
				.Append (reference.Version)
				.Append ("__");

			for (int i = 0; i < reference.PublicKeyToken.Length; i++)
				gac_folder.Append (reference.PublicKeyToken [i].ToString ("x2"));

			return Path.Combine (
				Path.Combine (
					Path.Combine (gac, reference.Name), gac_folder.ToString ()),
				reference.Name + ".dll");
		}
		#endregion
	}
}
