// 
// CreateFieldTests.cs
//  
// Author:
//       Nieve Goor
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
	public class ExtractFieldTests : ContextActionTestBase
	{
		[Test]
		public void TestWrongContext1 ()
		{
			TestWrongContext<ExtractFieldAction> (
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		$foo = 2;" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
		}
		
		[Test]
		public void TestWrongContext2 ()
		{
			TestWrongContext<ExtractFieldAction> (
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	int foo;" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		$foo = 2;" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
		}

		[Test]
		public void TestLocalInitializer()
		{
			string result = RunContextAction (
				new ExtractFieldAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		int $foo = 5;" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
			Console.WriteLine (result);
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	int foo;" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		foo = 5;" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}

		[Test]
		public void TestLocalDeclaration()
		{
			string result = RunContextAction (
				new ExtractFieldAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	void Test ()" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		int $foo;" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);
			Console.WriteLine (result);
			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	int foo;" + Environment.NewLine +
				"	void Test ()" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}
		
		[Test]
		public void TestCtorParam ()
		{
			string result = RunContextAction (
				new ExtractFieldAction (),
				"using System;" + Environment.NewLine +
					"class TestClass" + Environment.NewLine +
					"{" + Environment.NewLine +
					"	TestClass (int $foo)" + Environment.NewLine +
					"	{" + Environment.NewLine +
					"		" + Environment.NewLine +
					"	}" + Environment.NewLine +
					"}"
			);

			Assert.AreEqual (
				"using System;" + Environment.NewLine +
				"class TestClass" + Environment.NewLine +
				"{" + Environment.NewLine +
				"	int foo;" + Environment.NewLine +
				"	TestClass (int foo)" + Environment.NewLine +
				"	{" + Environment.NewLine +
				"		this.foo = foo;" + Environment.NewLine +
				"		" + Environment.NewLine +
				"	}" + Environment.NewLine +
				"}", result);
		}
	}
}

