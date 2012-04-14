// 
// ConvertForeachToFor.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Converts a foreach loop to for.
	/// </summary>
	[ContextAction("Convert 'foreach' loop to 'for'", Description = "Works on 'foreach' loops that allow direct access to its elements.")]
	public class ConvertForeachToForAction : ICodeActionProvider
	{
		public IEnumerable<CodeAction> GetActions(RefactoringContext context)
		{
			var foreachStatement = GetForeachStatement(context);
			if (foreachStatement == null) {
				yield break;
			}
			yield return new CodeAction(context.TranslateString("Convert 'foreach' loop to 'for'"), script => {
				var result = context.Resolve (foreachStatement.InExpression);
				var countProperty = GetCountProperty (result.Type);
				
				// TODO: use another variable name if 'i' is already in use
				var initializer = new VariableDeclarationStatement (new PrimitiveType ("int"), "i", new PrimitiveExpression (0));
				var id1 = new IdentifierExpression ("i");
				var id2 = id1.Clone ();
				var id3 = id1.Clone ();
				
				var forStatement = new ForStatement () {
					Initializers = { initializer },
					Condition = new BinaryOperatorExpression (id1, BinaryOperatorType.LessThan, new MemberReferenceExpression (foreachStatement.InExpression.Clone (), countProperty)),
					Iterators = { new ExpressionStatement (new UnaryOperatorExpression (UnaryOperatorType.PostIncrement, id2)) },
					EmbeddedStatement = new BlockStatement {
						new VariableDeclarationStatement (foreachStatement.VariableType.Clone (), foreachStatement.VariableName, new IndexerExpression (foreachStatement.InExpression.Clone (), id3))
					}
				};
				
				if (foreachStatement.EmbeddedStatement is BlockStatement) {
					foreach (var child in ((BlockStatement)foreachStatement.EmbeddedStatement).Statements) {
						forStatement.EmbeddedStatement.AddChild (child.Clone (), BlockStatement.StatementRole);
					}
				} else {
					forStatement.EmbeddedStatement.AddChild (foreachStatement.EmbeddedStatement.Clone (), BlockStatement.StatementRole);
				}
				
				script.Replace (foreachStatement, forStatement);
				script.Link (initializer.Variables.First ().NameToken, id1, id2, id3);
			});
		}
		
		
		static string GetCountProperty(IType type)
		{
			if (type.Kind == TypeKind.Array) {
				return "Length";
			}
			return "Count";
		}

		static ForeachStatement GetForeachStatement (RefactoringContext context)
		{
			var astNode = context.GetNode ();
			if (astNode == null)
				return null;
			var result = (astNode as ForeachStatement) ?? astNode.Parent as ForeachStatement;
			if (result == null)
				return null;
			var collection = context.Resolve (result.InExpression);
			if (collection.Type.Kind != TypeKind.Array && !collection.Type.GetProperties(p => p.IsIndexer).Any())
				return null;
			return result;
		}
	}
}
