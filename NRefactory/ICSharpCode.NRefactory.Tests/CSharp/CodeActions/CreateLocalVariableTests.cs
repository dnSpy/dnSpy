// 
// CreateLocalVariableTests.cs
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
	public class CreateLocalVariableTests : ContextActionTestBase
	{
		[Test()]
		public void TestSimpleMethodCall ()
		{
			string result = RunContextAction (
				new CreateLocalVariableAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		Console.WriteLine ($foo);" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		object foo;" + Environment.NewLine +
				"		Console.WriteLine (foo);" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test()]
		public void TestAssignment ()
		{
			string result = RunContextAction (
				new CreateLocalVariableAction (),
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
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		var foo = 0x10;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test()]
		public void ExplicitTestAssignment ()
		{
			TestRefactoringContext.UseExplict = true;
			string result = RunContextAction (
				new CreateLocalVariableAction (),
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
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		int foo = 0x10;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test()]
		public void TestOutParamCall ()
		{
			string result = RunContextAction (
				new CreateLocalVariableAction (),
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
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		string foo;" + Environment.NewLine +
				"		FooBar(out foo);" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}


		[Test()]
		public void TestReturn ()
		{
			string result = RunContextAction (
				new CreateLocalVariableAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	int Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		return $foo;" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
			
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	int Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		int foo;" + Environment.NewLine +
				"		return foo;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test()]
		public void TestYieldReturn ()
		{
			string result = RunContextAction (
				new CreateLocalVariableAction (),
					"using System;" + Environment.NewLine +
					"using System.Collections.Generic;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	IEnumerable<TestClass> Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		yield return $foo;" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
			
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"using System.Collections.Generic;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	IEnumerable<TestClass> Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		TestClass foo;" + Environment.NewLine +
				"		yield return foo;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}


		[Test()]
		public void TestWrongContext1()
		{
			// May be syntactically possible, but very unlikely.
			TestWrongContext<CreateLocalVariableAction>(
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
		public void TestBool()
		{
			Test<CreateLocalVariableAction>(
@"class TestClass
{
	void Test ()
	{
		object o = !$foo;
	}
}", 
@"class TestClass
{
	void Test ()
	{
		bool foo;
		object o = !foo;
	}
}");
		}

	}
}