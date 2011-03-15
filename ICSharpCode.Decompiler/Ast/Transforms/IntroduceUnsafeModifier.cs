// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	public class IntroduceUnsafeModifier : DepthFirstAstVisitor<object, bool>, IAstTransform
	{
		public void Run(AstNode compilationUnit)
		{
			compilationUnit.AcceptVisitor(this, null);
		}
		
		protected override bool VisitChildren(AstNode node, object data)
		{
			bool result = false;
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				result |= child.AcceptVisitor(this, data);
			}
			if (result && node is AttributedNode && !(node is Accessor)) {
				((AttributedNode)node).Modifiers |= Modifiers.Unsafe;
				return false;
			}
			return result;
		}
		
		public override bool VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			return true;
		}
		
		public override bool VisitComposedType(ComposedType composedType, object data)
		{
			if (composedType.PointerRank > 0)
				return true;
			else
				return base.VisitComposedType(composedType, data);
		}
		
		public override bool VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			if (unaryOperatorExpression.Operator == UnaryOperatorType.Dereference || unaryOperatorExpression.Operator == UnaryOperatorType.AddressOf)
				return true;
			else
				return base.VisitUnaryOperatorExpression(unaryOperatorExpression, data);
		}
		
		public override bool VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
		{
			bool result = base.VisitMemberReferenceExpression(memberReferenceExpression, data);
			UnaryOperatorExpression uoe = memberReferenceExpression.Target as UnaryOperatorExpression;
			if (uoe != null && uoe.Operator == UnaryOperatorType.Dereference) {
				PointerReferenceExpression pre = new PointerReferenceExpression();
				pre.Target = uoe.Expression.Detach();
				pre.MemberName = memberReferenceExpression.MemberName;
				memberReferenceExpression.TypeArguments.MoveTo(pre.TypeArguments);
				pre.CopyAnnotationsFrom(uoe);
				pre.CopyAnnotationsFrom(memberReferenceExpression);
				memberReferenceExpression.ReplaceWith(pre);
			}
			return result;
		}
	}
}
