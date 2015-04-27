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
	/// .
	/// </summary>
	public enum TypeKind : byte
	{
		/// <summary>Language-specific type that is not part of NRefactory.TypeSystem itself.</summary>
		Other,
		
		/// <summary>A <see cref="ITypeDefinition"/> or <see cref="ParameterizedType"/> that is a class.</summary>
		Class,
		/// <summary>A <see cref="ITypeDefinition"/> or <see cref="ParameterizedType"/> that is an interface.</summary>
		Interface,
		/// <summary>A <see cref="ITypeDefinition"/> or <see cref="ParameterizedType"/> that is a struct.</summary>
		Struct,
		/// <summary>A <see cref="ITypeDefinition"/> or <see cref="ParameterizedType"/> that is a delegate.</summary>
		/// <remarks><c>System.Delegate</c> itself is TypeKind.Class</remarks>
		Delegate,
		/// <summary>A <see cref="ITypeDefinition"/> that is an enum.</summary>
		/// <remarks><c>System.Enum</c> itself is TypeKind.Class</remarks>
		Enum,
		/// <summary>A <see cref="ITypeDefinition"/> that is a module (VB).</summary>
		Module,
		
		/// <summary>The <c>System.Void</c> type.</summary>
		/// <see cref="KnownTypeReference.Void"/>
		Void,
		
		/// <see cref="SpecialType.UnknownType"/>
		Unknown,
		/// <summary>The type of the null literal.</summary>
		/// <see cref="SpecialType.NullType"/>
		Null,
		/// <summary>Type representing the C# 'dynamic' type.</summary>
		/// <see cref="SpecialType.Dynamic"/>
		Dynamic,
		/// <summary>Represents missing type arguments in partially parameterized types.</summary>
		/// <see cref="SpecialType.UnboundTypeArgument"/>
		/// <see cref="IType.GetNestedTypes(Predicate{ITypeDefinition}, GetMemberOptions)"/>
		UnboundTypeArgument,
		
		/// <summary>The type is a type parameter.</summary>
		/// <see cref="ITypeParameter"/>
		TypeParameter,
		
		/// <summary>An array type</summary>
		/// <see cref="ArrayType"/>
		Array,
		/// <summary>A pointer type</summary>
		/// <see cref="PointerType"/>
		Pointer,
		/// <summary>A managed reference type</summary>
		/// <see cref="ByReferenceType"/>
		ByReference,
		/// <summary>An anonymous type</summary>
		/// <see cref="AnonymousType"/>
		Anonymous,
		
		/// <summary>Intersection of several types</summary>
		/// <see cref="IntersectionType"/>
		Intersection
	}
}
