// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
		
		TextLocation startLocation;
		public override TextLocation StartLocation {
			get {
				return startLocation;
			}
		}
		
		int length;
		public override TextLocation EndLocation {
			get {
				return new TextLocation(StartLocation.Line, StartLocation.Column + length);
			}
		}
		
		public object Value { get; private set; }
		
		string stringValue;
		
		public string StringValue {
			get { return stringValue ?? OutputVisitor.ToVBNetString(this); }
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
		
		public PrimitiveExpression(object value, TextLocation startLocation, int length)
		{
			this.Value = value;
			this.startLocation = startLocation;
			this.length = length;
		}
		
		public PrimitiveExpression(object value, string stringValue, TextLocation startLocation, int length)
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
