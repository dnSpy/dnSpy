// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		public static bool IsInvocable(IMember member, ITypeResolveContext context)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			// C# 4.0 spec, §7.4 member lookup
			if (member is IEvent || member is IMethod)
				return true;
			IType returnType = member.ReturnType.Resolve(context);
			if (returnType == SharedTypes.Dynamic)
				return true;
			return returnType.IsDelegate();
		}
		#endregion
		
		ITypeResolveContext context;
		ITypeDefinition currentTypeDefinition;
		IProjectContent currentProject;
		
		public MemberLookup(ITypeResolveContext context, ITypeDefinition currentTypeDefinition, IProjectContent currentProject)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			this.context = context;
			this.currentTypeDefinition = currentTypeDefinition;
			this.currentProject = currentProject;
		}
		
		#region IsAccessible
		public bool IsProtectedAccessAllowed(IType targetType)
		{
			ITypeDefinition typeDef = targetType.GetDefinition();
			return typeDef != null && typeDef.IsDerivedFrom(currentTypeDefinition, context);
		}
		
		/// <summary>
		/// Gets whether <paramref name="entity"/> is accessible in the current class.
		/// </summary>
		/// <param name="entity">The entity to test</param>
		/// <param name="allowProtectedAccess">Whether protected access is allowed.
		/// True if the type of the reference is derived from the current class.</param>
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
					var lookupTypeDefinition = currentTypeDefinition;
					while (lookupTypeDefinition != null) {
						if (entity.DeclaringTypeDefinition.Equals (lookupTypeDefinition)) 
							return true;
						lookupTypeDefinition = lookupTypeDefinition.DeclaringTypeDefinition;
					}
					return false;
				case Accessibility.Public:
					return true;
				case Accessibility.Protected:
					return allowProtectedAccess && IsProtectedAccessible(entity.DeclaringTypeDefinition);
				case Accessibility.Internal:
					return IsInternalAccessible(entity.ProjectContent);
				case Accessibility.ProtectedOrInternal:
					return (allowProtectedAccess && IsProtectedAccessible(entity.DeclaringTypeDefinition))
						|| IsInternalAccessible(entity.ProjectContent);
				case Accessibility.ProtectedAndInternal:
					return (allowProtectedAccess && IsProtectedAccessible(entity.DeclaringTypeDefinition))
						&& IsInternalAccessible(entity.ProjectContent);
				default:
					throw new Exception("Invalid value for Accessibility");
			}
		}
		
		bool IsInternalAccessible(IProjectContent declaringProject)
		{
			return declaringProject != null && currentProject != null && declaringProject.InternalsVisibleTo(currentProject, context);
		}
		
		bool IsProtectedAccessible(ITypeDefinition declaringType)
		{
			if (declaringType.Equals (currentTypeDefinition))
				return true;
			// PERF: this might hurt performance as this method is called several times (once for each member)
			// make sure resolving base types is cheap (caches?) or cache within the MemberLookup instance
			return currentTypeDefinition != null && currentTypeDefinition.IsDerivedFrom(declaringType, context);
		}
		#endregion
		
		public ResolveResult LookupType(IType declaringType, string name, IList<IType> typeArguments)
		{
			int typeArgumentCount = typeArguments.Count;
			Predicate<ITypeDefinition> typeFilter = delegate (ITypeDefinition d) {
				return d.TypeParameterCount == typeArgumentCount && d.Name == name && IsAccessible(d, true);
			};
			List<IType> types = declaringType.GetNestedTypes(context, typeFilter).ToList();
			RemoveTypesHiddenByOtherTypes(types);
			if (types.Count > 0)
				return CreateTypeResolveResult(types[0], types.Count > 1, typeArguments);
			else
				return new UnknownMemberResolveResult(declaringType, name, typeArguments);
		}
		
		void RemoveTypesHiddenByOtherTypes(List<IType> types)
		{
			for (int i = types.Count - 1; i >= 0; i--) {
				ITypeDefinition d = GetDeclaringTypeDef(types[i]);
				if (d == null)
					continue;
				// nested loop depends on the fact that the members of more derived classes appear later in the list
				for (int j = i + 1; j < types.Count; j++) {
					if (types[i].TypeParameterCount != types[j].TypeParameterCount)
						continue;
					ITypeDefinition s = GetDeclaringTypeDef(types[j]);
					if (s != null && s != d && s.IsDerivedFrom(d, context)) {
						// types[j] hides types[i]
						types.RemoveAt(i);
						break;
					}
				}
			}
		}
		
		ResolveResult CreateTypeResolveResult(IType returnedType, bool isAmbiguous, IList<IType> typeArguments)
		{
			if (typeArguments.Count > 0) {
				// parameterize the type if necessary
				ITypeDefinition returnedTypeDef = returnedType as ITypeDefinition;
				if (returnedTypeDef != null)
					returnedType = new ParameterizedType(returnedTypeDef, typeArguments);
			}
			if (isAmbiguous)
				return new AmbiguousTypeResolveResult(returnedType);
			else
				return new TypeResolveResult(returnedType);
		}
		
		/// <summary>
		/// Performs a member lookup.
		/// </summary>
		public ResolveResult Lookup(IType type, string name, IList<IType> typeArguments, bool isInvocation)
		{
			int typeArgumentCount = typeArguments.Count;
			
			List<IType> types = new List<IType>();
			List<IMember> members = new List<IMember>();
			if (!isInvocation) {
				// Consider nested types only if it's not an invocation. The type parameter count must match in this case.
				Predicate<ITypeDefinition> typeFilter = delegate (ITypeDefinition d) {
					// inner types contain the type parameters of outer types. therefore this count has to been adjusted.
					int correctedCount = d.TypeParameterCount - (d.DeclaringType != null ? d.DeclaringType.TypeParameterCount : 0);
					return correctedCount == typeArgumentCount && d.Name == name && IsAccessible(d, true);
				};
				types.AddRange(type.GetNestedTypes(context, typeFilter));
			}
			
			bool allowProtectedAccess = IsProtectedAccessAllowed(type);
			
			if (typeArgumentCount == 0) {
				Predicate<IMember> memberFilter = delegate(IMember member) {
					return !member.IsOverride && member.Name == name && IsAccessible(member, allowProtectedAccess);
				};
				members.AddRange(type.GetMembers(context, memberFilter));
				if (isInvocation)
					members.RemoveAll(m => !IsInvocable(m, context));
			} else {
				// No need to check for isInvocation/isInvocable here:
				// we filter out all non-methods
				Predicate<IMethod> memberFilter = delegate(IMethod method) {
					return method.TypeParameters.Count == typeArgumentCount
						&& !method.IsOverride && method.Name == name && IsAccessible(method, allowProtectedAccess);
				};
				members.AddRange(type.GetMethods(context, memberFilter).SafeCast<IMethod, IMember>());
			}
			
			// TODO: can't members also hide types?
			
			RemoveTypesHiddenByOtherTypes(types);
			// remove members hidden by types
			for (int i = 0; i < types.Count; i++) {
				ITypeDefinition d = GetDeclaringTypeDef(types[i]);
				if (d != null)
					members.RemoveAll(m => d.IsDerivedFrom(m.DeclaringTypeDefinition, context));
			}
			// remove members hidden by other members
			for (int i = members.Count - 1; i >= 0; i--) {
				ITypeDefinition d = members[i].DeclaringTypeDefinition;
				IMethod mi = members[i] as IMethod;
				// nested loop depends on the fact that the members of more derived classes appear later in the list
				for (int j = i + 1; j < members.Count; j++) {
					if (mi != null) {
						IMethod mj = members[j] as IMethod;
						if (mj != null && !ParameterListComparer.Instance.Equals(mi, mj))
							continue;
					}
					ITypeDefinition s = members[j].DeclaringTypeDefinition;
					if (s != null && s != d && s.IsDerivedFrom(d, context)) {
						// members[j] hides members[i]
						members.RemoveAt(i);
						break;
					}
				}
			}
			// remove interface members hidden by class members
			if (type is ITypeParameter) {
				// this can happen only with type parameters
				for (int i = members.Count - 1; i >= 0; i--) {
					ITypeDefinition d = members[i].DeclaringTypeDefinition;
					if (d.ClassType != ClassType.Interface)
						continue;
					IMethod mi = members[i] as IMethod;
					for (int j = 0; j < members.Count; j++) {
						if (mi != null) {
							IMethod mj = members[j] as IMethod;
							if (mj != null && !ParameterListComparer.Instance.Equals(mi, mj))
								continue;
						}
						ITypeDefinition s = members[j].DeclaringTypeDefinition;
						if (s != null && IsNonInterfaceType(s)) {
							// members[j] hides members[i]
							members.RemoveAt(i);
							break;
						}
					}
				}
			}
			
			if (types.Count > 0) {
				bool isAmbiguous = !(types.Count == 1 && members.Count == 0);
				return CreateTypeResolveResult(types[0], isAmbiguous, typeArguments);
			}
			if (members.Count == 0)
				return new UnknownMemberResolveResult(type, name, typeArguments);
			IMember firstNonMethod = members.FirstOrDefault(m => !(m is IMethod));
			if (members.Count == 1 && firstNonMethod != null)
				return new MemberResolveResult(firstNonMethod, context);
			if (firstNonMethod == null)
				return new MethodGroupResolveResult(type, name, members.ConvertAll(m => (IMethod)m), typeArguments);
			return new AmbiguousMemberResultResult(firstNonMethod, firstNonMethod.ReturnType.Resolve(context));
		}

		static bool IsNonInterfaceType(ITypeDefinition def)
		{
			// return type if def is neither an interface nor System.Object
			return def.ClassType != ClassType.Interface && !(def.Name == "Object" && def.Namespace == "System" && def.TypeParameterCount == 0);
		}
		
		static ITypeDefinition GetDeclaringTypeDef(IType type)
		{
			IType declType = type.DeclaringType;
			return declType != null ? declType.GetDefinition() : null;
		}
	}
}
