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

using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Represents a literal value.
	/// </summary>
	public class PrimitiveExpression : Expression
	{
		public static readonly object AnyValue = new object();
		
		TextLocation startLocation;
		public override TextLocation StartLocation {
			get {
				return startLocation;
			}
		}
		
		string literalValue;
		public override TextLocation EndLocation {
			get {
				return new TextLocation (StartLocation.Line, StartLocation.Column + literalValue.Length);
			}
		}
		
		object value;
		
		public object Value {
			get { return this.value; }
			set { 
				ThrowIfFrozen(); 
				this.value = value;
			}
		}
		
		public string LiteralValue {
			get { return literalValue; }
			set {
				if (value == null)
					throw new ArgumentNullException();
				ThrowIfFrozen();
				literalValue = value;
			}
		}
		
		public PrimitiveExpression (object value)
		{
			this.Value = value;
			this.literalValue = "";
		}
		
		public PrimitiveExpression (object value, string literalValue)
		{
			this.Value = value;
			this.literalValue = literalValue ?? "";
		}
		
		public PrimitiveExpression (object value, TextLocation startLocation, string literalValue)
		{
			this.Value = value;
			this.startLocation = startLocation;
			this.literalValue = literalValue ?? "";
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitPrimitiveExpression (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitPrimitiveExpression (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitPrimitiveExpression (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			PrimitiveExpression o = other as PrimitiveExpression;
			return o != null && (this.Value == AnyValue || object.Equals(this.Value, o.Value));
		}
	}
}
