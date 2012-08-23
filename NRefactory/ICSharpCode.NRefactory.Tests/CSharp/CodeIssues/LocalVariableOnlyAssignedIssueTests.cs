// 
// LocalVariableOnlyAssignedIssueTests.cs
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
	public class LocalVariableOnlyAssignedIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestUnusedValue ()
		{
			var input1 = @"
class TestClass
{
	void TestMethod()
	{
		int i = 1;
		i++;
	}
}";
			Test<LocalVariableOnlyAssignedIssue> (input1, 1);

			var input2 = @"
class TestClass
{
	void TestMethod()
	{
		int i;
		i = 1;
	}
}";
			Test<LocalVariableOnlyAssignedIssue> (input2, 1);
		}

		[Test]
		public void TestUsedValue ()
		{
			var input = @"
class TestClass
{
	int TestMethod()
	{
		int i;
		i = 1;
		int j = i + 1;
		return j;
	}
}";
			Test<LocalVariableOnlyAssignedIssue> (input, 0);
		}

		[Test]
		public void TestOutArgument ()
		{
			var input1 = @"
class TestClass
{
	void Test (out int i)
	{
		i = 1;
	}
	void TestMethod()
	{
		int i = 1;
		Test (out i);
	}
}";
			Test<LocalVariableOnlyAssignedIssue> (input1, 1);
		}
	}
}
