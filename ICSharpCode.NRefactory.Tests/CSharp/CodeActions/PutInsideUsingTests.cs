// 
// PutInsideUsingAction.cs
//  
// Author:
//       Mansheng Yang <lightyang0@gmail.com>
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
	public class PutInsideUsingTests : ContextActionTestBase
	{
		[Test]
		public void Test ()
		{
			Test<PutInsideUsingAction> (@"
interface ITest : System.IDisposable
{
	void Test ();
}
class TestClass
{
	void TestMethod (int i)
	{
		ITest obj $= null;
		obj.Test ();
		int a;
		if (i > 0)
			obj.Test ();
		a = 0;
	}
}", @"
interface ITest : System.IDisposable
{
	void Test ();
}
class TestClass
{
	void TestMethod (int i)
	{
		int a;
		using (ITest obj = null) {
			obj.Test ();
			if (i > 0)
				obj.Test ();
		}
		a = 0;
	}
}");
		}

		[Test]
		public void TestIDisposable ()
		{
			Test<PutInsideUsingAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.IDisposable obj $= null;
		obj.Method ();
	}
}", @"
class TestClass
{
	void TestMethod ()
	{
		using (System.IDisposable obj = null) {
			obj.Method ();
		}
	}
}");
		}
		
		[Test]
		public void TestTypeParameter ()
		{

			Test<PutInsideUsingAction> (@"
class TestClass
{
	void TestMethod<T> ()
		where T : System.IDisposable, new()
	{
		T obj $= new T ();
		obj.Method ();
	}
}", @"
class TestClass
{
	void TestMethod<T> ()
		where T : System.IDisposable, new()
	{
		using (T obj = new T ()) {
			obj.Method ();
		}
	}
}");
		}
		
		[Test]
		public void TestMultipleVariablesDeclaration ()
		{
			Test<PutInsideUsingAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.IDisposable obj, obj2 $= null, obj3;
		obj2.Method ();
	}
}", @"
class TestClass
{
	void TestMethod ()
	{
		System.IDisposable obj, obj3;
		using (System.IDisposable obj2 = null) {
			obj2.Method ();
		}
	}
}");
		}

		[Test]
		public void TestNullInitializer ()
		{
			TestWrongContext<PutInsideUsingAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.IDisposable $obj;
		obj.Method ();
	}
}");
		}

		[Test]
		public void TestMoveVariableDeclaration ()
		{
			Test<PutInsideUsingAction> (@"
class TestClass
{
	void TestMethod ()
	{
		System.IDisposable obj $= null;
		int a, b;
		a = b = 0;
		obj.Method ();
		a++;
	}
}", @"
class TestClass
{
	void TestMethod ()
	{
		int a;
		using (System.IDisposable obj = null) {
			int b;
			a = b = 0;
			obj.Method ();
		}
		a++;
	}
}");
		}
	}

}
