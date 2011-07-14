// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// [ Key ] .Identifier = Expression
	/// </summary>
	public class FieldInitializerExpression : Expression
	{
		public bool IsKey { get; set; }
		
		public VBTokenNode KeyToken {
			get { return GetChildByRole (Roles.Keyword); }
		}
		
		public VBTokenNode DotToken {
			get { return GetChildByRole (Roles.Dot); }
		}
		
		public Identifier Identifier {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public VBTokenNode AssignToken {
			get { return GetChildByRole (Roles.Assign); }
		}
		
		public Expression Expression {
			get { return GetChildByRole (Roles.Expression); }
			set { SetChildByRole (Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitFieldInitializerExpression(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			FieldInitializerExpression o = other as FieldInitializerExpression;
			return o != null && this.IsKey == o.IsKey && this.Identifier.DoMatch(o.Identifier, match) && this.Expression.DoMatch(o.Expression, match);
		}
	}
}
