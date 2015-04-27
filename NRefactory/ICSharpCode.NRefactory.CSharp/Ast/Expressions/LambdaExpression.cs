// 
// LambdaExpression.cs
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
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// [async] Parameters => Body
	/// </summary>
	public class LambdaExpression : Expression
	{
		public readonly static TokenRole AsyncModifierRole = new TokenRole ("async");
		public readonly static TokenRole ArrowRole = new TokenRole ("=>");
		public static readonly Role<AstNode> BodyRole = new Role<AstNode>("Body", AstNode.Null);
		
		bool isAsync;
		
		public bool IsAsync {
			get { return isAsync; }
			set { ThrowIfFrozen(); isAsync = value; }
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}

		public AstNodeCollection<ParameterDeclaration> Parameters {
			get { return GetChildrenByRole (Roles.Parameter); }
		}
		
		public CSharpTokenNode RParToken {
			get { return GetChildByRole (Roles.RPar); }
		}

		public CSharpTokenNode ArrowToken {
			get { return GetChildByRole (ArrowRole); }
		}
		
		public AstNode Body {
			get { return GetChildByRole (BodyRole); }
			set { SetChildByRole (BodyRole, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitLambdaExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitLambdaExpression (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitLambdaExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			LambdaExpression o = other as LambdaExpression;
			return o != null && this.IsAsync == o.IsAsync && this.Parameters.DoMatch(o.Parameters, match) && this.Body.DoMatch(o.Body, match);
		}
	}
}
