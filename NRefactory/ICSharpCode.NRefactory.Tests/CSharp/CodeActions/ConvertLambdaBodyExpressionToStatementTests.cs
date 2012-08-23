// 
// ConvertLambdaBodyExpressionToStatementTests.cs
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

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class ConvertLambdaBodyExpressionToStatementTests : ContextActionTestBase
	{
		[Test]
		public void TestReturn ()
		{
			Test<ConvertLambdaBodyExpressionToStatementAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.Func<int, int> f = i $=> i + 1;
	}
}", @"
class TestClass
{
	void TestMethod ()
	{
		System.Func<int, int> f = i => {
			return i + 1;
		};
	}
}");
		}

		[Test]
		public void TestExprStatement()
		{
			Test<ConvertLambdaBodyExpressionToStatementAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.Action<int> f = i $=> i++;
	}
}", @"
class TestClass
{
	void TestMethod ()
	{
		System.Action<int> f = i => {
			i++;
		};
	}
}");
		}

		[Test]
		public void TestWrongContext ()
		{
			TestWrongContext<ConvertLambdaBodyExpressionToStatementAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.Func<int, int> f = i $=> {
			return i + 1;
		};
	}
}");
		}

		[Test]
		public void TestParenthesis ()
		{
			Test<ConvertLambdaBodyExpressionToStatementAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.Func<int, int> f;
		f = (i $=> i + 1);
	}
}", @"
class TestClass
{
	void TestMethod ()
	{
		System.Func<int, int> f;
		f = (i => {
			return i + 1;
		});
	}
}");
		}

		[Test]
		public void TestInvocation ()
		{
			Test<ConvertLambdaBodyExpressionToStatementAction> (@"
class TestClass
{
	void Test (int k, System.Func<int, int> f) { }
	void TestMethod ()
	{
		Test (1, i $=> i + 1);
	}
}", @"
class TestClass
{
	void Test (int k, System.Func<int, int> f) { }
	void TestMethod ()
	{
		Test (1, i => {
			return i + 1;
		});
	}
}");
			Test<ConvertLambdaBodyExpressionToStatementAction> (@"
class TestClass
{
	void Test2 (System.Action<int> a) { }
	void TestMethod ()
	{
		Test2 (i $=> i++);
	}
}", @"
class TestClass
{
	void Test2 (System.Action<int> a) { }
	void TestMethod ()
	{
		Test2 (i => {
			i++;
		});
	}
}");
		}
	}
}
