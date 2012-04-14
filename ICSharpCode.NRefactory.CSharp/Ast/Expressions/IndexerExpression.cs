// 
// IndexerExpression.cs
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
	/// Target[Arguments]
	/// </summary>
	public class IndexerExpression : Expression
	{
		public Expression Target {
			get { return GetChildByRole (Roles.TargetExpression); }
			set { SetChildByRole(Roles.TargetExpression, value); }
		}
		
		public CSharpTokenNode LBracketToken {
			get { return GetChildByRole (Roles.LBracket); }
		}
		
		public AstNodeCollection<Expression> Arguments {
			get { return GetChildrenByRole<Expression>(Roles.Argument); }
		}
		
		public CSharpTokenNode RBracketToken {
			get { return GetChildByRole (Roles.RBracket); }
		}
		
		public IndexerExpression ()
		{
		}
		
		public IndexerExpression (Expression target, IEnumerable<Expression> arguments)
		{
			AddChild (target, Roles.TargetExpression);
			if (arguments != null) {
				foreach (var arg in arguments) {
					AddChild (arg, Roles.Argument);
				}
			}
		}
		
		public IndexerExpression (Expression target, params Expression[] arguments) : this (target, (IEnumerable<Expression>)arguments)
		{
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitIndexerExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitIndexerExpression (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitIndexerExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			IndexerExpression o = other as IndexerExpression;
			return o != null && this.Target.DoMatch(o.Target, match) && this.Arguments.DoMatch(o.Arguments, match);
		}
	}
}
