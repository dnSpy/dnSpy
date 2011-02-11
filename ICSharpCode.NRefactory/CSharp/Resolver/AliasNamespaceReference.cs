// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Looks up an alias (identifier in front of :: operator).
	/// </summary>
	/// <remarks>
	/// The member lookup performed by the :: operator is handled
	/// by <see cref="MemberTypeOrNamespaceReference"/>.
	/// </remarks>
	public class AliasNamespaceReference : ITypeOrNamespaceReference
	{
		readonly UsingScope parentUsingScope;
		readonly string identifier;
		
		public AliasNamespaceReference(string identifier, UsingScope parentUsingScope)
		{
			if (identifier == null)
				throw new ArgumentNullException("identifier");
			this.identifier = identifier;
			this.parentUsingScope = parentUsingScope;
		}
		
		public ResolveResult DoResolve(ITypeResolveContext context)
		{
			CSharpResolver r = new CSharpResolver(context);
			r.UsingScope = parentUsingScope;
			return r.ResolveAlias(identifier);
		}
		
		public NamespaceResolveResult ResolveNamespace(ITypeResolveContext context)
		{
			return DoResolve(context) as NamespaceResolveResult;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			return SharedTypes.UnknownType;
		}
		
		public override string ToString()
		{
			return identifier + "::";
		}
	}
}
