// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Decompiler
{
	public class NameVariables
	{
		static readonly Dictionary<string, string> typeNameToVariableNameDict = new Dictionary<string, string> {
			{ "System.Boolean", "flag" },
			{ "System.Byte", "b" },
			{ "System.SByte", "b" },
			{ "System.Int16", "num" },
			{ "System.Int32", "num" },
			{ "System.Int64", "num" },
			{ "System.UInt16", "num" },
			{ "System.UInt32", "num" },
			{ "System.UInt64", "num" },
			{ "System.Single", "num" },
			{ "System.Double", "num" },
			{ "System.Decimal", "num" },
			{ "System.String", "text" },
			{ "System.Object", "obj" },
		};
		
		
		public static void AssignNamesToVariables(IEnumerable<string> existingNames, IEnumerable<ILVariable> variables, ILBlock methodBody)
		{
			NameVariables nv = new NameVariables();
			nv.AddExistingNames(existingNames);
			foreach (ILVariable varDef in variables) {
				nv.AssignNameToVariable(varDef, methodBody.GetSelfAndChildrenRecursive<ILExpression>());
			}
		}
		
		Dictionary<string, int> typeNames = new Dictionary<string, int>();
		
		void AddExistingNames(IEnumerable<string> existingNames)
		{
			foreach (string name in existingNames) {
				if (string.IsNullOrEmpty(name))
					continue;
				// First, identify whether the name already ends with a number:
				int pos = name.Length;
				while (pos > 0 && name[pos-1] >= '0' && name[pos-1] <= '9')
					pos--;
				if (pos < name.Length) {
					int number;
					if (int.TryParse(name.Substring(pos), out number)) {
						string nameWithoutDigits = name.Substring(0, pos);
						int existingNumber;
						if (typeNames.TryGetValue(nameWithoutDigits, out existingNumber)) {
							typeNames[nameWithoutDigits] = Math.Max(number, existingNumber);
						} else {
							typeNames.Add(nameWithoutDigits, number);
						}
						continue;
					}
				}
				if (!typeNames.ContainsKey(name))
					typeNames.Add(name, 1);
			}
		}
		
		void AssignNameToVariable(ILVariable varDef, IEnumerable<ILExpression> allExpressions)
		{
			string proposedName = null;
			foreach (ILExpression expr in allExpressions) {
				if (expr.Operand != varDef)
					continue;
				if (expr.Code == ILCode.Stloc) {
					proposedName = GetNameFromExpression(expr.Arguments.Single());
				}
				if (proposedName != null)
					break;
			}
			if (proposedName == null)
				proposedName = GetNameByType(varDef.Type);
			
			if (!typeNames.ContainsKey(proposedName)) {
				typeNames.Add(proposedName, 0);
			}
			int count = ++typeNames[proposedName];
			if (count > 1) {
				varDef.Name = proposedName + count.ToString();
			} else {
				varDef.Name = proposedName;
			}
		}
		
		static string GetNameFromExpression(ILExpression expr)
		{
			switch (expr.Code) {
				case ILCode.Ldfld:
					// Use the field name only if it's not a field on this (avoid confusion between local variables and fields)
					if (!(expr.Arguments[0].Code == ILCode.Ldarg && ((ParameterDefinition)expr.Arguments[0].Operand).Index < 0))
						return ((FieldReference)expr.Operand).Name;
					break;
				case ILCode.Ldsfld:
					return ((FieldReference)expr.Operand).Name;
				case ILCode.Call:
				case ILCode.Callvirt:
					MethodReference mr = (MethodReference)expr.Operand;
					if (mr.Name.StartsWith("get_", StringComparison.Ordinal))
						return CleanUpVariableName(mr.Name.Substring(4));
					else if (mr.Name.StartsWith("Get", StringComparison.Ordinal) && mr.Name.Length >= 4 && char.IsUpper(mr.Name[3]))
						return CleanUpVariableName(mr.Name.Substring(3));
					break;
			}
			return null;
		}
		
		string GetNameByType(TypeReference type)
		{
			GenericInstanceType git = type as GenericInstanceType;
			if (git != null && git.ElementType.FullName == "System.Nullable`1" && git.GenericArguments.Count == 1) {
				type = ((GenericInstanceType)type).GenericArguments[0];
			}
			
			if (type.FullName == "System.Int32") {
				// try i,j,k, etc.
				for (char c = 'i'; c <= 'n'; c++) {
					if (!typeNames.ContainsKey(c.ToString()))
						return c.ToString();
				}
			}
			string name;
			if (type.IsArray) {
				name = "array";
			} else if (type.IsPointer) {
				name = "ptr";
			} else if (!typeNameToVariableNameDict.TryGetValue(type.FullName, out name)) {
				name = type.Name;
				// remove the 'I' for interfaces
				if (name.Length >= 3 && name[0] == 'I' && char.IsUpper(name[1]) && char.IsLower(name[2]))
					name = name.Substring(1);
				name = CleanUpVariableName(name);
			}
			return name;
		}
		
		static string CleanUpVariableName(string name)
		{
			// remove the backtick (generics)
			int pos = name.IndexOf('`');
			if (pos >= 0)
				name = name.Substring(0, pos);
			if (name.Length == 0)
				return "obj";
			else
				return char.ToLower(name[0]) + name.Substring(1);
		}
	}
}
