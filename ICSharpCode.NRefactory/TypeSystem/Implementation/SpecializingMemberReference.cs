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
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	[Serializable]
	public sealed class SpecializingMemberReference : IMemberReference
	{
		IMemberReference memberDefinitionReference;
		IList<ITypeReference> classTypeArgumentReferences;
		IList<ITypeReference> methodTypeArgumentReferences;
		
		public SpecializingMemberReference(IMemberReference memberDefinitionReference, IList<ITypeReference> classTypeArgumentReferences = null, IList<ITypeReference> methodTypeArgumentReferences = null)
		{
			if (memberDefinitionReference == null)
				throw new ArgumentNullException("memberDefinitionReference");
			this.memberDefinitionReference = memberDefinitionReference;
			this.classTypeArgumentReferences = classTypeArgumentReferences;
			this.methodTypeArgumentReferences = methodTypeArgumentReferences;
		}
		
		public IMember Resolve(ITypeResolveContext context)
		{
			var memberDefinition = memberDefinitionReference.Resolve(context);
			return SpecializedMember.Create(
				memberDefinition,
				new TypeParameterSubstitution(
					classTypeArgumentReferences != null ? classTypeArgumentReferences.Resolve(context) : null,
					methodTypeArgumentReferences != null ? methodTypeArgumentReferences.Resolve(context) : null
				)
			);
		}
		
		public ITypeReference DeclaringTypeReference {
			get {
				if (classTypeArgumentReferences != null)
					return new ParameterizedTypeReference(memberDefinitionReference.DeclaringTypeReference, classTypeArgumentReferences);
				else
					return memberDefinitionReference.DeclaringTypeReference;
			}
		}
	}
}
