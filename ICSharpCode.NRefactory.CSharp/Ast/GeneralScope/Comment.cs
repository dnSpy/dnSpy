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
	public enum CommentType {
		SingleLine,
		MultiLine,
		Documentation
	}
	
	public class Comment : AstNode, IRelocatable
	{
		public override NodeType NodeType {
			get {
				return NodeType.Whitespace;
			}
		}
		
		public CommentType CommentType {
			get;
			set;
		}
		
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
		
		#region IRelocationable implementation
		void IRelocatable.SetStartLocation (TextLocation startLocation)
		{
			int lineDelta = startLocation.Line - this.startLocation.Line;
			endLocation = new TextLocation (endLocation.Line + lineDelta, lineDelta != 0 ? endLocation.Column : endLocation.Column + startLocation.Column - this.startLocation.Column);
			this.startLocation = startLocation;
		}
		#endregion

		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data = default(T))
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

