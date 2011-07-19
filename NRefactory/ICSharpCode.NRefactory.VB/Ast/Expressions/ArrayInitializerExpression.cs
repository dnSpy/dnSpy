// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// { Elements }
	/// </summary>
	public class ArrayInitializerExpression : Expression
	{
		#region Null
		public new static readonly ArrayInitializerExpression Null = new NullArrayInitializerExpression ();
		
		sealed class NullArrayInitializerExpression : ArrayInitializerExpression
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion
		
		public readonly static Role<ArrayInitializerExpression> InitializerRole = new Role<ArrayInitializerExpression>("Initializer", ArrayInitializerExpression.Null);
		
		public AstNodeCollection<Expression> Elements {
			get { return GetChildrenByRole(Roles.Expression); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitArrayInitializerExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ArrayInitializerExpression o = other as ArrayInitializerExpression;
			return o != null && this.Elements.DoMatch(o.Elements, match);
		}
	}
}
