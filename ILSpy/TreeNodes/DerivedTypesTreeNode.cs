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
using System.Collections.Generic;
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
		readonly ThreadingSupport threading;

		public DerivedTypesTreeNode(AssemblyList list, TypeDefinition type)
		{
			this.list = list;
			this.type = type;
			this.LazyLoading = true;
			this.threading = new ThreadingSupport();
		}

		public override object Text
		{
			get { return "Derived Types"; }
		}

		public override object Icon
		{
			get { return Images.SubTypes; }
		}

		protected override void LoadChildren()
		{
			threading.LoadChildren(this, FetchChildren);
		}

		IEnumerable<ILSpyTreeNode> FetchChildren(CancellationToken cancellationToken)
		{
			// FetchChildren() runs on the main thread; but the enumerator will be consumed on a background thread
			var assemblies = list.GetAssemblies().Select(node => node.ModuleDefinition).Where(asm => asm != null).ToArray();
			return FindDerivedTypes(type, assemblies, cancellationToken);
		}

		internal static IEnumerable<DerivedTypesEntryNode> FindDerivedTypes(TypeDefinition type, ModuleDefinition[] assemblies, CancellationToken cancellationToken)
		{
			foreach (ModuleDefinition module in assemblies) {
				foreach (TypeDefinition td in TreeTraversal.PreOrder(module.Types, t => t.NestedTypes)) {
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
}