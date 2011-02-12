// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture, Ignore("LINQ not yet implemented")]
	public class LinqTests : ResolverTestBase
	{
		[Test]
		public void SimpleLinqTest()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		var r = from e in input
			where e.StartsWith(""/"")
			select e.Trim();
		r.ToString();
	}
}
";
			LocalResolveResult lrr = Resolve<LocalResolveResult>(program.Replace("where e", "where $e$"));
			Assert.AreEqual("System.String", lrr.Type.ReflectionName);
			lrr = Resolve<LocalResolveResult>(program.Replace("select e", "select $e$"));
			Assert.AreEqual("System.String", lrr.Type.ReflectionName);
			lrr = Resolve<LocalResolveResult>(program.Replace("from e", "from $e$"));
			Assert.AreEqual("System.String", lrr.Type.ReflectionName);
			
			lrr = Resolve<LocalResolveResult>(program.Replace("r.ToString", "$r$.ToString"));
			Assert.AreEqual("System.Collections.Generic.IEnumerable", lrr.Type.FullName);
			Assert.AreEqual("System.String", ((ParameterizedType)lrr.Type).TypeArguments[0].FullName);
		}
		
		[Test]
		public void LinqGroupTest()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		var r = from e in input
			group e.ToUpper() by e.Length;
		$r$.ToString();
	}
}
";
			LocalResolveResult lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.IEnumerable", lrr.Type.FullName);
			ParameterizedType rt = (ParameterizedType)((ParameterizedType)lrr.Type).TypeArguments[0];
			Assert.AreEqual("System.Linq.IGrouping", rt.FullName);
			Assert.AreEqual("System.Int32", rt.TypeArguments[0].FullName);
			Assert.AreEqual("System.String", rt.TypeArguments[1].FullName);
		}
		
		[Test]
		public void LinqQueryableGroupTest()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(IQueryable<string> input) {
		var r = from e in input
			group e.ToUpper() by e.Length;
		$r$.ToString();
	}
}
";
			LocalResolveResult lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Linq.IQueryable", lrr.Type.FullName);
			ParameterizedType rt = (ParameterizedType)((ParameterizedType)lrr.Type).TypeArguments[0];
			Assert.AreEqual("System.Linq.IGrouping", rt.FullName);
			Assert.AreEqual("System.Int32", rt.TypeArguments[0].FullName);
			Assert.AreEqual("System.String", rt.TypeArguments[1].FullName);
		}
		
		[Test]
		public void ParenthesizedLinqTest()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		$(from e in input select e.Length)$.ToArray();
	}
}
";
			ResolveResult rr = Resolve<ResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.IEnumerable", rr.Type.FullName);
			Assert.AreEqual("System.Int32", ((ParameterizedType)rr.Type).TypeArguments[0].FullName);
		}
		
		[Test]
		public void LinqSelectReturnTypeTest()
		{
			string program = @"using System;
class TestClass { static void M() {
	$(from a in new XYZ() select a.ToUpper())$.ToString();
}}
class XYZ {
	public int Select<U>(Func<string, U> f) { return 42; }
}";
			ResolveResult rr = Resolve<ResolveResult>(program);
			Assert.AreEqual("System.Int32", rr.Type.FullName);
		}
		
		[Test]
		public void LinqQueryContinuationTest()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		var r = from x in input
			select x.GetHashCode() into x
			where x == 42
			select x * x;
		r.ToString();
	}
}
";
			LocalResolveResult lrr = Resolve<LocalResolveResult>(program.Replace("from x", "from $x$"));
			Assert.AreEqual("System.String", lrr.Type.ReflectionName);
			lrr = Resolve<LocalResolveResult>(program.Replace("select x.G", "select $x$.G"));
			Assert.AreEqual("System.String", lrr.Type.ReflectionName);
			lrr = Resolve<LocalResolveResult>(program.Replace("into x", "into $x$"));
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
			lrr = Resolve<LocalResolveResult>(program.Replace("where x", "where $x$"));
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
			lrr = Resolve<LocalResolveResult>(program.Replace("select x * x", "select x * $x$"));
			Assert.AreEqual("System.Int32", lrr.Type.ReflectionName);
			
			lrr = Resolve<LocalResolveResult>(program.Replace("r.ToString", "$r$.ToString"));
			Assert.AreEqual("System.Collections.Generic.IEnumerable`1[[System.Int32]]", lrr.Type.ReflectionName);
		}
	}
}
