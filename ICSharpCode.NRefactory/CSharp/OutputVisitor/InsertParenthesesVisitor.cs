// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Inserts the parentheses into the AST that are needed to ensure the AST can be printed correctly.
	/// For example, if the AST contains
	/// BinaryOperatorExpresson(2, Mul, BinaryOperatorExpression(1, Add, 1))); printing that AST
	/// would incorrectly result in "2 * 1 + 1". By running InsertParenthesesVisitor, the necessary
	/// parentheses are inserted: "2 * (1 + 1)".
	/// </summary>
	public class InsertParenthesesVisitor : DepthFirstAstVisitor<object, object>
	{
		/// <summary>
		/// Gets/Sets whether the visitor should insert parentheses to make the code better looking.
		/// If this property is false, it will insert parentheses only where strictly required by the language spec.
		/// </summary>
		public bool InsertParenthesesForReadability { get; set; }
		
		const int Primary = 15;
		const int Unary = 14;
		const int RelationalAndTypeTesting = 10;
		const int Equality = 9;
		const int Conditional = 2;
		const int AssignmentAndLambda = 1;
		
		/// <summary>
		/// Gets the row number in the C# 4.0 spec operator precedence table.
		/// </summary>
		static int GetPrecedence(Expression expr)
		{
			// Note: the operator precedence table on MSDN is incorrect
			UnaryOperatorExpression uoe = expr as UnaryOperatorExpression;
			if (uoe != null) {
				if (uoe.Operator == UnaryOperatorType.PostDecrement || uoe.Operator == UnaryOperatorType.PostIncrement)
					return Primary;
				else
					return Unary;
			}
			if (expr is CastExpression)
				return Unary;
			BinaryOperatorExpression boe = expr as BinaryOperatorExpression;
			if (boe != null) {
				switch (boe.Operator) {
					case BinaryOperatorType.Multiply:
					case BinaryOperatorType.Divide:
					case BinaryOperatorType.Modulus:
						return 13; // multiplicative
					case BinaryOperatorType.Add:
					case BinaryOperatorType.Subtract:
						return 12; // additive
					case BinaryOperatorType.ShiftLeft:
					case BinaryOperatorType.ShiftRight:
						return 11;
					case BinaryOperatorType.GreaterThan:
					case BinaryOperatorType.GreaterThanOrEqual:
					case BinaryOperatorType.LessThan:
					case BinaryOperatorType.LessThanOrEqual:
						return RelationalAndTypeTesting;
					case BinaryOperatorType.Equality:
					case BinaryOperatorType.InEquality:
						return Equality;
					case BinaryOperatorType.BitwiseAnd:
						return 8;
					case BinaryOperatorType.ExclusiveOr:
						return 7;
					case BinaryOperatorType.BitwiseOr:
						return 6;
					case BinaryOperatorType.ConditionalAnd:
						return 5;
					case BinaryOperatorType.ConditionalOr:
						return 4;
					case BinaryOperatorType.NullCoalescing:
						return 3;
					default:
						throw new NotSupportedException("Invalid value for BinaryOperatorType");
				}
			}
			if (expr is IsExpression || expr is AsExpression)
				return RelationalAndTypeTesting;
			if (expr is ConditionalExpression)
				return Conditional;
			if (expr is AssignmentExpression || expr is LambdaExpression)
				return AssignmentAndLambda;
			// anything else: primary expression
			return Primary;
		}
		
		/// <summary>
		/// Parenthesizes the expression if it does not have the minimum required precedence.
		/// </summary>
		static void ParenthesizeIfRequired(Expression expr, int minimumPrecedence)
		{
			if (GetPrecedence(expr) < minimumPrecedence) {
				Parenthesize(expr);
			}
		}

		static void Parenthesize(Expression expr)
		{
			expr.ReplaceWith(e => new ParenthesizedExpression { Expression = e });
		}
		
		// Primary expressions
		public override object VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
		{
			ParenthesizeIfRequired(memberReferenceExpression.Target, Primary);
			return base.VisitMemberReferenceExpression(memberReferenceExpression, data);
		}
		
		public override object VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			ParenthesizeIfRequired(pointerReferenceExpression.Target, Primary);
			return base.VisitPointerReferenceExpression(pointerReferenceExpression, data);
		}
		
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			ParenthesizeIfRequired(invocationExpression.Target, Primary);
			return base.VisitInvocationExpression(invocationExpression, data);
		}
		
		public override object VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			ParenthesizeIfRequired(indexerExpression.Target, Primary);
			return base.VisitIndexerExpression(indexerExpression, data);
		}
		
		// Unary expressions
		public override object VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			ParenthesizeIfRequired(unaryOperatorExpression.Expression, Unary);
			return base.VisitUnaryOperatorExpression(unaryOperatorExpression, data);
		}
		
		public override object VisitCastExpression(CastExpression castExpression, object data)
		{
			ParenthesizeIfRequired(castExpression.Expression, Unary);
			// There's a nasty issue in the C# grammar: cast expressions including certain operators are ambiguous in some cases
			// "(int)-1" is fine, but "(A)-b" is not a cast.
			UnaryOperatorExpression uoe = castExpression.Expression as UnaryOperatorExpression;
			if (InsertParenthesesForReadability) {
				if (uoe != null)
					Parenthesize(castExpression.Expression);
			} else {
				if (uoe != null && !(uoe.Operator == UnaryOperatorType.BitNot || uoe.Operator == UnaryOperatorType.Not)) {
					if (TypeCanBeMisinterpretedAsExpression(castExpression.Type)) {
						Parenthesize(castExpression.Expression);
					}
				}
			}
			return base.VisitCastExpression(castExpression, data);
		}
		
		static bool TypeCanBeMisinterpretedAsExpression(AstType type)
		{
			// SimpleTypes can always be misinterpreted as IdentifierExpressions
			// MemberTypes can be misinterpreted as MemberReferenceExpressions if they don't use double colon
			// PrimitiveTypes or ComposedTypes can never be misinterpreted as expressions.
			MemberType mt = type as MemberType;
			if (mt != null)
				return !mt.IsDoubleColon;
			else
				return type is SimpleType;
		}
		
		// Binary Operators
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			int precedence = GetPrecedence(binaryOperatorExpression);
			if (binaryOperatorExpression.Operator == BinaryOperatorType.NullCoalescing) {
				if (InsertParenthesesForReadability) {
					ParenthesizeIfRequired(binaryOperatorExpression.Left, Primary);
					ParenthesizeIfRequired(binaryOperatorExpression.Right, Primary);
				} else {
					// ?? is right-associate
					ParenthesizeIfRequired(binaryOperatorExpression.Left, precedence + 1);
					ParenthesizeIfRequired(binaryOperatorExpression.Right, precedence);
				}
			} else {
				if (InsertParenthesesForReadability && precedence < Equality) {
					ParenthesizeIfRequired(binaryOperatorExpression.Left, Equality);
					ParenthesizeIfRequired(binaryOperatorExpression.Right, Equality);
				} else {
					// all other binary operators are left-associative
					ParenthesizeIfRequired(binaryOperatorExpression.Left, precedence);
					ParenthesizeIfRequired(binaryOperatorExpression.Right, precedence + 1);
				}
			}
			return base.VisitBinaryOperatorExpression(binaryOperatorExpression, data);
		}
		
		public override object VisitIsExpression(IsExpression isExpression, object data)
		{
			if (InsertParenthesesForReadability) {
				// few people know the precedence of 'is', so always put parentheses in nice-looking mode.
				ParenthesizeIfRequired(isExpression.Expression, Primary);
			} else {
				ParenthesizeIfRequired(isExpression.Expression, RelationalAndTypeTesting);
			}
			return base.VisitIsExpression(isExpression, data);
		}
		
		public override object VisitAsExpression(AsExpression asExpression, object data)
		{
			if (InsertParenthesesForReadability) {
				// few people know the precedence of 'as', so always put parentheses in nice-looking mode.
				ParenthesizeIfRequired(asExpression.Expression, Primary);
			} else {
				ParenthesizeIfRequired(asExpression.Expression, RelationalAndTypeTesting);
			}
			return base.VisitAsExpression(asExpression, data);
		}
		
		// Conditional operator
		public override object VisitConditionalExpression(ConditionalExpression conditionalExpression, object data)
		{
			// Associativity here is a bit tricky:
			// (a ? b : c ? d : e) == (a ? b : (c ? d : e))
			// (a ? b ? c : d : e) == (a ? (b ? c : d) : e)
			// Only ((a ? b : c) ? d : e) strictly needs the additional parentheses
			if (InsertParenthesesForReadability) {
				// Precedence of ?: can be confusing; so always put parentheses in nice-looking mode.
				ParenthesizeIfRequired(conditionalExpression.Condition, Primary);
				ParenthesizeIfRequired(conditionalExpression.TrueExpression, Primary);
				ParenthesizeIfRequired(conditionalExpression.FalseExpression, Primary);
			} else {
				ParenthesizeIfRequired(conditionalExpression.Condition, Conditional + 1);
				ParenthesizeIfRequired(conditionalExpression.TrueExpression, Conditional);
				ParenthesizeIfRequired(conditionalExpression.FalseExpression, Conditional);
			}
			return base.VisitConditionalExpression(conditionalExpression, data);
		}
		
		public override object VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			// assignment is right-associative
			ParenthesizeIfRequired(assignmentExpression.Left, AssignmentAndLambda + 1);
			if (InsertParenthesesForReadability) {
				ParenthesizeIfRequired(assignmentExpression.Right, RelationalAndTypeTesting + 1);
			} else {
				ParenthesizeIfRequired(assignmentExpression.Right, AssignmentAndLambda);
			}
			return base.VisitAssignmentExpression(assignmentExpression, data);
		}
		
		// don't need to handle lambdas, they have lowest precedence and unambiguous associativity
	}
}
