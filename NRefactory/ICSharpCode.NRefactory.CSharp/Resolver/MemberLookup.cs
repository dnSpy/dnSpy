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
using System.Diagnostics;
using System.Linq;

using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Implementation of member lookup (C# 4.0 spec, §7.4).
	/// </summary>
	public class MemberLookup
	{
		#region Static helper methods
		/// <summary>
		/// Gets whether the member is considered to be invocable.
		/// </summary>
		public static bool IsInvocable(IMember member)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			// C# 4.0 spec, §7.4 member lookup
			if (member is IEvent || member is IMethod)
				return true;
			IType returnType = member.ReturnType;
			return returnType.Kind == TypeKind.Dynamic || returnType.Kind == TypeKind.Delegate;
		}
		#endregion
		
		readonly ITypeDefinition currentTypeDefinition;
		readonly IAssembly currentAssembly;
		readonly bool isInEnumMemberInitializer;
		
		public MemberLookup(ITypeDefinition currentTypeDefinition, IAssembly currentAssembly, bool isInEnumMemberInitializer = false)
		{
			this.currentTypeDefinition = currentTypeDefinition;
			this.currentAssembly = currentAssembly;
			this.isInEnumMemberInitializer = isInEnumMemberInitializer;
		}
		
		#region IsAccessible
		/// <summary>
		/// Gets whether access to protected instance members of the target expression is possible.
		/// </summary>
		public bool IsProtectedAccessAllowed(ResolveResult targetResolveResult)
		{
			return targetResolveResult is ThisResolveResult || IsProtectedAccessAllowed(targetResolveResult.Type);
		}
		
		/// <summary>
		/// Gets whether access to protected instance members of the target type is possible.
		/// </summary>
		/// <remarks>
		/// This method does not consider the special case of the 'base' reference. If possible, use the
		/// <c>IsProtectedAccessAllowed(ResolveResult)</c> overload instead.
		/// </remarks>
		public bool IsProtectedAccessAllowed(IType targetType)
		{
			if (targetType.Kind == TypeKind.TypeParameter)
				targetType = ((ITypeParameter)targetType).EffectiveBaseClass;
			ITypeDefinition typeDef = targetType.GetDefinition();
			if (typeDef == null)
				return false;
			for (ITypeDefinition c = currentTypeDefinition; c != null; c = c.DeclaringTypeDefinition) {
				if (typeDef.IsDerivedFrom(c))
					return true;
			}
			return false;
		}
		
		/// <summary>
		/// Gets whether <paramref name="entity"/> is accessible in the current class.
		/// </summary>
		/// <param name="entity">The entity to test</param>
		/// <param name="allowProtectedAccess">
		/// Whether protected access to instance members is allowed.
		/// True if the type of the reference is derived from the current class.
		/// Protected static members may be accessible even if false is passed for this parameter.
		/// </param>
		public bool IsAccessible(IEntity entity, bool allowProtectedAccess)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");
			// C# 4.0 spec, §3.5.2 Accessiblity domains
			switch (entity.Accessibility) {
				case Accessibility.None:
					return false;
				case Accessibility.Private:
					// check for members of outer classes (private members of outer classes can be accessed)
					for (var t = currentTypeDefinition; t != null; t = t.DeclaringTypeDefinition) {
						if (t.Equals(entity.DeclaringTypeDefinition))
							return true;
					}
					return false;
				case Accessibility.Public:
					return true;
				case Accessibility.Protected:
					return IsProtectedAccessible(allowProtectedAccess, entity);
				case Accessibility.Internal:
					return IsInternalAccessible(entity.ParentAssembly);
				case Accessibility.ProtectedOrInternal:
					return IsInternalAccessible(entity.ParentAssembly) || IsProtectedAccessible(allowProtectedAccess, entity);
				case Accessibility.ProtectedAndInternal:
					return IsInternalAccessible(entity.ParentAssembly) && IsProtectedAccessible(allowProtectedAccess, entity);
				default:
					throw new Exception("Invalid value for Accessibility");
			}
		}
		
		bool IsInternalAccessible(IAssembly assembly)
		{
			return assembly != null && currentAssembly != null && assembly.InternalsVisibleTo(currentAssembly);
		}
		
		bool IsProtectedAccessible(bool allowProtectedAccess, IEntity entity)
		{
			// For static members and type definitions, we do not require the qualifying reference
			// to be derived from the current class (allowProtectedAccess).
			if (entity.IsStatic || entity.SymbolKind == SymbolKind.TypeDefinition)
				allowProtectedAccess = true;
			
			for (var t = currentTypeDefinition; t != null; t = t.DeclaringTypeDefinition) {
				if (t.Equals(entity.DeclaringTypeDefinition))
					return true;
				
				// PERF: this might hurt performance as this method is called several times (once for each member)
				// make sure resolving base types is cheap (caches?) or cache within the MemberLookup instance
				if (allowProtectedAccess && t.IsDerivedFrom(entity.DeclaringTypeDefinition))
					return true;
			}
			return false;
		}
		#endregion
		
		#region GetAccessibleMembers
		/// <summary>
		/// Retrieves all members that are accessible and not hidden (by being overridden or shadowed).
		/// Returns both members and nested type definitions. Does not include extension methods.
		/// </summary>
		public IEnumerable<IEntity> GetAccessibleMembers(ResolveResult targetResolveResult)
		{
			if (targetResolveResult == null)
				throw new ArgumentNullException("targetResolveResult");
			
			bool targetIsTypeParameter = targetResolveResult.Type.Kind == TypeKind.TypeParameter;
			bool allowProtectedAccess = IsProtectedAccessAllowed(targetResolveResult);
			
			// maps the member name to the list of lookup groups
			var lookupGroupDict = new Dictionary<string, List<LookupGroup>>();
			
			// This loop will handle base types before derived types.
			// The loop performs three jobs:
			// 1) It marks entries in lookup groups from base classes as removed when those members
			//    are hidden by a derived class.
			// 2) It adds a new lookup group with the members from a declaring type.
			// 3) It replaces virtual members with the overridden version, placing the override in the
			//    lookup group belonging to the base class.
			foreach (IType type in targetResolveResult.Type.GetNonInterfaceBaseTypes()) {
				
				List<IEntity> entities = new List<IEntity>();
				entities.AddRange(type.GetMembers(options: GetMemberOptions.IgnoreInheritedMembers));
				if (!targetIsTypeParameter) {
					var nestedTypes = type.GetNestedTypes(options: GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions);
					// GetDefinition() might return null if some IType has a strange implementation of GetNestedTypes.
					entities.AddRange(nestedTypes.Select(t => t.GetDefinition()).Where(td => td != null));
				}
				
				foreach (var entityGroup in entities.GroupBy(e => e.Name)) {
					
					List<LookupGroup> lookupGroups = new List<LookupGroup>();
					if (!lookupGroupDict.TryGetValue(entityGroup.Key, out lookupGroups))
						lookupGroupDict.Add(entityGroup.Key, lookupGroups = new List<LookupGroup>());
					
					List<IType> newNestedTypes = null;
					List<IParameterizedMember> newMethods = null;
					IMember newNonMethod = null;
					
					IEnumerable<IType> typeBaseTypes = null;
					
					if (!targetIsTypeParameter) {
						AddNestedTypes(type, entityGroup.OfType<IType>(), 0, lookupGroups, ref typeBaseTypes, ref newNestedTypes);
					}
					AddMembers(type, entityGroup.OfType<IMember>(), allowProtectedAccess, lookupGroups, false, ref typeBaseTypes, ref newMethods, ref newNonMethod);
					
					if (newNestedTypes != null || newMethods != null || newNonMethod != null)
						lookupGroups.Add(new LookupGroup(type, newNestedTypes, newMethods, newNonMethod));
				}
			}
			
			foreach (List<LookupGroup> lookupGroups in lookupGroupDict.Values) {
				// Remove interface members hidden by class members.
				if (targetIsTypeParameter) {
					// This can happen only with type parameters.
					RemoveInterfaceMembersHiddenByClassMembers(lookupGroups);
				}
				
				// Now report the results:
				foreach (LookupGroup lookupGroup in lookupGroups) {
					if (!lookupGroup.MethodsAreHidden) {
						foreach (IMethod method in lookupGroup.Methods) {
							yield return method;
						}
					}
					if (!lookupGroup.NonMethodIsHidden) {
						yield return lookupGroup.NonMethod;
					}
					if (lookupGroup.NestedTypes != null) {
						foreach (IType type in lookupGroup.NestedTypes) {
							ITypeDefinition typeDef = type.GetDefinition();
							if (typeDef != null)
								yield return typeDef;
						}
					}
				}
			}
		}
		#endregion
		
		#region class LookupGroup
		sealed class LookupGroup
		{
			public readonly IType DeclaringType;
			
			// When a nested type is hidden, it is simply removed from the list.
			public List<IType> NestedTypes;
			
			// When members are hidden, they are merely marked as hidden.
			// We still need to store the hidden methods so that the 'override' processing can
			// find them, so that it won't introduce the override as a new method.
			public readonly List<IParameterizedMember> Methods;
			public bool MethodsAreHidden;
			
			public IMember NonMethod;
			public bool NonMethodIsHidden;
			
			public LookupGroup(IType declaringType, List<IType> nestedTypes, List<IParameterizedMember> methods, IMember nonMethod)
			{
				this.DeclaringType = declaringType;
				this.NestedTypes = nestedTypes;
				this.Methods = methods;
				this.NonMethod = nonMethod;
				this.MethodsAreHidden = (methods == null || methods.Count == 0);
				this.NonMethodIsHidden = (nonMethod == null);
			}
			
			public bool AllHidden {
				get {
					if (NestedTypes != null && NestedTypes.Count > 0)
						return false;
					return NonMethodIsHidden && MethodsAreHidden;
				}
			}
		}
		#endregion
		
		#region LookupType
		public ResolveResult LookupType(IType declaringType, string name, IList<IType> typeArguments, bool parameterizeResultType = true)
		{
			if (declaringType == null)
				throw new ArgumentNullException("declaringType");
			if (name == null)
				throw new ArgumentNullException("name");
			if (typeArguments == null)
				throw new ArgumentNullException("typeArguments");
			
			int typeArgumentCount = typeArguments.Count;
			Predicate<ITypeDefinition> filter = delegate (ITypeDefinition d) {
				return InnerTypeParameterCount(d) == typeArgumentCount && d.Name == name && IsAccessible(d, true);
			};
			
			List<LookupGroup> lookupGroups = new List<LookupGroup>();
			if (declaringType.Kind != TypeKind.TypeParameter) {
				foreach (IType type in declaringType.GetNonInterfaceBaseTypes()) {
					List<IType> newNestedTypes = null;
					IEnumerable<IType> typeBaseTypes = null;
					
					IEnumerable<IType> nestedTypes;
					if (parameterizeResultType) {
						nestedTypes = type.GetNestedTypes(typeArguments, filter, GetMemberOptions.IgnoreInheritedMembers);
					} else {
						nestedTypes = type.GetNestedTypes(filter, GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions);
					}
					AddNestedTypes(type, nestedTypes, typeArgumentCount, lookupGroups, ref typeBaseTypes, ref newNestedTypes);
					
					if (newNestedTypes != null)
						lookupGroups.Add(new LookupGroup(type, newNestedTypes, null, null));
				}
			}
			
			lookupGroups.RemoveAll(g => g.AllHidden);
			Debug.Assert(lookupGroups.All(g => g.NestedTypes != null && g.NestedTypes.Count > 0));
			
			if (lookupGroups.Count == 0) {
				return new UnknownMemberResolveResult(declaringType, name, typeArguments);
			}
			
			LookupGroup resultGroup = lookupGroups[lookupGroups.Count - 1];
			
			if (resultGroup.NestedTypes.Count > 1 || lookupGroups.Count > 1)
				return new AmbiguousTypeResolveResult(resultGroup.NestedTypes[0]);
			else
				return new TypeResolveResult(resultGroup.NestedTypes[0]);
		}
		
		static int InnerTypeParameterCount(IType type)
		{
			// inner types contain the type parameters of outer types. therefore this count has to been adjusted.
			return type.TypeParameterCount - (type.DeclaringType != null ? type.DeclaringType.TypeParameterCount : 0);
		}
		#endregion
		
		#region Lookup
		/// <summary>
		/// Performs a member lookup.
		/// </summary>
		public ResolveResult Lookup(ResolveResult targetResolveResult, string name, IList<IType> typeArguments, bool isInvocation)
		{
			if (targetResolveResult == null)
				throw new ArgumentNullException("targetResolveResult");
			if (name == null)
				throw new ArgumentNullException("name");
			if (typeArguments == null)
				throw new ArgumentNullException("typeArguments");
			
			bool targetIsTypeParameter = targetResolveResult.Type.Kind == TypeKind.TypeParameter;
			
			bool allowProtectedAccess = IsProtectedAccessAllowed(targetResolveResult);
			Predicate<ITypeDefinition> nestedTypeFilter = delegate(ITypeDefinition entity) {
				return entity.Name == name && IsAccessible(entity, allowProtectedAccess);
			};
			Predicate<IUnresolvedMember> memberFilter = delegate(IUnresolvedMember entity) {
				// NOTE: Atm destructors can be looked up with 'Finalize'
				return entity.SymbolKind != SymbolKind.Indexer &&
				       entity.SymbolKind != SymbolKind.Operator && 
				       entity.Name == name;
			};
			
			List<LookupGroup> lookupGroups = new List<LookupGroup>();
			// This loop will handle base types before derived types.
			// The loop performs three jobs:
			// 1) It marks entries in lookup groups from base classes as removed when those members
			//    are hidden by a derived class.
			// 2) It adds a new lookup group with the members from a declaring type.
			// 3) It replaces virtual members with the overridden version, placing the override in the
			//    lookup group belonging to the base class.
			foreach (IType type in targetResolveResult.Type.GetNonInterfaceBaseTypes()) {
				
				List<IType> newNestedTypes = null;
				List<IParameterizedMember> newMethods = null;
				IMember newNonMethod = null;
				
				IEnumerable<IType> typeBaseTypes = null;
				
				if (!isInvocation && !targetIsTypeParameter) {
					// Consider nested types only if it's not an invocation.
					// type.GetNestedTypes() is checking the type parameter count for an exact match,
					// so we don't need to do that in our filter.
					var nestedTypes = type.GetNestedTypes(typeArguments, nestedTypeFilter, GetMemberOptions.IgnoreInheritedMembers);
					AddNestedTypes(type, nestedTypes, typeArguments.Count, lookupGroups, ref typeBaseTypes, ref newNestedTypes);
				}
				
				IEnumerable<IMember> members;
				if (typeArguments.Count == 0) {
					// Note: IsInvocable-checking cannot be done as part of the filter;
					// because it must be done after type substitution.
					members = type.GetMembers(memberFilter, GetMemberOptions.IgnoreInheritedMembers);
					if (isInvocation)
						members = members.Where(m => IsInvocable(m));
				} else {
					// No need to check for isInvocation/isInvocable here:
					// we only fetch methods
					members = type.GetMethods(typeArguments, memberFilter, GetMemberOptions.IgnoreInheritedMembers);
				}
				AddMembers(type, members, allowProtectedAccess, lookupGroups, false, ref typeBaseTypes, ref newMethods, ref newNonMethod);
				
				if (newNestedTypes != null || newMethods != null || newNonMethod != null)
					lookupGroups.Add(new LookupGroup(type, newNestedTypes, newMethods, newNonMethod));
			}
			
			// Remove interface members hidden by class members.
			if (targetIsTypeParameter) {
				// This can happen only with type parameters.
				RemoveInterfaceMembersHiddenByClassMembers(lookupGroups);
			}
			
			return CreateResult(targetResolveResult, lookupGroups, name, typeArguments);
		}
		#endregion
		
		#region Lookup Indexer
		/// <summary>
		/// Looks up the indexers on the target type.
		/// </summary>
		public IList<MethodListWithDeclaringType> LookupIndexers(ResolveResult targetResolveResult)
		{
			if (targetResolveResult == null)
				throw new ArgumentNullException("targetResolveResult");
			
			IType targetType = targetResolveResult.Type;
			bool allowProtectedAccess = IsProtectedAccessAllowed(targetResolveResult);
			Predicate<IUnresolvedProperty> filter = p => p.IsIndexer;
			
			List<LookupGroup> lookupGroups = new List<LookupGroup>();
			foreach (IType type in targetType.GetNonInterfaceBaseTypes()) {
				List<IParameterizedMember> newMethods = null;
				IMember newNonMethod = null;
				
				IEnumerable<IType> typeBaseTypes = null;
				
				var members = type.GetProperties(filter, GetMemberOptions.IgnoreInheritedMembers);
				AddMembers(type, members, allowProtectedAccess, lookupGroups, true, ref typeBaseTypes, ref newMethods, ref newNonMethod);
				
				if (newMethods != null || newNonMethod != null)
					lookupGroups.Add(new LookupGroup(type, null, newMethods, newNonMethod));
			}
			
			// Remove interface members hidden by class members.
			if (targetType.Kind == TypeKind.TypeParameter) {
				// This can happen only with type parameters.
				RemoveInterfaceMembersHiddenByClassMembers(lookupGroups);
			}
			
			// Remove all hidden groups
			lookupGroups.RemoveAll(g => g.MethodsAreHidden || g.Methods.Count == 0);
			
			MethodListWithDeclaringType[] methodLists = new MethodListWithDeclaringType[lookupGroups.Count];
			for (int i = 0; i < methodLists.Length; i++) {
				methodLists[i] = new MethodListWithDeclaringType(lookupGroups[i].DeclaringType, lookupGroups[i].Methods);
			}
			return methodLists;
		}
		#endregion
		
		#region AddNestedTypes
		/// <summary>
		/// Adds the nested types to 'newNestedTypes' and removes any hidden members from the existing lookup groups.
		/// </summary>
		/// <param name="type">Declaring type of the nested types</param>
		/// <param name="nestedTypes">List of nested types to add.</param>
		/// <param name="typeArgumentCount">The number of type arguments - used for hiding types from the base class</param>
		/// <param name="lookupGroups">List of existing lookup groups</param>
		/// <param name="typeBaseTypes">The base types of 'type' (initialized on demand)</param>
		/// <param name="newNestedTypes">The target list (created on demand).</param>
		void AddNestedTypes(IType type, IEnumerable<IType> nestedTypes, int typeArgumentCount,
		                    List<LookupGroup> lookupGroups,
		                    ref IEnumerable<IType> typeBaseTypes,
		                    ref List<IType> newNestedTypes)
		{
			foreach (IType nestedType in nestedTypes) {
				// Remove all non-types declared in a base type of 'type',
				// and all types with same number of type parameters declared in a base type of 'type'.
				foreach (var lookupGroup in lookupGroups) {
					if (lookupGroup.AllHidden)
						continue; // everything is already hidden
					if (typeBaseTypes == null)
						typeBaseTypes = type.GetNonInterfaceBaseTypes();
					
					if (typeBaseTypes.Contains(lookupGroup.DeclaringType)) {
						lookupGroup.MethodsAreHidden = true;
						lookupGroup.NonMethodIsHidden = true;
						if (lookupGroup.NestedTypes != null)
							lookupGroup.NestedTypes.RemoveAll(t => InnerTypeParameterCount(t) == typeArgumentCount);
					}
				}
				
				// Add the new nested type.
				if (newNestedTypes == null)
					newNestedTypes = new List<IType>();
				newNestedTypes.Add(nestedType);
			}
		}
		#endregion
		
		#region AddMembers
		/// <summary>
		/// Adds members to 'newMethods'/'newNonMethod'.
		/// Removes any members in the existing lookup groups that were hidden by added members.
		/// Substitutes 'virtual' members in the existing lookup groups for added 'override' members.
		/// </summary>
		/// <param name="type">Declaring type of the members</param>
		/// <param name="members">List of members to add.</param>
		/// <param name="allowProtectedAccess">Whether protected members are accessible</param>
		/// <param name="lookupGroups">List of existing lookup groups</param>
		/// <param name="treatAllParameterizedMembersAsMethods">Whether to treat properties as methods</param>
		/// <param name="typeBaseTypes">The base types of 'type' (initialized on demand)</param>
		/// <param name="newMethods">The target list for methods (created on demand).</param>
		/// <param name="newNonMethod">The target variable for non-method members.</param>
		void AddMembers(IType type, IEnumerable<IMember> members,
		                bool allowProtectedAccess,
		                List<LookupGroup> lookupGroups,
		                bool treatAllParameterizedMembersAsMethods,
		                ref IEnumerable<IType> typeBaseTypes, ref List<IParameterizedMember> newMethods, ref IMember newNonMethod)
		{
			foreach (IMember member in members) {
				if (!IsAccessible(member, allowProtectedAccess))
					continue;
				
				IParameterizedMember method;
				if (treatAllParameterizedMembersAsMethods)
					method = member as IParameterizedMember;
				else
					method = member as IMethod;
				
				bool replacedVirtualMemberWithOverride = false;
				if (member.IsOverride) {
					// Replacing virtual member with override:
					
					// Go backwards so that we find the corresponding virtual member
					// in the most-derived type
					for (int i = lookupGroups.Count - 1; i >= 0 && !replacedVirtualMemberWithOverride; i--) {
						if (typeBaseTypes == null)
							typeBaseTypes = type.GetNonInterfaceBaseTypes();
						
						var lookupGroup = lookupGroups[i];
						if (typeBaseTypes.Contains(lookupGroup.DeclaringType)) {
							if (method != null) {
								// Find the matching method, and replace it with the override
								for (int j = 0; j < lookupGroup.Methods.Count; j++) {
									if (SignatureComparer.Ordinal.Equals(method, lookupGroup.Methods[j])) {
										lookupGroup.Methods[j] = method;
										replacedVirtualMemberWithOverride = true;
										break;
									}
								}
							} else {
								// If the member type matches, replace it with the override
								if (lookupGroup.NonMethod != null && lookupGroup.NonMethod.SymbolKind == member.SymbolKind) {
									lookupGroup.NonMethod = member;
									replacedVirtualMemberWithOverride = true;
									break;
								}
							}
						}
					}
				}
				// If the member wasn't an override, or if we didn't find any matching virtual method,
				// proceed to add the member.
				if (!replacedVirtualMemberWithOverride) {
					// Make the member hide other members:
					foreach (var lookupGroup in lookupGroups) {
						if (lookupGroup.AllHidden)
							continue; // everything is already hidden
						if (typeBaseTypes == null)
							typeBaseTypes = type.GetNonInterfaceBaseTypes();
						
						if (typeBaseTypes.Contains(lookupGroup.DeclaringType)) {
							// Methods hide all non-methods; Non-methods hide everything
							lookupGroup.NestedTypes = null;
							lookupGroup.NonMethodIsHidden = true;
							if (method == null) { // !(member is IMethod)
								lookupGroup.MethodsAreHidden = true;
							}
						}
					}
					
					// Add the new member
					if (method != null) {
						if (newMethods == null)
							newMethods = new List<IParameterizedMember>();
						newMethods.Add(method);
					} else {
						newNonMethod = member;
					}
				}
			}
		}
		#endregion
		
		#region RemoveInterfaceMembersHiddenByClassMembers
		void RemoveInterfaceMembersHiddenByClassMembers(List<LookupGroup> lookupGroups)
		{
			foreach (var classLookupGroup in lookupGroups) {
				if (IsInterfaceOrSystemObject(classLookupGroup.DeclaringType))
					continue;
				// The current lookup groups contains class members that might hide interface members
				bool hasNestedTypes = classLookupGroup.NestedTypes != null && classLookupGroup.NestedTypes.Count > 0;
				if (hasNestedTypes || !classLookupGroup.NonMethodIsHidden) {
					// Hide all members from interface types
					foreach (var interfaceLookupGroup in lookupGroups) {
						if (IsInterfaceOrSystemObject(interfaceLookupGroup.DeclaringType)) {
							interfaceLookupGroup.NestedTypes = null;
							interfaceLookupGroup.NonMethodIsHidden = true;
							interfaceLookupGroup.MethodsAreHidden = true;
						}
					}
				} else if (!classLookupGroup.MethodsAreHidden) {
					foreach (var classMethod in classLookupGroup.Methods) {
						// Hide all non-methods from interface types, and all methods with the same signature
						// as a method in this class type.
						foreach (var interfaceLookupGroup in lookupGroups) {
							if (IsInterfaceOrSystemObject(interfaceLookupGroup.DeclaringType)) {
								interfaceLookupGroup.NestedTypes = null;
								interfaceLookupGroup.NonMethodIsHidden = true;
								if (interfaceLookupGroup.Methods != null && !interfaceLookupGroup.MethodsAreHidden) {
									// The mapping of virtual to overridden methods is already done,
									// so we can simply remove the methods from the collection
									interfaceLookupGroup.Methods.RemoveAll(
										m => SignatureComparer.Ordinal.Equals(classMethod, m));
								}
							}
						}
					}
				}
			}
		}
		
		static bool IsInterfaceOrSystemObject(IType type)
		{
			// return true if type is an interface or System.Object
			if (type.Kind == TypeKind.Interface)
				return true;
			ITypeDefinition d = type.GetDefinition();
			return d != null && d.KnownTypeCode == KnownTypeCode.Object;
		}
		#endregion
		
		#region CreateResult
		ResolveResult CreateResult(ResolveResult targetResolveResult, List<LookupGroup> lookupGroups, string name, IList<IType> typeArguments)
		{
			// Remove all hidden groups
			lookupGroups.RemoveAll(g => g.AllHidden);
			
			if (lookupGroups.Count == 0) {
				// No members found
				return new UnknownMemberResolveResult(targetResolveResult.Type, name, typeArguments);
			}
			
			if (lookupGroups.Any(g => !g.MethodsAreHidden && g.Methods.Count > 0)) {
				// If there are methods, make a MethodGroupResolveResult.
				// Note that a conflict between a member and a method (possible with multiple interface inheritance)
				// is only a warning, not an error, and the C# compiler will prefer the method group.
				List<MethodListWithDeclaringType> methodLists = new List<MethodListWithDeclaringType>();
				foreach (var lookupGroup in lookupGroups) {
					if (!lookupGroup.MethodsAreHidden && lookupGroup.Methods.Count > 0) {
						var methodListWithDeclType = new MethodListWithDeclaringType(lookupGroup.DeclaringType);
						foreach (var method in lookupGroup.Methods) {
							methodListWithDeclType.Add((IMethod)method);
						}
						methodLists.Add(methodListWithDeclType);
					}
				}
				
				return new MethodGroupResolveResult(targetResolveResult, name, methodLists, typeArguments);
			}
			
			// If there are ambiguities, report the most-derived result (last group)
			LookupGroup resultGroup = lookupGroups[lookupGroups.Count - 1];
			if (resultGroup.NestedTypes != null && resultGroup.NestedTypes.Count > 0) {
				if (resultGroup.NestedTypes.Count > 1 || !resultGroup.NonMethodIsHidden || lookupGroups.Count > 1)
					return new AmbiguousTypeResolveResult(resultGroup.NestedTypes[0]);
				else
					return new TypeResolveResult(resultGroup.NestedTypes[0]);
			}
			
			if (resultGroup.NonMethod.IsStatic && targetResolveResult is ThisResolveResult) {
				targetResolveResult = new TypeResolveResult(targetResolveResult.Type);
			}
			
			if (lookupGroups.Count > 1) {
				return new AmbiguousMemberResolveResult(targetResolveResult, resultGroup.NonMethod);
			} else {
				if (isInEnumMemberInitializer) {
					IField field = resultGroup.NonMethod as IField;
					if (field != null && field.DeclaringTypeDefinition != null && field.DeclaringTypeDefinition.Kind == TypeKind.Enum) {
						return new MemberResolveResult(
							targetResolveResult, field,
							field.DeclaringTypeDefinition.EnumUnderlyingType,
							field.IsConst, field.ConstantValue);
					}
				}
				return new MemberResolveResult(targetResolveResult, resultGroup.NonMethod);
			}
		}
		#endregion
	}
}
