using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.CSharp;

using Mono.Cecil;

namespace Decompiler
{
	public class CodeDomBuilder
	{
		CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
		Dictionary<string, CodeNamespace> codeNamespaces = new Dictionary<string, CodeNamespace>();
		
		public string GenerateCode()
		{
			CSharpCodeProvider provider = new CSharpCodeProvider();
			
			TextWriter stringWriter = new StringWriter();
			
			CodeGeneratorOptions options = new CodeGeneratorOptions();
			options.BlankLinesBetweenMembers = true;
			options.BracingStyle = "C";
			options.ElseOnClosing = true;
			options.IndentString = "\t";
			options.VerbatimOrder = true;
			
			provider.GenerateCodeFromCompileUnit(codeCompileUnit, stringWriter, options);
			
			// Remove the generated comments at the start
			string code = stringWriter.ToString();
			while(code.StartsWith("//") || code.StartsWith("\r\n")) {
				code = code.Remove(0, code.IndexOf("\r\n") + "\r\n".Length);
			}
			
			return code;
		}
		
		public void AddAssembly(AssemblyDefinition assemblyDefinition)
		{
			foreach(TypeDefinition typeDef in assemblyDefinition.MainModule.Types) {
				// Skip nested types - they will be added by the parent type
				if (typeDef.DeclaringType != null) continue;
				// Skip the <Module> class
				if (typeDef.Name == "<Module>") continue;
				
				AddType(typeDef);
			}
		}
		
		CodeNamespace GetCodeNamespace(string name)
		{
			if (codeNamespaces.ContainsKey(name)) {
				return codeNamespaces[name];
			} else {
				// Create the namespace
				CodeNamespace codeNamespace = new CodeNamespace(name);
				codeCompileUnit.Namespaces.Add(codeNamespace);
				codeNamespaces[name] = codeNamespace;
				return codeNamespace;
			}
		}
		
		public void AddType(TypeDefinition typeDef)
		{
			CodeTypeDeclaration codeType = CreateType(typeDef);
			GetCodeNamespace(typeDef.Namespace).Types.Add(codeType);
		}
		
		public CodeTypeDeclaration CreateType(TypeDefinition typeDef)
		{
			CodeTypeDeclaration codeType = new CodeTypeDeclaration();
			codeType.Name = typeDef.Name;
			
			// Copy modifiers across (includes 'is interface' attribute)
			codeType.TypeAttributes = (System.Reflection.TypeAttributes)typeDef.Attributes;
			
			// Is struct or enum?
			if (typeDef.IsValueType) {
				if (typeDef.IsEnum) {  // NB: Enum is value type
					codeType.IsEnum = true;
				} else {
					codeType.IsStruct = true;
				}
			}
			
			// Nested types
			foreach(TypeDefinition nestedTypeDef in typeDef.NestedTypes) {
				codeType.Members.Add(CreateType(nestedTypeDef));
			}
			
			// Base type
			if (typeDef.BaseType != null && !typeDef.IsValueType && typeDef.BaseType.FullName != Constants.Object) {
				codeType.BaseTypes.Add(typeDef.BaseType.FullName);
			}
			
			return codeType;
		}
	}
}
