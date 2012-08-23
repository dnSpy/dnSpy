//
// OptionalParameterCouldBeSkippedTests.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2012 Simon Lindgren
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
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class OptionalParameterCouldBeSkippedTests : InspectionActionTestBase
	{
		[Test]
		public void SimpleCase()
		{
			var input = @"
class TestClass
{
	void Foo(string a1 = ""a1"")
	{
	}

	void Bar()
	{
		Foo (""a1"");
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new OptionalParameterCouldBeSkippedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues [0];
			Assert.AreEqual(2, issue.Actions.Count);
				
			CheckFix(context, issues [0], @"
class TestClass
{
	void Foo(string a1 = ""a1"")
	{
	}

	void Bar()
	{
		Foo ();
	}
}");
		}

		[Test]
		public void ChecksConstructors()
		{
			var input = @"
class TestClass
{
	public TestClass(string a1 = ""a1"")
	{
	}

	void Bar()
	{
		var foo = new TestClass (""a1"");
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new OptionalParameterCouldBeSkippedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues [0];
			Assert.AreEqual(2, issue.Actions.Count);
			
			CheckFix(context, issues [0], @"
class TestClass
{
	public TestClass(string a1 = ""a1"")
	{
	}

	void Bar()
	{
		var foo = new TestClass ();
	}
}");
		}

		[Test]
		public void IgnoresAllParametersPreceedingANeededOne()
		{
			var input = @"
class TestClass
{
	void Foo(string a1 = ""a1"", string a2 = ""a2"", string a3 = ""a3"")
	{
	}

	void Bar()
	{
		Foo (""a1"", ""Another string"", ""a3"");
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new OptionalParameterCouldBeSkippedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			var issue = issues [0];
			Assert.AreEqual(2, issue.Actions.Count);
			
			CheckFix(context, issues [0], @"
class TestClass
{
	void Foo(string a1 = ""a1"", string a2 = ""a2"", string a3 = ""a3"")
	{
	}

	void Bar()
	{
		Foo (""a1"", ""Another string"");
	}
}");
		}
		
		[Test]
		public void ChecksParametersIfParamsAreUnused()
		{
			var input = @"
class TestClass
{
	void Foo(string a1 = ""a1"", string a2 = ""a2"", string a3 = ""a3"", params string[] extraStrings)
	{
	}

	void Bar()
	{
		Foo (""a1"", ""a2"", ""a3"");
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new OptionalParameterCouldBeSkippedIssue(), input, out context);
			Assert.AreEqual(3, issues.Count);
			Assert.AreEqual(2, issues[0].Actions.Count);
			Assert.AreEqual(2, issues[1].Actions.Count);
			Assert.AreEqual(2, issues[2].Actions.Count);
			
			CheckFix(context, issues [2], @"
class TestClass
{
	void Foo(string a1 = ""a1"", string a2 = ""a2"", string a3 = ""a3"", params string[] extraStrings)
	{
	}

	void Bar()
	{
		Foo ();
	}
}");
		}
		
		[Test]
		public void IgnoresIfParamsAreUsed()
		{
			var input = @"
class TestClass
{
	void Foo(string a1 = ""a1"", string a2 = ""a2"", string a3 = ""a3"", params string[] extraStrings)
	{
	}

	void Bar()
	{
		Foo (""a1"", ""a2"", ""a3"", ""extraString"");
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new OptionalParameterCouldBeSkippedIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void NamedArgument()
		{
			var input = @"
class TestClass
{
	void Foo(string a1 = ""a1"", string a2 = ""a2"")
	{
	}

	void Bar()
	{
		Foo (a2: ""a2"");
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new OptionalParameterCouldBeSkippedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
			Assert.AreEqual(2, issues[0].Actions.Count);

			CheckFix(context, issues [0], @"
class TestClass
{
	void Foo(string a1 = ""a1"", string a2 = ""a2"")
	{
	}

	void Bar()
	{
		Foo ();
	}
}");
		}
		
		[Test]
		public void DoesNotStopAtDifferingNamedParameters()
		{
			var input = @"
class TestClass
{
	void Foo(string a1 = ""a1"", string a2 = ""a2"", string a3 = ""a3"")
	{
	}

	void Bar()
	{
		Foo (""a1"", ""a2"", a3: ""non-default"");
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new OptionalParameterCouldBeSkippedIssue(), input, out context);
			Assert.AreEqual(2, issues.Count);
			Assert.AreEqual(2, issues[0].Actions.Count);
			Assert.AreEqual(2, issues[1].Actions.Count);
			
			CheckFix(context, issues [1], @"
class TestClass
{
	void Foo(string a1 = ""a1"", string a2 = ""a2"", string a3 = ""a3"")
	{
	}

	void Bar()
	{
		Foo (a3: ""non-default"");
	}
}");
		}
		
		[Test]
		public void DoesNotStopAtNamedParamsArray()
		{
			var input = @"
class TestClass
{
	void Foo(string a1 = ""a1"", string a2 = ""a2"", params string[] extras)
	{
	}

	void Bar()
	{
		Foo (""a1"", ""a2"", extras: new [] { ""extra1"" });
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new OptionalParameterCouldBeSkippedIssue(), input, out context);
			Assert.AreEqual(2, issues.Count);
			Assert.AreEqual(2, issues[0].Actions.Count);
			Assert.AreEqual(2, issues[1].Actions.Count);

			// TODO: Fix formatting
			CheckFix(context, issues [1], @"
class TestClass
{
	void Foo(string a1 = ""a1"", string a2 = ""a2"", params string[] extras)
	{
	}

	void Bar()
	{
		Foo (extras: new[] {
	""extra1""
});
	}
}");
		}
	}
}

