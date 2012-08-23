//
// ConvertInitializerToExplicitInitializationsAction.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Convert to explicit initializers",
	               Description = "Converts an object or collection initializer to explicit initializations.")]
	public class ConvertInitializerToExplicitInitializationsAction : ICodeActionProvider
	{
		#region ICodeActionProvider implementation

		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var codeAction = GetActionForVariableInitializer(context);
			if (codeAction != null) {
				yield return codeAction;
				yield break;
			}
			codeAction = GetActionForAssignmentExpression(context);
			if (codeAction != null) {
				yield return codeAction;
				yield break;
			}
		}
		
		public CodeAction GetActionForVariableInitializer(RefactoringContext context)
		{
			var variableInitializer = context.GetNode<VariableInitializer>();
			if (variableInitializer == null)
				return null;
			var declaration = variableInitializer.Parent as VariableDeclarationStatement;
			if (declaration == null)
				return null;
			if (variableInitializer.Initializer.IsNull)
				return null;
			var objectCreateExpression = variableInitializer.Initializer as ObjectCreateExpression;
			if (objectCreateExpression == null)
				return null;
			var converter = new InitializerConversionVisitor(context);
			Expression finalExpression;
			var statements = converter.ConvertInitializer(objectCreateExpression, out finalExpression);
			if (statements.Count > 0) {
				return new CodeAction(context.TranslateString("Convert to explicit initializers"), script => {
					foreach (var statement in statements) {
						script.InsertBefore(declaration, statement);
					}
					script.Replace(variableInitializer.Initializer, finalExpression);
				});
			}
			return null;
		}
		
		public CodeAction GetActionForAssignmentExpression(RefactoringContext context)
		{
			var assignmentExpression = context.GetNode<AssignmentExpression>();
			if (assignmentExpression == null)
				return null;
			var expressionStatement = assignmentExpression.Parent as ExpressionStatement;
			if (expressionStatement == null)
				return null;
			var objectCreateExpression = assignmentExpression.Right as ObjectCreateExpression;
			if (objectCreateExpression == null)
				return null;
			var converter = new InitializerConversionVisitor(context);
			Expression finalExpression;
			var statements = converter.ConvertInitializer(objectCreateExpression, out finalExpression);
			if (statements.Count > 0) {
				return new CodeAction(context.TranslateString("Convert to explicit initializers"), script => {
					foreach (var statement in statements) {
						script.InsertBefore(expressionStatement, statement);
					}
					script.Replace(assignmentExpression.Right, finalExpression);
				});
			}
			return null;
		}
		#endregion

		class InitializerConversionVisitor : DepthFirstAstVisitor<Expression, Expression>
		{
			RefactoringContext context;
			IList<Statement> statements;
			NamingHelper namingHelper;

			public InitializerConversionVisitor(RefactoringContext context)
			{
				this.context = context;
				namingHelper = new NamingHelper(context);
			}

			AstType GetDeclarationType(AstType type)
			{
				AstType declarationType;
				if (context.UseExplicitTypes) {
					declarationType = type.Clone();
				} else {
					declarationType = new SimpleType("var");
				}
				return declarationType;
			}

			public IList<Statement> ConvertInitializer(ObjectCreateExpression initializer, out Expression finalExpression)
			{
				statements = new List<Statement>();

				finalExpression = initializer.AcceptVisitor(this, null);

				return statements;
			}

			public override Expression VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, Expression data)
			{
				var creationType = objectCreateExpression.Type.Clone();

				var parameters = objectCreateExpression.Arguments.Select(arg => arg.Clone());
				var newCreation = new ObjectCreateExpression(creationType, parameters);
				if (objectCreateExpression.Initializer.IsNull) {
					return newCreation;
				} else {
					AstType declarationType = GetDeclarationType(objectCreateExpression.Type);
					var name = namingHelper.GenerateVariableName(objectCreateExpression.Type);
					var variableInitializer = new VariableDeclarationStatement(declarationType, name, newCreation);
					statements.Add(variableInitializer);

					var identifier = new IdentifierExpression(name);
					base.VisitObjectCreateExpression(objectCreateExpression, identifier);

					return identifier;
				}
			}

			public override Expression VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression, Expression data)
			{
				if (!(arrayInitializerExpression.Parent is ArrayInitializerExpression)) {
					return base.VisitArrayInitializerExpression(arrayInitializerExpression, data);
				}
				// this a tuple in a collection initializer
				var arguments = arrayInitializerExpression.Elements.Select(element => element.AcceptVisitor(this, null).Clone());
				var method = new MemberReferenceExpression {
					Target = data.Clone(),
					MemberName = "Add"
				};
				var statement = new ExpressionStatement(new InvocationExpression(method, arguments));
				statements.Add(statement);

				return null;
			}

			public override Expression VisitNamedExpression(NamedExpression namedExpression, Expression data)
			{
				var member = new MemberReferenceExpression {
					Target = data.Clone(),
					MemberName = namedExpression.Name
				};
				var expression = namedExpression.Expression.AcceptVisitor(this, member);
				if (expression != null) {
					var statement = new ExpressionStatement {
						Expression = new AssignmentExpression {
							Left = member,
							Right = expression.Clone()
						}
					};
					statements.Add(statement);
				}

				return null;
			}

			protected override Expression VisitChildren(AstNode node, Expression data)
			{
				// Most expressions should just be used as-is, and
				// a) need not be visited
				// b) only return themselves
				if (node is Expression && !(node is ObjectCreateExpression || node is ArrayInitializerExpression || node is NamedExpression)){
					return (Expression)node;
				}
				return base.VisitChildren(node, data);
			}
		}
	}
}

