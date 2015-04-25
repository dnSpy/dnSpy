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

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Static helper methods for working with nullable types.
	/// </summary>
	public static class NullableType
	{
		/// <summary>
		/// Gets whether the specified type is a nullable type.
		/// </summary>
		public static bool IsNullable(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			ParameterizedType pt = type as ParameterizedType;
			return pt != null && pt.TypeParameterCount == 1 && pt.GetDefinition().KnownTypeCode == KnownTypeCode.NullableOfT;
		}
		
		public static bool IsNonNullableValueType(IType type)
		{
			return type.IsReferenceType == false && !IsNullable(type);
		}
		
		/// <summary>
		/// Returns the element type, if <paramref name="type"/> is a nullable type.
		/// Otherwise, returns the type itself.
		/// </summary>
		public static IType GetUnderlyingType(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			ParameterizedType pt = type as ParameterizedType;
			if (pt != null && pt.TypeParameterCount == 1 && pt.FullName == "System.Nullable")
				return pt.GetTypeArgument(0);
			else
				return type;
		}
		
		/// <summary>
		/// Creates a nullable type.
		/// </summary>
		public static IType Create(ICompilation compilation, IType elementType)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (elementType == null)
				throw new ArgumentNullException("elementType");
			
			IType nullableType = compilation.FindType(KnownTypeCode.NullableOfT);
			ITypeDefinition nullableTypeDef = nullableType.GetDefinition();
			if (nullableTypeDef != null)
				return new ParameterizedType(nullableTypeDef, new [] { elementType });
			else
				return nullableType;
		}
		
		/// <summary>
		/// Creates a nullable type reference.
		/// </summary>
		public static ParameterizedTypeReference Create(ITypeReference elementType)
		{
			if (elementType == null)
				throw new ArgumentNullException("elementType");
			return new ParameterizedTypeReference(KnownTypeReference.NullableOfT, new [] { elementType });
		}
	}
}
