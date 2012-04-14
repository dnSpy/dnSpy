//
// CodeCompletionCSharp3Tests.cs
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
	public class CodeCompletionCSharp3Tests
	{
		/* Currently fails but works in monodevelop. Seems to be a bug in the unit test somewhere.
		[Test()]
		public void TestExtensionMethods ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"using System;

public static class EMClass
{
	public static int ToInt32Ext (this Program s)
	{
		return Int32.Parse (s);
	}
}

class Program
{
	static void Main (string[] args)
	{
		Program s;
		int i = s.$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("ToInt32Ext"), "extension method 'ToInt32Ext' not found.");
		}
		*/
		[Test()]
		public void TestVarLocalVariables ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"using System;

class Test
{
	public void TestMethod ()
	{
	}
}

class Program
{
	static void Main (string[] args)
	{
		var t = new Test ();
		$t.$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found.");
		}
		
		[Test()]
		public void TestVarLoopVariable ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"using System;

class Test
{
	public void TestMethod ()
	{
	}
}

class Program
{
	static void Main (string[] args)
	{
		var t = new Test[] {};
		foreach (var loopVar in t) {
			$loopVar.$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found.");
		}

		[Test()]
		public void TestAnonymousType ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
class Program
{
	static void Main (string[] args)
	{
		var t = new { TestInt = 6, TestChar='e', TestString =""Test""};
		$t.$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestInt"), "property 'TestInt' not found.");
			Assert.IsNotNull (provider.Find ("TestChar"), "property 'TestChar' not found.");
			Assert.IsNotNull (provider.Find ("TestString"), "property 'TestString' not found.");
		}
		
		[Test()]
		public void TestQueryExpression ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
using System;
using System.Collections.Generic;

static class Linq
{
	public static IEnumerable<T> Select<S, T> (this IEnumerable<S> collection, Func<S, T> func)
	{
	}
}

class Program
{
	public void TestMethod ()
	{
	}
	
	static void Main (string[] args)
	{
		Program[] numbers;
		foreach (var x in from n in numbers select n) {
			$x.$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found.");
		}
		
		[Test()]
		public void TestLambdaExpressionCase1 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateCtrlSpaceProvider (
@"
using System;
class Test
{	
	public void Foo ()
	{
		$Func<Test,int> x = s => s.$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
		}
		
		[Test()]
		public void TestLambdaExpressionCase2 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"

namespace System {
	public class Array {
		public Test this[int i] {
			get {
			}
			set {
			}
		}
	}
}

static class ExtMethods
{
	public static T Where<T>(this T[] t, Func<T, bool> pred)
	{
		return t;
	}
}

class Test
{
	public void TestMethod ()
	{
		Test[] en = new Test[0];
		var x = en.Where (t => t != null);
		$x.$
	}
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found.");
		}
		
		/// <summary>
		/// Bug 487237 - Broken lambda intellisense 
		/// </summary>
		[Test()]
		public void TestBug487237 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
public interface IHelper
{
    void DoIt ();
}

public class Program
{
	delegate T MyDelegate <T> (T t);
    
	static int Main ()
    {
        $MyDelegate<IHelper> e = helper => helper.$
        return 0;
    }
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("DoIt"), "method 'DoIt' not found.");
		}
		
		/// <summary>
		/// Bug 491016 - No intellisense for lambdas inside linq query
		/// </summary>
		[Test()]
		public void TestBug491016 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
using System;
using System.Collections.Generic;

namespace System.Collections.Generic {
	public interface IEnumerable<T>
	{
	
	}
}
namespace Foo
{
    class Data
    {
        public int Value = 5;
    }

    static class Ex
    {
        public static System.Collections.Generic.IEnumerable<TR> Foo<T, TR> (this System.Collections.Generic.IEnumerable<T> t, Func<T, TR> f)
        {
            yield return f (t.First ());
        }
    }

    public class C
    {
        public static void Main ()
        {
            System.Collections.Generic.IEnumerable<Data> i = new Data [0];
            $var prods = from pe in i.Foo (p2 => p2.$
        }
    }
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Value"), "field 'Value' not found.");
		}
		
		/// <summary>
		/// Bug 491017 - No intellisense for static LINQ queries
		/// </summary>
		[Test()]
		public void TestBug491017 ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
using System.Linq;
using System.Linq.Expressions;

class Test
{
    $object e = from entity in ""olololcolc"" select entity.$
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("ToString"), "method 'ToString' not found.");
			Assert.IsNull (provider.Find ("Length"), "property 'Length' found, but shouldn't (indicates wrong return type).");
		}
		
		[Test()]
		public void TestDefaultParameterBug ()
		{
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider (
@"
namespace Foo
{
    class Data
    {
        public int Value = 5;
    }

    public class C
    {
        public void Foo (bool aBool = false)
        {
			Data data;
            $data.$
        }
    }
}
");
			Assert.IsNotNull (provider, "provider == null");
			Assert.IsNotNull (provider.Find ("Value"), "field 'Value' not found.");
		}
		
		[Test()]
		public void TestLinqWhere() {
			CompletionDataList provider = CodeCompletionBugTests.CreateProvider(
@"
using System.Collections.Generic;
using System.Linq;
class A
{
	public static void Method1()
	{
    	int[] enumerable =  new int[]{1,2,3};
		$IEnumerable<int> q = from i in enumerable where i.$
	}
}

");
			Assert.IsNotNull(provider); // <--- here 0 item in the completion list
			Assert.IsNotNull(provider.Find("ToString"));
		}
		
		[Test()]
		public void TestLinqSelectContext () 
		{
			var provider = CodeCompletionBugTests.CreateProvider(
@"
using System.Collections.Generic;
using System.Linq;
class A
{
	public static void Main (string[] args)
	{
		$from a in args select n$
	}
}

");
			Assert.IsNotNull(provider); // <--- here 0 item in the completion list
			Assert.IsNotNull(provider.Find("new"), "'new' not found");
			Assert.IsNotNull(provider.Find("args"), "'args' not found");
			Assert.IsNotNull(provider.Find("a"), "'a' not found");
		}
		
		[Test()]
		public void TestLinqAnonymousTypeContext () 
		{
			var provider = CodeCompletionBugTests.CreateProvider(
@"
using System.Collections.Generic;
using System.Linq;
class A
{
	public static void Main (string[] args)
	{
		Test($from a in args select new { t$);
	}
}

");
			Assert.IsNotNull(provider);
			Assert.IsFalse (provider.AutoSelect );
		}
		
		[Test()]
		public void TestLinqAnonymousTypeContextCase2 () 
		{
			var provider = CodeCompletionBugTests.CreateProvider(
@"
using System.Collections.Generic;
using System.Linq;
class A
{
	public static void Main (string[] args)
	{
		$from a in args select new { test = a$
	}
}

");
			Assert.IsNotNull(provider); // <--- here 0 item in the completion list
			Assert.IsTrue (provider.AutoSelect );
			Assert.IsNotNull(provider.Find("a"), "'a' not found");
			Assert.IsNotNull(provider.Find("new"), "'new' not found");
			Assert.IsNotNull(provider.Find("args"), "'args' not found");
		}
		
		[Test()]
		public void TestLinqAnonymousTypeContextCase3 () 
		{
			var provider = CodeCompletionBugTests.CreateProvider(
@"
using System.Collections.Generic;
using System.Linq;
class A
{
	public static void Main (string[] args)
	{
		$from a in args select new { test = a }$
	}
}

");
			Assert.IsTrue(provider == null || provider.Count == 0); // <--- here 0 item in the completion list
		}
		
		[Test()]
		public void TestLinqExpressionContext () 
		{
			var provider = CodeCompletionBugTests.CreateProvider(
@"
using System.Collections.Generic;
using System.Linq;
class A
{
	public static void Main (string[] args)
	{
		$from a in args where a !$
	}
}

");
			Assert.IsTrue(provider == null || provider.Count == 0); // <--- here 0 item in the completion list
		}
	}
}
