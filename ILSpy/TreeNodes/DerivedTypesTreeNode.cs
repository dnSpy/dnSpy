// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.Utils;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes
{
	/// <summary>
	/// Lists the super types of a class.
	/// </summary>
	sealed class DerivedTypesTreeNode : ILSpyTreeNode
	{
		readonly AssemblyList list;
		readonly TypeDefinition type;
		ThreadingSupport threading;
		
		public DerivedTypesTreeNode(AssemblyList list, TypeDefinition type)
		{
			this.list = list;
			this.type = type;
			this.LazyLoading = true;
			this.threading = new ThreadingSupport();
		}
		
		public override object Text {
			get { return "Derived Types"; }
		}
		
		public override object Icon {
			get { return Images.SubTypes; }
		}
		
		protected override void LoadChildren()
		{
			threading.LoadChildren(this, FetchChildren);
		}
		
		IEnumerable<ILSpyTreeNode> FetchChildren(CancellationToken cancellationToken)
		{
			// FetchChildren() runs on the main thread; but the enumerator will be consumed on a background thread
			var assemblies = list.GetAssemblies().Select(node => node.AssemblyDefinition).Where(asm => asm != null).ToArray();
			return FindDerivedTypes(type, assemblies, cancellationToken);
		}
		
		internal static IEnumerable<DerivedTypesEntryNode> FindDerivedTypes(TypeDefinition type, AssemblyDefinition[] assemblies, CancellationToken cancellationToken)
		{
			foreach (AssemblyDefinition asm in assemblies) {
				foreach (TypeDefinition td in TreeTraversal.PreOrder(asm.MainModule.Types, t => t.NestedTypes)) {
					cancellationToken.ThrowIfCancellationRequested();
					if (type.IsInterface && td.HasInterfaces) {
						foreach (TypeReference typeRef in td.Interfaces) {
							if (IsSameType(typeRef, type))
								yield return new DerivedTypesEntryNode(td, assemblies);
						}
					} else if (!type.IsInterface && td.BaseType != null && IsSameType(td.BaseType, type)) {
						yield return new DerivedTypesEntryNode(td, assemblies);
					}
				}
			}
		}
		
		static bool IsSameType(TypeReference typeRef, TypeDefinition type)
		{
			if (typeRef.FullName == type.FullName)
				return true;
			if (typeRef.Name != type.Name || type.Namespace != typeRef.Namespace)
				return false;
			if (typeRef.IsNested || type.IsNested)
				if (!typeRef.IsNested || !type.IsNested || !IsSameType(typeRef.DeclaringType, type.DeclaringType))
					return false;
			var gTypeRef = typeRef as GenericInstanceType;
			if (gTypeRef != null || type.HasGenericParameters)
				if (gTypeRef == null || !type.HasGenericParameters || gTypeRef.GenericArguments.Count != type.GenericParameters.Count)
					return false;
			return true;
		}
		
		public override void Decompile(Language language, ITextOutput output, DecompilationOptions options)
		{
			threading.Decompile(language, output, options, EnsureLazyChildren);
		}
	}
	
	class DerivedTypesEntryNode : ILSpyTreeNode
	{
		TypeDefinition def;
		AssemblyDefinition[] assemblies;
		ThreadingSupport threading;
		
		public DerivedTypesEntryNode(TypeDefinition def, AssemblyDefinition[] assemblies)
		{
			this.def = def;
			this.assemblies = assemblies;
			this.LazyLoading = true;
			threading = new ThreadingSupport();
		}
		
		public override bool ShowExpander {
			get {
				return !def.IsSealed && base.ShowExpander;
			}
		}
		
		public override object Text {
			get { return this.Language.TypeToString(def, true); }
		}
		
		public override object Icon {
			get {
				return TypeTreeNode.GetIcon(def);
			}
		}
		
		protected override void LoadChildren()
		{
			threading.LoadChildren(this, FetchChildren);
		}
		
		IEnumerable<ILSpyTreeNode> FetchChildren(CancellationToken ct)
		{
			// FetchChildren() runs on the main thread; but the enumerator will be consumed on a background thread
			return DerivedTypesTreeNode.FindDerivedTypes(def, assemblies, ct);
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
