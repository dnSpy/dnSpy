// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// A type reference that wraps another type reference; but performs a substitution in the resolved type.
	/// </summary>
	public class SubstitutionTypeReference : ITypeReference
	{
		readonly ITypeReference baseTypeReference;
		readonly TypeVisitor substitution;
		
		public SubstitutionTypeReference(ITypeReference baseTypeReference, TypeVisitor substitution)
		{
			if (baseTypeReference == null)
				throw new ArgumentNullException("baseTypeReference");
			if (substitution == null)
				throw new ArgumentNullException("substitution");
			this.baseTypeReference = baseTypeReference;
			this.substitution = substitution;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			return baseTypeReference.Resolve(context).AcceptVisitor(substitution);
		}
	}
}
