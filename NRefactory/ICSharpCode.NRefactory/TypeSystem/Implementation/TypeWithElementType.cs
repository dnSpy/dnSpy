// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public abstract class TypeWithElementType : AbstractType
	{
		protected readonly IType elementType;
		
		protected TypeWithElementType(IType elementType)
		{
			if (elementType == null)
				throw new ArgumentNullException("elementType");
			this.elementType = elementType;
		}
		
		public override string Name {
			get { return elementType.Name + NameSuffix; }
		}
		
		public override string Namespace {
			get { return elementType.Namespace; }
		}
		
		public override string FullName {
			get { return elementType.FullName + NameSuffix; }
		}
		
		public override string ReflectionName {
			get { return elementType.ReflectionName + NameSuffix; }
		}
		
		public abstract string NameSuffix { get; }
		
		public IType ElementType {
			get { return elementType; }
		}
		
		// Force concrete implementations to override VisitChildren - the base implementation
		// in AbstractType assumes there are no children, but we know there is (at least) 1.
		public abstract override IType VisitChildren(TypeVisitor visitor);
	}
}
