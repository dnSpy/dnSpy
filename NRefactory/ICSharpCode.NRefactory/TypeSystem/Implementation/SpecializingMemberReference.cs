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
		ITypeReference declaringTypeReference;
		IMemberReference memberDefinitionReference;
		IList<ITypeReference> typeArgumentReferences;
		
		public SpecializingMemberReference(ITypeReference declaringTypeReference, IMemberReference memberDefinitionReference, IList<ITypeReference> typeArgumentReferences = null)
		{
			if (declaringTypeReference == null)
				throw new ArgumentNullException("declaringTypeReference");
			if (memberDefinitionReference == null)
				throw new ArgumentNullException("memberDefinitionReference");
			this.declaringTypeReference = declaringTypeReference;
			this.memberDefinitionReference = memberDefinitionReference;
			this.typeArgumentReferences = typeArgumentReferences;
		}
		
		public IMember Resolve(ITypeResolveContext context)
		{
			var declaringType = declaringTypeReference.Resolve(context);
			var memberDefinition = memberDefinitionReference.Resolve(context);
			IType[] typeArguments = null;
			if (typeArgumentReferences != null) {
				typeArguments = new IType[typeArgumentReferences.Count];
				for (int i = 0; i < typeArguments.Length; i++) {
					typeArguments[i] = typeArgumentReferences[i].Resolve(context);
				}
			}
			return CreateSpecializedMember(declaringType, memberDefinition, typeArguments);
		}
		
		internal static IMember CreateSpecializedMember(IType declaringType, IMember memberDefinition, IList<IType> typeArguments)
		{
			if (memberDefinition == null) {
				return null;
			} else if (memberDefinition is IMethod) {
				return new SpecializedMethod(declaringType, (IMethod)memberDefinition, typeArguments);
			} else if (memberDefinition is IProperty) {
				return new SpecializedProperty(declaringType, (IProperty)memberDefinition);
			} else if (memberDefinition is IField) {
				return new SpecializedField(declaringType, (IField)memberDefinition);
			} else if (memberDefinition is IEvent) {
				return new SpecializedEvent(declaringType, (IEvent)memberDefinition);
			} else {
				throw new NotSupportedException("Unknown IMember: " + memberDefinition);
			}
		}
	}
}
