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
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Method/field/property/event.
	/// </summary>
	public interface IUnresolvedMember : IUnresolvedEntity, IMemberReference
	{
		/// <summary>
		/// Gets the return type of this member.
		/// This property never returns null.
		/// </summary>
		ITypeReference ReturnType { get; }
		
		/// <summary>
		/// Gets whether this member is explicitly implementing an interface.
		/// If this property is true, the member can only be called through the interfaces it implements.
		/// </summary>
		bool IsExplicitInterfaceImplementation { get; }
		
		/// <summary>
		/// Gets the interfaces that are explicitly implemented by this member.
		/// </summary>
		IList<IMemberReference> ExplicitInterfaceImplementations { get; }
		
		/// <summary>
		/// Gets if the member is virtual. Is true only if the "virtual" modifier was used, but non-virtual
		/// members can be overridden, too; if they are abstract or overriding a method.
		/// </summary>
		bool IsVirtual { get; }
		
		/// <summary>
		/// Gets whether this member is overriding another member.
		/// </summary>
		bool IsOverride { get; }
		
		/// <summary>
		/// Gets if the member can be overridden. Returns true when the member is "abstract", "virtual" or "override" but not "sealed".
		/// </summary>
		bool IsOverridable { get; }
		
		/// <summary>
		/// Resolves the member.
		/// </summary>
		/// <param name="context">
		/// Context for looking up the member. The context must specify the current assembly.
		/// A <see cref="SimpleTypeResolveContext"/> that specifies the current assembly is sufficient.
		/// </param>
		/// <returns>
		/// Returns the resolved member, or <c>null</c> if the member could not be found.
		/// </returns>
		new IMember Resolve(ITypeResolveContext context);
		
		/// <summary>
		/// Creates the resolved member.
		/// </summary>
		/// <param name="context">
		/// The language-specific context that includes the parent type definition.
		/// <see cref="IUnresolvedTypeDefinition.CreateResolveContext"/>
		/// </param>
		IMember CreateResolved(ITypeResolveContext context);
	}
	
	public interface IMemberReference : ISymbolReference
	{
		/// <summary>
		/// Gets the declaring type reference for the member.
		/// </summary>
		ITypeReference DeclaringTypeReference { get; }
		
		/// <summary>
		/// Resolves the member.
		/// </summary>
		/// <param name="context">
		/// Context to use for resolving this member reference.
		/// Which kind of context is required depends on the which kind of member reference this is;
		/// please consult the documentation of the method that was used to create this member reference,
		/// or that of the class implementing this method.
		/// </param>
		/// <returns>
		/// Returns the resolved member, or <c>null</c> if the member could not be found.
		/// </returns>
		new IMember Resolve(ITypeResolveContext context);
	}
	
	/// <summary>
	/// Method/field/property/event.
	/// </summary>
	public interface IMember : IEntity
	{
		/// <summary>
		/// Gets the original member definition for this member.
		/// Returns <c>this</c> if this is not a specialized member.
		/// Specialized members are the result of overload resolution with type substitution.
		/// </summary>
		IMember MemberDefinition { get; }
		
		/// <summary>
		/// Gets the unresolved member instance from which this member was created.
		/// This property may return <c>null</c> for special members that do not have a corresponding unresolved member instance.
		/// </summary>
		/// <remarks>
		/// For specialized members, this property returns the unresolved member for the original member definition.
		/// For partial methods, this property returns the implementing partial method declaration, if one exists, and the
		/// defining partial method declaration otherwise.
		/// For the members used to represent the built-in C# operators like "operator +(int, int);", this property returns <c>null</c>.
		/// </remarks>
		IUnresolvedMember UnresolvedMember { get; }
		
		/// <summary>
		/// Gets the return type of this member.
		/// This property never returns <c>null</c>.
		/// </summary>
		IType ReturnType { get; }
		
		/// <summary>
		/// Gets the interface members implemented by this member (both implicitly and explicitly).
		/// </summary>
		IList<IMember> ImplementedInterfaceMembers { get; }
		
		/// <summary>
		/// Gets whether this member is explicitly implementing an interface.
		/// </summary>
		bool IsExplicitInterfaceImplementation { get; }
		
		/// <summary>
		/// Gets if the member is virtual. Is true only if the "virtual" modifier was used, but non-virtual
		/// members can be overridden, too; if they are abstract or overriding a method.
		/// </summary>
		bool IsVirtual { get; }
		
		/// <summary>
		/// Gets whether this member is overriding another member.
		/// </summary>
		bool IsOverride { get; }
		
		/// <summary>
		/// Gets if the member can be overridden. Returns true when the member is "abstract", "virtual" or "override" but not "sealed".
		/// </summary>
		bool IsOverridable { get; }
		
		/// <summary>
		/// Creates a member reference that can be used to rediscover this member in another compilation.
		/// </summary>
		/// <remarks>
		/// If this member is specialized using open generic types, the resulting member reference will need to be looked up in an appropriate generic context.
		/// Otherwise, the main resolve context of a compilation is sufficient.
		/// </remarks>
		[Obsolete("Use the ToReference method instead.")]
		IMemberReference ToMemberReference();
		
				/// <summary>
		/// Creates a member reference that can be used to rediscover this member in another compilation.
		/// </summary>
		/// <remarks>
		/// If this member is specialized using open generic types, the resulting member reference will need to be looked up in an appropriate generic context.
		/// Otherwise, the main resolve context of a compilation is sufficient.
		/// </remarks>
		new IMemberReference ToReference();

		/// <summary>
		/// Gets the substitution belonging to this specialized member.
		/// Returns TypeParameterSubstitution.Identity for not specialized members.
		/// </summary>
		TypeParameterSubstitution Substitution {
			get;
		}

		/// <summary>
		/// Specializes this member with the given substitution.
		/// If this member is already specialized, the new substitution is composed with the existing substition.
		/// </summary>
		IMember Specialize(TypeParameterSubstitution substitution);
	}
}
