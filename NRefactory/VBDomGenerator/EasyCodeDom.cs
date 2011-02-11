// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.CodeDom;

namespace ICSharpCode.EasyCodeDom
{
	public static class Easy
	{
		public static CodeTypeReference TypeRef(Type type)
		{
			return new CodeTypeReference(type, CodeTypeReferenceOptions.GlobalReference);
		}
		public static CodeTypeReference TypeRef(CodeTypeDeclaration type)
		{
			return new CodeTypeReference(type.Name);
		}
		public static CodeTypeReference TypeRef(string typeName, params string[] typeArguments)
		{
			CodeTypeReference tr = new CodeTypeReference(typeName);
			foreach (string ta in typeArguments) {
				tr.TypeArguments.Add(ta);
			}
			return tr;
		}
		
		/// <summary>
		/// Gets the CodeExpression for any primitive value that can be expressed as literal.
		/// Also works for enumeration values.
		/// </summary>
		public static CodeExpression Prim(object literalValue)
		{
			if (literalValue is Enum) {
				return Type(literalValue.GetType()).Field(literalValue.ToString());
			} else {
				return new CodePrimitiveExpression(literalValue);
			}
		}
		
		public static CodeTypeReferenceExpression Type(Type type)
		{
			return Type(TypeRef(type));
		}
		public static CodeTypeReferenceExpression Type(CodeTypeReference type)
		{
			return new CodeTypeReferenceExpression(type);
		}
		public static CodeTypeReferenceExpression Type(string type)
		{
			return Type(new CodeTypeReference(type));
		}
		
		public static CodeTypeOfExpression TypeOf(Type type)
		{
			return TypeOf(TypeRef(type));
		}
		public static CodeTypeOfExpression TypeOf(CodeTypeReference type)
		{
			return new CodeTypeOfExpression(type);
		}
		
		public static CodeObjectCreateExpression New(Type type, params CodeExpression[] arguments)
		{
			return New(TypeRef(type), arguments);
		}
		public static CodeObjectCreateExpression New(CodeTypeReference type, params CodeExpression[] arguments)
		{
			return new CodeObjectCreateExpression(type, arguments);
		}
		
		public static CodeVariableReferenceExpression Var(string name)
		{
			return new CodeVariableReferenceExpression(name);
		}
		
		public static CodeBinaryOperatorExpression Binary(CodeExpression left,
		                                                  CodeBinaryOperatorType op,
		                                                  CodeExpression right)
		{
			return new CodeBinaryOperatorExpression(left, op, right);
		}
		
		public static CodeThisReferenceExpression This {
			get {
				return new CodeThisReferenceExpression();
			}
		}
		
		public static CodeBaseReferenceExpression Base {
			get {
				return new CodeBaseReferenceExpression();
			}
		}
		
		public static CodePropertySetValueReferenceExpression Value {
			get {
				return new CodePropertySetValueReferenceExpression();
			}
		}
		
		public static CodePrimitiveExpression Null {
			get {
				return new CodePrimitiveExpression(null);
			}
		}
		
		public static void AddSummary(CodeTypeMember member, string summary)
		{
			member.Comments.Add(new CodeCommentStatement("<summary>", true));
			member.Comments.Add(new CodeCommentStatement(summary, true));
			member.Comments.Add(new CodeCommentStatement("</summary>", true));
		}
		
		internal static CodeAttributeDeclaration AddAttribute(CodeAttributeDeclarationCollection col,
		                                                      CodeTypeReference type,
		                                                      CodeExpression[] arguments)
		{
			CodeAttributeArgument[] attributeArguments = new CodeAttributeArgument[arguments.Length];
			for (int i = 0; i < arguments.Length; i++) {
				attributeArguments[i] = new CodeAttributeArgument(arguments[i]);
			}
			CodeAttributeDeclaration cad = new CodeAttributeDeclaration(type, attributeArguments);
			col.Add(cad);
			return cad;
		}
	}
	
