// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Lists the base types of a class.
	/// </summary>
	sealed class BaseTypesTreeNode : SharpTreeNode
	{
		readonly TypeDefinition type;
		
		public BaseTypesTreeNode(TypeDefinition type)
		{
			this.type = type;
			this.LazyLoading = true;
		}
		
		public override object Text {
			get { return "Base Types"; }
		}
		
		public override object Icon {
			get { return Images.Undo; }
		}
		
		protected override void LoadChildren()
		{
			AddBaseTypes(this.Children, type);
		}
		
		static void AddBaseTypes(SharpTreeNodeCollection children, TypeDefinition type)
		{
			if (type.BaseType != null)
				children.Add(new EntryNode(type.BaseType, false));
			foreach (TypeReference i in type.Interfaces) {
				children.Add(new EntryNode(i, true));
			}
		}
		
		sealed class EntryNode : SharpTreeNode
		{
			TypeReference tr;
			TypeDefinition def;
			bool isInterface;
			
			public EntryNode(TypeReference tr, bool isInterface)
			{
				if (tr == null)
					throw new ArgumentNullException("tr");
				this.tr = tr;
				this.def = tr.Resolve();
				this.isInterface = isInterface;
				this.LazyLoading = true;
			}
			
			public override bool ShowExpander {
				get {
					if (isInterface || tr.FullName == "System.Object")
						EnsureLazyChildren(); // need to create children to test whether we have any
					return base.ShowExpander;
				}
			}
			
			public override object Text {
				get { return tr.FullName; }
			}
			
			public override object Icon {
				get {
					if (def != null)
						return TypeTreeNode.GetIcon(def);
					else
						return isInterface ? Images.Interface : Images.Class;
				}
			}
			
			protected override void LoadChildren()
			{
				if (def != null)
					AddBaseTypes(this.Children, def);
			}
		}
	}
}
