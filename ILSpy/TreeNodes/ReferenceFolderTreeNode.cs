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
using dnSpy.Images;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// References folder.
	/// </summary>
	sealed class ReferenceFolderTreeNode : ILSpyTreeNode
	{
		readonly ModuleDefMD module;
		readonly AssemblyTreeNode parentAssembly;
		readonly AssemblyListTreeNode assemblyListTreeNode;
		
		public ReferenceFolderTreeNode(ModuleDefMD module, AssemblyTreeNode parentAssembly, AssemblyListTreeNode assemblyListTreeNode)
		{
			this.module = module;
			this.parentAssembly = parentAssembly;
			this.assemblyListTreeNode = assemblyListTreeNode;
			this.LazyLoading = true;
		}
		
		protected override void Write(ITextOutput output, Language language)
		{
			output.Write("References", TextTokenType.Text);
		}
		
		public override object Icon {
			get { return ImageCache.Instance.GetImage("ReferenceFolderClosed", BackgroundType.TreeNode); }
		}
		
		public override object ExpandedIcon {
			get { return ImageCache.Instance.GetImage("ReferenceFolderOpen", BackgroundType.TreeNode); }
		}

		public override FilterResult Filter(FilterSettings settings)
		{
			var res = settings.Filter.GetFilterResult(this);
			if (res.FilterResult != null)
				return res.FilterResult.Value;
			return base.Filter(settings);
		}
		
		protected override void LoadChildren()
		{
			foreach (var r in module.GetAssemblyRefs().OrderBy(r => r.Name.String))
				this.Children.Add(new AssemblyReferenceTreeNode(r, parentAssembly, assemblyListTreeNode));
			foreach (var r in module.GetModuleRefs().OrderBy(r => r.Name.String))
				this.Children.Add(new ModuleReferenceTreeNode(r));
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(EnsureChildrenFiltered));
			// Show metadata order of references
			foreach (var r in module.GetAssemblyRefs())
				new AssemblyReferenceTreeNode(r, parentAssembly, assemblyListTreeNode).Decompile(language, output, options);
			foreach (var r in module.GetModuleRefs())
				language.WriteCommentLine(output, IdentifierEscaper.Escape(r.Name));
		}

		public override NodePathName NodePathName {
			get { return new NodePathName("refs"); }
		}
	}
}
