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
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast
{
	public static class TypesHierarchyHelpers
	{
		public static bool IsBaseType(TypeDefinition baseType, TypeDefinition derivedType, bool resolveTypeArguments)
		{
			if (resolveTypeArguments)
				return BaseTypes(derivedType).Any(t => t.Item == baseType);
			else {
				var comparableBaseType = baseType.Resolve();
				if (comparableBaseType == null)
					return false;
				while (derivedType.BaseType != null) {
					var resolvedBaseType = derivedType.BaseType.Resolve();
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
		public static bool IsBaseMethod(MethodDefinition parentMethod, MethodDefinition childMethod)
		{
			if (parentMethod == null)
				throw new ArgumentNullException("parentMethod");
			if (childMethod == null)
				throw new ArgumentNullException("childMethod");

			if (parentMethod.Name != childMethod.Name)
				return false;

			if (parentMethod.HasParameters || childMethod.HasParameters)
				if (!parentMethod.HasParameters || !childMethod.HasParameters || parentMethod.Parameters.Count != childMethod.Parameters.Count)
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
		public static bool IsBaseProperty(PropertyDefinition parentProperty, PropertyDefinition childProperty)
		{
			if (parentProperty == null)
				throw new ArgumentNullException("parentProperty");
			if (childProperty == null)
				throw new ArgumentNullException("childProperty");

			if (parentProperty.Name != childProperty.Name)
				return false;

			if (parentProperty.HasParameters || childProperty.HasParameters)
				if (!parentProperty.HasParameters || !childProperty.HasParameters || parentProperty.Parameters.Count != childProperty.Parameters.Count)
					return false;

			return FindBaseProperties(childProperty).Any(m => m == parentProperty);
		}

		public static bool IsBaseEvent(EventDefinition parentEvent, EventDefinition childEvent)
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
		public static IEnumerable<MethodDefinition> FindBaseMethods(MethodDefinition method)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			var typeContext = CreateGenericContext(method.DeclaringType);
			var gMethod = typeContext.ApplyTo(method);

			foreach (var baseType in BaseTypes(method.DeclaringType))
				foreach (var baseMethod in baseType.Item.Methods)
					if (MatchMethod(baseType.ApplyTo(baseMethod), gMethod) && IsVisibleFromDerived(baseMethod, method.DeclaringType)) {
						yield return baseMethod;
						if (baseMethod.IsNewSlot == baseMethod.IsVirtual)
							yield break;
					}
		}

		/// <summary>
		/// Finds all properties from base types overridden or hidden by the specified property.
		/// </summary>
		/// <param name="property">The property which overrides or hides properties from base types.</param>
		/// <returns>Properties overriden or hidden by the specified property.</returns>
		public static IEnumerable<PropertyDefinition> FindBaseProperties(PropertyDefinition property)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			if ((property.GetMethod ?? property.SetMethod).HasOverrides)
				yield break;

			var typeContext = CreateGenericContext(property.DeclaringType);
			var gProperty = typeContext.ApplyTo(property);
			bool isIndexer = property.IsIndexer();

			foreach (var baseType in BaseTypes(property.DeclaringType))
				foreach (var baseProperty in baseType.Item.Properties)
					if (MatchProperty(baseType.ApplyTo(baseProperty), gProperty)
							&& IsVisibleFromDerived(baseProperty, property.DeclaringType)) {
						if (isIndexer != baseProperty.IsIndexer())
							continue;
						yield return baseProperty;
						var anyPropertyAccessor = baseProperty.GetMethod ?? baseProperty.SetMethod;
						if (anyPropertyAccessor.IsNewSlot == anyPropertyAccessor.IsVirtual)
							yield break;
					}
		}

		public static IEnumerable<EventDefinition> FindBaseEvents(EventDefinition eventDef)
		{
			if (eventDef == null)
				throw new ArgumentNullException("eventDef");

			var typeContext = CreateGenericContext(eventDef.DeclaringType);
			var gEvent = typeContext.ApplyTo(eventDef);

			foreach (var baseType in BaseTypes(eventDef.DeclaringType))
				foreach (var baseEvent in baseType.Item.Events)
					if (MatchEvent(baseType.ApplyTo(baseEvent), gEvent) && IsVisibleFromDerived(baseEvent, eventDef.DeclaringType)) {
						yield return baseEvent;
						var anyEventAccessor = baseEvent.AddMethod ?? baseEvent.RemoveMethod;
						if (anyEventAccessor.IsNewSlot == anyEventAccessor.IsVirtual)
							yield break;
					}

		}

		/// <summary>
		/// Determinates whether member of the base type is visible from a derived type.
		/// </summary>
		/// <param name="baseMember">The member which visibility is checked.</param>
		/// <param name="derivedType">The derived type.</param>
		/// <returns>true if the member is visible from derived type, othewise false.</returns>
		public static bool IsVisibleFromDerived(IMemberDefinition baseMember, TypeDefinition derivedType)
		{
			if (baseMember == null)
				throw new ArgumentNullException("baseMember");
			if (derivedType == null)
				throw new ArgumentNullException("derivedType");

			MethodAttributes attrs = GetAccessAttributes(baseMember) & MethodAttributes.MemberAccessMask;
			if (attrs == MethodAttributes.Private)
				return false;

			if (baseMember.DeclaringType.Module == derivedType.Module)
				return true;

			if (attrs == MethodAttributes.Assembly || attrs == MethodAttributes.FamANDAssem) {
				var derivedTypeAsm = derivedType.Module.Assembly;
				var asm = baseMember.DeclaringType.Module.Assembly;

				if (asm.HasCustomAttributes) {
					var attributes = asm.CustomAttributes
						.Where(attr => attr.AttributeType.FullName == "System.Runtime.CompilerServices.InternalsVisibleToAttribute");
					foreach (var attribute in attributes) {
						string assemblyName = attribute.ConstructorArguments[0].Value as string;
						assemblyName = assemblyName.Split(',')[0]; // strip off any public key info
						if (assemblyName == derivedTypeAsm.Name.Name)
							return true;
					}
				}

				return false;
			}

			return true;
		}

		private static MethodAttributes GetAccessAttributes(IMemberDefinition member)
		{
			var fld = member as FieldDefinition;
			if (fld != null)
				return (MethodAttributes)fld.Attributes;

			var method = member as MethodDefinition;
			if (method != null)
				return method.Attributes;

			var prop = member as PropertyDefinition;
			if (prop != null) {
				return (prop.GetMethod ?? prop.SetMethod).Attributes;
		}

			var evnt = member as EventDefinition;
			if (evnt != null) {
				return (evnt.AddMethod ?? evnt.RemoveMethod).Attributes;
			}

			var nestedType = member as TypeDefinition;
			if (nestedType != null) {
				if (nestedType.IsNestedPrivate)
					return MethodAttributes.Private;
				if (nestedType.IsNestedAssembly || nestedType.IsNestedFamilyAndAssembly)
					return MethodAttributes.Assembly;
				return MethodAttributes.Public;
			}

			throw new NotSupportedException();
		}

		private static bool MatchMethod(GenericContext<MethodDefinition> candidate, GenericContext<MethodDefinition> method)
		{
			var mCandidate = candidate.Item;
			var mMethod = method.Item;
			if (mCandidate.Name != mMethod.Name)
				return false;

			if (mCandidate.HasOverrides)
				return false;

			if (mCandidate.IsSpecialName != method.Item.IsSpecialName)
				return false;

			if (mCandidate.HasGenericParameters || mMethod.HasGenericParameters) {
				if (!mCandidate.HasGenericParameters || !mMethod.HasGenericParameters || mCandidate.GenericParameters.Count != mMethod.GenericParameters.Count)
					return false;
			}

			if (mCandidate.HasParameters || mMethod.HasParameters) {
				if (!mCandidate.HasParameters || !mMethod.HasParameters || mCandidate.Parameters.Count != mMethod.Parameters.Count)
					return false;

				for (int index = 0; index < mCandidate.Parameters.Count; index++) {
					if (!MatchParameters(candidate.ApplyTo(mCandidate.Parameters[index]), method.ApplyTo(mMethod.Parameters[index])))
						return false;
				}
			}

			return true;
		}

		public static bool MatchInterfaceMethod(MethodDefinition candidate, MethodDefinition method, TypeReference interfaceContextType)
		{
			var candidateContext = CreateGenericContext(candidate.DeclaringType);
			var gCandidate = candidateContext.ApplyTo(candidate);

			if (interfaceContextType is GenericInstanceType) {
				var methodContext = new GenericContext<TypeDefinition>(interfaceContextType.Resolve(), ((GenericInstanceType)interfaceContextType).GenericArguments);
				var gMethod = methodContext.ApplyTo(method);
				return MatchMethod(gCandidate, gMethod);
			} else {
				var methodContext = CreateGenericContext(interfaceContextType.Resolve());
				var gMethod = candidateContext.ApplyTo(method);
				return MatchMethod(gCandidate, gMethod);
			}
		}

		private static bool MatchProperty(GenericContext<PropertyDefinition> candidate, GenericContext<PropertyDefinition> property)
		{
			var mCandidate = candidate.Item;
			var mProperty = property.Item;
			if (mCandidate.Name != mProperty.Name)
				return false;

			if ((mCandidate.GetMethod ?? mCandidate.SetMethod).HasOverrides)
				return false;

			if (mCandidate.HasParameters || mProperty.HasParameters) {
				if (!mCandidate.HasParameters || !mProperty.HasParameters || mCandidate.Parameters.Count != mProperty.Parameters.Count)
					return false;

				for (int index = 0; index < mCandidate.Parameters.Count; index++) {
					if (!MatchParameters(candidate.ApplyTo(mCandidate.Parameters[index]), property.ApplyTo(mProperty.Parameters[index])))
						return false;
				}
			}

			return true;
		}

		private static bool MatchEvent(GenericContext<EventDefinition> candidate, GenericContext<EventDefinition> ev)
		{
			var mCandidate = candidate.Item;
			var mEvent = ev.Item;
			if (mCandidate.Name != mEvent.Name)
				return false;

			if ((mCandidate.AddMethod ?? mCandidate.RemoveMethod).HasOverrides)
				return false;

			if (!IsSameType(candidate.ResolveWithContext(mCandidate.EventType), ev.ResolveWithContext(mEvent.EventType)))
				return false;

			return true;
		}

		private static bool MatchParameters(GenericContext<ParameterDefinition> baseParameterType, GenericContext<ParameterDefinition> parameterType)
		{
			if (baseParameterType.Item.IsIn != parameterType.Item.IsIn ||
					baseParameterType.Item.IsOut != parameterType.Item.IsOut)
				return false;
			var baseParam = baseParameterType.ResolveWithContext(baseParameterType.Item.ParameterType);
			var param = parameterType.ResolveWithContext(parameterType.Item.ParameterType);
			return IsSameType(baseParam, param);
		}

		private static bool IsSameType(TypeReference tr1, TypeReference tr2)
		{
			if (tr1 == tr2)
				return true;
			if (tr1 == null || tr2 == null)
				return false;

			if (tr1.GetType() != tr2.GetType())
				return false;

			if (tr1.Name == tr2.Name && tr1.FullName == tr2.FullName)
				return true;

			return false;
		}

		private static IEnumerable<GenericContext<TypeDefinition>> BaseTypes(TypeDefinition type)
		{
			return BaseTypes(CreateGenericContext(type));
		}

		private static IEnumerable<GenericContext<TypeDefinition>> BaseTypes(GenericContext<TypeDefinition> type)
		{
			while (type.Item.BaseType != null) {
				var baseType = type.Item.BaseType;
				var genericBaseType = baseType as GenericInstanceType;
				if (genericBaseType != null) {
					type = new GenericContext<TypeDefinition>(genericBaseType.ResolveOrThrow(),
						genericBaseType.GenericArguments.Select(t => type.ResolveWithContext(t)));
				} else
					type = new GenericContext<TypeDefinition>(baseType.ResolveOrThrow());
				yield return type;
			}
		}

		private static GenericContext<TypeDefinition> CreateGenericContext(TypeDefinition type)
		{
			return type.HasGenericParameters
				? new GenericContext<TypeDefinition>(type, type.GenericParameters)
				: new GenericContext<TypeDefinition>(type);
		}

		struct GenericContext<T> where T : class
		{
			private static readonly ReadOnlyCollection<TypeReference> Empty = new ReadOnlyCollection<TypeReference>(new List<TypeReference>());
			private static readonly GenericParameter UnresolvedGenericTypeParameter =
				new DummyGenericParameterProvider(false).DummyParameter;
			private static readonly GenericParameter UnresolvedGenericMethodParameter =
				new DummyGenericParameterProvider(true).DummyParameter;

			public readonly T Item;
			public readonly ReadOnlyCollection<TypeReference> TypeArguments;

			public GenericContext(T item)
			{
				if (item == null)
					throw new ArgumentNullException("item");

				Item = item;
				TypeArguments = Empty;
			}

			public GenericContext(T item, IEnumerable<TypeReference> typeArguments)
			{
				if (item == null)
					throw new ArgumentNullException("item");

				Item = item;
				var list = new List<TypeReference>();
				foreach (var arg in typeArguments) {
					var resolved = arg != null ? arg.Resolve() : arg;
					list.Add(resolved != null ? resolved : arg);
				}
				TypeArguments = new ReadOnlyCollection<TypeReference>(list);
			}

			private GenericContext(T item, ReadOnlyCollection<TypeReference> typeArguments)
			{
				Item = item;
				TypeArguments = typeArguments;
			}

			public TypeReference ResolveWithContext(TypeReference type)
			{
				var genericParameter = type as GenericParameter;
				if (genericParameter != null)
					if (genericParameter.Owner.GenericParameterType == GenericParameterType.Type)
					return this.TypeArguments[genericParameter.Position];
					else
						return genericParameter.Owner.GenericParameterType == GenericParameterType.Type
							? UnresolvedGenericTypeParameter : UnresolvedGenericMethodParameter;
				var typeSpecification = type as TypeSpecification;
				if (typeSpecification != null) {
					var resolvedElementType = ResolveWithContext(typeSpecification.ElementType);
					return ReplaceElementType(typeSpecification, resolvedElementType);
				}
				return type.ResolveOrThrow();
			}

			private TypeReference ReplaceElementType(TypeSpecification ts, TypeReference newElementType)
			{
				var arrayType = ts as ArrayType;
				if (arrayType != null) {
					if (newElementType == arrayType.ElementType)
						return arrayType;
					var newArrayType = new ArrayType(newElementType, arrayType.Rank);
					for (int dimension = 0; dimension < arrayType.Rank; dimension++)
						newArrayType.Dimensions[dimension] = arrayType.Dimensions[dimension];
					return newArrayType;
				}
				var byReferenceType = ts as ByReferenceType;
				if (byReferenceType != null) {
					return new ByReferenceType(newElementType);
			}
				// TODO: should we throw an exception instead calling Resolve method?
				return ts.ResolveOrThrow();
			}

			public GenericContext<T2> ApplyTo<T2>(T2 item) where T2 : class
			{
				return new GenericContext<T2>(item, this.TypeArguments);
			}

			private class DummyGenericParameterProvider : IGenericParameterProvider
			{
				readonly Mono.Cecil.GenericParameterType type;
				readonly Mono.Collections.Generic.Collection<GenericParameter> parameters;

				public DummyGenericParameterProvider(bool methodTypeParameter)
				{
					type = methodTypeParameter ? Mono.Cecil.GenericParameterType.Method :
						Mono.Cecil.GenericParameterType.Type;
					parameters = new Mono.Collections.Generic.Collection<GenericParameter>(1);
					parameters.Add(new GenericParameter(this));
		}

				public GenericParameter DummyParameter
				{
					get { return parameters[0]; }
	}

				bool IGenericParameterProvider.HasGenericParameters
				{
					get { throw new NotImplementedException(); }
				}

				bool IGenericParameterProvider.IsDefinition
				{
					get { throw new NotImplementedException(); }
				}

				ModuleDefinition IGenericParameterProvider.Module
				{
					get { throw new NotImplementedException(); }
				}

				Mono.Collections.Generic.Collection<GenericParameter> IGenericParameterProvider.GenericParameters
				{
					get { return parameters; }
				}

				GenericParameterType IGenericParameterProvider.GenericParameterType
				{
					get { return type; }
				}

				MetadataToken IMetadataTokenProvider.MetadataToken
				{
					get
					{
						throw new NotImplementedException();
					}
					set
					{
						throw new NotImplementedException();
					}
				}
			}
		}
	}
}
