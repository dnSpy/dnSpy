// 
// SplitDeclarationListTests.cs
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
	public class SplitDeclarationListTests : ContextActionTestBase
	{
		[Test]
		public void TestLocalVariable ()
		{
			Test<SplitDeclarationListAction> (@"
class TestClass
{
	void TestMethod()
	{
		int $a, b, c;
	}
}", @"
class TestClass
{
	void TestMethod()
	{
		int a;
		int b;
		int c;
	}
}");
		}

		[Test]
		public void TestField ()
		{
			Test<SplitDeclarationListAction> (@"
class TestClass
{
	public int $a, b, c;
}", @"
class TestClass
{
	public int a;
	public int b;
	public int c;
}");
		}

		[Test]
		public void TestEvent ()
		{
			Test<SplitDeclarationListAction> (@"
class TestClass
{
	event System.EventHandler $a, b, c;
}", @"
class TestClass
{
	event System.EventHandler a;
	event System.EventHandler b;
	event System.EventHandler c;
}");
		}

		[Test]
		public void TestFixedField ()
		{
			Test<SplitDeclarationListAction> (@"
struct TestStruct
{
	unsafe fixed int $a[10], b[10], c[10];
}", @"
struct TestStruct
{
	unsafe fixed int a[10];
	unsafe fixed int b[10];
	unsafe fixed int c[10];
}");
		}

		[Test]
		public void TestVariableInFor ()
		{
			TestWrongContext<SplitDeclarationListAction> (@"
class TestClass
{
	void TestMethod ()
	{
		for (int a = 0, b = 0, $c = 0; a < 10; a++) {
		}
	}
}");
		}

		[Test]
		public void TestSingleVariable ()
		{
			TestWrongContext<SplitDeclarationListAction> (@"
class TestClass
{
	void TestMethod ()
	{
		int $a;
	}
}");
		}
	}
}
