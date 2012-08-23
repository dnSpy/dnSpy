// 
// BitwiseOperationOnNonFlagsEnumIssueTests.cs
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
	public class BitwiseOperationOnNonFlagsEnumIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestUnary ()
		{
			var input = @"
enum TestEnum
{
	Item1, Item2
}
class TestClass
{
	void TestMethod ()
	{
		var x = ~TestEnum.Item1;
	}
}";
			Test<BitwiseOperationOnNonFlagsEnumIssue> (input, 1);
		}

		public void TestAssignment (string op, bool bitwise = true)
		{
			var input = @"
enum TestEnum
{
	Item1, Item2
}
class TestClass
{
	void TestMethod ()
	{
		var x = TestEnum.Item1;
		x " + op + @"= TestEnum.Item2;
	}
}";
			Test<BitwiseOperationOnNonFlagsEnumIssue> (input, bitwise ? 1: 0);
		}
		[Test]
		public void TestAssignment ()
		{
			TestAssignment ("|");
			TestAssignment ("&");
			TestAssignment ("^");
			TestAssignment ("+", false);
		}

		public void TestBinary (string op, bool bitwise = true)
		{

			var input = @"
enum TestEnum
{
	Item1, Item2
}
class TestClass
{
	void TestMethod ()
	{
		var x =  TestEnum.Item1 " + op + @" TestEnum.Item2;
	}
}";
			Test<BitwiseOperationOnNonFlagsEnumIssue> (input, bitwise ? 1 : 0);
		}
		[Test]
		public void TestBinary()
		{
			TestBinary ("&");
			TestBinary ("|");
			TestBinary ("^");
			TestBinary ("+", false);
		}
	}
}
