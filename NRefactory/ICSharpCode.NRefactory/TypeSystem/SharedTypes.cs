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
	/// Contains static implementations of well-known types.
	/// </summary>
	public static class SharedTypes
	{
		/// <summary>
		/// Gets the type representing resolve errors.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "It's immutable")]
		public readonly static IType UnknownType = new SharedTypeImpl(TypeKind.Unknown, "?", isReferenceType: null);
		
		/// <summary>
		/// The null type is used as type of the null literal. It is a reference type without any members; and it is a subtype of all reference types.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "It's immutable")]
		public readonly static IType Null = new SharedTypeImpl(TypeKind.Null, "null", isReferenceType: true);
		
		/// <summary>
		/// Type representing the C# 'dynamic' type.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "It's immutable")]
		public readonly static IType Dynamic = new SharedTypeImpl(TypeKind.Dynamic, "dynamic", isReferenceType: true);
		
		/// <summary>
		/// A type used for unbound type arguments in partially parameterized types.
		/// </summary>
		/// <see cref="IType.GetNestedTypes"/>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "It's immutable")]
		public readonly static IType UnboundTypeArgument = new SharedTypeImpl(TypeKind.UnboundTypeArgument, "", isReferenceType: null);
		
		/*
		 * I'd like to define static instances for common types like
		 * void, int, etc.; but there are two problems with this:
		 * 
		 * SharedTypes.Void.GetDefinition().ProjectContent should return mscorlib, but
		 * we can't do that without providing a context.
		 * 
		 * Assuming we add a context parameter to GetDefinition():
		 * 
		 * SharedType.Void.Equals(SharedType.Void.GetDefinition(x))
		 * SharedType.Void.GetDefinition(y).Equals(SharedType.Void)
		 * should both return true.
		 * But if the type can have multiple definitions (multiple mscorlib versions loaded),
		 * then this is not possible without violating transitivity of Equals():
		 * 
		 * SharedType.Void.GetDefinition(x).Equals(SharedType.Void.GetDefinition(y))
		 * would have to return true even though these are two distinct definitions.
		 */
		
		[Serializable]
		sealed class SharedTypeImpl : AbstractType
		{
			readonly TypeKind kind;
			readonly string name;
			readonly bool? isReferenceType;
			
			public SharedTypeImpl(TypeKind kind, string name, bool? isReferenceType)
			{
				this.kind = kind;
				this.name = name;
				this.isReferenceType = isReferenceType;
			}
			
			public override TypeKind Kind {
				get { return kind; }
			}
			
			public override string Name {
				get { return name; }
			}
			
			public override bool? IsReferenceType(ITypeResolveContext context)
			{
				return isReferenceType;
			}
			
			public override bool Equals(IType other)
			{
				return other != null && other.Kind == kind;
			}
			
			public override int GetHashCode()
			{
				return (int)kind;
			}
		}
	}
}
