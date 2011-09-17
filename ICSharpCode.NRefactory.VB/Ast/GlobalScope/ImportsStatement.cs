// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class ImportsStatement : AstNode
	{
		public static readonly Role<ImportsClause> ImportsClauseRole = new Role<ImportsClause>("ImportsClause", ImportsClause.Null);
		
		public VBTokenNode Imports {
			get { return GetChildByRole(Roles.Keyword); }
		}
		
		public AstNodeCollection<ImportsClause> ImportsClauses {
			get { return GetChildrenByRole(ImportsClauseRole); }
		}
		
//		public override string ToString() {
//			return string.Format("[ImportsStatement ImportsClauses={0}]", GetCollectionString(ImportsClauses));
//		}
		
		protected internal override bool DoMatch(AstNode other, Match match)
		{
			ImportsStatement stmt = other as ImportsStatement;
			return stmt != null && stmt.ImportsClauses.DoMatch(ImportsClauses, match);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitImportsStatement(this, data);
		}
	}
}
