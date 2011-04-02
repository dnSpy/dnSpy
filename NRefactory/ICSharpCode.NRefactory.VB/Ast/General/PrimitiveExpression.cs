// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using ICSharpCode.NRefactory.VB.PrettyPrinter;
using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class PrimitiveExpression : Expression
	{
		string stringValue;
		
		public Parser.LiteralFormat LiteralFormat { get; set; }
		public object Value { get; set; }
		
		public string StringValue {
			get {
				if (stringValue == null)
					return VBNetOutputVisitor.ToVBNetString(this);
				else
					return stringValue;
			}
			set {
				stringValue = value == null ? String.Empty : value;
			}
		}
		
		public PrimitiveExpression(object val)
		{
			this.Value = val;
		}
		
		public PrimitiveExpression(object val, string stringValue)
		{
			this.Value       = val;
			this.StringValue = stringValue;
		}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return visitor.VisitPrimitiveExpression(this, data);
		}
		
		public override string ToString()
		{
			return String.Format("[PrimitiveExpression: Value={1}, ValueType={2}, StringValue={0}]",
			                     this.StringValue,
			                     this.Value,
			                     this.Value == null ? "null" : this.Value.GetType().FullName
			                    );
		}
	}
}