	public static class ExtensionMethods
	{
		public static CodeMethodInvokeExpression InvokeMethod(this CodeExpression expr, string name, params CodeExpression[] arguments)
		{
			return new CodeMethodInvokeExpression(expr, name, arguments);
		}
		
		public static CodeCastExpression CastTo(this CodeExpression expr, Type type)
		{
			return expr.CastTo(Easy.TypeRef(type));
		}
		public static CodeCastExpression CastTo(this CodeExpression expr, CodeTypeReference type)
		{
			return new CodeCastExpression(type, expr);
		}
		
		public static CodeIndexerExpression Index(this CodeExpression expr, params CodeExpression[] indices)
		{
			return new CodeIndexerExpression(expr, indices);
		}
		
		public static CodeFieldReferenceExpression Field(this CodeExpression expr, string name)
		{
			return new CodeFieldReferenceExpression(expr, name);
		}
		
		public static CodePropertyReferenceExpression Property(this CodeExpression expr, string name)
		{
			return new CodePropertyReferenceExpression(expr, name);
		}
		
		public static CodeNamespace AddNamespace(this CodeCompileUnit ccu, string name)
		{
			CodeNamespace n = new CodeNamespace(name);
			ccu.Namespaces.Add(n);
			return n;
		}
		
		public static CodeTypeDeclaration AddType(this CodeNamespace ns, string name)
		{
			CodeTypeDeclaration n = new CodeTypeDeclaration(name);
			ns.Types.Add(n);
			return n;
		}
		
		public static CodeNamespaceImport AddImport(this CodeNamespace ns, string nameSpace)
		{
			CodeNamespaceImport cni = new CodeNamespaceImport(nameSpace);
			ns.Imports.Add(cni);
			return cni;
		}
		
		public static CodeMemberField AddField(this CodeTypeDeclaration typeDecl, Type type, string name)
		{
			return typeDecl.AddField(Easy.TypeRef(type), name);
		}
		public static CodeMemberField AddField(this CodeTypeDeclaration typeDecl, CodeTypeReference type, string name)
		{
			CodeMemberField f = new CodeMemberField(type, name);
			typeDecl.Members.Add(f);
			return f;
		}
		
		public static EasyProperty AddProperty(this CodeTypeDeclaration typeDecl, Type type, string name)
		{
			return AddProperty(typeDecl, Easy.TypeRef(type), name);
		}
		public static EasyProperty AddProperty(this CodeTypeDeclaration typeDecl, CodeTypeReference type, string name)
		{
			EasyProperty p = new EasyProperty(type, name);
			typeDecl.Members.Add(p);
			if (typeDecl.IsInterface == false) {
				p.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			}
			return p;
		}
		
		public static EasyProperty AddProperty(this CodeTypeDeclaration typeDecl, CodeMemberField field, string name)
		{
			EasyProperty p = AddProperty(typeDecl, field.Type, name);
			p.Getter.Return(new CodeVariableReferenceExpression(field.Name));
			p.Attributes |= field.Attributes & MemberAttributes.Static; // copy static flag
			return p;
		}
		
		/// <summary>
		/// Adds a method with return type <c>void</c> and attributes=Public|Final to this type.
		/// </summary>
		public static EasyMethod AddMethod(this CodeTypeDeclaration typeDecl, string name)
		{
			return AddMethod(typeDecl, Easy.TypeRef(typeof(void)), name);
		}
		/// <summary>
		/// Adds a method with return type <paramref name="type"/> and attributes=Public|Final to this type.
		/// </summary>
		public static EasyMethod AddMethod(this CodeTypeDeclaration typeDecl, Type type, string name)
		{
			return AddMethod(typeDecl, Easy.TypeRef(type), name);
		}
		/// <summary>
		/// Adds a method with return type <paramref name="type"/> and attributes=Public|Final to this type.
		/// </summary>
		public static EasyMethod AddMethod(this CodeTypeDeclaration typeDecl, CodeTypeReference type, string name)
		{
			EasyMethod p = new EasyMethod(type, name);
			typeDecl.Members.Add(p);
			if (typeDecl.IsInterface == false) {
				p.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			}
			return p;
		}
		
