// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.VB.Ast;

namespace ICSharpCode.NRefactory.VB
{
	public class Comment : AstNode
	{
		public bool IsDocumentationComment { get; set; }
		
		public bool StartsLine {
			get;
			set;
		}
		
		public string Content {
			get;
			set;
		}
		
		TextLocation startLocation;
		public override TextLocation StartLocation {
			get { 
				return startLocation;
			}
		}
		
		TextLocation endLocation;
		public override TextLocation EndLocation {
			get {
				return endLocation;
			}
		}
		
		public Comment (string content, bool isDocumentation = false)
		{
			this.IsDocumentationComment = isDocumentation;
			this.Content = content;
		}
		
		public Comment (bool isDocumentation, TextLocation startLocation, TextLocation endLocation)
		{
			this.IsDocumentationComment = isDocumentation;
			this.startLocation = startLocation;
			this.endLocation = endLocation;
		}
	
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitComment(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			Comment o = other as Comment;
			return o != null && this.IsDocumentationComment == o.IsDocumentationComment && MatchString(this.Content, o.Content);
		}
	}
}
