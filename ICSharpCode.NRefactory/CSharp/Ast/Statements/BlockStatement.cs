// 
// BlockStatement.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// { Statements }
	/// </summary>
	public class BlockStatement : Statement
	{
		public static readonly Role<Statement> StatementRole = new Role<Statement>("Statement", Statement.Null);
		
		#region Null
		public static readonly new BlockStatement Null = new NullBlockStatement ();
		sealed class NullBlockStatement : BlockStatement
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
		}
		#endregion
		
		public CSharpTokenNode LBraceToken {
			get { return GetChildByRole (Roles.LBrace); }
		}
		
		public IEnumerable<Statement> Statements {
			get { return GetChildrenByRole (StatementRole); }
			set { SetChildrenByRole (StatementRole, value); }
		}
		
		public CSharpTokenNode RBraceToken {
			get { return GetChildByRole (Roles.RBrace); }
		}
		
		public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitBlockStatement (this, data);
		}
		
		#region Builder methods
		public void AddStatement(Statement statement)
		{
			AddChild(statement, StatementRole);
		}
		
		public void AddStatement(Expression expression)
		{
			AddChild(new ExpressionStatement { Expression = expression }, StatementRole);
		}
		
		public void AddAssignment(Expression left, Expression right)
		{
			AddStatement(new AssignmentExpression { Left = left, Operator = AssignmentOperatorType.Assign, Right = right });
		}
		
		public void AddReturnStatement(Expression expression)
		{
			AddStatement(new ReturnStatement { Expression = expression });
		}
		#endregion
	}
}
