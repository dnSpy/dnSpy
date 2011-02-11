// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.Ast
{
	public abstract class Expression : AbstractNode, INullable
	{
		public static Expression Null {
			get {
				return NullExpression.Instance;
			}
		}
		
		public virtual bool IsNull {
			get {
				return false;
			}
		}
		
		public static Expression CheckNull(Expression expression)
		{
			return expression == null ? NullExpression.Instance : expression;
		}
		
		/// <summary>
		/// Returns the existing expression plus the specified integer value.
		/// The old <paramref name="expr"/> object is not modified, but might be a subobject on the new expression
		/// (and thus its parent property is modified).
		/// </summary>
		public static Expression AddInteger(Expression expr, int value)
		{
			PrimitiveExpression pe = expr as PrimitiveExpression;
			if (pe != null && pe.Value is int) {
				int newVal = (int)pe.Value + value;
				return new PrimitiveExpression(newVal, newVal.ToString(System.Globalization.NumberFormatInfo.InvariantInfo));
			}
			BinaryOperatorExpression boe = expr as BinaryOperatorExpression;
			if (boe != null && boe.Op == BinaryOperatorType.Add) {
				// clone boe:
				boe = new BinaryOperatorExpression(boe.Left, boe.Op, boe.Right);
				
				boe.Right = AddInteger(boe.Right, value);
				if (boe.Right is PrimitiveExpression && ((PrimitiveExpression)boe.Right).Value is int) {
					int newVal = (int)((PrimitiveExpression)boe.Right).Value;
					if (newVal == 0) {
						return boe.Left;
					} else if (newVal < 0) {
						((PrimitiveExpression)boe.Right).Value = -newVal;
						boe.Op = BinaryOperatorType.Subtract;
					}
				}
				return boe;
			}
			if (boe != null && boe.Op == BinaryOperatorType.Subtract) {
				pe = boe.Right as PrimitiveExpression;
				if (pe != null && pe.Value is int) {
					int newVal = (int)pe.Value - value;
					if (newVal == 0)
						return boe.Left;
					
					// clone boe:
					boe = new BinaryOperatorExpression(boe.Left, boe.Op, boe.Right);
					
					if (newVal < 0) {
						newVal = -newVal;
						boe.Op = BinaryOperatorType.Add;
					}
					boe.Right = new PrimitiveExpression(newVal, newVal.ToString(System.Globalization.NumberFormatInfo.InvariantInfo));
					return boe;
				}
			}
			BinaryOperatorType opType = BinaryOperatorType.Add;
			if (value < 0) {
				value = -value;
				opType = BinaryOperatorType.Subtract;
			}
			return new BinaryOperatorExpression(expr, opType, new PrimitiveExpression(value, value.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)));
		}
	}
	
	internal sealed class NullExpression : Expression
	{
		internal static readonly NullExpression Instance = new NullExpression();
		
		public override bool IsNull {
			get {
				return true;
			}
		}
		
		public override object AcceptVisitor(IAstVisitor visitor, object data)
		{
			return null;
		}
		
		public override string ToString()
		{
			return String.Format("[NullExpression]");
		}
	}
}
