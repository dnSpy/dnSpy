// 
// DeclareLocalVariableTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class DeclareLocalVariableTests : ContextActionTestBase
	{
		[Test()]
		public void TestSimpleInline ()
		{
			TestRefactoringContext.UseExplict = true;
			Test<DeclareLocalVariableAction> (@"class TestClass
{
	int Foo() {}
	void Test ()
	{
		<-Foo()->;
	}
}", @"class TestClass
{
	int Foo() {}
	void Test ()
	{
		int i = Foo ();
	}
}");
		}
		[Test()]
		public void TestSimpleInlineImplicit ()
		{
			Test<DeclareLocalVariableAction> (@"class TestClass
{
	int Foo() {}
	void Test ()
	{
		<-Foo()->;
	}
}", @"class TestClass
{
	int Foo() {}
	void Test ()
	{
		var i = Foo ();
	}
}");
		}

		[Test()]
		public void TestReplaceAll ()
		{
			TestRefactoringContext.UseExplict = true;
			Test<DeclareLocalVariableAction> (@"class TestClass
{
	void Test ()
	{
		Console.WriteLine (<-5 + 3->);
		Console.WriteLine (5 + 3);
		Console.WriteLine (5 + 3);
	}
}", @"class TestClass
{
	void Test ()
	{
		int i = 5 + 3;
		Console.WriteLine (i);
		Console.WriteLine (i);
		Console.WriteLine (i);
	}
}", 1);
		}

		[Test()]
		public void TestReplaceAllImplicit ()
		{
			Test<DeclareLocalVariableAction> (@"class TestClass
{
	void Test ()
	{
		Console.WriteLine (<-5 + 3->);
		Console.WriteLine (5 + 3);
		Console.WriteLine (5 + 3);
	}
}", @"class TestClass
{
	void Test ()
	{
		var i = 5 + 3;
		Console.WriteLine (i);
		Console.WriteLine (i);
		Console.WriteLine (i);
	}
}", 1);
		}

		[Test()]
		public void DeclareLocalExpressionTest ()
		{
			TestRefactoringContext.UseExplict = true;
			Test<DeclareLocalVariableAction> (@"class TestClass
{
	void Test ()
	{
		Console.WriteLine (1 +<- 9 ->+ 5);
	}
}
", @"class TestClass
{
	void Test ()
	{
		int i = 9;
		Console.WriteLine (1 + i + 5);
	}
}
");
		}

		/// <summary>
		/// Bug 693855 - Extracting variable from ELSE IF puts it in the wrong place
		/// </summary>
		[Test()]
		public void TestBug693855 ()
		{
			TestRefactoringContext.UseExplict = true;
			Test<DeclareLocalVariableAction> (@"class TestClass
{
	void Test ()
	{
		string str = ""test"";
		if (str == ""something"") {
			//do A
		} else if (<-str == ""other""->) {
			//do B
		} else {
			//do C
		}
	}
}", 
@"class TestClass
{
	void Test ()
	{
		string str = ""test"";
		bool b = str == ""other"";
		if (str == ""something"") {
			//do A
		} else if (b) {
			//do B
		} else {
			//do C
		}
	}
}");
		}
		
		
		/// <summary>
		/// Bug 693875 - Extract Local on just method name leaves brackets in wrong place
		/// </summary>
		[Test()]
		public void TestBug693875 ()
		{
			TestRefactoringContext.UseExplict = true;
			Test<DeclareLocalVariableAction> (@"class TestClass
{
	void DoStuff() 
	{
		if (<-GetInt->() == 0) {
		}
	}
	
	int GetInt()
	{
		return 1;
	}
}", 
@"class TestClass
{
	void DoStuff() 
	{
		System.Func<int> getInt = GetInt;
		if (getInt() == 0) {
		}
	}
	
	int GetInt()
	{
		return 1;
	}
}");
		}

			
	}
}
