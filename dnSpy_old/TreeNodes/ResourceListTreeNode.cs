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
using System.Windows.Threading;
using dnlib.DotNet;
using dnSpy.Contracts;
using dnSpy.Contracts.Images;
using dnSpy.NRefactory;
using dnSpy.TreeNodes;
using ICSharpCode.Decompiler;
using ICSharpCode.TreeView;

namespace ICSharpCode.ILSpy.TreeNodes {
	/// <summary>
	/// Lists the embedded resources in an assembly.
	/// </summary>
	public sealed class ResourceListTreeNode : ILSpyTreeNode {
		readonly ModuleDef module;

		public ResourceListTreeNode(ModuleDef module) {
			this.LazyLoading = true;
			this.module = module;
		}

		protected override void Write(ITextOutput output, Language language) {
			output.Write("Resources", TextTokenType.Text);
		}

		public override object Icon {
			get { return Globals.App.ImageManager.GetImage(GetType().Assembly, "FolderClosed", BackgroundType.TreeNode); }
		}

		public override object ExpandedIcon {
			get { return Globals.App.ImageManager.GetImage(GetType().Assembly, "FolderOpen", BackgroundType.TreeNode); }
		}

		protected override void LoadChildren() {
			var ary = module.Resources.ToArray();
			Array.Sort(ary, ResourceComparer.Instance);
			foreach (var r in ary)
				this.Children.Add(ResourceFactory.Create(module, r));
		}

		protected override int GetNewChildIndex(SharpTreeNode node) {
			if (node is ResourceTreeNode)
				return GetNewChildIndex(node, (a, b) => ResourceComparer.Instance.Compare(((ResourceTreeNode)a).Resource, ((ResourceTreeNode)b).Resource));
			return base.GetNewChildIndex(node);
		}

		protected override bool SortOnNodeType {
			get { return false; }
		}

		public override FilterResult Filter(FilterSettings settings) {
			var res = settings.Filter.GetFilterResult(this);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			if (string.IsNullOrEmpty(settings.SearchTerm))
				return FilterResult.MatchAndRecurse;
			else
				return FilterResult.Recurse;
		}

		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options) {
			App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(EnsureChildrenFiltered));
			foreach (ResourceTreeNode child in this.Children) {
				child.Decompile(language, output);
				output.WriteLine();
			}
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("res"); }
		}
	}
}
