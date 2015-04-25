// 
// TextNode.cs
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
	/// A text node contains text without syntactic or semantic information.
	/// (non parseable part of a text)
	/// </summary>
	public class TextNode : AstNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Whitespace;
			}
		}

		public string Text {
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

		public TextNode(string text) : this (text, TextLocation.Empty, TextLocation.Empty)
		{
		}

		public TextNode(string text, TextLocation startLocation, TextLocation endLocation)
		{
			this.Text = text;
			this.startLocation = startLocation;
			this.endLocation = endLocation;
		}

		public override void AcceptVisitor(IAstVisitor visitor)
		{
			visitor.VisitText (this);
		}

		public override T AcceptVisitor<T>(IAstVisitor<T> visitor)
		{
			return visitor.VisitText (this);
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitText (this, data);
		}

		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			var o = other as TextNode;
			return o != null && o.Text == Text;
		}
	}
}

