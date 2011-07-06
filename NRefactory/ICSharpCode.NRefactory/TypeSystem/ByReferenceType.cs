
using System;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.TypeSystem
{
	public class ByReferenceType : TypeWithElementType
	{
		public ByReferenceType(IType elementType) : base(elementType)
		{
		}
		
		public override string NameSuffix {
			get {
				return "&";
			}
		}
		
		public override bool? IsReferenceType(ITypeResolveContext context)
		{
			return null;
		}
		
		public override int GetHashCode()
		{
			return elementType.GetHashCode() ^ 91725813;
		}
		
		public override bool Equals(IType other)
		{
			ByReferenceType a = other as ByReferenceType;
			return a != null && elementType.Equals(a.elementType);
		}
		
		public override IType AcceptVisitor(TypeVisitor visitor)
		{
			return visitor.VisitByReferenceType(this);
		}
		
		public override IType VisitChildren(TypeVisitor visitor)
		{
			IType e = elementType.AcceptVisitor(visitor);
			if (e == elementType)
				return this;
			else
				return new ByReferenceType(e);
		}
	}
	
	public class ByReferenceTypeReference : ITypeReference
	{
		readonly ITypeReference elementType;
		
		public ByReferenceTypeReference(ITypeReference elementType)
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
			return new ByReferenceType(elementType.Resolve(context));
		}
		
		public override string ToString()
		{
			return elementType.ToString() + "&";
		}
		
		public static ITypeReference Create(ITypeReference elementType)
		{
			if (elementType is IType)
				return new ByReferenceType((IType)elementType);
			else
				return new ByReferenceTypeReference(elementType);
		}
	}
}
