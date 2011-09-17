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
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class ResolveAtLocationTests : ResolverTestBase
	{
		[Test]
		public void UsingDeclaration()
		{
			Assert.IsNull(ResolveAtLocation("usi$ng System;"));
		}
		
		[Test]
		public void UsingDeclarationNamespace()
		{
			var rr = ResolveAtLocation<NamespaceResolveResult>("using $System;");
			Assert.AreEqual("System", rr.NamespaceName);
		}
		
		[Test]
		public void CatchClauseVariable()
		{
			var rr = ResolveAtLocation<LocalResolveResult>("using System; public class A { void M() { try { } catch (Exception e$x) { } } }");
			Assert.AreEqual("ex", rr.Variable.Name);
			Assert.AreEqual("System.Exception", rr.Type.FullName);
		}
		
		[Test]
		public void MethodInvocation()
		{
			var rr = ResolveAtLocation<CSharpInvocationResolveResult>(@"using System;
class A { void M() {
	Console.W$riteLine(1);
}}");
			Assert.AreEqual("System.Console.WriteLine", rr.Member.FullName);
			Assert.AreEqual("System.Int32", rr.Member.Parameters[0].Type.Resolve(context).FullName);
		}
		
		[Test]
		public void ImplicitlyTypedVariable()
		{
			var rr = ResolveAtLocation<TypeResolveResult>(@"using System;
class A { void M() {
	v$ar x = Environment.TickCount;
}}");
			Assert.AreEqual("System.Int32", rr.Type.FullName);
		}
		
		[Test, Ignore("Parser returns incorrect positions")]
		public void BaseCtorCall()
		{
			var rr = ResolveAtLocation<InvocationResolveResult>(@"using System;
class A { public A() : ba$se() {} }");
			Assert.AreEqual("System.Object..ctor", rr.Member.FullName);
		}
		
		[Test]
		public void Field()
		{
			var rr = ResolveAtLocation<MemberResolveResult>("public class A { int te$st; }");
			Assert.AreEqual("test", rr.Member.Name);
		}
		
		[Test]
		public void Field1InLineWithTwoFields()
		{
			var rr = ResolveAtLocation<MemberResolveResult>("public class A { int te$st, test2; }");
			Assert.AreEqual("test", rr.Member.Name);
		}
		
		[Test]
		public void Field2InLineWithTwoFields()
		{
			var rr = ResolveAtLocation<MemberResolveResult>("public class A { int test, te$st2; }");
			Assert.AreEqual("test2", rr.Member.Name);
		}
		
		[Test]
		public void Event()
		{
			var rr = ResolveAtLocation<MemberResolveResult>("public class A { event EventHandler Te$st; }");
			Assert.AreEqual("Test", rr.Member.Name);
		}
		
		[Test]
		public void Event1InLineWithTwoEvents()
		{
			var rr = ResolveAtLocation<MemberResolveResult>("public class A { event EventHandler Te$st, Test2; }");
			Assert.AreEqual("Test", rr.Member.Name);
		}
		
		[Test]
		public void Event2InLineWithTwoEvents()
		{
			var rr = ResolveAtLocation<MemberResolveResult>("public class A { event EventHandler Test, Te$st2; }");
			Assert.AreEqual("Test2", rr.Member.Name);
		}
	}
}
