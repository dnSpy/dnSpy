// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents a simple C# name. (a single non-qualified identifier with an optional list of type arguments)
	/// </summary>
	public sealed class SimpleTypeOrNamespaceReference : ITypeOrNamespaceReference
	{
		readonly ITypeDefinition parentTypeDefinition;
		readonly UsingScope parentUsingScope;
		readonly string identifier;
		readonly IList<ITypeReference> typeArguments;
		readonly bool isInUsingDeclaration;
		
		public SimpleTypeOrNamespaceReference(string identifier, IList<ITypeReference> typeArguments, ITypeDefinition parentTypeDefinition, UsingScope parentUsingScope, bool isInUsingDeclaration = false)
		{
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			this.identifier = identifier;
			this.typeArguments = typeArguments ?? EmptyList<ITypeReference>.Instance;
			this.parentTypeDefinition = parentTypeDefinition;
			this.parentUsingScope = parentUsingScope;
			this.isInUsingDeclaration = isInUsingDeclaration;
		}
		
		/// <summary>
		/// Adds a suffix to the identifier.
		/// Does not modify the existing type reference, but returns a new one.
		/// </summary>
		public SimpleTypeOrNamespaceReference AddSuffix(string suffix)
		{
			return new SimpleTypeOrNamespaceReference(identifier + suffix, typeArguments, parentTypeDefinition, parentUsingScope, isInUsingDeclaration);
		}
		
		public ResolveResult DoResolve(ITypeResolveContext context)
		{
			CSharpResolver r = new CSharpResolver(context);
			r.CurrentTypeDefinition = parentTypeDefinition != null ? parentTypeDefinition.GetCompoundClass() : null;
			r.UsingScope = parentUsingScope;
			IType[] typeArgs = new IType[typeArguments.Count];
			for (int i = 0; i < typeArgs.Length; i++) {
				typeArgs[i] = typeArguments[i].Resolve(context);
			}
			return r.LookupSimpleNamespaceOrTypeName(identifier, typeArgs, isInUsingDeclaration);
		}
		
		public NamespaceResolveResult ResolveNamespace(ITypeResolveContext context)
		{
			// TODO: use resolve context for original project, if possible
			return DoResolve(context) as NamespaceResolveResult;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			// TODO: use resolve context for original project, if possible; then map the result type into the new context
			TypeResolveResult rr = DoResolve(context) as TypeResolveResult;
			return rr != null ? rr.Type : SharedTypes.UnknownType;
		}
		
		public override string ToString()
		{
			if (typeArguments.Count == 0)
				return identifier;
			else
				return identifier + "<" + DotNet35Compat.StringJoin(",", typeArguments) + ">";
		}
	}
}
