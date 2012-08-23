//
// StaticFieldInGenericTypeTests.cs
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
using ICSharpCode.NRefactory.CSharp.CodeActions;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{

	[TestFixture]
	public class StaticFieldInGenericTypeTests : InspectionActionTestBase
	{

		[Test]
		public void GenericClass()
		{
			var input = @"
class Foo<T>
{
	static string Data;
}";
			TestRefactoringContext context;
			var issues = GetIssues(new StaticFieldInGenericTypeIssue(), input, out context);
			Assert.AreEqual(1, issues.Count);
		}
		
		[Test]
		public void NonGenericClass()
		{
			var input = @"
class Foo
{
	static string Data;
}";
			TestRefactoringContext context;
			var issues = GetIssues(new StaticFieldInGenericTypeIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
		
		[Test]
		public void NonStaticField()
		{
			var input = @"
class Foo<T>
{
	string Data;
}";
			TestRefactoringContext context;
			var issues = GetIssues(new StaticFieldInGenericTypeIssue(), input, out context);
			Assert.AreEqual(0, issues.Count);
		}
	}
}

