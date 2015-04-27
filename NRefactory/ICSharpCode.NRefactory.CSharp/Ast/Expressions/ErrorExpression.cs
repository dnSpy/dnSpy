// 
// ErrorExpression.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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
	[Obsolete("This class is obsolete. Remove all referencing code.")]
	public class EmptyExpression : AstNode
	{
		#region implemented abstract members of AstNode

		public override void AcceptVisitor(IAstVisitor visitor)
		{
			throw new NotImplementedException();
		}

		public override T AcceptVisitor<T>(IAstVisitor<T> visitor)
		{
			throw new NotImplementedException();
		}

		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			throw new NotImplementedException();
		}

		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}

		public override NodeType NodeType {
			get {
				throw new NotImplementedException();
			}
		}

		#endregion


	}

	public class ErrorExpression : Expression
	{
		TextLocation location;

		public override TextLocation StartLocation {
			get {
				return location;
			}
		}
		
		public override TextLocation EndLocation {
			get {
				return location;
			}
		}

		public string Error {
			get;
			private set;
		}

		public ErrorExpression ()
		{
		}

		public ErrorExpression (TextLocation location)
		{
			this.location = location;
		}

		public ErrorExpression (string error)
		{
			this.Error = error;
		}

		public ErrorExpression (string error, TextLocation location)
		{
			this.location = location;
			this.Error = error;
		}

		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitErrorNode(this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitErrorNode(this);
		}

		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitErrorNode(this, data);
		}

		protected internal override bool DoMatch (AstNode other, PatternMatching.Match match)
		{
			var o = other as ErrorExpression;
			return o != null;
		}
	}
}

