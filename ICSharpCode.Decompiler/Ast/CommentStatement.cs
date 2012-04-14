// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;

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
		
		public override void AcceptVisitor(IAstVisitor visitor)
		{
		}
		
		public override T AcceptVisitor<T>(IAstVisitor<T> visitor)
		{
			return default(T);
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
