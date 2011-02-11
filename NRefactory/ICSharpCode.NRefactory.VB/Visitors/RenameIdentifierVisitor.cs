// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.VB.Dom;

namespace ICSharpCode.NRefactory.VB.Visitors
{
	class RenameIdentifierVisitor : AbstractDomVisitor
	{
		protected StringComparer nameComparer;
		protected string from, to;
		
		public RenameIdentifierVisitor(string from, string to, StringComparer nameComparer)
		{
			this.nameComparer = nameComparer;
			this.from = from;
			this.to = to;
		}
		
		public override object VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			if (nameComparer.Equals(identifierExpression.Identifier, from)) {
				identifierExpression.Identifier = to;
			}
			return base.VisitIdentifierExpression(identifierExpression, data);
		}
	}
	
	sealed class RenameLocalVariableVisitor : RenameIdentifierVisitor
	{
		public RenameLocalVariableVisitor(string from, string to, StringComparer nameComparer)
			: base(from, to, nameComparer)
		{
		}
		
		public override object VisitVariableDeclaration(VariableDeclaration variableDeclaration, object data)
		{
			if (nameComparer.Equals(from, variableDeclaration.Name)) {
				variableDeclaration.Name = to;
			}
			return base.VisitVariableDeclaration(variableDeclaration, data);
		}
		
		public override object VisitParameterDeclarationExpression(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			if (nameComparer.Equals(from, parameterDeclarationExpression.ParameterName)) {
				parameterDeclarationExpression.ParameterName = to;
			}
			return base.VisitParameterDeclarationExpression(parameterDeclarationExpression, data);
		}
		
		public override object VisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			if (nameComparer.Equals(from, foreachStatement.VariableName)) {
				foreachStatement.VariableName = to;
			}
			return base.VisitForeachStatement(foreachStatement, data);
		}
	}
}
