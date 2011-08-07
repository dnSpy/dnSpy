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
using System.Diagnostics;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.XmlDoc
{
	/// <summary>
	/// Provides XML documentation tags.
	/// </summary>
	public sealed class XmlDocKeyProvider
	{
		#region GetKey
		public static string GetKey(MemberReference member)
		{
			StringBuilder b = new StringBuilder();
			if (member is TypeReference) {
				b.Append("T:");
				AppendTypeName(b, (TypeReference)member);
			} else {
				if (member is FieldReference)
					b.Append("F:");
				else if (member is PropertyDefinition)
					b.Append("P:");
				else if (member is EventDefinition)
					b.Append("E:");
				else if (member is MethodReference)
					b.Append("M:");
				AppendTypeName(b, member.DeclaringType);
				b.Append('.');
				b.Append(member.Name.Replace('.', '#'));
				IList<ParameterDefinition> parameters;
				TypeReference explicitReturnType = null;
				if (member is PropertyDefinition) {
					parameters = ((PropertyDefinition)member).Parameters;
				} else if (member is MethodReference) {
					MethodReference mr = (MethodReference)member;
					if (mr.HasGenericParameters) {
						b.Append("``");
						b.Append(mr.GenericParameters.Count);
					}
					parameters = mr.Parameters;
					if (mr.Name == "op_Implicit" || mr.Name == "op_Explicit") {
						explicitReturnType = mr.ReturnType;
					}
				} else {
					parameters = null;
				}
				if (parameters != null && parameters.Count > 0) {
					b.Append('(');
					for (int i = 0; i < parameters.Count; i++) {
						if (i > 0) b.Append(',');
						AppendTypeName(b, parameters[i].ParameterType);
					}
					b.Append(')');
				}
				if (explicitReturnType != null) {
					b.Append('~');
					AppendTypeName(b, explicitReturnType);
				}
			}
			return b.ToString();
		}
		
		static void AppendTypeName(StringBuilder b, TypeReference type)
		{
			if (type == null) {
				// could happen when a TypeSpecification has no ElementType; e.g. function pointers in C++/CLI assemblies
				return;
			}
			if (type is GenericInstanceType) {
				GenericInstanceType giType = (GenericInstanceType)type;
				AppendTypeNameWithArguments(b, giType.ElementType, giType.GenericArguments);
			} else if (type is TypeSpecification) {
				AppendTypeName(b, ((TypeSpecification)type).ElementType);
				ArrayType arrayType = type as ArrayType;
				if (arrayType != null) {
					b.Append('[');
					for (int i = 0; i < arrayType.Dimensions.Count; i++) {
						if (i > 0)
							b.Append(',');
						ArrayDimension ad = arrayType.Dimensions[i];
						if (ad.IsSized) {
							b.Append(ad.LowerBound);
							b.Append(':');
							b.Append(ad.UpperBound);
						}
					}
					b.Append(']');
				}
				ByReferenceType refType = type as ByReferenceType;
				if (refType != null) {
					b.Append('@');
				}
				PointerType ptrType = type as PointerType;
				if (ptrType != null) {
					b.Append('*');
				}
			} else {
				GenericParameter gp = type as GenericParameter;
				if (gp != null) {
					b.Append('`');
					if (gp.Owner.GenericParameterType == GenericParameterType.Method) {
						b.Append('`');
					}
					b.Append(gp.Position);
				} else if (type.DeclaringType != null) {
					AppendTypeName(b, type.DeclaringType);
					b.Append('.');
					b.Append(type.Name);
				} else {
					b.Append(type.FullName);
				}
			}
		}
		
		static int AppendTypeNameWithArguments(StringBuilder b, TypeReference type, IList<TypeReference> genericArguments)
		{
			int outerTypeParameterCount = 0;
			if (type.DeclaringType != null) {
				TypeReference declType = type.DeclaringType;
				outerTypeParameterCount = AppendTypeNameWithArguments(b, declType, genericArguments);
				b.Append('.');
			} else if (!string.IsNullOrEmpty(type.Namespace)) {
				b.Append(type.Namespace);
				b.Append('.');
			}
			int localTypeParameterCount = 0;
			b.Append(NRefactory.TypeSystem.ReflectionHelper.SplitTypeParameterCountFromReflectionName(type.Name, out localTypeParameterCount));
			
			if (localTypeParameterCount > 0) {
				int totalTypeParameterCount = outerTypeParameterCount + localTypeParameterCount;
				b.Append('{');
				for (int i = outerTypeParameterCount; i < totalTypeParameterCount && i < genericArguments.Count; i++) {
					if (i > outerTypeParameterCount) b.Append(',');
					AppendTypeName(b, genericArguments[i]);
				}
				b.Append('}');
			}
			return outerTypeParameterCount + localTypeParameterCount;
		}
		#endregion
		
		#region FindMemberByKey
		public static MemberReference FindMemberByKey(ModuleDefinition module, string key)
		{
			if (module == null)
				throw new ArgumentNullException("module");
			if (key == null || key.Length < 2 || key[1] != ':')
				return null;
			switch (key[0]) {
				case 'T':
					return FindType(module, key.Substring(2));
				case 'F':
					return FindMember(module, key, type => type.Fields);
				case 'P':
					return FindMember(module, key, type => type.Properties);
				case 'E':
					return FindMember(module, key, type => type.Events);
				case 'M':
					return FindMember(module, key, type => type.Methods);
				default:
					return null;
			}
		}
		
		static MemberReference FindMember(ModuleDefinition module, string key, Func<TypeDefinition, IEnumerable<MemberReference>> memberSelector)
		{
			Debug.WriteLine("Looking for member " + key);
			int parenPos = key.IndexOf('(');
			int dotPos;
			if (parenPos > 0) {
				dotPos = key.LastIndexOf('.', parenPos - 1, parenPos);
			} else {
				dotPos = key.LastIndexOf('.');
			}
			if (dotPos < 0) return null;
			TypeDefinition type = FindType(module, key.Substring(2, dotPos - 2));
			if (type == null)
				return null;
			string shortName;
			if (parenPos > 0) {
				shortName = key.Substring(dotPos + 1, parenPos - (dotPos + 1));
			} else {
				shortName = key.Substring(dotPos + 1);
			}
			Debug.WriteLine("Searching in type {0} for {1}", type.FullName, shortName);
			MemberReference shortNameMatch = null;
			foreach (MemberReference member in memberSelector(type)) {
				string memberKey = GetKey(member);
				Debug.WriteLine(memberKey);
				if (memberKey == key)
					return member;
				if (shortName == member.Name.Replace('.', '#'))
					shortNameMatch = member;
			}
			// if there's no match by ID string (key), return the match by name.
			return shortNameMatch;
		}
		
		static TypeDefinition FindType(ModuleDefinition module, string name)
		{
			int pos = name.LastIndexOf('.');
			string ns;
			if (pos >= 0) {
				ns = name.Substring(0, pos);
				name = name.Substring(pos + 1);
			} else {
				ns = string.Empty;
			}
			if (string.IsNullOrEmpty(name)) return null;
			TypeDefinition type = module.GetType(ns, name);
			if (type == null && ns.Length > 0) {
				// try if this is a nested type
				type = FindType(module, ns);
				if (type != null) {
					type = type.NestedTypes.FirstOrDefault(t => t.Name == name);
				}
			}
			return type;
		}
		#endregion
	}
}
