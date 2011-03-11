using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.PatternMatching;
using Mono.Cecil;
using Ast = ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	/// <summary>
	/// Replaces method calls with the appropriate operator expressions.
	/// Also simplifies "x = x op y" into "x op= y" where possible.
	/// </summary>
	public class ReplaceMethodCallsWithOperators : DepthFirstAstVisitor<object, object>, IAstTransform
	{
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			base.VisitInvocationExpression(invocationExpression, data);
			
			MethodReference methodRef = invocationExpression.Annotation<MethodReference>();
			if (methodRef == null)
				return null;
			var arguments = invocationExpression.Arguments.ToArray();
			
			// Reduce "String.Concat(a, b)" to "a + b"
			if (methodRef != null && methodRef.Name == "Concat" && methodRef.DeclaringType.FullName == "System.String" && arguments.Length >= 2)
			{
				invocationExpression.Arguments.Clear(); // detach arguments from invocationExpression
				Expression expr = arguments[0];
				for (int i = 1; i < arguments.Length; i++) {
					expr = new BinaryOperatorExpression(expr, BinaryOperatorType.Add, arguments[i]);
				}
				invocationExpression.ReplaceWith(expr);
				return null;
			}
			
			switch (methodRef.FullName) {
				case "System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle)":
					if (arguments.Length == 1) {
						MemberReferenceExpression mre = arguments[0] as MemberReferenceExpression;
						if (mre != null && mre.Target is TypeOfExpression && mre.MemberName == "TypeHandle") {
							invocationExpression.ReplaceWith(mre.Target);
							return null;
						}
					}
					break;
			}
			
			BinaryOperatorType? bop = GetBinaryOperatorTypeFromMetadataName(methodRef.Name);
			if (bop != null && arguments.Length == 2) {
				invocationExpression.Arguments.Clear(); // detach arguments from invocationExpression
				invocationExpression.ReplaceWith(
					new BinaryOperatorExpression(arguments[0], bop.Value, arguments[1]).WithAnnotation(methodRef)
				);
				return null;
			}
			UnaryOperatorType? uop = GetUnaryOperatorTypeFromMetadataName(methodRef.Name);
			if (uop != null && arguments.Length == 1) {
				arguments[0].Remove(); // detach argument
				invocationExpression.ReplaceWith(
					new UnaryOperatorExpression(uop.Value, arguments[0]).WithAnnotation(methodRef)
				);
				return null;
			}
			if (methodRef.Name == "op_Explicit" && arguments.Length == 1) {
				arguments[0].Remove(); // detach argument
				invocationExpression.ReplaceWith(
					arguments[0].CastTo(AstBuilder.ConvertType(methodRef.ReturnType, methodRef.MethodReturnType))
					.WithAnnotation(methodRef)
				);
				return null;
			}
			if (methodRef.Name == "op_Implicit" && arguments.Length == 1) {
				arguments[0].Remove(); // detach argument
				invocationExpression.ReplaceWith(arguments[0]);
				return null;
			}
			
			return null;
		}
		
		BinaryOperatorType? GetBinaryOperatorTypeFromMetadataName(string name)
		{
			switch (name) {
				case "op_Addition":
					return BinaryOperatorType.Add;
				case "op_Subtraction":
					return BinaryOperatorType.Subtract;
				case "op_Multiply":
					return BinaryOperatorType.Multiply;
				case "op_Division":
					return BinaryOperatorType.Divide;
				case "op_Modulus":
					return BinaryOperatorType.Modulus;
				case "op_BitwiseAnd":
					return BinaryOperatorType.BitwiseAnd;
				case "op_BitwiseOr":
					return BinaryOperatorType.BitwiseOr;
				case "op_ExlusiveOr":
					return BinaryOperatorType.ExclusiveOr;
				case "op_LeftShift":
					return BinaryOperatorType.ShiftLeft;
				case "op_RightShift":
					return BinaryOperatorType.ShiftRight;
				case "op_Equality":
					return BinaryOperatorType.Equality;
				case "op_Inequality":
					return BinaryOperatorType.InEquality;
				case "op_LessThan":
					return BinaryOperatorType.LessThan;
				case "op_LessThanOrEqual":
					return BinaryOperatorType.LessThanOrEqual;
				case "op_GreaterThan":
					return BinaryOperatorType.GreaterThan;
				case "op_GreaterThanOrEqual":
					return BinaryOperatorType.GreaterThanOrEqual;
				default:
					return null;
			}
		}
		
		UnaryOperatorType? GetUnaryOperatorTypeFromMetadataName(string name)
		{
			switch (name) {
				case "op_LogicalNot":
					return UnaryOperatorType.Not;
				case  "op_OnesComplement":
					return UnaryOperatorType.BitNot;
				case "op_UnaryNegation":
					return UnaryOperatorType.Minus;
				case "op_UnaryPlus":
					return UnaryOperatorType.Plus;
				case "op_Increment":
					return UnaryOperatorType.Increment;
				case "op_Decrement":
					return UnaryOperatorType.Decrement;
				default:
					return null;
			}
		}
		
		public override object VisitAssignmentExpression(AssignmentExpression assignment, object data)
		{
			base.VisitAssignmentExpression(assignment, data);
			// Combine "x = x op y" into "x op= y"
			BinaryOperatorExpression binary = assignment.Right as BinaryOperatorExpression;
			if (binary != null && assignment.Operator == AssignmentOperatorType.Assign) {
				if (IsWithoutSideEffects(assignment.Left) && assignment.Left.Match(binary.Left) != null) {
					switch (binary.Operator) {
						case BinaryOperatorType.Add:
							assignment.Operator = AssignmentOperatorType.Add;
							break;
						case BinaryOperatorType.Subtract:
							assignment.Operator = AssignmentOperatorType.Subtract;
							break;
						case BinaryOperatorType.Multiply:
							assignment.Operator = AssignmentOperatorType.Multiply;
							break;
						case BinaryOperatorType.Divide:
							assignment.Operator = AssignmentOperatorType.Divide;
							break;
						case BinaryOperatorType.Modulus:
							assignment.Operator = AssignmentOperatorType.Modulus;
							break;
						case BinaryOperatorType.ShiftLeft:
							assignment.Operator = AssignmentOperatorType.ShiftLeft;
							break;
						case BinaryOperatorType.ShiftRight:
							assignment.Operator = AssignmentOperatorType.ShiftRight;
							break;
						case BinaryOperatorType.BitwiseAnd:
							assignment.Operator = AssignmentOperatorType.BitwiseAnd;
							break;
						case BinaryOperatorType.BitwiseOr:
							assignment.Operator = AssignmentOperatorType.BitwiseOr;
							break;
						case BinaryOperatorType.ExclusiveOr:
							assignment.Operator = AssignmentOperatorType.ExclusiveOr;
							break;
					}
					if (assignment.Operator != AssignmentOperatorType.Assign) {
						// If we found a shorter operator, get rid of the BinaryOperatorExpression:
						assignment.CopyAnnotationsFrom(binary);
						assignment.Right = binary.Right;
					}
				}
			}
			if (assignment.Operator == AssignmentOperatorType.Add || assignment.Operator == AssignmentOperatorType.Subtract) {
				// detect increment/decrement
				if (assignment.Right.Match(new PrimitiveExpression(1)) != null) {
					// only if it's not a custom operator
					if (assignment.Annotation<MethodReference>() == null) {
						UnaryOperatorType type;
						// When the parent is an expression statement, pre- or post-increment doesn't matter;
						// so we can pick post-increment which is more commonly used (for (int i = 0; i < x; i++))
						if (assignment.Parent is ExpressionStatement)
							type = (assignment.Operator == AssignmentOperatorType.Add) ? UnaryOperatorType.PostIncrement : UnaryOperatorType.PostDecrement;
						else 
							type = (assignment.Operator == AssignmentOperatorType.Add) ? UnaryOperatorType.Increment : UnaryOperatorType.Decrement;
						assignment.ReplaceWith(new UnaryOperatorExpression(type, assignment.Left.Detach()).CopyAnnotationsFrom(assignment));
					}
				}
			}
			return null;
		}
		
		static bool IsWithoutSideEffects(Expression left)
		{
			if (left is ThisReferenceExpression)
				return true;
			if (left is IdentifierExpression)
				return true;
			MemberReferenceExpression mre = left as MemberReferenceExpression;
			if (mre != null)
				return mre.Annotation<FieldReference>() != null && IsWithoutSideEffects(mre.Target);
			return false;
		}
		
		void IAstTransform.Run(AstNode node)
		{
			node.AcceptVisitor(this, null);
		}
	}
}
