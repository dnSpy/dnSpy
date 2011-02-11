// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
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
