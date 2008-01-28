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
	}
}
