// 
// PrimitiveExpression.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
	/// <summary>
	/// Represents a literal value.
	/// </summary>
	public class PrimitiveExpression : Expression
	{
		AstLocation startLocation;
		public override AstLocation StartLocation {
			get {
				return startLocation;
			}
		}
		
		int length;
		public override AstLocation EndLocation {
			get {
				return new AstLocation (StartLocation.Line, StartLocation.Column + length);
			}
		}
		
		public object Value {
			get;
			private set;
		}
		
		public PrimitiveExpression (object value)
		{
			this.Value = value;
		}
		
		public PrimitiveExpression (object value, AstLocation startLocation, int length)
		{
			this.Value = value;
			this.startLocation = startLocation;
			this.length = length;
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitPrimitiveExpression (this, data);
		}
	}
}
