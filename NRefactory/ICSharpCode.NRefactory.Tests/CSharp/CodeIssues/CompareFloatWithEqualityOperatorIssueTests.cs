// 
// CompareFloatWithEqualityOperatorIssueTests.cs
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
	public class CompareFloatWithEqualityOperatorIssueTests : InspectionActionTestBase
	{
		public void Test (string inputOp, string outputOp)
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		double x = 0.1;
		bool test = x " + inputOp + @" 0.1;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		double x = 0.1;
		bool test = System.Math.Abs (x - 0.1) " + outputOp + @" EPSILON;
	}
}";
			Test<CompareFloatWithEqualityOperatorIssue> (input, 1, output);
		}

		[Test]
		public void TestEquality ()
		{
			Test ("==", "<");
		}

		[Test]
		public void TestInequality ()
		{
			Test ("!=", ">");
		}

		[Test]
		public void TestNaN ()
		{
			var input = @"
class TestClass
{
	void TestMethod (double x, float y)
	{
		bool test = x == System.Double.NaN;
		bool test2 = x != double.NaN;
		bool test3 = y == float.NaN;
		bool test4 = x != float.NaN;
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (double x, float y)
	{
		bool test = double.IsNaN (x);
		bool test2 = !double.IsNaN (x);
		bool test3 = float.IsNaN (y);
		bool test4 = !double.IsNaN (x);
	}
}";
			Test<CompareFloatWithEqualityOperatorIssue> (input, 4, output);
		}
	}
}
