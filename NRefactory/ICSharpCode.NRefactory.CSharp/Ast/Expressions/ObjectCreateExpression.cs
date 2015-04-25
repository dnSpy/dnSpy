// 
// ObjectCreateExpression.cs
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
	/// new Type(Arguments) { Initializer }
	/// </summary>
	public class ObjectCreateExpression : Expression
	{
		public readonly static TokenRole NewKeywordRole = new TokenRole ("new");
		public readonly static Role<ArrayInitializerExpression> InitializerRole = ArrayCreateExpression.InitializerRole;
		
		public CSharpTokenNode NewToken {
			get { return GetChildByRole (NewKeywordRole); }
		}
		
		public AstType Type {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole (Roles.Type, value); }
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public AstNodeCollection<Expression> Arguments {
			get { return GetChildrenByRole (Roles.Argument); }
		}
		
		public CSharpTokenNode RParToken {
			get { return GetChildByRole (Roles.RPar); }
		}
		
		public ArrayInitializerExpression Initializer {
			get { return GetChildByRole (InitializerRole); }
			set { SetChildByRole (InitializerRole, value); }
		}
		
		public ObjectCreateExpression ()
		{
		}
		
		public ObjectCreateExpression (AstType type, IEnumerable<Expression> arguments = null)
		{
			AddChild (type, Roles.Type);
			if (arguments != null) {
				foreach (var arg in arguments) {
					AddChild (arg, Roles.Argument);
				}
			}
		}
		
		public ObjectCreateExpression (AstType type, params Expression[] arguments) : this (type, (IEnumerable<Expression>)arguments)
		{
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitObjectCreateExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitObjectCreateExpression (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitObjectCreateExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ObjectCreateExpression o = other as ObjectCreateExpression;
			return o != null && this.Type.DoMatch(o.Type, match) && this.Arguments.DoMatch(o.Arguments, match) && this.Initializer.DoMatch(o.Initializer, match);
		}
	}
}
