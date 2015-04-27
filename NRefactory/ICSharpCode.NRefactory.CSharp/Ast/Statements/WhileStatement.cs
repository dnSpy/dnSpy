// 
// WhileStatement.cs
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
	/// "while (Condition) EmbeddedStatement"
	/// </summary>
	public class WhileStatement : Statement
	{
		public static readonly TokenRole WhileKeywordRole = new TokenRole ("while");
		
		public CSharpTokenNode WhileToken {
			get { return GetChildByRole (WhileKeywordRole); }
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public Expression Condition {
			get { return GetChildByRole (Roles.Condition); }
			set { SetChildByRole (Roles.Condition, value); }
		}
		
		public CSharpTokenNode RParToken {
			get { return GetChildByRole (Roles.RPar); }
		}
		
		public Statement EmbeddedStatement {
			get { return GetChildByRole (Roles.EmbeddedStatement); }
			set { SetChildByRole (Roles.EmbeddedStatement, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitWhileStatement (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitWhileStatement (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitWhileStatement (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			WhileStatement o = other as WhileStatement;
			return o != null && this.Condition.DoMatch(o.Condition, match) && this.EmbeddedStatement.DoMatch(o.EmbeddedStatement, match);
		}

		public WhileStatement()
		{
		}

		public WhileStatement(Expression condition, Statement embeddedStatement)
		{
			this.Condition = condition;
			this.EmbeddedStatement = embeddedStatement;
		}
	}
}
