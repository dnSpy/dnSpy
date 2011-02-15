using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;

namespace Decompiler.Transforms.Ast
{
	public class RemoveDeadLabels : DepthFirstAstVisitor<object, object>
	{
		List<string> usedLabels = new List<string>();
		bool collectingUsedLabels;
		
		public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			collectingUsedLabels = true;
			base.VisitConstructorDeclaration(constructorDeclaration, data);
			collectingUsedLabels = false;
			base.VisitConstructorDeclaration(constructorDeclaration, data);
			return null;
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			collectingUsedLabels = true;
			base.VisitMethodDeclaration(methodDeclaration, data);
			collectingUsedLabels = false;
			base.VisitMethodDeclaration(methodDeclaration, data);
			return null;
		}
		
		public override object VisitAccessor(Accessor accessor, object data)
		{
			collectingUsedLabels = true;
			base.VisitAccessor(accessor, data);
			collectingUsedLabels = false;
			return base.VisitAccessor(accessor, data);
		}
		
		public override object VisitGotoStatement(GotoStatement gotoStatement, object data)
		{
			if (collectingUsedLabels) {
				usedLabels.Add(gotoStatement.Label);
			}
			return null;
		}
		
		public override object VisitLabelStatement(LabelStatement labelStatement, object data)
		{
			if (!collectingUsedLabels) {
				if (!usedLabels.Contains(labelStatement.Label)) {
					labelStatement.Remove();
				}
			}
			return null;
		}
	}
}
