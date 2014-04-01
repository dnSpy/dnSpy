// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace ICSharpCode.Decompiler.Ast
{
	public static class TypesHierarchyHelpers
	{
		public static bool IsBaseType(TypeDef baseType, TypeDef derivedType, bool resolveTypeArguments)
		{
			if (resolveTypeArguments)
				return BaseTypes(derivedType).Any(t => t.Resolve() == baseType);
			else {
				var comparableBaseType = baseType;
				while (derivedType.BaseType != null) {
					var resolvedBaseType = derivedType.BaseType.ResolveTypeDefThrow();
					if (resolvedBaseType == null)
						return false;
					if (comparableBaseType == resolvedBaseType)
						return true;
					derivedType = resolvedBaseType;
				}
				return false;
			}
		}

		/// <summary>
		/// Determines whether one method overrides or hides another method.
		/// </summary>
		/// <param name="parentMethod">The method declared in a base type.</param>
		/// <param name="childMethod">The method declared in a derived type.</param>
		/// <returns>true if <paramref name="childMethod"/> hides or overrides <paramref name="parentMethod"/>,
		/// otherwise false.</returns>
		public static bool IsBaseMethod(MethodDef parentMethod, MethodDef childMethod)
		{
			if (parentMethod == null)
				throw new ArgumentNullException("parentMethod");
			if (childMethod == null)
				throw new ArgumentNullException("childMethod");

			if (parentMethod.Name != childMethod.Name)
				return false;

			var parentParams = parentMethod.MethodSig.GetParameters();
			var childParams = childMethod.MethodSig.GetParameters();
			if (parentParams.Count > 0 || childParams.Count > 0)
				if (parentParams.Count == 0 || childParams.Count == 0 || parentParams.Count != childParams.Count)
					return false;

			return FindBaseMethods(childMethod).Any(m => m == parentMethod);// || (parentMethod.HasGenericParameters && m.);
		}

		/// <summary>
		/// Determines whether a property overrides or hides another property.
		/// </summary>
		/// <param name="parentProperty">The property declared in a base type.</param>
		/// <param name="childProperty">The property declared in a derived type.</param>
		/// <returns>true if the <paramref name="childProperty"/> hides or overrides <paramref name="parentProperty"/>,
		/// otherwise false.</returns>
		public static bool IsBaseProperty(PropertyDef parentProperty, PropertyDef childProperty)
		{
			if (parentProperty == null)
				throw new ArgumentNullException("parentProperty");
			if (childProperty == null)
				throw new ArgumentNullException("childProperty");

			if (parentProperty.Name != childProperty.Name)
				return false;

			var parentParams = parentProperty.PropertySig.GetParameters();
			var childParams = childProperty.PropertySig.GetParameters();
			if (parentParams.Count > 0 || childParams.Count > 0)
				if (parentParams.Count == 0 || childParams.Count == 0 || parentParams.Count != childParams.Count)
					return false;

			return FindBaseProperties(childProperty).Any(m => m == parentProperty);
		}

		public static bool IsBaseEvent(EventDef parentEvent, EventDef childEvent)
		{
			if (parentEvent.Name != childEvent.Name)
				return false;

			return FindBaseEvents(childEvent).Any(m => m == parentEvent);
		}

		/// <summary>
		/// Finds all methods from base types overridden or hidden by the specified method.
		/// </summary>
		/// <param name="method">The method which overrides or hides methods from base types.</param>
		/// <returns>Methods overriden or hidden by the specified method.</returns>
		public static IEnumerable<MethodDef> FindBaseMethods(MethodDef method)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			foreach (var baseType in BaseTypes(method.DeclaringType))
			{
				var baseTypeDef = baseType.Resolve();
				foreach (var baseMethod in baseTypeDef.Methods)
					if (MatchMethod(baseMethod, Resolve(baseMethod.MethodSig, baseType), method) && IsVisibleFromDerived(baseMethod, method.DeclaringType))
					{
						yield return baseMethod;
						if (baseMethod.IsNewSlot == baseMethod.IsVirtual)
							yield break;
					}
			}
		}

		private static bool MatchMethod(MethodDef mCandidate, MethodSig mCandidateSig, MethodDef mMethod)
		{
			if (mCandidate.Name != mMethod.Name)
				return false;

			if (mCandidate.HasOverrides)
				return false;

			if (mCandidate.IsSpecialName != mMethod.IsSpecialName)
				return false;

			if (mCandidate.HasGenericParameters || mMethod.HasGenericParameters) {
				if (!mCandidate.HasGenericParameters || !mMethod.HasGenericParameters || mCandidate.GenericParameters.Count != mMethod.GenericParameters.Count)
					return false;
			}

			return new SigComparer().Equals(mCandidateSig, mMethod.MethodSig);
		}

		public static bool MatchInterfaceMethod(MethodDef candidate, MethodDef method, ITypeDefOrRef interfaceContextType)
		{
			var genericInstSig = interfaceContextType.TryGetGenericInstSig();
			if (genericInstSig != null) {
				return MatchMethod(candidate, GenericArgumentResolver.Resolve(candidate.MethodSig, genericInstSig.GenericArguments, null), method);
			} else {
				return MatchMethod(candidate, candidate.MethodSig, method);
			}
		}

		/// <summary>
		/// Finds all properties from base types overridden or hidden by the specified property.
		/// </summary>
		/// <param name="property">The property which overrides or hides properties from base types.</param>
		/// <returns>Properties overriden or hidden by the specified property.</returns>
		public static IEnumerable<PropertyDef> FindBaseProperties(PropertyDef property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			if ((property.GetMethod ?? property.SetMethod).HasOverrides)
				yield break;

			bool isIndexer = property.IsIndexer();

			foreach (var baseType in BaseTypes(property.DeclaringType))
			{
				var baseTypeDef = baseType.Resolve();
				foreach (var baseProperty in baseTypeDef.Properties)
					if (MatchProperty(baseProperty, Resolve(baseProperty.PropertySig, baseType), property)
							&& IsVisibleFromDerived(baseProperty, property.DeclaringType))
					{
						if (isIndexer != baseProperty.IsIndexer())
							continue;
						yield return baseProperty;
						var anyPropertyAccessor = baseProperty.GetMethod ?? baseProperty.SetMethod;
						if (anyPropertyAccessor.IsNewSlot == anyPropertyAccessor.IsVirtual)
							yield break;
					}
			}
		}

		private static bool MatchProperty(PropertyDef mCandidate, MethodSig mCandidateSig, PropertyDef mProperty)
		{
			if (mCandidate.Name != mProperty.Name)
				return false;

			if ((mCandidate.GetMethod ?? mCandidate.SetMethod).HasOverrides)
				return false;

			return new SigComparer().Equals(mCandidateSig, mProperty.PropertySig);
		}

		public static IEnumerable<EventDef> FindBaseEvents(EventDef eventDef)
		{
			if (eventDef == null)
				throw new ArgumentNullException("eventDef");

			var eventType = eventDef.EventType.ToTypeSig();

			foreach (var baseType in BaseTypes(eventDef.DeclaringType))
			{
				var baseTypeDef = baseType.Resolve();
				foreach (var baseEvent in baseTypeDef.Events)
					if (MatchEvent(baseEvent, Resolve(baseEvent.EventType.ToTypeSig(), baseType), eventDef, eventType) &&
						IsVisibleFromDerived(baseEvent, eventDef.DeclaringType))
					{
						yield return baseEvent;
						var anyEventAccessor = baseEvent.AddMethod ?? baseEvent.RemoveMethod;
						if (anyEventAccessor.IsNewSlot == anyEventAccessor.IsVirtual)
							yield break;
					}
			}
		}

		private static bool MatchEvent(EventDef mCandidate, TypeSig mCandidateType, EventDef mEvent, TypeSig mEventType)
		{
			if (mCandidate.Name != mEvent.Name)
				return false;

			if ((mCandidate.AddMethod ?? mCandidate.RemoveMethod).HasOverrides)
				return false;

			if (!new SigComparer().Equals(mCandidateType, mEventType))
				return false;

			return true;
		}

		/// <summary>
		/// Determinates whether member of the base type is visible from a derived type.
		/// </summary>
		/// <param name="baseMember">The member which visibility is checked.</param>
		/// <param name="derivedType">The derived type.</param>
		/// <returns>true if the member is visible from derived type, othewise false.</returns>
		public static bool IsVisibleFromDerived(IDefinition baseMember, TypeDef derivedType)
		{
			if (baseMember == null)
				throw new ArgumentNullException("baseMember");
			if (derivedType == null)
				throw new ArgumentNullException("derivedType");

			var visibility = IsVisibleFromDerived(baseMember);
			if (visibility.HasValue)
				return visibility.Value;

			if (baseMember.DeclaringType.Module == derivedType.Module)
				return true;
			// TODO: Check also InternalsVisibleToAttribute.
				return false;
		}

		private static bool? IsVisibleFromDerived(IDefinition member)
		{
			MethodAttributes attrs = GetAccessAttributes(member) & MethodAttributes.MemberAccessMask;
			if (attrs == MethodAttributes.Private)
				return false;
			if (attrs == MethodAttributes.Assembly || attrs == MethodAttributes.FamANDAssem)
				return null;
				return true;
		}

		private static MethodAttributes GetAccessAttributes(IDefinition member)
		{
			var fld = member as FieldDef;
			if (fld != null)
				return (MethodAttributes)fld.Attributes;

			var method = member as MethodDef;
			if (method != null)
				return method.Attributes;

			var prop = member as PropertyDef;
			if (prop != null) {
				return (prop.GetMethod ?? prop.SetMethod).Attributes;
		}

			var evnt = member as EventDef;
			if (evnt != null) {
				return (evnt.AddMethod ?? evnt.RemoveMethod).Attributes;
			}

			var nestedType = member as TypeDef;
			if (nestedType != null) {
				if (nestedType.IsNestedPrivate)
					return MethodAttributes.Private;
				if (nestedType.IsNestedAssembly || nestedType.IsNestedFamilyAndAssembly)
					return MethodAttributes.Assembly;
				return MethodAttributes.Public;
			}

			throw new NotSupportedException();
		}

		private static IEnumerable<TypeSig> BaseTypes(TypeDef type)
		{
			return BaseTypes(type.ToTypeSig());
		}

		private static IEnumerable<TypeSig> BaseTypes(TypeSig type)
		{
			TypeDef typeDef = type.GetTypeDefOrRef().ResolveTypeDefThrow();
			if (typeDef.BaseType == null)
				yield break;

			TypeSig baseType = type;
			do {
				var genericArgs = baseType.IsGenericInstanceType ? ((GenericInstSig)baseType).GenericArguments : null;
				baseType = GenericArgumentResolver.Resolve(typeDef.BaseType.ToTypeSig(), genericArgs, null);
				yield return baseType;

				typeDef = typeDef.BaseType.ResolveTypeDefThrow();
			} while (typeDef.BaseType != null);
		}

		private static TypeSig Resolve(TypeSig type, TypeSig typeContext)
		{
			var genericArgs = typeContext.IsGenericInstanceType ? ((GenericInstSig)typeContext).GenericArguments : null;
			return GenericArgumentResolver.Resolve(type, genericArgs, null);
		}

		private static MethodSig Resolve(MethodBaseSig method, TypeSig typeContext)
		{
			var genericArgs = typeContext.IsGenericInstanceType ? ((GenericInstSig)typeContext).GenericArguments : null;
			return GenericArgumentResolver.Resolve(method, genericArgs, null);
		}
	}
}
