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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// Converts a foreach loop to for.
	/// </summary>
	public class ConvertForeachToFor : IContextAction
	{
		public bool IsValid (RefactoringContext context)
		{
			return GetForeachStatement (context) != null;
		}

		static string GetCountProperty (IType type)
		{
			if (type.Kind == TypeKind.Array)
				return "Length";
			return "Count";
		}

		public void Run (RefactoringContext context)
		{
			var foreachStatement = GetForeachStatement (context);
			
			var result = context.Resolve (foreachStatement.InExpression);
			var countProperty = GetCountProperty (result.Type);
			
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
			
			using (var script = context.StartScript ()) {
				script.Replace (foreachStatement, forStatement);
				script.Link (initializer.Variables.First ().NameToken, id1, id2, id3);
			}
		}
		
		static ForeachStatement GetForeachStatement (RefactoringContext context)
		{
			var astNode = context.GetNode ();
			if (astNode == null)
				return null;
			var result = (astNode as ForeachStatement) ?? astNode.Parent as ForeachStatement;
			if (result == null || context.Resolve (result.InExpression) == null)
				return null;
			return result;
		}
	}
}
