// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.CodeDom;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using VBDomGenerator.Dom;
using ICSharpCode.EasyCodeDom;

namespace VBDomGenerator
{
	class MainClass
	{
		public const string VisitPrefix = "Visit";
		
		static readonly string[] lineEndings = { "\r\n", "\r", "\n" };
		
		public static void Main(string[] args)
		{
			string directory = "../../../ICSharpCode.NRefactory.VB/Dom/";
			string visitorsDir = "../../../ICSharpCode.NRefactory.VB/Visitors/";
			
			Debug.WriteLine("DOM Generator running...");
			if (!File.Exists(directory + "INode.cs")) {
				Debug.WriteLine("did not find output directory");
				return;
			}
			if (!File.Exists(visitorsDir + "AbstractDomTransformer.cs")) {
				Debug.WriteLine("did not find visitor output directory");
				return;
			}
			
			List<Type> nodeTypes = new List<Type>();
			foreach (Type type in typeof(MainClass).Assembly.GetTypes()) {
				if (type.IsClass && typeof(INode).IsAssignableFrom(type)) {
					nodeTypes.Add(type);
				}
			}
			nodeTypes.Sort(delegate(Type a, Type b) { return a.Name.CompareTo(b.Name); });
			
			CodeCompileUnit ccu = new CodeCompileUnit();
			CodeNamespace cns = ccu.AddNamespace("ICSharpCode.NRefactory.VB.Dom");
			cns.AddImport("System");
			cns.AddImport("System.Collections.Generic");
			foreach (Type type in nodeTypes) {
				if (type.GetCustomAttributes(typeof(CustomImplementationAttribute), false).Length == 0) {
					CodeTypeDeclaration ctd = cns.AddType(type.Name);
					if (type.IsAbstract) {
						ctd.TypeAttributes |= TypeAttributes.Abstract;
					}
					ctd.BaseTypes.Add(new CodeTypeReference(type.BaseType.Name));
					
					ProcessType(type, ctd);
					
					foreach (object o in type.GetCustomAttributes(false)) {
						if (o is TypeImplementationModifierAttribute) {
							(o as TypeImplementationModifierAttribute).ModifyImplementation(cns, ctd, type);
						}
					}
					
					if (!type.IsAbstract) {
						CodeMemberMethod method = new CodeMemberMethod();
						method.Name = "AcceptVisitor";
						method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
						method.Parameters.Add(new CodeParameterDeclarationExpression("IDomVisitor", "visitor"));
						method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), "data"));
						method.ReturnType = new CodeTypeReference(typeof(object));
						CodeExpression ex = new CodeVariableReferenceExpression("visitor");
						ex = new CodeMethodInvokeExpression(ex, VisitPrefix + ctd.Name,
						                                    new CodeThisReferenceExpression(),
						                                    new CodeVariableReferenceExpression("data"));
						method.Statements.Add(new CodeMethodReturnStatement(ex));
						ctd.Members.Add(method);
						
						method = new CodeMemberMethod();
						method.Name = "ToString";
						method.Attributes = MemberAttributes.Public | MemberAttributes.Override;
						method.ReturnType = new CodeTypeReference(typeof(string));
						method.Statements.Add(new CodeMethodReturnStatement(CreateToString(type)));
						ctd.Members.Add(method);
					}
				}
			}
			
			System.CodeDom.Compiler.CodeGeneratorOptions settings = new System.CodeDom.Compiler.CodeGeneratorOptions();
			settings.IndentString = "\t";
			settings.VerbatimOrder = true;
			
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromCompileUnit(ccu, writer, settings);
				File.WriteAllText(directory + "Generated.cs", NormalizeNewLines(writer));
			}
			
			ccu = new CodeCompileUnit();
			cns = ccu.AddNamespace("ICSharpCode.NRefactory.VB");
			cns.AddImport("System");
			cns.AddImport("ICSharpCode.NRefactory.VB.Dom");
			cns.Types.Add(CreateDomVisitorInterface(nodeTypes));
			
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromCompileUnit(ccu, writer, settings);
				File.WriteAllText(visitorsDir + "../IDomVisitor.cs", NormalizeNewLines(writer));
			}
			
			ccu = new CodeCompileUnit();
			cns = ccu.AddNamespace("ICSharpCode.NRefactory.VB.Visitors");
			cns.AddImport("System");
			cns.AddImport("System.Collections.Generic");
			cns.AddImport("System.Diagnostics");
			cns.AddImport("ICSharpCode.NRefactory.VB.Dom");
			cns.Types.Add(CreateDomVisitorClass(nodeTypes, false));
			
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromCompileUnit(ccu, writer, settings);
				File.WriteAllText(visitorsDir + "AbstractDomVisitor.cs", NormalizeNewLines(writer));
			}
			
			ccu = new CodeCompileUnit();
			cns = ccu.AddNamespace("ICSharpCode.NRefactory.VB.Visitors");
			cns.AddImport("System");
			cns.AddImport("System.Collections.Generic");
			cns.AddImport("System.Diagnostics");
			cns.AddImport("ICSharpCode.NRefactory.VB.Dom");
			cns.Types.Add(CreateDomVisitorClass(nodeTypes, true));
			
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromCompileUnit(ccu, writer, settings);
				File.WriteAllText(visitorsDir + "AbstractDomTransformer.cs", NormalizeNewLines(writer));
			}
			
			ccu = new CodeCompileUnit();
			cns = ccu.AddNamespace("ICSharpCode.NRefactory.VB.Visitors");
			cns.AddImport("System");
			cns.AddImport("ICSharpCode.NRefactory.VB.Dom");
			cns.Types.Add(CreateNodeTrackingDomVisitorClass(nodeTypes));
			
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromCompileUnit(ccu, writer, settings);
				// CodeDom cannot output "sealed", so we need to use this hack:
				File.WriteAllText(visitorsDir + "NodeTrackingDomVisitor.cs",
				                  NormalizeNewLines(writer).Replace("public override object", "public sealed override object"));
			}
			
			//NotImplementedDomVisitor
			ccu = new CodeCompileUnit();
			cns = ccu.AddNamespace("ICSharpCode.NRefactory.VB.Visitors");
			cns.AddImport("System");
			cns.AddImport("ICSharpCode.NRefactory.VB.Dom");
			cns.Types.Add(CreateNotImplementedDomVisitorClass(nodeTypes));
			
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromCompileUnit(ccu, writer, settings);
				File.WriteAllText(visitorsDir + "NotImplementedDomVisitor.cs", NormalizeNewLines(writer));
			}
			Debug.WriteLine("DOM Generator done!");
			
			Debug.WriteLine("start keyword list generation...");
			
			KeywordGenerator.Generate();
			
			Debug.WriteLine("keyword list generation done!");
		}
		
		static string NormalizeNewLines(StringWriter writer)
		{
			return string.Join(Environment.NewLine, writer.ToString().Split(lineEndings, StringSplitOptions.None));
		}
		
		static CodeTypeDeclaration CreateDomVisitorInterface(List<Type> nodeTypes)
		{
			CodeTypeDeclaration td = new CodeTypeDeclaration("IDomVisitor");
			td.IsInterface = true;
			
			foreach (Type t in nodeTypes) {
				if (!t.IsAbstract) {
					EasyMethod m = td.AddMethod(typeof(object), VisitPrefix + t.Name);
					m.AddParameter(ConvertType(t), GetFieldName(t.Name));
					m.AddParameter(typeof(object), "data");
				}
			}
			return td;
		}
		
		static CodeTypeDeclaration CreateDomVisitorClass(List<Type> nodeTypes, bool transformer)
		{
			CodeTypeDeclaration td = new CodeTypeDeclaration(transformer ? "AbstractDomTransformer" : "AbstractDomVisitor");
			td.TypeAttributes = TypeAttributes.Public | TypeAttributes.Abstract;
			td.BaseTypes.Add(new CodeTypeReference("IDomVisitor"));
			
			if (transformer) {
				string comment =
					"The AbstractDomTransformer will iterate through the whole Dom,\n " +
					"just like the AbstractDomVisitor. However, the AbstractDomTransformer allows\n " +
					"you to modify the Dom at the same time: It does not use 'foreach' internally,\n " +
					"so you can add members to collections of parents of the current node (but\n " +
					"you cannot insert or delete items as that will make the index used invalid).\n " +
					"You can use the methods ReplaceCurrentNode and RemoveCurrentNode to replace\n " +
					"or remove the current node, totally independent from the type of the parent node.";
				Easy.AddSummary(td, comment);
				
				CodeMemberField field = td.AddField(Easy.TypeRef("Stack", "INode"), "nodeStack");
				field.InitExpression = Easy.New(field.Type);
				
				/*
				CodeExpression nodeStack = Easy.Var("nodeStack");
				CodeMemberProperty p = new CodeMemberProperty();
				p.Name = "CurrentNode";
				p.Type = new CodeTypeReference("INode");
				p.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				p.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("currentNode")));
				p.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("currentNode"),
				                                            new CodePropertySetValueReferenceExpression()));
				td.Members.Add(p);
				 */
				
				EasyMethod m = td.AddMethod("ReplaceCurrentNode");
				m.AddParameter(Easy.TypeRef("INode"), "newNode");
				m.Statements.Add(Easy.Var("nodeStack").InvokeMethod("Pop"));
				m.Statements.Add(Easy.Var("nodeStack").InvokeMethod("Push", Easy.Var("newNode")));
				
				m = td.AddMethod("RemoveCurrentNode");
				m.Statements.Add(Easy.Var("nodeStack").InvokeMethod("Pop"));
				m.Statements.Add(Easy.Var("nodeStack").InvokeMethod("Push", Easy.Null));
			}
			
			foreach (Type type in nodeTypes) {
				if (!type.IsAbstract) {
					EasyMethod m = td.AddMethod(typeof(object), VisitPrefix + type.Name);
					m.Attributes = MemberAttributes.Public;
					m.AddParameter(ConvertType(type), GetFieldName(type.Name));
					m.AddParameter(typeof(object), "data");
					
					List<CodeStatement> assertions = new List<CodeStatement>();
					string varVariableName = GetFieldName(type.Name);
					CodeExpression var = Easy.Var(varVariableName);
					assertions.Add(AssertIsNotNull(var));
					
					AddFieldVisitCode(m, type, var, assertions, transformer);
					
					if (type.GetCustomAttributes(typeof(HasChildrenAttribute), true).Length > 0) {
						if (transformer) {
							m.Statements.Add(new CodeSnippetStatement(CreateTransformerLoop(varVariableName + ".Children", "INode")));
							m.Body.Return(Easy.Null);
						} else {
							m.Body.Return(var.InvokeMethod("AcceptChildren", Easy.This, Easy.Var("data")));
						}
					} else {
						CodeExpressionStatement lastStatement = null;
						if (m.Statements.Count > 0) {
							lastStatement = m.Statements[m.Statements.Count - 1] as CodeExpressionStatement;
						}
						if (lastStatement != null) {
							m.Statements.RemoveAt(m.Statements.Count - 1);
							m.Body.Return(lastStatement.Expression);
						} else {
							m.Body.Return(Easy.Null);
						}
					}
					
					for (int i = 0; i < assertions.Count; i++) {
						m.Statements.Insert(i, assertions[i]);
					}
				}
			}
			return td;
		}
		
		static void AddFieldVisitCode(EasyMethod m, Type type, CodeExpression var, List<CodeStatement> assertions, bool transformer)
		{
			if (type != null) {
				if (type.BaseType != typeof(StatementWithEmbeddedStatement)) {
					AddFieldVisitCode(m, type.BaseType, var, assertions, transformer);
				}
				foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)) {
					AddVisitCode(m, field, var, assertions, transformer);
				}
				if (type.BaseType == typeof(StatementWithEmbeddedStatement)) {
					AddFieldVisitCode(m, type.BaseType, var, assertions, transformer);
				}
			}
		}
		
		static CodeStatement AssertIsNotNull(CodeExpression expr)
		{
			return new CodeExpressionStatement(
				Easy.Type("Debug").InvokeMethod("Assert",
				                                Easy.Binary(expr,
				                                            CodeBinaryOperatorType.IdentityInequality,
				                                            Easy.Null))
			);
		}
		
		static string GetCode(CodeExpression ex)
		{
			using (StringWriter writer = new StringWriter()) {
				new Microsoft.CSharp.CSharpCodeProvider().GenerateCodeFromExpression(ex, writer, null);
				return writer.ToString();
			}
		}
		
		static string CreateTransformerLoop(string collection, string typeName)
		{
			return
				"\t\t\tfor (int i = 0; i < " + collection + ".Count; i++) {\n" +
				"\t\t\t\t" + typeName + " o = " + collection + "[i];\n" +
				"\t\t\t\tDebug.Assert(o != null);\n" +
				"\t\t\t\tnodeStack.Push(o);\n" +
				"\t\t\t\to.AcceptVisitor(this, data);\n" +
				(typeName == "INode"
				 ? "\t\t\t\to = nodeStack.Pop();\n"
				 : "\t\t\t\to = (" + typeName + ")nodeStack.Pop();\n") +
				"\t\t\t\tif (o == null)\n" +
				"\t\t\t\t\t" + collection + ".RemoveAt(i--);\n" +
				"\t\t\t\telse\n" +
				"\t\t\t\t\t" + collection + "[i] = o;\n" +
				"\t\t\t}";
		}
		
		static bool AddVisitCode(EasyMethod m, FieldInfo field, CodeExpression var, List<CodeStatement> assertions, bool transformer)
		{
			CodeExpression prop = var.Property(GetPropertyName(field.Name));
			CodeExpression nodeStack = Easy.Var("nodeStack");
			if (field.FieldType.FullName.StartsWith("System.Collections.Generic.List")) {
				Type elType = field.FieldType.GetGenericArguments()[0];
				if (!typeof(INode).IsAssignableFrom(elType))
					return false;
				assertions.Add(AssertIsNotNull(prop));
				string code;
				if (transformer) {
					code = CreateTransformerLoop(GetCode(prop), ConvertType(elType).BaseType);
				} else {
					code =
						"\t\t\tforeach (" + ConvertType(elType).BaseType + " o in " + GetCode(prop) + ") {\n" +
						"\t\t\t\tDebug.Assert(o != null);\n" +
						"\t\t\t\to.AcceptVisitor(this, data);\n" +
						"\t\t\t}";
				}
				m.Statements.Add(new CodeSnippetStatement(code));
				return true;
			}
			if (!typeof(INode).IsAssignableFrom(field.FieldType))
				return false;
			assertions.Add(AssertIsNotNull(prop));
			if (transformer) {
				m.Statements.Add(nodeStack.InvokeMethod("Push", prop));
			}
			m.Statements.Add(prop.InvokeMethod("AcceptVisitor",
			                                   Easy.This,
			                                   Easy.Var("data")));
			if (transformer) {
				m.Body.Assign(prop, nodeStack.InvokeMethod("Pop").CastTo(ConvertType(field.FieldType)));
			}
			return true;
		}
		
		static CodeExpression CreateToString(Type type)
		{
			CodeMethodInvokeExpression ie = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)),
			                                                               "Format");
			CodePrimitiveExpression prim = new CodePrimitiveExpression();
			ie.Parameters.Add(prim);
			string text = "[" + type.Name;
			int index = 0;
			do {
				foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)) {
					text += " " + GetPropertyName(field.Name) + "={" + index.ToString() + "}";
					index++;
					if (typeof(System.Collections.ICollection).IsAssignableFrom(field.FieldType)) {
						ie.Parameters.Add(new CodeSnippetExpression("GetCollectionString(" + GetPropertyName(field.Name) + ")"));
					} else {
						ie.Parameters.Add(new CodeVariableReferenceExpression(GetPropertyName(field.Name)));
					}
				}
				type = type.BaseType;
			} while (type != null);
			prim.Value = text + "]";
			if (ie.Parameters.Count == 1)
				return prim;
			else
				return ie;
			//	return String.Format("[AnonymousMethodExpression: Parameters={0} Body={1}]",
			//	                     GetCollectionString(Parameters),
			//	                     Body);
		}
		
		static void ProcessType(Type type, CodeTypeDeclaration ctd)
		{
			foreach (FieldInfo field in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)) {
				ctd.AddField(ConvertType(field.FieldType), field.Name).Attributes = 0;
			}
			foreach (FieldInfo field in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)) {
				EasyProperty p = ctd.AddProperty(ConvertType(field.FieldType), GetPropertyName(field.Name));
				p.Getter.Return(Easy.Var(field.Name));
				CodeExpression ex;
				if (field.FieldType.IsValueType)
					ex = new CodePropertySetValueReferenceExpression();
				else
					ex = GetDefaultValue("value", field);
				p.Setter.Assign(Easy.Var(field.Name), ex);
				if (typeof(INode).IsAssignableFrom(field.FieldType)) {
					if (typeof(INullable).IsAssignableFrom(field.FieldType)) {
						p.SetStatements.Add(new CodeSnippetStatement("\t\t\t\tif (!" +field.Name+".IsNull) "+field.Name+".Parent = this;"));
					} else {
						p.SetStatements.Add(new CodeSnippetStatement("\t\t\t\t"+field.Name+".Parent = this;"));
					}
				}
			}
			foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				CodeConstructor c = new CodeConstructor();
				if (type.IsAbstract)
					c.Attributes = MemberAttributes.Family;
				else
					c.Attributes = MemberAttributes.Public;
				ctd.Members.Add(c);
				ConstructorInfo baseCtor = GetBaseCtor(type);
				foreach(ParameterInfo param in ctor.GetParameters()) {
					c.Parameters.Add(new CodeParameterDeclarationExpression(ConvertType(param.ParameterType),
					                                                        param.Name));
					if (baseCtor != null && Array.Exists(baseCtor.GetParameters(), delegate(ParameterInfo p) { return param.Name == p.Name; }))
						continue;
					c.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(GetPropertyName(param.Name)),
					                                         new CodeVariableReferenceExpression(param.Name)));
				}
				if (baseCtor != null) {
					foreach(ParameterInfo param in baseCtor.GetParameters()) {
						c.BaseConstructorArgs.Add(new CodeVariableReferenceExpression(param.Name));
					}
				}
				// initialize fields that were not initialized by parameter
				foreach (FieldInfo field in type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic)) {
					if (field.FieldType.IsValueType && field.FieldType != typeof(Location))
						continue;
					if (Array.Exists(ctor.GetParameters(), delegate(ParameterInfo p) { return field.Name == p.Name; }))
						continue;
					c.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(field.Name),
					                                         GetDefaultValue(null, field)));
				}
			}
		}
		
		internal static ConstructorInfo GetBaseCtor(Type type)
		{
			ConstructorInfo[] list = type.BaseType.GetConstructors();
			if (list.Length == 0)
				return null;
			else
				return list[0];
		}
		
		internal static CodeExpression GetDefaultValue(string inputVariable, FieldInfo field)
		{
			string code;
			// get default value:
			if (field.FieldType == typeof(string)) {
				code = "\"\"";
				if (field.GetCustomAttributes(typeof(QuestionMarkDefaultAttribute), false).Length > 0) {
					if (inputVariable == null)
						return new CodePrimitiveExpression("?");
					else
						return new CodeSnippetExpression("string.IsNullOrEmpty(" + inputVariable + ") ? \"?\" : " + inputVariable);
				}
			} else if (field.FieldType.FullName.StartsWith("System.Collections.Generic.List")) {
				code = "new List<" + field.FieldType.GetGenericArguments()[0].Name + ">()";
			} else if (field.FieldType == typeof(Location)) {
				code = "Location.Empty";
			} else {
				code = field.FieldType.Name + ".Null";
			}
			if (inputVariable != null) {
				code = inputVariable + " ?? " + code;
			}
			return new CodeSnippetExpression(code);
		}
		
		internal static string GetFieldName(string typeName)
		{
			return char.ToLower(typeName[0]) + typeName.Substring(1);
		}
		
		internal static string GetPropertyName(string fieldName)
		{
			return char.ToUpper(fieldName[0]) + fieldName.Substring(1);
		}
		
		internal static CodeTypeReference ConvertType(Type type)
		{
			if (type.IsGenericType && !type.IsGenericTypeDefinition) {
				CodeTypeReference tr = ConvertType(type.GetGenericTypeDefinition());
				foreach (Type subType in type.GetGenericArguments()) {
					tr.TypeArguments.Add(ConvertType(subType));
				}
				return tr;
			} else if (type.FullName.StartsWith("VBDom") || type.FullName.StartsWith("System.Collections")) {
				if (type.Name == "Attribute")
					return new CodeTypeReference("ICSharpCode.NRefactory.VB.Dom.Attribute");
				return new CodeTypeReference(type.Name);
			} else {
				return new CodeTypeReference(type);
			}
		}
		
		static CodeTypeDeclaration CreateNodeTrackingDomVisitorClass(List<Type> nodeTypes)
		{
			CodeTypeDeclaration td = new CodeTypeDeclaration("NodeTrackingDomVisitor");
			td.TypeAttributes = TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract;
			td.BaseTypes.Add(new CodeTypeReference("AbstractDomVisitor"));
			
			string comment = "<summary>\n " +
				"The NodeTrackingDomVisitor will iterate through the whole Dom,\n " +
				"just like the AbstractDomVisitor, and calls the virtual methods\n " +
				"BeginVisit and EndVisit for each node being visited.\n " +
				"</summary>";
			td.Comments.Add(new CodeCommentStatement(comment, true));
			comment = "<remarks>\n " +
				"base.Visit(node, data) calls this.TrackedVisit(node, data), so if\n " +
				"you want to visit child nodes using the default visiting behaviour,\n " +
				"use base.TrackedVisit(parentNode, data).\n " +
				"</remarks>";
			td.Comments.Add(new CodeCommentStatement(comment, true));
			
			EasyMethod m = td.AddMethod("BeginVisit");
			m.Attributes = MemberAttributes.Family;
			m.AddParameter(Easy.TypeRef("INode"), "node");
			
			m = td.AddMethod("EndVisit");
			m.Attributes = MemberAttributes.Family;
			m.AddParameter(Easy.TypeRef("INode"), "node");
			
			foreach (Type type in nodeTypes) {
				if (!type.IsAbstract) {
					
					m = td.AddMethod(typeof(object), VisitPrefix + type.Name);
					m.Attributes = MemberAttributes.Public | MemberAttributes.Override;
					m.AddParameter(ConvertType(type), GetFieldName(type.Name));
					m.AddParameter(new CodeTypeReference(typeof(object)), "data");
					
					CodeExpression var = Easy.Var(GetFieldName(type.Name));
					
					m.Body.InvokeMethod(Easy.This, "BeginVisit", var);
					m.Body.DeclareVariable(typeof(object), "result").InitExpression
						= Easy.This.InvokeMethod("TrackedVisit" + type.Name, var, Easy.Var("data"));
					m.Body.InvokeMethod(Easy.This, "EndVisit", var);
					m.Body.Return(Easy.Var("result"));
				}
			}
			
			foreach (Type type in nodeTypes) {
				if (!type.IsAbstract) {
					
					m = td.AddMethod(typeof(object), "TrackedVisit" + type.Name);
					m.Attributes = MemberAttributes.Public;
					m.AddParameter(ConvertType(type), GetFieldName(type.Name));
					m.AddParameter(new CodeTypeReference(typeof(object)), "data");
					
					m.Body.Return(Easy.Base.InvokeMethod(VisitPrefix + type.Name, Easy.Var(GetFieldName(type.Name)), Easy.Var("data")));
				}
			}
			
			return td;
		}
		
		static CodeTypeDeclaration CreateNotImplementedDomVisitorClass(List<Type> nodeTypes)
		{
			CodeTypeDeclaration td = new CodeTypeDeclaration("NotImplementedDomVisitor");
			td.TypeAttributes = TypeAttributes.Public | TypeAttributes.Class;
			td.BaseTypes.Add(new CodeTypeReference("IDomVisitor"));
			
			string comment = "<summary>\n " +
				"IDomVisitor implementation that always throws NotImplementedExceptions.\n " +
				"</summary>";
			td.Comments.Add(new CodeCommentStatement(comment, true));
			
			foreach (Type type in nodeTypes) {
				if (!type.IsAbstract) {
					
					EasyMethod m = td.AddMethod(typeof(object), VisitPrefix + type.Name);
					m.Attributes = MemberAttributes.Public;
					m.AddParameter(ConvertType(type), GetFieldName(type.Name));
					m.AddParameter(new CodeTypeReference(typeof(object)), "data");
					
					m.Body.Throw(Easy.New(typeof(NotImplementedException), Easy.Prim(type.Name)));
				}
			}
			
			return td;
		}
	}
}
