// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
		public readonly static IType UnknownType = new UnknownTypeImpl();
		
		/// <summary>
		/// The null type is used as type of the null literal. It is a reference type without any members; and it is a subtype of all reference types.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "It's immutable")]
		public readonly static IType Null = new NullType();
		
		/// <summary>
		/// Type representing the C# 'dynamic' type.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification = "It's immutable")]
		public readonly static IType Dynamic = new DynamicType();
		
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
		
		/// <summary>
		/// Type representing resolve errors.
		/// </summary>
		sealed class UnknownTypeImpl : AbstractType
		{
			public override string Name {
				get { return "?"; }
			}
			
			public override bool? IsReferenceType(ITypeResolveContext context)
			{
				return null;
			}
			
			public override bool Equals(IType other)
			{
				return other is UnknownTypeImpl;
			}
			
			public override int GetHashCode()
			{
				return 950772036;
			}
		}
		
		/// <summary>
		/// Type of the 'null' literal.
		/// </summary>
		sealed class NullType : AbstractType
		{
			public override string Name {
				get { return "null"; }
			}
			
			public override bool? IsReferenceType(ITypeResolveContext context)
			{
				return true;
			}
			
			public override bool Equals(IType other)
			{
				return other is NullType;
			}
			
			public override int GetHashCode()
			{
				return 362709548;
			}
		}
		
		/// <summary>
		/// Type representing the C# 'dynamic' type.
		/// </summary>
		sealed class DynamicType : AbstractType
		{
			public override string Name {
				get { return "dynamic"; }
			}
			
			public override bool? IsReferenceType(ITypeResolveContext context)
			{
				return true;
			}
			
			public override bool Equals(IType other)
			{
				return other is DynamicType;
			}
			
			public override int GetHashCode()
			{
				return 31986112;
			}
		}
	}
}
