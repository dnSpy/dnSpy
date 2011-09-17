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
	public class LocalTypeInferenceTests : ResolverTestBase
	{
		[Test]
		public void TypeInferenceTest()
		{
			string program = @"class TestClass {
	static void Test() {
		var a = 3;
		$a$.ToString();
	}
}
";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Int32", lrr.Type.FullName);
		}
		
		[Test]
		public void TypeInferenceCycleTest()
		{
			string program = @"class TestClass {
	static void Test() {
		var a = a;
		$a$.ToString();
	}
}
";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreSame(SharedTypes.UnknownType, lrr.Type);
		}
		
		[Test]
		public void InvalidAnonymousTypeDeclaration()
		{
			// see SD-1393
			string program = @"using System;
class TestClass {
	static void Main() {
			var contact = {id = 54321};
			$contact$.ToString();
		} }";
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual(SharedTypes.UnknownType, lrr.Type);
		}
	}
}
