using System;
using System.Collections.Generic;
using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class Idioms: AbstractAstTransformer
	{
		public override object VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			base.VisitInvocationExpression(invocationExpression, data);
			
			// Reduce "String.Concat(a, b)" to "a + b"
			MemberReferenceExpression target = invocationExpression.TargetObject as MemberReferenceExpression;
			if (target != null &&
			    target.MemberName == "Concat" &&
			    invocationExpression.Arguments.Count == 2 &&
			    target.TargetObject is IdentifierExpression &&
			    (target.TargetObject as IdentifierExpression).Identifier == "String")
			{
				ReplaceCurrentNode(
					new BinaryOperatorExpression(
						invocationExpression.Arguments[0],
						BinaryOperatorType.Add,
						invocationExpression.Arguments[1]
					)
				);
			}
			
			return null;
		}
		
		public override object VisitAssignmentExpression(AssignmentExpression assignment, object data)
		{
			IdentifierExpression ident = assignment.Left as IdentifierExpression;
			BinaryOperatorExpression binary = assignment.Right as BinaryOperatorExpression;
			if (ident != null && binary != null) {
				IdentifierExpression binaryLeft = binary.Left as IdentifierExpression;
				if (binaryLeft != null &&
				    binaryLeft.Identifier == ident.Identifier) {
					if (binary.Right is PrimitiveExpression &&
					    1.Equals((binary.Right as PrimitiveExpression).Value)) {
						if (binary.Op == BinaryOperatorType.Add) {
							ReplaceCurrentNode(new UnaryOperatorExpression(ident, UnaryOperatorType.PostIncrement));
						}
						if (binary.Op == BinaryOperatorType.Subtract) {
							ReplaceCurrentNode(new UnaryOperatorExpression(ident, UnaryOperatorType.PostDecrement));
						}
					} else {
						if (binary.Op == BinaryOperatorType.Add) {
							ReplaceCurrentNode(new AssignmentExpression(ident, AssignmentOperatorType.Add, binary.Right));
						}
						if (binary.Op == BinaryOperatorType.Subtract) {
							ReplaceCurrentNode(new AssignmentExpression(ident, AssignmentOperatorType.Subtract, binary.Right));
						}
					}
					return null;
				}
			}
			return null;
		}
		
		public override object VisitCastExpression(CastExpression castExpression, object data)
		{
			if (castExpression.CastTo.Type == "int" &&
			    castExpression.Expression is MemberReferenceExpression &&
			    (castExpression.Expression as MemberReferenceExpression).MemberName == "Length") {
				ReplaceCurrentNode(castExpression.Expression);
				return null;
			}
			return base.VisitCastExpression(castExpression, data);
		}
	}
}
