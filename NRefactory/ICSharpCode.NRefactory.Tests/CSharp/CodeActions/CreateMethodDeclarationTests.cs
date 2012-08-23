// 
// CreateMethodDeclarationTests.cs
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
using System.Text;

namespace ICSharpCode.NRefactory.CSharp.CodeActions
{
	[TestFixture]
	public class CreateMethodDeclarationTests : ContextActionTestBase
	{
		[Test()]
		public void TestPrivateSimpleCreateMethod ()
		{
			Test<CreateMethodDeclarationAction> (@"class TestClass
{
	int member = 5;
	string Test { get; set; }

	void TestMethod ()
	{
		$NonExistantMethod (member, Test, 5);
	}
}", @"class TestClass
{
	int member = 5;
	string Test { get; set; }

	void NonExistantMethod (int member, string test, int i)
	{
		throw new System.NotImplementedException ();
	}
	void TestMethod ()
	{
		NonExistantMethod (member, Test, 5);
	}
}");
		}

		[Test()]
		public void TestStaticSimpleCreateMethod ()
		{
			Test<CreateMethodDeclarationAction> (@"class TestClass
{
	public static void TestMethod ()
	{
		int testLocalVar;
		$NonExistantMethod (testLocalVar);
	}
}", @"class TestClass
{
	static void NonExistantMethod (int testLocalVar)
	{
		throw new System.NotImplementedException ();
	}
	public static void TestMethod ()
	{
		int testLocalVar;
		NonExistantMethod (testLocalVar);
	}
}");
		}
		
		[Test()]
		public void TestGuessAssignmentReturnType ()
		{
			Test<CreateMethodDeclarationAction> (@"class TestClass
{
	static void TestMethod ()
	{
		int testLocalVar = $NonExistantMethod ();
	}
}", @"class TestClass
{
	static int NonExistantMethod ()
	{
		throw new System.NotImplementedException ();
	}
	static void TestMethod ()
	{
		int testLocalVar = NonExistantMethod ();
	}
}");
		}

		[Test()]
		public void TestGuessAssignmentReturnTypeCase2 ()
		{
			Test<CreateMethodDeclarationAction> (@"class TestClass
{
	static void TestMethod ()
	{
		int testLocalVar;
		testLocalVar = $NonExistantMethod ();
	}
}", @"class TestClass
{
	static int NonExistantMethod ()
	{
		throw new System.NotImplementedException ();
	}
	static void TestMethod ()
	{
		int testLocalVar;
		testLocalVar = NonExistantMethod ();
	}
}");
		}

		[Test()]
		public void TestGuessAssignmentReturnTypeCase3 ()
		{
			Test<CreateMethodDeclarationAction> (@"class TestClass
{
	static void TestMethod ()
	{
		var testLocalVar = (string)$NonExistantMethod ();
	}
}", @"class TestClass
{
	static string NonExistantMethod ()
	{
		throw new System.NotImplementedException ();
	}
	static void TestMethod ()
	{
		var testLocalVar = (string)NonExistantMethod ();
	}
}");
		}


		[Test()]
		public void TestGuessParameterType ()
		{
			Test<CreateMethodDeclarationAction> (@"class TestClass
{
	void TestMethod ()
	{
		Test ($NonExistantMethod ());
	}
	void Test (int a) {}

}", @"class TestClass
{
	int NonExistantMethod ()
	{
		throw new System.NotImplementedException ();
	}
	void TestMethod ()
	{
		Test (NonExistantMethod ());
	}
	void Test (int a) {}

}");
		}


		[Test()]
		public void TestCreateDelegateDeclarationIdentifierCase ()
		{
			Test<CreateMethodDeclarationAction> (@"class TestClass
{
	public event MyDelegate MyEvent;

	void TestMethod ()
	{
		MyEvent += $NonExistantMethod;
	}
}

public delegate string MyDelegate (int a, object b);
", @"class TestClass
{
	public event MyDelegate MyEvent;

	string NonExistantMethod (int a, object b)
	{
		throw new System.NotImplementedException ();
	}
	void TestMethod ()
	{
		MyEvent += NonExistantMethod;
	}
}

public delegate string MyDelegate (int a, object b);
");
		}

		[Test()]
		public void TestCreateDelegateDeclarationMemberReferenceCase ()
		{
			Test<CreateMethodDeclarationAction> (@"class TestClass
{
	public event MyDelegate MyEvent;

	void TestMethod ()
	{
		MyEvent += $this.NonExistantMethod;
	}
}

public delegate string MyDelegate (int a, object b);
", @"class TestClass
{
	public event MyDelegate MyEvent;

	string NonExistantMethod (int a, object b)
	{
		throw new System.NotImplementedException ();
	}
	void TestMethod ()
	{
		MyEvent += this.NonExistantMethod;
	}
}

public delegate string MyDelegate (int a, object b);
");
		}
		
		[Test()]
		public void TestCreateDelegateDeclarationInOtherClassMemberReferenceCase ()
		{
			Test<CreateMethodDeclarationAction> (@"class Foo {
}

class TestClass
{
	public event MyDelegate MyEvent;

	void TestMethod ()
	{
		MyEvent += $new Foo ().NonExistantMethod;
	}
}

public delegate string MyDelegate (int a, object b);
", @"class Foo {
	public string NonExistantMethod (int a, object b)
	{
		throw new System.NotImplementedException ();
	}
}

class TestClass
{
	public event MyDelegate MyEvent;

	void TestMethod ()
	{
		MyEvent += new Foo ().NonExistantMethod;
	}
}

public delegate string MyDelegate (int a, object b);
");
		}

		[Test()]
		public void TestRefOutCreateMethod ()
		{
			Test<CreateMethodDeclarationAction> (@"class TestClass
{
	void TestMethod ()
	{
		int a, b;
		$NonExistantMethod (ref a, out b);
	}
}", @"class TestClass
{
	void NonExistantMethod (ref int a, out int b)
	{
		throw new System.NotImplementedException ();
	}
	void TestMethod ()
	{
		int a, b;
		NonExistantMethod (ref a, out b);
	}
}");
		}
		
		[Test()]
		public void TestExternMethod ()
		{
			Test<CreateMethodDeclarationAction> (
@"
class FooBar
{
}

class TestClass
{
	void TestMethod ()
	{
		var fb = new FooBar ();
		fb.$NonExistantMethod ();
	}
}
", @"
class FooBar
{
	public void NonExistantMethod ()
	{
		throw new System.NotImplementedException ();
	}
}

class TestClass
{
	void TestMethod ()
	{
		var fb = new FooBar ();
		fb.NonExistantMethod ();
	}
}
");
		}
		
		[Test()]
		public void TestCreateInterfaceMethod ()
		{
			Test<CreateMethodDeclarationAction> (
@"
interface FooBar
{
}

class TestClass
{
	void TestMethod ()
	{
		FooBar fb;
		fb.$NonExistantMethod ();
	}
}
", @"
interface FooBar
{
	void NonExistantMethod ();
}

class TestClass
{
	void TestMethod ()
	{
		FooBar fb;
		fb.NonExistantMethod ();
	}
}
");
		}
		
		[Test()]
		public void TestCreateInStaticClassMethod ()
		{
			Test<CreateMethodDeclarationAction> (
@"
static class FooBar
{
}

class TestClass
{
	void TestMethod ()
	{
		FooBar.$NonExistantMethod ();
	}
}
", @"
static class FooBar
{
	public static void NonExistantMethod ()
	{
		throw new System.NotImplementedException ();
	}
}

class TestClass
{
	void TestMethod ()
	{
		FooBar.NonExistantMethod ();
	}
}
");
		}
		

		
		/// <summary>
		/// Bug 677522 - "Create Method" creates at wrong indent level
		/// </summary>
		[Test()]
		public void TestBug677522 ()
		{
			Test<CreateMethodDeclarationAction> (
@"namespace Test {
	class TestClass
	{
		void TestMethod ()
		{
			$NonExistantMethod ();
		}
	}
}
", @"namespace Test {
	class TestClass
	{
		void NonExistantMethod ()
		{
			throw new System.NotImplementedException ();
		}
		void TestMethod ()
		{
			NonExistantMethod ();
		}
	}
}
");
		}
		
		/// <summary>
		/// Bug 677527 - "Create Method" uses fully qualified namespace when "using" statement exists
		/// </summary>
		[Test()]
		public void TestBug677527 ()
		{
			Test<CreateMethodDeclarationAction> (
@"using System.Text;

namespace Test {
	class TestClass
	{
		void TestMethod ()
		{
			StringBuilder sb = new StringBuilder ();
			$NonExistantMethod (sb);
		}
	}
}
", @"using System.Text;

namespace Test {
	class TestClass
	{
		void NonExistantMethod (StringBuilder sb)
		{
			throw new System.NotImplementedException ();
		}
		void TestMethod ()
		{
			StringBuilder sb = new StringBuilder ();
			NonExistantMethod (sb);
		}
	}
}
");
		}
		
		
		/// <summary>
		/// Bug 693949 - Create method uses the wrong type for param
		/// </summary>
		[Ignore("TODO")]
		[Test()]
		public void TestBug693949 ()
		{
			// the c# code isn't 100% correct since test isn't accessible in Main (can't call non static method from static member)
			Test<CreateMethodDeclarationAction> (
@"using System.Text;

namespace Test {
	class TestClass
	{
		string test(string a)
		{
		}
	
		public static void Main(string[] args)
		{
			Type a = $M(test(""a""));
		}
	}
}
", @"using System.Text;

namespace Test {
	class TestClass
	{
		string test(string a)
		{
		}
	
		static Type M (string par1)
		{
			throw new System.NotImplementedException ();
		}

		public static void Main(string[] args)
		{
			Type a = $M(test(""a""));
		}
	}
}
");
		}
		
		/// <summary>
		/// Bug 469 - CreateMethod created a method incorrectly
		/// </summary>
		[Test()]
		public void TestBug469 ()
		{
			Test<CreateMethodDeclarationAction> (
@"class Test
{
	public override string ToString ()
	{
		$BeginDownloadingImage (this);
	}
}
", @"class Test
{
	void BeginDownloadingImage (Test test)
	{
		throw new System.NotImplementedException ();
	}
	public override string ToString ()
	{
		BeginDownloadingImage (this);
	}
}
");
		}
		
		[Test()]
		public void TestTestGuessReturnReturnType ()
		{
			Test<CreateMethodDeclarationAction> (
@"class Test
{
	public override string ToString ()
	{
		return $BeginDownloadingImage (this);
	}
}
", @"class Test
{
	string BeginDownloadingImage (Test test)
	{
		throw new System.NotImplementedException ();
	}
	public override string ToString ()
	{
		return BeginDownloadingImage (this);
	}
}
");
		}

		[Test()]
		public void TestStringParameterNameGuessing ()
		{
			Test<CreateMethodDeclarationAction> (@"class TestClass
{
	static void TestMethod ()
	{
		$NonExistantMethod (""Hello World!"");
	}
}", @"class TestClass
{
	static void NonExistantMethod (string helloWorld)
	{
		throw new System.NotImplementedException ();
	}
	static void TestMethod ()
	{
		NonExistantMethod (""Hello World!"");
	}
}");
		}

		[Test()]
		public void TestMethodInFrameworkClass ()
		{
			TestWrongContext<CreateMethodDeclarationAction> (
@"class TestClass
{
	void TestMethod ()
	{
		$System.Console.ImprovedWriteLine (""Think of it"");
	}
}
");
		}

		[Test()]
		public void TestCreateMethodOutOfDelegateCreation ()
		{
			Test<CreateMethodDeclarationAction> (
@"using System;
class Test
{
	public void ATest ()
	{
		new System.EventHandler<System.EventArgs>($BeginDownloadingImage);
	}
}
", @"using System;
class Test
{
	void BeginDownloadingImage (object sender, EventArgs e)
	{
		throw new NotImplementedException ();
	}
	public void ATest ()
	{
		new System.EventHandler<System.EventArgs>(BeginDownloadingImage);
	}
}
");
		}
		
		[Test()]
		public void TestStaticClassMethod ()
		{
			// Not 100% correct input code, but should work in that case as well.
			Test<CreateMethodDeclarationAction> (@"static class TestClass
{
	public TestClass ()
	{
		$Foo (5);
	}
}", @"static class TestClass
{
	static void Foo (int i)
	{
		throw new System.NotImplementedException ();
	}
	public TestClass ()
	{
		Foo (5);
	}
}");
		}

		[Test()]
		public void TestCreateFromIdentifierNestedInMethodCall ()
		{
			// Not 100% correct input code, but should work in that case as well.
			Test<CreateMethodDeclarationAction> (@"namespace System {
	class TestClass
	{
		public void FooBar (object test)
		{
			FooBar (new EventHandler ($Foo));
		}
	}
}", @"namespace System {
	class TestClass
	{
		void Foo (object sender, EventArgs e)
		{
			throw new NotImplementedException ();
		}
		public void FooBar (object test)
		{
			FooBar (new EventHandler (Foo));
		}
	}
}");
		}

		[Test]
		public void TestEnumCase()
		{
			TestWrongContext<CreateMethodDeclarationAction>(@"
enum AEnum { A }
class Foo
{
	public void Test ()
	{
		AEnum e = AEnum.B$ar ();
	}
}
");
		}


	}
}

