// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.Decompiler.Ast.Transforms
{
	public class IntroduceUnsafeModifier : DepthFirstAstVisitor<object, bool>, IAstTransform
	{
		public static readonly object PointerArithmeticAnnotation = new PointerArithmetic();
		
		sealed class PointerArithmetic {}
		
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
			base.VisitPointerReferenceExpression(pointerReferenceExpression, data);
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
			bool result = base.VisitUnaryOperatorExpression(unaryOperatorExpression, data);
			if (unaryOperatorExpression.Operator == UnaryOperatorType.Dereference) {
				BinaryOperatorExpression bop = unaryOperatorExpression.Expression as BinaryOperatorExpression;
				if (bop != null && bop.Operator == BinaryOperatorType.Add && bop.Annotation<PointerArithmetic>() != null) {
					// transform "*(ptr + int)" to "ptr[int]"
					IndexerExpression indexer = new IndexerExpression();
					indexer.Target = bop.Left.Detach();
					indexer.Arguments.Add(bop.Right.Detach());
					indexer.CopyAnnotationsFrom(unaryOperatorExpression);
					indexer.CopyAnnotationsFrom(bop);
					unaryOperatorExpression.ReplaceWith(indexer);
				}
				return true;
			} else if (unaryOperatorExpression.Operator == UnaryOperatorType.AddressOf) {
				return true;
			} else {
				return result;
			}
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
		
		public override bool VisitStackAllocExpression(StackAllocExpression stackAllocExpression, object data)
		{
			base.VisitStackAllocExpression(stackAllocExpression, data);
			return true;
		}
	}
}
