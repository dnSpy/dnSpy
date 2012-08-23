// 
// ExtractAnonymousMethodTests.cs
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
	public class ExtractAnonymousMethodTests : ContextActionTestBase
	{
		[Test]
		public void TestLambdaWithBodyStatement ()
		{
			Test<ExtractAnonymousMethodAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.Action<int> a = i $=>  { i++; };
	}
}", @"
class TestClass
{
	void Method (int i)
	{
		i++;
	}
	void TestMethod ()
	{
		System.Action<int> a = Method;
	}
}");
		}

		[Test]
		public void TestLambdaWithBodyExpression ()
		{
			Test<ExtractAnonymousMethodAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.Action<int> a = i $=> i++;
	}
}", @"
class TestClass
{
	void Method (int i)
	{
		i++;
	}
	void TestMethod ()
	{
		System.Action<int> a = Method;
	}
}");

			Test<ExtractAnonymousMethodAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.Func<int> a = () $=> 1;
	}
}", @"
class TestClass
{
	int Method ()
	{
		return 1;
	}
	void TestMethod ()
	{
		System.Func<int> a = Method;
	}
}");
		}

		[Test]
		public void TestAnonymousMethod ()
		{
			Test<ExtractAnonymousMethodAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.Action<int> a = $delegate (int i) { i++; };
	}
}", @"
class TestClass
{
	void Method (int i)
	{
		i++;
	}
	void TestMethod ()
	{
		System.Action<int> a = Method;
	}
}");
		}

		[Test]
		public void TestContainLocalReference ()
		{
			TestWrongContext<ExtractAnonymousMethodAction> (@"
class TestClass
{
	void TestMethod ()
	{
		int j = 1;
		System.Func<int, int> a = $delegate (int i) { return i + j; };
	}
}");
		}

		[Test]
		public void TestLambdaInField ()
		{
			Test<ExtractAnonymousMethodAction> (@"
class TestClass
{
	System.Action<int> a = i $=>  { i++; };
}", @"
class TestClass
{
	void Method (int i)
	{
		i++;
	}
	System.Action<int> a = Method;
}");
		}
	}
}
