// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	[TestFixture]
	public class UnsafeCodeTests : ResolverTestBase
	{
		[Test, Ignore("Parser returns incorrect positions")]
		public void FixedStatement()
		{
			string program = @"using System;
class TestClass {
	static void Main(byte[] a) {
		fixed (byte* p = a) {
			a = $p$;
		} } }";
			
			var lrr = Resolve<LocalResolveResult>(program);
			Assert.AreEqual("System.Byte*", lrr.Type.ReflectionName);
			
			var rr = Resolve<ResolveResult>(program.Replace("$p$", "$*p$"));
			Assert.AreEqual("System.Byte", lrr.Type.ReflectionName);
		}
	}
}
