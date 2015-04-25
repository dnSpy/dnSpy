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
using System.Linq;
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
	public static class IdStringProvider
	{
		#region GetIdString
		/// <summary>
		/// Gets the ID string (C# 4.0 spec, §A.3.1) for the specified entity.
		/// </summary>
		public static string GetIdString(this IEntity entity)
		{
			StringBuilder b = new StringBuilder();
			switch (entity.SymbolKind) {
				case SymbolKind.TypeDefinition:
					b.Append("T:");
					AppendTypeName(b, (ITypeDefinition)entity, false);
					return b.ToString();
				case SymbolKind.Field:
					b.Append("F:");
					break;
				case SymbolKind.Property:
				case SymbolKind.Indexer:
					b.Append("P:");
					break;
				case SymbolKind.Event:
					b.Append("E:");
					break;
				default:
					b.Append("M:");
					break;
			}
			IMember member = (IMember)entity;
			AppendTypeName(b, member.DeclaringType, false);
			b.Append('.');
			if (member.IsExplicitInterfaceImplementation && member.Name.IndexOf('.') < 0 && member.ImplementedInterfaceMembers.Count == 1) {
				AppendTypeName(b, member.ImplementedInterfaceMembers[0].DeclaringType, true);
				b.Append('#');
			}
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
					AppendTypeName(b, parameters[i].Type, false);
				}
				b.Append(')');
			}
			if (member.SymbolKind == SymbolKind.Operator && (member.Name == "op_Implicit" || member.Name == "op_Explicit")) {
				b.Append('~');
				AppendTypeName(b, member.ReturnType, false);
			}
			return b.ToString();
		}
		#endregion
		
		#region GetTypeName
		public static string GetTypeName(IType type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			StringBuilder b = new StringBuilder();
			AppendTypeName(b, type, false);
			return b.ToString();
		}
		
		static void AppendTypeName(StringBuilder b, IType type, bool explicitInterfaceImpl)
		{
			switch (type.Kind) {
				case TypeKind.Dynamic:
					b.Append(explicitInterfaceImpl ? "System#Object" : "System.Object");
					break;
				case TypeKind.TypeParameter:
					ITypeParameter tp = (ITypeParameter)type;
					if (explicitInterfaceImpl) {
						b.Append(tp.Name);
					} else {
						b.Append('`');
						if (tp.OwnerType == SymbolKind.Method)
							b.Append('`');
						b.Append(tp.Index);
					}
					break;
				case TypeKind.Array:
					ArrayType array = (ArrayType)type;
					AppendTypeName(b, array.ElementType, explicitInterfaceImpl);
					b.Append('[');
					if (array.Dimensions > 1) {
						for (int i = 0; i < array.Dimensions; i++) {
							if (i > 0)
								b.Append(explicitInterfaceImpl ? '@' : ',');
							if (!explicitInterfaceImpl)
								b.Append("0:");
						}
					}
					b.Append(']');
					break;
				case TypeKind.Pointer:
					AppendTypeName(b, ((PointerType)type).ElementType, explicitInterfaceImpl);
					b.Append('*');
					break;
				case TypeKind.ByReference:
					AppendTypeName(b, ((ByReferenceType)type).ElementType, explicitInterfaceImpl);
					b.Append('@');
					break;
				default:
					IType declType = type.DeclaringType;
					if (declType != null) {
						AppendTypeName(b, declType, explicitInterfaceImpl);
						b.Append(explicitInterfaceImpl ? '#' : '.');
						b.Append(type.Name);
						AppendTypeParameters(b, type, declType.TypeParameterCount, explicitInterfaceImpl);
					} else {
						if (explicitInterfaceImpl)
							b.Append(type.FullName.Replace('.', '#'));
						else
							b.Append(type.FullName);
						AppendTypeParameters(b, type, 0, explicitInterfaceImpl);
					}
					break;
			}
		}
		
		static void AppendTypeParameters(StringBuilder b, IType type, int outerTypeParameterCount, bool explicitInterfaceImpl)
		{
			int tpc = type.TypeParameterCount - outerTypeParameterCount;
			if (tpc > 0) {
				ParameterizedType pt = type as ParameterizedType;
				if (pt != null) {
					b.Append('{');
					var ta = pt.TypeArguments;
					for (int i = outerTypeParameterCount; i < ta.Count; i++) {
						if (i > outerTypeParameterCount)
							b.Append(explicitInterfaceImpl ? '@' : ',');
						AppendTypeName(b, ta[i], explicitInterfaceImpl);
					}
					b.Append('}');
				} else {
					b.Append('`');
					b.Append(tpc);
				}
			}
		}
		#endregion
		
		#region ParseMemberName
		/// <summary>
		/// Parse the ID string into a member reference.
		/// </summary>
		/// <param name="memberIdString">The ID string representing the member (with "M:", "F:", "P:" or "E:" prefix).</param>
		/// <returns>A member reference that represents the ID string.</returns>
		/// <exception cref="ReflectionNameParseException">The syntax of the ID string is invalid</exception>
		/// <remarks>
		/// The member reference will look in <see cref="ITypeResolveContext.CurrentAssembly"/> first,
		/// and if the member is not found there,
		/// it will look in all other assemblies of the compilation.
		/// </remarks>
		public static IMemberReference ParseMemberIdString(string memberIdString)
		{
			if (memberIdString == null)
				throw new ArgumentNullException("memberIdString");
			if (memberIdString.Length < 2 || memberIdString[1] != ':')
				throw new ReflectionNameParseException(0, "Missing type tag");
			char typeChar = memberIdString[0];
			int parenPos = memberIdString.IndexOf('(');
			if (parenPos < 0)
				parenPos = memberIdString.LastIndexOf('~');
			if (parenPos < 0)
				parenPos = memberIdString.Length;
			int dotPos = memberIdString.LastIndexOf('.', parenPos - 1);
			if (dotPos < 0)
				throw new ReflectionNameParseException(0, "Could not find '.' separating type name from member name");
			string typeName = memberIdString.Substring(0, dotPos);
			int pos = 2;
			ITypeReference typeReference = ParseTypeName(typeName, ref pos);
			if (pos != typeName.Length)
				throw new ReflectionNameParseException(pos, "Expected end of type name");
//			string memberName = memberIDString.Substring(dotPos + 1, parenPos - (dotPos + 1));
//			pos = memberName.LastIndexOf("``");
//			if (pos > 0)
//				memberName = memberName.Substring(0, pos);
//			memberName = memberName.Replace('#', '.');
			return new IdStringMemberReference(typeReference, typeChar, memberIdString);
		}
		#endregion
		
		#region ParseTypeName
		/// <summary>
		/// Parse the ID string type name into a type reference.
		/// </summary>
		/// <param name="typeName">The ID string representing the type (the "T:" prefix is optional).</param>
		/// <returns>A type reference that represents the ID string.</returns>
		/// <exception cref="ReflectionNameParseException">The syntax of the ID string is invalid</exception>
		/// <remarks>
		/// <para>
		/// The type reference will look in <see cref="ITypeResolveContext.CurrentAssembly"/> first,
		/// and if the type is not found there,
		/// it will look in all other assemblies of the compilation.
		/// </para>
		/// <para>
		/// If the type is open (contains type parameters '`0' or '``0'),
		/// an <see cref="ITypeResolveContext"/> with the appropriate CurrentTypeDefinition/CurrentMember is required
		/// to resolve the reference to the ITypeParameter.
		/// </para>
		/// </remarks>
		public static ITypeReference ParseTypeName(string typeName)
		{
			if (typeName == null)
				throw new ArgumentNullException("typeName");
			int pos = 0;
			if (typeName.StartsWith("T:", StringComparison.Ordinal))
				pos = 2;
			ITypeReference r = ParseTypeName(typeName, ref pos);
			if (pos < typeName.Length)
				throw new ReflectionNameParseException(pos, "Expected end of type name");
			return r;
		}
		
		static bool IsIDStringSpecialCharacter(char c)
		{
			switch (c) {
				case ':':
				case '{':
				case '}':
				case '[':
				case ']':
				case '(':
				case ')':
				case '`':
				case '*':
				case '@':
				case ',':
					return true;
				default:
					return false;
			}
		}
		
		static ITypeReference ParseTypeName(string typeName, ref int pos)
		{
			string reflectionTypeName = typeName;
			if (pos == typeName.Length)
				throw new ReflectionNameParseException(pos, "Unexpected end");
			ITypeReference result;
			if (reflectionTypeName[pos] == '`') {
				// type parameter reference
				pos++;
				if (pos == reflectionTypeName.Length)
					throw new ReflectionNameParseException(pos, "Unexpected end");
				if (reflectionTypeName[pos] == '`') {
					// method type parameter reference
					pos++;
					int index = ReflectionHelper.ReadTypeParameterCount(reflectionTypeName, ref pos);
					result = TypeParameterReference.Create(SymbolKind.Method, index);
				} else {
					// class type parameter reference
					int index = ReflectionHelper.ReadTypeParameterCount(reflectionTypeName, ref pos);
					result = TypeParameterReference.Create(SymbolKind.TypeDefinition, index);
				}
			} else {
				// not a type parameter reference: read the actual type name
				List<ITypeReference> typeArguments = new List<ITypeReference>();
				int typeParameterCount;
				string typeNameWithoutSuffix = ReadTypeName(typeName, ref pos, true, out typeParameterCount, typeArguments);
				result = new GetPotentiallyNestedClassTypeReference(typeNameWithoutSuffix, typeParameterCount);
				while (pos < typeName.Length && typeName[pos] == '.') {
					pos++;
					string nestedTypeName = ReadTypeName(typeName, ref pos, false, out typeParameterCount, typeArguments);
					result = new NestedTypeReference(result, nestedTypeName, typeParameterCount);
				}
				if (typeArguments.Count > 0) {
					result = new ParameterizedTypeReference(result, typeArguments);
				}
			}
			while (pos < typeName.Length) {
				switch (typeName[pos]) {
					case '[':
						int dimensions = 1;
						do {
							pos++;
							if (pos == typeName.Length)
								throw new ReflectionNameParseException(pos, "Unexpected end");
							if (typeName[pos] == ',')
								dimensions++;
						} while (typeName[pos] != ']');
						result = new ArrayTypeReference(result, dimensions);
						break;
					case '*':
						result = new PointerTypeReference(result);
						break;
					case '@':
						result = new ByReferenceTypeReference(result);
						break;
					default:
						return result;
				}
				pos++;
			}
			return result;
		}
		
		static string ReadTypeName(string typeName, ref int pos, bool allowDottedName, out int typeParameterCount, List<ITypeReference> typeArguments)
		{
			int startPos = pos;
			// skip the simple name portion:
			while (pos < typeName.Length && !IsIDStringSpecialCharacter(typeName[pos]) && (allowDottedName || typeName[pos] != '.'))
				pos++;
			if (pos == startPos)
				throw new ReflectionNameParseException(pos, "Expected type name");
			string shortTypeName = typeName.Substring(startPos, pos - startPos);
			// read type arguments:
			typeParameterCount = 0;
			if (pos < typeName.Length && typeName[pos] == '`') {
				// unbound generic type
				pos++;
				typeParameterCount = ReflectionHelper.ReadTypeParameterCount(typeName, ref pos);
			} else if (pos < typeName.Length && typeName[pos] == '{') {
				// bound generic type
				typeArguments = new List<ITypeReference>();
				do {
					pos++;
					typeArguments.Add(ParseTypeName(typeName, ref pos));
					typeParameterCount++;
					if (pos == typeName.Length)
						throw new ReflectionNameParseException(pos, "Unexpected end");
				} while (typeName[pos] == ',');
				if (typeName[pos] != '}')
					throw new ReflectionNameParseException(pos, "Expected '}'");
				pos++;
			}
			return shortTypeName;
		}
		#endregion
		
		#region FindEntity
		/// <summary>
		/// Finds the entity in the given type resolve context.
		/// </summary>
		/// <param name="idString">ID string of the entity.</param>
		/// <param name="context">Type resolve context</param>
		/// <returns>Returns the entity, or null if it is not found.</returns>
		/// <exception cref="ReflectionNameParseException">The syntax of the ID string is invalid</exception>
		public static IEntity FindEntity(string idString, ITypeResolveContext context)
		{
			if (idString == null)
				throw new ArgumentNullException("idString");
			if (context == null)
				throw new ArgumentNullException("context");
			if (idString.StartsWith("T:", StringComparison.Ordinal)) {
				return ParseTypeName(idString.Substring(2)).Resolve(context).GetDefinition();
			} else {
				return ParseMemberIdString(idString).Resolve(context);
			}
		}
		#endregion
	}
}
