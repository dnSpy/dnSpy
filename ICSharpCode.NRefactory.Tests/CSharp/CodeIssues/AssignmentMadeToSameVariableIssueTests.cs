// 
// AssignmentMadeToSameVariableIssueTests.cs
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
	public class AssignmentMadeToSameVariableIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestVariable ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int a = 0;
		a = a;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		int a = 0;
	}
}";
			Test<AssignmentMadeToSameVariableIssue> (input, 1, output);
		}

		[Test]
		public void TestParameter ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int a)
	{
		a = a;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int a)
	{
	}
}";
			Test<AssignmentMadeToSameVariableIssue> (input, 1, output);
		}

		[Test]
		public void TestField ()
		{
			var input = @"
class TestClass
{
	int a;
	void TestMethod ()
	{
		a = a;
		this.a = this.a;
		this.a = a;
	}
}";
			var output = @"
class TestClass
{
	int a;
	void TestMethod ()
	{
	}
}";
			Test<AssignmentMadeToSameVariableIssue> (input, 3, output);
		}

		[Test]
		public void TestFix ()
		{
			var input = @"
class TestClass
{
	void Test (int i) { }
	void TestMethod ()
	{
		int a = 0;
		a = a;
		Test (a = a);
	}
}";
			var output = @"
class TestClass
{
	void Test (int i) { }
	void TestMethod ()
	{
		int a = 0;
		Test (a);
	}
}";
			Test<AssignmentMadeToSameVariableIssue> (input, 2, output);
		}

		[Test]
		public void TestNoIssue ()
		{
			var input = @"
class TestClass
{
	int a;
	int b;

	public int Prop { get; set; }

	void TestMethod (int a)
	{
		a = b;
		this.a = b;
		this.a = a;
		Prop = Prop;
	}
}";
			Test<AssignmentMadeToSameVariableIssue> (input, 0);
		}
	}
}
