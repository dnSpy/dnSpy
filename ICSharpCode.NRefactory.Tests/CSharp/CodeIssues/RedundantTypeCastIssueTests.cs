// 
// RedundantTypeCastIssueTests.cs
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
	public class RedundantTypeCastIssueTests : InspectionActionTestBase
	{
		[Test]
		public void TestSameType ()
		{
			var input = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		var i2 = ((int)i);
	}
}";
			var output = @"
class TestClass
{
	void TestMethod ()
	{
		int i = 0;
		var i2 = i;
	}
}";
			Test<RedundantTypeCastIssue> (input, 1, output);
		}

		[Test]
		public void TestInvocation ()
		{
			var input = @"
class TestClass
{
	void Test (object obj)
	{
	}
	void TestMethod (object obj)
	{
		Test ((int)obj);
	}
}";
			var output = @"
class TestClass
{
	void Test (object obj)
	{
	}
	void TestMethod (object obj)
	{
		Test (obj);
	}
}";
			Test<RedundantTypeCastIssue> (input, 1, output);
		}

		[Test]
		public void TestLambdaInvocation ()
		{
			var input = @"
class TestClass
{
	void TestMethod (object obj)
	{
		System.Action<object> a;
		a ((int)obj);
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (object obj)
	{
		System.Action<object> a;
		a (obj);
	}
}";
			Test<RedundantTypeCastIssue> (input, 1, output);
		}

		[Test]
		public void TestMember ()
		{
			var input = @"
class TestClass
{
	void TestMethod (object obj)
	{
		var str = (obj as TestClass).ToString ();
	}
}";
			var output = @"
class TestClass
{
	void TestMethod (object obj)
	{
		var str = obj.ToString ();
	}
}";
			Test<RedundantTypeCastIssue> (input, 1, output);
		}

		[Test]
		public void TestNoIssue ()
		{
			var input = @"
class TestClass
{
	void Test (int k) { }
	void TestMethod (object obj)
	{
		int i = (int)obj + 1;
		Test ((long) obj);
		(obj as TestClass).Test (0);
	}
}";
			Test<RedundantTypeCastIssue> (input, 0);
		}
	}
}
