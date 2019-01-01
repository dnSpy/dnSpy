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
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler.XmlDoc {
	/// <summary>
	/// Provides XML documentation tags.
	/// </summary>
	public sealed class XmlDocKeyProvider {
		/// <summary>
		/// Gets an XML doc key
		/// </summary>
		/// <param name="member">Member</param>
		/// <param name="b">String builder</param>
		/// <returns></returns>
		public static StringBuilder GetKey(IMemberRef member, StringBuilder b) {
			if (member == null)
				return null;
			b.Clear();
			if (member is ITypeDefOrRef) {
				b.Append("T:");
				AppendTypeName(b, ((ITypeDefOrRef)member).ToTypeSig());
			}
			else {
				if (member.IsField)
					b.Append("F:");
				else if (member.IsPropertyDef)
					b.Append("P:");
				else if (member.IsEventDef)
					b.Append("E:");
				else if (member.IsMethod)
					b.Append("M:");
				AppendTypeName(b, member.DeclaringType.ToTypeSig());
				b.Append('.');
				b.Append(member.Name.Replace('.', '#'));
				IList<Parameter> parameters;
				TypeSig explicitReturnType = null;
				if (member.IsPropertyDef) {
					parameters = GetParameters((PropertyDef)member).ToList();
				}
				else if (member.IsMethod) {
					var mr = (IMethod)member;
					if (mr.NumberOfGenericParameters > 0) {
						b.Append("``");
						b.Append(mr.NumberOfGenericParameters);
					}
					parameters = mr.GetParameters();
					if (mr.Name == "op_Implicit" || mr.Name == "op_Explicit") {
						explicitReturnType = mr.MethodSig.GetRetType();
					}
				}
				else {
					parameters = null;
				}
				if (parameters != null && parameters.Any(a => a.IsNormalMethodParameter)) {
					b.Append('(');
					for (int i = 0; i < parameters.Count; i++) {
						var param = parameters[i];
						if (!param.IsNormalMethodParameter)
							continue;
						if (param.MethodSigIndex > 0)
							b.Append(',');
						AppendTypeName(b, param.Type);
					}
					b.Append(')');
				}
				if (explicitReturnType != null) {
					b.Append('~');
					AppendTypeName(b, explicitReturnType);
				}
			}
			return b;
		}

		static IEnumerable<Parameter> GetParameters(PropertyDef property) {
			if (property == null)
				yield break;
			if (property.GetMethod != null) {
				foreach (var param in property.GetMethod.Parameters)
					yield return param;
				yield break;
			}
			if (property.SetMethod != null) {
				int last = property.SetMethod.Parameters.Count - 1;
				foreach (var param in property.SetMethod.Parameters) {
					if (param.Index != last)
						yield return param;
				}
				yield break;
			}

			int i = 0;
			foreach (var param in property.PropertySig.GetParams()) {
				yield return new Parameter(i, i, param);
				i++;
			}
		}

		static void AppendTypeName(StringBuilder b, TypeSig type) {
			type = type.RemovePinnedAndModifiers();
			if (type == null)
				return;
			if (type is GenericInstSig giType) {
				AppendTypeNameWithArguments(b, giType.GenericType == null ? null : giType.GenericType.TypeDefOrRef, giType.GenericArguments);
				return;
			}
			if (type is ArraySigBase arrayType) {
				AppendTypeName(b, arrayType.Next);
				b.Append('[');
				var lowerBounds = arrayType.GetLowerBounds();
				var sizes = arrayType.GetSizes();
				for (int i = 0; i < arrayType.Rank; i++) {
					if (i > 0)
						b.Append(',');
					if (i < lowerBounds.Count && i < sizes.Count) {
						b.Append(lowerBounds[i]);
						b.Append(':');
						b.Append(sizes[i] + lowerBounds[i] - 1);
					}
				}
				b.Append(']');
				return;
			}
			if (type is ByRefSig refType) {
				AppendTypeName(b, refType.Next);
				b.Append('@');
				return;
			}
			if (type is PtrSig ptrType) {
				AppendTypeName(b, ptrType.Next);
				b.Append('*');
				return;
			}
			if (type is GenericSig gp) {
				b.Append('`');
				if (gp.IsMethodVar) {
					b.Append('`');
				}
				b.Append(gp.Number);
			}
			else {
				var typeRef = type.ToTypeDefOrRef();
				if (typeRef.DeclaringType != null) {
					AppendTypeName(b, typeRef.DeclaringType.ToTypeSig());
					b.Append('.');
					b.Append(typeRef.Name);
				}
				else {
					FullNameFactory.FullNameSB(type, false, null, null, null, b);
				}
			}
		}

		static int AppendTypeNameWithArguments(StringBuilder b, ITypeDefOrRef type, IList<TypeSig> genericArguments) {
			if (type == null)
				return 0;
			int outerTypeParameterCount = 0;
			if (type.DeclaringType != null) {
				ITypeDefOrRef declType = type.DeclaringType;
				outerTypeParameterCount = AppendTypeNameWithArguments(b, declType, genericArguments);
				b.Append('.');
			}
			else {
				int len = b.Length;
				FullNameFactory.NamespaceSB(type, true, b);
				if (len != b.Length)
					b.Append('.');
			}
			b.Append(SplitTypeParameterCountFromReflectionName(type.Name, out int localTypeParameterCount));

			if (localTypeParameterCount > 0) {
				int totalTypeParameterCount = outerTypeParameterCount + localTypeParameterCount;
				b.Append('{');
				for (int i = outerTypeParameterCount; i < totalTypeParameterCount && i < genericArguments.Count; i++) {
					if (i > outerTypeParameterCount)
						b.Append(',');
					AppendTypeName(b, genericArguments[i]);
				}
				b.Append('}');
			}
			return outerTypeParameterCount + localTypeParameterCount;
		}

		/// <summary>
		/// Removes the ` with type parameter count from the reflection name.
		/// </summary>
		/// <remarks>Do not use this method with the full name of inner classes.</remarks>
		public static string SplitTypeParameterCountFromReflectionName(string reflectionName, out int typeParameterCount) {
			int pos = reflectionName.LastIndexOf('`');
			if (pos < 0) {
				typeParameterCount = 0;
				return reflectionName;
			}
			else {
				string typeCount = reflectionName.Substring(pos + 1);
				if (int.TryParse(typeCount, out typeParameterCount))
					return reflectionName.Substring(0, pos);
				else
					return reflectionName;
			}
		}

		/// <summary>
		/// Finds a member by key
		/// </summary>
		/// <param name="module">Module to search</param>
		/// <param name="key">Key</param>
		/// <returns></returns>
		public static IMemberRef FindMemberByKey(ModuleDef module, string key) {
			if (module == null)
				throw new ArgumentNullException(nameof(module));
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

		static IMemberRef FindMember(ModuleDef module, string key, Func<TypeDef, IEnumerable<IMemberRef>> memberSelector) {
			int parenPos = key.IndexOf('(');
			int dotPos;
			if (parenPos > 0) {
				dotPos = key.LastIndexOf('.', parenPos - 1, parenPos);
			}
			else {
				dotPos = key.LastIndexOf('.');
			}
			if (dotPos < 0)
				return null;
			TypeDef type = FindType(module, key.Substring(2, dotPos - 2));
			if (type == null)
				return null;
			string shortName;
			if (parenPos > 0) {
				shortName = key.Substring(dotPos + 1, parenPos - (dotPos + 1));
			}
			else {
				shortName = key.Substring(dotPos + 1);
			}
			IMemberRef shortNameMatch = null;
			var sb = new StringBuilder();
			foreach (IMemberRef member in memberSelector(type)) {
				var memberKey = GetKey(member, sb);
				if (memberKey.CheckEquals(key))
					return member;
				if (shortName == member.Name.Replace('.', '#'))
					shortNameMatch = member;
			}
			// if there's no match by ID string (key), return the match by name.
			return shortNameMatch;
		}

		static TypeDef FindType(ModuleDef module, string name) {
			int pos = name.LastIndexOf('.');
			if (string.IsNullOrEmpty(name))
				return null;
			TypeDef type = module.Find(name, true);
			if (type == null && pos > 0) { // Original code only entered if ns.Length > 0
										   // try if this is a nested type
				type = FindType(module, name.Substring(0, pos));
				if (type != null) {
					foreach (var nt in type.NestedTypes) {
						if (nt.Name == name)
							return nt;
					}
					return null;
				}
			}
			return type;
		}
	}
}
