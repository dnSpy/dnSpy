// 
// ImplementInterfaceTests.cs
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
	public class ImplementInterfaceTests : ContextActionTestBase
	{
		[Test()]
		public void TestSimpleInterface()
		{
			Test<ImplementInterfaceAction>(@"using System;
class Foo : $IDisposable
{
}
", @"using System;
class Foo : IDisposable
{
	#region IDisposable implementation
	public void Dispose ()
	{
		throw new NotImplementedException ();
	}
	#endregion
}
");
		}


		/// <summary>
		/// Bug 663842 - Interface implementation does not include constraints
		/// </summary>
		[Test()]
		public void TestBug663842()
		{
			Test<ImplementInterfaceAction>(@"using System;
interface ITest {
	void MyMethod1<T> (T t) where T : new ();
	void MyMethod2<T> (T t) where T : class;
	void MyMethod3<T> (T t) where T : struct;
	void MyMethod4<T> (T t) where T : IDisposable, IServiceProvider;
}

class Foo : $ITest
{
}
", @"using System;
interface ITest {
	void MyMethod1<T> (T t) where T : new ();
	void MyMethod2<T> (T t) where T : class;
	void MyMethod3<T> (T t) where T : struct;
	void MyMethod4<T> (T t) where T : IDisposable, IServiceProvider;
}

class Foo : ITest
{
	#region ITest implementation
	public void MyMethod1<T> (T t) where T : new()
	{
		throw new NotImplementedException ();
	}
	public void MyMethod2<T> (T t) where T : class
	{
		throw new NotImplementedException ();
	}
	public void MyMethod3<T> (T t) where T : struct
	{
		throw new NotImplementedException ();
	}
	public void MyMethod4<T> (T t) where T : IDisposable, IServiceProvider
	{
		throw new NotImplementedException ();
	}
	#endregion
}
");
		}

		/// <summary>
		/// Bug 683007 - "Refactor/Implement implicit" creates explicit implementations of methods with same names
		/// </summary>
		[Test()]
		public void TestBug683007()
		{
			Test<ImplementInterfaceAction>(@"interface ITest {
	void M1();
	void M1(int x);
}

class Foo : $ITest
{
}", @"interface ITest {
	void M1();
	void M1(int x);
}

class Foo : ITest
{
	#region ITest implementation
	public void M1 ()
	{
		throw new System.NotImplementedException ();
	}
	public void M1 (int x)
	{
		throw new System.NotImplementedException ();
	}
	#endregion
}");
		}
		
		/// <summary>
		/// Bug 243 - Implement implicit interface doesn't handle overloads correctly. 
		/// </summary>
		[Test()]
		public void TestBug243()
		{
			Test<ImplementInterfaceAction>(@"interface ITest {
	void Inc (int n);
	void Inc (string message);
}

class Foo : $ITest
{
}
", @"interface ITest {
	void Inc (int n);
	void Inc (string message);
}

class Foo : ITest
{
	#region ITest implementation
	public void Inc (int n)
	{
		throw new System.NotImplementedException ();
	}
	public void Inc (string message)
	{
		throw new System.NotImplementedException ();
	}
	#endregion
}
");
		}
		
		
		/// <summary>
		/// Bug 2074 - [Regression] Implement Interface implicitly does not check the methods already exist 
		/// </summary>
		[Test()]
		public void TestBug2074()
		{
			Test<ImplementInterfaceAction>(@"interface ITest {
	void Method1 ();
	void Method2 ();
}

class Foo : $ITest
{
	public void Method2 () {}
}", @"interface ITest {
	void Method1 ();
	void Method2 ();
}

class Foo : ITest
{
	#region ITest implementation
	public void Method1 ()
	{
		throw new System.NotImplementedException ();
	}
	#endregion
	public void Method2 () {}
}");
		}
		
		/// <summary>
		/// Bug 3365 - MD cannot implement IEnumerable interface correctly  - MD cannot implement IEnumerable interface correctly 
		/// </summary>
		[Test()]
		public void TestBug3365()
		{
			Test<ImplementInterfaceAction>(@"using System;
using System.Collections;

public interface IA
{
	bool GetEnumerator ();
}

public interface ITest : IA, IEnumerable
{
}

class Foo : $ITest
{
}", @"using System;
using System.Collections;

public interface IA
{
	bool GetEnumerator ();
}

public interface ITest : IA, IEnumerable
{
}

class Foo : ITest
{
	#region IEnumerable implementation
	public IEnumerator GetEnumerator ()
	{
		throw new NotImplementedException ();
	}
	#endregion
	#region IA implementation
	bool IA.GetEnumerator ()
	{
		throw new NotImplementedException ();
	}
	#endregion
}");
		}


		/// <summary>
		/// Bug 4818 - Implement implicit does not handle 'params' types 
		/// </summary>
		[Test()]
		public void TestBug4818()
		{
			Test<ImplementInterfaceAction>(@"using System;
interface ITest {
	void OnScenesAdded (params ITest[] scenes);
}

class Foo : $ITest
{
}
", @"using System;
interface ITest {
	void OnScenesAdded (params ITest[] scenes);
}

class Foo : ITest
{
	#region ITest implementation
	public void OnScenesAdded (params ITest[] scenes)
	{
		throw new NotImplementedException ();
	}
	#endregion
}
");

			TestWrongContext<ImplementInterfaceAction>(@"using System;
interface ITest {
	void OnScenesAdded (params ITest[] scenes);
}

class Foo : $ITest
{
	#region ITest implementation
	public void OnScenesAdded (params ITest[] scenes)
	{
		throw new NotImplementedException ();
	}
	#endregion
}
");
		}
	}
}

