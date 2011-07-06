// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.TypeSystem
{
	public class PointerType : TypeWithElementType
	{
		public PointerType(IType elementType) : base(elementType)
		{
		}
		
		public override string NameSuffix {
			get {
				return "*";
			}
		}
		
		public override bool? IsReferenceType(ITypeResolveContext context)
		{
			return null;
		}
		
		public override int GetHashCode()
		{
			return elementType.GetHashCode() ^ 91725811;
		}
		
		public override bool Equals(IType other)
		{
			PointerType a = other as PointerType;
			return a != null && elementType.Equals(a.elementType);
		}
		
		public override IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitPointerType(this);
		}
		
		public override IType VisitChildren(TypeVisitor visitor)
		{
			IType e = elementType.AcceptVisitor(visitor);
			if (e == elementType)
				return this;
			else
				return new PointerType(e);
		}
	}
	
	public class PointerTypeReference : ITypeReference
	{
		readonly ITypeReference elementType;
		
		public PointerTypeReference(ITypeReference elementType)
		{
			if (elementType == null)
				throw new ArgumentNullException("elementType");
			this.elementType = elementType;
		}
		
		public ITypeReference ElementType {
			get { return elementType; }
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			return new PointerType(elementType.Resolve(context));
		}
		
		public override string ToString()
		{
			return elementType.ToString() + "*";
		}
		
		public static ITypeReference Create(ITypeReference elementType)
		{
			if (elementType is IType)
				return new PointerType((IType)elementType);
			else
				return new PointerTypeReference(elementType);
		}
	}
}
