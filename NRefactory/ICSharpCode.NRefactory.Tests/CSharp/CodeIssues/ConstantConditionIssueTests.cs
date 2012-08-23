// 
// ConstantConditionIssueTests.cs
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

using ICSharpCode.NRefactory.CSharp.Refactoring;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeIssues
{
	[TestFixture]
	public class ConstantConditionIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestConditionalExpression ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		var a = 1 > 0 ? 1 : 0;
		var b = 1 < 0 ? 1 : 0;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		var a = 1;
		var b = 0;
	}
}";
			Test<ConstantConditionIssue> (input, 2, output);
		}

		[Test]
		public void TestIf ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int i;
		if (1 > 0)
			i = 1;
		if (1 > 0) {
			i = 1;
		}
		if (1 < 0)
			i = 1;
		if (1 == 0) {
			i = 1;
		} else {
			i = 0;
		}
		if (1 == 0) {
			i = 1;
		} else
			i = 0;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		int i;
		i = 1;
		i = 1;
		i = 0;
		i = 0;
	}
}";
			Test<ConstantConditionIssue> (input, 5, output);
		}

		[Test]
		public void TestFor ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		for (int i = 0; 1 > 0; i++) ;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		for (int i = 0; true; i++) ;
	}
}";
			Test<ConstantConditionIssue> (input, 1, output);
		}

		[Test]
		public void TestWhile ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		while (1 > 0) ;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		while (true) ;
	}
}";
			Test<ConstantConditionIssue> (input, 1, output);
		}

		[Test]
		public void TestDoWhile ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		do {
		} while (1 < 0);
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		do {
		} while (false);
	}
}";
			Test<ConstantConditionIssue> (input, 1, output);
		}

		[Test]
		public void TestNoIssue ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int x = true)
	{
		while (true) ;
		if (false) ;
		if (x) ;
	}
}";
			Test<ConstantConditionIssue> (input, 0);
		}
	}
}
