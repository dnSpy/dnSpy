// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class UsingStatement : Statement
	{
		public static readonly Role<AstNode> ResourceRole = new Role<AstNode>("Resource", AstNode.Null);
		
		/// <remarks>either multiple VariableInitializers or one Expression</remarks>
		public AstNodeCollection<AstNode> Resources {
			get { return GetChildrenByRole(ResourceRole); }
		}
		
		public BlockStatement Body {
			get { return GetChildByRole(Roles.Body); }
			set { SetChildByRole(Roles.Body, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitUsingStatement(this, data);
		}
	}
}
