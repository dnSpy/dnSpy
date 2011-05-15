// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Converts a type by replacing all type definitions with the equivalent definitions in the new context.
	/// </summary>
	sealed class MapTypeIntoNewContext : TypeVisitor
	{
		readonly ITypeResolveContext context;
		
		public MapTypeIntoNewContext(ITypeResolveContext context)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
		}
		
		public override IType VisitTypeDefinition(ITypeDefinition type)
		{
			if (type.DeclaringTypeDefinition != null) {
				ITypeDefinition decl = type.DeclaringTypeDefinition.AcceptVisitor(this) as ITypeDefinition;
				if (decl != null) {
					foreach (ITypeDefinition c in decl.InnerClasses) {
						if (c.Name == type.Name && c.TypeParameterCount == type.TypeParameterCount)
							return c;
					}
				}
				return type;
			} else {
				return context.GetClass(type.Namespace, type.Name, type.TypeParameterCount, StringComparer.Ordinal) ?? type;
			}
		}
		
		public override IType VisitTypeParameter(ITypeParameter type)
		{
			// TODO: how to map type parameters?
			// It might have constraints, and those constraints might be mutually recursive.
			// Maybe reintroduce ITypeParameter.Owner?
			throw new NotImplementedException();
		}
	}
}
