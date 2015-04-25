// 
// Comment.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace ICSharpCode.NRefactory.CSharp
{
	public enum CommentType 
	{
		/// <summary>
		/// "//" comment
		/// </summary>
		SingleLine,
		/// <summary>
		/// "/* */" comment
		/// </summary>
		MultiLine,
		/// <summary>
		/// "///" comment
		/// </summary>
		Documentation,
		/// <summary>
		/// Inactive code (code in non-taken "#if")
		/// </summary>
		InactiveCode,
		/// <summary>
		/// "/** */" comment
		/// </summary>
		MultiLineDocumentation
	}
	
	public class Comment : AstNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Whitespace;
			}
		}
		
		CommentType commentType;
		
		public CommentType CommentType {
			get { return commentType; }
			set { ThrowIfFrozen(); commentType = value; }
		}
		
		/// <summary>
		/// Returns true if the <see cref="CommentType"/> is Documentation or MultiLineDocumentation.
		/// </summary>
		public bool IsDocumentation {
			get {
				return commentType == CommentType.Documentation || commentType == CommentType.MultiLineDocumentation;
			}
		}
		
		bool startsLine;
		
		public bool StartsLine {
			get { return startsLine; }
			set { ThrowIfFrozen(); startsLine = value; }
		}
		
		string content;
		
		public string Content {
			get { return content; }
			set { ThrowIfFrozen(); content = value; }
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
		
		public Comment (string content, CommentType type = CommentType.SingleLine)
		{
			this.CommentType = type;
			this.Content = content;
		}
		
		public Comment (CommentType commentType, TextLocation startLocation, TextLocation endLocation)
		{
			this.CommentType = commentType;
			this.startLocation = startLocation;
			this.endLocation = endLocation;
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitComment (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitComment (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitComment (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			Comment o = other as Comment;
			return o != null && this.CommentType == o.CommentType && MatchString(this.Content, o.Content);
		}
	}
}

