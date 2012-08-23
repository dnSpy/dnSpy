//
// IterateViaForeachTests.cs
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[ContextAction("Iterate via foreach", Description = "Iterates an IEnumerable with a foreach loop.")]
	public class IterateViaForeachAction : ICodeActionProvider
	{
		#region ICodeActionProvider implementation

		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			CodeAction action;
			action = ActionFromUsingStatement(context);
			if (action != null)
				yield return action;
			action = ActionFromVariableInitializer(context);
			if (action != null)
				yield return action;
			action = ActionFromExpressionStatement(context);
			if (action != null)
				yield return action;
		}

		CodeAction ActionFromUsingStatement(RefactoringContext context)
		{
			var initializer = context.GetNode<VariableInitializer>();
			if (initializer == null)
				return null;
			var initializerRR = context.Resolve(initializer) as LocalResolveResult;
			if (initializerRR == null)
				return null;
			var elementType = GetElementType(initializerRR, context);
			if (elementType == null)
				return null;
			var usingStatement = initializer.Parent.Parent as UsingStatement;
			if (usingStatement == null)
				return null;
			return new CodeAction(context.TranslateString("Iterate via foreach"), script => {
				var iterator = MakeForeach(new IdentifierExpression(initializer.Name), elementType, context);
				if (usingStatement.EmbeddedStatement is EmptyStatement) {
					var blockStatement = new BlockStatement();
					blockStatement.Statements.Add(iterator);
					script.Replace(usingStatement.EmbeddedStatement, blockStatement);
					script.FormatText(blockStatement);
				} else if (usingStatement.EmbeddedStatement is BlockStatement) {
					var anchorNode = usingStatement.EmbeddedStatement.FirstChild;
					script.InsertAfter(anchorNode, iterator);
					script.FormatText(usingStatement.EmbeddedStatement);
				}
			});
		}

		CodeAction ActionFromVariableInitializer(RefactoringContext context)
		{
			var initializer = context.GetNode<VariableInitializer>();
			if (initializer == null || initializer.Parent.Parent is ForStatement)
				return null;
			var initializerRR = context.Resolve(initializer) as LocalResolveResult;
			if (initializerRR == null)
				return null;
			var elementType = GetElementType(initializerRR, context);
			if (elementType == null)
				return null;

			return new CodeAction(context.TranslateString("Iterate via foreach"), script => {
				var iterator = MakeForeach(new IdentifierExpression(initializer.Name), elementType, context);
				script.InsertAfter(context.GetNode<Statement>(), iterator);
			});
		}

		CodeAction ActionFromExpressionStatement(RefactoringContext context)
		{
			var expressionStatement = context.GetNode<ExpressionStatement>();
			if (expressionStatement == null)
				return null;
			var expression = expressionStatement.Expression;
			if (expression is AssignmentExpression)
				expression = ((AssignmentExpression)expression).Left;
			var expressionRR = context.Resolve(expression);
			if (expressionRR == null)
				return null;
			var elementType = GetElementType(expressionRR, context);
			if (elementType == null)
				return null;
			return new CodeAction(context.TranslateString("Iterate via foreach"), script => {
				var iterator = MakeForeach(expression, elementType, context);
				if (expression == expressionStatement.Expression)
					script.Replace(expressionStatement, iterator);
				else
					script.InsertAfter(expressionStatement, iterator);
			});
		}

		static ForeachStatement MakeForeach(Expression node, IType type, RefactoringContext context)
		{
			var namingHelper = new NamingHelper(context);
			return new ForeachStatement() {
				VariableType = new SimpleType("var"),
				VariableName = namingHelper.GenerateVariableName(type),
				InExpression = node.Clone(),
				EmbeddedStatement = new BlockStatement()
			};
		}

		static IType GetElementType(ResolveResult rr, RefactoringContext context)
		{
			IType type = GetInterfaceType(rr.Type, "System.Collections.Generic.IEnumerable") ??
				GetInterfaceType(rr.Type, "System.Collections.IEnumerable");
			if (type == null)
				return null;

			var parameterizedType = type as ParameterizedType;
			if (parameterizedType != null)
				return parameterizedType.TypeArguments.First();
			else
				return context.Compilation.FindType(KnownTypeCode.Object);
		}

		static IType GetInterfaceType(IType type, string typeName)
		{
			string fullName = null;
			if (type.TypeParameterCount > 0)
				fullName = type.FullName.Split('`').First();
			else
				fullName = type.FullName;

			if (fullName == typeName)
				return type;
			foreach (var baseType in type.DirectBaseTypes) {
				IType retType = GetInterfaceType(baseType, typeName);
				if (retType != null)
					return retType;
			}
			return null;
		}

		#endregion
	}
}
