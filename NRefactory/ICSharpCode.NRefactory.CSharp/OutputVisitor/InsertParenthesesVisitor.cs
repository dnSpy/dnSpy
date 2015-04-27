// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
	public class InsertParenthesesVisitor : DepthFirstAstVisitor
	{
		/// <summary>
		/// Gets/Sets whether the visitor should insert parentheses to make the code better looking.
		/// If this property is false, it will insert parentheses only where strictly required by the language spec.
		/// </summary>
		public bool InsertParenthesesForReadability { get; set; }
		
		const int Primary = 16;
		const int QueryOrLambda = 15;
		const int Unary = 14;
		const int RelationalAndTypeTesting = 10;
		const int Equality = 9;
		const int Conditional = 2;
		const int Assignment = 1;
		
		/// <summary>
		/// Gets the row number in the C# 4.0 spec operator precedence table.
		/// </summary>
		static int GetPrecedence(Expression expr)
		{
			// Note: the operator precedence table on MSDN is incorrect
			if (expr is QueryExpression) {
				// Not part of the table in the C# spec, but we need to ensure that queries within
				// primary expressions get parenthesized.
				return QueryOrLambda;
			}
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
				return Assignment;
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
		public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
		{
			ParenthesizeIfRequired(memberReferenceExpression.Target, Primary);
			base.VisitMemberReferenceExpression(memberReferenceExpression);
		}
		
		public override void VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
		{
			ParenthesizeIfRequired(pointerReferenceExpression.Target, Primary);
			base.VisitPointerReferenceExpression(pointerReferenceExpression);
		}
		
		public override void VisitInvocationExpression(InvocationExpression invocationExpression)
		{
			ParenthesizeIfRequired(invocationExpression.Target, Primary);
			base.VisitInvocationExpression(invocationExpression);
		}
		
		public override void VisitIndexerExpression(IndexerExpression indexerExpression)
		{
			ParenthesizeIfRequired(indexerExpression.Target, Primary);
			ArrayCreateExpression ace = indexerExpression.Target as ArrayCreateExpression;
			if (ace != null && (InsertParenthesesForReadability || ace.Initializer.IsNull)) {
				// require parentheses for "(new int[1])[0]"
				Parenthesize(indexerExpression.Target);
			}
			base.VisitIndexerExpression(indexerExpression);
		}
		
		// Unary expressions
		public override void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
		{
			ParenthesizeIfRequired(unaryOperatorExpression.Expression, GetPrecedence(unaryOperatorExpression));
			UnaryOperatorExpression child = unaryOperatorExpression.Expression as UnaryOperatorExpression;
			if (child != null && InsertParenthesesForReadability)
				Parenthesize(child);
			base.VisitUnaryOperatorExpression(unaryOperatorExpression);
		}
		
		public override void VisitCastExpression(CastExpression castExpression)
		{
			ParenthesizeIfRequired(castExpression.Expression, InsertParenthesesForReadability ? Primary : Unary);
			// There's a nasty issue in the C# grammar: cast expressions including certain operators are ambiguous in some cases
			// "(int)-1" is fine, but "(A)-b" is not a cast.
			UnaryOperatorExpression uoe = castExpression.Expression as UnaryOperatorExpression;
			if (uoe != null && !(uoe.Operator == UnaryOperatorType.BitNot || uoe.Operator == UnaryOperatorType.Not)) {
				if (TypeCanBeMisinterpretedAsExpression(castExpression.Type)) {
					Parenthesize(castExpression.Expression);
				}
			}
			// The above issue can also happen with PrimitiveExpressions representing negative values:
			PrimitiveExpression pe = castExpression.Expression as PrimitiveExpression;
			if (pe != null && pe.Value != null && TypeCanBeMisinterpretedAsExpression(castExpression.Type)) {
				TypeCode typeCode = Type.GetTypeCode(pe.Value.GetType());
				switch (typeCode) {
					case TypeCode.SByte:
						if ((sbyte)pe.Value < 0)
							Parenthesize(castExpression.Expression);
						break;
					case TypeCode.Int16:
						if ((short)pe.Value < 0)
							Parenthesize(castExpression.Expression);
						break;
					case TypeCode.Int32:
						if ((int)pe.Value < 0)
							Parenthesize(castExpression.Expression);
						break;
					case TypeCode.Int64:
						if ((long)pe.Value < 0)
							Parenthesize(castExpression.Expression);
						break;
					case TypeCode.Single:
						if ((float)pe.Value < 0)
							Parenthesize(castExpression.Expression);
						break;
					case TypeCode.Double:
						if ((double)pe.Value < 0)
							Parenthesize(castExpression.Expression);
						break;
					case TypeCode.Decimal:
						if ((decimal)pe.Value < 0)
							Parenthesize(castExpression.Expression);
						break;
				}
			}
			base.VisitCastExpression(castExpression);
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
		public override void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
		{
			int precedence = GetPrecedence(binaryOperatorExpression);
			if (binaryOperatorExpression.Operator == BinaryOperatorType.NullCoalescing) {
				if (InsertParenthesesForReadability) {
					ParenthesizeIfRequired(binaryOperatorExpression.Left, Primary);
					ParenthesizeIfRequired(binaryOperatorExpression.Right, Primary);
				} else {
					// ?? is right-associative
					ParenthesizeIfRequired(binaryOperatorExpression.Left, precedence + 1);
					ParenthesizeIfRequired(binaryOperatorExpression.Right, precedence);
				}
			} else {
				if (InsertParenthesesForReadability && precedence < Equality) {
					// In readable mode, boost the priority of the left-hand side if the operator
					// there isn't the same as the operator on this expression.
					if (GetBinaryOperatorType(binaryOperatorExpression.Left) == binaryOperatorExpression.Operator) {
						ParenthesizeIfRequired(binaryOperatorExpression.Left, precedence);
					} else {
						ParenthesizeIfRequired(binaryOperatorExpression.Left, Equality);
					}
					ParenthesizeIfRequired(binaryOperatorExpression.Right, Equality);
				} else {
					// all other binary operators are left-associative
					ParenthesizeIfRequired(binaryOperatorExpression.Left, precedence);
					ParenthesizeIfRequired(binaryOperatorExpression.Right, precedence + 1);
				}
			}
			base.VisitBinaryOperatorExpression(binaryOperatorExpression);
		}
		
		BinaryOperatorType? GetBinaryOperatorType(Expression expr)
		{
			BinaryOperatorExpression boe = expr as BinaryOperatorExpression;
			if (boe != null)
				return boe.Operator;
			else
				return null;
		}
		
		public override void VisitIsExpression(IsExpression isExpression)
		{
			if (InsertParenthesesForReadability) {
				// few people know the precedence of 'is', so always put parentheses in nice-looking mode.
				ParenthesizeIfRequired(isExpression.Expression, Primary);
			} else {
				ParenthesizeIfRequired(isExpression.Expression, RelationalAndTypeTesting);
			}
			base.VisitIsExpression(isExpression);
		}
		
		public override void VisitAsExpression(AsExpression asExpression)
		{
			if (InsertParenthesesForReadability) {
				// few people know the precedence of 'as', so always put parentheses in nice-looking mode.
				ParenthesizeIfRequired(asExpression.Expression, Primary);
			} else {
				ParenthesizeIfRequired(asExpression.Expression, RelationalAndTypeTesting);
			}
			base.VisitAsExpression(asExpression);
		}
		
		// Conditional operator
		public override void VisitConditionalExpression(ConditionalExpression conditionalExpression)
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
			base.VisitConditionalExpression(conditionalExpression);
		}
		
		public override void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
		{
			// assignment is right-associative
			ParenthesizeIfRequired(assignmentExpression.Left, Assignment + 1);
			if (InsertParenthesesForReadability) {
				ParenthesizeIfRequired(assignmentExpression.Right, RelationalAndTypeTesting + 1);
			} else {
				ParenthesizeIfRequired(assignmentExpression.Right, Assignment);
			}
			base.VisitAssignmentExpression(assignmentExpression);
		}
		
		// don't need to handle lambdas, they have lowest precedence and unambiguous associativity
		
		public override void VisitQueryExpression(QueryExpression queryExpression)
		{
			// Query expressions are strange beasts:
			// "var a = -from b in c select d;" is valid, so queries bind stricter than unary expressions.
			// However, the end of the query is greedy. So their start sort of has a high precedence,
			// while their end has a very low precedence. We handle this by checking whether a query is used
			// as left part of a binary operator, and parenthesize it if required.
			if (queryExpression.Role == BinaryOperatorExpression.LeftRole)
				Parenthesize(queryExpression);
			if (queryExpression.Parent is IsExpression || queryExpression.Parent is AsExpression)
				Parenthesize(queryExpression);
			if (InsertParenthesesForReadability) {
				// when readability is desired, always parenthesize query expressions within unary or binary operators
				if (queryExpression.Parent is UnaryOperatorExpression || queryExpression.Parent is BinaryOperatorExpression)
					Parenthesize(queryExpression);
			}
			base.VisitQueryExpression(queryExpression);
		}
	}
}
