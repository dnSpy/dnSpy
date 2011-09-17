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
using ICSharpCode.NRefactory.TypeSystem.Implementation;

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
			return pt != null && pt.TypeParameterCount == 1 && pt.FullName == "System.Nullable";
		}
		
		public static bool IsNonNullableValueType(IType type, ITypeResolveContext context)
		{
			return type.IsReferenceType(context) == false && !IsNullable(type);
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
		public static IType Create(IType elementType, ITypeResolveContext context)
		{
			if (elementType == null)
				throw new ArgumentNullException("elementType");
			if (context == null)
				throw new ArgumentNullException("context");
			
			ITypeDefinition nullable = context.GetTypeDefinition("System", "Nullable", 1, StringComparer.Ordinal);
			if (nullable != null)
				return new ParameterizedType(nullable, new [] { elementType });
			else
				return SharedTypes.UnknownType;
		}
		
		static readonly ITypeReference NullableReference = new GetClassTypeReference("System", "Nullable", 1);
		
		/// <summary>
		/// Creates a nullable type reference.
		/// </summary>
		public static ITypeReference Create(ITypeReference elementType)
		{
			if (elementType == null)
				throw new ArgumentNullException("elementType");
			return new ParameterizedTypeReference(NullableReference, new [] { elementType });
		}
	}
}
