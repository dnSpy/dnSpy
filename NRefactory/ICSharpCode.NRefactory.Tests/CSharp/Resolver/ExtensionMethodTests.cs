// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class ExtensionMethodTests : ResolverTestBase
	{
		[Test]
		public void ExtensionMethodsTest()
		{
			string program = @"using XN;
class TestClass {
	static void Test(A a, B b, C c) {
		$;
	}
}
class A { }
class B {
	public void F(int i) { }
}
class C {
	public void F(object obj) { }
}
namespace XN {
	public static class XC {
		public static void F(this object obj, int i) { }
		public static void F(this object obj, string s) { }
	}
}
";
			MemberResolveResult mrr;
			
			mrr = Resolve<MemberResolveResult>(program.Replace("$", "$a.F(1)$"));
			Assert.AreEqual("XN.XC.F", mrr.Member.FullName);
			Assert.AreEqual("System.Int32", ((IMethod)mrr.Member).Parameters[1].Type.Resolve(context).FullName);
			
			mrr = Resolve<MemberResolveResult>(program.Replace("$", "$a.F(\"text\")$"));
			Assert.AreEqual("XN.XC.F", mrr.Member.FullName);
			Assert.AreEqual("System.String", ((IMethod)mrr.Member).Parameters[1].Type.Resolve(context).FullName);
			
			mrr = Resolve<MemberResolveResult>(program.Replace("$", "$b.F(1)$"));
			Assert.AreEqual("B.F", mrr.Member.FullName);
			
			mrr = Resolve<MemberResolveResult>(program.Replace("$", "$b.F(\"text\")$"));
			Assert.AreEqual("XN.XC.F", mrr.Member.FullName);
			Assert.AreEqual("System.String", ((IMethod)mrr.Member).Parameters[1].Type.Resolve(context).FullName);
			
			mrr = Resolve<MemberResolveResult>(program.Replace("$", "$c.F(1)$"));
			Assert.AreEqual("C.F", mrr.Member.FullName);
			
			mrr = Resolve<MemberResolveResult>(program.Replace("$", "$c.F(\"text\")$"));
			Assert.AreEqual("C.F", mrr.Member.FullName);
		}
		
		[Test, Ignore("Anonymous methods not yet implemented")]
		public void ExtensionMethodsTest2()
		{
			string program = @"using System; using System.Collections.Generic;
class TestClass {
	static void Test(string[] args) {
		$;
	}
}
public static class XC {
	public static int ToInt32(this string s) { return int.Parse(s); }
	public static T[] Slice<T>(this T[] source, int index, int count) { throw new NotImplementedException(); }
	public static IEnumerable<T> Filter<T>(this IEnumerable<T> source, Predicate<T> predicate) { throw new NotImplementedException(); }
}
";
			MemberResolveResult mrr;
			
			mrr = Resolve<MemberResolveResult>(program.Replace("$", "$\"text\".ToInt32()$"));
			Assert.AreEqual("XC.ToInt32", mrr.Member.FullName);
			
			mrr = Resolve<MemberResolveResult>(program.Replace("$", "$args.Slice(1, 2)$"));
			Assert.AreEqual("XC.Slice", mrr.Member.FullName);
			Assert.AreEqual("System.String[]", mrr.Type.ReflectionName);
			
			mrr = Resolve<MemberResolveResult>(program.Replace("$", "$args.Filter(delegate { return true; })$"));
			Assert.AreEqual("XC.Filter", mrr.Member.FullName);
			Assert.AreEqual("System.Collections.Generic.IEnumerable{System.String}", mrr.Type.ReflectionName);
		}
	}
}
