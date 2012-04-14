// 
// NotImplementedExceptionInspector.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using System;
using ICSharpCode.NRefactory.PatternMatching;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	/// <summary>
	/// This inspector just shows that there is a not implemented exception. It doesn't offer a fix.
	/// Should only be shown in overview bar, no underlining.
	/// </summary>
	[IssueDescription("Show NotImplementedExceptions", Description="Shows NotImplementedException throws in the quick task bar.", Category = IssueCategories.Notifications, Severity = Severity.Suggestion, IssueMarker = IssueMarker.None)]
	public class NotImplementedExceptionIssue : ICodeIssueProvider
	{
		public IEnumerable<CodeIssue> GetIssues(BaseRefactoringContext context)
		{
			return new GatherVisitor(context, this).GetIssues();
		}

		class GatherVisitor : GatherVisitorBase
		{
			readonly NotImplementedExceptionIssue inspector;
			
			public GatherVisitor (BaseRefactoringContext ctx, NotImplementedExceptionIssue inspector) : base (ctx)
			{
				this.inspector = inspector;
			}

			public override void VisitThrowStatement(ThrowStatement throwStatement)
			{
				var result = ctx.Resolve(throwStatement.Expression);
				if (result.Type.Equals(ctx.Compilation.FindType(typeof(System.NotImplementedException)))) {
					AddIssue(throwStatement, ctx.TranslateString("NotImplemented exception thrown"));
				}

				base.VisitThrowStatement(throwStatement);
			}
		}
	}
}

