// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	#if WITH_CONTRACTS
	[ContractClass(typeof(ITypeContract))]
	#endif
	public interface IType : ITypeReference, INamedElement, IEquatable<IType>
	{
		/// <summary>
		/// Gets whether the type is a reference type or value type.
		/// </summary>
		/// <returns>
		/// true, if the type is a reference type.
		/// false, if the type is a value type.
		/// null, if the type is not known (e.g. unconstrained generic type parameter or type not found)
		/// </returns>
		/// <remarks>
		/// The resolve context is required for type parameters with a constraint "T : SomeType":
		/// the type parameter is a reference type iff SomeType is a class type.
		/// </remarks>
		bool? IsReferenceType(ITypeResolveContext context);
		
		/// <summary>
		/// Gets the underlying type definition.
		/// Can return null for types which do not have a type definition (for example arrays, pointers, type parameters)
		/// </summary>
		ITypeDefinition GetDefinition();
		
		/// <summary>
		/// Gets the parent type, if this is a nested type.
		/// Returns null for top-level types.
		/// </summary>
		IType DeclaringType { get; }
		
		/// <summary>
		/// Gets the number of type parameters.
		/// </summary>
		int TypeParameterCount { get; }
		
		/// <summary>
		/// Calls ITypeVisitor.Visit for this type.
		/// </summary>
		/// <returns>The return value of the ITypeVisitor.Visit call</returns>
		IType AcceptVisitor(TypeVisitor visitor);
		
		/// <summary>
		/// Calls ITypeVisitor.Visit for all children of this type, and reconstructs this type with the children based
		/// on the return values of the visit calls.
		/// </summary>
		/// <returns>A copy of this type, with all children replaced by the return value of the corresponding visitor call.
		/// If the visitor returned the original types for all children (or if there are no children), returns <c>this</c>.
		/// </returns>
		IType VisitChildren(TypeVisitor visitor);
		
		/// <summary>
		/// Gets the direct base types.
		/// </summary>
		IEnumerable<IType> GetBaseTypes(ITypeResolveContext context);
		
		/// <summary>
		/// Gets inner classes (including inherited inner classes).
		/// </summary>
		IEnumerable<IType> GetNestedTypes(ITypeResolveContext context, Predicate<ITypeDefinition> filter = null);
		
		/// <summary>
		/// Gets all methods that can be called on this return type.
		/// </summary>
		/// <remarks>The list does not include constructors.</remarks>
		IEnumerable<IMethod> GetMethods(ITypeResolveContext context, Predicate<IMethod> filter = null);
		
		/// <summary>
		/// Gets all instance constructors for this type.
		/// </summary>
		/// <remarks>This list does not include constructors in base classes or static constructors.</remarks>
		IEnumerable<IMethod> GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter = null);
		
		/// <summary>
		/// Gets all properties that can be called on this return type.
		/// </summary>
		IEnumerable<IProperty> GetProperties(ITypeResolveContext context, Predicate<IProperty> filter = null);
		
		/// <summary>
		/// Gets all fields that can be called on this return type.
		/// </summary>
		IEnumerable<IField> GetFields(ITypeResolveContext context, Predicate<IField> filter = null);
		
		/// <summary>
		/// Gets all events that can be called on this return type.
		/// </summary>
		IEnumerable<IEvent> GetEvents(ITypeResolveContext context, Predicate<IEvent> filter = null);
		
		/// <summary>
		/// Gets all members that can be called on this return type.
		/// This is the union of GetFields(),GetProperties(),GetMethods() and GetEvents(). This does not include constructors.
		/// </summary>
		IEnumerable<IMember> GetMembers(ITypeResolveContext context, Predicate<IMember> filter = null);
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(IType))]
	abstract class ITypeContract : ITypeReferenceContract, IType
	{
		bool? IType.IsReferenceType(ITypeResolveContext context)
		{
			Contract.Requires(context != null);
			return null;
		}
		
		int IType.TypeParameterCount {
			get {
				Contract.Ensures(Contract.Result<int>() >= 0);
				return 0;
			}
		}
		
		IType IType.DeclaringType {
			get { return null; }
		}
		
		IEnumerable<IType> IType.GetBaseTypes(ITypeResolveContext context)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IType>>() != null);
			return null;
		}
		
		IEnumerable<IType> IType.GetNestedTypes(ITypeResolveContext context, Predicate<ITypeDefinition> filter)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IType>>() != null);
			return null;
		}

		IEnumerable<IMethod> IType.GetMethods(ITypeResolveContext context, Predicate<IMethod> filter)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IMethod>>() != null);
			return null;
		}
		
		IEnumerable<IMethod> IType.GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IMethod>>() != null);
			return null;
		}
		
		IEnumerable<IProperty> IType.GetProperties(ITypeResolveContext context, Predicate<IProperty> filter)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IProperty>>() != null);
			return null;
		}
		
		IEnumerable<IField> IType.GetFields(ITypeResolveContext context, Predicate<IField> filter)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IField>>() != null);
			return null;
		}
		
		IEnumerable<IEvent> IType.GetEvents(ITypeResolveContext context, Predicate<IEvent> filter)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IEvent>>() != null);
			return null;
		}
		
		IEnumerable<IMember> IType.GetEvents(ITypeResolveContext context, Predicate<IMember> filter)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IMember>>() != null);
			return null;
		}
		
		string INamedElement.FullName {
			get {
				Contract.Ensures(Contract.Result<string>() != null);
				return null;
			}
		}
		
		string INamedElement.Name {
			get {
				Contract.Ensures(Contract.Result<string>() != null);
				return null;
			}
		}
		
		string INamedElement.Namespace {
			get {
				Contract.Ensures(Contract.Result<string>() != null);
				return null;
			}
		}
		
		string INamedElement.ReflectionName {
			get {
				Contract.Ensures(Contract.Result<string>() != null);
				return null;
			}
		}
		
		ITypeDefinition IType.GetDefinition()
		{
			return null;
		}
		
		bool IEquatable<IType>.Equals(IType other)
		{
			return false;
		}
		
		IType IType.AcceptVisitor(TypeVisitor visitor)
		{
			Contract.Requires(visitor != null);
			Contract.Ensures(Contract.Result<IType>() != null);
			return this;
		}
		
		IType IType.VisitChildren(TypeVisitor visitor)
		{
			Contract.Requires(visitor != null);
			Contract.Ensures(Contract.Result<IType>() != null);
			return this;
		}
	}
	#endif
}
