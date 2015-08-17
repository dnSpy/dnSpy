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
using System.Diagnostics;
using System.Linq;
using dnSpy.Images;
using dnSpy.NRefactory;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes {
	/// <summary>
	/// Namespace node. The loading of the type nodes is handled by the parent AssemblyTreeNode.
	/// </summary>
	public sealed class NamespaceTreeNode : ILSpyTreeNode
	{
		string name;

		public string Name {
			get { return name; }
			set { name = value; }
		}
		
		public NamespaceTreeNode(string name)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			this.name = name;
		}
		
		protected override void Write(ITextOutput output, Language language)
		{
			Write(output, name);
		}

		public static ITextOutput Write(ITextOutput output, string name)
		{
			if (name.Length == 0)
				output.Write('-', TextTokenType.Operator);
			else {
				var parts = name.Split('.');
				for (int i = 0; i < parts.Length; i++) {
					if (i > 0)
						output.Write('.', TextTokenType.Operator);
					output.Write(IdentifierEscaper.Escape(parts[i]), TextTokenType.NamespacePart);
				}
			}
			return output;
		}
		
		public override object Icon {
			get { return ImageCache.Instance.GetImage("Namespace", BackgroundType.TreeNode); }
		}
		
		public override FilterResult Filter(FilterSettings settings)
		{
			var res = settings.Filter.GetFilterResult(this.Name, ((AssemblyTreeNode)Parent).LoadedAssembly);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			if (settings.SearchTermMatches(name))
				return FilterResult.MatchAndRecurse;
			else
				return FilterResult.Recurse;
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.DecompileNamespace(name, this.Children.OfType<TypeTreeNode>().Select(t => t.TypeDefinition), output, options);
		}

		internal void OnBeforeRemoved()
		{
			((AssemblyTreeNode)Parent).OnRemoved(this);
		}

		internal void OnReadded()
		{
			((AssemblyTreeNode)Parent).OnReadded(this);
		}

		internal void Append(TypeTreeNode typeNode)
		{
			bool b = name.Equals(typeNode.Namespace, StringComparison.Ordinal);
			Debug.Assert(b);
			if (!b)
				throw new InvalidOperationException();
			Children.Add(typeNode);
			((AssemblyTreeNode)Parent).OnReadded(typeNode);
		}

		internal TypeTreeNode RemoveLast()
		{
			Debug.Assert(Children.Count > 0);
			int index = Children.Count - 1;
			var typeNode = (TypeTreeNode)Children[index];
			Children.RemoveAt(index);
			((AssemblyTreeNode)Parent).OnRemoved(typeNode);
			return typeNode;
		}

		protected override int GetNewChildIndex(SharpTreeNode node)
		{
			if (node is TypeTreeNode)
				return GetNewChildIndex(node, (a, b) => AssemblyTreeNode.TypeStringComparer.Compare(((TypeTreeNode)a).TypeDefinition.FullName, ((TypeTreeNode)b).TypeDefinition.FullName));
			return base.GetNewChildIndex(node);
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("ns", name); }
		}
	}
}
