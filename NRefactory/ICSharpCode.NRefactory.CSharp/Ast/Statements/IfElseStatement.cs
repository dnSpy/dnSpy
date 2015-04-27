// 
// IfElseStatement.cs
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

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// if (Condition) TrueStatement else FalseStatement
	/// </summary>
	public class IfElseStatement : Statement
	{
		public readonly static TokenRole IfKeywordRole = new TokenRole ("if");
		public readonly static Role<Expression> ConditionRole = Roles.Condition;
		public readonly static Role<Statement> TrueRole = new Role<Statement>("True", Statement.Null);
		public readonly static TokenRole ElseKeywordRole = new TokenRole ("else");
		public readonly static Role<Statement> FalseRole = new Role<Statement>("False", Statement.Null);
		
		public CSharpTokenNode IfToken {
			get { return GetChildByRole (IfKeywordRole); }
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public Expression Condition {
			get { return GetChildByRole (ConditionRole); }
			set { SetChildByRole (ConditionRole, value); }
		}
		
		public CSharpTokenNode RParToken {
			get { return GetChildByRole (Roles.RPar); }
		}
		
		public Statement TrueStatement {
			get { return GetChildByRole (TrueRole); }
			set { SetChildByRole (TrueRole, value); }
		}
		
		public CSharpTokenNode ElseToken {
			get { return GetChildByRole (ElseKeywordRole); }
		}
		
		public Statement FalseStatement {
			get { return GetChildByRole (FalseRole); }
			set { SetChildByRole (FalseRole, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitIfElseStatement (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitIfElseStatement (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitIfElseStatement (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			IfElseStatement o = other as IfElseStatement;
			return o != null && this.Condition.DoMatch(o.Condition, match) && this.TrueStatement.DoMatch(o.TrueStatement, match) && this.FalseStatement.DoMatch(o.FalseStatement, match);
		}
		
		public IfElseStatement()
		{
		}
		
		public IfElseStatement(Expression condition, Statement trueStatement, Statement falseStatement = null)
		{
			this.Condition = condition;
			this.TrueStatement = trueStatement;
			this.FalseStatement = falseStatement;
		}
	}
}
