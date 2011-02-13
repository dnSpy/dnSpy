// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.ObjectModel;
using System.Linq;

using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Lists the super types of a class.
	/// </summary>
	sealed class DerivedTypesTreeNode : ILSpyTreeNode<DerivedTypesEntryNode>
	{
		readonly AssemblyList list;
		readonly TypeDefinition type;
		
		public DerivedTypesTreeNode(AssemblyList list, TypeDefinition type)
		{
			this.list = list;
			this.type = type;
			this.LazyLoading = true;
		}
		
		public override object Text {
			get { return "Derived Types"; }
		}
		
		public override object Icon {
			get { return Images.SubTypes; }
		}
		
		protected override void LoadChildren()
		{
			AddDerivedTypes(this.Children, type, list);
		}
		
		internal static void AddDerivedTypes(ObservableCollection<DerivedTypesEntryNode> children, TypeDefinition type, AssemblyList list)
		{
			foreach (var asmNode in list.Assemblies) {
				AssemblyDefinition asm = asmNode.AssemblyDefinition;
				if (asm == null)
					continue;
				foreach (TypeDefinition td in TreeTraversal.PreOrder(asm.MainModule.Types, t => t.NestedTypes)) {
					if (type.IsInterface && td.HasInterfaces) {
						foreach (TypeReference typeRef in td.Interfaces) {
							if (IsSameType(typeRef, type))
								children.Add(new DerivedTypesEntryNode(td, list));
						}
					} else if (!type.IsInterface && td.BaseType != null && IsSameType(td.BaseType, type)) {
						children.Add(new DerivedTypesEntryNode(td, list));
					}
				}
			}
		}
		
		static bool IsSameType(TypeReference typeRef, TypeDefinition type)
		{
			return typeRef.FullName == type.FullName;
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			EnsureLazyChildren();
			foreach (var child in this.Children) {
				child.Decompile(language, output, options);
			}
		}
	}
	
	class DerivedTypesEntryNode : ILSpyTreeNode<DerivedTypesEntryNode>
	{
		TypeDefinition def;
		AssemblyList list;
		
		public DerivedTypesEntryNode(TypeDefinition def, AssemblyList list)
		{
			this.def = def;
			this.list = list;
			this.LazyLoading = true;
		}
		
		public override bool ShowExpander {
			get {
				return !def.IsSealed && base.ShowExpander;
			}
		}
		
		public override object Text {
			get { return def.FullName; }
		}
		
		public override object Icon {
			get {
				return TypeTreeNode.GetIcon(def);
			}
		}
		
		protected override void LoadChildren()
		{
			DerivedTypesTreeNode.AddDerivedTypes(this.Children, def, list);
		}
		
		public override void ActivateItem(System.Windows.RoutedEventArgs e)
		{
			e.Handled = BaseTypesEntryNode.ActivateItem(this, def);
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			language.WriteCommentLine(output, language.TypeToString(def, true));
		}
	}
}
