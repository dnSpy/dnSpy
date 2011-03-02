// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Reference to a qualified type or namespace name.
	/// </summary>
	public sealed class MemberTypeOrNamespaceReference : ITypeOrNamespaceReference
	{
		readonly ITypeOrNamespaceReference target;
		readonly ITypeDefinition parentTypeDefinition;
		readonly UsingScope parentUsingScope;
		readonly string identifier;
		readonly IList<ITypeReference> typeArguments;
		
		public MemberTypeOrNamespaceReference(ITypeOrNamespaceReference target, string identifier, IList<ITypeReference> typeArguments, ITypeDefinition parentTypeDefinition, UsingScope parentUsingScope)
		{
			if (target == null)
				throw new ArgumentNullException("target");
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			this.target = target;
			this.identifier = identifier;
			this.typeArguments = typeArguments ?? EmptyList<ITypeReference>.Instance;
			this.parentTypeDefinition = parentTypeDefinition;
			this.parentUsingScope = parentUsingScope;
		}
		
		public ResolveResult DoResolve(ITypeResolveContext context)
		{
			ResolveResult targetRR = target.DoResolve(context);
			if (targetRR.IsError)
				return targetRR;
			CSharpResolver r = new CSharpResolver(context);
			r.CurrentTypeDefinition = parentTypeDefinition != null ? parentTypeDefinition.GetCompoundClass() : null;
			r.UsingScope = parentUsingScope;
			IType[] typeArgs = new IType[typeArguments.Count];
			for (int i = 0; i < typeArgs.Length; i++) {
				typeArgs[i] = typeArguments[i].Resolve(context);
			}
			return r.ResolveMemberAccess(targetRR, identifier, typeArgs, false);
		}
		
		public NamespaceResolveResult ResolveNamespace(ITypeResolveContext context)
		{
			return DoResolve(context) as NamespaceResolveResult;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			TypeResolveResult rr = DoResolve(context) as TypeResolveResult;
			return rr != null ? rr.Type : SharedTypes.UnknownType;
		}
		
		public override string ToString()
		{
			if (typeArguments.Count == 0)
				return target.ToString() + "." + identifier;
			else
				return target.ToString() + "." + identifier + "<" + DotNet35Compat.StringJoin(",", typeArguments) + ">";
		}
	}
}
