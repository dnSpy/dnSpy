// 
// ForStatement.cs
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
	/// for (Initializers; Condition; Iterators) EmbeddedStatement
	/// </summary>
	public class ForStatement : Statement
	{
		public static readonly TokenRole ForKeywordRole = new TokenRole ("for");
		public readonly static Role<Statement> InitializerRole = new Role<Statement>("Initializer", Statement.Null);
		public readonly static Role<Statement> IteratorRole = new Role<Statement>("Iterator", Statement.Null);
		
		public CSharpTokenNode ForToken {
			get { return GetChildByRole (ForKeywordRole); }
		}
		
		public CSharpTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		/// <summary>
		/// Gets the list of initializer statements.
		/// Note: this contains multiple statements for "for (a = 2, b = 1; a > b; a--)", but contains
		/// only a single statement for "for (int a = 2, b = 1; a > b; a--)" (a single VariableDeclarationStatement with two variables)
		/// </summary>
		public AstNodeCollection<Statement> Initializers {
			get { return GetChildrenByRole (InitializerRole); }
		}
		
		public Expression Condition {
			get { return GetChildByRole (Roles.Condition); }
			set { SetChildByRole (Roles.Condition, value); }
		}
		
		public AstNodeCollection<Statement> Iterators {
			get { return GetChildrenByRole (IteratorRole); }
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
			visitor.VisitForStatement (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitForStatement (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitForStatement (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ForStatement o = other as ForStatement;
			return o != null && this.Initializers.DoMatch(o.Initializers, match) && this.Condition.DoMatch(o.Condition, match)
				&& this.Iterators.DoMatch(o.Iterators, match) && this.EmbeddedStatement.DoMatch(o.EmbeddedStatement, match);
		}
	}
}
