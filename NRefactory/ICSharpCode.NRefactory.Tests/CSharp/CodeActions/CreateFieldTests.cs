// 
// CreateFieldTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
	public class CreateFieldTests : ContextActionTestBase
	{
	[Test()]
		public void TestWrongContext2 ()
		{
			TestWrongContext<CreateFieldAction> (
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		Console.WriteLine ($Foo());" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
		}

		[Test()]
		public void TestWrongContext3 ()
		{
			// May be syntactically possible, but very unlikely.
			TestWrongContext<CreateFieldAction> (
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		$foo();" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
		}

		[Test()]
		public void TestSimpleMethodCall ()
		{
			string result = RunContextAction (
				new CreateFieldAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		Console.WriteLine ($foo);" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
			Console.WriteLine (result);
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	object foo;" + Environment.NewLine +
				"" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		Console.WriteLine (foo);" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test()]
		public void TestAssignment ()
		{
			string result = RunContextAction (
				new CreateFieldAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		$foo = 0x10;" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);

			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	int foo;" + Environment.NewLine +
				"" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		foo = 0x10;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test()]
		public void TestOutParamCall ()
		{
			string result = RunContextAction (
				new CreateFieldAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void FooBar(out string par) {}" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		FooBar(out $foo);" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
			
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	void FooBar(out string par) {}" + Environment.NewLine +
				"	string foo;" + Environment.NewLine +
				"" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		FooBar(out foo);" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test()]
		public void TestStaticClassField ()
		{
			// Not 100% correct input code, but should work in that case as well.
			Test<CreateFieldAction> (@"static class TestClass
{
	public TestClass ()
	{
		$foo = 5;
	}
}", @"static class TestClass
{
	static int foo;

	public TestClass ()
	{
		foo = 5;
	}
}");
		}

		

	}
}