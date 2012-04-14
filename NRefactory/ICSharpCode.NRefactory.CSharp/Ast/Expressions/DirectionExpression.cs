// 
// DirectionExpression.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
	public enum FieldDirection
	{
		None,
		Out,
		Ref
	}
	
	/// <summary>
	/// ref Expression
	/// </summary>
	public class DirectionExpression : Expression
	{
		public readonly static TokenRole RefKeywordRole = new TokenRole ("ref");
		public readonly static TokenRole OutKeywordRole = new TokenRole ("out");
		
		public FieldDirection FieldDirection {
			get;
			set;
		}
		
		public CSharpTokenNode FieldDirectionToken {
			get { return FieldDirection == ICSharpCode.NRefactory.CSharp.FieldDirection.Ref ? GetChildByRole (RefKeywordRole) : GetChildByRole (OutKeywordRole); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public DirectionExpression ()
		{
		}
		
		public DirectionExpression (FieldDirection direction, Expression expression)
		{
			this.FieldDirection = direction;
			AddChild (expression, Roles.Expression);
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitDirectionExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitDirectionExpression (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitDirectionExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			DirectionExpression o = other as DirectionExpression;
			return o != null && this.FieldDirection == o.FieldDirection && this.Expression.DoMatch(o.Expression, match);
		}
	}
}
