// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	public abstract class TypeWithElementType : AbstractType
	{
		[CLSCompliant(false)]
		protected IType elementType;
		
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
