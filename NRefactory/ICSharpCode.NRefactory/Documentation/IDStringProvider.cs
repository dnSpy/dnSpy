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
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.Documentation
{
	/// <summary>
	/// Provides ID strings for entities. (C# 4.0 spec, §A.3.1)
	/// ID strings are used to identify members in XML documentation files.
	/// </summary>
	public static class IDStringProvider
	{
		/// <summary>
		/// Gets the ID string (C# 4.0 spec, §A.3.1) for the specified entity.
		/// </summary>
		/// <remarks>
		/// The type resolve context is optional and is not needed for entities loaded from assemblies:
		/// This method can get the ID string for any type reference produced by the CecilLoader without
		/// having to resolve the type reference.
		/// </remarks>
		public static string GetIDString(IEntity entity, ITypeResolveContext context = null)
		{
			StringBuilder b = new StringBuilder();
			switch (entity.EntityType) {
				case EntityType.TypeDefinition:
					b.Append("T:");
					AppendTypeName(b, (ITypeDefinition)entity);
					return b.ToString();
				case EntityType.Field:
					b.Append("F:");
					break;
				case EntityType.Property:
				case EntityType.Indexer:
					b.Append("P:");
					break;
				case EntityType.Event:
					b.Append("E:");
					break;
				default:
					b.Append("M:");
					break;
			}
			IMember member = (IMember)entity;
			AppendTypeName(b, member.DeclaringType);
			b.Append('.');
			b.Append(member.Name.Replace('.', '#'));
			IMethod method = member as IMethod;
			if (method != null && method.TypeParameters.Count > 0) {
				b.Append("``");
				b.Append(method.TypeParameters.Count);
			}
			IParameterizedMember parameterizedMember = member as IParameterizedMember;
			if (parameterizedMember != null && parameterizedMember.Parameters.Count > 0) {
				b.Append('(');
				var parameters = parameterizedMember.Parameters;
				for (int i = 0; i < parameters.Count; i++) {
					if (i > 0) b.Append(',');
					AppendTypeName(b, parameters[i].Type, context);
				}
				b.Append(')');
			}
			if (member.EntityType == EntityType.Operator && (member.Name == "op_Implicit" || member.Name == "op_Explicit")) {
				b.Append('~');
				AppendTypeName(b, member.ReturnType, context);
			}
			return b.ToString();
		}
		
		public static string GetTypeName(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			StringBuilder b = new StringBuilder();
			AppendTypeName(b, type);
			return b.ToString();
		}
		
		static void AppendTypeName(StringBuilder b, IType type)
		{
			switch (type.Kind) {
				case TypeKind.Dynamic:
					b.Append("System.Object");
					break;
				case TypeKind.TypeParameter:
					ITypeParameter tp = (ITypeParameter)type;
					b.Append('`');
					if (tp.OwnerType == EntityType.Method)
						b.Append('`');
					b.Append(tp.Index);
					break;
				case TypeKind.Array:
					ArrayType array = (ArrayType)type;
					AppendTypeName(b, array.ElementType);
					b.Append('[');
					if (array.Dimensions > 1) {
						for (int i = 0; i < array.Dimensions; i++) {
							if (i > 0) b.Append(',');
							b.Append("0:");
						}
					}
					b.Append(']');
					break;
				case TypeKind.Pointer:
					AppendTypeName(b, ((PointerType)type).ElementType);
					b.Append('*');
					break;
				case TypeKind.ByReference:
					AppendTypeName(b, ((ByReferenceType)type).ElementType);
					b.Append('@');
					break;
				default:
					IType declType = type.DeclaringType;
					if (declType != null) {
						AppendTypeName(b, declType);
						b.Append('.');
						b.Append(type.Name);
						AppendTypeParameters(b, type, declType.TypeParameterCount);
					} else {
						b.Append(type.FullName);
						AppendTypeParameters(b, type, 0);
					}
					break;
			}
		}
		
		static void AppendTypeParameters(StringBuilder b, IType type, int outerTypeParameterCount)
		{
			int tpc = type.TypeParameterCount - outerTypeParameterCount;
			if (tpc > 0) {
				ParameterizedType pt = type as ParameterizedType;
				if (pt != null) {
					b.Append('{');
					var ta = pt.TypeArguments;
					for (int i = outerTypeParameterCount; i < ta.Count; i++) {
						if (i > outerTypeParameterCount) b.Append(',');
						AppendTypeName(b, ta[i]);
					}
					b.Append('}');
				} else {
					b.Append('`');
					b.Append(tpc);
				}
			}
		}
		
		static void AppendTypeName(StringBuilder b, ITypeReference type, ITypeResolveContext context)
		{
			IType resolvedType = type as IType;
			if (resolvedType != null) {
				AppendTypeName(b, resolvedType);
				return;
			}
			KnownTypeReference knownType = type as KnownTypeReference;
			if (knownType != null) {
				if (!string.IsNullOrEmpty(knownType.Namespace)) {
					b.Append(knownType.Namespace);
					b.Append('.');
				}
				b.Append(knownType.Name);
				return;
			}
			GetClassTypeReference gctr = type as GetClassTypeReference;
			if (gctr != null) {
				if (!string.IsNullOrEmpty(gctr.Namespace)) {
					b.Append(gctr.Namespace);
					b.Append('.');
				}
				b.Append(gctr.Name);
				if (gctr.TypeParameterCount > 0) {
					b.Append('`');
					b.Append(gctr.TypeParameterCount);
				}
				return;
			}
			NestedTypeReference ntr = type as NestedTypeReference;
			if (ntr != null) {
				AppendTypeName(b, ntr.DeclaringTypeReference, context);
				b.Append('.');
				b.Append(ntr.Name);
				if (ntr.AdditionalTypeParameterCount > 0) {
					b.Append('`');
					b.Append(ntr.AdditionalTypeParameterCount);
				}
				return;
			}
			ParameterizedTypeReference pt = type as ParameterizedTypeReference;
			if (pt != null && IsGetClassTypeReference(pt.GenericType)) {
				AppendParameterizedTypeName(b, pt.GenericType, pt.TypeArguments, context);
				return;
			}
			ArrayTypeReference array = type as ArrayTypeReference;
			if (array != null) {
				AppendTypeName(b, array.ElementType, context);
				b.Append('[');
				if (array.Dimensions > 1) {
					for (int i = 0; i < array.Dimensions; i++) {
						if (i > 0) b.Append(',');
						b.Append("0:");
					}
				}
				b.Append(']');
				return;
			}
			PointerTypeReference ptr = type as PointerTypeReference;
			if (ptr != null) {
				AppendTypeName(b, ptr.ElementType, context);
				b.Append('*');
				return;
			}
			ByReferenceTypeReference brtr = type as ByReferenceTypeReference;
			if (brtr != null) {
				AppendTypeName(b, brtr.ElementType, context);
				b.Append('@');
				return;
			}
			if (context == null)
				b.Append('?');
			else
				AppendTypeName(b, type.Resolve(context));
		}
		
		static bool IsGetClassTypeReference(ITypeReference type)
		{
			NestedTypeReference ntr;
			while ((ntr = type as NestedTypeReference) != null)
				type = ntr.DeclaringTypeReference;
			return type is GetClassTypeReference;
		}
		
		static int AppendParameterizedTypeName(StringBuilder b, ITypeReference type, IList<ITypeReference> typeArguments, ITypeResolveContext context)
		{
			GetClassTypeReference gctr = type as GetClassTypeReference;
			if (gctr != null) {
				if (!string.IsNullOrEmpty(gctr.Namespace)) {
					b.Append(gctr.Namespace);
					b.Append('.');
				}
				b.Append(gctr.Name);
				if (gctr.TypeParameterCount > 0) {
					b.Append('{');
					for (int i = 0; i < gctr.TypeParameterCount && i < typeArguments.Count; i++) {
						if (i > 0) b.Append(',');
						AppendTypeName(b, typeArguments[i], context);
					}
					b.Append('}');
				}
				return gctr.TypeParameterCount;
			} else {
				NestedTypeReference ntr = (NestedTypeReference)type;
				int outerTpc = AppendParameterizedTypeName(b, ntr.DeclaringTypeReference, typeArguments, context);
				b.Append('.');
				if (ntr.AdditionalTypeParameterCount > 0) {
					b.Append('{');
					for (int i = 0; i < ntr.AdditionalTypeParameterCount && i + outerTpc < typeArguments.Count; i++) {
						if (i > 0) b.Append(',');
						AppendTypeName(b, typeArguments[i + outerTpc], context);
					}
					b.Append('}');
				}
				return outerTpc + ntr.AdditionalTypeParameterCount;
			}
		}
	}
}
