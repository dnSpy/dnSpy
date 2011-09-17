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
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// This interface represents a resolved type in the type system.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A type is potentially
	/// - a type definition (<see cref="ITypeDefiniton"/>, i.e. a class, struct, interface, delegate, or built-in primitive type)
	/// - a parameterized type (<see cref="ParameterizedType"/>, e.g. List&lt;int>)
	/// - a type parameter (<see cref="ITypeParameter"/>, e.g. T)
	/// - an array (<see cref="ArrayType"/>)
	/// - a pointer (<see cref="PointerType"/>)
	/// - a managed reference (<see cref="ByReferenceType"/>)
	/// - one of the special types (<see cref="SharedTypes.UnknownType"/>, <see cref="SharedTypes.Null"/>,
	///      <see cref="SharedTypes.Dynamic"/>, <see cref="SharedTypes.UnboundTypeArgument"/>)
	/// 
	/// The <see cref="IType.Kind"/> property can be used to switch on the kind of a type.
	/// </para>
	/// <para>
	/// IType uses the null object pattern: <see cref="SharedTypes.UnknownType"/> serves as the null object.
	/// Methods or properties returning IType never return null unless documented otherwise.
	/// </para>
	/// <para>
	/// Types should be compared for equality using the <see cref="IType.Equals(IType)"/> method.
	/// Identical types do not necessarily use the same object reference.
	/// </para>
	/// </remarks>
	#if WITH_CONTRACTS
	[ContractClass(typeof(ITypeContract))]
	#endif
	public interface IType : ITypeReference, INamedElement, IEquatable<IType>
	{
		/// <summary>
		/// Gets the type kind.
		/// </summary>
		TypeKind Kind { get; }
		
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
		/// Can return null for types which do not have a type definition (for example arrays, pointers, type parameters).
		/// 
		/// For partial classes, this method always returns the <see cref="CompoundTypeDefinition"/>.
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
		/// <param name="context">The context used for resolving type references</param>
		/// <returns>Returns the direct base types including interfaces</returns>
		IEnumerable<IType> GetBaseTypes(ITypeResolveContext context);
		
		/// <summary>
		/// Gets inner classes (including inherited inner classes).
		/// </summary>
		/// <param name="context">The context used for resolving type references</param>
		/// <param name="filter">The filter used to select which types to return.
		/// The filter is tested on the original type definitions (before parameterization).</param>
		/// <remarks>
		/// <para>
		/// If the nested type is generic (and has more type parameters than the outer class),
		/// this method will return a parameterized type,
		/// where the additional type parameters are set to <see cref="SharedType.UnboundTypeArgument"/>.
		/// </para>
		/// <para>
		/// Type parameters belonging to the outer class will have the value copied from the outer type
		/// if it is a parameterized type. Otherwise, those existing type parameters will be self-parameterized,
		/// and thus 'leaked' to the caller in the same way the GetMembers() method does not specialize members
		/// from an <see cref="ITypeDefinition"/> and 'leaks' type parameters in member signatures.
		/// </para>
		/// </remarks>
		/// <example>
		/// <code>
		/// class Base&lt;T> {
		/// 	class Nested&lt;X> {}
		/// }
		/// class Derived&lt;A, B> : Base&lt;B> {}
		/// 
		/// Derived[string,int].GetNestedTypes() = { Base`1+Nested`1[int, unbound] }
		/// Derived.GetNestedTypes() = { Base`1+Nested`1[`1, unbound] }
		/// Base[`1].GetNestedTypes() = { Base`1+Nested`1[`1, unbound] }
		/// Base.GetNestedTypes() = { Base`1+Nested`1[`0, unbound] }
		/// </code>
		/// </example>
		IEnumerable<IType> GetNestedTypes(ITypeResolveContext context, Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None);
		
		// Note that we cannot 'leak' the additional type parameter as we leak the normal type parameters, because
		// the index might collide. For example,
		//   class Base<T> { class Nested<X> {} }
		//   class Derived<A, B> : Base<B> { }
		// 
		// Derived<string, int>.GetNestedTypes() = Base+Nested<int, UnboundTypeArgument>
		// Derived.GetNestedTypes() = Base+Nested<`1, >
		//  Here `1 refers to B, and there's no way to return X as it would collide with B.
		
		/// <summary>
		/// Gets inner classes (including inherited inner classes)
		/// that have <c>typeArguments.Count</c> additional type parameters.
		/// </summary>
		/// <param name="typeArguments">The type arguments passed to the inner class</param>
		/// <param name="context">The context used for resolving type references</param>
		/// <param name="filter">The filter used to select which types to return.
		/// The filter is tested on the original type definitions (before parameterization).</param>
		/// <remarks>
		/// Type parameters belonging to the outer class will have the value copied from the outer type
		/// if it is a parameterized type. Otherwise, those existing type parameters will be self-parameterized,
		/// and thus 'leaked' to the caller in the same way the GetMembers() method does not specialize members
		/// from an <see cref="ITypeDefinition"/> and 'leaks' type parameters in member signatures.
		/// </remarks>
		IEnumerable<IType> GetNestedTypes(IList<IType> typeArguments, ITypeResolveContext context, Predicate<ITypeDefinition> filter = null, GetMemberOptions options = GetMemberOptions.None);
		
		/// <summary>
		/// Gets all instance constructors for this type.
		/// </summary>
		/// <param name="context">The context used for resolving type references</param>
		/// <param name="filter">The filter used to select which constructors to return.
		/// The filter is tested on the original method definitions (before specialization).</param>
		/// <remarks>
		/// <para>The result does not include constructors in base classes or static constructors.</para>
		/// <para>
		/// For methods on parameterized types, type substitution will be performed on the method signature,
		/// and the appropriate <see cref="SpecializedMethod"/> will be returned.
		/// </para>
		/// </remarks>
		IEnumerable<IMethod> GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers);
		
		/// <summary>
		/// Gets all methods that can be called on this type.
		/// </summary>
		/// <param name="context">The context used for resolving type references</param>
		/// <param name="filter">The filter used to select which methods to return.
		/// The filter is tested on the original method definitions (before specialization).</param>
		/// <remarks>
		/// <para>
		/// The result does not include constructors.
		/// </para>
		/// <para>
		/// For methods on parameterized types, type substitution will be performed on the method signature,
		/// and the appropriate <see cref="SpecializedMethod"/> will be returned.
		/// </para>
		/// <para>
		/// If the method being returned is generic, and this type is a parameterized type where the type
		/// arguments involve another method's type parameters, the resulting specialized signature
		/// will be ambiguous as to which method a type parameter belongs to.
		/// For example, "List[[``0]].GetMethods()" will return "ConvertAll(Converter`2[[``0, ``0]])".
		/// 
		/// If possible, use the other GetMethods() overload to supply type arguments to the method,
		/// so that both class and method type parameter can be substituted at the same time, so that
		/// the ambiguity can be avoided.
		/// </para>
		/// </remarks>
		IEnumerable<IMethod> GetMethods(ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None);
		
		/// <summary>
		/// Gets all generic methods that can be called on this type with the specified type arguments.
		/// </summary>
		/// <param name="typeArguments">The type arguments used for the method call.</param>
		/// <param name="context">The context used for resolving type references</param>
		/// <param name="filter">The filter used to select which methods to return.
		/// The filter is tested on the original method definitions (before specialization).</param>
		/// <remarks>
		/// <para>The result does not include constructors.</para>
		/// <para>
		/// Type substitution will be performed on the method signature, creating a <see cref="SpecializedMethod"/>
		/// with the specified type arguments.
		/// </para>
		/// <para>
		/// When the list of type arguments is empty, this method acts like the GetMethods() overload without
		/// the type arguments parameter - that is, it also returns generic methods,
		/// and the other overload's remarks about ambiguous signatures apply here as well.
		/// </para>
		/// </remarks>
		IEnumerable<IMethod> GetMethods(IList<IType> typeArguments, ITypeResolveContext context, Predicate<IMethod> filter = null, GetMemberOptions options = GetMemberOptions.None);
		
		/// <summary>
		/// Gets all properties that can be called on this type.
		/// </summary>
		/// <param name="context">The context used for resolving type references</param>
		/// <param name="filter">The filter used to select which properties to return.
		/// The filter is tested on the original property definitions (before specialization).</param>
		/// <remarks>
		/// For properties on parameterized types, type substitution will be performed on the property signature,
		/// and the appropriate <see cref="SpecializedProperty"/> will be returned.
		/// </remarks>
		IEnumerable<IProperty> GetProperties(ITypeResolveContext context, Predicate<IProperty> filter = null, GetMemberOptions options = GetMemberOptions.None);
		
		/// <summary>
		/// Gets all fields that can be accessed on this type.
		/// </summary>
		/// <param name="context">The context used for resolving type references</param>
		/// <param name="filter">The filter used to select which constructors to return.
		/// The filter is tested on the original field definitions (before specialization).</param>
		/// <remarks>
		/// For fields on parameterized types, type substitution will be performed on the field's return type,
		/// and the appropriate <see cref="SpecializedField"/> will be returned.
		/// </remarks>
		IEnumerable<IField> GetFields(ITypeResolveContext context, Predicate<IField> filter = null, GetMemberOptions options = GetMemberOptions.None);
		
		/// <summary>
		/// Gets all events that can be accessed on this type.
		/// </summary>
		/// <param name="context">The context used for resolving type references</param>
		/// <param name="filter">The filter used to select which events to return.
		/// The filter is tested on the original event definitions (before specialization).</param>
		/// <remarks>
		/// For fields on parameterized types, type substitution will be performed on the event's return type,
		/// and the appropriate <see cref="SpecializedEvent"/> will be returned.
		/// </remarks>
		IEnumerable<IEvent> GetEvents(ITypeResolveContext context, Predicate<IEvent> filter = null, GetMemberOptions options = GetMemberOptions.None);
		
		/// <summary>
		/// Gets all members that can be called on this type.
		/// </summary>
		/// <param name="context">The context used for resolving type references</param>
		/// <param name="filter">The filter used to select which members to return.
		/// The filter is tested on the original member definitions (before specialization).</param>
		/// <remarks>
		/// <para>
		/// The resulting list is the union of GetFields(), GetProperties(), GetMethods() and GetEvents().
		/// It does not include constructors.
		/// For parameterized types, type substitution will be performed.
		/// </para>
		/// <para>
		/// For generic methods, the remarks about ambiguous signatures from the
		/// <see cref="GetMethods(ITypeResolveContext, Predicate{IMethod})"/> method apply here as well.
		/// </para>
		/// </remarks>
		IEnumerable<IMember> GetMembers(ITypeResolveContext context, Predicate<IMember> filter = null, GetMemberOptions options = GetMemberOptions.None);
	}
	
	[Flags]
	public enum GetMemberOptions
	{
		/// <summary>
		/// No options specified - this is the default.
		/// Members will be specialized, and inherited members will be included.
		/// </summary>
		None = 0x00,
		/// <summary>
		/// Do not specialize the returned members - directly return the definitions.
		/// </summary>
		ReturnMemberDefinitions = 0x01,
		/// <summary>
		/// Do not list inherited members - only list members defined directly on this type.
		/// </summary>
		IgnoreInheritedMembers = 0x02
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
		
		IEnumerable<IType> IType.GetNestedTypes(ITypeResolveContext context, Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IType>>() != null);
			return null;
		}

		IEnumerable<IMethod> IType.GetMethods(ITypeResolveContext context, Predicate<IMethod> filter, GetMemberOptions options)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IMethod>>() != null);
			return null;
		}
		
		IEnumerable<IMethod> IType.GetConstructors(ITypeResolveContext context, Predicate<IMethod> filter, GetMemberOptions options)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IMethod>>() != null);
			return null;
		}
		
		IEnumerable<IProperty> IType.GetProperties(ITypeResolveContext context, Predicate<IProperty> filter, GetMemberOptions options)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IProperty>>() != null);
			return null;
		}
		
		IEnumerable<IField> IType.GetFields(ITypeResolveContext context, Predicate<IField> filter, GetMemberOptions options)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IField>>() != null);
			return null;
		}
		
		IEnumerable<IEvent> IType.GetEvents(ITypeResolveContext context, Predicate<IEvent> filter, GetMemberOptions options)
		{
			Contract.Requires(context != null);
			Contract.Ensures(Contract.Result<IEnumerable<IEvent>>() != null);
			return null;
		}
		
		IEnumerable<IMember> IType.GetEvents(ITypeResolveContext context, Predicate<IMember> filter, GetMemberOptions options)
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
