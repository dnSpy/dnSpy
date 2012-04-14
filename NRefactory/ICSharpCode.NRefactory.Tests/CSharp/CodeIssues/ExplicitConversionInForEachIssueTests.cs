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
using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class ExplicitConversionInForEachIssueTests : InspectionActionTestBase
	{
		[Test]
		public void NoWarningOnNonGenericCollection ()
		{
			var input = @"class Foo {
	void Bar (System.Collections.ArrayList c)
	{
		foreach (string element in c) { }
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new ExplicitConversionInForEachIssue (), input, out context);
			Assert.AreEqual (0, issues.Count);
		}
		
		[Test]
		public void NoWarningOnImplicitConversion ()
		{
			var input = @"class Foo {
	void Bar (System.Collections.Generic.List<int> c)
	{
		foreach (double element in c) { }
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new ExplicitConversionInForEachIssue (), input, out context);
			Assert.AreEqual (0, issues.Count);
		}
		
		[Test]
		public void WarningOnExplicitConversionBetweenInterfaces ()
		{
			var input = @"using System.Collections.Generic;
class Foo {
	void Bar (IList<IList<IDisposable>> c)
	{
		foreach (IDisposable element in c) { }
	}
}";

			TestRefactoringContext context;
			var issues = GetIssues (new ExplicitConversionInForEachIssue (), input, out context);
			Assert.AreEqual (1, issues.Count);
		}
	}
}
