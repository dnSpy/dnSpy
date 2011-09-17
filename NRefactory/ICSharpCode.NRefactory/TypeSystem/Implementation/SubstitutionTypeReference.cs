// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
		
		public static ITypeReference Create(ITypeReference baseTypeReference, TypeVisitor substitution)
		{
			IType baseType = baseTypeReference as IType;
			if (baseType != null && substitution != null) {
				return baseType.AcceptVisitor(substitution);
			} else {
				return new SubstitutionTypeReference(baseTypeReference, substitution);
			}
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			return baseTypeReference.Resolve(context).AcceptVisitor(substitution);
		}
		
		public override string ToString()
		{
			return "[SubstitutionTypeReference " + baseTypeReference + " " + substitution + "]";
		}
	}
}
