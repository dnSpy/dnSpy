// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.NRefactory.Ast
{
	public class PrimitiveExpression : Expression
	{
		object val;
		string stringValue;
		
		public object Value {
			get {
				return val;
			}
			set {
				val = value;
			}
		}
		
		public string StringValue {
			get {
				return stringValue;
			}
			set {
				stringValue = value == null ? String.Empty : value;
			}
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
			                     stringValue,
			                     Value,
			                     Value == null ? "null" : Value.GetType().FullName
			                    );
		}
	}
}
