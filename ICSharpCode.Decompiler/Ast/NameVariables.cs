// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

using ICSharpCode.Decompiler.ILAst;
using Mono.Cecil;

namespace ICSharpCode.Decompiler.Ast
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
			{ "System.Char", "c" }
		};
		
		
		public static void AssignNamesToVariables(IEnumerable<ParameterDefinition> parameters, IEnumerable<ILVariable> variables, ILBlock methodBody)
		{
			NameVariables nv = new NameVariables();
			nv.AddExistingNames(parameters.Select(p => p.Name));
			nv.AddExistingNames(variables.Where(v => v.IsGenerated).Select(v => v.Name));
			foreach (ParameterDefinition p in parameters) {
				if (string.IsNullOrEmpty(p.Name))
					p.Name = nv.GenerateNameForVariableOrParameter(p, p.ParameterType, methodBody);
			}
			foreach (ILVariable varDef in variables) {
				if (!varDef.IsGenerated) {
					varDef.Name = nv.GenerateNameForVariableOrParameter(varDef, varDef.Type, methodBody);
				}
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
		
		string GenerateNameForVariableOrParameter(object variableOrParameter, TypeReference varType, ILBlock methodBody)
		{
			var proposedNameForStores =
				(from expr in methodBody.GetSelfAndChildrenRecursive<ILExpression>()
				 where expr.Code == ILCode.Stloc && expr.Operand == variableOrParameter
				 select GetNameFromExpression(expr.Arguments.Single()) into name
				 where !string.IsNullOrEmpty(name)
				 select name).Distinct().ToList();
			
			string proposedName;
			if (proposedNameForStores.Count == 1) {
				proposedName = proposedNameForStores[0];
			} else {
				// TODO: infer proposed names from loads
				proposedName = GetNameByType(varType);
			}
			
			if (!typeNames.ContainsKey(proposedName)) {
				typeNames.Add(proposedName, 0);
			}
			int count = ++typeNames[proposedName];
			if (count > 1) {
				return proposedName + count.ToString();
			} else {
				return proposedName;
			}
		}
		
		static string GetNameFromExpression(ILExpression expr)
		{
			switch (expr.Code) {
				case ILCode.Ldfld:
					// Use the field name only if it's not a field on this (avoid confusion between local variables and fields)
					if (!(expr.Arguments[0].Code == ILCode.Ldarg && ((ParameterDefinition)expr.Arguments[0].Operand).Index < 0))
						return CleanUpVariableName(((FieldReference)expr.Operand).Name);
					break;
				case ILCode.Ldsfld:
					return CleanUpVariableName(((FieldReference)expr.Operand).Name);
				case ILCode.Call:
				case ILCode.Callvirt:
					MethodReference mr = (MethodReference)expr.Operand;
					if (mr.Name.StartsWith("get_", StringComparison.Ordinal) && mr.Parameters.Count == 0) {
						// use name from properties, but not from indexers
						return CleanUpVariableName(mr.Name.Substring(4));
					} else if (mr.Name.StartsWith("Get", StringComparison.Ordinal) && mr.Name.Length >= 4 && char.IsUpper(mr.Name[3])) {
						// use name from Get-methods
						return CleanUpVariableName(mr.Name.Substring(3));
					}
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
