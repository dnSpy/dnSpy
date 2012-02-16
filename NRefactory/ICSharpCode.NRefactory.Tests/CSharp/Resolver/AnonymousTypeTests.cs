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
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class AnonymousTypeTests : ResolverTestBase
	{
		const string programStart = @"using System;
using System.Collections.Generic;
using System.Linq;
class Test {
	void M(IEnumerable<string> list1, IEnumerable<int> list2) {
		";
		const string programEnd = " } }";
		
		[Test]
		public void Zip()
		{
			string program = programStart + "$var$ q = list1.Zip(list2, (a,b) => new { a, b });" + programEnd;
			var rr = Resolve<TypeResolveResult>(program);
			Assert.AreEqual("System.Collections.Generic.IEnumerable", rr.Type.FullName);
			var type = (AnonymousType)((ParameterizedType)rr.Type).TypeArguments.Single();
			Assert.AreEqual(TypeKind.Anonymous, type.Kind);
			Assert.AreEqual(2, type.Properties.Count);
			Assert.AreEqual("a", type.Properties[0].Name);
			Assert.AreEqual("b", type.Properties[1].Name);
			Assert.AreEqual("System.String", type.Properties[0].ReturnType.ReflectionName);
			Assert.AreEqual("System.Int32", type.Properties[1].ReturnType.ReflectionName);
		}
		
		[Test]
		public void ZipItem1()
		{
			string program = programStart + "var q = list1.Zip(list2, (a,b) => new { $Item1 = a$, Item2 = b });" + programEnd;
			var rr = Resolve<MemberResolveResult>(program);
			Assert.AreEqual(TypeKind.Anonymous, rr.Member.DeclaringType.Kind);
			Assert.AreEqual("Item1", rr.Member.Name);
			Assert.AreEqual(EntityType.Property, rr.Member.EntityType);
			Assert.AreEqual("System.String", rr.Member.ReturnType.FullName);
		}
	}
}
