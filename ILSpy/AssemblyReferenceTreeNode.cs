// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Node within assembly reference list.
	/// </summary>
	sealed class AssemblyReferenceTreeNode : SharpTreeNode
	{
		readonly AssemblyNameReference r;
		readonly AssemblyTreeNode parentAssembly;
		
		public AssemblyReferenceTreeNode(AssemblyNameReference r, AssemblyTreeNode parentAssembly)
		{
			if (parentAssembly == null)
				throw new ArgumentNullException("parentAssembly");
			if (r == null)
				throw new ArgumentNullException("r");
			this.r = r;
			this.parentAssembly = parentAssembly;
			this.LazyLoading = true;
		}
		
		public override object Text {
			get { return r.Name; }
		}
		
		public override object Icon {
			get { return Images.Assembly; }
		}
		
		public override void ActivateItem(System.Windows.RoutedEventArgs e)
		{
			var assemblyListNode = parentAssembly.Parent as AssemblyListTreeNode;
			if (assemblyListNode != null) {
				assemblyListNode.Select(parentAssembly.LookupReferencedAssembly(r.FullName));
				e.Handled = true;
			}
		}
		
		protected override void LoadChildren()
		{
			var refNode = parentAssembly.LookupReferencedAssembly(r.FullName);
			if (refNode != null) {
				AssemblyDefinition asm = refNode.AssemblyDefinition;
				if (asm != null) {
					foreach (var childRef in asm.MainModule.AssemblyReferences)
						this.Children.Add(new AssemblyReferenceTreeNode(childRef, refNode));
				}
			}
		}
	}
}
