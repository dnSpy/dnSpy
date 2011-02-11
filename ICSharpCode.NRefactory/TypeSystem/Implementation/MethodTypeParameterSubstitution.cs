// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Substitutes method type parameters with type arguments. Does not modify class type parameters.
	/// </summary>
	public class MethodTypeParameterSubstitution : TypeVisitor
	{
		readonly IList<IType> typeArguments;
		
		public MethodTypeParameterSubstitution(IList<IType> typeArguments)
		{
			this.typeArguments = typeArguments;
		}
		
		public override IType VisitTypeParameter(ITypeParameter type)
		{
			int index = type.Index;
			if (type.ParentMethod != null) {
				if (index >= 0 && index < typeArguments.Count)
					return typeArguments[index];
				else
					return SharedTypes.UnknownType;
			} else {
				return base.VisitTypeParameter(type);
			}
		}
	}
}
