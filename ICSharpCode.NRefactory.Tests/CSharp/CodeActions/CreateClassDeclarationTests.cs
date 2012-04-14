// 
// CreateClassDeclarationTests.cs
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
	public class CreateClassDeclarationTests : ContextActionTestBase
	{
		[Test()]
		public void TestCreateClass ()
		{
			Test<CreateClassDeclarationAction> (
@"
class TestClass
{
	void TestMethod ()
	{
		$new Foo (0, ""Hello"");
	}
}
", @"
class Foo
{
	public Foo (int i, string hello)
	{
		throw new System.NotImplementedException ();
	}
}
class TestClass
{
	void TestMethod ()
	{
		new Foo (0, ""Hello"");
	}
}
");
		}

		[Test()]
		public void TestNestedCreateClass()
		{
			Test<CreateClassDeclarationAction>(
@"
class TestClass
{
	void TestMethod ()
	{
		$new Foo (0);
	}
}
", @"
class TestClass
{
	class Foo
	{
		public Foo (int i)
		{
			throw new System.NotImplementedException ();
		}
	}
	void TestMethod ()
	{
		new Foo (0);
	}
}
", 1);
		}

		[Test()]
		public void TestEmptyConstructor ()
		{
			Test<CreateClassDeclarationAction> (
@"
class TestClass
{
	void TestMethod ()
	{
		$new Foo ();
	}
}
", @"
class Foo
{
}
class TestClass
{
	void TestMethod ()
	{
		new Foo ();
	}
}
");
		}

		[Test()]
		public void TestCreatePublicEventArgs ()
		{
			Test<CreateClassDeclarationAction> (
@"
class TestClass
{
	public event EventHandler<$MyEventArgs> evt;
}
", @"
public class MyEventArgs : System.EventArgs
{
}
class TestClass
{
	public event EventHandler<MyEventArgs> evt;
}
");
		}

		[Test()]
		public void TestCreateInternalEventArgs ()
		{
			Test<CreateClassDeclarationAction> (
@"
class TestClass
{
	internal event EventHandler<$MyEventArgs> evt;
}
", @"
class MyEventArgs : System.EventArgs
{
}
class TestClass
{
	internal event EventHandler<MyEventArgs> evt;
}
");
		}

		[Test()]
		public void TestCreateAttribute ()
		{
			Test<CreateClassDeclarationAction> (
@"
[$MyAttribute]
class TestClass
{
}
", @"
class MyAttribute : System.Attribute
{
}
[MyAttribute]
class TestClass
{
}
");
		}

		[Test()]
		public void TestCreateAttributeCase2 ()
		{
			Test<CreateClassDeclarationAction> (
@"
[$My]
class TestClass
{
}
", @"
class MyAttribute : System.Attribute
{
}
[My]
class TestClass
{
}
");
		}

		[Test()]
		public void TestCreateException ()
		{
			Test<CreateClassDeclarationAction> (
@"
class TestClass
{
	void TestMethod ()
	{
		throw $new MyException ();
	}
}
", @"
class MyException : System.Exception
{
}
class TestClass
{
	void TestMethod ()
	{
		throw new MyException ();
	}
}
");
		}

		[Test()]
		public void TestNotShowInEventTypes()
		{
			TestWrongContext<CreateClassDeclarationAction>(
@"
class TestClass
{
	event $MyEventHandler evt;
}
");
		}

		[Test()]
		public void TestCreateClassImplementingInterface()
		{
			Test<CreateClassDeclarationAction>(
@"
class TestClass
{
	void TestMethod (System.IDisposable d)
	{
		TestMethod ($new Foo ());
	}
}
", @"
class Foo : System.IDisposable
{
	public void Dispose ()
	{
		throw new System.NotImplementedException ();
	}
}
class TestClass
{
	void TestMethod (System.IDisposable d)
	{
		TestMethod (new Foo ());
	}
}
");
		}

		[Test()]
		public void TestCreateClassExtendingAbstractClass()
		{
			Test<CreateClassDeclarationAction>(
@"
class TestClass
{
	abstract class FooBar { protected abstract void SomeFoo (); public abstract int MoreFoo { get; } }
	void TestMethod (FooBar d)
	{
		TestMethod ($new Foo ());
	}
}
", @"
class Foo : FooBar
{
	public override int MoreFoo {
		get;
	}
	protected override void SomeFoo ()
	{
		throw new System.NotImplementedException ();
	}
}
class TestClass
{
	abstract class FooBar { protected abstract void SomeFoo (); public abstract int MoreFoo { get; } }
	void TestMethod (FooBar d)
	{
		TestMethod (new Foo ());
	}
}
");
		}

	}
}
