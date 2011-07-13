// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;

using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Converts from type system to the C# AST.
	/// </summary>
	public class TypeSystemAstBuilder
	{
		readonly CSharpResolver resolver;
		
		/// <summary>
		/// Creates a new TypeSystemAstBuilder.
		/// </summary>
		/// <param name="resolver">
		/// A resolver initialized for the position where the type will be inserted.
		/// </param>
		public TypeSystemAstBuilder(CSharpResolver resolver = null)
		{
			this.resolver = resolver;
		}
		
		public AstType ConvertType(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			TypeWithElementType typeWithElementType = type as TypeWithElementType;
			if (typeWithElementType != null) {
				if (typeWithElementType is PointerType) {
					return ConvertType(typeWithElementType.ElementType).MakePointerType();
				} else if (typeWithElementType is ArrayType) {
					return ConvertType(typeWithElementType.ElementType).MakeArrayType(((ArrayType)type).Dimensions);
				} else {
					// e.g. ByReferenceType; not supported as type in C#
					return ConvertType(typeWithElementType.ElementType);
				}
			}
			ParameterizedType pt = type as ParameterizedType;
			if (pt != null) {
				if (pt.Name == "Nullable" && pt.Namespace == "System" && pt.TypeParameterCount == 1) {
					return ConvertType(pt.TypeArguments[0]).MakeNullableType();
				}
				return ConvertTypeDefinition(pt.GetDefinition(), pt.TypeArguments);
			}
			ITypeDefinition typeDef = type as ITypeDefinition;
			if (typeDef != null) {
				if (typeDef.TypeParameterCount > 0) {
					// Create an unbound type
					IType[] typeArguments = new IType[typeDef.TypeParameterCount];
					for (int i = 0; i < typeArguments.Length; i++) {
						typeArguments[i] = SharedTypes.UnknownType;
					}
					return ConvertTypeDefinition(typeDef, typeArguments);
				} else {
					return ConvertTypeDefinition(typeDef, EmptyList<IType>.Instance);
				}
			}
			return new SimpleType(type.Name);
		}
		
		AstType ConvertTypeDefinition(ITypeDefinition typeDef, IList<IType> typeArguments)
		{
			Debug.Assert(typeArguments.Count >= typeDef.TypeParameterCount);
			switch (ReflectionHelper.GetTypeCode(typeDef)) {
				case TypeCode.Object:
					return new PrimitiveType("object");
				case TypeCode.Boolean:
					return new PrimitiveType("bool");
				case TypeCode.Char:
					return new PrimitiveType("char");
				case TypeCode.SByte:
					return new PrimitiveType("sbyte");
				case TypeCode.Byte:
					return new PrimitiveType("byte");
				case TypeCode.Int16:
					return new PrimitiveType("short");
				case TypeCode.UInt16:
					return new PrimitiveType("ushort");
				case TypeCode.Int32:
					return new PrimitiveType("int");
				case TypeCode.UInt32:
					return new PrimitiveType("uint");
				case TypeCode.Int64:
					return new PrimitiveType("long");
				case TypeCode.UInt64:
					return new PrimitiveType("ulong");
				case TypeCode.Single:
					return new PrimitiveType("float");
				case TypeCode.Double:
					return new PrimitiveType("double");
				case TypeCode.Decimal:
					return new PrimitiveType("decimal");
				case TypeCode.String:
					return new PrimitiveType("string");
			}
			// There is no type code for System.Void
			if (typeDef != null && typeDef.Namespace == "System" && typeDef.Name == "Void" && typeDef.TypeParameterCount == 0)
				return new PrimitiveType("void");
			if (resolver != null && TypeArgumentsTrivial(typeArguments, OuterTypeParameterCount(typeDef))) {
				TypeResolveResult trr = resolver.ResolveSimpleName(typeDef.Name, typeArguments) as TypeResolveResult;
				if (trr != null && !trr.IsError && trr.Type.GetDefinition() == typeDef) {
					// We can use the short type name
					SimpleType shortResult = new SimpleType(typeDef.Name);
					AddTypeArguments(shortResult, typeArguments, OuterTypeParameterCount(typeDef), typeDef.TypeParameterCount);
					return shortResult;
				}
			}
			
			MemberType result = new MemberType();
			if (typeDef.DeclaringTypeDefinition != null) {
				// Handle nested types
				result.Target = ConvertTypeDefinition(typeDef.DeclaringTypeDefinition, typeArguments);
			} else {
				// Handle top-level types
				if (string.IsNullOrEmpty(typeDef.Namespace)) {
					result.Target = new SimpleType("global");
					result.IsDoubleColon = true;
				} else {
					result.Target = ConvertNamespace(typeDef.Namespace);
				}
			}
			result.MemberName = typeDef.Name;
			AddTypeArguments(result, typeArguments, OuterTypeParameterCount(typeDef), typeDef.TypeParameterCount);
			return result;
		}
		
		/// <summary>
		/// Gets the number of type parameters belonging to outer classes.
		/// </summary>
		int OuterTypeParameterCount(ITypeDefinition typeDef)
		{
			if (typeDef.DeclaringType != null)
				return typeDef.DeclaringType.TypeParameterCount;
			else
				return 0;
		}
		
		/// <summary>
		/// Gets whether the first <paramref name="num"/> type arguments are trivial,
		/// that is, they point to a type parameter with the same index.
		/// </summary>
		bool TypeArgumentsTrivial(IList<IType> typeArguments, int num)
		{
			for (int i = 0; i < num; i++) {
				ITypeParameter tp = typeArguments[i] as ITypeParameter;
				if (!(tp != null && tp.OwnerType == EntityType.TypeDefinition && tp.Index == i))
					return false;
			}
			return true;
		}
		
		/// <summary>
		/// Adds type arguments to the result type.
		/// </summary>
		/// <param name="result">The result AST node (a SimpleType or MemberType)</param>
		/// <param name="typeArguments">The list of type arguments</param>
		/// <param name="startIndex">Index of first type argument to add</param>
		/// <param name="endIndex">Index after last type argument to add</param>
		void AddTypeArguments(AstType result, IList<IType> typeArguments, int startIndex, int endIndex)
		{
			for (int i = startIndex; i < endIndex; i++) {
				result.AddChild(ConvertType(typeArguments[i]), AstType.Roles.TypeArgument);
			}
		}
		
		AstType ConvertNamespace(string ns)
		{
			if (resolver != null) {
				// Look if there's an alias to the target namespace
				for (UsingScope usingScope = resolver.UsingScope; usingScope != null; usingScope = usingScope.Parent) {
					foreach (var pair in usingScope.UsingAliases) {
						// maybe add some caching? we're resolving all aliases N times when converting a namespace name with N parts
						NamespaceResolveResult nrr = pair.Value.ResolveNamespace(resolver.Context);
						if (nrr != null && nrr.NamespaceName == ns)
							return new SimpleType(pair.Key);
					}
				}
			}
			
			int pos = ns.LastIndexOf('.');
			if (pos < 0) {
				if (IsValidNamespace(ns)) {
					return new SimpleType(ns);
				} else {
					return new MemberType {
						Target = new SimpleType("global"),
						IsDoubleColon = true,
						MemberName = ns
					};
				}
			} else {
				string parentNamespace = ns.Substring(0, pos);
				string localNamespace = ns.Substring(pos + 1);
				return new MemberType {
					Target = ConvertNamespace(parentNamespace),
					MemberName = localNamespace
				};
			}
		}
		
		bool IsValidNamespace(string firstNamespacePart)
		{
			if (resolver == null)
				return true; // just assume namespaces are valid if we don't have a resolver
			NamespaceResolveResult nrr = resolver.ResolveSimpleName(firstNamespacePart, EmptyList<IType>.Instance) as NamespaceResolveResult;
			return nrr != null && !nrr.IsError && nrr.NamespaceName == firstNamespacePart;
		}
	}
}
