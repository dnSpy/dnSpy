// 
// WhitespaceNode.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

using System;
namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// A Whitespace node contains only whitespaces.
	/// </summary>
	public class WhitespaceNode : AstNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Whitespace;
			}
		}

		public string WhiteSpaceText {
			get;
			set;
		}

		TextLocation startLocation;
		public override TextLocation StartLocation {
			get { 
				return startLocation;
			}
		}
		
		public override TextLocation EndLocation {
			get {
				return new TextLocation (startLocation.Line, startLocation.Column + WhiteSpaceText.Length);
			}
		}

		public WhitespaceNode(string whiteSpaceText) : this (whiteSpaceText, TextLocation.Empty)
		{
		}

		public WhitespaceNode(string whiteSpaceText, TextLocation startLocation)
		{
			this.WhiteSpaceText = WhiteSpaceText;
			this.startLocation = startLocation;
		}

		public override void AcceptVisitor(IAstVisitor visitor)
		{
			visitor.VisitWhitespace (this);
		}

		public override T AcceptVisitor<T>(IAstVisitor<T> visitor)
		{
			return visitor.VisitWhitespace (this);
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitWhitespace (this, data);
		}

		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			var o = other as WhitespaceNode;
			return o != null && o.WhiteSpaceText == WhiteSpaceText;
		}
	}
}

