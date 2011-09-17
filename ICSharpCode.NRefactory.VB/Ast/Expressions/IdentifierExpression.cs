// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class IdentifierExpression : Expression
	{
		public IdentifierExpression()
		{
			
		}
		
		public IdentifierExpression(Identifier identifier)
		{
			this.Identifier = identifier;
		}
		
		public Identifier Identifier {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public AstNodeCollection<AstType> TypeArguments {
			get { return GetChildrenByRole(Roles.TypeArgument); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitIdentifierExpression(this, data);
		}
	}
}
