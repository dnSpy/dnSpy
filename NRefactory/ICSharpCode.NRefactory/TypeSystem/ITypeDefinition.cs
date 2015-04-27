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
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents an unresolved class, enum, interface, struct, delegate or VB module.
	/// For partial classes, an unresolved type definition represents only a single part.
	/// </summary>
	public interface IUnresolvedTypeDefinition : ITypeReference, IUnresolvedEntity
	{
		TypeKind Kind { get; }
		
		FullTypeName FullTypeName { get; }
		IList<ITypeReference> BaseTypes { get; }
		IList<IUnresolvedTypeParameter> TypeParameters { get; }
		
		IList<IUnresolvedTypeDefinition> NestedTypes { get; }
		IList<IUnresolvedMember> Members { get; }
		
		IEnumerable<IUnresolvedMethod> Methods { get; }
		IEnumerable<IUnresolvedProperty> Properties { get; }
		IEnumerable<IUnresolvedField> Fields { get; }
		IEnumerable<IUnresolvedEvent> Events { get; }
		
		/// <summary>
		/// Gets whether the type definition contains extension methods.
		/// Returns null when the type definition needs to be resolved in order to determine whether
		/// methods are extension methods.
		/// </summary>
		bool? HasExtensionMethods { get; }
		
		/// <summary>
		/// Gets whether the partial modifier is set on this part of the type definition.
		/// </summary>
		bool IsPartial { get; }
		
		/// <summary>
		/// Gets whether this unresolved type definition causes the addition of a default constructor
		/// if no other constructor is present.
		/// </summary>
		bool AddDefaultConstructorIfRequired { get; }
		
		/// <summary>
		/// Looks up the resolved type definition from the <paramref name="context"/> corresponding to this unresolved
		/// type definition.
		/// </summary>
		/// <param name="context">
		/// Context for looking up the type. The context must specify the current assembly.
		/// A <see cref="SimpleTypeResolveContext"/> that specifies the current assembly is sufficient.
		/// </param>
		/// <returns>
		/// Returns the resolved type definition.
		/// In case of an error, returns an <see cref="Implementation.UnknownType"/> instance.
		/// Never returns null.
		/// </returns>
		new IType Resolve(ITypeResolveContext context);
		
		/// <summary>
		/// This method is used to add language-specific elements like the C# UsingScope
		/// to the type resolve context.
		/// </summary>
		/// <param name="parentContext">The parent context (e.g. the parent assembly),
		/// including the parent type definition for inner classes.</param>
		/// <returns>
		/// The parent context, modified to include language-specific elements (e.g. using scope)
		/// associated with this type definition.
		/// </returns>
		/// <remarks>
		/// Use <c>unresolvedTypeDef.CreateResolveContext(parentContext).WithTypeDefinition(typeDef)</c> to
		/// create the context for use within the type definition.
		/// </remarks>
		ITypeResolveContext CreateResolveContext(ITypeResolveContext parentContext);
	}
	
	/// <summary>
	/// Represents a class, enum, interface, struct, delegate or VB module.
	/// For partial classes, this represents the whole class.
	/// </summary>
	public interface ITypeDefinition : IType, IEntity
	{
		/// <summary>
		/// Returns all parts that contribute to this type definition.
		/// Non-partial classes have a single part that represents the whole class.
		/// </summary>
		IList<IUnresolvedTypeDefinition> Parts { get; }
		
		IList<ITypeParameter> TypeParameters { get; }
		
		IList<ITypeDefinition> NestedTypes { get; }
		IList<IMember> Members { get; }
		
		IEnumerable<IField> Fields { get; }
		IEnumerable<IMethod> Methods { get; }
		IEnumerable<IProperty> Properties { get; }
		IEnumerable<IEvent> Events { get; }
		
		/// <summary>
		/// Gets the known type code for this type definition.
		/// </summary>
		KnownTypeCode KnownTypeCode { get; }
		
		/// <summary>
		/// For enums: returns the underlying primitive type.
		/// For all other types: returns <see cref="SpecialType.UnknownType"/>.
		/// </summary>
		IType EnumUnderlyingType { get; }
		
		/// <summary>
		/// Gets the full name of this type.
		/// </summary>
		FullTypeName FullTypeName { get; }
		
		/// <summary>
		/// Gets/Sets the declaring type (incl. type arguments, if any).
		/// This property never returns null -- for top-level entities, it returns SharedTypes.UnknownType.
		/// </summary>
		new IType DeclaringType { get; } // solves ambiguity between IType.DeclaringType and IEntity.DeclaringType
		
		/// <summary>
		/// Gets whether this type contains extension methods.
		/// </summary>
		/// <remarks>This property is used to speed up the search for extension methods.</remarks>
		bool HasExtensionMethods { get; }
		
		/// <summary>
		/// Gets whether this type definition is made up of one or more partial classes.
		/// </summary>
		bool IsPartial { get; }
		
		/// <summary>
		/// Determines how this type is implementing the specified interface member.
		/// </summary>
		/// <returns>
		/// The method on this type that implements the interface member;
		/// or null if the type does not implement the interface.
		/// </returns>
		IMember GetInterfaceImplementation(IMember interfaceMember);
		
		/// <summary>
		/// Determines how this type is implementing the specified interface members.
		/// </summary>
		/// <returns>
		/// For each interface member, this method returns the class member 
		/// that implements the interface member.
		/// For interface members that are missing an implementation, the
		/// result collection will contain a null element.
		/// </returns>
		IList<IMember> GetInterfaceImplementation(IList<IMember> interfaceMembers);
	}
}
