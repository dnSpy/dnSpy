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
using System.Globalization;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	[Serializable]
	public sealed class TypeParameterReference : ITypeReference, ISupportsInterning
	{
		readonly EntityType ownerType;
		readonly int index;
		
		public TypeParameterReference(EntityType ownerType, int index)
		{
			this.ownerType = ownerType;
			this.index = index;
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			if (ownerType == EntityType.Method) {
				IMethod method = context.CurrentMember as IMethod;
				if (method != null && index < method.TypeParameters.Count) {
					return method.TypeParameters[index];
				}
				return DummyTypeParameter.GetMethodTypeParameter(index);
			} else if (ownerType == EntityType.TypeDefinition) {
				ITypeDefinition typeDef = context.CurrentTypeDefinition;
				if (typeDef != null && index < typeDef.TypeParameters.Count) {
					return typeDef.TypeParameters[index];
				}
				return DummyTypeParameter.GetClassTypeParameter(index);
			} else {
				return SpecialType.UnknownType;
			}
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return index * 33 + (int)ownerType;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			TypeParameterReference r = other as TypeParameterReference;
			return r != null && index == r.index && ownerType == r.ownerType;
		}
		
		public override string ToString()
		{
			if (ownerType == EntityType.Method)
				return "!!" + index.ToString(CultureInfo.InvariantCulture);
			else
				return "!" + index.ToString(CultureInfo.InvariantCulture);
		}
	}
}
