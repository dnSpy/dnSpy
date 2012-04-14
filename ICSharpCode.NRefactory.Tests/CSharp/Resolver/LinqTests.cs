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
	public class LinqTests : ResolverTestBase
	{
		[Test]
		public void SimpleLinq()
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
		public void Group()
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
		public void QueryableGroup()
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
		public void Parenthesized()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		$(from e in input select e.Length)$.ToArray();
	}
}
";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("System.Linq.Enumerable.Select", rr.Member.FullName);
			Assert.AreEqual("System.Collections.Generic.IEnumerable", rr.Type.FullName);
			Assert.AreEqual("System.Int32", ((ParameterizedType)rr.Type).TypeArguments[0].FullName);
		}
		
		[Test]
		public void SelectReturnType()
		{
			string program = @"using System;
class TestClass { static void M() {
	$(from a in new XYZ() select a.ToUpper())$.ToString();
}}
class XYZ {
	public int Select<U>(Func<string, U> f) { return 42; }
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.AreEqual("XYZ.Select", rr.Member.FullName);
			Assert.AreEqual("System.Int32", rr.Type.FullName);
		}
		
		[Test]
		public void Continuation()
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
		
		[Test]
		public void OrderingWithSelectCall()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		$var$ r = from x in input
			orderby x.Length
			select x + x;
	}
}
";
			TypeResolveResult rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.IEnumerable`1[[System.String]]", rr.Type.ReflectionName);
		}
		
		[Test]
		public void OrderingWithoutSelectCall()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		$var$ r = from x in input
			orderby x.Length
			select x;
	}
}
";
			TypeResolveResult rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Linq.IOrderedEnumerable`1[[System.String]]", rr.Type.ReflectionName);
		}
		
		[Test]
		public void OrderingWithSelectCallDueToSecondRangeVariable1()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		$var$ r = from x in input
			from y in input
			orderby x.Length
			select x;
	}
}
";
			TypeResolveResult rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.IEnumerable`1[[System.String]]", rr.Type.ReflectionName);
		}
		
		[Test]
		public void OrderingWithSelectCallDueToSecondRangeVariable2()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		$var$ r = from x in input
			join y in input on x equals y
			orderby x.Length
			select x;
	}
}
";
			TypeResolveResult rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.IEnumerable`1[[System.String]]", rr.Type.ReflectionName);
		}
		
		[Test]
		public void OrderingWithSelectCallDueToSecondRangeVariable3()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		$var$ r = from x in input
			join y in input on x equals y into g
			orderby x.Length
			select x;
	}
}
";
			TypeResolveResult rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.IEnumerable`1[[System.String]]", rr.Type.ReflectionName);
		}
		
		[Test]
		public void OrderingWithSelectCallDueToSecondRangeVariable4()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		$var$ r = from x in input
			let y = x
			orderby x.Length
			select x;
	}
}
";
			TypeResolveResult rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.IEnumerable`1[[System.String]]", rr.Type.ReflectionName);
		}
		
		[Test]
		public void DegenerateQuery()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	void Test(string[] input) {
		$var$ r = from x in input select x;
	}
}
";
			TypeResolveResult rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.IEnumerable`1[[System.String]]", rr.Type.ReflectionName);
		}
		
		[Test]
		public void GroupJoinWithCustomMethod()
		{
			string program = @"using System;
using System.Collections.Generic;
class TestClass { static void M(long [] args) {
	var q = (from a in new XYZ() join b in args on a equals b into g select g);
}}
class XYZ {
	public XYZ GroupJoin<T, R>(IEnumerable<T> f, Func<string, object> key1, Func<T, object> key2, Func<string, decimal, R> s) { return this; }
	public int Select<U>(Func<string, U> f) { return 42; }
}";
			var local = Resolve<LocalResolveResult>(program.Replace("into g", "into $g$"));
			Assert.AreEqual("System.Decimal", local.Type.FullName);
			
			local = Resolve<LocalResolveResult>(program.Replace("select g", "select $g$"));
			Assert.AreEqual("System.Decimal", local.Type.FullName);
			
			var trr = Resolve<TypeResolveResult>(program.Replace("var", "$var$"));
			Assert.AreEqual("XYZ", trr.Type.FullName); // because 'Select' is done as part of GroupJoin()
		}
		
		[Test]
		public void GroupJoinWithOverloadedCustomMethod()
		{
			string program = @"using System;
using System.Collections.Generic;
class TestClass
{
	static void M(string[] args)
	{
		var q = (from a in new XYZ() $join b in args on a equals b into g$ select g.ToUpper());
	}
}
class XYZ
{
	public int GroupJoin(IEnumerable<string> f, Func<string, object> key1, Func<string, object> key2, Func<string, int, int> s) { return 0; }
	public decimal GroupJoin(IEnumerable<string> f, Func<string, object> key1, Func<string, object> key2, Func<string, string, string> s) { return 0; }
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("GroupJoin", rr.Member.Name);
			Assert.AreEqual("System.Decimal", rr.Type.FullName);
			
			rr = Resolve<CSharpInvocationResolveResult>(program.Replace("g.ToUpper()", "g.CompareTo(42)"));
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("GroupJoin", rr.Member.Name);
			Assert.AreEqual("System.Int32", rr.Type.FullName);
		}
		
		[Test]
		public void GroupWithQueryContinuation()
		{
			string program = @"using System; using System.Linq;
class TestClass
{
	static void M(string[] args)
	{
		var query =
		from w in ""one to three"".Split()
			group w by w.Length into g
			orderby g.Key descending
			select new { g.Key, Count = g.Count(), Avg = g.Average ($w$ => w.Length) };
	}
}";
			var rr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.String", rr.Type.FullName);
		}
		
		[Test]
		public void SelectManyInvocation()
		{
			string program = @"using System; using System.Linq;
class TestClass
{
	static void M(string[] args)
	{
		var query = from w in args $from c in w$ select c - '0';
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("SelectMany", rr.Member.Name);
			Assert.AreEqual(3, rr.Member.Parameters.Count);
			var typeArguments = ((SpecializedMethod)rr.Member).TypeArguments;
			Assert.AreEqual(3, typeArguments.Count);
			Assert.AreEqual("System.String", typeArguments[0].ReflectionName, "TSource");
			Assert.AreEqual("System.Char", typeArguments[1].ReflectionName, "TCollection");
			Assert.AreEqual("System.Int32", typeArguments[2].ReflectionName, "TResult");
		}
		
		[Test]
		public void SelectManyInvocationWithTransparentIdentifier()
		{
			string program = @"using System; using System.Linq;
class TestClass
{
	static void M(string[] args)
	{
		var query = from w in args $from c in w$ orderby c select c - '0';
	}
}";
			var rr = Resolve<CSharpInvocationResolveResult>(program);
			Assert.IsFalse(rr.IsError);
			Assert.AreEqual("SelectMany", rr.Member.Name);
			Assert.AreEqual(3, rr.Member.Parameters.Count);
			var typeArguments = ((SpecializedMethod)rr.Member).TypeArguments;
			Assert.AreEqual(3, typeArguments.Count);
			Assert.AreEqual("System.String", typeArguments[0].ReflectionName, "TSource");
			Assert.AreEqual("System.Char", typeArguments[1].ReflectionName, "TCollection");
			Assert.AreEqual(TypeKind.Anonymous, typeArguments[2].Kind, "TResult");
		}
		
		[Test]
		public void FromClauseDoesNotResolveToSourceVariable()
		{
			string program = @"using System; using System.Linq;
class TestClass {
	static void M(string[] args) {
		var query = $from w in args$ select int.Parse(w);
	}}";
			var rr = Resolve<ConversionResolveResult>(program);
			Assert.AreEqual("System.String[]", rr.Type.ReflectionName);
			Assert.AreEqual(Conversion.IdentityConversion, rr.Conversion);
		}
	}
}
