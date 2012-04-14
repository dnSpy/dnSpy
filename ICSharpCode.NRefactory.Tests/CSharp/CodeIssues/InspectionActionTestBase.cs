// 
// InspectionActionTestBase.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.CodeActions;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	public abstract class InspectionActionTestBase
	{
		protected static List<CodeIssue> GetIssues (ICodeIssueProvider action, string input, out TestRefactoringContext context)
		{
			context = TestRefactoringContext.Create (input);
			
			return new List<CodeIssue> (action.GetIssues (context));
		}

		protected static void CheckFix (TestRefactoringContext ctx, CodeIssue issue, string expectedOutput)
		{
			using (var script = ctx.StartScript ())
				issue.Actions.First ().Run (script);
			Assert.AreEqual (expectedOutput, ctx.Text);
		}

		protected static void CheckFix (TestRefactoringContext ctx, IEnumerable<CodeIssue> issues, string expectedOutput)
		{
			using (var script = ctx.StartScript ()) {
				foreach (var issue in issues) {
					issue.Actions.First ().Run (script);
				}
			}
			bool pass = expectedOutput == ctx.Text;
			if (!pass) {
				Console.WriteLine (ctx.Text);
			}
			Assert.AreEqual (expectedOutput, ctx.Text);
		}
	}
	
}
