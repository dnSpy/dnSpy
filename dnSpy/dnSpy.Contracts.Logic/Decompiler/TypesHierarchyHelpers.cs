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
using System.Linq;
using dnlib.DotNet;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace dnSpy.Contracts.Decompiler {
	public static class TypesHierarchyHelpers {
		public static bool IsBaseType(TypeDef? baseType, TypeDef? derivedType, bool resolveTypeArguments) {
			if (baseType is null || derivedType is null)
				return false;
			if (resolveTypeArguments)
				return BaseTypes(derivedType).Any(t => new SigComparer().Equals(t.Resolve(), baseType));
			else {
				var comparableBaseType = baseType.ResolveTypeDef();
				if (comparableBaseType is null)
					return false;
				while (!(derivedType.BaseType is null)) {
					var resolvedBaseType = derivedType.BaseType.Resolve();
					if (resolvedBaseType is null)
						return false;
					if (new SigComparer().Equals(comparableBaseType, resolvedBaseType))
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
		public static bool IsBaseMethod(MethodDef? parentMethod, MethodDef? childMethod) {
			if (parentMethod is null)
				return false;
			if (childMethod is null)
				return false;

			if (parentMethod.Name != childMethod.Name)
				return false;

			var parentParams = parentMethod.MethodSig.GetParamCount();
			var childParams = childMethod.MethodSig.GetParamCount();
			if (parentParams > 0 || childParams > 0)
				if (parentParams == 0 || childParams == 0 || parentParams != childParams)
					return false;

			return FindBaseMethods(childMethod).Any(m => CheckEquals(m, parentMethod));// || (parentMethod.HasGenericParameters && m.);
		}

		static bool CheckEquals(IMemberRef? mr1, IMemberRef? mr2) =>
			new SigComparer(SigComparerOptions.CompareDeclaringTypes | SigComparerOptions.PrivateScopeIsComparable).Equals(mr1, mr2);

		/// <summary>
		/// Determines whether a property overrides or hides another property.
		/// </summary>
		/// <param name="parentProperty">The property declared in a base type.</param>
		/// <param name="childProperty">The property declared in a derived type.</param>
		/// <returns>true if the <paramref name="childProperty"/> hides or overrides <paramref name="parentProperty"/>,
		/// otherwise false.</returns>
		public static bool IsBaseProperty(PropertyDef? parentProperty, PropertyDef? childProperty) {
			if (parentProperty is null)
				return false;
			if (childProperty is null)
				return false;

			if (parentProperty.Name != childProperty.Name)
				return false;

			var parentParams = parentProperty.PropertySig.GetParamCount();
			var childParams = childProperty.PropertySig.GetParamCount();
			if (parentParams > 0 || childParams > 0)
				if (parentParams == 0 || childParams == 0 || parentParams != childParams)
					return false;

			return FindBaseProperties(childProperty).Any(m => CheckEquals(m, parentProperty));
		}

		public static bool IsBaseEvent(EventDef? parentEvent, EventDef childEvent) {
			if (parentEvent is null || parentEvent.Name != childEvent.Name)
				return false;

			return FindBaseEvents(childEvent).Any(m => CheckEquals(m, parentEvent));
		}

		/// <summary>
		/// Finds all methods from base types overridden or hidden by the specified method.
		/// </summary>
		/// <param name="method">The method which overrides or hides methods from base types.</param>
		/// <param name="compareReturnType">Compare return type if true</param>
		/// <returns>Methods overriden or hidden by the specified method.</returns>
		public static IEnumerable<MethodDef> FindBaseMethods(MethodDef? method, bool compareReturnType = true) =>
			FindBaseMethods(method, method?.DeclaringType, compareReturnType);

		public static IEnumerable<MethodDef> FindBaseMethods(MethodDef? method, TypeDef? declType, bool compareReturnType = true) {
			if (method is null || declType is null)
				yield break;

			foreach (var baseType in BaseTypes(declType)) {
				var baseTypeDef = baseType.Resolve();
				if (baseTypeDef is null)
					continue;
				foreach (var baseMethod in baseTypeDef.Methods) {
					if (MatchMethod(baseMethod, Resolve(baseMethod.MethodSig, baseType), method, compareReturnType) && IsVisibleFromDerived(baseMethod, declType)) {
						yield return baseMethod;
						if (baseMethod.IsNewSlot == baseMethod.IsVirtual)
							yield break;
					}
				}
			}
		}

		private static bool MatchMethod(MethodDef? mCandidate, MethodBaseSig? mCandidateSig, MethodDef? mMethod, bool compareReturnType = true) =>
			MatchMethod(mCandidate, mCandidateSig, mMethod, mMethod?.MethodSig, compareReturnType);

		static bool MatchMethod(MethodDef? mCandidate, MethodBaseSig? mCandidateSig, MethodDef? mMethod, MethodBaseSig? mMethodSig, bool compareReturnType = true) {
			if (mCandidate is null || mCandidateSig is null || mMethod is null)
				return false;

			if (mCandidate.Name != mMethod.Name)
				return false;

			var options = compareReturnType ? 0 : SigComparerOptions.DontCompareReturnType;
			return new SigComparer(options).Equals(mCandidateSig, mMethodSig);
		}

		public static bool MatchInterfaceMethod(MethodDef? candidate, MethodDef? method, ITypeDefOrRef interfaceContextType) {
			var genericInstSig = interfaceContextType.TryGetGenericInstSig();
			if (!(genericInstSig is null)) {
				return MatchMethod(candidate, candidate?.MethodSig, method, GenericArgumentResolver.Resolve(method?.MethodSig, genericInstSig.GenericArguments, null));
			}
			else {
				return MatchMethod(candidate, candidate?.MethodSig, method);
			}
		}

		/// <summary>
		/// Finds all properties from base types overridden or hidden by the specified property.
		/// </summary>
		/// <param name="property">The property which overrides or hides properties from base types.</param>
		/// <returns>Properties overriden or hidden by the specified property.</returns>
		public static IEnumerable<PropertyDef> FindBaseProperties(PropertyDef? property) =>
			FindBaseProperties(property, property?.DeclaringType);

		public static IEnumerable<PropertyDef> FindBaseProperties(PropertyDef? property, TypeDef? declType) {
			if (property is null)
				yield break;

			bool isIndexer = property.IsIndexer();

			foreach (var baseType in BaseTypes(declType)) {
				var baseTypeDef = baseType.Resolve();
				if (baseTypeDef is null)
					continue;
				foreach (var baseProperty in baseTypeDef.Properties) {
					if (MatchProperty(baseProperty, Resolve(baseProperty.PropertySig, baseType), property)
							&& IsVisibleFromDerived(baseProperty, declType)) {
						if (isIndexer != baseProperty.IsIndexer())
							continue;
						yield return baseProperty;
						var anyPropertyAccessor = baseProperty.GetMethod ?? baseProperty.SetMethod;
						if (!(anyPropertyAccessor is null) && anyPropertyAccessor.IsNewSlot == anyPropertyAccessor.IsVirtual)
							yield break;
					}
				}
			}
		}

		private static bool MatchProperty(PropertyDef? mCandidate, MethodBaseSig? mCandidateSig, PropertyDef? mProperty) {
			if (mCandidate is null || mCandidateSig is null || mProperty is null)
				return false;
			if (mCandidate.Name != mProperty.Name)
				return false;

			return new SigComparer().Equals(mCandidateSig, mProperty.PropertySig);
		}

		public static IEnumerable<EventDef> FindBaseEvents(EventDef? eventDef) =>
			FindBaseEvents(eventDef, eventDef?.DeclaringType);

		public static IEnumerable<EventDef> FindBaseEvents(EventDef? eventDef, TypeDef? declType) {
			if (eventDef is null)
				yield break;

			var eventType = eventDef.EventType.ToTypeSig();

			foreach (var baseType in BaseTypes(declType)) {
				var baseTypeDef = baseType.Resolve();
				if (baseTypeDef is null)
					continue;
				foreach (var baseEvent in baseTypeDef.Events) {
					if (MatchEvent(baseEvent, Resolve(baseEvent.EventType.ToTypeSig(), baseType), eventDef, eventType) &&
						IsVisibleFromDerived(baseEvent, declType)) {
						yield return baseEvent;
						var anyEventAccessor = baseEvent.AddMethod ?? baseEvent.RemoveMethod ?? baseEvent.InvokeMethod;
						if (!(anyEventAccessor is null) && anyEventAccessor.IsNewSlot == anyEventAccessor.IsVirtual)
							yield break;
					}
				}
			}
		}

		private static bool MatchEvent(EventDef? mCandidate, TypeSig? mCandidateType, EventDef? mEvent, TypeSig? mEventType) {
			if (mCandidate is null || mCandidateType is null || mEvent is null || mEventType is null)
				return false;
			if (mCandidate.Name != mEvent.Name)
				return false;

			return new SigComparer().Equals(mCandidateType, mEventType);
		}

		/// <summary>
		/// Determinates whether member of the base type is visible from a derived type.
		/// </summary>
		/// <param name="baseMember">The member which visibility is checked.</param>
		/// <param name="derivedType">The derived type.</param>
		/// <returns>true if the member is visible from derived type, othewise false.</returns>
		public static bool IsVisibleFromDerived(IMemberDef? baseMember, TypeDef? derivedType) {
			if (baseMember is null)
				return false;
			if (derivedType is null)
				return false;

			MethodAttributes attrs = GetAccessAttributes(baseMember) & MethodAttributes.MemberAccessMask;
			if (attrs == MethodAttributes.Private)
				return false;

			if (baseMember.DeclaringType.Module == derivedType.Module)
				return true;

			if (attrs == MethodAttributes.Assembly || attrs == MethodAttributes.FamANDAssem) {
				var derivedTypeAsm = derivedType.Module.Assembly;
				var asm = baseMember.DeclaringType.Module.Assembly;

				if (!(derivedTypeAsm is null) && !(asm is null)) {
					foreach (var attribute in asm.CustomAttributes) {
						if (!Compare(attribute.AttributeType, systemRuntimeCompilerServicesString, internalsVisibleToAttributeString))
							continue;
						if (attribute.ConstructorArguments.Count == 0)
							continue;
						string assemblyName = attribute.ConstructorArguments[0].Value as UTF8String;
						if (assemblyName is null)
							continue;
						assemblyName = new AssemblyNameInfo(assemblyName).Name;
						if (StringComparer.OrdinalIgnoreCase.Equals(assemblyName, derivedTypeAsm.Name))
							return true;
					}
					foreach (var attribute in derivedTypeAsm.CustomAttributes) {
						if (!Compare(attribute.AttributeType, systemRuntimeCompilerServicesString, ignoresAccessChecksToAttributeString))
							continue;
						if (attribute.ConstructorArguments.Count == 0)
							continue;
						string assemblyName = attribute.ConstructorArguments[0].Value as UTF8String;
						if (assemblyName is null)
							continue;
						assemblyName = new AssemblyNameInfo(assemblyName).Name;
						if (StringComparer.OrdinalIgnoreCase.Equals(assemblyName, asm.Name))
							return true;
					}
				}

				return false;
			}

			return true;
		}
		static readonly UTF8String systemRuntimeCompilerServicesString = new UTF8String("System.Runtime.CompilerServices");
		static readonly UTF8String internalsVisibleToAttributeString = new UTF8String("InternalsVisibleToAttribute");
		static readonly UTF8String ignoresAccessChecksToAttributeString = new UTF8String("IgnoresAccessChecksToAttribute");

		static bool Compare(ITypeDefOrRef? type, UTF8String expNs, UTF8String expName) {
			if (type is null)
				return false;

			if (type is TypeRef tr)
				return tr.Namespace == expNs && tr.Name == expName;
			if (type is TypeDef td)
				return td.Namespace == expNs && td.Name == expName;

			return false;
		}

		private static MethodAttributes GetAccessAttributes(IMemberDef member) {
			if (member is FieldDef fld)
				return (MethodAttributes)fld.Attributes;

			if (member is MethodDef method)
				return method.Attributes;

			if (member is PropertyDef prop) {
				var accMeth = prop.GetMethod ?? prop.SetMethod;
				return accMeth is null ? 0 : accMeth.Attributes;
			}

			if (member is EventDef evnt) {
				var m = evnt.AddMethod ?? evnt.RemoveMethod ?? evnt.InvokeMethod;
				return m is null ? 0 : m.Attributes;
			}

			if (member is TypeDef nestedType) {
				if (nestedType.IsNestedPrivate)
					return MethodAttributes.Private;
				if (nestedType.IsNestedAssembly || nestedType.IsNestedFamilyAndAssembly)
					return MethodAttributes.Assembly;
				return MethodAttributes.Public;
			}

			return 0;
		}

		private static IEnumerable<TypeSig> BaseTypes(TypeDef? typeDef) {
			if (typeDef is null)
				yield break;
			if (typeDef.BaseType is null)
				yield break;

			TypeSig? baseType = typeDef.ToTypeSig();
			do {
				var genericArgs = baseType is GenericInstSig ? ((GenericInstSig)baseType).GenericArguments : null;
				baseType = GenericArgumentResolver.Resolve(typeDef.BaseType.ToTypeSig(), genericArgs, null);
				if (baseType is null)
					yield break;
				yield return baseType;

				typeDef = typeDef.BaseType.ResolveTypeDef();
				if (typeDef is null)
					break;
			} while (!(typeDef.BaseType is null));
		}

		public static IEnumerable<TypeSig> GetTypeAndBaseTypes(TypeDef? type) {
			if (type is null)
				yield break;
			yield return type.ToTypeSig();
			foreach (var baseType in BaseTypes(type))
				yield return baseType;
		}

		private static TypeSig? Resolve(TypeSig? type, TypeSig? typeContext) {
			var genericArgs = typeContext is GenericInstSig ? ((GenericInstSig)typeContext).GenericArguments : null;
			return GenericArgumentResolver.Resolve(type, genericArgs, null);
		}

		private static MethodBaseSig? Resolve(MethodBaseSig? method, TypeSig? typeContext) {
			var genericArgs = typeContext is GenericInstSig ? ((GenericInstSig)typeContext).GenericArguments : null;
			return GenericArgumentResolver.Resolve(method, genericArgs, null);
		}
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
