// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

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
		/// Gets the EasyExpression for any primitive value that can be expressed as literal.
		/// Also works for enumeration values.
		/// </summary>
		public static EasyExpression Prim(object literalValue)
		{
			if (literalValue is Enum) {
				return Type(literalValue.GetType()).Field(literalValue.ToString());
			} else {
				return new EasyExpression(new CodePrimitiveExpression(literalValue));
			}
		}
		
		public static EasyExpression Type(Type type)
		{
			return Type(TypeRef(type));
		}
		public static EasyExpression Type(CodeTypeReference type)
		{
			return new EasyExpression(new CodeTypeReferenceExpression(type));
		}
		public static EasyExpression Type(string type)
		{
			return Type(new CodeTypeReference(type));
		}
		
		public static EasyExpression TypeOf(Type type)
		{
			return TypeOf(TypeRef(type));
		}
		public static EasyExpression TypeOf(CodeTypeReference type)
		{
			return new EasyExpression(new CodeTypeOfExpression(type));
		}
		
		public static EasyExpression New(Type type, params CodeExpression[] arguments)
		{
			return New(TypeRef(type), arguments);
		}
		public static EasyExpression New(CodeTypeReference type, params CodeExpression[] arguments)
		{
			return new EasyExpression(new CodeObjectCreateExpression(type, arguments));
		}
		
		public static EasyExpression Var(string name)
		{
			return new EasyExpression(new CodeVariableReferenceExpression(name));
		}
		
		public static EasyExpression Binary(CodeExpression left,
		                                    CodeBinaryOperatorType op,
		                                    CodeExpression right)
		{
			return new EasyExpression(new CodeBinaryOperatorExpression(left, op, right));
		}
		
		public static EasyExpression This {
			get {
				return new EasyExpression(new CodeThisReferenceExpression());
			}
		}
		
		public static EasyExpression Base {
			get {
				return new EasyExpression(new CodeBaseReferenceExpression());
			}
		}
		
		public static EasyExpression Value {
			get {
				return new EasyExpression(new CodePropertySetValueReferenceExpression());
			}
		}
		
		public static EasyExpression Null {
			get {
				return new EasyExpression(new CodePrimitiveExpression(null));
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
	
	public sealed class EasyExpression
	{
		readonly CodeExpression expr;
		
		public EasyExpression(CodeExpression expr)
		{
			this.expr = expr;
		}
		
		public static implicit operator CodeExpression(EasyExpression expr)
		{
			return expr.expr;
		}
		
		public EasyExpression InvokeMethod(string name, params CodeExpression[] arguments)
		{
			return new EasyExpression(new CodeMethodInvokeExpression(expr, name, arguments));
		}
		
		public EasyExpression CastTo(Type type)
		{
			return CastTo(Easy.TypeRef(type));
		}
		public EasyExpression CastTo(CodeTypeReference type)
		{
			return new EasyExpression(new CodeCastExpression(type, expr));
		}
		
		public EasyExpression Index(params CodeExpression[] indices)
		{
			return new EasyExpression(new CodeIndexerExpression(expr, indices));
		}
		
		public EasyExpression Field(string name)
		{
			return new EasyExpression(new CodeFieldReferenceExpression(expr, name));
		}
		
		public EasyExpression Property(string name)
		{
			return new EasyExpression(new CodePropertyReferenceExpression(expr, name));
		}
	}
	
	public class EasyCompileUnit : CodeCompileUnit
	{
		public EasyNamespace AddNamespace(string name)
		{
			EasyNamespace n = new EasyNamespace(name);
			this.Namespaces.Add(n);
			return n;
		}
	}
	
	public class EasyNamespace : CodeNamespace
	{
		public EasyNamespace() : base() {}
		public EasyNamespace(string name) : base(name) {}
		
		public EasyTypeDeclaration AddType(string name)
		{
			EasyTypeDeclaration n = new EasyTypeDeclaration(name);
			this.Types.Add(n);
			return n;
		}
		
		public CodeNamespaceImport AddImport(string nameSpace)
		{
			CodeNamespaceImport cni = new CodeNamespaceImport(nameSpace);
			this.Imports.Add(cni);
			return cni;
		}
	}
	
	public class EasyTypeDeclaration : CodeTypeDeclaration
	{
		public EasyTypeDeclaration() : base() {}
		public EasyTypeDeclaration(string name) : base(name) {}
		
		public CodeAttributeDeclaration AddAttribute(Type type, params CodeExpression[] arguments)
		{
			return Easy.AddAttribute(this.CustomAttributes, Easy.TypeRef(type), arguments);
		}
		public CodeAttributeDeclaration AddAttribute(CodeTypeReference type, params CodeExpression[] arguments)
		{
			return Easy.AddAttribute(this.CustomAttributes, type, arguments);
		}
		
		public EasyField AddField(Type type, string name)
		{
			return AddField(Easy.TypeRef(type), name);
		}
		public EasyField AddField(CodeTypeReference type, string name)
		{
			EasyField f = new EasyField(type, name);
			this.Members.Add(f);
			return f;
		}
		
		public EasyProperty AddProperty(Type type, string name)
		{
			return AddProperty(Easy.TypeRef(type), name);
		}
		public EasyProperty AddProperty(CodeTypeReference type, string name)
		{
			EasyProperty p = new EasyProperty(type, name);
			this.Members.Add(p);
			if (this.IsInterface == false) {
				p.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			}
			return p;
		}
		
		public EasyProperty AddProperty(CodeMemberField field, string name)
		{
			EasyProperty p = AddProperty(field.Type, name);
			p.Getter.Return(new CodeVariableReferenceExpression(field.Name));
			p.Attributes |= field.Attributes & MemberAttributes.Static; // copy static flag
			return p;
		}
		
		/// <summary>
		/// Adds a method with return type <c>void</c> and attributes=Public|Final to this type.
		/// </summary>
		public EasyMethod AddMethod(string name)
		{
			return AddMethod(Easy.TypeRef(typeof(void)), name);
		}
		/// <summary>
		/// Adds a method with return type <paramref name="type"/> and attributes=Public|Final to this type.
		/// </summary>
		public EasyMethod AddMethod(Type type, string name)
		{
			return AddMethod(Easy.TypeRef(type), name);
		}
		/// <summary>
		/// Adds a method with return type <paramref name="type"/> and attributes=Public|Final to this type.
		/// </summary>
		public EasyMethod AddMethod(CodeTypeReference type, string name)
		{
			EasyMethod p = new EasyMethod(type, name);
			this.Members.Add(p);
			if (this.IsInterface == false) {
				p.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			}
			return p;
		}
	}
	
	public class EasyField : CodeMemberField
	{
		public EasyField() : base() {}
		public EasyField(CodeTypeReference type, string name) : base(type, name) {}
		
		public CodeAttributeDeclaration AddAttribute(Type type, params CodeExpression[] arguments)
		{
			return Easy.AddAttribute(this.CustomAttributes, Easy.TypeRef(type), arguments);
		}
		public CodeAttributeDeclaration AddAttribute(CodeTypeReference type, params CodeExpression[] arguments)
		{
			return Easy.AddAttribute(this.CustomAttributes, type, arguments);
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
		
		public CodeAttributeDeclaration AddAttribute(Type type, params CodeExpression[] arguments)
		{
			return Easy.AddAttribute(this.CustomAttributes, Easy.TypeRef(type), arguments);
		}
		public CodeAttributeDeclaration AddAttribute(CodeTypeReference type, params CodeExpression[] arguments)
		{
			return Easy.AddAttribute(this.CustomAttributes, type, arguments);
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
		
		public CodeAttributeDeclaration AddAttribute(Type type, params CodeExpression[] arguments)
		{
			return Easy.AddAttribute(this.CustomAttributes, Easy.TypeRef(type), arguments);
		}
		public CodeAttributeDeclaration AddAttribute(CodeTypeReference type, params CodeExpression[] arguments)
		{
			return Easy.AddAttribute(this.CustomAttributes, type, arguments);
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
