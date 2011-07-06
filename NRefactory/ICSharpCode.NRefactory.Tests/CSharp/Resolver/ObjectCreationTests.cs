// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
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
			MemberResolveResult result = Resolve<MemberResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.List..ctor", result.Member.FullName);
			
			Assert.AreEqual("System.Collections.Generic.List`1[[System.String]]", result.Type.ReflectionName);
		}
		
		[Test]
		public void NonExistingClass()
		{
			string program = @"class A {
	void Method() {
		var a = $new ThisClassDoesNotExist()$;
	}
}
";
			ResolveResult result = Resolve<ResolveResult>(program);
			Assert.AreSame(SharedTypes.UnknownType, result.Type);
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
			Assert.AreSame(SharedTypes.UnknownType, result.Type);
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
			MemberResolveResult result = Resolve<MemberResolveResult>(program.Replace("$", "$new A()$"));
			IMethod m = (IMethod)result.Member;
			Assert.IsFalse(m.IsStatic, "new A() is static");
			Assert.AreEqual(0, m.Parameters.Count, "new A() parameter count");
			Assert.AreEqual("A", result.Type.FullName);
			
			result = Resolve<MemberResolveResult>(program.Replace("$", "$new A(10)$"));
			m = (IMethod)result.Member;
			Assert.AreEqual(1, m.Parameters.Count, "new A(10) parameter count");
			Assert.AreEqual("intVal", m.Parameters[0].Name, "new A(10) parameter");
			
			result = Resolve<MemberResolveResult>(program.Replace("$", "$new A(11.1)$"));
			m = (IMethod)result.Member;
			Assert.AreEqual(1, m.Parameters.Count, "new A(11.1) parameter count");
			Assert.AreEqual("dblVal", m.Parameters[0].Name, "new A(11.1) parameter");
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
			MemberResolveResult result = Resolve<MemberResolveResult>(program);
			IMethod m = (IMethod)result.Member;
			Assert.IsNotNull(m);
			Assert.AreEqual("A", result.Type.ReflectionName);
			Assert.AreEqual(0, m.Parameters.Count);
		}
		
		[Test, Ignore("Not implemented")]
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
			MemberResolveResult mrr = Resolve<MemberResolveResult>(program, "base(b)");
			Assert.AreEqual("A..ctor", mrr.Member.FullName);
			
			mrr = Resolve<MemberResolveResult>(program, "base(c)");
			Assert.AreEqual("B..ctor", mrr.Member.FullName);
			
			mrr = Resolve<MemberResolveResult>(program, "this(0)");
			Assert.AreEqual("C..ctor", mrr.Member.FullName);
		}
	}
}
