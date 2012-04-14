// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription("Incorrect element type in foreach over generic collection",
	                  Description= "Detects hidden explicit conversions in foreach loops.",
	                  Category = IssueCategories.CodeQualityIssues,
	                  Severity = Severity.Warning)]
	public class ExplicitConversionInForEachIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase
		{
			CSharpConversions conversions;
			
			public GatherVisitor (BaseRefactoringContext ctx) : base (ctx)
			{
			}
			
			public override void VisitForeachStatement(ForeachStatement foreachStatement)
			{
				base.VisitForeachStatement(foreachStatement);
				var rr = ctx.Resolve(foreachStatement) as ForEachResolveResult;
				if (rr == null)
					return;
				if (rr.ElementType.Kind == TypeKind.Unknown)
					return;
				if (ReflectionHelper.GetTypeCode(rr.ElementType) == TypeCode.Object)
					return;
				if (conversions == null) {
					conversions = CSharpConversions.Get(ctx.Compilation);
				}
				Conversion c = conversions.ImplicitConversion(rr.ElementType, rr.ElementVariable.Type);
				if (c.IsValid)
					return;
				var csResolver = ctx.GetResolverStateBefore(foreachStatement);
				var builder = new TypeSystemAstBuilder(csResolver);
				AstType elementType = builder.ConvertType(rr.ElementType);
				AstType variableType = foreachStatement.VariableType;
				string text = ctx.TranslateString("Collection element type '{0}' is not implicitly convertible to '{1}'");
				AddIssue(variableType, string.Format(text, elementType.GetText(), variableType.GetText()));
			}
		}
	}
}
