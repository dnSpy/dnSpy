// 
// CreateIndexerTests.cs
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
	public class CreateIndexerTests : ContextActionTestBase
	{
		public void TestCreateIndexer (string input, string output)
		{
			string result = RunContextAction (new CreateIndexerAction (), CreateMethodDeclarationTests.HomogenizeEol (input));
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
		public void TestIndexer ()
		{
			TestCreateIndexer (
@"
class TestClass
{
	void TestMethod ()
	{
		$this[0] = 2;
	}
}
", @"
class TestClass
{
	int this [int i] {
		get {
			throw new System.NotImplementedException ();
		}
		set {
			throw new System.NotImplementedException ();
		}
	}
	void TestMethod ()
	{
		this[0] = 2;
	}
}
");
		}
		[Test()]
		public void TestInterfaceIndexer ()
		{
			TestCreateIndexer (
@"
interface FooBar
{
}

class TestClass
{
	void TestMethod ()
	{
		FooBar fb;
		$fb[0] = 2;
	}
}
", @"
interface FooBar
{
	int this [int i] {
		get;
		set;
	}
}

class TestClass
{
	void TestMethod ()
	{
		FooBar fb;
		fb[0] = 2;
	}
}
");
		}

		[Test()]
		public void TestExternIndexer ()
		{
			TestCreateIndexer (
@"
class FooBar
{
}

class TestClass
{
	void TestMethod ()
	{
		FooBar fb;
		$fb[0] = 2;
	}
}
", @"
class FooBar
{
	public int this [int i] {
		get {
			throw new System.NotImplementedException ();
		}
		set {
			throw new System.NotImplementedException ();
		}
	}
}

class TestClass
{
	void TestMethod ()
	{
		FooBar fb;
		fb[0] = 2;
	}
}
");
		}

		[Test()]
		public void TestindexerInFrameworkClass ()
		{
			TestWrongContext<CreateIndexerAction> (
@"class TestClass
{
	void TestMethod ()
	{
		$new System.Buffer ()[0] = 2;
	}
}
");
		}

		[Test]
		public void TestEnumCase()
		{
			TestWrongContext<CreateIndexerAction>(@"
enum AEnum { A }
class Foo
{
	public void Test ()
	{
		AEnum e;
		$e[0] = 2;
	}
}
");
		}
	}
}

