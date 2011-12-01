// 
// ObjectInitializerTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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

namespace ICSharpCode.NRefactory.CSharp.CodeCompletion
{
	public class ObjectInitializerTests : TestBase
	{
		/// <summary>
		/// Bug 487236 - Object initializer completion uses wrong type
		/// </summary>
		[Test()]
		public void TestBug487236 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
public class A
{
	public string Name { get; set; }
}

class MyTest
{
	public void Test ()
	{
		$var x = new A () { $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Name"), "property 'Name' not found.");
		}
		
		/// <summary>
		/// Bug 487236 - Object initializer completion uses wrong type
		/// </summary>
		[Test()]
		public void TestBug487236B ()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
public class A
{
	public string Name { get; set; }
}

class MyTest
{
	public void Test ()
	{
		$A x = new NotExists () { $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNull (provider.Find ("Name"), "property 'Name' found, but shouldn't'.");
		}
		
		[Test()]
		public void TestField ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"
public class A
{
	public int Test;
}

class MyTest
{
	public void Test ()
	{
		$new A () { T$
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("Test"), "field 'Test' not found.");
			});
		}
		
		[Test()]
		public void TestProperty ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"
public class A
{
	public int Test { get; set; }
}

class MyTest
{
	public void Test ()
	{
		$new A () { T$
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("Test"), "property 'Test' not found.");
			});
		}
		
		
		
/// <summary>
		/// Bug 526667 - wrong code completion in object initialisation (new O() {...};)
		/// </summary>
		[Test()]
		public void TestBug526667 ()
		{
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
using System;
using System.Collections.Generic;

public class O
{
	public string X {
		get;
		set;
	}
	public string Y {
		get;
		set;
	}
	public List<string> Z {
		get;
		set;
	}

	public static O A ()
	{
		return new O {
			X = ""x"",
			Z = new List<string> (new string[] {
				""abc"",
				""def""
			})
			$, $
		};
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Y"), "property 'Y' not found.");
		}
		
		[Test()]
		public void TestObjectInitializer ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"class foo {
	public string bar { get; set; }
	public string baz { get; set; }
}

class test {
	public void testcc ()
	{
		foo f = new foo () {
			$b$
		};
	}
}
", provider => {
			Assert.IsNotNull (provider.Find ("bar"), "property 'bar' not found.");
			Assert.IsNotNull (provider.Find ("baz"), "property 'baz' not found.");
			});
		}
		

		/// <summary>
		/// Bug 1745 - [New Resolver] Invalid completion in class initialization
		/// </summary>
		[Test()]
		public void TestBug1745 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (

@"class Test {
	public int TF1 { get; set; }
}

class CCTest {
	void TestMethod ()
	{
		$new Test () { TF1 = T$
	}
}
", provider => {
			Assert.IsNull (provider.Find ("TF1"));
			Assert.IsNotNull (provider.Find ("Test"));
			});
		}	
		
		[Test()]
		public void TestBugAfterBracketContext ()
		{
			var provider = CodeCompletionBugTests.CreateProvider (
@"class Test {
	public int TF1 { get; set; }
}

class CCTest {
	void TestMethod ()
	{
		$new Test () {$
	}
}
");
			Assert.IsTrue (provider == null || provider.Count == 0);
		}
		
		/// <summary>
		/// Bug 487236 - Object initializer completion uses wrong type
		/// </summary>
		[Test()]
		public void TestObjectInitializerContinuation ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"
public class A
{
	public string Name { get; set; }
}

class MyTest
{
	static string Str = ""hello"";

	public void Test ()
	{
		$var x = new A () { Name = MyTest.$
	}
}
", provider => {
			Assert.IsNotNull (provider.Find ("Str"), "field 'Str' not found.");
			});
		}
	}
}

