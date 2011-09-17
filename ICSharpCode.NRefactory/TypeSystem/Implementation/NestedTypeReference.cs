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
	/// Type reference used to reference nested types.
	/// </summary>
	[Serializable]
	public sealed class NestedTypeReference : ITypeReference, ISupportsInterning
	{
		ITypeReference declaringTypeRef;
		string name;
		int additionalTypeParameterCount;
		
		/// <summary>
		/// Creates a new NestedTypeReference.
		/// </summary>
		/// <param name="declaringTypeRef">Reference to the declaring type.</param>
		/// <param name="name">Name of the nested class</param>
		/// <param name="additionalTypeParameterCount">Number of type parameters on the inner class (without type parameters on baseTypeRef)</param>
		/// <remarks>
		/// <paramref name="declaringTypeRef"/> must be exactly the (unbound) declaring type, not a derived type, not a parameterized type.
		/// NestedTypeReference thus always resolves to a type definition, never to (partially) parameterized types.
		/// </remarks>
		public NestedTypeReference(ITypeReference declaringTypeRef, string name, int additionalTypeParameterCount)
		{
			if (declaringTypeRef == null)
				throw new ArgumentNullException("declaringTypeRef");
			if (name == null)
				throw new ArgumentNullException("name");
			this.declaringTypeRef = declaringTypeRef;
			this.name = name;
			this.additionalTypeParameterCount = additionalTypeParameterCount;
		}
		
		public ITypeReference DeclaringTypeReference {
			get { return declaringTypeRef; }
		}
		
		public string Name {
			get { return name; }
		}
		
		public int AdditionalTypeParameterCount {
			get { return additionalTypeParameterCount; }
		}
		
		public IType Resolve(ITypeResolveContext context)
		{
			ITypeDefinition declaringType = declaringTypeRef.Resolve(context) as ITypeDefinition;
			if (declaringType != null) {
				int tpc = declaringType.TypeParameterCount;
				foreach (IType type in declaringType.NestedTypes) {
					if (type.Name == name && type.TypeParameterCount == tpc + additionalTypeParameterCount)
						return type;
				}
			}
			return SharedTypes.UnknownType;
		}
		
		public override string ToString()
		{
			if (additionalTypeParameterCount == 0)
				return declaringTypeRef + "+" + name;
			else
				return declaringTypeRef + "+" + name + "`" + additionalTypeParameterCount;
		}
		
		void ISupportsInterning.PrepareForInterning(IInterningProvider provider)
		{
			declaringTypeRef = provider.Intern(declaringTypeRef);
			name = provider.Intern(name);
		}
		
		int ISupportsInterning.GetHashCodeForInterning()
		{
			return declaringTypeRef.GetHashCode() ^ name.GetHashCode() ^ additionalTypeParameterCount;
		}
		
		bool ISupportsInterning.EqualsForInterning(ISupportsInterning other)
		{
			NestedTypeReference o = other as NestedTypeReference;
			return o != null && declaringTypeRef == o.declaringTypeRef && name == o.name && additionalTypeParameterCount == o.additionalTypeParameterCount;
		}
	}
}
