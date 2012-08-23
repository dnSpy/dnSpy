// 
// ReferenceEqualsCalledWithValueTypeIssueTest.cs
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
	public class ReferenceEqualsCalledWithValueTypeIssueTest : InspectionActionTestBase
	{
		[Test]
		public void TestValueType ()
		{
			var input = @"
class TestClass
{
	void TestMethod (int i, int j)
	{
		var x = object.ReferenceEquals (i, j);
		var x2 = ReferenceEquals (i, j);
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (int i, int j)
	{
		var x = object.Equals (i, j);
		var x2 = object.Equals (i, j);
	}
}";
			Test<ReferenceEqualsCalledWithValueTypeIssue> (input, 2, output);
		}

		[Test]
		public void TestNoIssue ()
		{
			var input = @"
class TestClass
{
	void TestMethod<T> (object i, T j)
	{
		var x = object.ReferenceEquals (i, i);
		var x2 = object.ReferenceEquals (j, j);
	}
}";
			Test<ReferenceEqualsCalledWithValueTypeIssue> (input, 0);
		}
	}
}
