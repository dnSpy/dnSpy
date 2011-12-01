//
// CodeCompletionCSharpTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.CodeCompletion
{
	[TestFixture()]
	public class CodeCompletionCSharpTests : TestBase
	{
		[Test()]
		public void TestUsingDeclaration ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"namespace Test {
	class Class
	{
	}
}

$using $
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "namespace 'Test' not found.");
		}
		
		[Test()]
		public void TestLocalVariableDeclaration ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"class Test
{
	void Test ()
	{
		Test t;
		$t.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestObjectCreationExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"class Test
{
	void Test ()
	{
		$new Test ().$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestCastExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"class Test
{
	void Test ()
	{
		object o;
		$((Test)o).$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestThisReferenceExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"class Test
{
	void Test ()
	{
		$this.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestBaseReferenceExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	public void Test ()
	{
	}
}

class Test2 : Test
{
	void Test2 ()
	{
		$base.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
			Assert.IsNull (provider.Find ("Test2"), "method 'Test2' found but shouldn't.");
		}
		
		[Test()]
		public void TestConditionalExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	void Test ()
	{
		Test a, b;
		$(1 == 1 ? a : b).$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}

		[Test()]
		public void TestIndexerExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	void Test ()
	{
		Test[] a;
		$a[0].$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestInvocationExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	static Test GetTest () {}
	
	void Test ()
	{
		$GetTest ().$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestParenthesizedExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	void Test ()
	{
		$(this).$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestForeachLoopVariable ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Test
{
	void Test ()
	{
		foreach (Test t in new string[] {""hello""})
			$t.$;
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found.");
		}
		
		[Test()]
		public void TestMethodAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class AClass
{
	public AClass TestMethod ()
	{
	}
}

class Test
{
	void Test ()
	{
		AClass a;
		$a.TestMethod().$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod ' not found.");
		}
		
		[Test()]
		public void TestFieldAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class AClass
{
	public AClass TestField;
}

class Test
{
	void Test ()
	{
		AClass a;
		$a.TestField.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestField"), "field 'TestField' not found.");
		}
		
		[Test()]
		public void TestPropertyAccess ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class AClass
{
	public AClass TestProperty { get { return null; } }
}

class Test
{
	void Test ()
	{
		AClass a;
		$a.TestProperty.$
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestProperty"), "property 'TestProperty' not found.");
		}

		[Test()]
		public void TestAsCompletionContext ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class A
{
}

class B
{
}

class C : A
{
}

class Test
{
	public void TestMethod (object test)
	{
		$A a = test as $
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("A"), "class 'A' not found.");
			Assert.IsNotNull (provider.Find ("C"), "class 'C' not found.");
			Assert.IsNull (provider.Find ("B"), "class 'B' found, but shouldn't.");
			Assert.IsNull (provider.Find ("Test"), "class 'Test' found, but shouldn't.");
		}
		
		
			
		[Test()]
		public void TestLocalVariableNameContext ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
public class TestMyLongName
{
	public void Method ()
	{
		$TestMyLongName $
	}
}");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("testMyLongName"), "name 'testMyLongName' not found.");
			Assert.IsNotNull (provider.Find ("myLongName"), "name 'myLongName' not found.");
			Assert.IsNotNull (provider.Find ("longName"), "name 'longName' not found.");
			Assert.IsNotNull (provider.Find ("name"), "name 'name' not found.");
		}

		
	}
}