		public static CodeAttributeDeclaration AddAttribute(this CodeTypeMember typeMember, Type type, params CodeExpression[] arguments)
		{
			return Easy.AddAttribute(typeMember.CustomAttributes, Easy.TypeRef(type), arguments);
		}
		public static CodeAttributeDeclaration AddAttribute(this CodeTypeMember typeMember, CodeTypeReference type, params CodeExpression[] arguments)
		{
			return Easy.AddAttribute(typeMember.CustomAttributes, type, arguments);
		}
	}
	
	public class EasyProperty : CodeMemberProperty
	{
		EasyBlock getter, setter;
		
		public EasyProperty()
		{
			getter = new EasyBlock(this.GetStatements);
			setter = new EasyBlock(this.SetStatements);
		}
		
		public EasyProperty(CodeTypeReference type, string name) : this()
		{
			this.Type = type;
			this.Name = name;
		}
		
		public EasyBlock Getter {
			get { return getter; }
		}
		
		public EasyBlock Setter {
			get { return setter; }
		}
	}
	
	public class EasyMethod : CodeMemberMethod
	{
		EasyBlock body;
		
		public EasyMethod()
		{
			body = new EasyBlock(this.Statements);
		}
		
		public EasyMethod(CodeTypeReference type, string name) : this()
		{
			this.ReturnType = type;
			this.Name = name;
		}
		
		public CodeParameterDeclarationExpression AddParameter(Type type, string name)
		{
			return AddParameter(Easy.TypeRef(type), name);
		}
		public CodeParameterDeclarationExpression AddParameter(CodeTypeReference type, string name)
		{
			CodeParameterDeclarationExpression cpde;
			cpde = new CodeParameterDeclarationExpression(type, name);
			this.Parameters.Add(cpde);
			return cpde;
		}
		
		public EasyBlock Body {
			get { return body; }
		}
	}
	
	public sealed class EasyBlock
	{
		readonly CodeStatementCollection csc;
		
		public EasyBlock(CodeStatementCollection csc)
		{
			this.csc = csc;
		}
		
		public CodeMethodReturnStatement Return(CodeExpression expr)
		{
			CodeMethodReturnStatement st = new CodeMethodReturnStatement(expr);
			csc.Add(st);
			return st;
		}
		
		public CodeThrowExceptionStatement Throw(CodeExpression expr)
		{
			CodeThrowExceptionStatement st = new CodeThrowExceptionStatement(expr);
			csc.Add(st);
			return st;
		}
		
		public CodeAssignStatement Assign(CodeExpression lhs, CodeExpression rhs)
		{
			CodeAssignStatement st = new CodeAssignStatement(lhs, rhs);
			csc.Add(st);
			return st;
		}
		
		/// <summary>
		/// Execute one expression as statement.
		/// </summary>
		public CodeExpressionStatement Add(CodeExpression expr)
		{
			CodeExpressionStatement st = new CodeExpressionStatement(expr);
			csc.Add(st);
			return st;
		}
		
		/// <summary>
		/// Adds the statement.
		/// </summary>
		public CodeStatement Add(CodeStatement st)
		{
			csc.Add(st);
			return st;
		}
		
		/// <summary>
		/// Invoke a method on target as statement.
		/// </summary>
		public CodeExpressionStatement InvokeMethod(CodeExpression target, string name, params CodeExpression[] arguments)
		{
			return Add(new CodeMethodInvokeExpression(target, name, arguments));
		}
		
		/// <summary>
		/// Declares a local variable.
		/// </summary>
		public CodeVariableDeclarationStatement DeclareVariable(Type type, string name)
		{
			return DeclareVariable(Easy.TypeRef(type), name);
		}
		/// <summary>
		/// Declares a local variable.
		/// </summary>
		public CodeVariableDeclarationStatement DeclareVariable(CodeTypeReference type, string name)
		{
			CodeVariableDeclarationStatement st = new CodeVariableDeclarationStatement(type, name);
			csc.Add(st);
			return st;
		}
	}
}
