// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Represents a named argument passed to a method or attribute.
	/// name: expression
	/// </summary>
	public class NamedArgumentExpression : Expression
	{
		public NamedArgumentExpression()
		{
		}
		
		public NamedArgumentExpression(string name, Expression expression)
		{
			this.Name = name;
			this.Expression = expression;
		}
		
		public string Name {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole(Roles.Identifier, CSharp.Identifier.Create (value));
			}
		}
		
		public Identifier NameToken {
			get {
				return GetChildByRole (Roles.Identifier);
			}
			set {
				SetChildByRole(Roles.Identifier, value);
			}
		}
		
		public CSharpTokenNode ColonToken {
			get { return GetChildByRole (Roles.Colon); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitNamedArgumentExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitNamedArgumentExpression (this);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitNamedArgumentExpression(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			NamedArgumentExpression o = other as NamedArgumentExpression;
			return o != null && MatchString(this.Name, o.Name) && this.Expression.DoMatch(o.Expression, match);
		}
	}
}
