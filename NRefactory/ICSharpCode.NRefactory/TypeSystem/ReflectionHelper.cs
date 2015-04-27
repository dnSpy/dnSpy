// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
		
		#region ICompilation.FindType
		/// <summary>
		/// Retrieves the specified type in this compilation.
		/// Returns <see cref="SpecialType.UnknownType"/> if the type cannot be found in this compilation.
		/// </summary>
		/// <remarks>
		/// This method cannot be used with open types; all type parameters will be substituted
		/// with <see cref="SpecialType.UnknownType"/>.
		/// </remarks>
		public static IType FindType(this ICompilation compilation, Type type)
		{
			return type.ToTypeReference().Resolve(compilation.TypeResolveContext);
		}
		#endregion
		
		#region Type.ToTypeReference()
		/// <summary>
		/// Creates a reference to the specified type.
		/// </summary>
		/// <param name="type">The type to be converted.</param>
		/// <returns>Returns the type reference.</returns>
		/// <remarks>
		/// If the type is open (contains type parameters '`0' or '``0'),
		/// an <see cref="ITypeResolveContext"/> with the appropriate CurrentTypeDefinition/CurrentMember is required
		/// to resolve the type reference.
		/// For closed types, the root type resolve context for the compilation is sufficient.
		/// </remarks>
		public static ITypeReference ToTypeReference(this Type type)
		{
			if (type == null)
				return SpecialType.UnknownType;
			if (type.IsGenericType && !type.IsGenericTypeDefinition) {
				ITypeReference def = ToTypeReference(type.GetGenericTypeDefinition());
				Type[] arguments = type.GetGenericArguments();
				ITypeReference[] args = new ITypeReference[arguments.Length];
				bool allUnbound = true;
				for (int i = 0; i < arguments.Length; i++) {
					args[i] = ToTypeReference(arguments[i]);
					allUnbound &= args[i].Equals(SpecialType.UnboundTypeArgument);
				}
				if (allUnbound)
					return def;
				else
					return new ParameterizedTypeReference(def, args);
			} else if (type.IsArray) {
				return new ArrayTypeReference(ToTypeReference(type.GetElementType()), type.GetArrayRank());
			} else if (type.IsPointer) {
				return new PointerTypeReference(ToTypeReference(type.GetElementType()));
			} else if (type.IsByRef) {
				return new ByReferenceTypeReference(ToTypeReference(type.GetElementType()));
			} else if (type.IsGenericParameter) {
				if (type.DeclaringMethod != null) {
					return TypeParameterReference.Create(SymbolKind.Method, type.GenericParameterPosition);
				} else {
					return TypeParameterReference.Create(SymbolKind.TypeDefinition, type.GenericParameterPosition);
				}
			} else if (type.DeclaringType != null) {
				if (type == typeof(Dynamic))
					return SpecialType.Dynamic;
				else if (type == typeof(Null))
					return SpecialType.NullType;
				else if (type == typeof(UnboundTypeArgument))
					return SpecialType.UnboundTypeArgument;
				ITypeReference baseTypeRef = ToTypeReference(type.DeclaringType);
				int typeParameterCount;
				string name = SplitTypeParameterCountFromReflectionName(type.Name, out typeParameterCount);
				return new NestedTypeReference(baseTypeRef, name, typeParameterCount);
			} else {
				IAssemblyReference assemblyReference = new DefaultAssemblyReference(type.Assembly.FullName);
				int typeParameterCount;
				string name = SplitTypeParameterCountFromReflectionName(type.Name, out typeParameterCount);
				return new GetClassTypeReference(assemblyReference, type.Namespace, name, typeParameterCount);
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
		
		#region TypeCode support
		/// <summary>
		/// Retrieves a built-in type using the specified type code.
		/// </summary>
		public static IType FindType(this ICompilation compilation, TypeCode typeCode)
		{
			return compilation.FindType((KnownTypeCode)typeCode);
		}
		
		/// <summary>
		/// Creates a reference to the specified type.
		/// </summary>
		/// <param name="typeCode">The type to be converted.</param>
		/// <returns>Returns the type reference.</returns>
		public static ITypeReference ToTypeReference(this TypeCode typeCode)
		{
			return KnownTypeReference.Get((KnownTypeCode)typeCode);
		}
		
		/// <summary>
		/// Gets the type code for the specified type, or TypeCode.Empty if none of the other type codes match.
		/// </summary>
		public static TypeCode GetTypeCode(IType type)
		{
			ITypeDefinition def = type as ITypeDefinition;
			if (def != null) {
				KnownTypeCode typeCode = def.KnownTypeCode;
				if (typeCode <= KnownTypeCode.String && typeCode != KnownTypeCode.Void)
					return (TypeCode)typeCode;
				else
					return TypeCode.Empty;
			}
			return TypeCode.Empty;
		}
		#endregion
		
		#region ParseReflectionName
		/// <summary>
		/// Parses a reflection name into a type reference.
		/// </summary>
		/// <param name="reflectionTypeName">The reflection name of the type.</param>
		/// <returns>A type reference that represents the reflection name.</returns>
		/// <exception cref="ReflectionNameParseException">The syntax of the reflection type name is invalid</exception>
		/// <remarks>
		/// If the type is open (contains type parameters '`0' or '``0'),
		/// an <see cref="ITypeResolveContext"/> with the appropriate CurrentTypeDefinition/CurrentMember is required
		/// to resolve the reference to the ITypeParameter.
		/// For looking up closed, assembly qualified type names, the root type resolve context for the compilation
		/// is sufficient.
		/// When looking up a type name that isn't assembly qualified, the type reference will look in
		/// <see cref="ITypeResolveContext.CurrentAssembly"/> first, and if the type is not found there,
		/// it will look in all other assemblies of the compilation.
		/// </remarks>
		/// <seealso cref="FullTypeName(string)"/>
		public static ITypeReference ParseReflectionName(string reflectionTypeName)
		{
			if (reflectionTypeName == null)
				throw new ArgumentNullException("reflectionTypeName");
			int pos = 0;
			ITypeReference r = ParseReflectionName(reflectionTypeName, ref pos);
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
		
		static ITypeReference ParseReflectionName(string reflectionTypeName, ref int pos)
		{
			if (pos == reflectionTypeName.Length)
				throw new ReflectionNameParseException(pos, "Unexpected end");
			ITypeReference reference;
			if (reflectionTypeName[pos] == '`') {
				// type parameter reference
				pos++;
				if (pos == reflectionTypeName.Length)
					throw new ReflectionNameParseException(pos, "Unexpected end");
				if (reflectionTypeName[pos] == '`') {
					// method type parameter reference
					pos++;
					int index = ReadTypeParameterCount(reflectionTypeName, ref pos);
					reference = TypeParameterReference.Create(SymbolKind.Method, index);
				} else {
					// class type parameter reference
					int index = ReadTypeParameterCount(reflectionTypeName, ref pos);
					reference = TypeParameterReference.Create(SymbolKind.TypeDefinition, index);
				}
			} else {
				// not a type parameter reference: read the actual type name
				int tpc;
				string typeName = ReadTypeName(reflectionTypeName, ref pos, out tpc);
				string assemblyName = SkipAheadAndReadAssemblyName(reflectionTypeName, pos);
				reference = CreateGetClassTypeReference(assemblyName, typeName, tpc);
			}
			// read type suffixes
			while (pos < reflectionTypeName.Length) {
				switch (reflectionTypeName[pos++]) {
					case '+':
						int tpc;
						string typeName = ReadTypeName(reflectionTypeName, ref pos, out tpc);
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
							List<ITypeReference> typeArguments = new List<ITypeReference>();
							pos++;
							typeArguments.Add(ParseReflectionName(reflectionTypeName, ref pos));
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
								
								typeArguments.Add(ParseReflectionName(reflectionTypeName, ref pos));
								
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
		
		static ITypeReference CreateGetClassTypeReference(string assemblyName, string typeName, int tpc)
		{
			IAssemblyReference assemblyReference;
			if (assemblyName != null) {
				assemblyReference = new DefaultAssemblyReference(assemblyName);
			} else {
				assemblyReference = null;
			}
			int pos = typeName.LastIndexOf('.');
			if (pos < 0)
				return new GetClassTypeReference(assemblyReference, string.Empty, typeName, tpc);
			else
				return new GetClassTypeReference(assemblyReference, typeName.Substring(0, pos), typeName.Substring(pos + 1), tpc);
		}
		
		static string SkipAheadAndReadAssemblyName(string reflectionTypeName, int pos)
		{
			int nestingLevel = 0;
			while (pos < reflectionTypeName.Length) {
				switch (reflectionTypeName[pos++]) {
					case '[':
						nestingLevel++;
						break;
					case ']':
						if (nestingLevel == 0)
							return null;
						nestingLevel--;
						break;
					case ',':
						if (nestingLevel == 0) {
							// first skip the whitespace
							while (pos < reflectionTypeName.Length && reflectionTypeName[pos] == ' ')
								pos++;
							// everything up to the end/next ']' is the assembly name
							int endPos = pos;
							while (endPos < reflectionTypeName.Length && reflectionTypeName[endPos] != ']')
								endPos++;
							return reflectionTypeName.Substring(pos, endPos - pos);
						}
						break;
				}
			}
			return null;
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
		
		internal static int ReadTypeParameterCount(string reflectionTypeName, ref int pos)
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
