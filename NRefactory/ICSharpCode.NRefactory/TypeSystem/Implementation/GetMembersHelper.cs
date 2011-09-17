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
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Provides helper methods for implementing GetMembers() on IType-implementations.
	/// Note: GetMembersHelper will recursively call back into IType.GetMembers(), but only with
	/// both GetMemberOptions.IgnoreInheritedMembers and GetMemberOptions.ReturnMemberDefinitions set,
	/// and only the 'simple' overloads (not taking type arguments).
	/// 
	/// Ensure that your IType implementation does not use the GetMembersHelper if both flags are set,
	/// otherwise you'll get a StackOverflowException!
	/// </summary>
	static class GetMembersHelper
	{
		#region GetNestedTypes
		public static IEnumerable<IType> GetNestedTypes(IType type, ITypeResolveContext context, Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			return GetNestedTypes(type, null, context, filter, options);
		}
		
		public static IEnumerable<IType> GetNestedTypes(IType type, IList<IType> nestedTypeArguments, ITypeResolveContext context, Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetNestedTypesImpl(type, nestedTypeArguments, context, filter, options);
			} else {
				return type.GetNonInterfaceBaseTypes(context).SelectMany(t => GetNestedTypesImpl(t, nestedTypeArguments, context, filter, options));
			}
		}
		
		static IEnumerable<IType> GetNestedTypesImpl(IType outerType, IList<IType> nestedTypeArguments, ITypeResolveContext context, Predicate<ITypeDefinition> filter, GetMemberOptions options)
		{
			ITypeDefinition outerTypeDef = outerType.GetDefinition();
			if (outerTypeDef == null)
				yield break;
			
			int outerTypeParameterCount = outerTypeDef.TypeParameterCount;
			ParameterizedType pt = outerType as ParameterizedType;
			foreach (ITypeDefinition nestedType in outerTypeDef.NestedTypes) {
				int totalTypeParameterCount = nestedType.TypeParameterCount;
				if (nestedTypeArguments != null) {
					if (totalTypeParameterCount - outerTypeParameterCount != nestedTypeArguments.Count)
						continue;
				}
				if (!(filter == null || filter(nestedType)))
					continue;
				
				if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions) {
					yield return nestedType;
				} else if (totalTypeParameterCount == 0 || (pt == null && totalTypeParameterCount == outerTypeParameterCount)) {
					// The nested type has no new type parameters, and there are no type arguments
					// to copy from the outer type
					// -> we can directly return the nested type definition
					yield return nestedType;
				} else {
					// We need to parameterize the nested type
					IType[] newTypeArguments = new IType[totalTypeParameterCount];
					for (int i = 0; i < outerTypeParameterCount; i++) {
						newTypeArguments[i] = pt != null ? pt.GetTypeArgument(i) : outerTypeDef.TypeParameters[i];
					}
					for (int i = outerTypeParameterCount; i < totalTypeParameterCount; i++) {
						if (nestedTypeArguments != null)
							newTypeArguments[i] = nestedTypeArguments[i - outerTypeParameterCount];
						else
							newTypeArguments[i] = SharedTypes.UnboundTypeArgument;
					}
					yield return new ParameterizedType(nestedType, newTypeArguments);
				}
			}
		}
		#endregion
		
		#region GetMethods
		public static IEnumerable<IMethod> GetMethods(IType type, ITypeResolveContext context, Predicate<IMethod> filter, GetMemberOptions options)
		{
			return GetMethods(type, null, context, filter, options);
		}
		
		public static IEnumerable<IMethod> GetMethods(IType type, IList<IType> typeArguments, ITypeResolveContext context, Predicate<IMethod> filter, GetMemberOptions options)
		{
			if (typeArguments != null && typeArguments.Count > 0) {
				filter = FilterTypeParameterCount(typeArguments.Count).And(filter);
			}
			
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetMethodsImpl(type, typeArguments, context, filter, options);
			} else {
				return type.GetNonInterfaceBaseTypes(context).SelectMany(t => GetMethodsImpl(t, typeArguments, context, filter, options));
			}
		}
		
		static Predicate<IMethod> FilterTypeParameterCount(int expectedTypeParameterCount)
		{
			return m => m.TypeParameters.Count == expectedTypeParameterCount;
		}
		
		const GetMemberOptions declaredMembers = GetMemberOptions.IgnoreInheritedMembers | GetMemberOptions.ReturnMemberDefinitions;
		
		static IEnumerable<IMethod> GetMethodsImpl(IType baseType, IList<IType> methodTypeArguments, ITypeResolveContext context, Predicate<IMethod> filter, GetMemberOptions options)
		{
			IEnumerable<IMethod> declaredMethods = baseType.GetMethods(context, filter, options | declaredMembers);
			
			ParameterizedType pt = baseType as ParameterizedType;
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == 0
			    && (pt != null || (methodTypeArguments != null && methodTypeArguments.Count > 0)))
			{
				TypeParameterSubstitution substitution = null;
				foreach (IMethod m in declaredMethods) {
					if (methodTypeArguments != null && methodTypeArguments.Count > 0) {
						if (m.TypeParameters.Count != methodTypeArguments.Count)
							continue;
					}
					if (substitution == null) {
						if (pt != null)
							substitution = pt.GetSubstitution(methodTypeArguments);
						else
							substitution = new TypeParameterSubstitution(null, methodTypeArguments);
					}
					yield return new SpecializedMethod(baseType, m, methodTypeArguments, substitution, context);
				}
			} else {
				foreach (IMethod m in declaredMethods) {
					yield return m;
				}
			}
		}
		#endregion
		
		#region GetConstructors
		public static IEnumerable<IMethod> GetConstructors(IType type, ITypeResolveContext context, Predicate<IMethod> filter, GetMemberOptions options)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetConstructorsImpl(type, context, filter, options);
			} else {
				return type.GetNonInterfaceBaseTypes(context).SelectMany(t => GetConstructorsImpl(t, context, filter, options));
			}
		}
		
		static IEnumerable<IMethod> GetConstructorsImpl(IType baseType, ITypeResolveContext context, Predicate<IMethod> filter, GetMemberOptions options)
		{
			IEnumerable<IMethod> declaredCtors = baseType.GetConstructors(context, filter, options | declaredMembers);
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions) {
				return declaredCtors;
			}
			
			ParameterizedType pt = baseType as ParameterizedType;
			if (pt != null) {
				var substitution = pt.GetSubstitution();
				return declaredCtors.Select(m => new SpecializedMethod(pt, m, null, substitution, context));
			} else {
				return declaredCtors;
			}
		}
		#endregion
		
		#region GetProperties
		public static IEnumerable<IProperty> GetProperties(IType type, ITypeResolveContext context, Predicate<IProperty> filter, GetMemberOptions options)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetPropertiesImpl(type, context, filter, options);
			} else {
				return type.GetNonInterfaceBaseTypes(context).SelectMany(t => GetPropertiesImpl(t, context, filter, options));
			}
		}
		
		static IEnumerable<IProperty> GetPropertiesImpl(IType baseType, ITypeResolveContext context, Predicate<IProperty> filter, GetMemberOptions options)
		{
			IEnumerable<IProperty> declaredProperties = baseType.GetProperties(context, filter, options | declaredMembers);
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions) {
				return declaredProperties;
			}
			
			ParameterizedType pt = baseType as ParameterizedType;
			if (pt != null) {
				var substitution = pt.GetSubstitution();
				return declaredProperties.Select(m => new SpecializedProperty(pt, m, substitution, context));
			} else {
				return declaredProperties;
			}
		}
		#endregion
		
		#region GetFields
		public static IEnumerable<IField> GetFields(IType type, ITypeResolveContext context, Predicate<IField> filter, GetMemberOptions options)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetFieldsImpl(type, context, filter, options);
			} else {
				return type.GetNonInterfaceBaseTypes(context).SelectMany(t => GetFieldsImpl(t, context, filter, options));
			}
		}
		
		static IEnumerable<IField> GetFieldsImpl(IType baseType, ITypeResolveContext context, Predicate<IField> filter, GetMemberOptions options)
		{
			IEnumerable<IField> declaredFields = baseType.GetFields(context, filter, options | declaredMembers);
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions) {
				return declaredFields;
			}
			
			ParameterizedType pt = baseType as ParameterizedType;
			if (pt != null) {
				var substitution = pt.GetSubstitution();
				return declaredFields.Select(m => new SpecializedField(pt, m, substitution, context));
			} else {
				return declaredFields;
			}
		}
		#endregion
		
		#region GetEvents
		public static IEnumerable<IEvent> GetEvents(IType type, ITypeResolveContext context, Predicate<IEvent> filter, GetMemberOptions options)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetEventsImpl(type, context, filter, options);
			} else {
				return type.GetNonInterfaceBaseTypes(context).SelectMany(t => GetEventsImpl(t, context, filter, options));
			}
		}
		
		static IEnumerable<IEvent> GetEventsImpl(IType baseType, ITypeResolveContext context, Predicate<IEvent> filter, GetMemberOptions options)
		{
			IEnumerable<IEvent> declaredEvents = baseType.GetEvents(context, filter, options | declaredMembers);
			if ((options & GetMemberOptions.ReturnMemberDefinitions) == GetMemberOptions.ReturnMemberDefinitions) {
				return declaredEvents;
			}
			
			ParameterizedType pt = baseType as ParameterizedType;
			if (pt != null) {
				var substitution = pt.GetSubstitution();
				return declaredEvents.Select(m => new SpecializedEvent(pt, m, substitution, context));
			} else {
				return declaredEvents;
			}
		}
		#endregion
		
		#region GetMembers
		public static IEnumerable<IMember> GetMembers(IType type, ITypeResolveContext context, Predicate<IMember> filter, GetMemberOptions options)
		{
			if ((options & GetMemberOptions.IgnoreInheritedMembers) == GetMemberOptions.IgnoreInheritedMembers) {
				return GetMembersImpl(type, context, filter, options);
			} else {
				return type.GetNonInterfaceBaseTypes(context).SelectMany(t => GetMembersImpl(t, context, filter, options));
			}
		}
		
		static IEnumerable<IMember> GetMembersImpl(IType baseType, ITypeResolveContext context, Predicate<IMember> filter, GetMemberOptions options)
		{
			foreach (var m in GetMethodsImpl(baseType, null, context, filter, options))
				yield return m;
			foreach (var m in GetPropertiesImpl(baseType, context, filter, options))
				yield return m;
			foreach (var m in GetFieldsImpl(baseType, context, filter, options))
				yield return m;
			foreach (var m in GetEventsImpl(baseType, context, filter, options))
				yield return m;
		}
		#endregion
	}
}
