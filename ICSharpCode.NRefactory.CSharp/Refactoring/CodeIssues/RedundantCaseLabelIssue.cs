// 
// RedundantCaseLabelIssue.cs
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

using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	[IssueDescription ("Redundant 'case' label",
						Description = "Redundant 'case' label",
						Category = IssueCategories.Redundancies,
						Severity = Severity.Warning,
						IssueMarker = IssueMarker.GrayOut)]
	public class RedundantCaseLabelIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues (BaseRefactoringContext context)
		{
			return new GatherVisitor (context).GetIssues ();
		}

		class GatherVisitor : GatherVisitorBase
		{
			public GatherVisitor(BaseRefactoringContext ctx)
				: base (ctx)
			{
			}

			public override void VisitSwitchSection (SwitchSection switchSection)
			{
				base.VisitSwitchSection (switchSection);

				if (switchSection.CaseLabels.Count <2)
					return;

				var lastLabel = switchSection.CaseLabels.LastOrNullObject ();
				if (!lastLabel.Expression.IsNull)
					return;
				AddIssue (switchSection.FirstChild.StartLocation, lastLabel.StartLocation,
					ctx.TranslateString ("Remove redundant 'case' label"), scipt => {
						foreach (var label in switchSection.CaseLabels) {
							if (label != lastLabel)
								scipt.Remove (label);
						}
					});
			}
		}
	}
}
