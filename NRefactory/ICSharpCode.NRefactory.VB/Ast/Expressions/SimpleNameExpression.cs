// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Description of IdentifierExpression.
	/// </summary>
	public class SimpleNameExpression : Expression
	{
		public Identifier Identifier { get; set; }
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var node = other as SimpleNameExpression;
			return node != null
				&& Identifier.DoMatch(node.Identifier, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSimpleNameExpression(this, data);
		}
	}
}
