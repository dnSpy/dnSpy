// 
// ParserBugTests.cs
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
using ICSharpCode.NRefactory.CSharp;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp.Parser.Bugs
{
	[TestFixture]
	public class ParserBugTests
	{
		/// <summary>
		/// Bug 4252 - override bug in mcs ast
		/// </summary>
		[Ignore("Still open")]
		[Test]
		public void TestBug4242()
		{
			string code = @"
class Foo
{

    class Bar
    {
          override foo
    }

    public Foo () 
    {
    }
}";
			var unit = SyntaxTree.Parse(code);
			var type = unit.Members.First() as TypeDeclaration;
			var constructor = type.Members.Skip(1).First() as ConstructorDeclaration;
			var passed = !constructor.HasModifier(Modifiers.Override);
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}
		
		/// <summary>
		/// Bug 4059 - Return statement without semicolon missing in the AST
		/// </summary>
		[Test]
		public void TestBug4059()
		{
			string code = @"
class Stub
{
    Test A ()
    {
        return new Test ()
    }
}";
			var unit = SyntaxTree.Parse(code);
			var type = unit.Members.First() as TypeDeclaration;
			var method = type.Members.First() as MethodDeclaration;
			bool passed = method.Body.Statements.FirstOrDefault() is ReturnStatement;
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}
		
		/// <summary>
		/// Bug 4058 - Unattached parameter attributes should be included in the AST
		/// </summary>
		[Test]
		public void TestBug4058()
		{
			string code = @"
class TestClass
{
  TestClass([attr])
  {
  }
}";
			var unit = SyntaxTree.Parse(code);
			
			var type = unit.Members.First() as TypeDeclaration;
			var constructor = type.Members.First() as ConstructorDeclaration;
			bool passed = constructor.GetNodeAt<AttributeSection>(constructor.LParToken.StartLocation.Line, constructor.LParToken.StartLocation.Column + 1) != null;
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}
		
		
		
		/// <summary>
		/// Bug 3952 - Syntax errors that causes AST not inserted 
		/// </summary>
		[Ignore("Still open")]
		[Test]
		public void TestBug3952()
		{
			string code = @"
class Foo
{
	void Bar()
	{
		Test(new Foo (
	}
}";
			var unit = SyntaxTree.Parse(code);
			
			var type = unit.Members.First() as TypeDeclaration;
			var method = type.Members.First() as MethodDeclaration;
			bool passed = !method.Body.IsNull;
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}
		
		/// <summary>
		/// Bug 3578 - For condition not in the AST.
		/// </summary>
		[Test]
		public void TestBug3578()
		{
			string code = 
@"class Foo
{
	void Bar ()
	{
		for (int i = 0; i < foo.bar)
	}
}";
			var unit = SyntaxTree.Parse(code);
			
			bool passed = @"class Foo
{
	void Bar ()
	{
		for (int i = 0; i < foo.bar;)
	}
}" == unit.GetText ().Trim ();
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}
		
		/// <summary>
		/// Bug 3517 - Incomplete conditional operator in the AST request.
		/// </summary>
		[Test]
		public void TestBug3517()
		{
			string code = 
@"class Test
{
	void Foo ()
	{
		a = cond ? expr
	}
}";
			var unit = SyntaxTree.Parse(code);
			
			var type = unit.Members.First() as TypeDeclaration;
			var method = type.Members.First() as MethodDeclaration;
			var exprStmt = method.Body.Statements.FirstOrDefault() as ExpressionStatement;
			var expr = exprStmt.Expression as AssignmentExpression;
			bool passed = expr != null && expr.Right is ConditionalExpression;
			
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}
		
		[Test]
		public void TestBug3517Case2()
		{
			string code = 
@"class Test
{
	void Foo ()
	{
		a = cond ? expr :
	}
}";
			var unit = SyntaxTree.Parse(code);
			
			var type = unit.Members.First() as TypeDeclaration;
			var method = type.Members.First() as MethodDeclaration;
			var exprStmt = method.Body.Statements.FirstOrDefault() as ExpressionStatement;
			var expr = exprStmt.Expression as AssignmentExpression;
			bool passed = expr != null && expr.Right is ConditionalExpression;
			
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}
		
		/// <summary>
		/// Bug 3468 - Local variable declarations are not inserted in ast & break declaring member locations.
		/// </summary>
		[Test]
		public void TestBug3468()
		{
			string code = 
@"class C
{
    public static void Main (string[] args)
    {
        string str = 
    }
}";
			var unit = SyntaxTree.Parse(code);
			
			var type = unit.Members.First() as TypeDeclaration;
			var method = type.Members.First() as MethodDeclaration;
			bool passed = !method.Body.IsNull;
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}
		
		/// <summary>
		/// Bug 3288 - Try ... catch not added to the ast if catch block is missing
		/// </summary>
		[Test]
		public void TestBug3288()
		{
			string code = 
@"class Test
{
	public void Main(string[] args)
	{
		try {
		} catch (Exception name)
	}
}";
			var unit = SyntaxTree.Parse(code);
			
			var type = unit.Members.First() as TypeDeclaration;
			var method = type.Members.First() as MethodDeclaration;
			bool passed = method.Body.Statements.FirstOrDefault() is TryCatchStatement;
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}
		
		/// <summary>
		/// Bug 3155 - Anonymous methods in variable declarations don't produce an ast, if the ';' is missing.
		/// </summary>
		[Test]
		public void TestBug3155()
		{
			string code = 
@"using System;

class Test
{
    void Foo ()
    {
        Action<int> act = delegate (int testMe) {

        }
    }
}
";
			var unit = SyntaxTree.Parse(code);
			
			bool passed = unit.GetText().Trim() == @"using System;
class Test
{
	void Foo ()
	{
		Action<int> act = delegate (int testMe) {
		};
	}
}";
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}

		/// <summary>
		/// Bug 4556 - AST broken for unclosed invocation
		/// </summary>
		[Ignore ()]
		[Test]
		public void TestBug4556()
		{
			string code = 
@"using System;

class Foo
{
    public static void Main (string[] args)
    {
        Console.WriteLine (""foo"", 
    }
}
";
			var unit = SyntaxTree.Parse(code);
			
			var type = unit.Members.First() as TypeDeclaration;
			var method = type.Members.First() as MethodDeclaration;
			bool passed = !method.Body.IsNull;
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}
		
		/// <summary>
		/// Bug 5064 - Autocomplete doesn't include object initializer properties in yield return 
		/// </summary>
		[Ignore("Still open")]
		[Test]
		public void TestBug5064()
		{
			string code = 
@"public class Bar
{
    public IEnumerable<Foo> GetFoos()
    {
        yield return new Foo { }
    }
}";
			var unit = SyntaxTree.Parse(code);
			
			bool passed = unit.GetText().Trim() == @"public class Bar
{
	public IEnumerable<Foo> GetFoos()
	{
		yield return new Foo { };
	}
}";
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}

		/// <summary>
		/// Bug 5389 - Code completion does not work well inside a dictionary initializer
		/// </summary>
		[Ignore("Still open")]
		[Test()]
		public void TestBug5389()
		{
			string code = 
@"class Foo
{
	static Dictionary<Tuple<Type, string>, string> CreatePropertyMap ()
	{
		return new Dictionary<Tuple<Type, string>, string> {
			{ Tuple.Create (typeof (MainClass), ""Prop1""), ""Prop2"" },
			{ Tuple.C }
		}
	}
}
";
			var unit = SyntaxTree.Parse(code);
			
			var type = unit.Members.First() as TypeDeclaration;
			var method = type.Members.First() as MethodDeclaration;
			var stmt = method.Body.Statements.First () as ReturnStatement;
			bool passed = stmt.Expression is ObjectCreateExpression;
			if (!passed) {
				Console.WriteLine("Expected:" + code);
				Console.WriteLine("Was:" + unit.GetText());
			}
			Assert.IsTrue(passed);
		}
	}
}

