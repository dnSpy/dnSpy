// 
// CreatePropertyTests.cs
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
	public class CreatePropertyTests : ContextActionTestBase
	{
		[Test()]
		public void TestSimpleMethodCall ()
		{
			string result = RunContextAction (
				new CreatePropertyAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		Console.WriteLine ($Foo);" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	object Foo {" + Environment.NewLine +
				"		get;" + Environment.NewLine +
				"		set;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		Console.WriteLine (Foo);" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test()]
		public void TestAssignment ()
		{
			string result = RunContextAction (
				new CreatePropertyAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		$Foo = 0x10;" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);

			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	int Foo {" + Environment.NewLine +
				"		get;" + Environment.NewLine +
				"		set;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		Foo = 0x10;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test()]
		public void TestOutParamCall ()
		{
			string result = RunContextAction (
				new CreatePropertyAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void FooBar(out string par) {}" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		FooBar(out $Foo);" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
			
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	void FooBar(out string par) {}" + Environment.NewLine +
				"	string Foo {" + Environment.NewLine +
				"		get;" + Environment.NewLine +
				"		set;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		FooBar(out Foo);" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test()]
		public void TestStaticProperty ()
		{
			string result = RunContextAction (
				new CreatePropertyAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	static void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		$Foo = 0x10;" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);

			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	static int Foo {" + Environment.NewLine +
				"		get;" + Environment.NewLine +
				"		set;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"	static void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		Foo = 0x10;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		public void TestCreateProperty (string input, string output)
		{
			string result = RunContextAction (new CreatePropertyAction (), CreateMethodDeclarationTests.HomogenizeEol (input));
			bool passed = result == output;
			if (!passed) {
				Console.WriteLine ("-----------Expected:");
				Console.WriteLine (output);
				Console.WriteLine ("-----------Got:");
				Console.WriteLine (result);
			}
			Assert.AreEqual (CreateMethodDeclarationTests.HomogenizeEol (output), result);
		}

		[Test()]
		public void TestExternProperty ()
		{
			TestCreateProperty (
@"
interface FooBar
{
}

class TestClass
{
	void TestMethod ()
	{
		FooBar fb;
		fb.$NonExistantProperty = 5;
	}
}
", @"
interface FooBar
{
	int NonExistantProperty {
		get;
		set;
	}
}

class TestClass
{
	void TestMethod ()
	{
		FooBar fb;
		fb.NonExistantProperty = 5;
	}
}
");
		}

		[Test()]
		public void TestWrongContext1 ()
		{
			// May be syntactically possible, but very unlikely.
			TestWrongContext<CreatePropertyAction> (
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		$Foo();" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
		}

		[Test()]
		public void TestStaticClassProperty ()
		{
			// Not 100% correct input code, but should work in that case as well.
			Test<CreatePropertyAction> (@"static class TestClass
{
	public TestClass ()
	{
		$Foo = 5;
	}
}", @"static class TestClass
{
	static int Foo {
		get;
		set;
	}
	public TestClass ()
	{
		Foo = 5;
	}
}");
		}

		[Test()]
		public void CreateStaticPropertyInCurrentType()
		{
			Test<CreatePropertyAction> (@"class TestClass
{
	public TestClass ()
	{
		TestClass.$Foo = 5;
	}
}", @"class TestClass
{
	static int Foo {
		get;
		set;
	}
	public TestClass ()
	{
		TestClass.Foo = 5;
	}
}");
		}

		[Test]
		public void TestEnumCase()
		{
			TestWrongContext<CreatePropertyAction>(@"
enum AEnum { A }
class Foo
{
	public void Test ()
	{
		AEnum e = AEnum.B$ar;
	}
}
");
		}
	}
}