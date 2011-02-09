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
		
		public override object VisitPropertyGetRegion(PropertyGetRegion propertyGetRegion, object data)
		{
			collectingUsedLabels = true;
			base.VisitPropertyGetRegion(propertyGetRegion, data);
			collectingUsedLabels = false;
			return base.VisitPropertyGetRegion(propertyGetRegion, data);
		}
		
		public override object VisitPropertySetRegion(PropertySetRegion propertySetRegion, object data)
		{
			collectingUsedLabels = true;
			base.VisitPropertySetRegion(propertySetRegion, data);
			collectingUsedLabels = false;
			return base.VisitPropertySetRegion(propertySetRegion, data);
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
