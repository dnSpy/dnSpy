// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.IO;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// SyncLock Expression <br />
	/// 	Block <br />
	/// End SyncLock
	/// </summary>
	public class SyncLockStatement : Statement
	{
		public Expression Expression {
			get { return GetChildByRole(Roles.Expression); }
			set { SetChildByRole(Roles.Expression, value); }
		}
		
		public BlockStatement Body {
			get { return GetChildByRole(Roles.Body); }
			set { SetChildByRole(Roles.Body, value); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSyncLockStatement(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
	}
}
