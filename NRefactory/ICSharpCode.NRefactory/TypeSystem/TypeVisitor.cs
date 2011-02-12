// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Base class for the visitor pattern on <see cref="IType"/>.
	/// </summary>
	public abstract class TypeVisitor
	{
		public virtual IType VisitTypeDefinition(ITypeDefinition type)
		{
			return type.VisitChildren(this);
		}
		
		public virtual IType VisitTypeParameter(ITypeParameter type)
		{
			return type.VisitChildren(this);
		}
		
		public virtual IType VisitParameterizedType(ParameterizedType type)
		{
			return type.VisitChildren(this);
		}
		
		public virtual IType VisitArrayType(ArrayType type)
		{
			return type.VisitChildren(this);
		}
		
		public virtual IType VisitPointerType(PointerType type)
		{
			return type.VisitChildren(this);
		}
		
		public virtual IType VisitByReferenceType(ByReferenceType type)
		{
			return type.VisitChildren(this);
		}
		
		public virtual IType VisitOtherType(IType type)
		{
			return type.VisitChildren(this);
		}
	}
}
