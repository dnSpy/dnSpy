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
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Static helper methods for reflection names.
	/// </summary>
	public static class ReflectionHelper
	{
		/// <summary>
		/// A reflection class used to represent <c>null</c>.
		/// </summary>
		public sealed class Null {}
		
		/// <summary>
		/// A reflection class used to represent <c>dynamic</c>.
		/// </summary>
		public sealed class Dynamic {}
		
		/// <summary>
		/// A reflection class used to represent an unbound type argument.
		/// </summary>
		public sealed class UnboundTypeArgument {}
		
		#region ITypeResolveContext.GetTypeDefinition(Type)
		/// <summary>
		/// Retrieves a type definition.
		/// </summary>
		/// <returns>Returns the type definition; or null if it is not found.</returns>
		/// <remarks>
		/// This method retrieves the type definition; consider using <code>type.ToTypeReference().Resolve(context)</code> instead
		/// if you need an <see cref="IType"/>.
		/// </remarks>
		public static ITypeDefinition GetTypeDefinition(this ITypeResolveContext context, Type type)
		{
			if (type == null)
				return null;
			while (type.IsArray || type.IsPointer || type.IsByRef)
				type = type.GetElementType();
			if (type.IsGenericType && !type.IsGenericTypeDefinition)
				type = type.GetGenericTypeDefinition();
			if (type.IsGenericParameter)
				return null;
			if (type.DeclaringType != null) {
				ITypeDefinition declaringType = GetTypeDefinition(context, type.DeclaringType);
				if (declaringType != null) {
					int typeParameterCount;
					string name = SplitTypeParameterCountFromReflectionName(type.Name, out typeParameterCount);
					typeParameterCount += declaringType.TypeParameterCount;
					foreach (ITypeDefinition nestedType in declaringType.NestedTypes) {
						if (nestedType.Name == name && nestedType.TypeParameterCount == typeParameterCount) {
							return nestedType;
						}
					}
				}
				return null;
			} else {
				int typeParameterCount;
				string name = SplitTypeParameterCountFromReflectionName(type.Name, out typeParameterCount);
				return context.GetTypeDefinition(type.Namespace, name, typeParameterCount, StringComparer.Ordinal);
			}
		}
		#endregion
		
		#region Type.ToTypeReference()
		/// <summary>
		/// Creates a reference to the specified type.
		/// </summary>
		/// <param name="type">The type to be converted.</param>
		/// <param name="entity">The parent entity, used to fetch the ITypeParameter for generic types.</param>
		/// <returns>Returns the type reference.</returns>
		public static ITypeReference ToTypeReference(this Type type, IEntity entity = null)
		{
			if (type == null)
				return SharedTypes.UnknownType;
			if (type.IsGenericType && !type.IsGenericTypeDefinition) {
				ITypeReference def = ToTypeReference(type.GetGenericTypeDefinition(), entity);
				Type[] arguments = type.GetGenericArguments();
				ITypeReference[] args = new ITypeReference[arguments.Length];
				bool allUnbound = true;
				for (int i = 0; i < arguments.Length; i++) {
					args[i] = ToTypeReference(arguments[i], entity);
					allUnbound &= args[i].Equals(SharedTypes.UnboundTypeArgument);
				}
				if (allUnbound)
					return def;
				else
					return new ParameterizedTypeReference(def, args);
			} else if (type.IsArray) {
				return new ArrayTypeReference(ToTypeReference(type.GetElementType(), entity), type.GetArrayRank());
			} else if (type.IsPointer) {
				return new PointerTypeReference(ToTypeReference(type.GetElementType(), entity));
			} else if (type.IsByRef) {
				return new ByReferenceTypeReference(ToTypeReference(type.GetElementType(), entity));
			} else if (type.IsGenericParameter) {
				if (type.DeclaringMethod != null) {
					IMethod method = entity as IMethod;
					if (method != null) {
						if (type.GenericParameterPosition < method.TypeParameters.Count) {
							return method.TypeParameters[type.GenericParameterPosition];
						}
					}
					return SharedTypes.UnknownType;
				} else {
					ITypeDefinition c = (entity as ITypeDefinition) ?? (entity != null ? entity.DeclaringTypeDefinition : null);
					if (c != null && type.GenericParameterPosition < c.TypeParameters.Count) {
						if (c.TypeParameters[type.GenericParameterPosition].Name == type.Name) {
							return c.TypeParameters[type.GenericParameterPosition];
						}
					}
					return SharedTypes.UnknownType;
				}
			} else if (type.DeclaringType != null) {
				if (type == typeof(Dynamic))
					return SharedTypes.Dynamic;
				else if (type == typeof(Null))
					return SharedTypes.Null;
				else if (type == typeof(UnboundTypeArgument))
					return SharedTypes.UnboundTypeArgument;
				ITypeReference baseTypeRef = ToTypeReference(type.DeclaringType, entity);
				int typeParameterCount;
				string name = SplitTypeParameterCountFromReflectionName(type.Name, out typeParameterCount);
				return new NestedTypeReference(baseTypeRef, name, typeParameterCount);
			} else {
				int typeParameterCount;
				string name = SplitTypeParameterCountFromReflectionName(type.Name, out typeParameterCount);
				return new GetClassTypeReference(type.Namespace, name, typeParameterCount);
			}
		}
		#endregion
		
		#region SplitTypeParameterCountFromReflectionName
		/// <summary>
		/// Removes the ` with type parameter count from the reflection name.
		/// </summary>
		/// <remarks>Do not use this method with the full name of inner classes.</remarks>
		public static string SplitTypeParameterCountFromReflectionName(string reflectionName)
		{
			int pos = reflectionName.LastIndexOf('`');
			if (pos < 0) {
				return reflectionName;
			} else {
				return reflectionName.Substring(0, pos);
			}
		}
		
		/// <summary>
		/// Removes the ` with type parameter count from the reflection name.
		/// </summary>
		/// <remarks>Do not use this method with the full name of inner classes.</remarks>
		public static string SplitTypeParameterCountFromReflectionName(string reflectionName, out int typeParameterCount)
		{
			int pos = reflectionName.LastIndexOf('`');
			if (pos < 0) {
				typeParameterCount = 0;
				return reflectionName;
			} else {
				string typeCount = reflectionName.Substring(pos + 1);
				if (int.TryParse(typeCount, out typeParameterCount))
					return reflectionName.Substring(0, pos);
				else
					return reflectionName;
			}
		}
		#endregion
		
		#region TypeCode.ToTypeReference()
		static readonly ITypeReference[] primitiveTypeReferences = {
			SharedTypes.UnknownType, // TypeCode.Empty
			KnownTypeReference.Object,
			new GetClassTypeReference("System", "DBNull", 0),
			KnownTypeReference.Boolean,
			KnownTypeReference.Char,
			KnownTypeReference.SByte,
			KnownTypeReference.Byte,
			KnownTypeReference.Int16,
			KnownTypeReference.UInt16,
			KnownTypeReference.Int32,
			KnownTypeReference.UInt32,
			KnownTypeReference.Int64,
			KnownTypeReference.UInt64,
			KnownTypeReference.Single,
			KnownTypeReference.Double,
			KnownTypeReference.Decimal,
			new GetClassTypeReference("System", "DateTime", 0),
			SharedTypes.UnknownType, // (TypeCode)17 has no enum value?
			KnownTypeReference.String
		};
		
		/// <summary>
		/// Creates a reference to the specified type.
		/// </summary>
		/// <param name="typeCode">The type to be converted.</param>
		/// <returns>Returns the type reference.</returns>
		public static ITypeReference ToTypeReference(this TypeCode typeCode)
		{
			return primitiveTypeReferences[(int)typeCode];
		}
		#endregion
		
		#region GetTypeCode
		static readonly string[] typeNamesByTypeCode = {
			"Void", "Object", "DBNull", "Boolean", "Char",
			"SByte", "Byte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64",
			"Single", "Double", "Decimal", "DateTime", null, "String"
		};
		
		static readonly string[] csharpTypeNamesByTypeCode = {
			"void", "object", null, "bool", "char",
			"sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong",
			"float", "double", "decimal", null, null, "string"
		};
		
		internal static int ByTypeCodeArraySize {
			get { return typeNamesByTypeCode.Length; }
		}
		
		public static string GetShortNameByTypeCode(TypeCode typeCode)
		{
			return typeNamesByTypeCode[(int)typeCode];
		}
		
		public static string GetCSharpNameByTypeCode(TypeCode typeCode)
		{
			return csharpTypeNamesByTypeCode[(int)typeCode];
		}
		
		/// <summary>
		/// Gets the type code for the specified type, or TypeCode.Empty if none of the other type codes matches.
		/// </summary>
		public static TypeCode GetTypeCode(IType type)
		{
			ITypeDefinition def = type as ITypeDefinition;
			if (def != null && def.TypeParameterCount == 0 && def.Namespace == "System") {
				string[] typeNames = typeNamesByTypeCode;
				string name = def.Name;
				for (int i = 1; i < typeNames.Length; i++) {
					if (name == typeNames[i])
						return (TypeCode)i;
				}
			}
			return TypeCode.Empty;
		}
		#endregion
		
		#region ParseReflectionName
		/// <summary>
		/// Parses a reflection name into a type reference.
		/// </summary>
		/// <param name="reflectionTypeName">The reflection name of the type.</param>
		/// <param name="parentEntity">Parent entity, used to find the type parameters for open types.
		/// If no entity is provided, type parameters are converted to <see cref="SharedTypes.UnknownType"/>.</param>
		/// <exception cref="ReflectionNameParseException">The syntax of the reflection type name is invalid</exception>
		/// <returns>A type reference that represents the reflection name.</returns>
		public static ITypeReference ParseReflectionName(string reflectionTypeName, IEntity parentEntity = null)
		{
			if (reflectionTypeName == null)
				throw new ArgumentNullException("reflectionTypeName");
			int pos = 0;
			ITypeReference r = ParseReflectionName(reflectionTypeName, ref pos, parentEntity);
			if (pos < reflectionTypeName.Length)
				throw new ReflectionNameParseException(pos, "Expected end of type name");
			return r;
		}
		
		static bool IsReflectionNameSpecialCharacter(char c)
		{
			switch (c) {
				case '+':
				case '`':
				case '[':
				case ']':
				case ',':
				case '*':
				case '&':
					return true;
				default:
					return false;
			}
		}
		
		static ITypeReference ParseReflectionName(string reflectionTypeName, ref int pos, IEntity entity)
		{
			if (pos == reflectionTypeName.Length)
				throw new ReflectionNameParseException(pos, "Unexpected end");
			if (reflectionTypeName[pos] == '`') {
				// type parameter reference
				pos++;
				if (pos == reflectionTypeName.Length)
					throw new ReflectionNameParseException(pos, "Unexpected end");
				if (reflectionTypeName[pos] == '`') {
					// method type parameter reference
					pos++;
					int index = ReadTypeParameterCount(reflectionTypeName, ref pos);
					IMethod method = entity as IMethod;
					if (method != null && index >= 0 && index < method.TypeParameters.Count)
						return method.TypeParameters[index];
					else
						return SharedTypes.UnknownType;
				} else {
					// class type parameter reference
					int index = ReadTypeParameterCount(reflectionTypeName, ref pos);
					ITypeDefinition c = (entity as ITypeDefinition) ?? (entity != null ? entity.DeclaringTypeDefinition : null);
					if (c != null && index >= 0 && index < c.TypeParameters.Count)
						return c.TypeParameters[index];
					else
						return SharedTypes.UnknownType;
				}
			}
			// not a type parameter reference: read the actual type name
			int tpc;
			string typeName = ReadTypeName(reflectionTypeName, ref pos, out tpc);
			ITypeReference reference = new GetClassTypeReference(typeName, tpc);
			// read type suffixes
			while (pos < reflectionTypeName.Length) {
				switch (reflectionTypeName[pos++]) {
					case '+':
						typeName = ReadTypeName(reflectionTypeName, ref pos, out tpc);
						reference = new NestedTypeReference(reference, typeName, tpc);
						break;
					case '*':
						reference = new PointerTypeReference(reference);
						break;
					case '&':
						reference = new ByReferenceTypeReference(reference);
						break;
					case '[':
						// this might be an array or a generic type
						if (pos == reflectionTypeName.Length)
							throw new ReflectionNameParseException(pos, "Unexpected end");
						if (reflectionTypeName[pos] == '[') {
							// it's a generic type
							List<ITypeReference> typeArguments = new List<ITypeReference>(tpc);
							pos++;
							typeArguments.Add(ParseReflectionName(reflectionTypeName, ref pos, entity));
							if (pos < reflectionTypeName.Length && reflectionTypeName[pos] == ']')
								pos++;
							else
								throw new ReflectionNameParseException(pos, "Expected end of type argument");
							
							while (pos < reflectionTypeName.Length && reflectionTypeName[pos] == ',') {
								pos++;
								if (pos < reflectionTypeName.Length && reflectionTypeName[pos] == '[')
									pos++;
								else
									throw new ReflectionNameParseException(pos, "Expected another type argument");
								
								typeArguments.Add(ParseReflectionName(reflectionTypeName, ref pos, entity));
								
								if (pos < reflectionTypeName.Length && reflectionTypeName[pos] == ']')
									pos++;
								else
									throw new ReflectionNameParseException(pos, "Expected end of type argument");
							}
							
							if (pos < reflectionTypeName.Length && reflectionTypeName[pos] == ']') {
								pos++;
								reference = new ParameterizedTypeReference(reference, typeArguments);
							} else {
								throw new ReflectionNameParseException(pos, "Expected end of generic type");
							}
						} else {
							// it's an array
							int dimensions = 1;
							while (pos < reflectionTypeName.Length && reflectionTypeName[pos] == ',') {
								dimensions++;
								pos++;
							}
							if (pos < reflectionTypeName.Length && reflectionTypeName[pos] == ']') {
								pos++; // end of array
								reference = new ArrayTypeReference(reference, dimensions);
							} else {
								throw new ReflectionNameParseException(pos, "Invalid array modifier");
							}
						}
						break;
					case ',':
						// assembly qualified name, ignore everything up to the end/next ']'
						while (pos < reflectionTypeName.Length && reflectionTypeName[pos] != ']')
							pos++;
						break;
					default:
						pos--; // reset pos to the character we couldn't read
						if (reflectionTypeName[pos] == ']')
							return reference; // return from a nested generic
						else
							throw new ReflectionNameParseException(pos, "Unexpected character: '" + reflectionTypeName[pos] + "'");
				}
			}
			return reference;
		}
		
		static string ReadTypeName(string reflectionTypeName, ref int pos, out int tpc)
		{
			int startPos = pos;
			// skip the simple name portion:
			while (pos < reflectionTypeName.Length && !IsReflectionNameSpecialCharacter(reflectionTypeName[pos]))
				pos++;
			if (pos == startPos)
				throw new ReflectionNameParseException(pos, "Expected type name");
			string typeName = reflectionTypeName.Substring(startPos, pos - startPos);
			if (pos < reflectionTypeName.Length && reflectionTypeName[pos] == '`') {
				pos++;
				tpc = ReadTypeParameterCount(reflectionTypeName, ref pos);
			} else {
				tpc = 0;
			}
			return typeName;
		}
		
		static int ReadTypeParameterCount(string reflectionTypeName, ref int pos)
		{
			int startPos = pos;
			while (pos < reflectionTypeName.Length) {
				char c = reflectionTypeName[pos];
				if (c < '0' || c > '9')
					break;
				pos++;
			}
			int tpc;
			if (!int.TryParse(reflectionTypeName.Substring(startPos, pos - startPos), out tpc))
				throw new ReflectionNameParseException(pos, "Expected type parameter count");
			return tpc;
		}
		#endregion
	}
}
