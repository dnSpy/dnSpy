// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Visitors
{
	/// <summary>
	/// Prefixes the names of the specified fields with the prefix and replaces the use.
	/// </summary>
	public class PrefixFieldsVisitor : AbstractAstVisitor
	{
		List<VariableDeclaration> fields;
		List<string> curBlock = new List<string>();
		Stack<List<string>> blocks = new Stack<List<string>>();
		string prefix;
		
		public PrefixFieldsVisitor(List<VariableDeclaration> fields, string prefix)
		{
			this.fields = fields;
			this.prefix = prefix;
		}
		
		public void Run(INode typeDeclaration)
		{
			typeDeclaration.AcceptVisitor(this, null);
			foreach (VariableDeclaration decl in fields) {
				decl.Name = prefix + decl.Name;
			}
		}
		
		public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			Push();
			object result = base.VisitTypeDeclaration(typeDeclaration, data);
			Pop();
			return result;
		}
		
		public override object VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			Push();
			object result = base.VisitBlockStatement(blockStatement, data);
			Pop();
			return result;
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			Push();
			object result = base.VisitMethodDeclaration(methodDeclaration, data);
			Pop();
			return result;
		}
		
		public override object VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			Push();
			object result = base.VisitPropertyDeclaration(propertyDeclaration, data);
			Pop();
			return result;
		}
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			Push();
			object result = base.VisitConstructorDeclaration(constructorDeclaration, data);
			Pop();
			return result;
		}
		
		private void Push()
		{
			blocks.Push(curBlock);
			curBlock = new List<string>();
		}
		
		private void Pop()
		{
			curBlock = blocks.Pop();
		}
		
		public override object VisitVariableDeclaration(VariableDeclaration variableDeclaration, object data)
		{
			// process local variables only
			if (fields.Contains(variableDeclaration)) {
				return null;
			}
			curBlock.Add(variableDeclaration.Name);
			return base.VisitVariableDeclaration(variableDeclaration, data);
		}
		
		public override object VisitParameterDeclarationExpression(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			curBlock.Add(parameterDeclarationExpression.ParameterName);
			//print("add parameter ${parameterDeclarationExpression.ParameterName} to block")
			return base.VisitParameterDeclarationExpression(parameterDeclarationExpression, data);
		}
		
		public override object VisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			curBlock.Add(foreachStatement.VariableName);
			return base.VisitForeachStatement(foreachStatement, data);
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			string name = identifierExpression.Identifier;
			foreach (VariableDeclaration var in fields) {
				if (var.Name == name && !IsLocal(name)) {
					identifierExpression.Identifier = prefix + name;
					break;
				}
			}
			return base.VisitIdentifierExpression(identifierExpression, data);
		}
		
		public override object VisitMemberReferenceExpression(MemberReferenceExpression fieldReferenceExpression, object data)
		{
			if (fieldReferenceExpression.TargetObject is ThisReferenceExpression) {
				string name = fieldReferenceExpression.MemberName;
				foreach (VariableDeclaration var in fields) {
					if (var.Name == name) {
						fieldReferenceExpression.MemberName = prefix + name;
						break;
					}
				}
			}
			return base.VisitMemberReferenceExpression(fieldReferenceExpression, data);
		}
		
		bool IsLocal(string name)
		{
			foreach (List<string> block in blocks) {
				if (block.Contains(name))
					return true;
			}
			return curBlock.Contains(name);
		}
	}
}
