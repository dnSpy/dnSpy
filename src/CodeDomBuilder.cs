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
			
			AddTypeMembers(codeType, typeDef);
			
			return codeType;
		}
		
		MemberAttributes GetMethodAttributes(MethodDefinition methodDef)
		{
			MemberAttributes noAttribs = (MemberAttributes)0;
			return
				// Access modifiers
				(methodDef.IsCompilerControlled ? noAttribs : noAttribs) |
				(methodDef.IsPrivate ? MemberAttributes.Private : noAttribs) |
				(methodDef.IsFamilyAndAssembly ? MemberAttributes.FamilyAndAssembly : noAttribs) |
				(methodDef.IsAssembly ? MemberAttributes.Assembly : noAttribs) |
				(methodDef.IsFamily ? MemberAttributes.Family : noAttribs) |
				(methodDef.IsFamilyOrAssembly ? MemberAttributes.FamilyOrAssembly : noAttribs) |
				(methodDef.IsPublic ? MemberAttributes.Public : noAttribs) |
				// Others
				(methodDef.IsStatic ? MemberAttributes.Static : noAttribs) |
				// Method specific
				(methodDef.IsFinal ? MemberAttributes.Final : noAttribs) |
				(methodDef.IsAbstract ? MemberAttributes.Abstract : noAttribs);
		}
		
		void AddTypeMembers(CodeTypeDeclaration codeType, TypeDefinition typeDef)
		{
			MemberAttributes noAttribs = (MemberAttributes)0;
			
			// Add fields
			foreach(FieldDefinition fieldDef in typeDef.Fields) {
				CodeMemberField codeField = new CodeMemberField();
				codeField.Name = fieldDef.Name;
				codeField.Type = new CodeTypeReference(fieldDef.FieldType.FullName);
				codeField.Attributes = 
					// Access modifiers
					(fieldDef.IsCompilerControlled ? noAttribs : noAttribs) |
					(fieldDef.IsPrivate ? MemberAttributes.Private : noAttribs) |
					(fieldDef.IsFamilyAndAssembly ? MemberAttributes.FamilyAndAssembly : noAttribs) |
					(fieldDef.IsAssembly ? MemberAttributes.Assembly : noAttribs) |
					(fieldDef.IsFamily ? MemberAttributes.Family : noAttribs) |
					(fieldDef.IsFamilyOrAssembly ? MemberAttributes.FamilyOrAssembly : noAttribs) |
					(fieldDef.IsPublic ? MemberAttributes.Public : noAttribs) |
					// Others
					(fieldDef.IsLiteral ? MemberAttributes.Const : noAttribs) |
					(fieldDef.IsStatic ? MemberAttributes.Static : noAttribs);
				
				codeType.Members.Add(codeField);
			}
			
			// Add properties
			foreach(PropertyDefinition propDef in typeDef.Properties) {
				CodeMemberProperty codeProp = new CodeMemberProperty();
				codeProp.Name = propDef.Name;
				codeProp.Type = new CodeTypeReference(propDef.PropertyType.FullName);
				codeProp.Attributes = GetMethodAttributes(propDef.GetMethod);
				
				codeType.Members.Add(codeProp);
			}
			
			foreach(EventDefinition eventDef in typeDef.Events) {
				CodeMemberEvent codeEvent = new CodeMemberEvent();
				codeEvent.Name = eventDef.Name;
				codeEvent.Type = new CodeTypeReference(eventDef.EventType.FullName);
				codeEvent.Attributes = GetMethodAttributes(eventDef.AddMethod);
				
				codeType.Members.Add(codeEvent);
			}
			
			foreach(MethodDefinition methodDef in typeDef.Methods) {
				if (methodDef.IsSpecialName) continue;
				
				CodeMemberMethod codeMethod = new CodeMemberMethod();
				codeMethod.Name = methodDef.Name;
				codeMethod.ReturnType = new CodeTypeReference(methodDef.ReturnType.ReturnType.FullName);
				codeMethod.Attributes = GetMethodAttributes(methodDef);
				
				foreach(ParameterDefinition paramDef in methodDef.Parameters) {
					CodeParameterDeclarationExpression codeParam = new CodeParameterDeclarationExpression();
					codeParam.Name = paramDef.Name;
					codeParam.Type = new CodeTypeReference(paramDef.ParameterType.FullName);
					if (paramDef.IsIn && !paramDef.IsOut) codeParam.Direction = FieldDirection.In;
					if (!paramDef.IsIn && paramDef.IsOut) codeParam.Direction = FieldDirection.Out;
					if (paramDef.IsIn && paramDef.IsOut) codeParam.Direction = FieldDirection.Ref;
					
					codeMethod.Parameters.Add(codeParam);
				}
				
				codeType.Members.Add(codeMethod);
			}
		}
	}
}
