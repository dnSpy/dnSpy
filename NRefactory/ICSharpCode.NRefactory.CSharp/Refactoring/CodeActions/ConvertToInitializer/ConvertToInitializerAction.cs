//
// ConvertToInitializerAction.cs
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Convert to initializer",
	               Description = "Converts a set of assignments and .Add() calls to an initializer.")]
	public class ConvertToInitializerAction : ICodeActionProvider
	{
		#region ICodeActionProvider implementation
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var initializer = context.GetNode<VariableInitializer>();
			if (initializer != null) {
				var action = HandleInitializer(context, initializer);
				if (action != null)
					yield return action;
			}
			var expressionStatement = context.GetNode<ExpressionStatement>();
			if (expressionStatement != null) {
				var action = HandleExpressionStatement(context, expressionStatement);
				if (action != null)
					yield return action;
			}
		}

		CodeAction HandleInitializer(RefactoringContext context, VariableInitializer initializer)
		{
			var objectCreateExpression = initializer.Initializer as ObjectCreateExpression;
			if (objectCreateExpression == null)
				return null;
			var initializerRR = context.Resolve(initializer) as LocalResolveResult;
			if (initializerRR == null)
				return null;
			IList<AstNode> statements = GetNodes(context.GetNode<Statement>());
			var converter = new StatementsToInitializerConverter(context);
			var newInitializer = converter.ConvertToInitializer(initializer, ref statements);
			if (statements.Count == 0)
				return null;
			return MakeAction(context, initializer, newInitializer, statements);
		}

		CodeAction HandleExpressionStatement(RefactoringContext context, ExpressionStatement expressionStatement)
		{
			var expression = expressionStatement.Expression as AssignmentExpression;
			if (expression == null)
				return null;
			if (!(expression.Right is ObjectCreateExpression))
				return null;
			var expressionResolveResult = context.Resolve(expression.Left);
			if (!(expressionResolveResult is LocalResolveResult) && !(expressionResolveResult is MemberResolveResult))
				return null;
			IList<AstNode> statements = GetNodes(context.GetNode<Statement>());
			var converter = new StatementsToInitializerConverter(context);
			var newExpression = converter.ConvertToInitializer(expression, ref statements);
			if (statements.Count == 0)
				return null;
			return MakeAction(context, expression, newExpression, statements);
		}
		
		List<AstNode> GetNodes(Statement startStatement)
		{
			var statements = new List<AstNode>();
			AstNode currentNode = startStatement.NextSibling;
			while (currentNode != null) {
				if (currentNode is Statement || currentNode is Comment)
					statements.Add(currentNode);
				currentNode = currentNode.NextSibling;
			}
			return statements;
		}
		
		CodeAction MakeAction(RefactoringContext context, AstNode oldNode, AstNode replacementNode, IEnumerable<AstNode> toRemove)
		{
			return new CodeAction(context.TranslateString("Convert to initializer"), script => {
				foreach (var statement in toRemove)
					script.Remove(statement);
				script.Replace(oldNode, replacementNode);
			});
		}
		#endregion
	}
}

