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
using ICSharpCode.Decompiler;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// References folder.
	/// </summary>
	sealed class ReferenceFolderTreeNode : ILSpyTreeNode
	{
		readonly ModuleDefinition module;
		readonly AssemblyTreeNode parentAssembly;
		
		public ReferenceFolderTreeNode(ModuleDefinition module, AssemblyTreeNode parentAssembly)
		{
			this.module = module;
			this.parentAssembly = parentAssembly;
			this.LazyLoading = true;
		}
		
		public override object Text {
			get { return "References"; }
		}
		
		public override object Icon {
			get { return Images.ReferenceFolderClosed; }
		}
		
		public override object ExpandedIcon {
			get { return Images.ReferenceFolderOpen; }
		}
		
		protected override void LoadChildren()
		{
			foreach (var r in module.AssemblyReferences.OrderBy(r => r.Name))
				this.Children.Add(new AssemblyReferenceTreeNode(r, parentAssembly));
			foreach (var r in module.ModuleReferences.OrderBy(r => r.Name))
				this.Children.Add(new ModuleReferenceTreeNode(r));
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(EnsureLazyChildren));
			// Show metadata order of references
			foreach (var r in module.AssemblyReferences)
				new AssemblyReferenceTreeNode(r, parentAssembly).Decompile(language, output, options);
			foreach (var r in module.ModuleReferences)
				language.WriteCommentLine(output, r.Name);
		}
	}
}
