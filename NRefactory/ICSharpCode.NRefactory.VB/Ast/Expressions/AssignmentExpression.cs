// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class AssignmentExpression : Expression
	{
		public readonly static Role<Expression> LeftExpressionRole = BinaryOperatorExpression.LeftExpressionRole;
		public readonly static Role<VBTokenNode> OperatorRole = BinaryOperatorExpression.OperatorRole;
		public readonly static Role<Expression> RightExpressionRole = BinaryOperatorExpression.RightExpressionRole;
		
		public AssignmentExpression(Expression left, AssignmentOperatorType type, Expression right)
		{
			AddChild(left, LeftExpressionRole);
			AddChild(right, RightExpressionRole);
			Operator = type;
		}
		
		public Expression Left {
			get { return GetChildByRole(LeftExpressionRole); }
			set { SetChildByRole(LeftExpressionRole, value); }
		}
		
		public AssignmentOperatorType Operator { get; set; }
		
		public Expression Right {
			get { return GetChildByRole(RightExpressionRole); }
			set { SetChildByRole(RightExpressionRole, value); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			throw new NotImplementedException();
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitAssignmentExpression(this, data);
		}
	}
}
