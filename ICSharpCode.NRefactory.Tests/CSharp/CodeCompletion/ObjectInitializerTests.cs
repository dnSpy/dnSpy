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
		[Test()]
		public void TestArrayInitializerStart ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;

class MyTest
{
	public void Test ()
	{
		$new [] { M$
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("Tuple"), "class 'MyTest' not found.");
			});
		}
		
		[Test()]
		public void TestArrayInitializerSimple ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;

class MyTest
{
	public void Test ()
	{
		$new [] { Tuple.$
	}
}
", provider => {
				Assert.IsNotNull (provider.Find ("Create"), "method 'Create' not found.");
			});
		}
		
		/// <summary>
		/// Bug 432727 - No completion if no constructor
		/// </summary>
		[Test()]
		public void TestArrayInitializerParameterContext ()
		{
			var provider = ParameterCompletionTests.CreateProvider (
@"using System;

class MyTest
{
	public void Test ()
	{
		$new [] { Tuple.Create($
	}
}");
			Assert.IsNotNull (provider, "provider was not created.");
			Assert.Greater (provider.Count, 1);
		}
		
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
		$new A () { $
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
		new foo () {
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
		
		/// <summary>
		/// Bug 2434 - Object-Initializer Intellisense broken when using constructor arguments
		/// </summary>
		[Test()]
		public void TestBug2434 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"
class User
{
  public User() {}
  public User(int id) { }

  public string Id { get; set; }
  public string Name { get; set; }
}

class MyTest
{
	static string Str = ""hello"";

	public void Test ()
	{
		$new User(12) { 
			N$
	}
}
", provider => {
			Assert.IsNotNull (provider.Find ("Id"), "Property 'Id' not found.");
			Assert.IsNotNull (provider.Find ("Name"), "Property 'Name' not found.");
			});
		}
		
		[Test()]
		public void TestBug2434Case2 ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"
class User
{
  public User() {}
  public User(int id) { }

  public string Id { get; set; }
  public string Name { get; set; }
}

class MyTest
{
	static string Str = ""hello"";

	public void Test ()
	{
		string myString;

		$new User(12) { 
			Name = S$
	}
}
", provider => {
				Assert.IsNull (provider.Find ("Id"), "Property 'Id' found.");
				Assert.IsNull (provider.Find ("Name"), "Property 'Name' found.");
				Assert.IsNotNull (provider.Find ("Str"), "Field 'Str' not found.");
				Assert.IsNotNull (provider.Find ("myString"), "Local 'myString' not found.");
			});
		}
		
		[Test()]
		public void TestNewKeywordInInitializer ()
		{
			CodeCompletionBugTests.CombinedProviderTest (
@"using System;

class O
{
	public int Int { get; set; }
	public object Obj { get; set; }
}

class Test
{
	public void Method ()
	{
		$var foo = new O() { Int = 5, Obj = n$
	}
}
", (provider) => {
				Assert.IsNotNull (provider.Find ("new"), "keyword 'new' not found.");
			});
		}
		
		[Test()]
		public void TestCollectionInitializer()
		{
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;
using System.Collections.Generic;

class Test
{
	public void Method ()
	{
		new List<Test> () {
			$n$
		};
	}
}
", (provider) => {
				Assert.IsNotNull(provider.Find("new"), "keyword 'new' not found.");
			});
		}


		/// <summary>
		/// Bug 4284 - NewResolver does not offer completion for properties in constructor initialization (edit)
		/// </summary>
		[Test()]
		public void TestBug4284()
		{
			// only affects ctrl+space completion.
			var provider = CodeCompletionBugTests.CreateCtrlSpaceProvider(
@"public class ClassName
{
	public int Foo { get; set; }
}
class MainClass
{
	void Method ()
	{
		var stuff = new ClassName  {
			$
		};
	}
}
");
			Assert.IsNotNull(provider.Find("Foo"), "'Foo' not found.");
		}

		[Test()]
		public void TestArrayInitializerObjectCreation()
		{
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;
using System.Collections.Generic;

class MyTest
{
	public void Test ()
	{
		$new IEnumerable<string>[] { new L$
	}
}
", provider => {
				Assert.IsNotNull(provider.Find("List<string>"), "class 'List<string>' not found.");
			}
			);
		}

		[Test()]
		public void TestArrayInitializerObjectCreationNarrowing()
		{
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;
using System.Collections.Generic;
class MyList : List<IEnumerable<string>> {}
class MyTest
{
	public void Test ()
	{
		$new MyList { new L$
	}
}
", provider => {
				Assert.IsNotNull(provider.Find("List<string>"), "class 'List<string>' not found.");
			}
			);
		}

		[Test()]
		public void TestObjectCreationEnumerable()
		{
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;
using System.Collections.Generic;
class MyList : List<IEnumerable<string>> { public bool MyProp {get;set; } }
class MyTest
{
	public void Test ()
	{
		$new MyList { n$
	}
}
", provider => {
				Assert.IsNotNull(provider.Find("new"), "'new' not found.");
				Assert.IsNotNull(provider.Find("MyProp"), "'MyProp' not found.");
			}
			);
		}

		[Test()]
		public void TestObjectCreationForbiddenInArrayInitializers()
		{
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;
using System.Collections.Generic;
class MyList : List<IEnumerable<string>> { public bool MyProp {get;set; } }
class MyTest
{
	public void Test ()
	{
		$new MyList { new List<string> (), n$
	}
}
", provider => {
				Assert.IsNotNull(provider.Find("new"), "'new' not found.");
				Assert.IsNull(provider.Find("MyProp"), "'MyProp' found.");
			}
			);
		}

		[Test()]
		public void TestArrayInitializersForbiddenInObjectCreation()
		{
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;
using System.Collections.Generic;
class MyList : List<IEnumerable<string>> { public bool MyProp {get;set; }  public bool MyProp2 {get;set; }  }
class MyTest
{
	public void Test ()
	{
		$new MyList { MyProp = true, n$
	}
}
", provider => {
				Assert.IsNull(provider.Find("new"), "'new' found.");
				Assert.IsNotNull(provider.Find("MyProp2"), "'MyProp2' not found.");
			}
			);
		}

		/// <summary>
		/// Bug 5126 - Multiple projects including the same files don't update their typesystem properly
		/// </summary>
		[Test()]
		public void TestBug5126()
		{
			CodeCompletionBugTests.CombinedProviderTest(
@"using System;
using System.Collections.Generic;
class MyList { public bool MyProp {get;set; }  public bool MyProp2 {get;set; }  }
class MyTest
{
	public void Test ()
	{
		$new MyList { n$
	}
}
", provider => {
				Assert.IsNull(provider.Find("new"), "'new' found.");
				Assert.IsNotNull(provider.Find("MyProp"), "'MyProp' not found.");
				Assert.IsNotNull(provider.Find("MyProp2"), "'MyProp2' not found.");
			}
			);
		}

	}

}

