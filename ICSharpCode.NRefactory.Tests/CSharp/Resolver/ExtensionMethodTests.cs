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
using System.Linq;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
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
			InvocationResolveResult mrr;
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$a.F(1)$"));
			Assert.AreEqual("XN.XC.F", mrr.Member.FullName);
			Assert.AreEqual("System.Int32", mrr.Member.Parameters[1].Type.FullName);
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$a.F(\"text\")$"));
			Assert.AreEqual("XN.XC.F", mrr.Member.FullName);
			Assert.AreEqual("System.String", mrr.Member.Parameters[1].Type.FullName);
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$b.F(1)$"));
			Assert.AreEqual("B.F", mrr.Member.FullName);
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$b.F(\"text\")$"));
			Assert.AreEqual("XN.XC.F", mrr.Member.FullName);
			Assert.AreEqual("System.String", mrr.Member.Parameters[1].Type.FullName);
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$c.F(1)$"));
			Assert.AreEqual("C.F", mrr.Member.FullName);
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$c.F(\"text\")$"));
			Assert.AreEqual("C.F", mrr.Member.FullName);
		}
		
		[Test]
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
			CSharpInvocationResolveResult mrr;
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$\"text\".ToInt32()$"));
			Assert.AreEqual("XC.ToInt32", mrr.Member.FullName);
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$args.Slice(1, 2)$"));
			Assert.AreEqual("XC.Slice", mrr.Member.FullName);
			Assert.AreEqual("System.String[]", mrr.Type.ReflectionName);
			
			mrr = Resolve<CSharpInvocationResolveResult>(program.Replace("$", "$args.Filter(delegate { return true; })$"));
			Assert.AreEqual("XC.Filter", mrr.Member.FullName);
			Assert.AreEqual("System.Collections.Generic.IEnumerable`1[[System.String]]", mrr.Type.ReflectionName);
		}
		
		[Test]
		public void FirstIsEligibleExtensionMethod()
		{
			string program = @"using System; using System.Collections.Generic;
public static class XC {
	$public static TSource First<TSource>(this IEnumerable<TSource> source) {}$
}
";
			var mrr = Resolve<MemberResolveResult>(program);
			var targetType = compilation.FindType(typeof(string[]));
			IType[] inferredTypes;
			bool isEligible = CSharpResolver.IsEligibleExtensionMethod(targetType, (IMethod)mrr.Member, true, out inferredTypes);
			Assert.IsTrue(isEligible);
			Assert.AreEqual(1, inferredTypes.Length);
			Assert.AreEqual("System.String", inferredTypes[0].ReflectionName);
		}
		
		
		[Test]
		public void InferTypeFromOverwrittenMethodArguments()
		{
			string program = @"using System.Collections.Generic;
		using System.Linq;
		
		public class A { }
		
		public class B : A { }
		
		class Program
		{
			static void Main(string[] args)
			{
				IEnumerable<B> list = new List<B>();
				var arr = $list.ToArray<A>()$;
			}
		}
";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("A[]", rr.Type.ReflectionName);
			Assert.AreEqual("System.Linq.Enumerable.ToArray", rr.Member.FullName);
			Assert.AreEqual("A", ((SpecializedMethod)rr.Member).TypeArguments.Single().ReflectionName);
		}
		
		[Test]
		public void TypeInferenceBasedOnTargetTypeAndArgumentType()
		{
			string program = @"using System.Collections.Generic;
		using System.Linq;
		
		public class A { }
		public class B : A { }
		
		static class Program
		{
			static void Main(A a, B b)
			{
				var x = $b.Choose(a)$;
			}
			
			public static T Choose<T>(this T a, T b) { }
		}
";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("A", rr.Type.ReflectionName);
		}
		
		[Test]
		public void PartiallySpecializedMethod()
		{
			string program = @"using System.Collections.Generic;
		using System.Linq;
		
		public class A { }
		public class B : A { }
		
		static class Program
		{
			static void Main(A a, B b)
			{
				var x = $b.Choose$(a);
			}
			
			public static T Choose<T>(this T a, T b) { }
		}
";
			var rr = Resolve<MethodGroupResolveResult>(program);
			Assert.IsFalse(rr.Methods.Any());
			// We deliberately do not specialize the method unless partial specialization is requested explicitly.
			// This is because the actual type (when considering the whole invocation, not just the method group)
			// is actually A.
			Assert.AreEqual("``0", rr.GetExtensionMethods().Single().Single().ReturnType.ReflectionName);
			Assert.AreEqual("``0", rr.GetEligibleExtensionMethods(false).Single().Single().ReturnType.ReflectionName);
			Assert.AreEqual("B", rr.GetEligibleExtensionMethods(true).Single().Single().ReturnType.ReflectionName);
		}
		
		[Test]
		public void CreateDelegateFromExtensionMethod()
		{
			string program = @"using System;
	static class Program
	{
		static void Main() {
			Func<string> f = $"""".id$;
		}
		static string id(this string x) { return x; }
	}
";
			Conversion c = GetConversion(program);
			Assert.IsTrue(c.IsValid);
			Assert.IsTrue(c.IsMethodGroupConversion);
			Assert.AreEqual("Program.id", c.Method.FullName);
		}
		
		[Test]
		public void InferDelegateTypeFromExtensionMethod()
		{
			string program = @"using System;
	static class Program
	{
		static void Main() {
			$call("""".id)$;
		}
		
		static string id(this string x) { return x; }
		static T call<T>(Func<T> f) { }
	}
";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("System.String", rr.Type.FullName);
		}
	}
}
