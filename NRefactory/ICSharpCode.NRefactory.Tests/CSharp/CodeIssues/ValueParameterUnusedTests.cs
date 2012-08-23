//
// SetterDoesNotUseValueParameterTests.cs
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
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.CSharp.CodeActions;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	public class ValueParameterUnusedTests : InspectionActionTestBase
	{
		[Test]
		public void TestPropertySetter()
		{
			var input = @"class A
{
	int Property1
	{
		set {
			int val = value;
		}
	}
	int Property2
	{
		set {
		}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ValueParameterUnusedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
		}

		[Test]
		public void TestMatchingIndexerSetter()
		{
			var input = @"class A
{
	A this[int index]
	{
		set {
		}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ValueParameterUnusedIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
		}

		[Test]
		public void TestMatchingEventAdder()
		{
			var input = @"class A	
{
	delegate void TestEventHandler ();
	event TestEventHandler EventTested
	{
		add {
		}
		remove {
		}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ValueParameterUnusedIssue(), input, out context);
			Assert.AreEqual(2, issues.Count);
		}

		[Test]
		public void TestNonMatchingIndexerSetter()
		{
			var input = @"class A
{
	A this[int index]
	{
		set {
			A a = value;
		}
	}
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ValueParameterUnusedIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}

		[Test]
		public void IgnoresAutoSetter()
		{
			var input = @"class A
{
	string  Property { set; }
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ValueParameterUnusedIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void IgnoreReadOnlyProperty()
		{
			var input = @"class A
{
string  Property { get; }
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ValueParameterUnusedIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void DoesNotCrashOnNullIndexerAccessorBody()
		{
			var input = @"abstract class A
{
public abstract string this[int i] { get; set; }
}";
			TestRefactoringContext context;
			var issues = GetIssues(new ValueParameterUnusedIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
	}
}

