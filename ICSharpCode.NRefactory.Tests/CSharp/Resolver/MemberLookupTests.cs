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
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class MemberLookupTests : ResolverTestBase
	{
		MemberLookup lookup;
		
		public override void SetUp()
		{
			base.SetUp();
			lookup = new MemberLookup(null, compilation.MainAssembly);
		}
		
		CSharpUnresolvedFile Parse(string program)
		{
			SyntaxTree syntaxTree = SyntaxTree.Parse(program, "test.cs");
			CSharpUnresolvedFile unresolvedFile = syntaxTree.ToTypeSystem();
			project = project.AddOrUpdateFiles(unresolvedFile);
			compilation = project.CreateCompilation();
			return unresolvedFile;
		}
		
		[Test]
		public void GroupMethodsByDeclaringType()
		{
			string program = @"
class Base {
	public virtual void Method() {}
}
class Middle : Base {
	public void Method(int p) {}
}
class Derived : Middle {
	public override void Method() {}
}";
			var unresolvedFile = Parse(program);
			ITypeDefinition derived = compilation.MainAssembly.GetTypeDefinition(unresolvedFile.TopLevelTypeDefinitions[2]);
			var rr = lookup.Lookup(new ResolveResult(derived), "Method", EmptyList<IType>.Instance, true) as MethodGroupResolveResult;
			Assert.AreEqual(2, rr.MethodsGroupedByDeclaringType.Count());
			
			var baseGroup = rr.MethodsGroupedByDeclaringType.ElementAt(0);
			Assert.AreEqual("Base", baseGroup.DeclaringType.ReflectionName);
			Assert.AreEqual(1, baseGroup.Count);
			Assert.AreEqual("Derived.Method", baseGroup[0].FullName);
			
			var middleGroup = rr.MethodsGroupedByDeclaringType.ElementAt(1);
			Assert.AreEqual("Middle", middleGroup.DeclaringType.ReflectionName);
			Assert.AreEqual(1, middleGroup.Count);
			Assert.AreEqual("Middle.Method", middleGroup[0].FullName);
		}
		
		[Test]
		public void MethodInGenericClassOverriddenByConcreteMethod()
		{
			string program = @"
class Base<T> {
	public virtual void Method(T a) {}
}
class Derived : Base<int> {
	public override void Method(int a) {}
	public override void Method(string a) {}
}";
			var unresolvedFile = Parse(program);
			ITypeDefinition derived = compilation.MainAssembly.GetTypeDefinition(unresolvedFile.TopLevelTypeDefinitions[1]);
			var rr = lookup.Lookup(new ResolveResult(derived), "Method", EmptyList<IType>.Instance, true) as MethodGroupResolveResult;
			Assert.AreEqual(2, rr.MethodsGroupedByDeclaringType.Count());
			
			var baseGroup = rr.MethodsGroupedByDeclaringType.ElementAt(0);
			Assert.AreEqual("Base`1[[System.Int32]]", baseGroup.DeclaringType.ReflectionName);
			Assert.AreEqual(1, baseGroup.Count);
			Assert.AreEqual("Derived.Method", baseGroup[0].FullName);
			Assert.AreEqual("System.Int32", baseGroup[0].Parameters[0].Type.ReflectionName);
			
			var derivedGroup = rr.MethodsGroupedByDeclaringType.ElementAt(1);
			Assert.AreEqual("Derived", derivedGroup.DeclaringType.ReflectionName);
			Assert.AreEqual(1, derivedGroup.Count);
			Assert.AreEqual("Derived.Method", derivedGroup[0].FullName);
			Assert.AreEqual("System.String", derivedGroup[0].Parameters[0].Type.ReflectionName);
		}
		
		[Test]
		public void GenericMethod()
		{
			string program = @"
class Base {
	public virtual void Method<T>(T a) {}
}
class Derived : Base {
	public override void Method<S>(S a) {}
}";
			var unresolvedFile = Parse(program);
			ITypeDefinition derived = compilation.MainAssembly.GetTypeDefinition(unresolvedFile.TopLevelTypeDefinitions[1]);
			var rr = lookup.Lookup(new ResolveResult(derived), "Method", EmptyList<IType>.Instance, true) as MethodGroupResolveResult;
			Assert.AreEqual(1, rr.MethodsGroupedByDeclaringType.Count());
			
			var baseGroup = rr.MethodsGroupedByDeclaringType.ElementAt(0);
			Assert.AreEqual("Base", baseGroup.DeclaringType.ReflectionName);
			Assert.AreEqual(1, baseGroup.Count);
			Assert.AreEqual("Derived.Method", baseGroup[0].FullName);
			Assert.AreEqual("``0", baseGroup[0].Parameters[0].Type.ReflectionName);
		}
		
		[Test]
		public void InstanceFieldImplicitThis()
		{
			string program = @"class Test {
	public int Field;
	int M() { return $Field$; }
}";
			var rr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("Test.Field", rr.Member.FullName);
			Assert.IsTrue(rr.TargetResult is ThisResolveResult);
		}
		
		[Test]
		public void InstanceFieldExplicitThis()
		{
			string program = @"class Test {
	public int Field;
	int M() { return $this.Field$; }
}";
			var rr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("Test.Field", rr.Member.FullName);
			Assert.IsTrue(rr.TargetResult is ThisResolveResult);
		}
		
		[Test]
		public void StaticField()
		{
			string program = @"class Test {
	public static int Field;
	int M() { return $Field$; }
}";
			var rr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("Test.Field", rr.Member.FullName);
			Assert.IsTrue(rr.TargetResult is TypeResolveResult);
		}
		
		[Test]
		public void InstanceMethodImplicitThis()
		{
			string program = @"using System;
class Test {
	void F() {}
	public void M() {
		$F()$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("Test.F", rr.Member.FullName);
			Assert.IsInstanceOf<ThisResolveResult>(rr.TargetResult);
		}
		
		[Test]
		public void InstanceMethodExplicitThis()
		{
			string program = @"using System;
class Test {
	void F() {}
	public void M() {
		$this.F()$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("Test.F", rr.Member.FullName);
			Assert.IsInstanceOf<ThisResolveResult>(rr.TargetResult);
		}
		
		[Test]
		public void StaticMethod()
		{
			string program = @"using System;
class Test {
	static void F() {}
	public void M() {
		$F()$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("Test.F", rr.Member.FullName);
			Assert.IsInstanceOf<TypeResolveResult>(rr.TargetResult);
		}
		
		[Test]
		public void TestOuterTemplateParameter()
		{
			string program = @"public class A<T>
{
	public class B
	{
		public T field;
	}
}

public class Foo
{
	public void Bar ()
	{
		A<int>.B baz = new A<int>.B ();
		$baz.field$.ToString ();
	}
}";
			var lrr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.FullName);
		}
		
		[Test]
		public void TestOuterTemplateParameterInDerivedClass()
		{
			string program = @"public class A<T>
{
	public class B
	{
		public T field;
	}
}

public class Foo : A<int>.B
{
	public void Bar ()
	{
		$field$.ToString ();
	}
}";
			var lrr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.FullName);
		}
		
		[Test]
		public void TestOuterTemplateParameterInDerivedClass2()
		{
			string program = @"public class A<T>
{
	public class B
	{
		public T field;
	}
}

public class Foo : A<int>
{
	public void Bar (B v)
	{
		$v.field$.ToString ();
	}
}";
			var lrr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.FullName);
		}
		
		[Test]
		public void MemberInGenericClassReferringToInnerClass()
		{
			string program = @"public class Foo<T> {
	public class TestFoo { }
	public TestFoo Bar = new TestFoo ();
}
public class Test {
	public void SomeMethod (Foo<Test> foo) {
		var f = $foo.Bar$;
	}
}";
			var mrr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("Foo`1+TestFoo[[Test]]", mrr.Type.ReflectionName);
		}
		
		[Test]
		public void ProtectedBaseMethodCall()
		{
			string program = @"using System;
public class Base {
	protected virtual void M() {}
}
public class Test : Base {
	protected override void M() {
		$base.M()$;
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("Base.M", rr.Member.FullName);
		}
		
		[Test]
		public void ProtectedBaseFieldAccess()
		{
			string program = @"using System;
public class Base {
	protected int Field;
}
public class Test : Base {
	public new int Field;
	protected override void M() {
		$base.Field$ = 1;
	}
}";
			var rr = Resolve<MemberResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("Base.Field", rr.Member.FullName);
		}
		
		[Test]
		public void ThisHasSameTypeAsFieldInGenericClass()
		{
			string program = @"using System;
public struct C<T> {
	public C(C<T> other) {
		$M(this, other)$;
	}
	static void M<T>(T a, T b) {}
}
";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("C`1[[`0]]", rr.Arguments[0].Type.ReflectionName);
			Assert.AreEqual("C`1[[`0]]", rr.Arguments[1].Type.ReflectionName);
		}
		
		[Test]
		public void ProtectedFieldInOuterClass()
		{
			string program = @"using System;
class Base {
  protected int X;
}
class Derived : Base {
  class Inner {
     public int M(Derived d) { return $d.X$; }
}}";
			var rr = Resolve<MemberResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("Base.X", rr.Member.FullName);
		}
		
		[Test]
		public void ProtectedMemberViaTypeParameter()
		{
			string program = @"using System;
class Base
{
	protected void Test() {}
	public void Test(int a = 0) {}
}
class Derived<T> : Base where T : Derived<T>
{
	void M(Derived<T> a, Base b, T c) {
		a.Test(); // calls Test()
		b.Test(); // calls Test(int)
		c.Test(); // calls Test()
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program.Replace("a.Test()", "$a.Test()$"));
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual(0, rr.Member.Parameters.Count);
			
			rr = Resolve<CSharpInvocationResolveResult>(program.Replace("b.Test()", "$b.Test()$"));
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual(1, rr.Member.Parameters.Count);
			
			rr = Resolve<CSharpInvocationResolveResult>(program.Replace("c.Test()", "$c.Test()$"));
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual(0, rr.Member.Parameters.Count);
		}
		
		[Test]
		public void MethodGroupConversionForGenericMethodHasSpecializedMethod()
		{
			string program = @"using System;
class TestClass {
	void F<T>(T x) {}
	public void M() {
		System.Action<int> f;
		f = $F$;
	}
}";
			var conversion = GetConversion(program);
			Assert.IsTrue(conversion.IsValid);
			Assert.IsTrue(conversion.IsMethodGroupConversion);
			Assert.IsInstanceOf<SpecializedMethod>(conversion.Method);
			Assert.AreEqual(
				new[] { "System.Int32" },
				((SpecializedMethod)conversion.Method).TypeArguments.Select(t => t.ReflectionName).ToArray());
		}
		
		[Test]
		public void PartialMethod_WithoutImplementation()
		{
			string program = @"using System;
class TestClass {
	$partial void M();$
}";
			var mrr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("TestClass.M", mrr.Member.FullName);
		}
		
		[Test]
		public void PartialMethod_Declaration()
		{
			string program = @"using System;
class TestClass {
	$partial void M();$
	
	partial void M() {}
}";
			var mrr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("TestClass.M", mrr.Member.FullName);
		}
		
		[Test]
		public void PartialMethod_Implementation()
		{
			string program = @"using System;
class TestClass {
	partial void M();
	
	$partial void M() {}$
}";
			var mrr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("TestClass.M", mrr.Member.FullName);
		}
		
		[Test]
		public void MembersWithoutWhitespace()
		{
			string program = @"using System;
class TestClass {
	void A();$void B();$void C();
}";
			var mrr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("TestClass.B", mrr.Member.FullName);
		}
	}
}
