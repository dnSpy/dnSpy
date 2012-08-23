// 
// ParameterOnlyAssignedIssue.cs
// 
// Author:
//      Mansheng Yang <lightyang0@gmail.com>
// 
// Copyright (c) 2012 Mansheng Yang <lightyang0@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using ICSharpCode.NRefactory.Semantics;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Parameter is only assigned",
					   Description = "Parameter is assigned by its value is never used.",
					   Category = IssueCategories.CodeQualityIssues,
					   Severity = Severity.Warning,
					   IssueMarker = IssueMarker.Underline)]
	public class ParameterOnlyAssignedIssue : VariableOnlyAssignedIssue
	{
		internal override GatherVisitorBase GetGatherVisitor (BaseRefactoringContext ctx, SyntaxTree unit)
		{
			return new GatherVisitor (ctx, unit);
		}

		private class GatherVisitor : GatherVisitorBase
		{
			SyntaxTree unit;

			public GatherVisitor (BaseRefactoringContext ctx, SyntaxTree unit)
				: base (ctx)
			{
				this.unit = unit;
			}

			public override void VisitParameterDeclaration (ParameterDeclaration parameterDeclaration)
			{
				base.VisitParameterDeclaration (parameterDeclaration);

				var resolveResult = ctx.Resolve (parameterDeclaration) as LocalResolveResult;
				if (resolveResult == null)
					return;
				if (!TestOnlyAssigned (ctx, unit, resolveResult.Variable))
					return;
				AddIssue (parameterDeclaration.NameToken, 
					ctx.TranslateString ("Parameter is assigned by its value is never used"));
			}
		}
	}
}
