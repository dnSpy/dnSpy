// 
// ConditionalExpression.cs
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
	/// Condition ? TrueExpression : FalseExpression
	/// </summary>
	public class ConditionalExpression : Expression
	{
		public readonly static Role<Expression> ConditionRole = Roles.Condition;
		public readonly static TokenRole QuestionMarkRole = new TokenRole("?");
		public readonly static Role<Expression> TrueRole = new Role<Expression>("True", Expression.Null);
		public readonly static TokenRole ColonRole = Roles.Colon;
		public readonly static Role<Expression> FalseRole = new Role<Expression>("False", Expression.Null);
		
		public Expression Condition {
			get { return GetChildByRole(ConditionRole); }
			set { SetChildByRole(ConditionRole, value); }
		}
		
		public CSharpTokenNode QuestionMarkToken {
			get { return GetChildByRole (QuestionMarkRole); }
		}
		
		public Expression TrueExpression {
			get { return GetChildByRole(TrueRole); }
			set { SetChildByRole(TrueRole, value); }
		}
		
		public CSharpTokenNode ColonToken {
			get { return GetChildByRole (ColonRole); }
		}
		
		public Expression FalseExpression {
			get { return GetChildByRole(FalseRole); }
			set { SetChildByRole(FalseRole, value); }
		}
		
		public ConditionalExpression ()
		{
		}
		
		public ConditionalExpression (Expression condition, Expression trueExpression, Expression falseExpression)
		{
			AddChild (condition, ConditionRole);
			AddChild (trueExpression, TrueRole);
			AddChild (falseExpression, FalseRole);
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitConditionalExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitConditionalExpression (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitConditionalExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ConditionalExpression o = other as ConditionalExpression;
			return o != null && this.Condition.DoMatch(o.Condition, match) && this.TrueExpression.DoMatch(o.TrueExpression, match) && this.FalseExpression.DoMatch(o.FalseExpression, match);
		}

		#region Builder methods
		public override MemberReferenceExpression Member(string memberName)
		{
			return new MemberReferenceExpression { Target = this, MemberName = memberName };
		}

		public override IndexerExpression Indexer(IEnumerable<Expression> arguments)
		{
			IndexerExpression expr = new IndexerExpression();
			expr.Target = new ParenthesizedExpression(this);
			expr.Arguments.AddRange(arguments);
			return expr;
		}

		public override IndexerExpression Indexer(params Expression[] arguments)
		{
			IndexerExpression expr = new IndexerExpression();
			expr.Target = new ParenthesizedExpression(this);
			expr.Arguments.AddRange(arguments);
			return expr;
		}

		public override InvocationExpression Invoke(string methodName, IEnumerable<AstType> typeArguments, IEnumerable<Expression> arguments)
		{
			InvocationExpression ie = new InvocationExpression();
			MemberReferenceExpression mre = new MemberReferenceExpression();
			mre.Target = new ParenthesizedExpression(this);
			mre.MemberName = methodName;
			mre.TypeArguments.AddRange(typeArguments);
			ie.Target = mre;
			ie.Arguments.AddRange(arguments);
			return ie;
		}

		public override InvocationExpression Invoke(IEnumerable<Expression> arguments)
		{
			InvocationExpression ie = new InvocationExpression();
			ie.Target = new ParenthesizedExpression(this);
			ie.Arguments.AddRange(arguments);
			return ie;
		}

		public override InvocationExpression Invoke(params Expression[] arguments)
		{
			InvocationExpression ie = new InvocationExpression();
			ie.Target = new ParenthesizedExpression(this);
			ie.Arguments.AddRange(arguments);
			return ie;
		}

		public override CastExpression CastTo(AstType type)
		{
			return new CastExpression { Type = type,  Expression = new ParenthesizedExpression(this) };
		}

		public override AsExpression CastAs(AstType type)
		{
			return new AsExpression { Type = type,  Expression = new ParenthesizedExpression(this) };
		}

		public override IsExpression IsType(AstType type)
		{
			return new IsExpression { Type = type,  Expression = new ParenthesizedExpression(this) };
		}
		#endregion
	}
}
