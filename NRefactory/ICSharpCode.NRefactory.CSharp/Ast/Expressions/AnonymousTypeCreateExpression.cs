// 
// AnonymousTypeCreateExpression.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// new { [ExpressionList] }
	/// </summary>
	public class AnonymousTypeCreateExpression : Expression
	{
		public readonly static TokenRole NewKeywordRole = new TokenRole ("new");
		
		public CSharpTokenNode NewToken {
			get { return GetChildByRole (NewKeywordRole); }
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public AstNodeCollection<Expression> Initializers {
			get { return GetChildrenByRole (Roles.Expression); }
		}
		
		public CSharpTokenNode RParToken {
			get { return GetChildByRole (Roles.RPar); }
		}
		
		public AnonymousTypeCreateExpression ()
		{
		}
		
		public AnonymousTypeCreateExpression (IEnumerable<Expression> initializers)
		{
			foreach (var ini in initializers) {
				AddChild (ini, Roles.Expression);
			}
		}
		
		public AnonymousTypeCreateExpression (params Expression[] initializer) : this ((IEnumerable<Expression>)initializer)
		{
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitAnonymousTypeCreateExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitAnonymousTypeCreateExpression (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitAnonymousTypeCreateExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			var o = other as AnonymousTypeCreateExpression;
			return o != null && this.Initializers.DoMatch(o.Initializers, match);
		}
	}
}

