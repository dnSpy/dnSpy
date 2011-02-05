using System;
using System.Collections.Generic;
using Ast = ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

namespace Decompiler.Transforms.Ast
{
	public class RemoveDeadLabels: AbstractAstTransformer
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
					RemoveCurrentNode();
				}
			}
			return null;
		}
	}
}
