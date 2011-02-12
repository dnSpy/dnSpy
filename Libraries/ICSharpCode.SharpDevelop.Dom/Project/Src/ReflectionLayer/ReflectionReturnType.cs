// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ICSharpCode.SharpDevelop.Dom.ReflectionLayer
{
	public static class ReflectionReturnType
	{
		public static bool IsDefaultType(Type type)
		{
			return !type.IsArray && !type.IsGenericType && !type.IsGenericParameter;
		}
		
		#region Parse Reflection Type Name
		public static IReturnType Parse(IProjectContent pc, string reflectionTypeName)
		{
			if (pc == null)
				throw new ArgumentNullException("pc");
			using (var tokenizer = Tokenize(reflectionTypeName)) {
				tokenizer.MoveNext();
				IReturnType result = Parse(pc, tokenizer);
				if (tokenizer.Current != null)
					throw new ReflectionTypeNameSyntaxError("Expected end of type name, but found " + tokenizer.Current);
				return result;
			}
		}
		
		static IReturnType Parse(IProjectContent pc, IEnumerator<string> tokenizer)
		{
			string typeName = tokenizer.Current;
			if (typeName == null)
				throw new ReflectionTypeNameSyntaxError("Unexpected end of type name");
			tokenizer.MoveNext();
			int typeParameterCount;
			typeName = ReflectionClass.SplitTypeParameterCountFromReflectionName(typeName, out typeParameterCount);
			IReturnType rt = new GetClassReturnType(pc, typeName, typeParameterCount);
			if (tokenizer.Current == "[") {
				// this is a constructed type
				List<IReturnType> typeArguments = new List<IReturnType>();
				do {
					tokenizer.MoveNext();
					if (tokenizer.Current != "[")
						throw new ReflectionTypeNameSyntaxError("Expected '['");
					tokenizer.MoveNext();
					typeArguments.Add(Parse(pc, tokenizer));
					if (tokenizer.Current != "]")
						throw new ReflectionTypeNameSyntaxError("Expected ']' after generic argument");
					tokenizer.MoveNext();
				} while (tokenizer.Current == ",");
				if (tokenizer.Current != "]")
					throw new ReflectionTypeNameSyntaxError("Expected ']' after generic argument list");
				tokenizer.MoveNext();
				
				rt = new ConstructedReturnType(rt, typeArguments);
			}
			while (tokenizer.Current == ",") {
				tokenizer.MoveNext();
				string token = tokenizer.Current;
				if (token != null && token != "," && token != "[" && token != "]")
					tokenizer.MoveNext();
			}
			return rt;
		}
		
		static IEnumerator<string> Tokenize(string reflectionTypeName)
		{
			StringBuilder currentText = new StringBuilder();
			for (int i = 0; i < reflectionTypeName.Length; i++) {
				char c = reflectionTypeName[i];
				if (c == ',' || c == '[' || c == ']') {
					if (currentText.Length > 0) {
						yield return currentText.ToString();
						currentText.Length = 0;
					}
					yield return c.ToString();
				} else {
					currentText.Append(c);
				}
			}
			if (currentText.Length > 0)
				yield return currentText.ToString();
			yield return null;
		}
		#endregion
		
		/// <summary>
		/// Creates a IReturnType from the reflection type.
		/// </summary>
		/// <param name="pc">The project content used as context.</param>
		/// <param name="entity">The member used as context (e.g. as GenericReturnType)</param>
		/// <param name="type">The reflection return type that should be converted</param>
		/// <param name="createLazyReturnType">Set this parameter to false to create a direct return type
		/// (without GetClassReturnType indirection) where possible</param>
		/// <param name="forceGenericType">Set this parameter to false to allow unbound generic types</param>
		/// <returns>The IReturnType</returns>
		/// <param name="attributeProvider">Attribute provider for lookup of [Dynamic] attribute</param>
		public static IReturnType Create(IProjectContent pc, Type type,
		                                 IEntity entity = null,
		                                 bool createLazyReturnType = false,
		                                 bool forceGenericType = true,
		                                 ICustomAttributeProvider attributeProvider = null)
		{
			if (pc == null)
				throw new ArgumentNullException("pc");
			if (type == null)
				throw new ArgumentNullException("type");
			int typeIndex = 0;
			return Create(pc, type, entity, createLazyReturnType, attributeProvider, ref typeIndex, forceGenericType);
		}
		
		public static IReturnType Create(IEntity entity, Type type,
		                                 bool createLazyReturnType = false,
		                                 bool forceGenericType = true,
		                                 ICustomAttributeProvider attributeProvider = null)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");
			if (type == null)
				throw new ArgumentNullException("type");
			int typeIndex = 0;
			return Create(entity.ProjectContent, type, entity, createLazyReturnType, attributeProvider, ref typeIndex, forceGenericType);
		}
		
		static IReturnType Create(IProjectContent pc, Type type,
		                          IEntity member,
		                          bool createLazyReturnType,
		                          ICustomAttributeProvider attributeProvider,
		                          ref int typeIndex,
		                          bool forceGenericType = true)
		{
			if (type.IsByRef) {
				// TODO: Use ByRefRefReturnType
				return Create(pc, type.GetElementType(), member, createLazyReturnType, attributeProvider, ref typeIndex);
			} else if (type.IsPointer) {
				typeIndex++;
				return new PointerReturnType(Create(pc, type.GetElementType(), member, createLazyReturnType, attributeProvider, ref typeIndex));
			} else if (type.IsArray) {
				typeIndex++;
				return new ArrayReturnType(pc, Create(pc, type.GetElementType(), member, createLazyReturnType, attributeProvider, ref typeIndex), type.GetArrayRank());
			} else if (type.IsGenericType && (forceGenericType || !type.IsGenericTypeDefinition)) {
				IReturnType baseType = Create(pc, type.GetGenericTypeDefinition(), member, createLazyReturnType, attributeProvider, ref typeIndex, forceGenericType: false);
				Type[] args = type.GetGenericArguments();
				List<IReturnType> para = new List<IReturnType>(args.Length);
				for (int i = 0; i < args.Length; ++i) {
					typeIndex++;
					para.Add(Create(pc, args[i], member, createLazyReturnType, attributeProvider, ref typeIndex));
				}
				return new ConstructedReturnType(baseType, para);
			} else if (type.IsGenericParameter) {
				IClass c = (member is IClass) ? (IClass)member : (member is IMember) ? ((IMember)member).DeclaringType : null;
				if (c != null && type.GenericParameterPosition < c.TypeParameters.Count) {
					if (c.TypeParameters[type.GenericParameterPosition].Name == type.Name) {
						return new GenericReturnType(c.TypeParameters[type.GenericParameterPosition]);
					}
				}
				if (type.DeclaringMethod != null) {
					IMethod method = member as IMethod;
					if (method != null) {
						if (type.GenericParameterPosition < method.TypeParameters.Count) {
							return new GenericReturnType(method.TypeParameters[type.GenericParameterPosition]);
						}
						return new GenericReturnType(new DefaultTypeParameter(method, type));
					}
				}
				return new GenericReturnType(new DefaultTypeParameter(c, type));
			} else {
				string name = type.FullName;
				if (name == null)
					throw new ApplicationException("type.FullName returned null. Type: " + type.ToString());
				int typeParameterCount;
				name = ReflectionClass.ConvertReflectionNameToFullName(name, out typeParameterCount);
				
				if (typeParameterCount == 0 && name == "System.Object" && HasDynamicAttribute(attributeProvider, typeIndex))
					return new DynamicReturnType(pc);
				
				if (!createLazyReturnType) {
					IClass c = pc.GetClass(name, typeParameterCount);
					if (c != null)
						return c.DefaultReturnType;
					// example where name is not found: pointers like System.Char*
					// or when the class is in a assembly that is not referenced
				}
				return new GetClassReturnType(pc, name, typeParameterCount);
			}
		}
		
		static bool HasDynamicAttribute(ICustomAttributeProvider attributeProvider, int typeIndex)
		{
			if (attributeProvider == null)
				return false;
			object[] attributes = attributeProvider.GetCustomAttributes(typeof(DynamicAttribute), false);
			if (attributes.Length == 0)
				return false;
			DynamicAttribute attr = attributes[0] as DynamicAttribute;
			if (attr != null) {
				var transformFlags = attr.TransformFlags;
				if (transformFlags != null && typeIndex < transformFlags.Count)
					return transformFlags[typeIndex];
				return true;
			}
			return false;
		}
	}
}
