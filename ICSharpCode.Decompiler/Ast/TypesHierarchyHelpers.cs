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
				var comparableBaseType = baseType.ResolveOrThrow();
				while (derivedType.BaseType != null) {
					var resolvedBaseType = derivedType.BaseType.ResolveOrThrow();
					if (resolvedBaseType == null)
						return false;
					if (comparableBaseType == resolvedBaseType)
						return true;
					derivedType = resolvedBaseType;
				}
				return false;
			}
		}

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

		public static IEnumerable<MethodDefinition> FindBaseMethods(MethodDefinition method)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			var typeContext = CreateGenericContext(method.DeclaringType);
			var gMethod = typeContext.ApplyTo(method);

			foreach (var baseType in BaseTypes(method.DeclaringType))
				foreach (var baseMethod in baseType.Item.Methods)
					if (MatchMethod(baseType.ApplyTo(baseMethod), gMethod) && IsVisbleFrom(baseMethod, method)) {
						yield return baseMethod;
						if (!(baseMethod.IsNewSlot ^ baseMethod.IsVirtual))
							yield break;
					}
		}

		public static IEnumerable<PropertyDefinition> FindBaseProperties(PropertyDefinition property, bool ignoreResolveExceptions = false)
		{
			if (property == null)
				throw new ArgumentNullException("property");

			var typeContext = CreateGenericContext(property.DeclaringType);
			var gProperty = typeContext.ApplyTo(property);

			foreach (var baseType in BaseTypes(property.DeclaringType))
				foreach (var baseProperty in baseType.Item.Properties)
					if (MatchProperty(baseType.ApplyTo(baseProperty), gProperty) && IsVisbleFrom(baseProperty, property)) {
						yield return baseProperty;
						var anyPropertyAccessor = baseProperty.GetMethod ?? baseProperty.SetMethod;
						if (!(anyPropertyAccessor.IsNewSlot ^ anyPropertyAccessor.IsVirtual))
							yield break;
					}

		}

		private static bool IsVisbleFrom(MethodDefinition baseCandidate, MethodDefinition method)
		{
			if (baseCandidate.IsPrivate)
				return false;
			if ((baseCandidate.IsAssembly || baseCandidate.IsFamilyAndAssembly) && baseCandidate.Module != method.Module)
				return false;
			return true;
		}

		private static bool IsVisbleFrom(PropertyDefinition baseCandidate, PropertyDefinition property)
		{
			if (baseCandidate.GetMethod != null && property.GetMethod != null && IsVisbleFrom(baseCandidate.GetMethod, property.GetMethod))
				return true;
			if (baseCandidate.SetMethod != null && property.SetMethod != null && IsVisbleFrom(baseCandidate.SetMethod, property.SetMethod))
				return true;
			return false;
		}

		private static bool MatchMethod(GenericContext<MethodDefinition> candidate, GenericContext<MethodDefinition> method)
		{
			var mCandidate = candidate.Item;
			var mMethod = method.Item;
			if (mCandidate.Name != mMethod.Name)
				return false;

			if (mCandidate.HasOverrides)
				return false;

			if (!IsSameType(candidate.ResolveWithContext(mCandidate.ReturnType), method.ResolveWithContext(mMethod.ReturnType)))
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

		private static bool MatchProperty(GenericContext<PropertyDefinition> candidate, GenericContext<PropertyDefinition> property)
		{
			var mCandidate = candidate.Item;
			var mProperty = property.Item;
			if (mCandidate.Name != mProperty.Name)
				return false;

			if ((mCandidate.GetMethod ?? mCandidate.SetMethod).HasOverrides)
				return false;

			if (!IsSameType(candidate.ResolveWithContext(mCandidate.PropertyType), property.ResolveWithContext(mProperty.PropertyType)))
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

		private static bool MatchParameters(GenericContext<ParameterDefinition> baseParameterType, GenericContext<ParameterDefinition> parameterType)
		{
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
				if (genericParameter != null && genericParameter.Owner.GenericParameterType == GenericParameterType.Type) {
					return this.TypeArguments[genericParameter.Position];
				}
				var arrayType = type as ArrayType;
				if (arrayType != null) {
					var resolvedElementType = ResolveWithContext(arrayType.ElementType);
					if (resolvedElementType == null)
						return null;
					if (resolvedElementType == arrayType.ElementType)
						return arrayType;
					var newArrayType = new ArrayType(resolvedElementType, arrayType.Rank);
					for (int dimension = 0; dimension < arrayType.Rank; dimension++)
						newArrayType.Dimensions[dimension] = arrayType.Dimensions[dimension];
					return newArrayType;
				}
				return type.ResolveOrThrow();
			}

			public GenericContext<T2> ApplyTo<T2>(T2 item) where T2 : class
			{
				return new GenericContext<T2>(item, this.TypeArguments);
			}
		}
	}
}
