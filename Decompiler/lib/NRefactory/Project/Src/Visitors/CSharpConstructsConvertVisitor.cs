// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Visitors
{
	/// <summary>
	/// Converts special C# constructs to use more general AST classes.
	/// </summary>
	public class CSharpConstructsConvertVisitor : ConvertVisitorBase
	{
		// The following conversions are implemented:
		//   a == null -> a Is Nothing
		//   a != null -> a Is Not Nothing
		//   i++ / ++i as statement: convert to i += 1
		//   i-- / --i as statement: convert to i -= 1
		//   ForStatement -> ForNextStatement when for-loop is simple
		//   if (Event != null) Event(this, bla); -> RaiseEvent Event(this, bla)
		//   Casts to value types are marked as conversions
		
		public override object VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			if (binaryOperatorExpression.Op == BinaryOperatorType.Equality || binaryOperatorExpression.Op == BinaryOperatorType.InEquality) {
				if (IsNullLiteralExpression(binaryOperatorExpression.Left)) {
					Expression tmp = binaryOperatorExpression.Left;
					binaryOperatorExpression.Left = binaryOperatorExpression.Right;
					binaryOperatorExpression.Right = tmp;
				}
				if (IsNullLiteralExpression(binaryOperatorExpression.Right)) {
					if (binaryOperatorExpression.Op == BinaryOperatorType.Equality) {
						binaryOperatorExpression.Op = BinaryOperatorType.ReferenceEquality;
					} else {
						binaryOperatorExpression.Op = BinaryOperatorType.ReferenceInequality;
					}
				}
			}
			return base.VisitBinaryOperatorExpression(binaryOperatorExpression, data);
		}
		
		static bool IsNullLiteralExpression(Expression expr)
		{
			PrimitiveExpression pe = expr as PrimitiveExpression;
			if (pe == null) return false;
			return pe.Value == null;
		}
		
		
		public override object VisitExpressionStatement(ExpressionStatement expressionStatement, object data)
		{
			UnaryOperatorExpression uoe = expressionStatement.Expression as UnaryOperatorExpression;
			if (uoe != null) {
				switch (uoe.Op) {
					case UnaryOperatorType.Increment:
					case UnaryOperatorType.PostIncrement:
						expressionStatement.Expression = new AssignmentExpression(uoe.Expression, AssignmentOperatorType.Add, new PrimitiveExpression(1, "1"));
						break;
					case UnaryOperatorType.Decrement:
					case UnaryOperatorType.PostDecrement:
						expressionStatement.Expression = new AssignmentExpression(uoe.Expression, AssignmentOperatorType.Subtract, new PrimitiveExpression(1, "1"));
						break;
				}
			}
			return base.VisitExpressionStatement(expressionStatement, data);
		}
		
		public override object VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			BinaryOperatorExpression boe = ifElseStatement.Condition as BinaryOperatorExpression;
			// the BinaryOperatorExpression might be inside a ParenthesizedExpression
			if (boe == null && ifElseStatement.Condition is ParenthesizedExpression) {
				boe = (ifElseStatement.Condition as ParenthesizedExpression).Expression as BinaryOperatorExpression;
			}
			if (ifElseStatement.ElseIfSections.Count == 0
			    && ifElseStatement.FalseStatement.Count == 0
			    && ifElseStatement.TrueStatement.Count == 1
			    && boe != null
			    && boe.Op == BinaryOperatorType.InEquality
			    && (IsNullLiteralExpression(boe.Left) || IsNullLiteralExpression(boe.Right))
			   )
			{
				string ident = GetPossibleEventName(boe.Left) ?? GetPossibleEventName(boe.Right);
				ExpressionStatement se = ifElseStatement.TrueStatement[0] as ExpressionStatement;
				if (se == null) {
					BlockStatement block = ifElseStatement.TrueStatement[0] as BlockStatement;
					if (block != null && block.Children.Count == 1) {
						se = block.Children[0] as ExpressionStatement;
					}
				}
				if (ident != null && se != null) {
					InvocationExpression ie = se.Expression as InvocationExpression;
					if (ie != null && GetPossibleEventName(ie.TargetObject) == ident) {
						ReplaceCurrentNode(new RaiseEventStatement(ident, ie.Arguments));
					}
				}
			}
			return base.VisitIfElseStatement(ifElseStatement, data);
		}
		
		string GetPossibleEventName(Expression expression)
		{
			IdentifierExpression ident = expression as IdentifierExpression;
			if (ident != null)
				return ident.Identifier;
			MemberReferenceExpression fre = expression as MemberReferenceExpression;
			if (fre != null && fre.TargetObject is ThisReferenceExpression)
				return fre.MemberName;
			return null;
		}
		
		public override object VisitForStatement(ForStatement forStatement, object data)
		{
			base.VisitForStatement(forStatement, data);
			ConvertForStatement(forStatement);
			return null;
		}
		
		void ConvertForStatement(ForStatement forStatement)
		{
			//   ForStatement -> ForNextStatement when for-loop is simple
			
			// only the following forms of the for-statement are allowed:
			// for (TypeReference name = start; name < oneAfterEnd; name += step)
			// for (name = start; name < oneAfterEnd; name += step)
			// for (TypeReference name = start; name <= end; name += step)
			// for (name = start; name <= end; name += step)
			// for (TypeReference name = start; name > oneAfterEnd; name -= step)
			// for (name = start; name > oneAfterEnd; name -= step)
			// for (TypeReference name = start; name >= end; name -= step)
			// for (name = start; name >= end; name -= step)
			
			// check if the form is valid and collect TypeReference, name, start, end and step
			if (forStatement.Initializers.Count != 1)
				return;
			if (forStatement.Iterator.Count != 1)
				return;
			ExpressionStatement statement = forStatement.Iterator[0] as ExpressionStatement;
			if (statement == null)
				return;
			AssignmentExpression iterator = statement.Expression as AssignmentExpression;
			if (iterator == null || (iterator.Op != AssignmentOperatorType.Add && iterator.Op != AssignmentOperatorType.Subtract))
				return;
			IdentifierExpression iteratorIdentifier = iterator.Left as IdentifierExpression;
			if (iteratorIdentifier == null)
				return;
			PrimitiveExpression stepExpression = iterator.Right as PrimitiveExpression;
			if (stepExpression == null || !(stepExpression.Value is int))
				return;
			int step = (int)stepExpression.Value;
			if (iterator.Op == AssignmentOperatorType.Subtract)
				step = -step;
			
			BinaryOperatorExpression condition = forStatement.Condition as BinaryOperatorExpression;
			if (condition == null || !(condition.Left is IdentifierExpression))
				return;
			if ((condition.Left as IdentifierExpression).Identifier != iteratorIdentifier.Identifier)
				return;
			Expression end;
			if (iterator.Op == AssignmentOperatorType.Subtract) {
				if (condition.Op == BinaryOperatorType.GreaterThanOrEqual) {
					end = condition.Right;
				} else if (condition.Op == BinaryOperatorType.GreaterThan) {
					end = Expression.AddInteger(condition.Right, 1);
				} else {
					return;
				}
			} else {
				if (condition.Op == BinaryOperatorType.LessThanOrEqual) {
					end = condition.Right;
				} else if (condition.Op == BinaryOperatorType.LessThan) {
					end = Expression.AddInteger(condition.Right, -1);
				} else {
					return;
				}
			}
			
			Expression start;
			TypeReference typeReference = null;
			LocalVariableDeclaration varDecl = forStatement.Initializers[0] as LocalVariableDeclaration;
			if (varDecl != null) {
				if (varDecl.Variables.Count != 1
				    || varDecl.Variables[0].Name != iteratorIdentifier.Identifier
				    || varDecl.Variables[0].Initializer == null)
					return;
				typeReference = varDecl.GetTypeForVariable(0);
				start = varDecl.Variables[0].Initializer;
			} else {
				statement = forStatement.Initializers[0] as ExpressionStatement;
				if (statement == null)
					return;
				AssignmentExpression assign = statement.Expression as AssignmentExpression;
				if (assign == null || assign.Op != AssignmentOperatorType.Assign)
					return;
				if (!(assign.Left is IdentifierExpression))
					return;
				if ((assign.Left as IdentifierExpression).Identifier != iteratorIdentifier.Identifier)
					return;
				start = assign.Right;
			}
			
			ReplaceCurrentNode(
				new ForNextStatement {
					TypeReference = typeReference,
					VariableName = iteratorIdentifier.Identifier,
					Start = start,
					End = end,
					Step = (step == 1) ? null : new PrimitiveExpression(step, step.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)),
					EmbeddedStatement = forStatement.EmbeddedStatement
				});
		}
		
		public override object VisitCastExpression(CastExpression castExpression, object data)
		{
			if (castExpression.CastType == CastType.Cast) {
				// Casts to value types are marked as conversions
				// this code only supports primitive types, user-defined value types are handled by
				// the DOM-aware CSharpToVBNetConvertVisitor
				string type;
				if (TypeReference.PrimitiveTypesCSharpReverse.TryGetValue(castExpression.CastTo.Type, out type)) {
					if (type != "object" && type != "string") {
						// type is value type
						castExpression.CastType = CastType.Conversion;
					}
				}
			}
			return base.VisitCastExpression(castExpression, data);
		}
	}
}
