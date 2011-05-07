// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using ICSharpCode.NRefactory.VB.PrettyPrinter;
using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Represents a literal value.
	/// </summary>
	public class PrimitiveExpression : Expression
	{
		public static readonly object AnyValue = new object();
		
		AstLocation startLocation;
		public override AstLocation StartLocation {
			get {
				return startLocation;
			}
		}
		
		int length;
		public override AstLocation EndLocation {
			get {
				return new AstLocation(StartLocation.Line, StartLocation.Column + length);
			}
		}
		
		public object Value { get; private set; }
		
		string stringValue;
		
		public string StringValue {
			get { return stringValue; } // TODO ?? VBNetOutputVisitor.ToVBNetString(this); }
		}
		
		public PrimitiveExpression(object value)
		{
			this.Value = value;
		}
		
		public PrimitiveExpression(object value, string stringValue)
		{
			this.Value = value;
			this.stringValue = stringValue;
		}
		
		public PrimitiveExpression(object value, AstLocation startLocation, int length)
		{
			this.Value = value;
			this.startLocation = startLocation;
			this.length = length;
		}
		
		public PrimitiveExpression(object value, string stringValue, AstLocation startLocation, int length)
		{
			this.Value = value;
			this.stringValue = stringValue;
			this.startLocation = startLocation;
			this.length = length;
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitPrimitiveExpression(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			PrimitiveExpression o = other as PrimitiveExpression;
			return o != null && (this.Value == AnyValue || object.Equals(this.Value, o.Value));
		}
	}
}
