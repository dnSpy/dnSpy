// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// References folder.
	/// </summary>
	sealed class ReferenceFolderTreeNode : SharpTreeNode
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
			foreach (var r in module.AssemblyReferences)
				this.Children.Add(new AssemblyReferenceTreeNode(r, parentAssembly));
			foreach (var r in module.ModuleReferences)
				this.Children.Add(new ModuleReferenceTreeNode(r));
		}
	}
}
