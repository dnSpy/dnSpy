// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Represents a named argument passed to a method or attribute.
	/// </summary>
	public class NamedArgumentExpression : Expression
	{
		public string Identifier {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole(Roles.Identifier, CSharp.Identifier.Create (value, AstLocation.Empty));
			}
		}
		
		public Identifier IdentifierToken {
			get {
				return GetChildByRole (Roles.Identifier);
			}
			set {
				SetChildByRole(Roles.Identifier, value);
			}
		}
		
		public CSharpTokenNode AssignToken {
			get { return GetChildByRole (Roles.Assign); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitNamedArgumentExpression(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			NamedArgumentExpression o = other as NamedArgumentExpression;
			return o != null && MatchString(this.Identifier, o.Identifier) && this.Expression.DoMatch(o.Expression, match);
		}
	}
}
