// 
// LocalVariableHidesMemberIssueTests.cs
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
	public class LocalVariableHidesMemberIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestField ()
		{
			var input = @"
class TestClass
{
	int i;
	void TestMethod ()
	{
		int i, j;
	}
}";
			Test<LocalVariableHidesMemberIssue> (input, 1);
		}

		[Test]
		public void TestMethod ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int TestMethod;
	}
}";
			Test<LocalVariableHidesMemberIssue> (input, 1);
		}

		[Test]
		public void TestForeach ()
		{
			var input = @"
class TestClass
{
	int i;
	void TestMethod ()
	{
		int[] array = new int [10];
		foreach (var i in array) ;
	}
}";
			Test<LocalVariableHidesMemberIssue> (input, 1);
		}

		[Test]
		public void TestStatic ()
		{
			var input = @"
class TestClass
{
	static int i;
	static void TestMethod2 ()
	{
		int i;
	}
}";
			Test<LocalVariableHidesMemberIssue> (input, 1);
		}

		[Test]
		public void TestStaticNoIssue ()
		{
			var input = @"
class TestClass
{
	static int i;
	int j;
	void TestMethod ()
	{
		int i;
	}
	static void TestMethod2 ()
	{
		int j;
	}
}";
			Test<LocalVariableHidesMemberIssue> (input, 0);
		}
	}
}
