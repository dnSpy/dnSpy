// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture, Ignore("Lambdas not supported by resolver")]
	public class LambdaTests : ResolverTestBase
	{
		[Test]
		public void SimpleLambdaTest()
		{
			string program = @"using System;
class TestClass {
	static void Main() {
		Test(i => Console.WriteLine(i));
	}
	public void Test(Action<int> ac) { ac(42); }
}";
			var lrr = Resolve<LocalResolveResult>(program.Replace("(i)", "($i$)"));
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
			
			lrr = Resolve<LocalResolveResult>(program.Replace("i =>", "$i$ =>"));
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaInConstructorTest()
		{
			string program = @"using System;
class TestClass {
	static void Main() {
		TestClass t = new TestClass(i => Console.WriteLine($i$));
	}
	public TestClass(Action<int> ac) { ac(42); }
}";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaInGenericConstructorTest()
		{
			string program = @"using System;
class TestClass {
	static void Main() {
		var t = new SomeClass<string>(i => Console.WriteLine($i$));
	}
}
class SomeClass<T> {
	public SomeClass(Action<T> ac) { }
}";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.String", lrr.Type.ReflectionName);
		}
		
		#region Lambda In Initializer
		[Test]
		public void LambdaInCollectionInitializerTest1()
		{
			string program = @"using System;
class TestClass {
	static void Main() {
		Converter<int, string>[] arr = {
			i => $i$.ToString()
		};
	}
}
";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaInCollectionInitializerTest2()
		{
			string program = @"using System; using System.Collections.Generic;
class TestClass {
	static void Main() {
		a = new List<Converter<int, string>> {
			i => $i$.ToString()
		};
	}
}
";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaInCollectionInitializerTest3()
		{
			string program = @"using System;
class TestClass {
	static void Main() {
		a = new Converter<int, string>[] {
			i => $i$.ToString()
		};
	}
}
";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaInCollectionInitializerTest4()
		{
			string program = @"using System;
class TestClass {
	Converter<int, string>[] field = new Converter<int, string>[] {
		i => $i$.ToString()
	};
}
";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaInCollectionInitializerTest5()
		{
			string program = @"using System;
class TestClass {
	Converter<int, string>[] field = {
		i => $i$.ToString()
	};
}
";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaInObjectInitializerTest()
		{
			string program = @"using System;
class X {
	void SomeMethod() {
		Helper h = new Helper {
			F = i => $i$.ToString()
		};
	}
}
class Helper {
	public Converter<int, string> F;
}
";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		#endregion
		
		[Test]
		public void LambdaExpressionInCastExpression()
		{
			string program = @"using System;
static class TestClass {
	static void Main(string[] args) {
		var f = (Func<int, string>) ( i => $i$ );
	}
	public delegate R Func<T, R>(T arg);
}";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaExpressionInReturnStatement()
		{
			string program = @"using System;
static class TestClass {
	static Converter<int, string> GetToString() {
		return i => $i$.ToString();
	}
}";
			var lrr = Resolve<LocalResolveResult>(program, "i");
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaExpressionInReturnStatementInStatementLambda()
		{
			string program = @"using System;
static class TestClass {
	static void SomeMethod() {
		Func<Func<string, string>> getStringTransformer = () => {
			return s => $s$.ToUpper();
		};
	}
	public delegate R Func<T, R>(T arg);
	public delegate R Func<R>();
}";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.String", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaExpressionInReturnStatementInAnonymousMethod()
		{
			string program = @"using System;
static class TestClass {
	static void SomeMethod() {
		Func<Func<string, string>> getStringTransformer = delegate {
			return s => $s$.ToUpper();
		};
	}
	public delegate R Func<T, R>(T arg);
	public delegate R Func<R>();
}";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.String", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void CurriedLambdaExpressionInCastExpression()
		{
			string program = @"using System;
static class TestClass {
	static void Main(string[] args) {
		var f = (Func<char, Func<string, int>>) ( a => b => 0 );
	}
	public delegate R Func<T, R>(T arg);
}";
			var lrr = Resolve<LocalResolveResult>(program.Replace("a =>", "$a$ =>"));
			Assert.AreEqual("System.Char", lrr.Type.ReflectionName);
			
			lrr = Resolve<LocalResolveResult>(program.Replace("b =>", "$b$ =>"));
			Assert.AreEqual("System.String", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaExpressionInVariableInitializer()
		{
			string program = @"using System;
static class TestClass {
	static void Main() {
		Func<int, string> f = $i$ => i.ToString();
	}
	public delegate R Func<T, R>(T arg);
}";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaExpressionInVariableAssignment()
		{
			string program = @"using System;
static class TestClass {
	static void Main() {
		Func<int, string> f;
 		f = $i$ => i.ToString();
	}
	public delegate R Func<T, R>(T arg);
}";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		[Test]
		public void LambdaInDelegateCall()
		{
			string program = @"using System;
class TestClass {
	static void Main() {
		Func<Func<int, string>, char> f;
		f($i$ => i.ToString());
	}
	public delegate R Func<T, R>(T arg);
}";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
		}
		
		/* TODO write test for this
class A
{
    static void Foo(string x, Action<Action> y) { Console.WriteLine(1); }
    static void Foo(object x, Func<Func<int>, int> y) { Console.WriteLine(2); }

    static void Main()
    {
        Foo(null, x => x()); // Prints 1
        Foo(null, x => (x())); // Prints 2
    }
}
		 */
	}
}
