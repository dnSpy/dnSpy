// 
// ErrorNode.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Xamarin (http://www.xamarin.com);
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
	/// Represents a parsing error in the ast. At the moment it only represents missing closing bracket.
	/// This closing bracket is replaced by a node at the highest possible position.
	/// (To make GetAstNodeAt (line, col) working).
	/// </summary>
	public class ErrorNode : AstNode
	{
		static TextLocation maxLoc = new TextLocation (int.MaxValue, int.MaxValue);
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
				
		public override TextLocation StartLocation {
			get {
				return maxLoc;
			}
		}
		
		public override TextLocation EndLocation {
			get {
				return maxLoc;
			}
		}
		
		public ErrorNode ()
		{
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return default (T);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			// nothing
			return default (S);
		}
			
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			var o = other as ErrorNode;
			return o != null;
		}
		
		public override string ToString ()
		{
			return "[ErrorNode]";
		}
	}
}

