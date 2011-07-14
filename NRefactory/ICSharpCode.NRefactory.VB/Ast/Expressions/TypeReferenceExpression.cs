// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Represents an AstType as an expression.
	/// This is used when calling a method on a primitive type: "Integer.Parse()"
	/// </summary>
	public class TypeReferenceExpression : Expression
	{
		public AstType Type {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTypeReferenceExpression(this, data);
		}
		
		public TypeReferenceExpression ()
		{
		}
		
		public TypeReferenceExpression (AstType type)
		{
			SetChildByRole(Roles.Type, type);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			TypeReferenceExpression o = other as TypeReferenceExpression;
			return o != null && this.Type.DoMatch(o.Type, match);
		}
	}
}
