// 
// NegativeRelationalExpressionIssueTests.cs
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
	public class NegativeRelationalExpressionIssueTests : InspectionActionTestBase
	{

		public void Test (string op, string negatedOp)
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		var x = !(1 " + op + @" 2);
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		var x = 1 " + negatedOp + @" 2;
	}
}";
			Test<NegativeRelationalExpressionIssue> (input, 1, output);
		}

		[Test]
		public void TestEquality ()
		{
			Test ("==", "!=");
		}

		[Test]
		public void TestInEquality ()
		{
			Test ("!=", "==");
		}

		[Test]
		public void TestGreaterThan ()
		{
			Test (">", "<=");
		}

		[Test]
		public void TestGreaterThanOrEqual ()
		{
			Test (">=", "<");
		}

		[Test]
		public void TestLessThan ()
		{
			Test ("<", ">=");
		}

		[Test]
		public void TestLessThanOrEqual ()
		{
			Test ("<=", ">");
		}

		[Test]
		public void TestFloatingPoint ()
		{
			var input = @"
class TestClass
{
	void TestMethod (double d)
	{
		var x = !(d > 0.1);
	}
}";
			Test<NegativeRelationalExpressionIssue> (input, 0);
		}
	}
}
