using System;
using System.Collections.Generic;

using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class RemoveParenthesis: AbstractAstTransformer
	{
		public override object VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			// The following do not need to be parenthesized
			if (parenthesizedExpression.Expression is IdentifierExpression ||
			    parenthesizedExpression.Expression is PrimitiveExpression ||
			    parenthesizedExpression.Expression is ParenthesizedExpression) {
				ReplaceCurrentNode(parenthesizedExpression.Expression);
				return null;
			}
			return base.VisitParenthesizedExpression(parenthesizedExpression, data);
		}
		
		public override object VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			assignmentExpression.Left = Deparenthesize(assignmentExpression.Left);
			assignmentExpression.Right = Deparenthesize(assignmentExpression.Right);
			return base.VisitAssignmentExpression(assignmentExpression, data);
		}
		
		public override object VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			ifElseStatement.Condition = Deparenthesize(ifElseStatement.Condition);
			return base.VisitIfElseStatement(ifElseStatement, data);
		}
		
		public override object VisitVariableDeclaration(VariableDeclaration variableDeclaration, object data)
		{
			variableDeclaration.Initializer = Deparenthesize(variableDeclaration.Initializer);
			return base.VisitVariableDeclaration(variableDeclaration, data);
		}
		
		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unary, object data)
		{
			if (GetPrecedence(unary.Expression) > GetPrecedence(unary)) {
				unary.Expression = Deparenthesize(unary.Expression);
			}
			return base.VisitUnaryOperatorExpression(unary, data);
		}
		
		public override object VisitMemberReferenceExpression(MemberReferenceExpression memberRef, object data)
		{
			if (GetPrecedence(memberRef.TargetObject) >= GetPrecedence(memberRef)) {
				memberRef.TargetObject = Deparenthesize(memberRef.TargetObject);
			}
			return base.VisitMemberReferenceExpression(memberRef, data);
		}
		
		public override object VisitInvocationExpression(InvocationExpression invocation, object data)
		{
			if (GetPrecedence(invocation.TargetObject) >= GetPrecedence(invocation)) {
				invocation.TargetObject = Deparenthesize(invocation.TargetObject);
			}
			return base.VisitInvocationExpression(invocation, data);
		}
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binary, object data)
		{
			int? myPrecedence = GetPrecedence(binary);
			if (GetPrecedence(binary.Left) > myPrecedence) {
				binary.Left = Deparenthesize(binary.Left);
			}
			if (GetPrecedence(binary.Right) > myPrecedence) {
				binary.Right = Deparenthesize(binary.Right);
			}
			// Associativity
			if (GetPrecedence(binary.Left) == myPrecedence && myPrecedence.HasValue) {
				binary.Left = Deparenthesize(binary.Left);
			}
			return base.VisitBinaryOperatorExpression(binary, data);
		}
		
		public override object VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			expressionStatement.Expression = Deparenthesize(expressionStatement.Expression);
			return base.VisitExpressionStatement(expressionStatement, data);
		}
		
		public override object VisitForStatement(ForStatement forStatement, object data)
		{
			forStatement.Condition = Deparenthesize(forStatement.Condition);
			return base.VisitForStatement(forStatement, data);
		}
		
		Expression Deparenthesize(Expression expr)
		{
			if (expr is ParenthesizedExpression) {
				return Deparenthesize(((ParenthesizedExpression)expr).Expression);
			} else {
				return expr;
			}
		}
		
		int? GetPrecedence(Expression expr)
		{
			if (expr is ParenthesizedExpression) {
				return GetPrecedence(((ParenthesizedExpression)expr).Expression);
			}
			
			UnaryOperatorExpression unary = expr as UnaryOperatorExpression;
			BinaryOperatorExpression binary = expr as BinaryOperatorExpression;
			
			// see http://msdn2.microsoft.com/en-us/library/ms173145.aspx
			
			//	Primary
			//		x.y
			if (expr is MemberReferenceExpression)                             return 15;
			//		f(x)
			if (expr is InvocationExpression)                                  return 15;
			//		a[x]
			if (expr is IndexerExpression)                                     return 15;
			//		x++
			if (unary != null && unary.Op == UnaryOperatorType.PostIncrement)  return 15;
			//		x--
			if (unary != null && unary.Op == UnaryOperatorType.PostDecrement)  return 15;
			//		new T(...)
			if (expr is ObjectCreateExpression)                                return 15;
			//		new T(...){...}
			//		new {...}
			//		new T[...]
			if (expr is ArrayCreateExpression)                                 return 15;
			//		typeof(T)
			if (expr is TypeOfExpression)                                      return 15;
			//		checked(x)
			//		unchecked(x)
			//		default (T)
			//		delegate {}
			//	Unary
			//		+x
			if (unary != null && unary.Op == UnaryOperatorType.Plus)           return 14;
			//		-x
			if (unary != null && unary.Op == UnaryOperatorType.Minus)          return 14;
			//		!x
			if (unary != null && unary.Op == UnaryOperatorType.Not)            return 14;
			//		~x
			if (unary != null && unary.Op == UnaryOperatorType.BitNot)         return 14;
			//		++x
			if (unary != null && unary.Op == UnaryOperatorType.Increment)      return 14;
			//		--x
			if (unary != null && unary.Op == UnaryOperatorType.Decrement)      return 14;
			//		(T)x
			//	Multiplicative
			//		*, ,
			if (binary != null && binary.Op == BinaryOperatorType.Multiply)    return 13;
			//		/ 
			if (binary != null && binary.Op == BinaryOperatorType.Divide)      return 13;
			//		% 
			if (binary != null && binary.Op == BinaryOperatorType.Modulus)     return 13;
			//	Additive
			//		x + y
			if (binary != null && binary.Op == BinaryOperatorType.Add)         return 12;
			//		x - y
			if (binary != null && binary.Op == BinaryOperatorType.Subtract)    return 12;
			//	Shift
			//		x << y
			//		x >> y
			//	Relational and Type Testing
			//		x < y
			if (binary != null && binary.Op == BinaryOperatorType.LessThan)           return 10;
			//		x > y
			if (binary != null && binary.Op == BinaryOperatorType.GreaterThan)        return 10;
			//		x <= y
			if (binary != null && binary.Op == BinaryOperatorType.LessThanOrEqual)    return 10;
			//		x >= y
			if (binary != null && binary.Op == BinaryOperatorType.GreaterThanOrEqual) return 10;
			//		x is T
			//		x as T
			//	Equality
			//		x == y
			if (binary != null && binary.Op == BinaryOperatorType.Equality)    return 9;
			//		x != y
			if (binary != null && binary.Op == BinaryOperatorType.InEquality)  return 9;
			//	Logical AND
			//		x & y
			if (binary != null && binary.Op == BinaryOperatorType.BitwiseAnd)  return 8;
			//	Logical XOR
			//		x ^ y
			if (binary != null && binary.Op == BinaryOperatorType.ExclusiveOr) return 7;
			//	Logical OR
			//		x | y
			if (binary != null && binary.Op == BinaryOperatorType.BitwiseOr)   return 6;
			//	Conditional AND
			//		x && y
			if (binary != null && binary.Op == BinaryOperatorType.LogicalAnd)  return 5;
			//	Conditional OR
			//		x || y
			if (binary != null && binary.Op == BinaryOperatorType.LogicalOr)   return 4;
			//	Null coalescing
			//		X ?? y
			//	Conditional
			//		x ?: y : z
			//	Assignment or anonymous function
			//		=, , =>
			if (expr is AssignmentExpression)                                  return 1;
			//		x op= y
			//		(T x) => y
			
			return null;
		}
	}
}
