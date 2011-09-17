// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Description of InstanceExpression.
	/// </summary>
	public class InstanceExpression : Expression
	{
		TextLocation location;
		
		public InstanceExpression(InstanceExpressionType type, TextLocation location)
		{
			this.Type = type;
			this.location = location;
		}
		
		public override TextLocation StartLocation {
			get { return location; }
		}
		
		public override TextLocation EndLocation {
			get {
				switch (Type) {
					case InstanceExpressionType.Me:
						return new TextLocation(location.Line, location.Column + "Me".Length);
					case InstanceExpressionType.MyBase:
						return new TextLocation(location.Line, location.Column + "MyBase".Length);
					case InstanceExpressionType.MyClass:
						return new TextLocation(location.Line, location.Column + "MyClass".Length);
					default:
						throw new Exception("Invalid value for InstanceExpressionType");
				}
			}
		}
		
		public InstanceExpressionType Type { get; set; }
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var expr = other as InstanceExpression;
			return expr != null &&
				Type == expr.Type;
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitInstanceExpression(this, data);
		}
	}
	
	public enum InstanceExpressionType
	{
		Me,
		MyBase,
		MyClass
	}
}
