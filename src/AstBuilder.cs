using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.CSharp;

using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler
{
	public class AstBuilder
	{
		CompilationUnit astCompileUnit = new CompilationUnit();
		Dictionary<string, NamespaceDeclaration> astNamespaces = new Dictionary<string, NamespaceDeclaration>();
		
		public string GenerateCode()
		{
			CSharpOutputVisitor csOutVisitor = new CSharpOutputVisitor();
			
			astCompileUnit.AcceptVisitor(new Transforms.Ast.RemoveGotos(), null);
			astCompileUnit.AcceptVisitor(new Transforms.Ast.RemoveDeadLabels(), null);
			astCompileUnit.AcceptVisitor(new Transforms.Ast.SimplifyTypeReferences(), null);

			
			astCompileUnit.AcceptVisitor(csOutVisitor, null);
			
			string code = csOutVisitor.Text;
			code = code.Replace(":\r\n\t\t\t", ": ");
			code = code.Replace(": }", ":\r\n\t\t\t}");
			code = code.Replace(":\r\n\t\t", ": ");
			code = code.Replace(": }", ":\r\n\t\t}");
			code = code.Replace(":\r\n\t", ": ");
			code = code.Replace(": }", ":\r\n\t}");
			code = code.Replace("\t", "    ");
			code = code.Replace("\"/*", "");
			code = code.Replace("*/\";", "");
			
			// Post processing commands
			while(true) { 
				int endIndex = code.IndexOf("[JoinLine]") + "[JoinLine]".Length;
				if (endIndex != -1) {
					int startIndex = code.LastIndexOf("\r\n", endIndex, endIndex);
					if (startIndex != -1) {
						code = code.Remove(startIndex, endIndex - startIndex);
						continue;
					}
				}
				break;
			}
			while(true) { 
				int endIndex = code.IndexOf("[Tab]");
				if (endIndex != -1) {
					int startIndex = code.LastIndexOf("\r\n", endIndex, endIndex);
					if (startIndex != -1) {
						code = code.Remove(endIndex, "[Tab]".Length);
						code = code.Insert(endIndex, new string(' ', 40 - endIndex + startIndex));
						continue;
					}
				}
				break;
			}
			
			return code;
		}
		
		public void AddAssembly(AssemblyDefinition assemblyDefinition)
		{
			Ast.UsingDeclaration astUsing = new Ast.UsingDeclaration("System");
			astCompileUnit.Children.Add(astUsing);
			
			foreach(TypeDefinition typeDef in assemblyDefinition.MainModule.Types) {
				// Skip nested types - they will be added by the parent type
				if (typeDef.DeclaringType != null) continue;
				// Skip the <Module> class
				if (typeDef.Name == "<Module>") continue;
				
				AddType(typeDef);
			}
		}
		
		NamespaceDeclaration GetCodeNamespace(string name)
		{
			if (string.IsNullOrEmpty(name)) {
				return null;
			}
			if (astNamespaces.ContainsKey(name)) {
				return astNamespaces[name];
			} else {
				// Create the namespace
				NamespaceDeclaration astNamespace = new NamespaceDeclaration(name);
				astCompileUnit.Children.Add(astNamespace);
				astNamespaces[name] = astNamespace;
				return astNamespace;
			}
		}
		
		public void AddType(TypeDefinition typeDef)
		{
			TypeDeclaration astType = CreateType(typeDef);
			NamespaceDeclaration astNS = GetCodeNamespace(typeDef.Namespace);
			if (astNS != null) {
				astNS.Children.Add(astType);
			} else {
				astCompileUnit.Children.Add(astType);
			}
		}
		
		public TypeDeclaration CreateType(TypeDefinition typeDef)
		{
			TypeDeclaration astType = new TypeDeclaration(ConvertModifiers(typeDef), new List<AttributeSection>());
			astType.Name = typeDef.Name;
			
			if (typeDef.IsEnum) {  // NB: Enum is value type
				astType.Type = ClassType.Enum;
			} else if (typeDef.IsValueType) {
				astType.Type = ClassType.Struct;
			} else if (typeDef.IsInterface) {
				astType.Type = ClassType.Interface;
			} else {
				astType.Type = ClassType.Class;
			}
			
			// Nested types
			foreach(TypeDefinition nestedTypeDef in typeDef.NestedTypes) {
				astType.Children.Add(CreateType(nestedTypeDef));
			}
			
			// Base type
			if (typeDef.BaseType != null && !typeDef.IsValueType && typeDef.BaseType.FullName != Constants.Object) {
				astType.BaseTypes.Add(new Ast.TypeReference(typeDef.BaseType.FullName));
			}
			
			AddTypeMembers(astType, typeDef);
			
			return astType;
		}
		
		Modifiers ConvertModifiers(TypeDefinition typeDef)
		{
			return
				(typeDef.IsNestedPrivate            ? Modifiers.Private    : Modifiers.None) |
				(typeDef.IsNestedFamilyAndAssembly  ? Modifiers.Protected  : Modifiers.None) | // TODO: Extended access
				(typeDef.IsNestedAssembly           ? Modifiers.Internal   : Modifiers.None) |
				(typeDef.IsNestedFamily             ? Modifiers.Protected  : Modifiers.None) |
				(typeDef.IsNestedFamilyOrAssembly   ? Modifiers.Protected | Modifiers.Internal : Modifiers.None) |
				(typeDef.IsPublic                   ? Modifiers.Public     : Modifiers.None) |
				(typeDef.IsAbstract                 ? Modifiers.Abstract   : Modifiers.None);
		}
		
		Modifiers ConvertModifiers(FieldDefinition fieldDef)
		{
			return
				(fieldDef.IsPrivate            ? Modifiers.Private    : Modifiers.None) |
				(fieldDef.IsFamilyAndAssembly  ? Modifiers.Protected  : Modifiers.None) | // TODO: Extended access
				(fieldDef.IsAssembly           ? Modifiers.Internal   : Modifiers.None) |
				(fieldDef.IsFamily             ? Modifiers.Protected  : Modifiers.None) |
				(fieldDef.IsFamilyOrAssembly   ? Modifiers.Protected | Modifiers.Internal : Modifiers.None) |
				(fieldDef.IsPublic             ? Modifiers.Public     : Modifiers.None) |
				(fieldDef.IsLiteral            ? Modifiers.Const      : Modifiers.None) |
				(fieldDef.IsStatic             ? Modifiers.Static     : Modifiers.None);
		}
		
		Modifiers ConvertModifiers(MethodDefinition methodDef)
		{
			return
				(methodDef.IsCompilerControlled ? Modifiers.None       : Modifiers.None) |
				(methodDef.IsPrivate            ? Modifiers.Private    : Modifiers.None) |
				(methodDef.IsFamilyAndAssembly  ? Modifiers.Protected  : Modifiers.None) | // TODO: Extended access
				(methodDef.IsAssembly           ? Modifiers.Internal   : Modifiers.None) |
				(methodDef.IsFamily             ? Modifiers.Protected  : Modifiers.None) |
				(methodDef.IsFamilyOrAssembly   ? Modifiers.Protected | Modifiers.Internal : Modifiers.None) |
				(methodDef.IsPublic             ? Modifiers.Public     : Modifiers.None) |
				(methodDef.IsStatic             ? Modifiers.Static     : Modifiers.None) |
				(methodDef.IsVirtual            ? Modifiers.Virtual    : Modifiers.None) |
				(methodDef.IsAbstract           ? Modifiers.Abstract   : Modifiers.None);
		}
		
		void AddTypeMembers(TypeDeclaration astType, TypeDefinition typeDef)
		{
			// Add fields
			foreach(FieldDefinition fieldDef in typeDef.Fields) {
				Ast.FieldDeclaration astField = new Ast.FieldDeclaration(new List<AttributeSection>());
				astField.Fields.Add(new Ast.VariableDeclaration(fieldDef.Name));
				astField.TypeReference = new Ast.TypeReference(fieldDef.FieldType.FullName);
				astField.Modifier = ConvertModifiers(fieldDef);
				
				astType.Children.Add(astField);
			}
			
			// Add properties
			foreach(PropertyDefinition propDef in typeDef.Properties) {
				Ast.PropertyDeclaration astProp = new Ast.PropertyDeclaration(
					ConvertModifiers(propDef.GetMethod),
					new List<AttributeSection>(),
					propDef.Name,
					new List<ParameterDeclarationExpression>()
				);
				astProp.TypeReference = new Ast.TypeReference(propDef.PropertyType.FullName);
				astType.Children.Add(astProp);
			}
			
			foreach(EventDefinition eventDef in typeDef.Events) {
				Ast.EventDeclaration astEvent = new Ast.EventDeclaration();
				astEvent.Name = eventDef.Name;
				astEvent.TypeReference = new Ast.TypeReference(eventDef.EventType.FullName);
				astEvent.Modifier = ConvertModifiers(eventDef.AddMethod);
				
				astType.Children.Add(astEvent);
			}
			
			foreach(MethodDefinition methodDef in typeDef.Methods) {
				if (methodDef.IsSpecialName) continue;
				
				Ast.MethodDeclaration astMethod = new Ast.MethodDeclaration();
				astMethod.Name = methodDef.Name;
				astMethod.TypeReference = new Ast.TypeReference(methodDef.ReturnType.ReturnType.FullName);
				astMethod.Modifier = ConvertModifiers(methodDef);
				
				foreach(ParameterDefinition paramDef in methodDef.Parameters) {
					Ast.ParameterDeclarationExpression astParam = new Ast.ParameterDeclarationExpression(
						new Ast.TypeReference(paramDef.ParameterType.FullName),
						paramDef.Name
					);
					
					if (paramDef.IsIn && !paramDef.IsOut) astParam.ParamModifier = ParameterModifiers.In;
					if (!paramDef.IsIn && paramDef.IsOut) astParam.ParamModifier = ParameterModifiers.Out;
					if (paramDef.IsIn && paramDef.IsOut)  astParam.ParamModifier = ParameterModifiers.Ref;
					
					astMethod.Parameters.Add(astParam);
				}
				
				astMethod.Body = AstMetodBodyBuilder.CreateMetodBody(methodDef);
				
				astType.Children.Add(astMethod);
			}
		}
	}
}
