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
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Contains extension methods for the type system.
	/// </summary>
	public static class TypeSystemExtensions
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
			if (type == null)
				throw new ArgumentNullException("type");
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
			if (type == null)
				throw new ArgumentNullException("type");
			BaseTypeCollector collector = new BaseTypeCollector();
			collector.SkipImplementedInterfaces = true;
			collector.CollectBaseTypes(type);
			return collector;
		}
		#endregion
		
		#region GetAllBaseTypeDefinitions
		/// <summary>
		/// Gets all base type definitions.
		/// The output is ordered so that base types occur before derived types.
		/// </summary>
		/// <remarks>
		/// This is equivalent to type.GetAllBaseTypes().Select(t => t.GetDefinition()).Where(d => d != null).Distinct().
		/// </remarks>
		public static IEnumerable<ITypeDefinition> GetAllBaseTypeDefinitions(this IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			
			return type.GetAllBaseTypes().Select(t => t.GetDefinition()).Where(d => d != null).Distinct();
		}
		
		/// <summary>
		/// Gets whether this type definition is derived from the base type definition.
		/// </summary>
		public static bool IsDerivedFrom(this ITypeDefinition type, ITypeDefinition baseType)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (baseType == null)
				return false;
			if (type.Compilation != baseType.Compilation) {
				throw new InvalidOperationException("Both arguments to IsDerivedFrom() must be from the same compilation.");
			}
			return type.GetAllBaseTypeDefinitions().Contains(baseType);
		}
		
		/// <summary>
		/// Gets whether this type definition is derived from a given known type.
		/// </summary>
		public static bool IsDerivedFrom(this ITypeDefinition type, KnownTypeCode baseType)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (baseType == KnownTypeCode.None)
				return false;
			return IsDerivedFrom(type, type.Compilation.FindType(baseType).GetDefinition());
		}
		#endregion
		
		#region IsOpen / IsUnbound / IsKnownType
		sealed class TypeClassificationVisitor : TypeVisitor
		{
			internal bool isOpen;
			internal IEntity typeParameterOwner;
			int typeParameterOwnerNestingLevel;
			
			public override IType VisitTypeParameter(ITypeParameter type)
			{
				isOpen = true;
				// If both classes and methods, or different classes (nested types)
				// are involved, find the most specific one
				int newNestingLevel = GetNestingLevel(type.Owner);
				if (newNestingLevel > typeParameterOwnerNestingLevel) {
					typeParameterOwner = type.Owner;
					typeParameterOwnerNestingLevel = newNestingLevel;
				}
				return base.VisitTypeParameter(type);
			}
			
			static int GetNestingLevel(IEntity entity)
			{
				int level = 0;
				while (entity != null) {
					level++;
					entity = entity.DeclaringTypeDefinition;
				}
				return level;
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
		/// Gets the entity that owns the type parameters occurring in the specified type.
		/// If both class and method type parameters are present, the method is returned.
		/// Returns null if the specified type is closed.
		/// </summary>
		/// <seealso cref="IsOpen"/>
		static IEntity GetTypeParameterOwner(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			TypeClassificationVisitor v = new TypeClassificationVisitor();
			type.AcceptVisitor(v);
			return v.typeParameterOwner;
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
		
		/// <summary>
		/// Gets whether the type is the specified known type.
		/// For generic known types, this returns true any parameterization of the type (and also for the definition itself).
		/// </summary>
		public static bool IsKnownType(this IType type, KnownTypeCode knownType)
		{
			var def = type.GetDefinition();
			return def != null && def.KnownTypeCode == knownType;
		}
		#endregion
		
		#region Import
		/// <summary>
		/// Imports a symbol from another compilation.
		/// </summary>
		public static ISymbol Import(this ICompilation compilation, ISymbol symbol)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (symbol == null)
				return null;
			switch (symbol.SymbolKind) {
				case SymbolKind.TypeParameter:
					return (ITypeParameter)Import(compilation, (IType)symbol);
				case SymbolKind.Variable:
					IVariable v = (IVariable)symbol;
					return new DefaultVariable(
						Import(compilation, v.Type),
						v.Name, v.Region, v.IsConst, v.ConstantValue
					);
				case SymbolKind.Parameter:
					IParameter p = (IParameter)symbol;
					if (p.Owner != null) {
						int index = p.Owner.Parameters.IndexOf(p);
						var owner = (IParameterizedMember)Import(compilation, p.Owner);
						if (owner == null || index < 0 || index >= owner.Parameters.Count)
							return null;
						return owner.Parameters[index];
					} else {
						return new DefaultParameter(
							Import(compilation, p.Type),
							p.Name, null, p.Region,
							null, p.IsRef, p.IsOut, p.IsParams
						);
					}
				case SymbolKind.Namespace:
					return Import(compilation, (INamespace)symbol);
				default:
					if (symbol is IEntity)
						return Import(compilation, (IEntity)symbol);
					throw new NotSupportedException("Unsupported symbol kind: " + symbol.SymbolKind);
			}
		}
		
		/// <summary>
		/// Imports a type from another compilation.
		/// </summary>
		public static IType Import(this ICompilation compilation, IType type)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (type == null)
				return null;
			var compilationProvider = type as ICompilationProvider;
			if (compilationProvider != null && compilationProvider.Compilation == compilation)
				return type;
			IEntity typeParameterOwner = GetTypeParameterOwner(type);
			IEntity importedTypeParameterOwner = compilation.Import(typeParameterOwner);
			if (importedTypeParameterOwner != null) {
				return type.ToTypeReference().Resolve(new SimpleTypeResolveContext(importedTypeParameterOwner));
			} else {
				return type.ToTypeReference().Resolve(compilation.TypeResolveContext);
			}
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
				return ((IMember)entity).ToReference().Resolve(compilation.TypeResolveContext);
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
			return member.ToReference().Resolve(compilation.TypeResolveContext);
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
		
		/// <summary>
		/// Imports a namespace from another compilation.
		/// </summary>
		/// <remarks>
		/// This method may return null if the namespace does not exist in the target compilation.
		/// </remarks>
		public static INamespace Import(this ICompilation compilation, INamespace ns)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			if (ns == null)
				return null;
			if (ns.ParentNamespace == null) {
				// root namespace
				return compilation.GetNamespaceForExternAlias(ns.ExternAlias);
			} else {
				INamespace parent = Import(compilation, ns.ParentNamespace);
				if (parent != null)
					return parent.GetChildNamespace(ns.Name);
				else
					return null;
			}
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
			if (type.Kind == TypeKind.Delegate)
				return type.GetMethods(m => m.Name == "Invoke", GetMemberOptions.IgnoreInheritedMembers).FirstOrDefault();
			else
				return null;
		}
		#endregion
		
		#region GetType/Member
		/// <summary>
		/// Gets all unresolved type definitions from the file.
		/// For partial classes, each part is returned.
		/// </summary>
		public static IEnumerable<IUnresolvedTypeDefinition> GetAllTypeDefinitions (this IUnresolvedFile file)
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
			return compilation.Assemblies.SelectMany(a => a.GetAllTypeDefinitions());
		}

		/// <summary>
		/// Gets all top level type definitions in the compilation.
		/// This may include types from referenced assemblies that are not accessible in the main assembly.
		/// </summary>
		public static IEnumerable<ITypeDefinition> GetTopLevelTypeDefinitons (this ICompilation compilation)
		{
			return compilation.Assemblies.SelectMany(a => a.TopLevelTypeDefinitions);
		}
		
		/// <summary>
		/// Gets the type (potentially a nested type) defined at the specified location.
		/// Returns null if no type is defined at that location.
		/// </summary>
		public static IUnresolvedTypeDefinition GetInnermostTypeDefinition (this IUnresolvedFile file, int line, int column)
		{
			return file.GetInnermostTypeDefinition (new TextLocation (line, column));
		}
		
		/// <summary>
		/// Gets the member defined at the specified location.
		/// Returns null if no member is defined at that location.
		/// </summary>
		public static IUnresolvedMember GetMember (this IUnresolvedFile file, int line, int column)
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
			if (baseType == null)
				throw new ArgumentNullException ("baseType");
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
			if (baseType == null)
				throw new ArgumentNullException ("baseType");
			foreach (var contextType in baseType.Compilation.GetAllTypeDefinitions ()) {
				if (contextType.IsDerivedFrom (baseType))
					yield return contextType;
			}
		}
		#endregion
		
		#region IAssembly.GetTypeDefinition()
		/// <summary>
		/// Retrieves the specified type in this compilation.
		/// Returns an <see cref="UnknownType"/> if the type cannot be found in this compilation.
		/// </summary>
		/// <remarks>
		/// There can be multiple types with the same full name in a compilation, as a
		/// full type name is only unique per assembly.
		/// If there are multiple possible matches, this method will return just one of them.
		/// When possible, use <see cref="IAssembly.GetTypeDefinition"/> instead to
		/// retrieve a type from a specific assembly.
		/// </remarks>
		public static IType FindType(this ICompilation compilation, FullTypeName fullTypeName)
		{
			if (compilation == null)
				throw new ArgumentNullException("compilation");
			foreach (IAssembly asm in compilation.Assemblies) {
				ITypeDefinition def = asm.GetTypeDefinition(fullTypeName);
				if (def != null)
					return def;
			}
			return new UnknownType(fullTypeName);
		}
		
		/// <summary>
		/// Gets the type definition for the specified unresolved type.
		/// Returns null if the unresolved type does not belong to this assembly.
		/// </summary>
		public static ITypeDefinition GetTypeDefinition(this IAssembly assembly, FullTypeName fullTypeName)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");
			TopLevelTypeName topLevelTypeName = fullTypeName.TopLevelTypeName;
			ITypeDefinition typeDef = assembly.GetTypeDefinition(topLevelTypeName);
			if (typeDef == null)
				return null;
			int typeParameterCount = topLevelTypeName.TypeParameterCount;
			for (int i = 0; i < fullTypeName.NestingLevel; i++) {
				string name = fullTypeName.GetNestedTypeName(i);
				typeParameterCount += fullTypeName.GetNestedTypeAdditionalTypeParameterCount(i);
				typeDef = FindNestedType(typeDef, name, typeParameterCount);
				if (typeDef == null)
					break;
			}
			return typeDef;
		}
		
		static ITypeDefinition FindNestedType(ITypeDefinition typeDef, string name, int typeParameterCount)
		{
			foreach (var nestedType in typeDef.NestedTypes) {
				if (nestedType.Name == name && nestedType.TypeParameterCount == typeParameterCount)
					return nestedType;
			}
			return null;
		}
		#endregion

		#region ITypeReference.Resolve(ICompilation)

		/// <summary>
		/// Resolves a type reference in the compilation's main type resolve context.
		/// Some type references require a more specific type resolve context and will not resolve using this method.
		/// </summary>
		/// <returns>
		/// Returns the resolved type.
		/// In case of an error, returns <see cref="SpecialType.UnknownType"/>.
		/// Never returns null.
		/// </returns>
		public static IType Resolve (this ITypeReference reference, ICompilation compilation)
		{
			if (reference == null)
				throw new ArgumentNullException ("reference");
			if (compilation == null)
				throw new ArgumentNullException ("compilation");
			return reference.Resolve (compilation.TypeResolveContext);
		}
		#endregion
		
		#region ITypeDefinition.GetAttribute
		/// <summary>
		/// Gets the attribute of the specified attribute type (or derived attribute types).
		/// </summary>
		/// <param name="entity">The entity on which the attributes are declared.</param>
		/// <param name="attributeType">The attribute type to look for.</param>
		/// <param name="inherit">
		/// Specifies whether attributes inherited from base classes and base members (if the given <paramref name="entity"/> in an <c>override</c>)
		/// should be returned. The default is <c>true</c>.
		/// </param>
		/// <returns>
		/// Returns the attribute that was found; or <c>null</c> if none was found.
		/// If inherit is true, an from the entity itself will be returned if possible;
		/// and the base entity will only be searched if none exists.
		/// </returns>
		public static IAttribute GetAttribute(this IEntity entity, IType attributeType, bool inherit = true)
		{
			return GetAttributes(entity, attributeType, inherit).FirstOrDefault();
		}
		
		/// <summary>
		/// Gets the attributes of the specified attribute type (or derived attribute types).
		/// </summary>
		/// <param name="entity">The entity on which the attributes are declared.</param>
		/// <param name="attributeType">The attribute type to look for.</param>
		/// <param name="inherit">
		/// Specifies whether attributes inherited from base classes and base members (if the given <paramref name="entity"/> in an <c>override</c>)
		/// should be returned. The default is <c>true</c>.
		/// </param>
		/// <returns>
		/// Returns the list of attributes that were found.
		/// If inherit is true, attributes from the entity itself are returned first; followed by attributes inherited from the base entity.
		/// </returns>
		public static IEnumerable<IAttribute> GetAttributes(this IEntity entity, IType attributeType, bool inherit = true)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");
			if (attributeType == null)
				throw new ArgumentNullException("attributeType");
			return GetAttributes(entity, attributeType.Equals, inherit);
		}
		
		/// <summary>
		/// Gets the attribute of the specified attribute type (or derived attribute types).
		/// </summary>
		/// <param name="entity">The entity on which the attributes are declared.</param>
		/// <param name="attributeType">The attribute type to look for.</param>
		/// <param name="inherit">
		/// Specifies whether attributes inherited from base classes and base members (if the given <paramref name="entity"/> in an <c>override</c>)
		/// should be returned. The default is <c>true</c>.
		/// </param>
		/// <returns>
		/// Returns the attribute that was found; or <c>null</c> if none was found.
		/// If inherit is true, an from the entity itself will be returned if possible;
		/// and the base entity will only be searched if none exists.
		/// </returns>
		public static IAttribute GetAttribute(this IEntity entity, FullTypeName attributeType, bool inherit = true)
		{
			return GetAttributes(entity, attributeType, inherit).FirstOrDefault();
		}
		
		/// <summary>
		/// Gets the attributes of the specified attribute type (or derived attribute types).
		/// </summary>
		/// <param name="entity">The entity on which the attributes are declared.</param>
		/// <param name="attributeType">The attribute type to look for.</param>
		/// <param name="inherit">
		/// Specifies whether attributes inherited from base classes and base members (if the given <paramref name="entity"/> in an <c>override</c>)
		/// should be returned. The default is <c>true</c>.
		/// </param>
		/// <returns>
		/// Returns the list of attributes that were found.
		/// If inherit is true, attributes from the entity itself are returned first; followed by attributes inherited from the base entity.
		/// </returns>
		public static IEnumerable<IAttribute> GetAttributes(this IEntity entity, FullTypeName attributeType, bool inherit = true)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");
			return GetAttributes(entity, attrType => {
			                     	ITypeDefinition typeDef = attrType.GetDefinition();
			                     	return typeDef != null && typeDef.FullTypeName == attributeType;
			                     }, inherit);
		}

		/// <summary>
		/// Gets the attribute of the specified attribute type (or derived attribute types).
		/// </summary>
		/// <param name="entity">The entity on which the attributes are declared.</param>
		/// <param name="inherit">
		/// Specifies whether attributes inherited from base classes and base members (if the given <paramref name="entity"/> in an <c>override</c>)
		/// should be returned. The default is <c>true</c>.
		/// </param>
		/// <returns>
		/// Returns the attribute that was found; or <c>null</c> if none was found.
		/// If inherit is true, an from the entity itself will be returned if possible;
		/// and the base entity will only be searched if none exists.
		/// </returns>
		public static IEnumerable<IAttribute> GetAttributes(this IEntity entity, bool inherit = true)
		{
			if (entity == null)
				throw new ArgumentNullException ("entity");
			return GetAttributes(entity, a => true, inherit);
		}
		
		static IEnumerable<IAttribute> GetAttributes(IEntity entity, Predicate<IType> attributeTypePredicate, bool inherit)
		{
			if (!inherit) {
				foreach (var attr in entity.Attributes) {
					if (attributeTypePredicate(attr.AttributeType))
						yield return attr;
				}
				yield break;
			}
			ITypeDefinition typeDef = entity as ITypeDefinition;
			if (typeDef != null) {
				foreach (var baseType in typeDef.GetNonInterfaceBaseTypes().Reverse()) {
					ITypeDefinition baseTypeDef = baseType.GetDefinition();
					if (baseTypeDef == null)
						continue;
					foreach (var attr in baseTypeDef.Attributes) {
						if (attributeTypePredicate(attr.AttributeType))
							yield return attr;
					}
				}
				yield break;
			}
			IMember member = entity as IMember;
			if (member != null) {
				HashSet<IMember> visitedMembers = new HashSet<IMember>();
				do {
					member = member.MemberDefinition; // it's sufficient to look at the definitions
					if (!visitedMembers.Add(member)) {
						// abort if we seem to be in an infinite loop (cyclic inheritance)
						break;
					}
					foreach (var attr in member.Attributes) {
						if (attributeTypePredicate(attr.AttributeType))
							yield return attr;
					}
				} while (member.IsOverride && (member = InheritanceHelper.GetBaseMember(member)) != null);
				yield break;
			}
			throw new NotSupportedException("Unknown entity type");
		}
		#endregion
		
		#region IAssembly.GetTypeDefinition(string,string,int)
		/// <summary>
		/// Gets the type definition for a top-level type.
		/// </summary>
		/// <remarks>This method uses ordinal name comparison, not the compilation's name comparer.</remarks>
		public static ITypeDefinition GetTypeDefinition(this IAssembly assembly, string namespaceName, string name, int typeParameterCount = 0)
		{
			if (assembly == null)
				throw new ArgumentNullException ("assembly");
			return assembly.GetTypeDefinition (new TopLevelTypeName (namespaceName, name, typeParameterCount));
		}
		#endregion
		
		#region ResolveResult
		public static ISymbol GetSymbol(this ResolveResult rr)
		{
			if (rr is LocalResolveResult) {
				return ((LocalResolveResult)rr).Variable;
			} else if (rr is MemberResolveResult) {
				return ((MemberResolveResult)rr).Member;
			} else if (rr is TypeResolveResult) {
				return ((TypeResolveResult)rr).Type.GetDefinition();
			}
			
			return null;
		}
		#endregion
	}
}
