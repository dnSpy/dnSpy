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
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Contains extension methods for the type system.
	/// </summary>
	public static class ExtensionMethods
	{
		#region GetAllBaseTypes
		/// <summary>
		/// Gets all base types.
		/// </summary>
		/// <remarks>This is the reflexive and transitive closure of <see cref="IType.DirectBaseTypes"/>.
		/// Note that this method does not return all supertypes - doing so is impossible due to contravariance
		/// (and undesirable for covariance as the list could become very large).
		/// 
		/// The output is ordered so that base types occur before derived types.
		/// </remarks>
		public static IEnumerable<IType> GetAllBaseTypes(this IType type)
		{
			BaseTypeCollector collector = new BaseTypeCollector();
			collector.CollectBaseTypes(type);
			return collector;
		}
		
		/// <summary>
		/// Gets all non-interface base types.
		/// </summary>
		/// <remarks>
		/// When <paramref name="type"/> is an interface, this method will also return base interfaces (return same output as GetAllBaseTypes()).
		/// 
		/// The output is ordered so that base types occur before derived types.
		/// </remarks>
		public static IEnumerable<IType> GetNonInterfaceBaseTypes(this IType type)
		{
			BaseTypeCollector collector = new BaseTypeCollector();
			collector.SkipImplementedInterfaces = true;
			collector.CollectBaseTypes(type);
			return collector;
		}
		#endregion
		
		#region GetAllBaseTypeDefinitions
		/// <summary>
		/// Gets all base type definitions.
		/// </summary>
		/// <remarks>
		/// This is equivalent to type.GetAllBaseTypes().Select(t => t.GetDefinition()).Where(d => d != null).Distinct().
		/// </remarks>
		public static IEnumerable<ITypeDefinition> GetAllBaseTypeDefinitions(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			
			HashSet<ITypeDefinition> typeDefinitions = new HashSet<ITypeDefinition>();
			Func<ITypeDefinition, IEnumerable<ITypeDefinition>> recursion =
				t => t.DirectBaseTypes.Select(b => b.GetDefinition()).Where(d => d != null && typeDefinitions.Add(d));
			
			ITypeDefinition typeDef = type.GetDefinition();
			if (typeDef != null) {
				typeDefinitions.Add(typeDef);
				return TreeTraversal.PreOrder(typeDef, recursion);
			} else {
				return TreeTraversal.PreOrder(
					type.DirectBaseTypes.Select(b => b.GetDefinition()).Where(d => d != null && typeDefinitions.Add(d)),
					recursion);
			}
		}
		
		/// <summary>
		/// Gets whether this type definition is derived from the base type defintiion.
		/// </summary>
		public static bool IsDerivedFrom(this ITypeDefinition type, ITypeDefinition baseType)
		{
			if (type.Compilation != baseType.Compilation) {
				throw new InvalidOperationException("Both arguments to IsDerivedFrom() must be from the same compilation.");
			}
			return GetAllBaseTypeDefinitions(type).Contains(baseType);
		}
		#endregion
		
		#region IsOpen / IsUnbound
		sealed class TypeClassificationVisitor : TypeVisitor
		{
			internal bool isOpen;
			
			public override IType VisitTypeParameter(ITypeParameter type)
			{
				isOpen = true;
				return base.VisitTypeParameter(type);
			}
		}
		
		/// <summary>
		/// Gets whether the type is an open type (contains type parameters).
		/// </summary>
		/// <example>
		/// <code>
		/// class X&lt;T&gt; {
		///   List&lt;T&gt; open;
		///   X&lt;X&lt;T[]&gt;&gt; open;
		///   X&lt;string&gt; closed;
		///   int closed;
		/// }
		/// </code>
		/// </example>
		public static bool IsOpen(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			TypeClassificationVisitor v = new TypeClassificationVisitor();
			type.AcceptVisitor(v);
			return v.isOpen;
		}
		
		/// <summary>
		/// Gets whether the type is unbound (is a generic type, but no type arguments were provided).
		/// </summary>
		/// <remarks>
		/// In "<c>typeof(List&lt;Dictionary&lt;,&gt;&gt;)</c>", only the Dictionary is unbound, the List is considered
		/// bound despite containing an unbound type.
		/// This method returns false for partially parameterized types (<c>Dictionary&lt;string, &gt;</c>).
		/// </remarks>
		public static bool IsUnbound(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			return type is ITypeDefinition && type.TypeParameterCount > 0;
		}
		#endregion
		
		#region Import
		/// <summary>
		/// Imports a type from another compilation.
		/// </summary>
		public static IType Import(this ICompilation compilation, IType type)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (type == null)
				return null;
			return type.ToTypeReference().Resolve(compilation.TypeResolveContext);
		}
		
		/// <summary>
		/// Imports a type from another compilation.
		/// </summary>
		public static ITypeDefinition Import(this ICompilation compilation, ITypeDefinition typeDefinition)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (typeDefinition == null)
				return null;
			if (typeDefinition.Compilation == compilation)
				return typeDefinition;
			return typeDefinition.ToTypeReference().Resolve(compilation.TypeResolveContext).GetDefinition();
		}
		
		/// <summary>
		/// Imports an entity from another compilation.
		/// </summary>
		public static IEntity Import(this ICompilation compilation, IEntity entity)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (entity == null)
				return null;
			if (entity.Compilation == compilation)
				return entity;
			if (entity is IMember)
				return ((IMember)entity).ToMemberReference().Resolve(compilation.TypeResolveContext);
			else if (entity is ITypeDefinition)
				return ((ITypeDefinition)entity).ToTypeReference().Resolve(compilation.TypeResolveContext).GetDefinition();
			else
				throw new NotSupportedException("Unknown entity type");
		}
		
		/// <summary>
		/// Imports a member from another compilation.
		/// </summary>
		public static IMember Import(this ICompilation compilation, IMember member)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (member == null)
				return null;
			if (member.Compilation == compilation)
				return member;
			return member.ToMemberReference().Resolve(compilation.TypeResolveContext);
		}
		
		/// <summary>
		/// Imports a member from another compilation.
		/// </summary>
		public static IMethod Import(this ICompilation compilation, IMethod method)
		{
			return (IMethod)compilation.Import((IMember)method);
		}
		
		/// <summary>
		/// Imports a member from another compilation.
		/// </summary>
		public static IField Import(this ICompilation compilation, IField field)
		{
			return (IField)compilation.Import((IMember)field);
		}
		
		/// <summary>
		/// Imports a member from another compilation.
		/// </summary>
		public static IEvent Import(this ICompilation compilation, IEvent ev)
		{
			return (IEvent)compilation.Import((IMember)ev);
		}
		
		/// <summary>
		/// Imports a member from another compilation.
		/// </summary>
		public static IProperty Import(this ICompilation compilation, IProperty property)
		{
			return (IProperty)compilation.Import((IMember)property);
		}
		#endregion
		
		#region GetDelegateInvokeMethod
		/// <summary>
		/// Gets the invoke method for a delegate type.
		/// </summary>
		/// <remarks>
		/// Returns null if the type is not a delegate type; or if the invoke method could not be found.
		/// </remarks>
		public static IMethod GetDelegateInvokeMethod(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			ITypeDefinition def = type.GetDefinition();
			if (def != null && def.Kind == TypeKind.Delegate) {
				foreach (IMember member in def.Members) {
					if (member.Name == "Invoke" && member is IMethod) {
						ParameterizedType pt = type as ParameterizedType;
						if (pt != null) {
							return new SpecializedMethod(pt, (IMethod)member);
						}
						return (IMethod)member;
					}
				}
			}
			return null;
		}
		#endregion
		
		#region GetType/Member
		/// <summary>
		/// Gets all unresolved type definitions from the file.
		/// For partial classes, each part is returned.
		/// </summary>
		public static IEnumerable<IUnresolvedTypeDefinition> GetAllTypeDefinitions (this IParsedFile file)
		{
			return TreeTraversal.PreOrder(file.TopLevelTypeDefinitions, t => t.NestedTypes);
		}
		
		/// <summary>
		/// Gets all unresolved type definitions from the assembly.
		/// For partial classes, each part is returned.
		/// </summary>
		public static IEnumerable<IUnresolvedTypeDefinition> GetAllTypeDefinitions (this IUnresolvedAssembly assembly)
		{
			return TreeTraversal.PreOrder(assembly.TopLevelTypeDefinitions, t => t.NestedTypes);
		}
		
		public static IEnumerable<ITypeDefinition> GetAllTypeDefinitions (this IAssembly assembly)
		{
			return TreeTraversal.PreOrder(assembly.TopLevelTypeDefinitions, t => t.NestedTypes);
		}
		
		/// <summary>
		/// Gets all type definitions in the compilation.
		/// This may include types from referenced assemblies that are not accessible in the main assembly.
		/// </summary>
		public static IEnumerable<ITypeDefinition> GetAllTypeDefinitions (this ICompilation compilation)
		{
			return compilation.MainAssembly.GetAllTypeDefinitions()
				.Concat(compilation.ReferencedAssemblies.SelectMany(a => a.GetAllTypeDefinitions()));
		}
		
		/// <summary>
		/// Gets the type (potentially a nested type) defined at the specified location.
		/// Returns null if no type is defined at that location.
		/// </summary>
		public static IUnresolvedTypeDefinition GetInnermostTypeDefinition (this IParsedFile file, int line, int column)
		{
			return file.GetInnermostTypeDefinition (new TextLocation (line, column));
		}
		
		/// <summary>
		/// Gets the member defined at the specified location.
		/// Returns null if no member is defined at that location.
		/// </summary>
		public static IUnresolvedMember GetMember (this IParsedFile file, int line, int column)
		{
			return file.GetMember (new TextLocation (line, column));
		}
		#endregion
		
		#region Resolve on collections
		public static IList<IAttribute> CreateResolvedAttributes(this IList<IUnresolvedAttribute> attributes, ITypeResolveContext context)
		{
			if (attributes == null)
				throw new ArgumentNullException("attributes");
			if (attributes.Count == 0)
				return EmptyList<IAttribute>.Instance;
			else
				return new ProjectedList<ITypeResolveContext, IUnresolvedAttribute, IAttribute>(context, attributes, (c, a) => a.CreateResolvedAttribute(c));
		}
		
		public static IList<ITypeParameter> CreateResolvedTypeParameters(this IList<IUnresolvedTypeParameter> typeParameters, ITypeResolveContext context)
		{
			if (typeParameters == null)
				throw new ArgumentNullException("typeParameters");
			if (typeParameters.Count == 0)
				return EmptyList<ITypeParameter>.Instance;
			else
				return new ProjectedList<ITypeResolveContext, IUnresolvedTypeParameter, ITypeParameter>(context, typeParameters, (c, a) => a.CreateResolvedTypeParameter(c));
		}
		
		public static IList<IParameter> CreateResolvedParameters(this IList<IUnresolvedParameter> parameters, ITypeResolveContext context)
		{
			if (parameters == null)
				throw new ArgumentNullException("parameters");
			if (parameters.Count == 0)
				return EmptyList<IParameter>.Instance;
			else
				return new ProjectedList<ITypeResolveContext, IUnresolvedParameter, IParameter>(context, parameters, (c, a) => a.CreateResolvedParameter(c));
		}
		
		public static IList<IType> Resolve(this IList<ITypeReference> typeReferences, ITypeResolveContext context)
		{
			if (typeReferences == null)
				throw new ArgumentNullException("typeReferences");
			if (typeReferences.Count == 0)
				return EmptyList<IType>.Instance;
			else
				return new ProjectedList<ITypeResolveContext, ITypeReference, IType>(context, typeReferences, (c, t) => t.Resolve(c));
		}
		
		// There is intentionally no Resolve() overload for IList<IMemberReference>: the resulting IList<Member> would
		// contains nulls when there are resolve errors.
		
		public static IList<ResolveResult> Resolve(this IList<IConstantValue> constantValues, ITypeResolveContext context)
		{
			if (constantValues == null)
				throw new ArgumentNullException("constantValues");
			if (constantValues.Count == 0)
				return EmptyList<ResolveResult>.Instance;
			else
				return new ProjectedList<ITypeResolveContext, IConstantValue, ResolveResult>(context, constantValues, (c, t) => t.Resolve(c));
		}
		#endregion
		
		#region GetSubTypeDefinitions
		public static IEnumerable<ITypeDefinition> GetSubTypeDefinitions (this IType baseType)
		{
			var def = baseType.GetDefinition ();
			if (def == null)
				return Enumerable.Empty<ITypeDefinition> ();
			return def.GetSubTypeDefinitions ();
		}
		
		/// <summary>
		/// Gets all sub type definitions defined in a context.
		/// </summary>
		public static IEnumerable<ITypeDefinition> GetSubTypeDefinitions (this ITypeDefinition baseType)
		{
			foreach (var contextType in baseType.Compilation.GetAllTypeDefinitions ()) {
				if (contextType.IsDerivedFrom (baseType))
					yield return contextType;
			}
		}
		#endregion
		
		#region IAssembly.GetTypeDefinition()
		/// <summary>
		/// Gets the type definition for the specified unresolved type.
		/// Returns null if the unresolved type does not belong to this assembly.
		/// </summary>
		public static ITypeDefinition GetTypeDefinition(this IAssembly assembly, IUnresolvedTypeDefinition unresolved)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");
			if (unresolved == null)
				return null;
			if (unresolved.DeclaringTypeDefinition != null) {
				ITypeDefinition parentType = GetTypeDefinition(assembly, unresolved.DeclaringTypeDefinition);
				if (parentType == null)
					return null;
				foreach (var nestedType in parentType.NestedTypes) {
					if (nestedType.Name == unresolved.Name && nestedType.TypeParameterCount == unresolved.TypeParameters.Count)
						return nestedType;
				}
				return null;
			} else {
				return assembly.GetTypeDefinition(unresolved.Namespace, unresolved.Name, unresolved.TypeParameters.Count);
			}
		}
		#endregion
	}
}
