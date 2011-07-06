// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
			return pt != null && pt.TypeArguments.Count == 1 && pt.FullName == "System.Nullable";
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
			if (pt != null && pt.TypeArguments.Count == 1 && pt.FullName == "System.Nullable")
				return pt.TypeArguments[0];
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
