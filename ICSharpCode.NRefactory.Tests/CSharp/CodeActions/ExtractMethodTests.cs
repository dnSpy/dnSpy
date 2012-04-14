// 
// ExtractMethodTests.cs
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
using NUnit.Framework;
using ICSharpCode.NRefactory.CSharp.Refactoring.ExtractMethod;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class ExtractMethodTests : ContextActionTestBase
	{

		[Ignore("FIXME!!")]
		[Test()]
		public void ExtractMethodResultStatementTest()
		{
			Test<ExtractMethodAction>(@"class TestClass
{
	int member = 5;
	void TestMethod ()
	{
		int i = 5;
		<-i = member + 1;->
		Console.WriteLine (i);
	}
}
", @"class TestClass
{
	int member = 5;
	void NewMethod (ref int i)
	{
		i = member + 1;
	}
	void TestMethod ()
	{
		int i = 5;
		NewMethod (ref i);
		Console.WriteLine (i);
	}
}
");
		}
		
		[Test()]
		public void ExtractMethodResultExpressionTest()
		{
			Test<ExtractMethodAction>(@"class TestClass
{
	int member = 5;
	void TestMethod ()
	{
		int i = <-member + 1->;
		Console.WriteLine (i);
	}
}
", @"class TestClass
{
	int member = 5;
	int NewMethod ()
	{
		return member + 1;
	}
	void TestMethod ()
	{
		int i = NewMethod ();
		Console.WriteLine (i);
	}
}
");
		}
		
		[Ignore("FIXME!!")]
		[Test()]
		public void ExtractMethodStaticResultStatementTest()
		{
			Test<ExtractMethodAction>(@"class TestClass
{
	void TestMethod ()
	{
		int i = 5;
		<-i = i + 1;->
		Console.WriteLine (i);
	}
}
", @"class TestClass
{
	static void NewMethod (ref int i)
	{
		i = i + 1;
	}
	void TestMethod ()
	{
		int i = 5;
		NewMethod (ref i);
		Console.WriteLine (i);
	}
}
");
		}
		
		[Test()]
		public void ExtractMethodStaticResultExpressionTest()
		{
			Test<ExtractMethodAction>(@"class TestClass
{
	void TestMethod ()
	{
		int i = <-5 + 1->;
		Console.WriteLine (i);
	}
}
", @"class TestClass
{
	static int NewMethod ()
	{
		return 5 + 1;
	}
	void TestMethod ()
	{
		int i = NewMethod ();
		Console.WriteLine (i);
	}
}
");
		}
		
		[Ignore("FIXME!!")]
		[Test()]
		public void ExtractMethodMultiVariableTest()
		{
			Test<ExtractMethodAction>(@"class TestClass
{
	int member;
	void TestMethod ()
	{
		int i = 5, j = 10, k;
		<-j = i + j;
		k = j + member;->
		Console.WriteLine (k + j);
	}
}
", @"class TestClass
{
	int member;
	void NewMethod (ref int j, int i, out int k)
	{
		j = i + j;
		k = j + member;
	}
	void TestMethod ()
	{
		int i = 5, j = 10, k;
		NewMethod (i, ref j, out k);
		Console.WriteLine (k + j);
	}
}
");
		}
		
		/// <summary>
		/// Bug 607990 - "Extract Method" refactoring sometimes tries to pass in unnecessary parameter depending on selection
		/// </summary>
		[Test()]
		public void TestBug607990()
		{
			Test<ExtractMethodAction>(@"class TestClass
{
	void TestMethod ()
	{
		<-Object obj1 = new Object();
		obj1.ToString();->
	}
}
", @"class TestClass
{
	static void NewMethod ()
	{
		Object obj1 = new Object ();
		obj1.ToString ();
	}
	void TestMethod ()
	{
		NewMethod ();
	}
}
");
		}
		
		
		/// <summary>
		/// Bug 616193 - Extract method passes param with does not exists any more in main method
		/// </summary>
		[Ignore("FIXME!!")]
		[Test()]
		public void TestBug616193()
		{
			Test<ExtractMethodAction>(@"class TestClass
{
	void TestMethod ()
	{
		string ret;
		string x;
		int y;
		<-string z = ret + y;
		ret = x + z;->
	}
}
", @"class TestClass
{
	static void NewMethod (out string ret, string x, int y)
	{
		string z = ret + y;
		ret = x + z;
	}
	void TestMethod ()
	{
		string ret;
		string x;
		int y;
		NewMethod (out ret, x, y);
	}
}
");
		}
		
		/// <summary>
		/// Bug 616199 - Extract method forgets to return a local var which is used in main method
		/// </summary>
		[Ignore("FIXME!!")]
		[Test()]
		public void TestBug616199()
		{
			Test<ExtractMethodAction>(@"class TestClass
{
	void TestMethod ()
	{
		<-string z = ""test"" + ""x"";->
		string ret = ""test1"" + z;
	}
}
", @"class TestClass
{
	static string NewMethod ()
	{
		string z = ""test"" + ""x"";
		return z;
	}
	void TestMethod ()
	{
		string z = NewMethod ();
		string ret = ""test1"" + z;
	}
}
");
		}
		
		/// <summary>
		/// Bug 666271 - "Extract Method" on single line adds two semi-colons in method, none in replaced text
		/// </summary>
		[Test()]
		public void TestBug666271()
		{
			Test<ExtractMethodAction>(@"class TestClass
{
	void TestMethod ()
	{
		<-TestMethod ();->
	}
}
", @"class TestClass
{
	void NewMethod ()
	{
		TestMethod ();
	}
	void TestMethod ()
	{
		NewMethod ();
	}
}
");
		}
		
		
		/// <summary>
		/// Bug 693944 - Extracted method returns void instead of the correct type
		/// </summary>
		[Test()]
		public void TestBug693944()
		{
			Test<ExtractMethodAction>(@"class TestClass
{
	void TestMethod ()
	{
		TestMethod (<-""Hello""->);
	}
}
", @"class TestClass
{
	static string NewMethod ()
	{
		return ""Hello"";
	}
	void TestMethod ()
	{
		TestMethod (NewMethod ());
	}
}
");
		}
		
		
		[Ignore("FIXME!!")]
		[Test()]
		public void ExtractMethodMultiVariableWithLocalReturnVariableTest()
		{
			Test<ExtractMethodAction>(@"class TestClass
{
	int member;
	void TestMethod ()
	{
		int i = 5, j = 10, k;
		<-int test;
		j = i + j;
		k = j + member;
		test = i + j + k;->
		Console.WriteLine (test);
	}
}
", @"class TestClass
{
	int member;
	void NewMethod (ref int j, int i, out int k, out int test)
	{
		j = i + j;
		k = j + member;
		test = i + j + k;
	}
	void TestMethod ()
	{
		int i = 5, j = 10, k;
		int test;
		NewMethod (i, ref j, out k, out test);
		Console.WriteLine (test);
	}
}
");
		}
	}
}

