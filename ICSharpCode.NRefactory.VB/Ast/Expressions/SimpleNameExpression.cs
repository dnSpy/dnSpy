// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Description of SimpleNameExpression.
	/// </summary>
	public class SimpleNameExpression : Expression
	{
		public Identifier Identifier { get; set; }
		
		public AstNodeCollection<AstType> TypeArguments {
			get { return GetChildrenByRole(Roles.TypeArgument); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var node = other as SimpleNameExpression;
			return node != null
				&& Identifier.DoMatch(node.Identifier, match)
				&& TypeArguments.DoMatch(node.TypeArguments, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSimpleNameExpression(this, data);
		}
	}
}
