// 
// TypeCompatibilityHelper.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	class TypeCompatibilityHelper
	{
		public enum TypeCompatiblity
		{
			MayOfProvidedType,
			AlwaysOfProvidedType,
			NeverOfProvidedType
		}

		static bool CheckTypeParameterConstraints (IType type, IEnumerable<IType> baseTypes,
				ITypeParameter typeParameter)
		{
			if (!typeParameter.DirectBaseTypes.All (t => baseTypes.Any (t2 => t2.Equals (t))))
				return false;
			if (typeParameter.HasDefaultConstructorConstraint &&
				!type.GetConstructors (c => c.IsPublic && c.Parameters.Count == 0).Any ())
				return false;
			return true;
		}

		public static TypeCompatiblity CheckTypeCompatibility(IType exprType, IType providedType)
		{
			var exprBaseTypes = exprType.GetAllBaseTypes ().ToArray ();

			// providedType is a base type of exprType
			if (exprBaseTypes.Any (t => t.Equals (providedType)))
				return TypeCompatiblity.AlwaysOfProvidedType;

			if ((exprType.IsReferenceType == true && providedType.IsReferenceType == false) ||
			    (exprType.IsReferenceType == false && providedType.IsReferenceType == true))
				return TypeCompatiblity.NeverOfProvidedType;

			var typeParameter = exprType as ITypeParameter;
			var providedTypeParameter = providedType as ITypeParameter;

			if (typeParameter != null) {
				// check if providedType can be a derived type
				var providedBaseTypes = providedType.GetAllBaseTypes ().ToArray ();
				var providedTypeDef = providedType.GetDefinition ();
				// if providedType is sealed, check if it fullfills all the type parameter constraints,
				// otherwise, only check if it is derived from EffectiveBaseClass
				if (providedTypeParameter == null && (providedTypeDef == null || providedTypeDef.IsSealed)) {
					if (CheckTypeParameterConstraints (providedType, providedBaseTypes, typeParameter))
						return TypeCompatiblity.MayOfProvidedType;
				} else if (providedBaseTypes.Any (t => t.Equals (typeParameter.EffectiveBaseClass))) {
					return TypeCompatiblity.MayOfProvidedType;
				}

				// if providedType is also a type parameter, check if base classes are compatible
				if (providedTypeParameter != null &&
					exprBaseTypes.Any (t => t.Equals (providedTypeParameter.EffectiveBaseClass)))
					return TypeCompatiblity.MayOfProvidedType;

				return TypeCompatiblity.NeverOfProvidedType;
			}
			// check if exprType fullfills all the type parameter constraints
			if (providedTypeParameter != null &&
				CheckTypeParameterConstraints (exprType, exprBaseTypes, providedTypeParameter))
				return TypeCompatiblity.MayOfProvidedType;

			switch (exprType.Kind) {
				case TypeKind.Class:
					var exprTypeDef = exprType.GetDefinition ();
					if (exprTypeDef == null)
						return TypeCompatiblity.MayOfProvidedType;
					// exprType is sealed, but providedType is not a base type of it or it does not
					// fullfill all the type parameter constraints
					if (exprTypeDef.IsSealed)
						break;

					// check if providedType can be a derived type
					if (providedType.Kind == TypeKind.Interface ||
						providedType.GetAllBaseTypes ().Any (t => t.Equals (exprType)))
						return TypeCompatiblity.MayOfProvidedType;

					if (providedTypeParameter != null &&
						exprBaseTypes.Any (t => t.Equals (providedTypeParameter.EffectiveBaseClass)))
						return TypeCompatiblity.MayOfProvidedType;

					break;

				case TypeKind.Struct:
				case TypeKind.Delegate:
				case TypeKind.Enum:
				case TypeKind.Array:
				case TypeKind.Anonymous:
				case TypeKind.Null:
					break;

				default:
					return TypeCompatiblity.MayOfProvidedType;
			}
			return TypeCompatiblity.NeverOfProvidedType;
		}
	}
}
