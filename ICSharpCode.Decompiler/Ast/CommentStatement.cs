// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.PatternMatching;

namespace ICSharpCode.Decompiler.Ast
{
	/// <summary>
	/// Allows storing comments inside IEnumerable{Statement}. Used in the AstMethodBuilder.
	/// CommentStatement nodes are replaced with regular comments later on.
	/// </summary>
	class CommentStatement : Statement
	{
		string comment;
		
		public CommentStatement(string comment)
		{
			if (comment == null)
				throw new ArgumentNullException("comment");
			this.comment = comment;
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return default(S);
		}
		
		public static void ReplaceAll(AstNode tree)
		{
			foreach (var cs in tree.Descendants.OfType<CommentStatement>()) {
				cs.Parent.InsertChildBefore(cs, new Comment(cs.comment), Roles.Comment);
				cs.Remove();
			}
		}
		
		protected override bool DoMatch(AstNode other, Match match)
		{
			CommentStatement o = other as CommentStatement;
			return o != null && MatchString(comment, o.comment);
		}
	}
}
