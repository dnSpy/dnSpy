// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class TryStatement : Statement
	{
		public static readonly Role<BlockStatement> FinallyBlockRole = new Role<BlockStatement>("FinallyBlock", Ast.BlockStatement.Null);
		
		public BlockStatement Body {
			get { return GetChildByRole(Roles.Body); }
			set { SetChildByRole(Roles.Body, value); }
		}
		
		public AstNodeCollection<CatchBlock> CatchBlocks {
			get { return GetChildrenByRole(CatchBlock.CatchBlockRole); }
		}
		
		public BlockStatement FinallyBlock {
			get { return GetChildByRole(FinallyBlockRole); }
			set { SetChildByRole(FinallyBlockRole, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTryStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
	}
	
	public class CatchBlock : BlockStatement
	{
		public static readonly Role<CatchBlock> CatchBlockRole = new Role<CatchBlock>("CatchBlockRole");
		
		public Identifier ExceptionVariable {
			get { return GetChildByRole(Roles.Identifier); }
			set { SetChildByRole(Roles.Identifier, value); }
		}
		
		public AstType ExceptionType {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		public Expression WhenExpression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCatchBlock(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
	}	
}
