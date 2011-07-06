// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Type reference used to reference nested types.
	/// </summary>
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
