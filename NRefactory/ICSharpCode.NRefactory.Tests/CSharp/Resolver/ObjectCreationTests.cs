// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class ObjectCreationTests : ResolverTestBase
	{
		[Test]
		public void GenericObjectCreation()
		{
			string program = @"using System.Collections.Generic;
class A {
	static void Main() {
		var a = $new List<string>()$;
	}
}
";
			var result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.List..ctor", result.Member.FullName);
			
			Assert.AreEqual("System.Collections.Generic.List`1[[System.String]]", result.Type.ReflectionName);
		}
		
		[Test]
		public void NonExistingClass ()
		{
			string program = @"class A {
	void Method() {
		var a = $new ThisClassDoesNotExist()$;
	}
}
";
			ResolveResult result = Resolve (program);
			Assert.IsTrue (result.IsError);
			Assert.AreSame(SpecialType.UnknownType, result.Type);
		}
		
		[Test]
		public void NonExistingClassTypeName()
		{
			string program = @"class A {
	void Method() {
		var a = new $ThisClassDoesNotExist$();
	}
}
";
			UnknownIdentifierResolveResult result = Resolve<UnknownIdentifierResolveResult>(program);
			Assert.AreEqual("ThisClassDoesNotExist", result.Identifier);
			Assert.AreSame(SpecialType.UnknownType, result.Type);
		}
		
		[Test]
		public void CTorOverloadLookupTest()
		{
			string program = @"class A {
	void Method() {
		$;
	}
	
	static A() {}
	A() {}
	A(int intVal) {}
	A(double dblVal) {}
}
";
			var result = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$new A()$"));
			Assert.IsFalse(result.Member.IsStatic, "new A() is static");
			Assert.AreEqual(0, result.Member.Parameters.Count, "new A() parameter count");
			Assert.AreEqual("A", result.Type.FullName);
			
			result = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$new A(10)$"));
			Assert.AreEqual(1, result.Member.Parameters.Count, "new A(10) parameter count");
			Assert.AreEqual("intVal", result.Member.Parameters[0].Name, "new A(10) parameter");
			
			result = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$new A(11.1)$"));
			Assert.AreEqual(1, result.Member.Parameters.Count, "new A(11.1) parameter count");
			Assert.AreEqual("dblVal", result.Member.Parameters[0].Name, "new A(11.1) parameter");
		}
		
		[Test]
		public void DefaultCTorOverloadLookupTest()
		{
			string program = @"class A {
	void Method() {
		$new A()$;
	}
}
";
			var result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("A", result.Type.ReflectionName);
			Assert.AreEqual(0, result.Member.Parameters.Count);
		}
		
		[Test]
		public void ChainedConstructorCall()
		{
			string program = @"using System;
class A {
	public A(int a) {}
}
class B : A {
	public B(int b)
		: base(b)
 	{}
}
class C : B {
	public C(int c)
		: base(c)
 	{}
 	
 	public C()
		: this(0)
 	{}
}
";
			InvocationResolveResult mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("base(b)", "$base(b)$"));
			Assert.AreEqual("A..ctor", mrr.Member.FullName);
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("base(c)", "$base(c)$"));
			Assert.AreEqual("B..ctor", mrr.Member.FullName);
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("this(0)", "$this(0)$"));
			Assert.AreEqual("C..ctor", mrr.Member.FullName);
		}
		
		[Test]
		public void FieldReferenceInObjectInitializer()
		{
			string program = @"class A {
	public int Property;
}
class B {
	void Method() {
		var x = new A() { $Property = 0$ };
	}
}";
			MemberResolveResult result = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("A.Property", result.Member.FullName);
		}
		
		[Test]
		public void FieldReferenceInNestedObjectInitializer()
		{
			string program = @"class Point { public float X, Y; }
class Rect { public Point TopLeft, BottomRight; }
class B {
	void Method() {
		var x = new Rect() { TopLeft = { $X = 1$ } };
	}
}";
			MemberResolveResult result = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("Point.X", result.Member.FullName);
		}

		[Ignore("Broken")]
		[Test]
		public void CollectionInitializerTest()
		{
			string program = @"using System.Collections.Generic;
class B {
	void Method() {
		var x = new List<int>() { ${ 0 }$ };
	}
}";
			var result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.List.Add", result.Member.FullName);
		}
		
		[Ignore("Broken on mcs/mac os x")]
		[Test]
		public void DictionaryInitializerTest()
		{
			string program = @"using System.Collections.Generic;
class B {
	void Method() {
		var x = new Dictionary<char, int>() { ${ 'a', 0 }$ };
	}
}";
			var result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.Dictionary.Add", result.Member.FullName);
		}
		
		[Test]
		public void CanCallProtectedBaseConstructorInCtorInitializer()
		{
			string program = @"using System.Collections.Generic;
class A { protected A(int x) {} }
class B : A { public B(int y) : $base(y)$ { } }";
			var result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(result.IsError);
			Assert.AreEqual("A..ctor", result.Member.FullName);
		}
		
		[Test]
		public void CannotCallProtectedBaseConstructorAsNewObject()
		{
			string program = @"using System.Collections.Generic;
class A { protected A(int x) {} }
class B : A { public B(int y) { $new A(y)$; } }";
			var result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsTrue(result.IsError);
			Assert.AreEqual(OverloadResolutionErrors.Inaccessible, result.OverloadResolutionErrors);
			Assert.AreEqual("A..ctor", result.Member.FullName); // should still find member even if it's not accessible
		}
		
		[Test]
		public void CannotCallProtectedDerivedConstructorAsNewObject()
		{
			string program = @"using System.Collections.Generic;
class A { protected A(int x) { $new B(x)$; } }
class B : A { protected B(int y) {} }";
			var result = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsTrue(result.IsError);
			Assert.AreEqual(OverloadResolutionErrors.Inaccessible, result.OverloadResolutionErrors);
			Assert.AreEqual("B..ctor", result.Member.FullName); // should still find member even if it's not accessible
		}
		
		[Test]
		public void ComplexObjectInitializer()
		{
			string program = @"using System;
using System.Collections.Generic;
struct Point { public int X, Y; }
class Test {
	public Point Pos;
	public List<string> List = new List<string>();
	public Dictionary<string, int> Dict = new Dictionary<string, int>();
	
	static object M() {
		return $new Test {
			Pos = { X = 1, Y = 2 },
			List = { ""Hello"", ""World"" },
			Dict = { { ""A"", 1 } }
		}$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("Test..ctor", rr.Member.FullName);
			Assert.AreEqual("Test", rr.Type.ReflectionName);
			Assert.AreEqual(5, rr.InitializerStatements.Count);
		}
		
		[Test]
		public void CreateGeneric()
		{
			string program = @"using System;
class Test<T> where T : new() {
	object x = $new T()$;
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual(TypeKind.TypeParameter, rr.Type.Kind);
			Assert.AreEqual(TypeKind.TypeParameter, rr.Member.DeclaringType.Kind);
		}
		
		[Test]
		public void CreateDelegateFromMethodGroup()
		{
			string program = @"using System;
delegate void D(int i);
class C {
	void M(int y) {
		D d = $new D(M)$;
	}
}";
			var rr = Resolve<ConversionResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.IsTrue(rr.Conversion.IsIdentityConversion);
			var rr2 = (ConversionResolveResult)rr.Input;
			Assert.IsFalse(rr2.IsError);
			Assert.IsTrue(rr2.Conversion.IsMethodGroupConversion);
			
			Assert.AreEqual("C.M", rr2.Conversion.Method.FullName);
			var mgrr = (MethodGroupResolveResult)rr2.Input;
			Assert.IsInstanceOf<ThisResolveResult>(mgrr.TargetResult);
		}
		
		[Test]
		public void CreateDelegateFromDelegate()
		{
			string program = @"using System;
delegate void D1(int i);
delegate void D2(int i);
class C {
	void M(D1 d1) {
		D2 d2 = $new D2(d1)$;
	}
}";
			var rr = Resolve<ConversionResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.IsTrue(rr.Conversion.IsMethodGroupConversion);
			Assert.AreEqual("D1.Invoke", rr.Conversion.Method.FullName);
			var mgrr = (MethodGroupResolveResult)rr.Input;
			Assert.IsInstanceOf<LocalResolveResult>(mgrr.TargetResult);
		}
	}
}
