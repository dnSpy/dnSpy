// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Provides XML documentation tags.
	/// </summary>
	sealed class XmlDocKeyProvider
	{
		#region GetKey
		public static string GetKey(MemberReference member)
		{
			StringBuilder b = new StringBuilder();
			if (member is TypeReference) {
				b.Append("T:");
				AppendTypeName(b, (TypeDefinition)member);
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
				b.Append(member.Name);
				IList<ParameterDefinition> parameters;
				if (member is PropertyDefinition) {
					parameters = ((PropertyDefinition)member).Parameters;
				} else if (member is MethodReference) {
					parameters = ((MethodReference)member).Parameters;
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
			}
			return b.ToString();
		}
		
		static void AppendTypeName(StringBuilder b, TypeReference type)
		{
			if (type is TypeSpecification) {
				AppendTypeName(b, ((TypeSpecification)type).ElementType);
				ArrayType arrayType = type as ArrayType;
				if (arrayType != null) {
					b.Append('[');
					for (int i = 1; i < arrayType.Dimensions.Count; i++) {
						b.Append(',');
					}
					b.Append(']');
				}
				ByReferenceType refType = type as ByReferenceType;
				if (refType != null) {
					b.Append('@');
				}
				GenericInstanceType giType = type as GenericInstanceType;
				if (giType != null) {
					b.Append('{');
					for (int i = 0; i < giType.GenericArguments.Count; i++) {
						if (i > 0) b.Append(',');
						AppendTypeName(b, giType.GenericArguments[i]);
					}
					b.Append('}');
				}
				PointerType ptrType = type as PointerType;
				if (ptrType != null) {
					b.Append('*'); // TODO: is this correct?
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
			int pos = key.IndexOf('(');
			int dotPos;
			if (pos > 0) {
				dotPos = key.LastIndexOf('.', 0, pos);
			} else {
				dotPos = key.LastIndexOf('.');
			}
			TypeDefinition type = FindType(module, key.Substring(2, dotPos - 2));
			if (type == null)
				return null;
			foreach (MemberReference member in memberSelector(type)) {
				if (GetKey(member) == key)
					return member;
			}
			return null;
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
